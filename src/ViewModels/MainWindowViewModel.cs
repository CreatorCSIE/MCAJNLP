using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;
using Avalonia.Threading;
using Avalonia.Controls;
using MCAJNLP.Models;
using MCAJNLP.Services;

namespace MCAJNLP.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<string> VersionKeys { get; } = new();
        private Dictionary<string, VersionConfig> _allConfigs = new();

        // ==========================================
        // 【新增】1. 实时 JSON 预览属性
        // ==========================================
        private string _versionJsonPreview = "";
        public string VersionJsonPreview
        {
            get => _versionJsonPreview;
            set { _versionJsonPreview = value; OnPropertyChanged(); }
        }

        public MainWindowViewModel()
        {
            LoadAllVersions();
            Dispatcher.UIThread.Post(async () => 
            {
                await CheckVersionsAndRedirectAsync();
            });
            VType = "classic"; // 默认编辑阶段
            LoadSettings();
        }
        
        public class UserSettings
        {
            public string LastSelectedVersion { get; set; } = "";
            public string LastUsername { get; set; } = "Guest";
            public string LastServerIp { get; set; } = "";
            public string LastServerPort { get; set; } = "";
        }

        // ==========================================
        // 【新增】2. 内存刷新 JSON 预览的方法
        // ==========================================
        private static readonly JsonSerializerOptions SharedJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault // 统一过滤规则
        };
        private void UpdateJsonPreview()
        {
            try
            {
                // 直接序列化内存中的 _allConfigs，无需经过硬盘 I/O，极其丝滑
                VersionJsonPreview = JsonSerializer.Serialize(_allConfigs, SharedJsonOptions);
            }
            catch (Exception ex)
            {
                VersionJsonPreview = $"[Error] JSON 预览生成失败: {ex.Message}";
            }
        }

        public void LoadAllVersions()
        {
            _allConfigs = ConfigManager.LoadConfig();
            VersionKeys.Clear();
            foreach (var key in _allConfigs.Keys)
            {
                VersionKeys.Add(key);
            }
            
            // ⭐ 修复：如果还有版本，默认选第一个；如果没有版本了，必须显式清空选中状态！
            if (VersionKeys.Any()) 
            {
                SelectedLaunchKey = VersionKeys.First();
            }
            else
            {
                SelectedLaunchKey = ""; 
            }
            // 加载完成后，立即更新一次 JSON 预览
            UpdateJsonPreview();
        }

        // ==========================================
        // 【新增】3. 拖拽排序逻辑 (对应 MainWindow.axaml.cs 的调用)
        // ==========================================
        public void MoveVersion(string draggedKey, string targetKey)
        {
            int oldIndex = VersionKeys.IndexOf(draggedKey);
            int newIndex = VersionKeys.IndexOf(targetKey);
            if (oldIndex == -1 || newIndex == -1 || oldIndex == newIndex) return;

            // (A) 在绑定的 ObservableCollection 中移动位置（触发 UI 顺滑的重拍动画）
            VersionKeys.Move(oldIndex, newIndex);

            // (B) 关键：根据重排后的新顺序重新构建 Dictionary，以维护 json 文件的物理写入顺序
            var reorderedConfigs = new Dictionary<string, VersionConfig>();
            foreach (var key in VersionKeys)
            {
                if (_allConfigs.TryGetValue(key, out var config))
                {
                    reorderedConfigs[key] = config;
                }
            }
            _allConfigs = reorderedConfigs;

            // (C) 写入本地物理文件
            ConfigManager.SaveConfig(_allConfigs);

            // (D) 同步刷新右下角的实时 JSON 预览
            UpdateJsonPreview();
        }

        #region == 启动器业务 (Minecraft.html -> javaws 迁移) ==

        private string _selectedLaunchKey = "";
        public string SelectedLaunchKey
        {
            get => _selectedLaunchKey;
            set
            {
                if (_selectedLaunchKey != value)
                {
                    _selectedLaunchKey = value;
                    OnPropertyChanged();
                    OnLaunchVersionChanged();
                }
            }
        }

        private string _username = "Guest";
        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        private string _serverIp = "";
        public string ServerIp
        {
            get => _serverIp;
            set { _serverIp = value; OnPropertyChanged(); }
        }

        private string _serverPort = "25565";
        public string ServerPort
        {
            get => _serverPort;
            set { _serverPort = value; OnPropertyChanged(); }
        }

        private bool _isMultiplayerEnabled;
        public bool IsMultiplayerEnabled
        {
            get => _isMultiplayerEnabled;
            set { _isMultiplayerEnabled = value; OnPropertyChanged(); }
        }

        private string _controlsText = "";
        public string ControlsText
        {
            get => _controlsText;
            set { _controlsText = value; OnPropertyChanged(); }
        }

        private void OnLaunchVersionChanged()
        {
            if (string.IsNullOrEmpty(SelectedLaunchKey) || !_allConfigs.TryGetValue(SelectedLaunchKey, out var config)) 
            {
                // 彻底清空右侧状态，防止状态残留
                IsMultiplayerEnabled = false;
                ControlsText = "未选择任何版本，请先选择或配置一个游戏版本"; 
                return;
            }

            IsMultiplayerEnabled = config.Type == "classic_mp";

            var sb = new StringBuilder();
            sb.AppendLine("WASD to move");
            sb.AppendLine("Space to jump");
            sb.AppendLine("R to respawn at the spawn point.");

            if (config.HasSetSpawn != false) sb.AppendLine("Enter to set a new spawn point.");
            if (config.HasHuman == true) sb.AppendLine("G to spawn human");
            if (config.HasBlockMenu == true) sb.AppendLine("B to open block menu");
            if (!string.IsNullOrEmpty(config.InventoryKey)) sb.AppendLine($"{config.InventoryKey} to open inventory");
            if (config.HasDrop == true) sb.AppendLine("Q to drop things");
            if (config.HasSneak == true) sb.AppendLine("Shift to sneak");

            sb.AppendLine("F to toggle fog distance");
            sb.AppendLine("Escape to release mouse and open game menu");
            sb.AppendLine("1-9 or scrollwheel to change building block type");
            sb.AppendLine("Left mouse button to add a block");
            sb.AppendLine("Right mouse button to remove a block");
            sb.AppendLine("Middle mouse button to copy block type");

            if (config.Type == "classic_mp" || config.Type == "alpha" || config.Type == "beta")
            {
                sb.AppendLine("\nIn Multiplayer:");
                sb.AppendLine("T to chat");
                sb.AppendLine("Tab to list players");
            }

            ControlsText = sb.ToString();
        }

        public void LaunchGame()
        {
            if (!_allConfigs.TryGetValue(SelectedLaunchKey, out var config)) return;

            string rootDir = ConfigManager.GetRootDir();
            string jnlpPath = Path.Combine(rootDir, "Minecraft.jnlp");

            string cleanPath = rootDir.Replace("\\", "/");
            if (!cleanPath.StartsWith("/"))
            {
                cleanPath = "/" + cleanPath;
            }
            string codebase = $"file://{cleanPath}/";

            string sessionId = new Random().Next(10000000, 99999999).ToString();
            string jarPath = config.Jar;

            string vmArgs = "-Xmx800M -XX:MaxDirectMemorySize=1024M -Djava.util.Arrays.useLegacyMergeSort=true -Dsun.java2d.uiScale.enabled=false -Dsun.java2d.dpiaware=false -Dorg.lwjgl.util.NoChecks=true";
            string fixArgs = "-Dhttp.proxyHost=betacraft.uk -Dhttp.proxyPort=11702 -Dhttp.nonProxyHosts=api.betacraft.uk|files.betacraft.uk -Dsun.java2d.noddraw=true -Dsun.awt.noerasebackground=true -Dsun.java2d.d3d=false -Dsun.java2d.opengl=false -Dsun.java2d.pmoffscreen=false -Djava.net.useSystemProxies=false";

            string finalServerIp = string.Empty;
            string finalServerPort = string.Empty;
            if (IsMultiplayerEnabled)
            {
                finalServerIp = ServerIp;
                finalServerPort = ServerPort;
            }
            else
            {
                finalServerIp = string.Empty;
                finalServerPort = string.Empty;
                Debug.WriteLine("[Security Check] 当前非多人版本，已自动截断拦截 IP/Port 联机参数。");
            }
            string multiParams = "";
            if (config.Type == "classic_mp" && !string.IsNullOrWhiteSpace(finalServerIp))
            {
                multiParams = $"    <param name=\"server\" value=\"{finalServerIp}\" />\n" +
                              $"    <param name=\"port\" value=\"{finalServerPort}\" />";
            }

            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            xml.AppendLine($"<jnlp spec=\"1.0+\" codebase=\"{codebase}\" href=\"Minecraft.jnlp\">");
            xml.AppendLine("  <information>");
            xml.AppendLine($"    <title>{config.Title}</title>");
            xml.AppendLine("    <vendor>Hawk</vendor>");
            xml.AppendLine("    <description>Minecraft Applet Offline Launcher</description>");
            xml.AppendLine("    <icon href=\"favicon.jpg\"/>");
            xml.AppendLine("  </information>");
            xml.AppendLine("  <security>");
            xml.AppendLine("    <all-permissions/>");
            xml.AppendLine("  </security>");
            xml.AppendLine("  <resources>");
            xml.AppendLine($"    <j2se version=\"1.8*\" java-vm-args=\"{vmArgs} {fixArgs}\"/>");
            xml.AppendLine("    <jar href=\"LWJGL/lwjgl_util_applet.jar\" />");
            xml.AppendLine("  </resources>");
            xml.AppendLine($"  <applet-desc name=\"{config.Title}\" main-class=\"{config.MainClass}\" width=\"{config.Width}\" height=\"{config.Height}\">");
            xml.AppendLine($"    <param name=\"al_title\" value=\"{config.Title}\"/>");
            xml.AppendLine($"    <param name=\"al_main\" value=\"{config.MainClass}\"/>");
            xml.AppendLine("    <param name=\"al_logo\" value=\"bg/logo_small.png\"/>");
            xml.AppendLine("    <param name=\"al_progressbar\" value=\"bg/loader.gif\"/>");
            xml.AppendLine("    <param name=\"al_cache\" value=\"false\"/>");
            xml.AppendLine($"    <param name=\"al_jars\" value=\"LWJGL/lwjgl.jar, LWJGL/jinput.jar, LWJGL/lwjgl_util.jar, {jarPath}\"/>");
            xml.AppendLine("    <param name=\"al_windows\" value=\"LWJGL/windows_natives.jar\"/>");
            xml.AppendLine("    <param name=\"al_linux\" value=\"LWJGL/linux_natives.jar\"/>");
            xml.AppendLine("    <param name=\"al_mac\" value=\"LWJGL/macosx_natives.jar\"/>");
            xml.AppendLine("    <param name=\"al_solaris\" value=\"LWJGL/solaris_natives.jar\"/>");
            xml.AppendLine("    <param name=\"al_debug\" value=\"false\"/>");
            xml.AppendLine("    <param name=\"al_version\" value=\"2.01\"/>");
            xml.AppendLine("    <param name=\"separate_jvm\" value=\"false\"/>");
            xml.AppendLine("    <param name=\"boxmessage\" value=\"Minecraft started\"/>");
            xml.AppendLine("    <param name=\"boxbgcolor\" value=\"#000000\"/>");
            xml.AppendLine("    <param name=\"image\" value=\"favicon.jpg\"/>");
            xml.AppendLine("    <param name=\"al_bgcolor\" value=\"000000\"/>");
            xml.AppendLine("    <param name=\"al_fgcolor\" value=\"ffffff\"/>");
            xml.AppendLine("    <param name=\"al_errorcolor\" value=\"ff0000\"/>");
            xml.AppendLine("    <param name=\"al_prepend_host\" value=\"false\"/>");
            xml.AppendLine($"    <param name=\"username\" value=\"{Username}\"/>");
            xml.AppendLine($"    <param name=\"sessionid\" value=\"{sessionId}\"/>");
            if (!string.IsNullOrEmpty(multiParams))
            {
                xml.AppendLine(multiParams);
            }
            xml.AppendLine("  </applet-desc>");
            xml.AppendLine("</jnlp>");

            File.WriteAllText(jnlpPath, xml.ToString(), Encoding.UTF8);

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "javaws",
                    WorkingDirectory = rootDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                // 使用 ArgumentList 完美解决跨平台路径空格与双引号逃逸问题
                startInfo.ArgumentList.Add(jnlpPath); 
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"javaws 调用失败: {ex.Message}");
            }
        }

        #endregion

        #region == 版本管理业务 (version.html -> ViewModel 完美对应) ==

        private string _vKey = "";
        public string VKey
        {
            get => _vKey;
            set { if (SetProperty(ref _vKey, value)) AutoSuggestPaths(); OnPropertyChanged(nameof(IsExistingVersion));}
        }

        private string _vName = "";
        public string VName
        {
            get => _vName;
            set { _vName = value; OnPropertyChanged(); }
        }

        private string _vType = "classic";
        public string VType
        {
            get => _vType;
            set { if (SetProperty(ref _vType, value)) { OnTypePresetChange(); AutoSuggestPaths(); } }
        }

        private string _vTitle = "";
        public string VTitle
        {
            get => _vTitle;
            set { _vTitle = value; OnPropertyChanged(); }
        }

        private string _vMainClass = "com.mojang.minecraft.MinecraftApplet";
        public string VMainClass
        {
            get => _vMainClass;
            set { _vMainClass = value; OnPropertyChanged(); }
        }

        private string _vJar = "";
        public string VJar
        {
            get => _vJar;
            set { _vJar = value; OnPropertyChanged(); }
        }

        private int _vWidth = 854;
        public int VWidth
        {
            get => _vWidth;
            set { _vWidth = value; OnPropertyChanged(); }
        }

        private int _vHeight = 480;
        public int VHeight
        {
            get => _vHeight;
            set { _vHeight = value; OnPropertyChanged(); }
        }

        private bool _has15aPatch;
        public bool Has15aPatch
        {
            get => _has15aPatch;
            set { _has15aPatch = value; OnPropertyChanged(); }
        }

        private bool _dpiFix;
        public bool DpiFix
        {
            get => _dpiFix;
            set { _dpiFix = value; OnPropertyChanged(); }
        }

        private bool _hasPaid;
        public bool HasPaid
        {
            get => _hasPaid;
            set { _hasPaid = value; OnPropertyChanged(); }
        }

        private bool _hasHuman;
        public bool HasHuman
        {
            get => _hasHuman;
            set { _hasHuman = value; OnPropertyChanged(); }
        }

        private bool _hasBlockMenu;
        public bool HasBlockMenu
        {
            get => _hasBlockMenu;
            set { _hasBlockMenu = value; OnPropertyChanged(); }
        }

        private bool _hasSetSpawn;
        public bool HasSetSpawn
        {
            get => _hasSetSpawn;
            set { _hasSetSpawn = value; OnPropertyChanged(); }
        }

        private bool _hasInventory;
        public bool HasInventory
        {
            get => _hasInventory;
            set { _hasInventory = value; OnPropertyChanged(); }
        }

        private string _inventoryKey = "I";
        public string InventoryKey
        {
            get => _inventoryKey;
            set { _inventoryKey = value; OnPropertyChanged(); }
        }

        private bool _hasDrop;
        public bool HasDrop
        {
            get => _hasDrop;
            set { _hasDrop = value; OnPropertyChanged(); }
        }

        private bool _hasSneak;
        public bool HasSneak
        {
            get => _hasSneak;
            set { _hasSneak = value; OnPropertyChanged(); }
        }

        private int _selectedTabIndex = 0;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                // ⭐ 核心拦截逻辑：当用户企图切换到【游戏启动器】(0) 且配置库为空时
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    // 2. 拦截检查：如果是切到【游戏启动器】(0) 且配置库依然为空
                    if (value == 0 && (_allConfigs == null || _allConfigs.Count == 0))
                    {
                        // 3. 关键：推迟到 UI 渲染的下一个周期，避免与当前正处于 Click 途中的 UI 状态冲突
                        Dispatcher.UIThread.Post(async () =>
                        {
                            // 强行改写为 1！因为之前同步成了 0，此时会实打实触发 0 -> 1 的更新通知，UI 就会乖乖被拽回来
                            SelectedTabIndex = 1;
                            // 紧接着弹出精美居中的警告
                            await ShowOwnerCenteredBoxAsync(
                                "无法切换", 
                                "由于当前未配置任何游戏版本，无法进入【游戏启动器】！\n请先在当前页面填写并保存至少一个版本配置。", 
                                ButtonEnum.Ok, 
                                Icon.Warning);
                        });
                    }
                }
                // 正常情况：允许切换标签页
                SetProperty(ref _selectedTabIndex, value);
            }
        }

        private void OnTypePresetChange()
        {
            if (VType == "classic" || VType == "classic_mp")
                VMainClass = "com.mojang.minecraft.MinecraftApplet";
            else if (VType == "isom")
                VMainClass = "net.minecraft.isom.IsomPreviewApplet";
            else
                VMainClass = "net.minecraft.client.MinecraftApplet";
        }

        private void AutoSuggestPaths()
        {
            string key = VKey.Trim();
            string stageFolder = VType == "classic_mp" || VType == "classic" ? "classic" : VType;

            VJar = string.IsNullOrEmpty(key) ? "" : $"bin/{stageFolder}/{key}.jar";
            VName = key;
            string safeKey = System.Text.RegularExpressions.Regex.Replace(key, "[^a-zA-Z0-9_]", "_");
            VTitle = string.IsNullOrEmpty(key) ? "" : $"Minecraft_{safeKey}";
        }

        public void SaveOrUpdateVersion()
        {
            if (string.IsNullOrWhiteSpace(VKey)) return;

            var config = new VersionConfig
            {
                Name = VName,
                Type = VType,
                Title = VTitle,
                MainClass = VMainClass,
                Jar = VJar,
                Width = VWidth,
                Height = VHeight,
                Has15aPatch = Has15aPatch ? true : false,
                DpiFix = DpiFix ? true : false,
                HasPaid = HasPaid ? true : false,
                HasHuman = HasHuman ? true : false,
                HasBlockMenu = HasBlockMenu ? true : false,
                HasSetSpawn = HasSetSpawn ? true : false,
                InventoryKey = HasInventory ? InventoryKey : string.Empty,
                HasDrop = HasDrop ? true : false,
                HasSneak = HasSneak ? true : false
            };

            _allConfigs[VKey] = config;
            ConfigManager.SaveConfig(_allConfigs);
            OnPropertyChanged(nameof(IsExistingVersion));
            
            // 写入成功后重新加载（这会自动刷新 UI ListBox 和 右下角实时 JSON 预览）
            LoadAllVersions();
        }

        public bool IsExistingVersion 
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VKey) || _allConfigs == null)
                    return false;
                    
                // 只有当前输入的 VKey 已经存在于配置文件中，才允许删除
                return _allConfigs.ContainsKey(VKey);
            }
        }

        public void LoadToForm(string key)
        {
            if (!_allConfigs.TryGetValue(key, out var data)) return;

            VKey = key;
            VName = data.Name;
            VType = data.Type;
            VTitle = data.Title;
            VMainClass = data.MainClass;
            VJar = data.Jar;
            VWidth = data.Width;
            VHeight = data.Height;

            Has15aPatch = data.Has15aPatch == true;
            DpiFix = data.DpiFix == true;
            HasPaid = data.HasPaid == true;
            HasHuman = data.HasHuman == true;
            HasBlockMenu = data.HasBlockMenu == true;
            HasSetSpawn = data.HasSetSpawn == true;
            HasInventory = !string.IsNullOrEmpty(data.InventoryKey);
            InventoryKey = data.InventoryKey ?? "I";
            HasDrop = data.HasDrop == true;
            HasSneak = data.HasSneak == true;
        }
        public void ResetFields()
        {
            // 1. 清空文本框
            VKey = string.Empty;
            VName = string.Empty;
            VType = "classic"; // 默认选择 classic 阶段
            VTitle = string.Empty;
            VMainClass = "com.mojang.minecraft.MinecraftApplet"; // 恢复主类默认值
            VJar = string.Empty;
            VWidth = 854;  // 恢复默认分辨率
            VHeight = 480;
            // 2. 将所有勾选框全部恢复为 false
            Has15aPatch = false;
            DpiFix = false;
            HasPaid = false;
            HasHuman = false;
            HasBlockMenu = false;
            HasSetSpawn = false;
            HasDrop = false;
            HasSneak = false;
            
            // 3. 额外项置空
            InventoryKey = string.Empty;
            // 4. ⭐ 极其重要：重置后更新“删除”按钮的状态（使其变为不可用）
            OnPropertyChanged(nameof(IsExistingVersion)); 
            // 如果是 Code-behind 则执行：BtnDelete.IsEnabled = false;
        }
        private async Task<ButtonResult> ShowOwnerCenteredBoxAsync(string title, string message, ButtonEnum button, Icon icon)
        {
            // ⭐ 关键：显式通过 Params 对象创建，强制指定启动位置为 CenterOwner
            var box = MessageBoxManager.GetMessageBoxStandard(new MsBox.Avalonia.Dto.MessageBoxStandardParams
            {
                ContentTitle = title,
                ContentMessage = message,
                ButtonDefinitions = button,
                Icon = icon,
                WindowStartupLocation = WindowStartupLocation.CenterOwner // 👈 强制居中于父窗口
            });
            // 获取当前活跃的 MainWindow
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                return await box.ShowWindowDialogAsync(desktop.MainWindow); // 以父窗口为宿主弹出
            }
            
            // 降级兼容：防万一没拿到主窗口
            return await box.ShowAsync();
        }
        public async Task CheckVersionsAndRedirectAsync()
        {
            // 1. 判断当前版本配置字典是否为空
            if (_allConfigs == null || _allConfigs.Count == 0)
            {
                // 2. 居中弹出友好的引导提示
                await ShowOwnerCenteredBoxAsync(
                    "未检测到版本配置", 
                    "当前未检测到任何 Minecraft 游戏版本！\n点击确定后，系统将自动引导您前往【可视化配置管理】进行添加。", 
                    ButtonEnum.Ok, 
                    Icon.Warning);
                // 3. 用户点击确定后，自动切换到第二个标签页（可视化配置管理）
                SelectedTabIndex = 1; 
            }
        }
        // ==========================================
        // 【带空验证】写入 / 保存版本 (Command 绑定)
        // ==========================================
        public async Task SaveVersionAsync()
        {
            // ⭐ 1. 严格非空校验（去除前后空格）
            if (string.IsNullOrWhiteSpace(VKey))
            {
                await ShowOwnerCenteredBoxAsync(
                    "保存失败", 
                    "当前编辑的版本 Key (VKey) 不能为空，请输入后再试！", 
                    ButtonEnum.Ok, 
                    Icon.Warning); // 👈 提示框完美居中
                return; // 拦截执行，阻止物理写入
            }
            // 2. 执行保存主逻辑
            SaveOrUpdateVersion();
            
            // 3. 提示保存成功
            await ShowOwnerCenteredBoxAsync(
                "提示", 
                "配置已成功写入 / 保存！", 
                ButtonEnum.Ok, 
                Icon.Info); // 👈 提示框完美居中
        }
        // ==========================================
        // 删除版本 (Command 绑定)
        // ==========================================
        public async Task DeleteVersionAsync()
        {
            // 1. 防御性检查
            if (string.IsNullOrWhiteSpace(VKey) || !_allConfigs.ContainsKey(VKey))
            {
                await ShowOwnerCenteredBoxAsync(
                    "提示", 
                    "当前编辑的版本不存在于配置中，无法删除！", 
                    ButtonEnum.Ok, 
                    Icon.Warning);
                return;
            }
            // 2. 安全提示：避免误删
            var result = await ShowOwnerCenteredBoxAsync(
                "确认删除", 
                $"确定要删除版本 [{VKey}] 吗？此操作无法撤销！", 
                ButtonEnum.YesNo, 
                Icon.Question);
            
            // 异步等待用户点击结果
            if (result == ButtonResult.Yes)
            {
                // 从字典中移除并保存
                _allConfigs.Remove(VKey);
                ConfigManager.SaveConfig(_allConfigs);
                
                // 刷新列表和预览
                LoadAllVersions();
                
                // 重置表单
                ResetFields();
                
                // 提示删除成功
                await ShowOwnerCenteredBoxAsync(
                    "提示", 
                    "删除成功！", 
                    ButtonEnum.Ok, 
                    Icon.Info);
            }
        }

        private readonly string _settingsPath = Path.Combine(ConfigManager.ConfigDirectory, "settings.json");

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var settings = JsonSerializer.Deserialize<UserSettings>(json);
                    if (settings != null)
                    {
                        SelectedLaunchKey = settings.LastSelectedVersion;
                        Username = settings.LastUsername;
                        ServerIp = settings.LastServerIp;
                        ServerPort = settings.LastServerPort;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] 加载设置失败: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                var settings = new UserSettings
                {
                    LastSelectedVersion = SelectedLaunchKey ?? "",
                    LastUsername = Username ?? "Guest",
                    LastServerIp = ServerIp ?? "",
                    LastServerPort = ServerPort ?? ""
                };
                var directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
                
                Debug.WriteLine("[Settings] 保存设置成功！");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Settings] 保存设置异常: {ex.Message}");
            }
        }

        #endregion

        private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}