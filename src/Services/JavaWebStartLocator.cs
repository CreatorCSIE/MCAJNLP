using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCAJNLP.Services
{
    /// <summary>
    /// 一个 javaws 候选项及其验证结论。
    /// RejectReason 为 null 表示通过验证、可以用来启动。
    /// </summary>
    public sealed class JavawsCandidate
    {
        public required string JavawsPath { get; init; }
        public required string Source { get; init; }
        public string? JavaVersion { get; init; }
        public bool Is64BitJvm { get; init; }
        public string? RejectReason { get; init; }

        public bool IsUsable => RejectReason is null;
    }

    /// <summary>
    /// Java Web Start（javaws）探针。
    ///
    /// 探测优先级（越靠前越可靠）：
    ///   1. Windows 注册表 JavaSoft 键（64 位视图优先，再 32 位视图）
    ///   2. Program Files 安装目录扫描（64 位目录优先，再 Program Files (x86)）
    ///   3. 环境变量 JAVA_HOME / JDK_HOME
    ///   4. 环境变量 PATH
    ///
    /// 注册表与 PATH 都会留下卸载残留（例如 Oracle 的 32 位 java8path 转发壳：
    /// 进程能创建、但同目录 java.exe 立刻以 0xC0000005 崩溃，表现为“点了启动没任何反应”），
    /// 因此候选项只是路径线索，必须逐个用 java.exe -version 实测确认它是真实可用的 Java 8 才允许使用。
    /// </summary>
    public static class JavaWebStartLocator
    {
        private const string SourceRegistry = "注册表";
        private const string SourceProgramFiles = "安装目录扫描";
        private const string SourceJavaHome = "JAVA_HOME";
        private const string SourcePath = "PATH";
        private const string SourcePosix = "常规 JVM 目录";

        private static readonly bool OnWindows = OperatingSystem.IsWindows();
        private static readonly string ExeSuffix = OnWindows ? ".exe" : "";
        private static readonly string JavawsFileName = "javaws" + ExeSuffix;
        private static readonly StringComparer PathTextComparer = OnWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(3);
        private const int InstallRootScanDepth = 3;

        private static readonly Regex RegistryValueLine = new(
            @"^\s+(?<n>\S+)\s+REG_(?:EXPAND_)?SZ\s+(?<d>.+?)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private static readonly Regex VersionLine = new(
            "version\\s+\"(?<v>[0-9][^\"]*)\"", RegexOptions.Compiled);

        private static readonly string[] RegistryKeys =
        {
            @"HKLM\SOFTWARE\JavaSoft\Java Web Start",
            @"HKLM\SOFTWARE\JavaSoft\Java Runtime Environment",
            @"HKLM\SOFTWARE\JavaSoft\Java Development Kit",
        };

        private static readonly string[] RegistryPathValues = { "Home", "RuntimeLib", "JavaHome", "Path" };

        private static readonly string[] VendorDirectoryNames =
        {
            "Java", "Eclipse Adoptium", "AdoptOpenJDK", "Amazon Corretto",
            "Zulu", "Microsoft", "BellSoft", "Semeru", "Eclipse Foundation",
        };

        private static readonly string[] PosixJvmRoots =
        {
            "/usr/lib/jvm", "/usr/java", "/opt/java", "/usr/lib64/jvm",
            "/Library/Java/JavaVirtualMachines",
        };

        /// <summary>
        /// 按优先级收集候选项并逐个验证；返回值保持“越靠前越优先”的顺序。
        /// </summary>
        public static async Task<IReadOnlyList<JavawsCandidate>> ProbeAsync()
        {
            var discovered = await DiscoverAsync().ConfigureAwait(false);

            // java -version 需要起进程，逐个跑太慢，这里并发验证后再按原顺序返回
            var pending = discovered.Select(pair => ValidateAsync(pair.JavawsPath, pair.Source)).ToArray();
            return await Task.WhenAll(pending).ConfigureAwait(false);
        }

        /// <summary>
        /// 用选定的 javaws 启动 JNLP，并观察它是否“创建即退出”（转发壳就是这样）。
        /// </summary>
        public static async Task<(bool Launched, string Detail)> TryLaunchAsync(
            JavawsCandidate candidate, string jnlpPath, string workingDirectory, int livenessCheckMs = 1200)
        {
            Process? process = null;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = candidate.JavawsPath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                startInfo.ArgumentList.Add(jnlpPath);

                process = Process.Start(startInfo);
                if (process is null)
                {
                    return (false, "Process.Start 未返回进程对象");
                }

                await Task.Delay(livenessCheckMs).ConfigureAwait(false);

                if (!process.HasExited)
                {
                    return (true, candidate.JavawsPath);
                }

                int exitCode = SafeExitCode(process);
                // 独立 JVM 模式下 javaws 会正常交接后以 0 退出，不能算失败
                return exitCode == 0
                    ? (true, candidate.JavawsPath)
                    : (false, $"启动后 {livenessCheckMs}ms 内退出，退出码 {FormatExitCode(exitCode)}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// 最后一招：交给系统的 .jnlp 文件关联（通常指向已注册的 javaws）。
        /// </summary>
        public static (bool Launched, string Detail) LaunchByFileAssociation(string jnlpPath, string workingDirectory)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = jnlpPath,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = true,
                };
                using var process = Process.Start(startInfo);
                return process is null
                    ? (false, "文件关联未能创建进程")
                    : (true, "系统 .jnlp 文件关联");
            }
            catch (Exception ex)
            {
                return (false, "文件关联唤起失败：" + ex.Message);
            }
        }

        /// <summary>
        /// 生成给使用者看的探测报告（每行一个候选项，含被拒原因）。
        /// </summary>
        public static string Describe(IReadOnlyList<JavawsCandidate> candidates)
        {
            if (candidates.Count == 0)
            {
                return "（未找到任何 javaws 候选项）";
            }

            var lines = candidates.Select(c => c.IsUsable
                ? $"[可用] {c.JavawsPath}\n       Java {c.JavaVersion}（{(c.Is64BitJvm ? "64" : "32")} 位）· 来源：{c.Source}"
                : $"[不可用] {c.JavawsPath}\n       {c.RejectReason} · 来源：{c.Source}");
            return string.Join("\n", lines);
        }

        private static string FormatExitCode(int code)
        {
            return code < 0
                ? $"0x{((uint)code):X8}（{code}）"
                : code.ToString();
        }

        private static int SafeExitCode(Process process)
        {
            try
            {
                return process.ExitCode;
            }
            catch
            {
                return -1;
            }
        }

        // ---------- 候选项收集 ----------

        private readonly record struct Found(string JavawsPath, string Source);

        private static async Task<List<Found>> DiscoverAsync()
        {
            var ordered = new List<Found>();
            var seen = new HashSet<string>(PathTextComparer);

            void AddAll(IEnumerable<string> paths, string source)
            {
                foreach (string path in paths)
                {
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        continue;
                    }
                    string? full = ToExistingFullPath(path);
                    if (full is not null && seen.Add(full))
                    {
                        ordered.Add(new Found(full, source));
                    }
                }
            }

            if (OnWindows)
            {
                // 1. 注册表：先 64 位视图，再 32 位视图
                var registry64 = await ReadRegistryJavaRootsAsync("/reg:64").ConfigureAwait(false);
                var registry32 = await ReadRegistryJavaRootsAsync("/reg:32").ConfigureAwait(false);
                AddAll(registry64.SelectMany(FromJavaHome), SourceRegistry);
                AddAll(registry32.SelectMany(FromJavaHome), SourceRegistry + "（32 位视图）");

                // 2. 安装目录扫描：先 64 位 Program Files，再 x86
                foreach (string root in ProgramFilesRoots(false))
                {
                    AddAll(ScanForJavaws(root, InstallRootScanDepth), SourceProgramFiles);
                }
                foreach (string root in ProgramFilesRoots(true))
                {
                    AddAll(ScanForJavaws(root, InstallRootScanDepth), SourceProgramFiles + "（32 位）");
                }
            }
            else
            {
                foreach (string root in PosixJvmRoots)
                {
                    AddAll(ScanForJavaws(root, InstallRootScanDepth), SourcePosix);
                }
            }

            // 3. JAVA_HOME / JDK_HOME
            AddAll(
                new[] { Environment.GetEnvironmentVariable("JAVA_HOME"), Environment.GetEnvironmentVariable("JDK_HOME") }
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .SelectMany(v => FromJavaHome(v!)),
                SourceJavaHome);

            // 4. PATH（优先级最低：最容易命中 java8path 之类的转发壳）
            AddAll(FromPathVariable(), SourcePath);

            return ordered;
        }

        private static string? ToExistingFullPath(string path)
        {
            try
            {
                string full = Path.GetFullPath(path);
                return File.Exists(full) ? full : null;
            }
            catch
            {
                return null;
            }
        }

        private static IEnumerable<string> FromJavaHome(string javaHome)
        {
            string trimmed = javaHome.Trim().Trim('"');
            if (trimmed.Length == 0)
            {
                yield break;
            }

            // 注册表里既可能给出 JRE/JDK 根目录，也可能直接给出 ...\bin 目录
            string[] bases = { trimmed.TrimEnd('\\', '/'), Path.Combine(trimmed.TrimEnd('\\', '/'), "bin") };
            foreach (string baseDir in bases)
            {
                yield return Path.Combine(baseDir, JavawsFileName);
                yield return Path.Combine(baseDir, "jre", "bin", JavawsFileName);
            }
        }

        private static IEnumerable<string> FromPathVariable()
        {
            string? path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(path))
            {
                yield break;
            }

            foreach (string entry in path.Split(Path.PathSeparator))
            {
                string dir = entry.Trim().Trim('"');
                if (dir.Length == 0)
                {
                    continue;
                }
                yield return Path.Combine(dir, JavawsFileName);
            }
        }

        private static IEnumerable<string> ProgramFilesRoots(bool preferX86)
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string? pf = Environment.GetEnvironmentVariable(preferX86 ? "ProgramFiles(x86)" : "ProgramFiles");
            if (string.IsNullOrWhiteSpace(pf))
            {
                pf = preferX86
                    ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
                    : programFiles;
            }

            if (string.IsNullOrWhiteSpace(pf))
            {
                yield break;
            }

            foreach (string vendor in VendorDirectoryNames)
            {
                yield return Path.Combine(pf, vendor);
            }
        }

        private static IEnumerable<string> ScanForJavaws(string root, int maxDepth)
        {
            var results = new List<string>();
            if (!Directory.Exists(root))
            {
                return results;
            }

            var visited = new HashSet<string>(PathTextComparer);
            var queue = new Queue<(string Dir, int Depth)>();
            queue.Enqueue((root, 0));

            while (queue.Count > 0)
            {
                (string dir, int depth) = queue.Dequeue();

                string direct = Path.Combine(dir, JavawsFileName);
                if (File.Exists(direct))
                {
                    results.Add(direct);
                }

                if (depth >= maxDepth)
                {
                    continue;
                }

                IEnumerable<string> children;
                try
                {
                    children = Directory.EnumerateDirectories(dir);
                }
                catch
                {
                    continue; // 无权限的子目录直接跳过
                }

                foreach (string child in children)
                {
                    string normalized = child.TrimEnd('\\', '/');
                    if (visited.Add(normalized))
                    {
                        queue.Enqueue((normalized, depth + 1));
                    }
                }
            }

            return results;
        }

        private static async Task<List<string>> ReadRegistryJavaRootsAsync(string viewFlag)
        {
            var roots = new List<string>();
            var queries = RegistryKeys
                .Select(key => RunToolAsync("reg.exe", $"query \"{key}\" /s {viewFlag}"))
                .ToArray();

            foreach ((int? exitCode, string output, string? _) in await Task.WhenAll(queries).ConfigureAwait(false))
            {
                if (exitCode != 0 || string.IsNullOrEmpty(output))
                {
                    continue; // 键不存在（例如从未装过 Oracle Java）
                }

                foreach (Match match in RegistryValueLine.Matches(output))
                {
                    if (!RegistryPathValues.Contains(match.Groups["n"].Value, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string value = Environment.ExpandEnvironmentVariables(match.Groups["d"].Value.Trim().Trim('"'));
                    if (value.Length > 0)
                    {
                        roots.Add(value);
                    }
                }
            }

            return roots;
        }

        // ---------- 真实性验证 ----------

        private static async Task<JavawsCandidate> ValidateAsync(string javawsPath, string source)
        {
            JavawsCandidate Reject(string reason) => new()
            {
                JavawsPath = javawsPath,
                Source = source,
                RejectReason = reason,
            };

            string? binDir = Path.GetDirectoryName(javawsPath);
            if (binDir is null)
            {
                return Reject("路径异常");
            }

            string javaExe = Path.Combine(binDir, "java" + ExeSuffix);
            if (!File.Exists(javaExe))
            {
                return Reject("同目录没有 java.exe，属于残缺安装");
            }

            (int? exitCode, string output, string? startupError) probe = await RunToolAsync(javaExe, "-version").ConfigureAwait(false);

            if (probe.startupError is not null)
            {
                return Reject(probe.startupError);
            }
            if (probe.exitCode is null)
            {
                return Reject($"java.exe -version 在 {ToolTimeout.TotalSeconds:0} 秒内没有响应");
            }
            if (probe.exitCode != 0)
            {
                return Reject($"java.exe -version 异常退出（退出码 {FormatExitCode(probe.exitCode.Value)}），疑似卸载残留的转发壳");
            }
            if (string.IsNullOrWhiteSpace(probe.output))
            {
                return Reject("java.exe -version 无任何输出，疑似卸载残留的转发壳");
            }

            Match versionMatch = VersionLine.Match(probe.output);
            if (!versionMatch.Success)
            {
                return Reject("无法从 java -version 输出中解析出版本号");
            }

            string version = versionMatch.Groups["v"].Value;
            bool is64Bit = probe.output.Contains("64-Bit", StringComparison.OrdinalIgnoreCase)
                || probe.output.Contains("64-bit", StringComparison.OrdinalIgnoreCase)
                || probe.output.Contains("64 位", StringComparison.OrdinalIgnoreCase);

            if (!version.StartsWith("1.8", StringComparison.Ordinal))
            {
                return Reject($"检测到 Java {version}，不是 Java 8（Java 11 起 javaws 已被移除）");
            }

            return new JavawsCandidate
            {
                JavawsPath = javawsPath,
                Source = source,
                JavaVersion = version,
                Is64BitJvm = is64Bit,
            };
        }

        /// <summary>
        /// 运行一个命令行工具并收集输出。
        /// ExitCode 为 null 表示超时；StartupError 非空表示进程根本没跑起来（例如文件已被删除）。
        /// </summary>
        private static async Task<(int? ExitCode, string Output, string? StartupError)> RunToolAsync(string fileName, string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = new Process { StartInfo = psi };
            try
            {
                if (!process.Start())
                {
                    return (null, string.Empty, $"无法启动 {fileName}");
                }
            }
            catch (Exception ex)
            {
                return (null, string.Empty, $"无法启动 {fileName}：{ex.Message}");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            var bothStreams = Task.WhenAll(stdoutTask, stderrTask);

            if (await Task.WhenAny(bothStreams, Task.Delay(ToolTimeout)).ConfigureAwait(false) != bothStreams)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // 进程已自行退出
                }
                return (null, string.Empty, null); // null 退出码 + 无启动错误 = 超时
            }

            string output = stdoutTask.Result + Environment.NewLine + stderrTask.Result;
            int exitCode;
            try
            {
                exitCode = process.ExitCode;
            }
            catch
            {
                exitCode = -1;
            }
            return (exitCode, output, null);
        }
    }
}
