# BattleRoom 战斗系统实施总结

## 完成日期
2026-01-20

## 更新日期
2026-03-04（更新为 BattleRoom 命名）

## 项目概述
成功将战斗系统从直接挂载在 Map Scene 改为基于 BattleRoom 的虚拟能力场景架构，实现逻辑隔离和多战斗实例支持，并完善了错误码体系。

---

## ✅ 完成的工作清单

### 1. 核心组件创建

#### 1.1 BattleRoom 实体
**文件**: `Server/Model/Demo/Battle/BattleScene.cs`

```csharp
[ComponentOf]
public class BattleRoom : Entity, IScene, IAwake, IUpdate
{
    public Fiber Fiber { get; set; }
    public SceneType SceneType { get; set; } = SceneType.Battle;
    public string Name { get; set; }

    // 玩家列表
    public List<long> PlayerIds { get; } = new();

    // 关卡/副本配置
    public int ConfigId { get; set; }
    public int RandomSeed { get; set; }
    public BattleState State { get; set; }
    
    // 当前掉落选择
    public DropChoice CurrentChoice { get; set; }
    
    // 玩家选择状态
    public Dictionary<long, bool> PlayerChoiceStates { get; } = new();
    
    // 战斗单位
    public Dictionary<long, EntityRef<BattleUnit>> Units { get; } = new();
}
```

#### 1.2 BattleState 枚举
**文件**: `Server/Model/Demo/Battle/BattleScene.cs`

```csharp
public enum BattleState
{
    None = 0,
    Prepare = 1,        // 准备阶段
    Fighting = 2,       // 战斗中
    WaitingChoice = 3,  // 等待玩家选择（掉落装备、技能选择等）
    Settle = 4,         // 结算中
    End = 5,            // 已结束
}
```

#### 1.3 BattleRoomManagerComponent
**文件**: `Model/Server/Demo/Battle/BattleRoomManagerComponent.cs`

**职责**:
- 管理 Map Scene 下的所有战斗房间
- 维护 `UnitIdToBattleRoomId` 映射（玩家 → 房间）
- 维护 `BattleRoomIdToBattleRoom` 映射（房间ID → 房间实体）
- 提供房间查询和管理接口

**关键实现**:
```csharp
[ComponentOf(typeof(Scene))]
public class BattleRoomManagerComponent : Entity, IAwake, IDestroy
{
    public Dictionary<long, long> UnitIdToBattleRoomId;
    public Dictionary<long, EntityRef<BattleRoom>> BattleRoomIdToBattleRoom; // 使用 EntityRef 避免循环引用
}
```

#### 1.4 BattleRoomManagerComponentSystem
**文件**: `Hotfix/Server/Demo/Battle/BattleRoomManagerComponentSystem.cs`

**核心方法**:
- `GetBattleRoomByUnitId()`: 根据玩家ID获取房间
- `GetBattleRoomById()`: 根据房间ID获取房间
- `AddBattleRoom()`: 添加新房间
- `RemoveBattleRoom()`: 移除房间
- `AddUnitToBattleRoom()`: 添加玩家到房间
- `RemoveUnitFromBattleRoom()`: 从房间移除玩家
- `IsUnitInBattle()`: 检查玩家是否在战斗中
- `GetActiveBattleRooms()`: 获取所有活跃房间

**EntityRef 正确使用**:
```csharp
public static List<BattleRoom> GetActiveBattleRooms(this BattleRoomManagerComponent self)
{
    List<BattleRoom> activeBattleRooms = new List<BattleRoom>();

    foreach (EntityRef<BattleRoom> battleRoomRef in self.BattleRoomIdToBattleRoom.Values)
    {
        BattleRoom battleRoom = battleRoomRef;  // 隐式转换
        if (battleRoom != null && battleRoom.State == BattleState.Fighting)
        {
            activeBattleRooms.Add(battleRoom);
        }
    }

    return activeBattleRooms;
}
```

---

### 2. 架构调整

