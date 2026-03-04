# C2M_StartBattleHandler 实现指南

## 📝 概述

本文档说明了如何实现完整的战斗系统 Handler，包括服务端和客户端的完整流程。

---

## 🏗️ 架构概览

```
客户端                    服务端
  │                        │
  │  BattleHelper          │  C2M_StartBattleHandler
  │  ↓                     │  ↓
  │  发送 C2M_StartBattle  →  接收请求
  │                        │  ↓
  │                        │  创建 BattleRoom
  │                        │  ↓
  │                        │  初始化战斗
  │                        │  ↓
  │  ← 接收 M2C_StartBattle  返回响应
  │  ↓
  │  进入战斗场景
```

---

## 📂 文件结构

### 服务端文件

```
Server/
├── Model/
│   └── Demo/
│       └── Battle/
│           ├── BattleScene.cs                    # BattleRoom 实体定义
│           └── BattleRoomManagerComponent.cs     # 房间管理器组件
│
└── Hotfix/
    └── Demo/
        └── Battle/
            ├── BattleRoomManagerComponentSystem.cs  # 房间管理器系统
            └── Handler/
                ├── C2M_StartBattleHandler.cs        # 开始战斗
                ├── C2M_ExitBattleHandler.cs         # 退出战斗
                └── C2M_TeamStartBattleHandler.cs    # 组队战斗
```

### 客户端文件

```
Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/
├── BattleHelper.cs              # 战斗辅助类
└── BattleUnitHelper.cs          # 战斗单位辅助类
```

---

## 🔧 实现步骤

### 步骤 1: 创建 BattleRoomManagerComponent (Model)

**文件**: `Server/Model/Demo/Battle/BattleRoomManagerComponent.cs`

```csharp
[ComponentOf(typeof(Scene))]
public class BattleRoomManagerComponent : Entity, IAwake, IDestroy
{
    public Dictionary<long, long> UnitIdToBattleRoomId { get; set; }
    public Dictionary<long, EntityRef<BattleRoom>> BattleRoomIdToBattleRoom { get; set; }
}
```

**作用**: 管理所有战斗房间和玩家映射关系

---

### 步骤 2: 创建 BattleRoomManagerComponentSystem (Hotfix)

**文件**: `Server/Hotfix/Demo/Battle/BattleRoomManagerComponentSystem.cs`

**核心方法**:
- `AddBattleRoom()` - 添加战斗房间
- `RemoveBattleRoom()` - 移除战斗房间
- `AddUnitToBattleRoom()` - 添加玩家映射
- `RemoveUnitFromBattleRoom()` - 移除玩家映射
- `IsUnitInBattle()` - 检查玩家是否在战斗中
- `GetBattleRoomByUnitId()` - 根据玩家ID获取房间
- `GetBattleRoomById()` - 根据房间ID获取房间

---

### 步骤 3: 创建 C2M_StartBattleHandler

**文件**: `Server/Hotfix/Demo/Battle/Handler/C2M_StartBattleHandler.cs`

**处理流程**:

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
{
    protected override async ETTask Run(Unit unit, C2M_StartBattle request, M2C_StartBattle response)
    {
        // 1. 获取房间管理器
        BattleRoomManagerComponent roomManager = ...;
        
        // 2. 检查玩家是否已在战斗中
        if (roomManager.IsUnitInBattle(unit.Id)) { ... }
        
        // 3. 创建 BattleRoom
        BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
        
        // 4. 添加到管理器
        roomManager.AddBattleRoom(battleRoom);
        roomManager.AddUnitToBattleRoom(unit.Id, battleRoom.Id);
        
        // 5. 初始化战斗内容
        await InitWaveBattle(battleRoom, unit, request.totalWaves);
        
        // 6. 开始战斗
        battleRoom.State = BattleState.Fighting;
        
        // 7. 响应客户端
        response.battleId = battleRoom.Id;
    }
}
```

---

### 步骤 4: 创建 BattleHelper (客户端)

**文件**: `Unity/Assets/GameScripts/HotFix/GameLogic/Module/Battle/BattleHelper.cs`

**核心方法**:

```csharp
public static class BattleHelper
{
    // 开始单人战斗
    public static async ETTask<long> StartBattle(Scene scene, int battleType, int totalWaves)
    {
        C2M_StartBattle request = C2M_StartBattle.Create();
        request.battleType = battleType;
        request.totalWaves = totalWaves;
        
        M2C_StartBattle response = await scene.GetComponent<ClientSenderComponent>().Call(request);
        
        if (response.Error != ErrorCode.ERR_Success)
        {
            Log.Error($"开始战斗失败: {response.Message}");
            return 0;
        }
        
        return response.battleId;
    }
}
```

---

## 🎮 使用示例

### 客户端调用

```csharp
// 在 UI 按钮点击事件中
private async void OnStartBattleButtonClick()
{
    Scene scene = GetCurrentScene();
    
    // 使用 BattleHelper 开始战斗
    long battleId = await BattleHelper.StartBattle(scene, battleType: 0, totalWaves: 5);
    
    if (battleId > 0)
    {
        Log.Info($"战斗开始成功，BattleId: {battleId}");
        
        // 保存 battleId
        SaveCurrentBattleId(battleId);
        
        // 进入战斗场景
        await EnterBattleScene(battleId);
    }
    else
    {
        ShowErrorTip("战斗开始失败");
    }
}
```

---

## 🔍 关键技术点

### 1. MessageLocationHandler

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
```

