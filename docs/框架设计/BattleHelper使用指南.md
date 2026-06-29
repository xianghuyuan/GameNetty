# BattleHelper 使用指南

> 最后更新: 2026-04-13

## 概述

`BattleHelper` 是战斗系统的辅助类，封装了所有战斗相关的网络请求操作，遵循 ET 框架的 Helper 模式。

**文件位置**: `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/BattleHelper.cs`

---

## 📝 使用方法

### 1. 开始单人战斗

```csharp
// 在按钮点击事件中
private async void OnStartBattleButtonClick()
{
    Scene scene = GetCurrentScene();
    
    // 开始战斗，指定关卡配置ID
    long battleId = await BattleHelper.StartBattle(scene, stageId: 1001, battleType: 0);
    
    if (battleId > 0)
    {
        // 战斗开始成功（内部已调用 BattleComponent.CreateBattle）
        Log.Info($"战斗开始，BattleId: {battleId}");
        
        // 保存 battleId 供后续使用
        SaveCurrentBattleId(battleId);
    }
    else
    {
        // 战斗开始失败
        ShowErrorTip("战斗开始失败");
    }
}
```

### 2. 战斗准备就绪

```csharp
// 客户端加载完战斗资源后通知服务端
private async void OnBattleReady()
{
    Scene scene = GetCurrentScene();
    await BattleHelper.BattleReady(scene);
    // 服务端收到后会将 BattleRoom.State 设为 Fighting，并启动所有 AI 决策
}
```

### 2. 开启组队战斗（队长）

```csharp
// 队长点击开始战斗
private async void OnTeamLeaderStartBattle()
{
    Scene scene = GetCurrentScene();
    long teamId = GetCurrentTeamId();
    
    // 开启组队战斗
    long battleId = await BattleHelper.StartTeamBattle(scene, teamId, battleType: 0, totalWaves: 5);
    
    if (battleId > 0)
    {
        Log.Info($"组队战斗开始，BattleId: {battleId}");
        // 所有队员会自动收到通知
    }
    else
    {
        ShowErrorTip("开启组队战斗失败");
    }
}
```

### 3. 加入组队战斗（队员）

```csharp
// 队员中途加入战斗
private async void OnJoinTeamBattle(long battleId)
{
    Scene scene = GetCurrentScene();
    
    bool success = await BattleHelper.JoinTeamBattle(scene, battleId);
    
    if (success)
    {
        Log.Info("成功加入组队战斗");
        await EnterBattleScene(battleId);
    }
    else
    {
        ShowErrorTip("加入战斗失败");
    }
}
```

### 4. 退出战斗

```csharp
// 玩家点击退出战斗
private async void OnExitBattleButtonClick()
{
    Scene scene = GetCurrentScene();
    long battleId = GetCurrentBattleId();
    
    bool success = await BattleHelper.ExitBattle(scene, battleId);
    
    if (success)
    {
        Log.Info("成功退出战斗");
        // 返回主城
        await ReturnToMainCity();
    }
    else
    {
        ShowErrorTip("退出战斗失败");
    }
}
```

### 5. 释放技能

```csharp
// 玩家释放技能
private async void OnCastSkill(int skillId, long targetId)
{
    Scene scene = GetCurrentScene();
    
    bool success = await BattleHelper.CastSkill(scene, skillId, targetId);
    
    if (success)
    {
        Log.Debug($"技能释放成功: {skillId}");
        // 播放技能特效
        PlaySkillEffect(skillId);
    }
    else
    {
        ShowErrorTip("技能释放失败");
    }
}
```

### 6. 同步玩家位置

```csharp
// 客户端权威移动时，定期同步位置给服务端
// 供 Boss AI 追踪玩家位置使用
private void OnPlayerMoved(float3 position)
{
    Scene scene = GetCurrentScene();
    long battleId = GetCurrentBattleId();
    
    // 发送单向消息（不等响应）
    BattleHelper.SyncPlayerPosition(scene, battleId, position);
}
```

---

## 🎯 完整示例：战斗流程

```csharp
public class BattleController
{
    private Scene scene;
    private long currentBattleId;
    
    /// <summary>
    /// 开始战斗流程
    /// </summary>
    public async ETTask StartBattleFlow(int stageId)
    {
        // 1. 开始战斗（内部已调用 BattleComponent.CreateBattle）
        currentBattleId = await BattleHelper.StartBattle(scene, stageId: stageId, battleType: 0);
        
        if (currentBattleId == 0)
        {
            Log.Error("战斗开始失败");
            return;
        }
        
        // 2. 加载战斗场景资源...
        
        // 3. 资源加载完成后通知服务端准备就绪
        await BattleHelper.BattleReady(scene);
        
        // 4. 监听战斗事件
        RegisterBattleEvents();
        
        Log.Info("战斗流程启动完成");
    }
    
    /// <summary>
    /// 战斗中释放技能
    /// </summary>
    public async ETTask CastSkillInBattle(int skillId, long targetId)
    {
        bool success = await BattleHelper.CastSkill(scene, skillId, targetId);
        
        if (success)
        {
            // 播放技能动画
            PlaySkillAnimation(skillId);
        }
    }
    
    /// <summary>
    /// 玩家移动时同步位置
    /// </summary>
    public void OnPlayerPositionChanged(float3 position)
    {
        if (currentBattleId == 0) return;
        BattleHelper.SyncPlayerPosition(scene, currentBattleId, position);
    }
    
    /// <summary>
    /// 结束战斗流程
    /// </summary>
    public async ETTask EndBattleFlow()
    {
        // 1. 退出战斗
        bool success = await BattleHelper.ExitBattle(scene, currentBattleId);
        
        if (!success)
        {
            Log.Error("退出战斗失败");
            return;
        }
        
        // 2. 清理战斗数据
        CleanupBattleData();
        
        Log.Info("战斗流程结束");
    }
    
    private void CleanupBattleData()
    {
        currentBattleId = 0;
    }
    
    private void PlaySkillAnimation(int skillId)
    {
        // 播放技能动画和特效
    }
}
```

