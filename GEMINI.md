# GameNetty 项目上下文 (Project Context)

GameNetty 是一个基于 ET8.1 框架进行深度解耦的高性能游戏服务器/客户端框架。它旨在保留 ET 框架（C# .NET Core 分布式服务器、高开发效率）优势的同时，通过彻底分离前后端工程，解决源码权限管理和项目耦合问题，使其更适合中大型团队的商业化开发。

## 项目概览

- **核心框架**：基于 ET8.1。
- **服务器平台**：.NET 8.0。
- **客户端平台**：Unity (推荐 2019.4.12+)。
- **配表工具**：使用 [Luban](https://github.com/focus-creative-games/luban) 进行 Excel 到 C#/Bytes 的导出。
- **核心特性**：
  - **前后端分离架构**：通过项目引用与链接方式分离客户端与服务端代码库。
  - **热更新支持**：服务端通过动态 DLL 加载（Model.dll / Hotfix.dll）实现热更。
  - **极致精简**：客户端插件化，无侵入式集成，核心库极小（约 750k）。

## 目录结构说明

- `Server/`：服务端解决方案（`Server.sln`）及其项目（`App`, `Core`, `Hotfix`, `Loader`, `Model`, `ThirdParty`）。
- `Unity/`：Unity 客户端工程。
  - `Assets/ET/`：ET 框架客户端运行时与编辑器脚本。
  - `Assets/Scripts/`：共享源码目录，通常被服务端项目通过 `<Link>` 方式引用。
- `Share/`：共享开发工具与分析器（`Share.sln`）。
  - `Analyzer/`：Roslyn 分析器，用于约束 ET 编码规范。
  - `Share.SourceGenerator/`：ET 的源代码生成器。
- `Tools/`：外部工具，主要是 Luban 配表相关。
  - `Luban/`：配置导出脚本（`GenConfig_Server.sh` / `.bat`）。
- `Config/`：配置文件定义（Excel, Proto 等）及导出的二进制数据。
- `Bin/`：服务端编译产物及热更 DLL 的输出目录。

## 构建与运行流程

### 1. 服务端构建
1. **编译 Share 工具**：打开并编译 `Share/Share.sln`。这是必须的第一步，因为服务端项目依赖于此处生成的分析器和源码生成器。
2. **编译 Server**：打开并编译 `Server/Server.sln`。
3. **运行**：通过 IDE 或运行 `Bin/App.exe` 启动服务器。

### 2. 配置导出 (Luban)
当修改了 Excel 或 Proto 定义后：
- 运行 `Tools/Luban/GenConfig_Server.sh` 导出服务端配置。
- 运行 `Tools/Luban/GenConfig_Client.sh` 导出客户端配置。

## 开发规范与约定

- **代码共享**：服务端项目（`Model`, `Hotfix`）利用 `.csproj` 中的 `<Link>` 标签引用 `Unity/Assets/Scripts/` 下的代码。这种方式在保持物理路径分离的同时实现了逻辑共享。
- **ET 编码规范**：
  - 遵循组件化（Component-based）与 Actor 模型。
  - 必须通过 `Share/` 提供的分析器校验，确保组件装饰（Attribute）、异步用法（ETTask）符合框架要求。
- **热更逻辑分区**：
  - **Hotfix 层**：存放可动态热更的业务逻辑。
  - **Model 层**：存放数据结构、核心组件定义（通常较少变动）。

## 关键文件清单

- `Server/App/Program.cs`：服务端入口。
- `Server/Loader/Init.cs`：服务器初始化流程。
- `Server/Loader/CodeLoader.cs`：负责 `Model.dll` 与 `Hotfix.dll` 的加载与热重载。
- `Tools/Luban/CONFIG_EXPORT_RULES.md`：Luban 配表导出规则说明。