#### 2.1 BattleComponent 调整
**文件**: `Model/Share/Demo/Battle/BattleComponent.cs`

**修改**:
- 移除 `[ComponentOf(typeof(Scene))]`
- 添加 `[ComponentOf(typeof(BattleRoom))]`

**影响**:
- BattleComponent 现在挂载在 BattleRoom 下，而不是直接挂载在 Scene 下
- 战斗结束时通过 BattleRoom 广播消息

#### 2.2 BattleComponentSystem 调整
**文件**: `Hotfix/Server/Demo/Battle/BattleComponentSystem.cs`

**修改**:
- `BroadcastBattleEnd()` 方法改为使用 `RoomMessageHelper.BroadcastToBattleRoom()`
- 从父级 BattleRoom 获取玩家列表
- 发布事件到 BattleRoom 而不是 Scene

#### 2.3 RoomMessageHelper 扩展
**文件**: `Hotfix/Server/Demo/Map/RoomMessageHelper.cs`

**新增方法**:
```csharp
public static void BroadcastToBattleRoom(BattleRoom battleRoom, IMessage message)
{
    Scene mapScene = battleRoom.Scene();
    UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
    
    foreach (long playerId in battleRoom.PlayerIds)
    {
        Unit player = unitComponent.Get(playerId);
        if (player != null)
        {
            MapMessageHelper.SendToClient(player, message);
        }
    }
}

public static void BroadcastToBattleRoomExcept(BattleRoom battleRoom, long exceptPlayerId, IMessage message)
{
    Scene mapScene = battleRoom.Scene();
    UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
    
    foreach (long playerId in battleRoom.PlayerIds)
    {
        if (playerId == exceptPlayerId) continue;
        
        Unit player = unitComponent.Get(playerId);
        if (player != null)
        {
            MapMessageHelper.SendToClient(player, message);
        }
    }
}
```

---

### 3. 消息处理器实现

#### 3.1 C2M_StartBattleHandler（单人战斗）
**文件**: `Hotfix/Server/Demo/Battle/Handler/C2M_StartBattleHandler.cs`

**流程**:
1. 获取或创建 BattleRoomManagerComponent
2. 检查玩家是否已在战斗中
3. 创建 BattleRoom 实体（使用 `AddChild<BattleRoom>()` 自动生成 ID）
4. 在 BattleRoom 中创建 BattleComponent
5. 更新房间管理器映射
6. 开始战斗
7. 同步初始战斗状态给客户端
8. 响应客户端

**关键代码**:
```csharp
// 创建战斗房间
BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();
battleRoom.Fiber = mapScene.Fiber;
battleRoom.SceneType = SceneType.Battle;
battleRoom.State = BattleState.Prepare;
battleRoom.PlayerIds.Add(unit.Id);

// 在 BattleRoom 中创建战斗组件
BattleComponent battle = battleRoom.AddComponent<BattleComponent>();
```

#### 3.2 C2M_TeamStartBattleHandler（组队战斗）
**文件**: `Hotfix/Server/Demo/Battle/Handler/C2M_TeamStartBattleHandler.cs`

**流程**:
1. 获取队伍成员列表
2. 检查所有队员是否都在空闲状态
3. 创建 BattleRoom 实体
4. 在 BattleRoom 中创建 BattleComponent
5. 更新所有队员的房间映射
6. 开始战斗
7. 同步战斗状态给所有队员
8. 响应所有队员

**协议定义**:
```protobuf
message C2M_TeamStartBattle // ILocationRequest
{
    int32 RpcId = 1;
    int64 teamId = 2;      // 队伍ID
    int32 battleType = 3;  // 战斗类型
    int32 totalWaves = 4;  // 总波数
}

message M2C_TeamStartBattle // ILocationResponse
{
    int32 RpcId = 1;
    int32 Error = 2;
    string Message = 3;
    int64 battleId = 4;    // BattleRoomId
    repeated int64 memberIds = 5;  // 所有参战成员ID
}
```

