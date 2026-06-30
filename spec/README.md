# 功能模块规格目录

本目录用于存放功能模块规格说明，并按文档性质拆分为功能逻辑和美术需求两类。

## 目录职责

### `logic/`

按功能领域分层存放功能逻辑文档，关注玩法规则、系统设计、协议/配置、服务端底层逻辑、客户端底层逻辑、同步边界、HUD/界面状态逻辑和验收标准。每个功能逻辑 spec 必须分别说明服务端与客户端职责。

当前层级：

- `logic/core/`：核心玩法、全局规则、跨系统基础能力。
- `logic/battle/ai/`：战斗 AI、目标选择、自动战斗、高性能小怪逻辑。
- `logic/battle/view/`：战斗单位表现层、逻辑到表现的映射。
- `logic/battle/hud/`：战斗内 HUD 状态、界面交互逻辑和客户端 Widget 绑定。
- `logic/battle/vehicle/`：战斗中的载具、载具 Buff、载具成长和战斗效果逻辑。

### `art/`

存放美术需求文档，关注 UI/场景资源需求、风格基准、拆图清单、资源命名、目录归属和接入规范。

## 功能逻辑文档

### AI 与战斗单位

- [GameNetty_AI系统.md](./logic/battle/ai/GameNetty_AI系统.md)
- [割草游戏高性能 AI 逻辑设计文档.md](./logic/battle/ai/割草游戏高性能%20AI%20逻辑设计文档.md)
- [战斗单位表现层实现_TMP.md](./logic/battle/view/战斗单位表现层实现_TMP.md)

### 战斗主界面

- [战斗主界面策划案.md](./logic/battle/hud/战斗主界面策划案.md)

### 核心玩法

- [构筑系统总览.md](./logic/core/构筑系统总览.md)
- [玩家动线与局外养成路线.md](./logic/core/玩家动线与局外养成路线.md)

### 载具与 Buff

- [载具镶嵌Buff系统设计文档.md](./logic/battle/vehicle/载具镶嵌Buff系统设计文档.md)

## 美术需求文档

### 战斗主界面

- [战斗主界面资源需求.md](./art/战斗主界面资源需求.md)

### 横版舞台与视觉资源

- [横版战斗背景视差设计.md](./art/横版战斗背景视差设计.md)
- [横版舞台资源接入规范.md](./art/横版舞台资源接入规范.md)
- [月夜森林纸雕舞台拆图清单.md](./art/月夜森林纸雕舞台拆图清单.md)

## 新增文档规范

功能逻辑文档放入 `spec/logic/`，美术需求文档放入 `spec/art/`。

建议文件命名使用小写短横线：

```text
spec/
  logic/
    core/
      build-system.md
    battle/
      ai/
        minion-ai.md
      hud/
        battle-main-ui.md
      vehicle/
        vehicle-buff.md
  art/
    battle-main-ui-assets.md
    forest-stage-art-assets.md
```

建议每个功能逻辑 spec 包含以下内容：

```markdown
# 功能名

## 背景

## 目标

## 非目标

## 所属层级

## 业务规则

## 服务端底层逻辑

### 实体与组件

### 系统入口

### 数据与配置

### 权威校验

### 状态同步

### 生命周期与清理

### 性能约束

## 客户端底层逻辑

### 模块与组件

### 消息入口

### 本地状态

### 表现事件

### HUD/Prefab 绑定

### 本地预测与服务端校正

## 协议与配置

## 双端同步边界

## 与美术需求的关系

## 边界情况

## 验收标准
```

建议每个美术需求 spec 包含以下内容：

```markdown
# 资源需求名

## 目标

## 风格基准

## 资源清单

## 尺寸与格式

## 命名与目录

## 接入规则

## 验收标准
```
