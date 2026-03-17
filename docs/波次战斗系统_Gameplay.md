# 波次战斗系统 Gameplay 文档

## 1. 系统概述

波次战斗是游戏的核心战斗模式之一。玩家进入战斗后，服务器按波次生成怪物，玩家需要击败每一波怪物才能进入下一波，直到所有波次完成或战斗失败。

## 2. 战斗流程

```
客户端发起战斗请求 (C2M_StartBattle)
        │
        ▼
服务器创建战斗房间 (BattleRoom)
        │
        ▼
服务器初始化波次管理器 (WaveManagerComponent)
        │
        ▼
  ┌─────────────────────────────┐
  │  服务器通知客户端波次开始     │◄──────────┐
  │  (M2C_WaveStart)            │           │
  │  - waveNumber: 当前波次编号  │           │
  │  - totalWaves: 总波数        │           │
  │  - monsterCount: 怪物数量    │           │
  └──────────┬──────────────────┘           │
             │                              │
             ▼                              │
  服务器按配置生成怪物                        │
             │                              │
             ▼                              │
  玩家击败所有怪物                            │
             │                              │
             ▼                              │
  服务器通知客户端波次完成                     │
  (M2C_WaveComplete)                        │
             │                              │
             ▼                              │
       是否还有下一波？ ──── 是 ────────────┘
             │
             否
             │
             ▼
  服务器通知客户端战斗结束
  (M2C_BattleEnd)
```

## 3. 配置系统

### 3.1 WaveConfig（波次配置表）

配置文件：`Config/Excel/GameConfig/WaveConfig.xlsx`

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | int | 波次配置ID（主键） |
| WaveNumber | int | 波次编号 |
| MonsterConfigId | int | 怪物配置ID（引用MonsterUnitConfig） |
| MonsterCount | int | 本波怪物数量 |
| SpawnInterval | int | 波次间隔时间（毫秒） |
| Desc | string | 描述 |

### 3.2 当前配置数据

| 波次 | 怪物配置 | 数量 | 间隔 | 描述 |
|------|---------|------|------|------|
| 1 | 3001 | 3 | 5000ms | 第1波-哥布林 |
| 2 | 3002 | 4 | 5000ms | 第2波-狼人 |
| 3 | 3003 | 5 | 5000ms | 第3波-兽人战士 |
| 4 | 3004 | 6 | 4000ms | 第4波-暗影刺客 |
| 5 | 3005 | 7 | 4000ms | 第5波-炎魔领主 |

## 4. 消息协议

### 4.1 C2M_StartBattle（客户端→服务器）
- 玩家请求开始战斗

### 4.2 M2C_WaveStart（服务器→客户端）
- battleId: 战斗ID
- waveNumber: 当前波次编号
- totalWaves: 总波数
- monsterCount: 本波怪物数量

### 4.3 M2C_WaveComplete（服务器→客户端）
- battleId: 战斗ID
- waveNumber: 完成的波次编号
- totalWaves: 总波数
- duration: 本波耗时（秒）

### 4.4 M2C_BattleEnd（服务器→客户端）
- battleId: 战斗ID
- success: 是否胜利
- duration: 战斗总耗时（秒）

## 5. 客户端架构

### 5.1 核心类

| 类名 | 职责 |
|------|------|
| BattleComponent | 管理所有战斗实例 |
| Battle | 单场战斗实体，存储波次、状态等数据 |
| BattleHelper | 战斗辅助方法（发起战斗等） |
| BattleEventType | 定义战斗相关事件（WaveStart/WaveComplete/BattleEnd） |

### 5.2 消息处理器

| Handler | 处理消息 | 职责 |
|---------|---------|------|
| M2C_WaveStartHandler | M2C_WaveStart | 更新波次数据，发布WaveStart事件 |
| M2C_WaveCompleteHandler | M2C_WaveComplete | 发布WaveComplete事件 |
| M2C_BattleEndHandler | M2C_BattleEnd | 更新战斗状态，清理战斗实例 |

### 5.3 事件流

```
M2C_WaveStart 消息到达
    → M2C_WaveStartHandler 处理
        → 更新 Battle.CurrentWave / TotalWaves
        → 发布 WaveStart 事件
            → [待实现] UI 监听并显示波次信息
            → [待实现] 音效系统播放波次开始音效
            → [待实现] 特效系统播放波次开始特效
```

## 6. 服务端架构

### 6.1 核心类

| 类名 | 职责 |
|------|------|
| BattleRoom | 战斗房间实体 |
| WaveManagerComponent | 波次管理组件，挂载在BattleRoom上 |
| WaveManagerComponentSystem | 波次管理逻辑（从WaveConfig读取配置） |

### 6.2 关键方法

- `GetMonsterConfigIdForWave(wave)`: 从WaveConfig读取怪物配置ID
- `GetMonsterCountForWave(wave)`: 从WaveConfig读取怪物数量
- `StartNextWave()`: 开始下一波，发送M2C_WaveStart消息
- `OnWaveComplete()`: 波次完成，发送M2C_WaveComplete消息

## 7. 待实现功能（表现层）

### 7.1 UI 表现
- [ ] 波次信息HUD：显示"第 X 波 / 共 Y 波"
- [ ] 波次开始提示：屏幕中央显示"第X波来袭！"动画
- [ ] 怪物数量显示：显示剩余怪物数量
- [ ] 波次进度条：显示当前波次进度

### 7.2 音效
- [ ] 波次开始音效
- [ ] 波次完成音效
- [ ] 战斗胜利/失败音效
- [ ] Boss波次特殊音效

### 7.3 特效/动画
- [ ] 波次开始过场动画
- [ ] 波次倒计时（准备时间）
- [ ] 怪物生成特效
- [ ] 波次完成庆祝特效
- [ ] 战斗结算界面

### 7.4 其他
- [ ] 怪物预告：显示下一波即将出现的怪物类型
- [ ] 难度提示：标记精英波次和Boss波次
- [ ] 奖励预览：显示通关奖励