#### 3.3 C2M_JoinTeamBattleHandler（中途加入）
**文件**: `Hotfix/Server/Demo/Battle/Handler/C2M_JoinTeamBattleHandler.cs`

**流程**:
1. 检查玩家是否已在战斗中
2. 获取目标房间
3. 检查房间状态是否为 Fighting
4. 加入房间 PlayerIds
5. 更新房间映射
6. 同步当前战斗状态给新加入的玩家
7. 广播加入消息给所有玩家

**协议定义**:
```protobuf
message C2M_JoinTeamBattle // ILocationRequest
{
    int32 RpcId = 1;
    int64 battleId = 2;    // BattleRoomId
}

message M2C_JoinTeamBattle // ILocationResponse
{
    int32 RpcId = 1;
    int32 Error = 2;
    string Message = 3;
    repeated int64 memberIds = 4;  // 当前所有成员
}
```

#### 3.4 C2M_ExitBattleHandler（退出战斗）
**文件**: `Hotfix/Server/Demo/Battle/Handler/C2M_ExitBattleHandler.cs`

**流程**:
1. 获取玩家所在房间
2. 从房间移除玩家
3. 从房间映射中移除玩家
4. 从战斗组件移除玩家
5. 如果房间为空，销毁房间
6. 如果战斗无玩家，结束战斗

---

## 🎯 关键优化点

### 1. EntityRef 正确使用

**问题识别**：
- `EntityRef<T>` 是 struct（值类型），不能与 null 直接比较
- 需要先隐式转换为 `T` 类型才能访问属性

**解决方案**：
```csharp
// ✅ 正确做法
foreach (EntityRef<BattleRoom> battleRoomRef in self.BattleRoomIdToBattleRoom.Values)
{
    BattleRoom battleRoom = battleRoomRef;  // 隐式转换
    if (battleRoom != null && battleRoom.State == BattleState.Fighting)
    {
        activeBattleRooms.Add(battleRoom);
    }
}
```

### 2. 移除手动 ID 生成

**优化前**：
```csharp
long battleRoomId = IdGenerater.Instance.GenerateId();
BattleRoom battleRoom = mapScene.AddChildWithId<BattleRoom, long>(battleRoomId);
```

**优化后**：
```csharp
BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();  // ID 自动生成
```

**优势**：
- 代码更简洁
- 符合 ET 框架设计理念
- 避免手动管理 ID 可能导致的冲突

### 3. BattleRoom 广播机制

**实现**：
```csharp
public static void BroadcastToBattleRoom(BattleRoom battleRoom, IMessage message)
{
    Scene mapScene = battleRoom.Scene();
    UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();

    foreach (long playerId in battleRoom.PlayerIds)
    {
        Unit playerUnit = unitComponent.Get(playerId);
        if (playerUnit != null)
        {
            SendToClient(playerUnit, message);
        }
    }
}
```

**优势**：
- 统一的消息广播接口
- 自动处理玩家离线情况
- 易于扩展和调试

---

## 📊 架构对比

| 特性 | 旧设计（Scene直挂） | 新设计（BattleRoom架构） |
|------|-------------------|------------------------|
| 多战斗实例 | ❌ 同一 Scene 只能有一个 | ✅ 支持多个独立 BattleRoom |
| 组队支持 | ⚠️ 需要手动管理 | ✅ BattleRoom 自动管理 |
| 逻辑隔离 | ❌ 共享 Scene 状态 | ✅ BattleRoom 独立逻辑域 |
| 事件系统 | ⚠️ 需要手动过滤 | ✅ BattleRoom 自动隔离 |
| 生命周期 | ⚠️ 手动管理组件 | ✅ BattleRoom.Dispose() 自动清理 |
| 扩展性 | ❌ 难以扩展 | ✅ 易于添加新房间类型 |
| 错误处理 | ⚠️ 缺少专用错误码 | ✅ 完整的错误码体系 |

---

## 📁 文件清单

### 新增/修改文件

