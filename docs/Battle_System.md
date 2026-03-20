# 战斗系统功能文档

## 功能概述

本文档描述从战斗开始到战斗结束的完整流程，包括战斗房间初始化、波次管理、单位战斗（攻击/死亡）、波次完成和战斗结束等核心功能。

## 核心流程图

```
战斗开始 (InitBattle)
    │
    ▼
┌─────────────────┐
│  创建战斗房间    │
│  BattleRoom    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ 创建玩家战斗单位 │
│  Hero Unit     │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  创建波次管理器  │
│ WaveManager    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  开始第一波      │
│ StartFirstWave │
└────────┬────────┘
         │
         ▼
    ┌────┴────┐
    │  战斗中  │
    │ Fighting│
    └────┬────┘
         │
    ┌────┼────┐
    ▼    ▼    ▼
 攻击  死亡  波次完成
    │    │    │
    └────┼────┘
         ▼
    ┌────────┐
    │战斗结束 │
    └────────┘
```

## 战斗房间 (BattleRoom)

### 初始化战斗

```csharp
// 单人战斗初始化
BattleRoom.InitBattle(Unit playerUnit, int stageId, int battleType)

// 组队战斗初始化
BattleRoom.InitTeamBattle(Scene mapScene, List<long> memberIds, int battleType)
```

**功能说明：**
- 创建玩家战斗单位（Hero Unit）
- 根据 stageId 获取关卡配置
- 创建 WaveManagerComponent 管理波次

### 状态管理

| 状态 | 说明 |
|------|------|
| Prepare | 准备阶段 |
| Fighting | 战斗中 |
| End | 战斗结束 |

### 核心方法

| 方法 | 功能 |
|------|------|
| AddPlayer | 添加玩家到房间 |
| RemovePlayer | 移除玩家 |
| GetUnit | 获取战斗单位 |
| RemoveUnit | 移除战斗单位 |
| GetAllUnits | 获取所有存活单位 |
| GetUnitsByCamp | 按阵营获取单位 |
| BroadcastToPlayers | 广播消息给所有玩家 |

---

## 波次管理 (WaveManagerComponent)

### 波次状态

| 状态 | 说明 |
|------|------|
| None | 未开始 |
| Preparing | 准备中 |
| Fighting | 战斗中 |
| Completed | 已完成 |

### 核心流程

```csharp
// 1. 开始第一波
await WaveManager.StartFirstWave();

// 2. 开始下一波
await WaveManager.StartNextWave();

// 3. 怪物死亡回调
await WaveManager.OnMonsterDead(long monsterId);

// 4. 波次完成
await WaveManager.OnWaveCompleted();

// 5. 所有波次完成（战斗胜利）
await WaveManager.OnAllWavesCompleted();
```

### 怪物生成

```csharp
// 从波次配置生成怪物
await WaveManager.SpawnWaveMonsters(int waveConfigId);

// 从刷怪配置生成
await WaveManager.SpawnFromSpawnConfig(SpawnConfig spawnConfig);
```

**生成流程：**
1. 读取 WaveConfig 获取批次信息
2. 遍历批次，读取 SpawnConfig
3. 根据延迟时间依次生成怪物
4. 随机分布怪物位置（SpreadRange）
5. 广播怪物创建消息给客户端

---

## 战斗单位 (BattleUnit)

### 单位阵营

| 阵营 | 说明 |
|------|------|
| Friend | 友方（玩家） |
| Enemy | 敌方（怪物） |

### 属性系统

通过 NumericComponent 管理战斗属性：

| 属性 | 说明 |
|------|------|
| Hp | 生命值 |
| MaxHp | 最大生命值 |
| Attack | 攻击力 |
| Defense | 防御力 |

---

## 攻击系统

### 客户端请求

```csharp
// 客户端发起攻击
C2M_AttackTarget {
    long AttackerId;    // 攻击者ID
    long TargetId;      // 目标ID（可选）
    int SkillId;        // 技能ID
}
```

### 服务端处理 (C2M_AttackTargetHandler)

```csharp
// 处理流程：
1. 验证玩家是否在战斗中
2. 检查是否处于自动战斗模式
3. 验证攻击冷却
4. 查找攻击范围内的所有敌人（范围攻击）
5. 计算伤害
6. 扣除目标生命值
7. 返回攻击结果
```

### 伤害计算

```csharp
int damage = attack - defense;
if (damage < 1) damage = 1;  // 最低伤害为1
```

### 服务端响应

```csharp
M2C_AttackTarget {
    int Error;
    string Message;
    CombatResultProto result;
}

CombatResultProto {
    long AttackerId;
    long TargetId;
    int Damage;
    int AttackerCurrentHp;
    int TargetCurrentHp;
    bool TargetDead;
    bool AttackerDead;
}
```

---

## 死亡系统 (BattleUnitDead_Event)

### 事件触发

当战斗单位生命值 <= 0 时，触发 BattleUnitDead 事件。

### 处理逻辑

```csharp
// 怪物死亡
if (deadUnit.Camp == UnitCamp.Enemy) {
    // 通知 WaveManager 处理怪物死亡
    await waveManager.OnMonsterDead(deadUnit.Id);
}

// 玩家死亡
else if (deadUnit.Camp == UnitCamp.Friend) {
    // 检查是否所有玩家都死亡
    if (allHeroesDead) {
        await OnBattleFailed(battleRoom);
    }
}
```

---

## 波次完成

### 单波完成条件

所有怪物死亡后，触发波次完成：

```csharp
if (CurrentWaveMonsterIds.Count == 0 && State == WaveState.Fighting) {
    await OnWaveCompleted();
}
```

### 波次完成处理

1. 广播 M2C_WaveComplete 消息
2. 判断是否还有下一波
3. 如果有下一波且 AutoStartNextWave=true，自动开始下一波
4. 否则等待玩家手动开始

---

## 战斗结束

### 胜利条件

所有波次完成（所有怪物死亡）

```csharp
// M2C_BattleEnd
{
    long BattleId;
    bool Success;  // true = 胜利
    int Duration;  // 战斗持续时间（毫秒）
}
```

### 失败条件

所有玩家英雄死亡

### 结束处理

1. 广播 M2C_BattleEnd 消息
2. 等待 3-5 秒（展示结算界面）
3. 移除玩家出战斗房间
4. 销毁 BattleRoom

---

## 客户端消息

| 消息 | 方向 | 说明 |
|------|------|------|
| C2M_AttackTarget | C→S | 玩家攻击请求 |
| M2C_AttackTarget | S→C | 攻击结果 |
| M2C_CreateBattleUnits | S→C | 创建战斗单位 |
| M2C_WaveStart | S→C | 波次开始 |
| M2C_WaveComplete | S→C | 波次完成 |
| M2C_BattleEnd | S→C | 战斗结束 |

---

## 注意事项

1. **服务器验证**：所有攻击请求必须在服务端验证，不能信任客户端数据
2. **状态同步**：战斗状态变化需要及时同步到客户端
3. **原子性**：怪物死亡和波次完成需要保证原子性，避免重复处理
4. **范围攻击**：当前实现为范围攻击，会攻击范围内所有敌人
5. **自动战斗**：自动战斗模式下忽略手动攻击请求
6. **冷却机制**：攻击有冷却时间，需通过 IsAttackReady() 检查
