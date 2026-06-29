# WaveManagerComponent 波次管理组件

> 最后更新: 2026-04-13

## 📝 概述

`WaveManagerComponent` 是波次战斗的核心组件，负责管理波次流程、配置驱动的怪物生成、波次完成判定等功能。挂载在 `BattleRoom` 上。

---

## 🏗️ 组件结构

### 组件定义

**文件**: `Server/Model/Demo/Battle/WaveManagerComponent.cs`

```csharp
[ComponentOf(typeof(BattleRoom))]
public class WaveManagerComponent : Entity, IAwake<int, List<int>>, IDestroy
{
    public int StageConfigId { get; set; }          // 关卡配置ID
    public int TotalWaves { get; set; }             // 总波数（从 WaveConfigIds.Count 计算）
    public List<int> WaveConfigIds { get; set; }    // 波次配置ID列表
    public int CurrentWaveIndex { get; set; }       // 当前波次索引（从0开始）
    public WaveState State { get; set; }            // 当前波次状态
    public long WaveStartTime { get; set; }         // 当前波次开始时间
    public List<long> CurrentWaveMonsterIds { get; set; }  // 当前波次的怪物ID列表
    public int WaveInterval { get; set; }           // 波次间隔时间（毫秒）
    public bool AutoStartNextWave { get; set; }     // 是否自动开始下一波
}
```

### 波次状态

```csharp
public enum WaveState
{
    None = 0,
    Preparing = 1,   // 准备中（波次间隔）
    Fighting = 2,    // 战斗中
    Completed = 3,   // 已完成
}
```

---

## 🎮 核心功能

### 1. 波次流程管理

```
开始战斗
    ↓
StartFirstWave() → StartNextWave()
    ↓
读取 WaveConfig → 遍历 Batches → 读取 SpawnConfig
    ↓
SpawnFromSpawnConfig: 区分 Boss/精英 vs 杂兵
    ├─ Boss/精英: CreateMonster → registry.Register → 广播 M2C_CreateBattleUnits
    └─ 杂兵: CreateMinion → registry.Register → 广播 M2C_SpawnWave（客户端本地创建）
    ↓
State = Fighting - 战斗中
    ↓
OnMonsterDead() - 怪物死亡回调
    ├─ CurrentWaveMonsterIds.Remove
    ├─ SpatialGrid.Remove
    └─ Registry.Unregister
    ↓
所有怪物死亡？
    ↓ 是
OnWaveCompleted() - 波次完成
    ↓
还有下一波？
    ↓ 是
AutoStartNextWave?
    ├─ 是 → StartNextWave() - 自动开始下一波
    └─ 否 → State = Completed, 等待 TriggerNextWave() 手动触发
    ↓
（循环）
    ↓ 否
OnAllWavesCompleted() - 所有波次完成
    ├─ battleRoom.State = End
    ├─ 广播 M2C_BattleEnd
    ├─ WaitAsync(5000)
    └─ 清理 + battleRoom.Dispose()
```

### 2. 配置驱动的刷怪体系

```
WaveManagerComponent
  └─ WaveConfigIds[] (List<int>)
       └─ WaveConfig (per wave)
            ├─ Batches[] (支持延迟刷怪)
            │    ├─ SpawnId → SpawnConfig
            │    └─ Delay (ms, 可选延迟)
            └─ SpawnConfig
                 ├─ PositionX, SpreadRange
                 └─ Monsters[]
                      ├─ MonsterId → MonsterUnitConfig
                      └─ Count
```

**怪物类型路由**（SpawnFromSpawnConfig 内部）:
- `MonsterType == 3` (Boss/精英) → `UnitFactory.CreateMonster` → 完整服务端实体
- `MonsterType != 3` (杂兵) → `UnitFactory.CreateMinion` → 轻量服务端实体 + `M2C_SpawnWave` 下发给客户端

---

## 📋 核心方法

### Awake(stageConfigId, waveConfigIds)
初始化波次管理器

```csharp
WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int, List<int>>(stageId, waveConfigIds);
// stageId: 关卡配置ID
// waveConfigIds: 波次配置ID列表（从 StageConfig 获取）
// TotalWaves 自动从 waveConfigIds.Count 计算
```

### StartFirstWave()
开始第一波

```csharp
await waveManager.StartFirstWave();
// 内部调用 StartNextWave()
```

### TriggerNextWave()
手动触发下一波（需 State == Completed）

```csharp
await waveManager.TriggerNextWave();
// 仅在 AutoStartNextWave = false 时需要手动调用
```

### StartNextWave() (internal)
开始下一波

**流程**：
1. 检查是否还有下一波 (CurrentWaveIndex < TotalWaves - 1)
2. 如果所有波次已完成 → OnAllWavesCompleted
3. CurrentWaveIndex++，进入 Preparing 状态
4. 读取 WaveConfig → 计算 monsterCount → 广播 M2C_WaveStart
5. SpawnWaveMonsters(waveConfigId) → 配置驱动的刷怪
6. 进入 Fighting 状态

