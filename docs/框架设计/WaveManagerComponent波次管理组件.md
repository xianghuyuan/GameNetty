# WaveManagerComponent 波次管理组件

## 📝 概述

`WaveManagerComponent` 是波次战斗的核心组件，负责管理波次流程、怪物生成、波次完成判定等功能。

---

## 🏗️ 组件结构

### 组件定义

```csharp
[ComponentOf(typeof(BattleRoom))]
public class WaveManagerComponent : Entity, IAwake<int>, IDestroy
{
    public int TotalWaves { get; set; }              // 总波数
    public int CurrentWave { get; set; }             // 当前波次（从1开始）
    public WaveState State { get; set; }             // 当前波次状态
    public long WaveStartTime { get; set; }          // 当前波次开始时间
    public List<long> CurrentWaveMonsterIds { get; set; }  // 当前波次的怪物ID列表
    public int WaveInterval { get; set; }            // 波次间隔时间（毫秒）
    public bool AutoStartNextWave { get; set; }      // 是否自动开始下一波
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
StartFirstWave() - 开始第一波
    ↓
SpawnWaveMonsters() - 生成怪物
    ↓
State = Fighting - 战斗中
    ↓
OnMonsterDead() - 怪物死亡回调
    ↓
所有怪物死亡？
    ↓ 是
OnWaveCompleted() - 波次完成
    ↓
还有下一波？
    ↓ 是
等待间隔时间
    ↓
StartNextWave() - 开始下一波
    ↓
（循环）
    ↓ 否
OnAllWavesCompleted() - 所有波次完成
    ↓
战斗胜利
```

---

## 📋 核心方法

### StartFirstWave()
开始第一波

```csharp
WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int>(totalWaves);
await waveManager.StartFirstWave();
```

### StartNextWave()
开始下一波

```csharp
await waveManager.StartNextWave();
```

**流程**：
1. 检查是否还有下一波
2. 进入准备状态
3. 广播波次开始消息 `M2C_WaveStart`
4. 等待间隔时间（第一波除外）
5. 生成怪物
6. 进入战斗状态

### OnMonsterDead()
怪物死亡回调

```csharp
await waveManager.OnMonsterDead(monsterId);
```

**功能**：
- 从当前波次列表移除怪物
- 检查是否所有怪物都死亡
- 如果是，触发波次完成

### OnWaveCompleted()
当前波次完成

**功能**：
- 设置状态为已完成
- 计算耗时
- 广播波次完成消息 `M2C_WaveComplete`
- 自动开始下一波（如果启用）

### OnAllWavesCompleted()
所有波次完成

**功能**：
- 设置战斗状态为结束
- 广播战斗结束消息 `M2C_BattleEnd`
- 延迟清理房间

---

## 🎯 使用示例

### 服务端：初始化波次战斗

```csharp
private async ETTask InitWaveBattle(BattleRoom battleRoom, Unit unit, int totalWaves)
{
    // 1. 创建玩家战斗单位
    BattleUnit playerUnit = UnitFactory.CreateHero(
        battleRoom, 
        unit.Id, 
        unit.ConfigId, 
        new Vector3(0, 0, 0)
    );
    
    battleRoom.Units[playerUnit.Id] = playerUnit;
    
    // 2. 添加波次管理组件
    WaveManagerComponent waveManager = battleRoom.AddComponent<WaveManagerComponent, int>(totalWaves);
    
    // 3. 开始第一波
    await waveManager.StartFirstWave();
}
```

### 服务端：怪物死亡处理

```csharp
// 在怪物死亡事件中
[Event(SceneType.Battle)]
public class BattleUnitDeadEvent_WaveManager : AEvent<Scene, BattleUnitDeadEvent>
{
    protected override async ETTask Run(Scene scene, BattleUnitDeadEvent args)
    {
        BattleRoom battleRoom = scene as BattleRoom;
        if (battleRoom == null) return;
        
        // 检查是否是怪物
        if (args.Unit.Camp != UnitCamp.Enemy) return;
        
        // 通知波次管理器
        WaveManagerComponent waveManager = battleRoom.GetComponent<WaveManagerComponent>();
        if (waveManager != null)
        {
            await waveManager.OnMonsterDead(args.Unit.Id);
        }
    }
}
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

### 怪物数量规则

```csharp
private static int GetMonsterCountForWave(this WaveManagerComponent self, int wave)
{
    // 简单的递增规则：每波增加1个怪物
    // 第1波: 3个，第2波: 4个，第3波: 5个...
    return 2 + wave;
}
```

**可自定义为**：
- 固定数量
- 线性增长
- 指数增长
- 从配置表读取

### 怪物配置ID

```csharp
private static int GetMonsterConfigIdForWave(this WaveManagerComponent self, int wave)
{
    // TODO: 从配置表读取
    // 可以根据波次返回不同的怪物类型
    return 2001; // 普通怪物配置ID
}
```

**可扩展为**：
- 每波不同的怪物类型
- 混合多种怪物
- Boss波（特殊波次）
- 精英怪物

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

// 玩家点击"开始下一波"按钮时
await waveManager.StartNextWave();
```

### 2. 强制完成当前波次（调试用）

```csharp
await waveManager.ForceCompleteCurrentWave();
```

### 3. 自定义怪物生成位置

```csharp
// 修改 SpawnWaveMonsters 方法
float3 position = new float3(
    Random.Range(-5, 5),  // 随机X位置
    0,
    Random.Range(5, 15)   // 随机Z位置
);
```

### 4. Boss波

```csharp
private static int GetMonsterConfigIdForWave(this WaveManagerComponent self, int wave)
{
    // 每5波出现一个Boss
    if (wave % 5 == 0)
    {
        return 3001; // Boss配置ID
    }
    return 2001; // 普通怪物配置ID
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

### 1. 怪物死亡必须通知波次管理器

```csharp
// ❌ 错误：怪物死亡后没有通知
monster.Dispose();

// ✅ 正确：先通知波次管理器
await waveManager.OnMonsterDead(monster.Id);
monster.Dispose();
```

### 2. 房间清理时机

波次管理器会在所有波次完成后自动清理房间，延迟5秒给客户端显示结算界面。

### 3. 并发问题

波次管理器使用 `async/await`，确保波次流程按顺序执行，不会出现并发问题。

---

## 🚀 扩展建议

### 1. 配置表驱动

```csharp
// 从配置表读取波次配置
WaveConfig config = WaveConfigCategory.Instance.Get(waveId);
int monsterCount = config.MonsterCount;
int monsterConfigId = config.MonsterConfigId;
```

### 2. 随机事件

```csharp
// 某些波次触发随机事件
if (wave == 3)
{
    SpawnEliteMonster(); // 生成精英怪
}
```

### 3. 奖励系统

```csharp
// 波次完成后给予奖励
private async ETTask OnWaveCompleted(this WaveManagerComponent self)
{
    // 给予金币奖励
    GiveWaveReward(self.CurrentWave);
    
    // ...
}
```

### 4. 难度调整

```csharp
// 根据玩家数量调整怪物数量
int playerCount = battleRoom.PlayerIds.Count;
int monsterCount = baseCount * playerCount;
```

---

## 📖 相关文档

- [C2M_StartBattleHandler实现指南.md](./C2M_StartBattleHandler实现指南.md)
- [基于BattleRoom的多人战斗系统设计.md](./基于BattleRoom的多人战斗系统设计.md)

---

**创建日期**: 2026-03-04
**作者**: Droid
**版本**: v1.0
