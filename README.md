> [!IMPORTANT]
> **系统兼容性提示**：本软件目前**仅支持 Windows** 系统。
> Linux 移植**已暂缓、路线待定**：游戏侧的 LWJGL Linux Natives 与 `al_linux` 参数均已就绪，但 Linux 上没有随 JDK 分发的 Java Web Start（`javaws`）实现，直接依赖各发行版的 IcedTea-Web 并不可靠；我们在评估「自带 JNLP 宿主」等替代路线，确定前不再承诺 Linux 可用性。
>
> *Compatibility Notice: Currently this software only supports **Windows**. The Linux port is **on hold with its approach still under evaluation** — the LWJGL Linux natives and `al_linux` parameter are already in place, but there is no JDK-shipped Java Web Start (`javaws`) on Linux, so relying on each distro's IcedTea-Web is not dependable.*

# MCAJNLP - Minecraft Applet JNLP 离线启动器

`MCAJNLP` 是一个基于 .NET 与 **Avalonia UI** 跨平台框架开发的、致力于高度还原与离线启动 Minecraft 早期历史版本 JNLP (Java Network Launch Protocol) 与 Applet 运行体验的桌面启动器客户端。

本项目彻底解决了现代浏览器全面废弃 NPAPI 插件后无法运行 Java Applet 的痛点，让玩家无需安装复古浏览器，即可在本地直接、安全、原汁原味地唤起并重温早期网页版 Minecraft。

---

## 🌟 核心功能特性

