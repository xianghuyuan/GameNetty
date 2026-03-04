# BattleHelper 使用指南

## 概述

`BattleHelper` 是战斗系统的辅助类，封装了所有战斗相关的网络请求操作，遵循 ET 框架的 Helper 模式。

---

## 📝 使用方法

### 1. 开始单人战斗

```csharp
// 在按钮点击事件中
private async void OnStartBattleButtonClick()
{
    Scene scene = GetCurrentScene();
    
    // 开始波次战斗，5波怪物
    long battleId = await BattleHelper.StartBattle(scene, battleType: 0, totalWaves: 5);
    
    if (battleId > 0)
    {
        // 战斗开始成功
        Log.Info($"战斗开始，BattleId: {battleId}");
        
        // 保存 battleId 供后续使用
        SaveCurrentBattleId(battleId);
        
        // 进入战斗场景
        await EnterBattleScene(battleId);
    }
    else
    {
        // 战斗开始失败
        ShowErrorTip("战斗开始失败");
    }
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
    public async ETTask StartBattleFlow()
    {
        // 1. 开始战斗
        currentBattleId = await BattleHelper.StartBattle(scene, battleType: 0, totalWaves: 5);
        
        if (currentBattleId == 0)
        {
            Log.Error("战斗开始失败");
            return;
        }
        
        // 2. 进入战斗场景
        await EnterBattleScene();
        
        // 3. 监听战斗事件
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
        
        // 3. 返回主城
        await ReturnToMainCity();
        
        Log.Info("战斗流程结束");
    }
    
    private async ETTask EnterBattleScene()
    {
        // 加载战斗场景
        await SceneChangeHelper.SceneChangeTo(scene, "BattleScene", currentBattleId);
    }
    
    private void RegisterBattleEvents()
    {
        // 注册战斗事件监听
        // 例如：伤害、死亡、波次完成等
    }
    
    private void CleanupBattleData()
    {
        currentBattleId = 0;
        // 清理其他战斗数据
    }
    
    private async ETTask ReturnToMainCity()
    {
        await SceneChangeHelper.SceneChangeTo(scene, "MainCity", 0);
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
- `battleType`: 战斗类型（0=波次战斗, 1=副本, 2=Boss）
- `totalWaves`: 总波数（默认5）

**返回：**
- `long`: 战斗ID（BattleRoomId），失败返回0

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
// 战斗类型枚举
public enum BattleType
{
    WaveBattle = 0,  // 波次战斗：一波一波的怪物
    Dungeon = 1,     // 副本：有剧情和关卡的副本
    Boss = 2,        // Boss战：单个强力Boss
}
```

**使用示例：**
```csharp
// 波次战斗
await BattleHelper.StartBattle(scene, battleType: 0, totalWaves: 5);

// 副本
await BattleHelper.StartBattle(scene, battleType: 1, totalWaves: 0);

// Boss战
await BattleHelper.StartBattle(scene, battleType: 2, totalWaves: 0);
```

---

## 🔗 相关文档

- [基于BattleRoom的多人战斗系统设计.md](../../docs/框架设计/基于BattleRoom的多人战斗系统设计.md) - 战斗系统架构设计
- [BattleRoom战斗系统实施总结.md](../../docs/框架设计/BattleRoom战斗系统实施总结.md) - 实施细节

---

**创建日期**: 2026-03-04
**作者**: Droid
