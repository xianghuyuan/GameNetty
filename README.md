# GameNetty (ET8.1 + TEngine + Luban)

[![UnityVersion](https://img.shields.io/badge/Unity%20Ver-2019.4.12++-blue.svg?style=flat-square)](https://github.com/ALEXTANGXIAO/GameNetty)
[![License](https://img.shields.io/github/license/ALEXTANGXIAO/GameNetty)](https://github.com/ALEXTANGXIAO/GameNetty)

GameNetty 是一个基于 **ET8.1** 框架进行深度解耦的高性能游戏开发解决方案。它旨在保留 ET 框架在 C# 分布式服务器和开发效率上的优势，同时通过彻底分离前后端工程，解决源码权限管理和项目耦合问题，使其更适合中大型团队的商业化开发。

---

## 🚀 核心架构与改动

- **深度解耦**: 
  - `Model/Hotfix`: 逻辑层 (纯 C# / ET Fiber)，处理协议与计算。
  - `ModelView/HotfixView`: 表现层 (Unity / TEngine)，处理渲染与 UI。
- **极致精简**: 客户端插件化，核心库极小（约 750k），几乎零成本、无侵入地嵌入你的项目。
- **UI 系统**: 全面集成 **TEngine**，支持自动绑定与极速开发。
- **配表工具**: 使用 **Luban** 导出 Excel 配置文件，完美兼容。
- **资源管理**: 集成 **YooAsset**，支持完善的按需加载与补丁更新。

---

## 📚 开发指南与技术文档

为了帮助开发者快速从传统 ET 切换到 GameNetty 开发模式，请按顺序阅读以下文档：

1. [**核心开发指南**](./docs/GameNetty_Development_Guide.md) - **必读：** 理解解耦架构、Entity 绑定与开发规范。
2. [**网络通信机制**](./docs/GameNetty_Network_Architecture.md) - 掌握基于 Fiber 的消息流转与解耦收发路径。
3. [**时差滚动系统实现**](./docs/Parallax_Scrolling_System.md) - **实战案例：** 在 ECS 架构下实现 2D 时差滚动逻辑。
4. [**高性能 AI 设计**](./docs/GameNetty_AI_System.md) - 针对 Roguelike/割草类游戏的 AI 优化思路。

---

## 🏗 快速开始

### 1. 环境准备
- **IDE**: JetBrains Rider (推荐) 或 VS2022。
- **Unity**: 2019.4.12+ (推荐 2021.3.x 及以上)。
- **.NET SDK**: .NET 8.0。

### 2. 运行服务器
1. 打开 `Share/Share.sln` 编译全部。
2. 打开 `Server/Server.sln` 编译并运行。（必须先执行第 1 步）。
3. 运行 `Tools/Luban/GenConfig_Server.sh` 导出配置。

---

## 🤝 特别鸣谢
- **[ET](https://github.com/egametang/ET)**: 提供核心底层分布式框架。
- **[TEngine](https://github.com/1689726/TEngine)**: 提供优秀的 UI 表现层与模块化工具。
- **[Luban](https://github.com/focus-creative-games/luban)**: 提供强大的配表导出解决方案。

[**如果觉得本仓库对您有帮助，不妨请我可爱的女儿买杯奶茶吧~**](Books/Donate.md)
