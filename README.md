# MCAJNLP - Minecraft Applet JNLP 离线启动器

`MCAJNLP` 是一个基于 .NET 与 **Avalonia UI** 跨平台框架开发的、致力于高度还原与离线启动 Minecraft 早期历史版本 JNLP (Java Network Launch Protocol) 与 Applet 运行体验的桌面启动器客户端。

本项目彻底解决了现代浏览器全面废弃 NPAPI 插件后无法运行 Java Applet 的痛点，让玩家无需安装复古浏览器，即可在本地直接、安全、原汁原味地唤起并重温早期网页版 Minecraft。

---

## 🌟 核心功能特性

- **现代化的跨平台 GUI 界面**：基于 .NET 强类型语言与 Avalonia 框架构建，不仅界面优雅美观，还天生支持在 Windows、Linux 上无缝运行。
- **多阶段历史版本自由管理**：支持 Classic、Indev、Infdev、Alpha、Beta、Release 正式版（直至 1.5.2）以及经典的 [Infinite Map Visualizer](https://minecraft.wiki/w/Infinite_Map_Visualizer) 等距地图预览器。
- **纯本地 JNLP 一键拉起**：启动器会在本地动态生成符合安全规范的 `.jnlp` 描述文件，全自动配置环境并调用本地 Java Web Start (`javaws`) 运行游戏，摆脱对 Pale Moon 或 IE 等老旧浏览器的依赖。
- **内置全套多平台 Native 运行库（LWJGL）**：
  - 仓库内预置了针对 Windows、Linux、macOS 以及 Solaris 平台的 LWJGL（轻量级 Java 游戏库）二进制 Natives 动态链接库及 JInput 等基础依赖。
- **图形化配置管理器 (`version.json` / `settings.json`)**：
  - 支持在启动器内自定义离线玩家名（自动生成随机 SessionID），自由调节内存大小以及本地存储路径，配置文件本地自动持久化。

---

## 📁 客户端 JAR 包放置指引 (Client JAR Placement)

⚠️ **特别说明（版权合规）**：受 DMCA 与版权合规限制，**本 GitHub 仓库不提供、不分发任何官方 Minecraft 游戏 `.jar` 客户端文件**（仓库仅包含 LWJGL 基础依赖与启动器网页源码）。

请自行准备或提取您的 Minecraft 历史版本 `.jar` 文件，并将其放置在 `bin/` 目录下对应的子文件夹中：

- **Classic JAR**：放置于 `bin/classic/`（例如 `bin/classic/c0.0.22a_05.jar`） [2]
- **Indev JAR**：放置于 `bin/indev/`（例如 `bin/indev/in-20100223.jar`） [2]
- **Infdev JAR**：放置于 `bin/infdev/`
- **Alpha JAR**：放置于 `bin/alpha/`
- **Beta JAR**：放置于 `bin/beta/`
- **Release JAR**：放置于 `bin/release/`
- **Infinite Map Visualizer JAR**：放置于 `bin/isom/`

> **提示**：放置的 `.jar` 文件名与路径，需要与您在启动器设置或 `version.json` 配置文件中填写的 `jar` 路径字段保持一致。

---

## 💻 运行环境要求

### 1. 支持的 Java 运行环境 (JRE)
- **推荐版本：Java 8**（**推荐使用 32位 或 64位 Java 8 JRE/JDK**）。
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

### 2. 普通玩家使用 (Run Precompiled Release)
1. 前往 [Releases 页面](https://github.com/CreatorCSIE/MCAJNLP/releases) 下载适合您系统架构的最新压缩包。
2. 解压压缩包到本地任意目录（路径最好不要含有中文或特殊字符）。
3. 按照 [【客户端 JAR 包放置指引】](#-客户端-jar-包放置指引-client-jar-placement) 将游戏 JAR 放入对应的 `bin/` 子目录 [2]。
4. 双击运行 `MCAJNLP.exe`（Windows）或执行 `./MCAJNLP`（Linux）。
5. 在 GUI 界面中选择您想体验的版本，输入离线游戏 ID，点击 **【启动 / Launch】** 即可。

---

## ❓ 常见问题排查与解决

### 1. 遇到 `java.security.AccessControlException` 安全报错
由于 Java 默认的沙箱机制对本地文件读写与网络套接字（Socket）有严格限制，需要手动修改 Java 本地配置文件 `java.policy`：

- **文件路径**：
  - **64位 OS / 64位 Java**：`C:\Program Files\Java\<你的Java版本>\lib\security\java.policy` [2]
  - **32位 Java**：`C:\Program Files (x86)\Java\<你的Java版本>\lib\security\java.policy` [2]
  - **Linux**：`/usr/lib/jvm/<你的Java版本>/lib/security/java.policy` [2]

- **修改方法**：使用管理员权限打开该文件，在大括号 `grant { ... };` 内的末尾追加以下两行权限声明：
  ```text
  permission java.net.SocketPermission "*:*", "accept,connect,resolve";
  permission java.security.AllPermission;
  ```

### 2. 提示找不到 `javaws` 或 JNLP 无法关联打开
- 请确认您的默认 Java 环境是 **Java 8 (JRE 1.8)** 或更早版本，并在安装时勾选了“关联 `.jnlp` 文件”选项。
- 如果系统中存在多个 Java 版本导致关联混乱，可以在启动器设置中手动指定 `javaws.exe`（通常位于 `C:\Program Files\Java\jre1.8.x_xxx\bin\javaws.exe`）的绝对路径。

---

## 📄 许可证与版权声明 (License & Copyright)

- 本项目仅供 Minecraft 历史版本研究、Applet 怀旧与技术交流使用。
- 为了防止代码被恶意倒卖、闭源修改或注入恶意软件，本项目源码已全面升级，采用 **[GNU General Public License v3 (GPL v3) 许可证](LICENSE)** 进行强制开源。
  - **Copyright (c) 2021-2026 CreatorCSIE. All rights reserved.**
- **第三方资产与商标免责声明**：
  - 本仓库仅包含 LWJGL 基础库、启动器配置及 C# 客户端源码，**不包含、不分发任何官方 Minecraft 游戏 `.jar` 客户端包或音效资源**。
  - 本项目所涉及的 Minecraft 游戏资产、商标与品牌版权均归 **Mojang Studios / Microsoft** 所有。
```