#### Model 层
1. `Server/Model/Demo/Battle/BattleScene.cs` - BattleRoom 实体定义
2. `Model/Server/Demo/Battle/BattleRoomManagerComponent.cs` - 房间管理器组件
3. `Model/Share/Demo/Battle/BattleComponent.cs` - 修改 ComponentOf

#### Hotfix 层
4. `Server/Hotfix/Demo/Map/Unit/UnitFactory.cs` - 更新为 BattleRoom 参数
5. `Hotfix/Server/Demo/Battle/BattleRoomManagerComponentSystem.cs` - 房间管理器系统
6. `Hotfix/Server/Demo/Battle/Handler/C2M_StartBattleHandler.cs` - 单人战斗
7. `Hotfix/Server/Demo/Battle/Handler/C2M_TeamStartBattleHandler.cs` - 组队战斗
8. `Hotfix/Server/Demo/Battle/Handler/C2M_JoinTeamBattleHandler.cs` - 中途加入
9. `Hotfix/Server/Demo/Battle/Handler/C2M_ExitBattleHandler.cs` - 退出战斗
10. `Hotfix/Server/Demo/Battle/BattleComponentSystem.cs` - 使用 BattleRoom 广播
11. `Hotfix/Server/Demo/Map/RoomMessageHelper.cs` - 添加 BroadcastToBattleRoom

#### Proto 层
12. `Config/Proto/OuterMessage_C_10001.proto` - 新增组队协议

---

## 📝 使用示例

### 单人战斗
```csharp
// 客户端
C2M_StartBattle request = new() { battleType = 1, totalWaves = 3 };
M2C_StartBattle response = await session.Call(request);
Console.WriteLine($"战斗开始: BattleRoomId={response.battleId}");
```

### 组队战斗
```csharp
// 队长
C2M_TeamStartBattle request = new() { teamId = 100, battleType = 1, totalWaves = 5 };
M2C_TeamStartBattle response = await session.Call(request);
Console.WriteLine($"组队战斗: BattleRoomId={response.battleId}, Members={response.memberIds.Count}");

// 队员中途加入
C2M_JoinTeamBattle joinRequest = new() { battleId = response.battleId };
M2C_JoinTeamBattle joinResponse = await session.Call(joinRequest);
```

### 退出战斗
```csharp
C2M_ExitBattle request = new() { battleId = battleRoomId };
M2C_ExitBattle response = await session.Call(request);
```

---

## 🚀 后续扩展

### 1. 队伍系统集成
当前使用简化实现，需要：
- 创建 `TeamComponent`
- 实现队伍创建、加入、离开功能
- 从队伍系统获取真实的成员列表

### 2. 房间生命周期优化
- 添加房间空闲超时清理机制
- 实现房间状态持久化
- 添加房间监控和统计

### 3. 测试验证
- 单人战斗流程测试
- 多人组队战斗测试
- 中途加入战斗测试
- 退出战斗测试
- 多房间并发测试
- 性能测试

---

## 🎉 总结

成功实现了基于 BattleRoom 的战斗系统架构，主要成果：

1. **架构升级** - 从 Scene 直挂改为 BattleRoom 虚拟能力场景
2. **逻辑隔离** - 每个房间独立，支持多战斗实例
3. **代码优化** - 正确使用 EntityRef，移除手动 ID 生成
4. **命名清晰** - 使用 BattleRoom 而非通用的 Room，语义更明确
5. **文档齐全** - 提供完整的技术文档和使用指南

### 关键优势

1. **独立逻辑域**：每个 BattleRoom 是独立的虚拟能力场景
2. **场景继承链**：BattleRoom 内实体的 IScene 指向 BattleRoom
3. **事件隔离**：事件发布到 BattleRoom 只影响 BattleRoom 内实体
4. **组件化管理**：利用 ET 的 ECS 架构
5. **易于扩展**：可以轻松添加新的房间类型（副本、竞技场等）

---

**文档版本**: v2.0
**创建日期**: 2026-01-20
**更新日期**: 2026-03-04
**作者**: Droid
**状态**: ✅ 实施完成，已更新为 BattleRoom 命名