---

## 📋 API 参考

### StartBattle
开始单人战斗

**参数：**
- `scene`: 当前场景
- `stageId`: 关卡配置ID（优先使用，默认0）
- `battleType`: 战斗类型（0=波次战斗, 1=副本, 2=Boss）

**返回：**
- `long`: 战斗ID（BattleRoomId），失败返回0

**行为**:
- 内部调用 `BattleComponent.CreateBattle(battleId, battleType)` 设置战斗状态

---

### BattleReady
通知服务端客户端已准备就绪

**参数：**
- `scene`: 当前场景

**行为**:
- 发送 `C2M_BattleReady` 请求（协程方式，不等响应）
- 服务端收到后将 `BattleRoom.State` 设为 Fighting，启动所有 AI 决策

---

### StartTeamBattle
队长开启组队战斗

**参数：**
- `scene`: 当前场景
- `teamId`: 队伍ID
- `battleType`: 战斗类型
- `totalWaves`: 总波数（默认5）

**返回：**
- `long`: 战斗ID，失败返回0

---

### JoinTeamBattle
加入进行中的组队战斗

**参数：**
- `scene`: 当前场景
- `battleId`: 战斗ID

**返回：**
- `bool`: 是否成功

---

### ExitBattle
退出战斗

**参数：**
- `scene`: 当前场景
- `battleId`: 战斗ID

**返回：**
- `bool`: 是否成功

---

### CastSkill
释放技能

**参数：**
- `scene`: 当前场景
- `skillId`: 技能ID
- `targetId`: 目标ID（可选，默认0）

**返回：**
- `bool`: 是否成功

---

### SyncPlayerPosition
同步玩家位置到服务端

**参数：**
- `scene`: 当前场景
- `battleId`: 战斗ID
- `position`: 玩家当前位置 (float3)

**行为**:
- 发送 `C2M_PlayerPositionSync` 单向消息（Send，不等待响应）
- 供 Boss AI 追踪玩家位置使用
- 包含 `BattleMoveDebugLog.RecordClientPosSync` 调试记录

---

## ⚠️ 注意事项

### 1. Scene 获取
确保正确获取当前场景：
```csharp
Scene scene = GetCurrentScene(); // 或者从 Root 获取
```

### 2. 错误处理
Helper 方法已经包含了基本的错误日志，但建议在业务层添加用户提示：
```csharp
long battleId = await BattleHelper.StartBattle(scene, 0, 5);
if (battleId == 0)
{
    // 显示用户友好的错误提示
    ShowErrorTip("战斗开始失败，请稍后重试");
}
```

### 3. BattleId 管理
战斗ID需要在整个战斗流程中保持，建议：
```csharp
// 保存到组件
public class BattleManagerComponent : Entity
{
    public long CurrentBattleId { get; set; }
}

// 或保存到全局
public static class BattleContext
{
    public static long CurrentBattleId { get; set; }
}
```

### 4. 异步调用
所有 Helper 方法都是异步的，需要使用 `await`：
```csharp
// ✅ 正确
long battleId = await BattleHelper.StartBattle(scene, 0, 5);

// ❌ 错误
long battleId = BattleHelper.StartBattle(scene, 0, 5); // 编译错误
```

---

## 🎮 战斗类型说明

```csharp
// 战斗类型（作为 StartBattle 的 battleType 参数）
// 0 = WaveBattle: 波次战斗，一波一波的怪物
// 1 = Dungeon: 副本，有剧情和关卡的副本
// 2 = Boss: Boss战，单个强力Boss
```

**使用示例：**
```csharp
// 波次战斗（指定关卡配置ID）
await BattleHelper.StartBattle(scene, stageId: 1001, battleType: 0);

// Boss战
await BattleHelper.StartBattle(scene, stageId: 2001, battleType: 2);
```

---

## 🔗 相关文档

- [BattleRoom战斗系统实施总结.md](./BattleRoom战斗系统实施总结.md) - 战斗系统架构与 BattleUnitRegistryComponent
- [WaveManagerComponent波次管理组件.md](./WaveManagerComponent波次管理组件.md) - 波次管理详情

---

**创建日期**: 2026-03-04
**更新日期**: 2026-04-13
**作者**: Droid