**说明**:
- `MessageLocationHandler` 用于处理需要定位到具体 Unit 的消息
- 第一个泛型参数 `Unit` 表示消息的目标实体
- 第二个泛型参数 `C2M_StartBattle` 是请求消息
- 第三个泛型参数 `M2C_StartBattle` 是响应消息

---

### 2. BattleRoom 创建

```csharp
BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
battleRoom.Fiber = mapScene.Fiber;
battleRoom.SceneType = SceneType.Battle;
```

**关键点**:
- BattleRoom 作为 Map Scene 的子实体创建
- 继承 Map Scene 的 Fiber（共享纤程）
- 设置 SceneType 为 Battle

---

### 3. EntityRef 使用

```csharp
public Dictionary<long, EntityRef<BattleRoom>> BattleRoomIdToBattleRoom { get; set; }

// 使用时需要隐式转换
BattleRoom battleRoom = battleRoomRef;
```

**原因**:
- `EntityRef<T>` 是值类型，避免循环引用
- 使用时需要先转换为实体类型

---

### 4. 错误处理

```csharp
if (roomManager.IsUnitInBattle(unit.Id))
{
    response.Error = ErrorCode.ERR_AlreadyInBattle;
    response.Message = "玩家已在战斗中";
    return;
}
```

**最佳实践**:
- 每个错误情况都设置 Error 和 Message
- 使用预定义的错误码
- 记录日志便于调试

---

## ⚠️ 常见问题

### Q1: 为什么要用 BattleRoomManagerComponent？

**A**: 
- 统一管理所有战斗房间
- 维护玩家到房间的映射关系
- 防止玩家重复进入战斗
- 便于查询和清理房间

---

### Q2: BattleRoom 和 Scene 的区别？

**A**:
- `BattleRoom` 是 Entity，同时实现了 IScene 接口
- 作为 Entity 可以添加到 Map Scene 的子实体树
- 作为 IScene 形成独立的虚拟能力场景
- BattleRoom 内的实体的 IScene 指向 BattleRoom 本身

---

### Q3: 如何处理战斗结束？

**A**:
```csharp
// 战斗结束时
battleRoom.State = BattleState.End;

// 清理玩家映射
foreach (long playerId in battleRoom.PlayerIds)
{
    roomManager.RemoveUnitFromBattleRoom(playerId);
}

// 移除并销毁房间
roomManager.RemoveBattleRoom(battleRoom.Id);
battleRoom.Dispose();
```

---

### Q4: 如何支持多人组队战斗？

**A**: 使用 `C2M_TeamStartBattleHandler`
- 队长发起战斗请求
- 检查所有队员状态
- 为所有队员创建战斗单位
- 所有队员加入同一个 BattleRoom

---

## 📊 完整流程图

```
客户端点击开始战斗
    ↓
BattleHelper.StartBattle()
    ↓
发送 C2M_StartBattle 消息
    ↓
服务端 C2M_StartBattleHandler 接收
    ↓
检查玩家状态（是否已在战斗中）
    ↓
创建 BattleRoom 实体
    ↓
添加到 BattleRoomManagerComponent
    ↓
初始化战斗内容（创建玩家单位、怪物等）
    ↓
设置战斗状态为 Fighting
    ↓
返回 M2C_StartBattle 响应
    ↓
客户端接收响应
    ↓
保存 battleId
    ↓
进入战斗场景
```

---

## 🎯 测试清单

- [ ] 单人战斗能否正常开始
- [ ] 重复开始战斗是否被拦截
- [ ] 战斗房间是否正确创建
- [ ] 玩家映射是否正确添加
- [ ] 战斗单位是否正确创建
- [ ] 退出战斗是否正常
- [ ] 房间是否正确销毁
- [ ] 组队战斗是否正常
- [ ] 错误处理是否完善
- [ ] 日志是否完整

---

## 🔗 相关文档

- [基于BattleRoom的多人战斗系统设计.md](./基于BattleRoom的多人战斗系统设计.md) - 架构设计
- [BattleHelper使用指南.md](./BattleHelper使用指南.md) - 客户端使用
- [BattleRoom战斗系统实施总结.md](./BattleRoom战斗系统实施总结.md) - 实施总结

---

**创建日期**: 2026-03-04
**作者**: Droid
**版本**: v1.0
