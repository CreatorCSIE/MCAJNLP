using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCAJNLP.Models;

namespace MCAJNLP.Services
{
    public static class ConfigManager
    {
        // 定位真正的根目录：由于二进制文件输出在 /build 中，上一级即为项目根目录 (..)
        public static string ConfigDirectory
        {
            get
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // 1. 判断是否在 bin 目录下运行（开发调试阶段）
                // 使用 Path.DirectorySeparatorChar 确保同时兼容 Windows 和 Linux/Mac
                string binSegment = $"{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}";
                bool isDev = baseDir.Contains(binSegment, StringComparison.OrdinalIgnoreCase);
                if (isDev)
                {
                    // 开发环境：往上跳一级（到 build/ 这一级），去读写里面的 config
                    return Path.GetFullPath(Path.Combine(baseDir, "..", "config"));
                }
                else
                {
                    // 生产环境：直接使用 exe 同级目录下的 config
                    return Path.Combine(baseDir, "config");
                }
            }
        }
        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "version.json");

        // 游戏资源根目录：LWJGL/、bin/、bg/ 等 JNLP 引用的资源都放在这里，
        // 也就是 config 目录的上一级（开发时是仓库根目录，发布包里是 exe 同级目录）。
        // 注意：不能返回 ConfigDirectory，否则生成的 codebase 会让 javaws 去 config/ 下找 jar，导致启动静默失败。
        public static string GetRootDir()
        {
            string parent = Path.GetFullPath(Path.Combine(ConfigDirectory, ".."));
            return parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public static Dictionary<string, VersionConfig> LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                    return new Dictionary<string, VersionConfig>();
                }

                string jsonContent = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<Dictionary<string, VersionConfig>>(jsonContent) 
                       ?? new Dictionary<string, VersionConfig>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载 JSON 失败: {ex.Message}");
                return new Dictionary<string, VersionConfig>();
            }
        }
        private static readonly JsonSerializerOptions SharedJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault // 统一过滤规则
        };

        public static void SaveConfig(Dictionary<string, VersionConfig> config)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
                
                
                string jsonContent = JsonSerializer.Serialize(config, SharedJsonOptions);
                File.WriteAllText(ConfigPath, jsonContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入 JSON 失败: {ex.Message}");
            }
        }
    }
}