### OnMonsterDead(monsterId)
怪物死亡回调

```csharp
await waveManager.OnMonsterDead(monsterId);
```

**功能**：
- 从当前波次列表移除怪物
- 从 SpatialGrid 移除
- 从 BattleUnitRegistryComponent 移除 (Unregister)
- 检查是否所有怪物都死亡
- 如果是，触发波次完成

### OnWaveCompleted() (internal)
当前波次完成

**功能**：
- 设置状态为已完成
- 计算耗时
- 广播波次完成消息 `M2C_WaveComplete`
- 如果 AutoStartNextWave → 自动开始下一波
- 否则保持 Completed 状态等待 TriggerNextWave()

### OnAllWavesCompleted() (internal)
所有波次完成

**功能**：
- 设置 battleRoom.State = End
- 广播战斗结束消息 `M2C_BattleEnd`
- WaitAsync(5000) 延迟
- 清理所有玩家映射 → roomManager 移除 → battleRoom.Dispose()

### ForceCompleteCurrentWave()
强制完成当前波次（调试用）

```csharp
await waveManager.ForceCompleteCurrentWave();
// 遍历 CurrentWaveMonsterIds → Dispose 所有怪物 → OnWaveCompleted
// 通过 BattleUnitRegistryComponent 查找并 Dispose
```

---

## 🎯 使用示例

### 服务端：初始化波次战斗

```csharp
private void InitWaveBattle(BattleRoom battleRoom, Unit playerUnit, int stageId)
{
    // 1. 创建玩家战斗单位
    BattleUnit playerUnit = UnitFactory.CreateHero(
        battleRoom, 
        playerUnit, 
        new Vector3(PlayerSpawnX, 0, 0)
    );
    
    // 通过注册表注册
    BattleUnitRegistryComponent registry = battleRoom.GetComponent<BattleUnitRegistryComponent>();
    registry.Register(playerUnit);
    
    // 2. 获取关卡配置
    StageConfigInfo stageInfo = GetStageConfig(stageId);
    
    // 3. 添加波次管理组件（配置驱动）
    WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int, List<int>>(
        stageId, 
        stageInfo.WaveConfigIds
    );
    
    // 4. 开始第一波（通常在 StartFirstWave 中调用）
    await waveManager.StartFirstWave();
}
```

### 服务端：怪物死亡处理

```csharp
// 怪物死亡事件中，WaveManagerComponent.OnMonsterDead 已包含以下逻辑:
// 1. CurrentWaveMonsterIds.Remove(monsterId)
// 2. SpatialGrid.Remove(monsterId) 
// 3. BattleUnitRegistryComponent.Unregister(monsterId)
// 无需外部手动处理
```

### 客户端：接收波次消息

```csharp
// 波次开始
[MessageHandler(SceneType.Demo)]
public class M2C_WaveStartHandler : MessageHandler<Scene, M2C_WaveStart>
{
    protected override async ETTask Run(Scene scene, M2C_WaveStart message)
    {
        Log.Info($"第 {message.waveNumber}/{message.totalWaves} 波开始！怪物数量: {message.monsterCount}");
        
        // 显示波次UI
        ShowWaveUI(message.waveNumber, message.totalWaves);
        
        // 播放波次开始音效
        PlayWaveStartSound();
        
        await ETTask.CompletedTask;
    }
}

// 波次完成
[MessageHandler(SceneType.Demo)]
public class M2C_WaveCompleteHandler : MessageHandler<Scene, M2C_WaveComplete>
{
    protected override async ETTask Run(Scene scene, M2C_WaveComplete message)
    {
        Log.Info($"第 {message.waveNumber} 波完成！耗时: {message.duration}秒");
        
        // 显示波次完成提示
        ShowWaveCompleteUI(message.waveNumber, message.duration);
        
        // 播放胜利音效
        PlayWaveCompleteSound();
        
        await ETTask.CompletedTask;
    }
}
```

---

## ⚙️ 配置说明

### 配置驱动的刷怪体系

波次管理器完全由配置表驱动，涉及以下配置表：

| 配置表 | 用途 |
|--------|------|
| `WaveConfig` | 波次配置，包含 Batches 列表 |
| `SpawnConfig` | 刷怪批次配置，定义位置、扩散范围、怪物列表 |
| `MonsterUnitConfig` | 怪物属性配置，包含 Type 字段区分 Boss/精英/杂兵 |
| `UnitCombatConfig` | 战斗配置，包含 AutoSkillIds、NormalAttackSkillId |

### 怪物类型路由

```csharp
// SpawnFromSpawnConfig 内部逻辑:
MonsterUnitConfig monsterConfig = MonsterUnitConfigCategory.Instance.GetOrDefault(monsterInfo.MonsterId);
bool isBoss = monsterConfig != null && monsterConfig.Type == 3;

if (isBoss) {
    // Boss/精英 → CreateMonster (完整服务端实体) + 广播 M2C_CreateBattleUnits
} else {
    // 杂兵 → CreateMinion (轻量服务端实体) + 广播 M2C_SpawnWave (客户端本地创建)
}
```

