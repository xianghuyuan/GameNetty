# GameNetty 项目文档目录

本目录保存项目长期技术文档、框架说明、战斗流程说明和美术生产流程。具体功能模块规格统一放在仓库根目录的 [`spec/`](../spec/README.md)，其中功能逻辑文档放在 `spec/logic/`，美术需求文档放在 `spec/art/`。

## 文档分类

### 框架设计

框架设计文档包含底层架构、核心系统和通用组件的设计与实现。

- [框架设计索引](./框架设计/README.md)
- [BattleRoom架构说明-Unit与BattleUnit.md](./框架设计/BattleRoom架构说明-Unit与BattleUnit.md)
- [BattleHelper使用指南.md](./框架设计/BattleHelper使用指南.md)
- [WaveManagerComponent波次管理组件.md](./框架设计/WaveManagerComponent波次管理组件.md)
- [移动组件详细实现文档.md](./框架设计/移动组件详细实现文档.md)
- [GameNetty网络架构.md](./框架设计/GameNetty网络架构.md)
- [配置表体系说明.md](./框架设计/配置表体系说明.md)

### 战斗流程与玩法基线

这些文档描述当前战斗系统生命周期、核心玩法和流程图，作为功能规格和实现时的基础参考。

- [gameplay.md](./gameplay.md)
- [battle_flow.md](./battle_flow.md)
- [battle_flow_mermaid.md](./battle_flow_mermaid.md)
- [战斗流程图.md](./战斗流程图.md)

### 美术与 UI 生产流程

美术规格、UI 资源工作流和任务板放在 `docs/art/`。

- [art 索引](./art/README.md)
- [UI_BattleMain_ArtSpec.md](./art/ui/UI_BattleMain_ArtSpec.md)
- [BattleMain_UI_Workflow.md](./art/ui/BattleMain_UI_Workflow.md)
- [BattleMain_UI_TaskCenter.md](./art/ui/BattleMain_UI_TaskCenter.md)

### 功能模块规格

功能模块文档不再放在 `docs/功能设计/`，统一迁移到 [`spec/`](../spec/README.md)。

- [spec/README.md](../spec/README.md)
- [GameNetty_AI系统.md](../spec/logic/battle/ai/GameNetty_AI系统.md)
- [割草游戏高性能 AI 逻辑设计文档.md](../spec/logic/battle/ai/割草游戏高性能%20AI%20逻辑设计文档.md)
- [构筑系统总览.md](../spec/logic/core/构筑系统总览.md)
- [战斗主界面策划案.md](../spec/logic/battle/hud/战斗主界面策划案.md)
- [载具镶嵌Buff系统设计文档.md](../spec/logic/battle/vehicle/载具镶嵌Buff系统设计文档.md)
- [战斗主界面资源需求.md](../spec/art/战斗主界面资源需求.md)

## 目录职责

### `docs/`

保存长期文档和跨功能的技术说明：

- 框架设计与通用组件
- 战斗流程和玩法基线
- 美术、UI、资源生产流程
- 配置、网络、ET 框架说明

### `spec/`

保存具体功能模块的规格说明：

- `spec/logic/`：按功能领域分层，保存功能目标、业务规则、协议与配置、服务端设计、客户端设计、HUD/界面状态逻辑和验收标准
- `spec/art/`：美术风格、资源清单、拆图、尺寸格式、命名目录、接入规则和验收标准

## 文档编写规范

- 功能逻辑规格写入 `spec/logic/`。
- 美术需求规格写入 `spec/art/`。
- 框架、流程、通用指南写入 `docs/`。
- 美术和 UI 生产流程写入 `docs/art/`。
- 文件名语义清晰；项目术语如 ET、BattleRoom、AI 保留英文。
- 文档内引用其他文件时使用相对路径。