- **现代化的跨平台 GUI 界面**：基于 .NET 强类型语言与 Avalonia 框架构建，不仅界面优雅美观，框架本身也具备跨平台能力（当前发行版仅提供 Windows 产物）。
- **多阶段历史版本自由管理**：支持 Classic、Indev、Infdev、Alpha、Beta、Release 正式版（直至 1.5.2）以及经典的 [Infinite Map Visualizer](https://minecraft.wiki/w/Infinite_Map_Visualizer) 等距地图预览器。
- **纯本地 JNLP 一键拉起**：启动器会在本地动态生成符合安全规范的 `.jnlp` 描述文件（写入程序根目录），并**自动探测**本地 Java Web Start (`javaws`)——按注册表、安装目录、`JAVA_HOME`、`PATH` 的优先级寻找候选，再用 `java -version` 逐个验证真实性——全自动配置环境并运行游戏，摆脱对 Pale Moon 或 IE 等老旧浏览器的依赖。
- **内置全套多平台 Native 运行库（LWJGL）**：
  - 仓库内预置了针对 Windows、Linux、macOS 以及 Solaris 平台的 LWJGL（轻量级 Java 游戏库）二进制 Natives 动态链接库及 JInput 等基础依赖。
- **图形化配置管理器 (`version.json` / `settings.json`)**：
  - 支持在启动器内自定义离线玩家名（自动生成随机 SessionID），自由调节内存大小以及本地存储路径，配置文件本地自动持久化。

---

## 📁 客户端 JAR 包放置指引 (Client JAR Placement)

⚠️ **特别说明（版权合规）**：受 DMCA 与版权合规限制，**本 GitHub 仓库不提供、不分发任何官方 Minecraft 游戏 `.jar` 客户端文件**（仓库仅包含 LWJGL 基础依赖与启动器源码）。

请自行准备或提取您的 Minecraft 历史版本 `.jar` 文件，并将其放置在 `bin/` 目录下对应的子文件夹中：

- **Classic JAR**：放置于 `bin/classic/`（例如 `bin/classic/c0.0.22a_05.jar`） 
- **Indev JAR**：放置于 `bin/indev/`（例如 `bin/indev/in-20100223.jar`） 
- **Infdev JAR**：放置于 `bin/infdev/`
- **Alpha JAR**：放置于 `bin/alpha/`
- **Beta JAR**：放置于 `bin/beta/`
- **Release JAR**：放置于 `bin/release/`
- **Infinite Map Visualizer JAR**：放置于 `bin/isom/`

> **提示**：放置的 `.jar` 文件名与路径，需要与您在启动器设置或 `version.json` 配置文件中填写的 `jar` 路径字段保持一致。

---

## 💻 运行环境要求

### 1. 支持的 Java 运行环境 (JRE)
- **推荐版本：Java 8**（**推荐使用 32位 或 64位 Java 8 JRE/JDK**，两者都能运行；启动器探测时会优先选用 64 位的 javaws）。
  * ⚠️ **特别注意**：由于 Oracle 在 Java 9 及之后版本中废弃并移除了 Java Web Start (`javaws`)，若要直接拉起 JNLP，您的电脑上**必须安装有 Java 8 或更早版本的 JRE**。
- **归档支持：Java 6 ~ Java 7**（对于部分极端怀旧、在 Java 8 下有渲染 Bug 的早期 Applet，可考虑使用较低版本）。

> 如需下载 Oracle 历史归档版 Java JRE，可复制以下链接至浏览器：
> - **Java 8 官方下载**：[https://www.java.com/zh-CN/download/](https://www.java.com/zh-CN/download/)
> - **Java 6 归档下载**：[https://www.oracle.com/java/technologies/javase-java-archive-javase6-downloads.html](https://www.oracle.com/java/technologies/javase-java-archive-javase6-downloads.html)
> - **Java 7 归档下载**：[https://www.oracle.com/java/technologies/javase/javase7-archive-downloads.html](https://www.oracle.com/java/technologies/javase/javase7-archive-downloads.html)

### 2. .NET 运行时 (C# 运行基础)
- 启动器基于 .NET 开发，运行需要本地安装有 **.NET Runtime**。如果你下载的是 Self-Contained（独立免打包）版本，则无需额外安装。

---

## 🚀 如何编译与运行？

### 1. 开发者本地构建 (Build from Source)
如果您希望本地调试或自行编译本项目，请确保您的电脑已安装 [.NET SDK](https://dotnet.microsoft.com/)，随后在控制台执行：

```bash
# 克隆仓库并切换到 dev 开发分支
git clone -b dev https://github.com/CreatorCSIE/MCAJNLP.git
cd MCAJNLP

# 运行启动器
dotnet run --project src/MCAJNLP.csproj
```

### 2. 普通玩家使用教程 (Run Precompiled Release)
1. 前往 [Releases 页面](https://github.com/CreatorCSIE/MCAJNLP/releases) 下载适合您系统架构的最新压缩包。
2. 解压压缩包到本地任意目录（路径最好不要含有中文或特殊字符）。
3. 按照 [【客户端 JAR 包放置指引】](#-客户端-jar-包放置指引-client-jar-placement) 将游戏 JAR 放入对应的 `bin/` 子目录。
4. 双击运行 `MCAJNLP.exe`（目前仅支持 Windows）。
5. 在 GUI 界面中选择您想体验的版本，输入离线游戏 ID，点击 **【启动 / Launch】** 即可。

---

## 🌐 Localization / 国际化与多语言支持

本项目（包含启动器页面与文档）随时欢迎社区提供多语言本地化（i18n）与翻译支持！  
*This repository, launcher UI, and documentation welcome community contributions for localization (i18n) and translations at any time!*

如果你希望为本项目贡献其他语言（如 English、繁體中文等）的 README 文档或界面翻译，欢迎随时提交 **Pull Request** 或开 **Issue** 讨论！

---

## ❓ 常见问题排查与解决

### 1. 遇到 `java.security.AccessControlException` 安全报错
由于 Java 默认的沙箱机制对本地文件读写与网络套接字（Socket）有严格限制，需要手动修改 Java 本地配置文件 `java.policy`：

- **文件路径**：
  - **64位 OS / 64位 Java**：`C:\Program Files\Java\<你的Java版本>\lib\security\java.policy`
  - **32位 Java**：`C:\Program Files (x86)\Java\<你的Java版本>\lib\security\java.policy`
  - **Linux**：`/usr/lib/jvm/<你的Java版本>/lib/security/java.policy`

- **修改方法**：使用管理员权限打开该文件，在大括号 `grant { ... };` 内的末尾追加以下两行权限声明：
  ```text
  permission java.net.SocketPermission "*:*", "accept,connect,resolve";
  permission java.security.AllPermission;
  ```

### 2. 提示找不到 `javaws`，或点击【启动游戏】后没有任何反应
启动器内置 **javaws 探针**，不依赖系统 `PATH` 的先后顺序：

- **探测顺序**：注册表（`HKLM\SOFTWARE\JavaSoft\Java Web Start / Java Runtime Environment / Java Development Kit`，先 64 位视图、再 32 位视图）→ 安装目录扫描（`Program Files` 优先于 `Program Files (x86)`，覆盖 `Java`、Adoptium、Corretto、Zulu、Semeru 等厂商目录）→ `JAVA_HOME` / `JDK_HOME` → `PATH`。
- **候选必须实测**：每个候选都会用同目录下的 `java.exe -version` 校验，要求能真实输出版本号且为 Java 8。因此**卸载残留的转发壳会被自动跳过**（典型症状是同目录 `java.exe` 直接以 `0xC0000005` 崩溃，表现为“点了启动没进程”）。
- **启动与回退**：按优先级逐个试运行选出的 javaws，若某个进程创建后立即以非 0 退出码结束则换下一个；全部不可用时兜底交给系统 `.jnlp` 文件关联；仍失败会弹窗列出**完整探测表与被拒原因**。
- **玩家可以自行处理**：安装 **64 位 Java 8**（Oracle JRE，或任何自带 `javaws` 的 JDK 8）；或将 `JAVA_HOME` 指向该 JRE/JDK 根目录；或直接双击程序根目录下的 `Minecraft.jnlp` 手动唤起。

### 3. 升级后游戏仍打不开（JNLP 与 LWJGL 缓存）
- 启动器生成的描述文件固定是**程序根目录**下的 `Minecraft.jnlp`（自 1.2 起不再写入 `config\` 子目录），其 `codebase` 同样指向该根目录；旧版本遗留在 `config\` 里的同名文件属于失效残留，可直接删除。
- 从 1.0 / 1.1 升级上来的玩家请先清理 LWJGL 缓存：`Win + R` → 输入 `%TEMP%` → 删除其中的 `lwjglcache` 文件夹后重试。
- 若提示 jar 缺失，请对照弹窗中列出的**实际查找位置**检查 `version.json` 里的 `jar` 字段与 `bin/` 下的文件名是否一致。

---

## 📄 许可证与版权声明 (License & Copyright)

- 本项目仅供 Minecraft 历史版本研究、Applet 怀旧与技术交流使用。
- 为了防止代码被恶意倒卖、闭源修改或注入恶意软件，本项目源码已全面升级，采用 **[GNU General Public License v3 (GPL v3) 许可证](LICENSE)** 进行强制开源。
  - **Copyright (c) 2021-2026 CreatorCSIE. All rights reserved.**
- **第三方资产与商标免责声明**：
  - 本仓库仅包含 LWJGL 基础库、启动器配置及 C# 客户端源码，**不包含、不分发任何官方 Minecraft 游戏 `.jar` 客户端包或音效资源**。
  - 本项目所涉及的 Minecraft 游戏资产、商标与品牌版权均归 **Mojang Studios / Microsoft** 所有。