### 怪物数量

```csharp
// 从配置计算总怪物数:
GetTotalMonsterCount(waveConfigId)
// 遍历 WaveConfig.Batches → SpawnConfig → Monsters[] → 累加 Count
```

### 波次间隔时间

```csharp
waveManager.WaveInterval = 5000; // 5秒间隔
```

### 自动开始下一波

```csharp
waveManager.AutoStartNextWave = true;  // 自动开始
waveManager.AutoStartNextWave = false; // 手动开始（需要玩家点击）
```

---

## 📊 消息协议

### M2C_WaveStart - 波次开始

```protobuf
message M2C_WaveStart // IMessage
{
    int64 battleId = 1;
    int32 waveNumber = 2;
    int32 totalWaves = 3;
    int32 monsterCount = 4;
}
```

### M2C_WaveComplete - 波次完成

```protobuf
message M2C_WaveComplete // IMessage
{
    int64 battleId = 1;
    int32 waveNumber = 2;
    int32 totalWaves = 3;
    int32 duration = 4;  // 持续时间（秒）
}
```

### M2C_BattleEnd - 战斗结束

```protobuf
message M2C_BattleEnd // IMessage
{
    int64 battleId = 1;
    bool success = 2;
    int32 duration = 3;  // 持续时间（秒）
    map<int64, int32> playerDamage = 4;  // 玩家伤害统计
}
```

---

## 🔧 高级功能

### 1. 手动开始下一波

```csharp
// 设置为手动模式
waveManager.AutoStartNextWave = false;

// 波次完成后不会自动开始下一波
// 需要手动触发
await waveManager.TriggerNextWave();
```

### 2. 强制完成当前波次（调试用）

```csharp
await waveManager.ForceCompleteCurrentWave();
// 通过 BattleUnitRegistryComponent 获取并 Dispose 所有怪物
```

### 3. 批次延迟生成

在 `WaveConfig.Batches` 中配置 `Delay` 字段，可以实现批次间的延迟生成：
```csharp
if (batch.Delay > 0)
{
    await self.Root().GetComponent<TimerComponent>().WaitAsync(batch.Delay);
}
```

---

## 🎨 UI 集成示例

### 波次进度显示

```csharp
public class WaveUIController : MonoBehaviour
{
    public Text waveText;
    public Slider progressSlider;
    
    public void UpdateWaveInfo(int currentWave, int totalWaves)
    {
        waveText.text = $"第 {currentWave}/{totalWaves} 波";
        progressSlider.value = (float)currentWave / totalWaves;
    }
    
    public void ShowWaveStart(int waveNumber)
    {
        // 显示"第X波开始"动画
        ShowBigText($"第 {waveNumber} 波");
    }
    
    public void ShowWaveComplete(int waveNumber, int duration)
    {
        // 显示"第X波完成"提示
        ShowNotification($"第 {waveNumber} 波完成！耗时 {duration} 秒");
    }
}
```

---

## ⚠️ 注意事项

### 1. 怪物死亡必须通过 OnMonsterDead 处理

```csharp
// OnMonsterDead 内部已包含:
// 1. CurrentWaveMonsterIds.Remove(monsterId)
// 2. SpatialGrid.Remove(monsterId)
// 3. Registry.Unregister(monsterId)
// 确保怪物死亡时调用 OnMonsterDead 以维护数据一致性
```

### 2. 注册表一致性

所有通过 WaveManager 创建的怪物都会通过 `BattleUnitRegistryComponent.Register()` 注册。死亡时通过 `OnMonsterDead` 自动 Unregister。外部代码不应直接操作注册表，而是通过 BattleRoom 的 `GetUnit/ForEachUnit` 方法。

### 3. 房间清理时机

波次管理器会在所有波次完成后自动清理房间：先等待 5 秒给客户端显示结算界面，然后清理所有玩家映射并 Dispose BattleRoom。

### 4. 配置完整性

确保 `WaveConfig` → `SpawnConfig` → `MonsterUnitConfig` 配置链完整。配置缺失时会有 Log.Error 提示，不会崩溃但该批次不会生成怪物。

---

## 🚀 扩展建议

### 1. 波次事件系统

```csharp
// 在 OnWaveCompleted 中发布波次完成事件
// 可用于：奖励发放、成就计数、难度调整等
```

### 2. 动态难度调整

```csharp
// 根据玩家数量或战斗时长调整怪物属性
// 可在 SpawnFromSpawnConfig 中读取调整后的属性
```

### 3. 多波次并行

```csharp
// 当前是严格串行，如需多波同时存在
// 可修改 AutoStartNextWave 逻辑允许波次叠加
```

---

## 📖 相关文档

- [C2M_StartBattleHandler实现指南.md](./C2M_StartBattleHandler实现指南.md)
- [基于BattleRoom的多人战斗系统设计.md](./基于BattleRoom的多人战斗系统设计.md)

---

**创建日期**: 2026-03-04
**更新日期**: 2026-04-13
**作者**: Droid
**版本**: v2.0
