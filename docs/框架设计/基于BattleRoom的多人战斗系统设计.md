# 基于 BattleRoom 的多人战斗系统设计（ET 框架）

## 1. 文档概述

### 1.1 目标
设计一个支持多人组队、多战斗实例的战斗系统，基于 ET 框架的 BattleRoom（虚拟能力场景）设计，实现战斗逻辑隔离和状态同步。

### 1.2 背景
- **当前问题**：`BattleComponent` 直接挂在 Map Scene 上，同一时刻只能有一个战斗实例
- **需求**：支持多人组队、多个独立战斗副本、战斗逻辑隔离
- **解决方案**：基于 ET 的 BattleRoom 设计（Entity + IScene），创建独立的战斗虚拟能力场景

### 1.3 核心原则
- 服务器权威：所有战斗逻辑在服务器计算，客户端仅做表现
- 逻辑隔离：不同战斗实例互不影响
- 状态同步：基于事件驱动的服务器权威状态同步机制
  - 服务器计算所有战斗逻辑和状态变化
  - 通过 BattleRoom 事件系统广播状态变化给所有参与者
  - 客户端接收状态更新并同步表现层
- 组件化设计：利用 ET 的 ECS 架构
- 热更新支持：核心逻辑在 Hotfix 层

---

## 2. 架构设计

### 2.1 整体架构

```
Map Process (地图服务器进程)
├── Map Scene (SceneType.Map)
│   ├── UnitComponent (全局玩家列表)
│   │   ├── Unit A (玩家A)
│   │   ├── Unit B (玩家B)
│   │   └── Unit C (玩家C)
│   ├── BattleRoomManagerComponent (战斗房间管理器)
│   │   └── UnitIdToBattleRoomId (字典: Unit → BattleRoom 映射)
│   ├── BattleRoom (战斗房间1 - BattleRoom Entity + IScene)
│   │   ├── Fiber (继承自 Map Scene)
│   │   ├── SceneType = SceneType.Battle
│   │   ├── BattleRoomId = 1001
│   │   ├── BattleComponent (战斗组件)
│   │   │   ├── BattleId = 1001
│   │   │   ├── BattleType = WaveBattle
│   │   │   ├── State = Fighting
│   │   │   ├── PlayerIds = {A, B}
│   │   │   └── MonsterIds = {...}
│   │   ├── WaveManagerComponent (波次管理)
│   │   ├── Unit A (引用自全局)
│   │   └── Unit B (引用自全局)
│   └── BattleRoom (战斗房间2 - BattleRoom Entity + IScene)
│       ├── Fiber (继承自 Map Scene)
│       ├── SceneType = SceneType.Battle
│       ├── BattleRoomId = 1002
│       ├── BattleComponent
│       │   ├── PlayerIds = {C}
│       │   └── MonsterIds = {...}
│       └── Unit C (引用自全局)
```
```

### 2.2 核心概念：虚拟能力场景（Virtual Capability Scene）

#### 2.2.1 BattleRoom 的双重身份

```csharp
[ComponentOf]  // ← 可以作为组件添加
public class BattleRoom: Entity, IScene, IAwake, IUpdate
{
    // IScene 接口实现
    public Fiber Fiber { get; set; }        // 所属纤程
    public SceneType SceneType { get; set; } // 场景类型

    // BattleRoom 特有数据
    public long BattleRoomId;
    public HashSet<long> PlayerIds;
}
```

**关键特性：**
1. **作为 Entity**：可以添加到 Map Scene 的子实体树
2. **实现 IScene**：成为独立的"虚拟能力场景"
3. **场景继承链**：BattleRoom 的子实体继承 BattleRoom 的 IScene，而非 Map Scene

#### 2.2.2 场景继承链机制

```csharp
// 添加 BattleRoom 到 Map Scene
BattleRoom battleRoom = mapScene.AddChild<BattleRoom>();

// BattleRoom 的 IScene 指向自己（因为实现了 IScene）
battleRoom.IScene = battleRoom;

// 添加玩家到 BattleRoom
Unit player = battleRoom.AddChild<Unit>();
// player.IScene = battleRoom (不是 mapScene！)

// 结果：
// - player.Domain() 返回 battleRoom
// - player.Fiber() 返回 battleRoom.Fiber
// - player.Zone() 返回 battleRoom.Fiber.Zone
```

**优势：**
- ✅ BattleRoom 内的实体形成独立的逻辑域
- ✅ 事件发布到 BattleRoom 只影响 BattleRoom 内的实体
- ✅ 可以独立销毁 BattleRoom，不影响其他 BattleRoom

---

## 3. 核心组件设计

### 3.1 BattleRoomManagerComponent（战斗房间管理器）

**位置**：`Model/Server/Demo/Battle/BattleRoomManagerComponent.cs`

```csharp
[ComponentOf(typeof(Scene))]
public class BattleRoomManagerComponent : Entity, IAwake, IDestroy
{
    // Unit ID → BattleRoom ID 映射
    public Dictionary<long, long> UnitIdToRoomId;

    // BattleRoom ID → BattleRoom Entity 映射
    public Dictionary<long, BattleRoom> RoomIdToRoom;
}
```

**职责**：
- 管理所有战斗房间
- 维护玩家到房间的映射关系
- 提供房间查询接口

### 3.2 BattleRoom（战斗房间实体）

**位置**：`Model/Demo/Battle/BattleScene.cs`（已存在）

```csharp
[ComponentOf]
public class BattleRoom: Entity, IScene, IAwake, IUpdate
{
    // IScene 接口
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
    
    // 玩家选择状态：playerId -> 是否已选择
    public Dictionary<long, bool> PlayerChoiceStates { get; } = new();
    
    // 战斗单位
    public Dictionary<long, EntityRef<BattleUnit>> Units { get; } = new();
}
```

**已有枚举**：

```csharp
// 战斗状态
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

### 3.3 BattleComponent（战斗组件）

**位置**：`Model/Share/Demo/Battle/BattleComponent.cs`（已存在，需调整）

**修改**：移除 `[ComponentOf(typeof(Scene))]`，改为 `[ComponentOf(typeof(BattleRoom))]`

```csharp
[ComponentOf(typeof(BattleRoom))]
public class BattleComponent : Entity, IAwake<long>, IDestroy
{
    public long BattleId;              // 战斗 ID
    public BattleType Type;            // 战斗类型
    public BattleState State;          // 战斗状态
    public long StartTime;             // 开始时间
    public long EndTime;               // 结束时间

    public HashSet<long> PlayerIds;    // 参战玩家 ID
    public HashSet<long> MonsterIds;   // 战斗怪物 ID

    // ... 其他字段
}
```

**调整原因**：
- BattleComponent 现在挂在 BattleRoom 下，不是直接挂在 Scene 下
- BattleRoom 负责管理 BattleComponent 生命周期

---

## 4. 战斗流程设计

### 4.1 创建单人战斗

#### 4.1.1 客户端请求

```protobuf
// 请求开始战斗
message C2M_StartBattle {
    int32 battleType = 1;      // BattleType
    int32 totalWaves = 2;      // 波次战斗的总波数
    int32 dungeonId = 3;       // 副本 ID
}

// 响应
message M2C_StartBattle {
    int32 error = 1;
    string message = 2;
    int64 battleId = 3;        // 实际上是 RoomId
}
```

#### 4.1.2 服务端处理流程

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_StartBattleHandler : MessageLocationHandler<Unit, C2M_StartBattle, M2C_StartBattle>
{
    protected override async ETTask Run(Unit unit, C2M_StartBattle request, M2C_StartBattle response)
    {
        Scene mapScene = unit.Scene();

        // 1. 检查玩家是否已在战斗中
        BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
        if (roomManager == null)
        {
            roomManager = mapScene.AddComponent<BattleRoomManagerComponent>();
        }

        if (roomManager.UnitIdToRoomId.ContainsKey(unit.Id))
        {
            response.Error = ErrorCode.ERR_AlreadyInBattle;
            response.Message = "玩家已在战斗中";
            return;
        }

        // 2. 创建战斗房间
        BattleRoom battleRoom = mapScene.AddChildW<BattleRoom>();
        room.Fiber = mapScene.Fiber;
        room.SceneType = SceneType.Battle;
        room.RoomType = RoomType.Battle;
        room.State = RoomState.Prepare;
        room.PlayerIds = new HashSet<long> { unit.Id };

        Log.Info($"创建战斗房间: RoomId={roomId}, Player={unit.Id}");

        // 3. 在 BattleRoom 中创建战斗组件
        BattleComponent battle = room.AddComponent<BattleComponent, long>(roomId);
        battle.SetBattleType((BattleType)request.battleType);
        battle.SetTotalWaves(request.totalWaves);

        // 4. 将玩家添加到房间（引用，不是创建新 Unit）
        // 注意：这里不创建新 Unit，而是将玩家 Unit 添加到 BattleRoom 的域中
        // 通过事件系统通知玩家进入战斗状态

        // 5. 更新映射关系
        roomManager.UnitIdToRoomId[unit.Id] = roomId;
        roomManager.RoomIdToRoom[roomId] = room;

        // 6. 开始战斗
        List<long> playerIds = new List<long> { unit.Id };
        await battle.StartBattle(playerIds);

        // 7. 同步初始战斗状态给客户端
        M2C_BattleStateSync stateMsg = M2C_BattleStateSync.Create();
        stateMsg.BattleId = roomId;
        stateMsg.State = (int)battle.State;
        stateMsg.PlayerIds.AddRange(playerIds);
        MapMessageHelper.SendToClient(unit, stateMsg);

        // 8. 响应客户端
        response.battleId = roomId;
        response.Error = ErrorCode.ERR_Success;
        response.Message = "战斗开始";

        Log.Info($"玩家 {unit.Id} 开始战斗: RoomId={roomId}");
    }
}
```

### 4.2 多人组队战斗

#### 4.2.1 创建队伍

```csharp
// 队伍组件
[ComponentOf(typeof(Scene))]
public class TeamComponent : Entity
{
    public long TeamId;
    public HashSet<long> MemberIds;
    public long LeaderId;
}

// 创建队伍请求
message C2M_CreateTeam {
}

message M2C_CreateTeam {
    int64 teamId = 1;
}
```

#### 4.2.2 队长开启战斗

```protobuf
// 队长请求开启战斗
message C2M_TeamStartBattle {
    int64 teamId = 1;
    int32 battleType = 2;
    int32 totalWaves = 3;
}

message M2C_TeamStartBattle {
    int32 error = 1;
    string message = 2;
    int64 battleId = 3; // RoomId
    repeated int64 memberIds = 4;
}
```

#### 4.2.3 服务端处理

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_TeamStartBattleHandler : MessageLocationHandler<Unit, C2M_TeamStartBattle, M2C_TeamStartBattle>
{
    protected override async ETTask Run(Unit leader, C2M_TeamStartBattle request, M2C_TeamStartBattle response)
    {
        Scene mapScene = leader.Scene();

        // 1. 获取队伍
        TeamComponent team = mapScene.GetComponent<TeamComponent>().GetTeam(request.teamId);
        if (team.LeaderId != leader.Id)
        {
            response.Error = ErrorCode.ERR_NotTeamLeader;
            response.Message = "只有队长可以开启战斗";
            return;
        }

        // 2. 检查所有队员是否都在空闲状态
        BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
        foreach (long memberId in team.MemberIds)
        {
            if (roomManager.UnitIdToRoomId.ContainsKey(memberId))
            {
                response.Error = ErrorCode.ERR_TeamMemberInBattle;
                response.Message = $"队员 {memberId} 已在战斗中";
                return;
            }
        }

        // 3. 创建战斗房间
        long roomId = IdGenerater.Instance.GenerateId();
        BattleRoom battleRoom = mapScene.AddChildWithId<BattleRoom, long>(roomId);
        room.Fiber = mapScene.Fiber;
        room.SceneType = SceneType.Battle;
        room.RoomType = RoomType.WaveBattle;
        room.State = RoomState.Prepare;
        room.PlayerIds = new HashSet<long>(team.MemberIds);

        // 4. 在 BattleRoom 中创建战斗组件
        BattleComponent battle = room.AddComponent<BattleComponent, long>(roomId);
        battle.SetBattleType(BattleType.WaveBattle);
        battle.SetTotalWaves(request.totalWaves);

        // 5. 更新所有队员的映射
        foreach (long memberId in team.MemberIds)
        {
            roomManager.UnitIdToRoomId[memberId] = roomId;
        }
        roomManager.RoomIdToRoom[roomId] = room;

        // 6. 开始战斗
        await battle.StartBattle(team.MemberIds.ToList());

        // 7. 同步战斗状态给所有队员
        M2C_BattleStateSync stateMsg = M2C_BattleStateSync.Create();
        stateMsg.BattleId = roomId;
        stateMsg.State = (int)battle.State;
        stateMsg.PlayerIds.AddRange(team.MemberIds);
        
        foreach (long memberId in team.MemberIds)
        {
            Unit member = mapScene.GetComponent<UnitComponent>().Get(memberId);
            MapMessageHelper.SendToClient(member, stateMsg);
        }

        // 8. 通知所有队员战斗开始
        response.battleId = roomId;
        response.memberIds.AddRange(team.MemberIds);
        response.Error = ErrorCode.ERR_Success;

        foreach (long memberId in team.MemberIds)
        {
            Unit member = mapScene.GetComponent<UnitComponent>().Get(memberId);
            MapMessageHelper.SendToClient(member, response);
        }

        Log.Info($"队伍 {request.teamId} 开启战斗: RoomId={roomId}, MemberCount={team.MemberIds.Count}");
    }
}
```

#### 4.2.4 队员加入战斗（战斗中途）

```protobuf
// 队员加入战斗
message C2M_JoinTeamBattle {
    int64 battleId = 1; // RoomId
}

message M2C_JoinTeamBattle {
    int32 error = 1;
    string message = 2;
    repeated int64 memberIds = 3; // 当前所有成员
}
```

```csharp
[MessageLocationHandler(SceneType.Map)]
public class C2M_JoinTeamBattleHandler : MessageLocationHandler<Unit, C2M_JoinTeamBattle, M2C_JoinTeamBattle>
{
    protected override async ETTask Run(Unit unit, C2M_JoinTeamBattle request, M2C_JoinTeamBattle response)
    {
        Scene mapScene = unit.Scene();
        BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();

        // 1. 获取房间
        if (!roomManager.RoomIdToRoom.TryGetValue(request.battleId, out BattleRoom battleRoom))
        {
            response.Error = ErrorCode.ERR_BattleNotFound;
            response.Message = "战斗不存在";
            return;
        }

        // 2. 检查房间状态
        if (room.State != RoomState.Fighting)
        {
            response.Error = ErrorCode.ERR_BattleNotActive;
            response.Message = "战斗未进行中";
            return;
        }

        // 3. 加入房间
        room.PlayerIds.Add(unit.Id);
        roomManager.UnitIdToRoomId[unit.Id] = request.battleId;

        // 4. 同步当前战斗状态给新加入的玩家
        BattleComponent battle = room.GetComponent<BattleComponent>();
        M2C_BattleStateSync stateMsg = M2C_BattleStateSync.Create();
        stateMsg.BattleId = request.battleId;
        stateMsg.State = (int)battle.State;
        stateMsg.PlayerIds.AddRange(room.PlayerIds);
        MapMessageHelper.SendToClient(unit, stateMsg);

        // 5. 通知房间内所有玩家（包括新加入的）
        response.memberIds.AddRange(room.PlayerIds);
        response.Error = ErrorCode.ERR_Success;

        RoomMessageHelper.BroadcastToRoom(room, response);

        Log.Info($"玩家 {unit.Id} 加入战斗: RoomId={request.battleId}");
    }
}
```

### 4.3 战斗结束

```csharp
// BattleComponent.EndBattle() 方法
public static async ETTask EndBattle(this BattleComponent self, bool success)
{
    BattleRoom battleRoom = self.GetParent<BattleRoom>();
    room.State = RoomState.Ended;
    self.SetState(success ? BattleState.Success : BattleState.Failed);
    self.EndTime = TimeInfo.Instance.ServerFrameTime();

    // 计算奖励
    foreach (long playerId in self.GetPlayerIds())
    {
        // 结算逻辑...
    }

    // 构造战斗结束消息
    M2C_BattleEnd msg = M2C_BattleEnd.Create();
    msg.battleId = self.GetBattleId();
    msg.success = success;
    msg.duration = (self.EndTime - self.StartTime) / 1000;
    
    // 广播给所有玩家
    RoomMessageHelper.BroadcastToRoom(room, msg);

    // 触发战斗结束事件
    await room.PublishAsync(new BattleStateChangeEvent
    {
        Battle = self,
        OldState = BattleState.Fighting,
        NewState = self.State
    });

    // 延迟销毁房间（给客户端时间接收消息）
    await TimerComponent.Instance.WaitAsync(5000);

    // 清理映射
    Scene mapScene = room.Scene();
    BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
    foreach (long playerId in self.GetPlayerIds())
    {
        roomManager.UnitIdToRoomId.Remove(playerId);
    }
    roomManager.RoomIdToRoom.Remove(room.Id);

    // 销毁房间（自动销毁所有子组件）
    room.Dispose();
}
```

---

## 5. 状态同步机制（服务器权威 + 事件驱动）

### 5.1 同步架构概述

#### 5.1.1 核心原则

```
客户端输入 → 服务器验证 → 服务器计算 → 状态变化事件 → 广播同步 → 客户端表现
```

**关键特性：**
1. **服务器权威**：所有战斗逻辑、伤害计算、状态变化在服务器执行
2. **事件驱动**：状态变化通过 ET 事件系统触发同步
3. **BattleRoom 隔离**：事件仅在 BattleRoom 内传播，不影响其他战斗
4. **客户端预表现**：客户端可以做预表现，但以服务器状态为准

#### 5.1.2 同步层次

```
┌─────────────────────────────────────────┐
│  客户端层（表现层）                        │
│  - 接收状态更新                           │
│  - 播放动画/特效                          │
│  - 可选：客户端预测（需回滚）              │
└─────────────────────────────────────────┘
                    ↑
                    │ M2C_StateSync
                    │
┌─────────────────────────────────────────┐
│  服务器 BattleRoom 层（逻辑层）                  │
│  - 接收客户端输入                         │
│  - 验证合法性                             │
│  - 执行战斗逻辑                           │
│  - 触发状态变化事件                       │
│  - 广播状态给 BattleRoom 内所有玩家              │
└─────────────────────────────────────────┘
```

### 5.2 BattleRoom 内消息广播

#### 5.2.1 广播工具方法

```csharp
// MapMessageHelper 扩展方法
public static class RoomMessageHelper
{
    /// <summary>
    /// 广播消息给 BattleRoom 内所有玩家
    /// </summary>
    public static void BroadcastToBattleRoom(BattleRoom battleRoom, IMessage message)
    {
        Scene mapScene = room.Scene();
        UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
        
        foreach (long playerId in room.PlayerIds)
        {
            Unit player = unitComponent.Get(playerId);
            if (player != null)
            {
                MapMessageHelper.SendToClient(player, message);
            }
        }
    }

    /// <summary>
    /// 广播消息给 BattleRoom 内除指定玩家外的所有玩家
    /// </summary>
    public static void BroadcastToBattleRoomExcept(BattleRoom battleRoom, long exceptPlayerId, IMessage message)
    {
        Scene mapScene = room.Scene();
        UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
        
        foreach (long playerId in room.PlayerIds)
        {
            if (playerId == exceptPlayerId) continue;
            
            Unit player = unitComponent.Get(playerId);
            if (player != null)
            {
                MapMessageHelper.SendToClient(player, message);
            }
        }
    }
}
```

### 5.3 事件驱动状态同步

#### 5.3.1 战斗状态变化事件

```csharp
// 定义状态变化事件
namespace ET.Server
{
    // 单位受到伤害事件
    public struct UnitDamageEvent
    {
        public Unit Attacker;
        public Unit Target;
        public int Damage;
        public bool IsCrit;
        public DamageType DamageType;
    }

    // 单位死亡事件
    public struct UnitDeadEvent
    {
        public Unit Unit;
        public Unit Killer;
    }

    // 技能释放事件
    public struct SkillCastEvent
    {
        public Unit Caster;
        public int SkillId;
        public long TargetId;
        public Vector3 TargetPosition;
    }

    // Buff 添加/移除事件
    public struct BuffChangeEvent
    {
        public Unit Unit;
        public int BuffId;
        public bool IsAdd; // true=添加, false=移除
    }

    // 战斗状态变化事件
    public struct BattleStateChangeEvent
    {
        public BattleComponent Battle;
        public BattleState OldState;
        public BattleState NewState;
    }
}
```

#### 5.3.2 事件监听与广播

```csharp
// 伤害事件 → 广播给 BattleRoom
[Event(SceneType.Battle)]
public class UnitDamageEvent_SyncToRoom : AEvent<Scene, UnitDamageEvent>
{
    protected override async ETTask Run(Scene scene, UnitDamageEvent args)
    {
        BattleRoom battleRoom = scene as BattleRoom;
        if (room == null) return;

        // 构造同步消息
        M2C_UnitDamage msg = M2C_UnitDamage.Create();
        msg.AttackerId = args.Attacker.Id;
        msg.TargetId = args.Target.Id;
        msg.Damage = args.Damage;
        msg.IsCrit = args.IsCrit;
        msg.DamageType = (int)args.DamageType;
        msg.CurrentHp = args.Target.GetComponent<NumericComponent>().GetAsInt(NumericType.Hp);
        msg.MaxHp = args.Target.GetComponent<NumericComponent>().GetAsInt(NumericType.MaxHp);

        // 广播给 BattleRoom 内所有玩家
        RoomMessageHelper.BroadcastToRoom(room, msg);

        Log.Debug($"[BattleRoom {battleRoom.Id}] 伤害同步: {args.Attacker.Id} → {args.Target.Id}, 伤害={args.Damage}");
        
        await ETTask.CompletedTask;
    }
}

// 死亡事件 → 广播给 BattleRoom
[Event(SceneType.Battle)]
public class UnitDeadEvent_SyncToRoom : AEvent<Scene, UnitDeadEvent>
{
    protected override async ETTask Run(Scene scene, UnitDeadEvent args)
    {
        BattleRoom battleRoom = scene as BattleRoom;
        if (room == null) return;

        M2C_UnitDead msg = M2C_UnitDead.Create();
        msg.UnitId = args.Unit.Id;
        msg.KillerId = args.Killer?.Id ?? 0;

        RoomMessageHelper.BroadcastToRoom(room, msg);

        Log.Info($"[BattleRoom {battleRoom.Id}] 单位死亡: {args.Unit.Id}, 击杀者={msg.KillerId}");
        
        await ETTask.CompletedTask;
    }
}

// 技能释放事件 → 广播给 BattleRoom
[Event(SceneType.Battle)]
public class SkillCastEvent_SyncToRoom : AEvent<Scene, SkillCastEvent>
{
    protected override async ETTask Run(Scene scene, SkillCastEvent args)
    {
        BattleRoom battleRoom = scene as BattleRoom;
        if (room == null) return;

        M2C_SkillCast msg = M2C_SkillCast.Create();
        msg.CasterId = args.Caster.Id;
        msg.SkillId = args.SkillId;
        msg.TargetId = args.TargetId;
        msg.TargetPosition = args.TargetPosition;

        RoomMessageHelper.BroadcastToRoom(room, msg);

        Log.Debug($"[BattleRoom {battleRoom.Id}] 技能释放: {args.Caster.Id} 使用技能 {args.SkillId}");
        
        await ETTask.CompletedTask;
    }
}

// Buff 变化事件 → 广播给 BattleRoom
[Event(SceneType.Battle)]
public class BuffChangeEvent_SyncToRoom : AEvent<Scene, BuffChangeEvent>
{
    protected override async ETTask Run(Scene scene, BuffChangeEvent args)
    {
        BattleRoom battleRoom = scene as BattleRoom;
        if (room == null) return;

        M2C_BuffChange msg = M2C_BuffChange.Create();
        msg.UnitId = args.Unit.Id;
        msg.BuffId = args.BuffId;
        msg.IsAdd = args.IsAdd;

        RoomMessageHelper.BroadcastToRoom(room, msg);

        Log.Debug($"[BattleRoom {battleRoom.Id}] Buff变化: {args.Unit.Id} {(args.IsAdd ? "添加" : "移除")} Buff {args.BuffId}");
        
        await ETTask.CompletedTask;
    }
}
```

### 5.4 客户端输入处理（服务器验证）

#### 5.4.1 技能释放流程

```csharp
// 客户端请求释放技能
[MessageLocationHandler(SceneType.Map)]
public class C2M_CastSkillHandler : MessageLocationHandler<Unit, C2M_CastSkill, M2C_CastSkill>
{
    protected override async ETTask Run(Unit unit, C2M_CastSkill request, M2C_CastSkill response)
    {
        // 1. 获取玩家所在的 BattleRoom
        BattleRoomManagerComponent roomManager = unit.Scene().GetComponent<BattleRoomManagerComponent>();
        if (!roomManager.UnitIdToRoomId.TryGetValue(unit.Id, out long roomId))
        {
            response.Error = ErrorCode.ERR_NotInBattle;
            response.Message = "玩家不在战斗中";
            return;
        }

        BattleRoom battleRoom = roomManager.RoomIdToRoom[roomId];
        BattleComponent battle = room.GetComponent<BattleComponent>();

        // 2. 验证战斗状态
        if (battle.State != BattleState.Fighting)
        {
            response.Error = ErrorCode.ERR_BattleNotActive;
            response.Message = "战斗未进行中";
            return;
        }

        // 3. 验证技能合法性
        SkillComponent skillComp = unit.GetComponent<SkillComponent>();
        if (!skillComp.CanCastSkill(request.SkillId))
        {
            response.Error = ErrorCode.ERR_SkillCannotCast;
            response.Message = "技能无法释放";
            return;
        }

        // 4. 服务器执行技能逻辑
        bool success = await skillComp.CastSkill(request.SkillId, request.TargetId, request.TargetPosition);
        
        if (!success)
        {
            response.Error = ErrorCode.ERR_SkillCastFailed;
            response.Message = "技能释放失败";
            return;
        }

        // 5. 触发技能释放事件（自动广播给 BattleRoom）
        room.PublishAsync(new SkillCastEvent
        {
            Caster = unit,
            SkillId = request.SkillId,
            TargetId = request.TargetId,
            TargetPosition = request.TargetPosition
        }).Coroutine();

        // 6. 响应客户端
        response.Error = ErrorCode.ERR_Success;
        
        Log.Debug($"玩家 {unit.Id} 释放技能 {request.SkillId}");
    }
}
```

#### 5.4.2 移动同步流程

```csharp
// 客户端上报移动
[MessageLocationHandler(SceneType.Map)]
public class C2M_MoveHandler : MessageLocationHandler<Unit, C2M_Move, M2C_Move>
{
    protected override async ETTask Run(Unit unit, C2M_Move request, M2C_Move response)
    {
        // 1. 获取玩家所在的 BattleRoom
        BattleRoomManagerComponent roomManager = unit.Scene().GetComponent<BattleRoomManagerComponent>();
        if (!roomManager.UnitIdToRoomId.TryGetValue(unit.Id, out long roomId))
        {
            response.Error = ErrorCode.ERR_NotInBattle;
            return;
        }

        BattleRoom battleRoom = roomManager.RoomIdToRoom[roomId];

        // 2. 服务器验证移动合法性
        Vector3 oldPos = unit.Position;
        Vector3 newPos = request.Position;
        
        // 简单的速度检查（防作弊）
        float distance = Vector3.Distance(oldPos, newPos);
        float maxDistance = unit.GetComponent<NumericComponent>().GetAsFloat(NumericType.Speed) * 0.1f; // 假设100ms一次
        
        if (distance > maxDistance * 1.2f) // 允许20%误差
        {
            response.Error = ErrorCode.ERR_MoveSpeedTooFast;
            response.Message = "移动速度异常";
            return;
        }

        // 3. 更新服务器位置
        unit.Position = newPos;
        unit.Rotation = request.Rotation;

        // 4. 广播给 BattleRoom 内其他玩家（不包括自己）
        M2C_UnitMove msg = M2C_UnitMove.Create();
        msg.UnitId = unit.Id;
        msg.Position = newPos;
        msg.Rotation = request.Rotation;
        
        RoomMessageHelper.BroadcastToRoomExcept(room, unit.Id, msg);

        response.Error = ErrorCode.ERR_Success;
        
        await ETTask.CompletedTask;
    }
}
```

### 5.5 客户端状态同步处理

#### 5.5.1 伤害同步

```csharp
// 客户端接收伤害消息
[MessageHandler(SceneType.Demo)]
public class M2C_UnitDamageHandler : MessageHandler<Scene, M2C_UnitDamage>
{
    protected override async ETTask Run(Scene scene, M2C_UnitDamage message)
    {
        // 1. 获取攻击者和目标
        Unit attacker = scene.GetComponent<UnitComponent>().Get(message.AttackerId);
        Unit target = scene.GetComponent<UnitComponent>().Get(message.TargetId);

        if (target == null)
        {
            Log.Warning($"目标单位不存在: {message.TargetId}");
            return;
        }

        // 2. 更新目标血量（客户端同步）
        NumericComponent numeric = target.GetComponent<NumericComponent>();
        numeric.Set(NumericType.Hp, message.CurrentHp);

        // 3. 播放伤害表现
        // - 伤害数字飘字
        // - 受击动画
        // - 受击特效
        // - 暴击特效（如果是暴击）
        
        Log.Info($"收到伤害同步: {message.AttackerId} → {message.TargetId}, 伤害={message.Damage}, 暴击={message.IsCrit}");
        
        await ETTask.CompletedTask;
    }
}
```

#### 5.5.2 技能同步

```csharp
// 客户端接收技能释放消息
[MessageHandler(SceneType.Demo)]
public class M2C_SkillCastHandler : MessageHandler<Scene, M2C_SkillCast>
{
    protected override async ETTask Run(Scene scene, M2C_SkillCast message)
    {
        // 1. 获取释放者
        Unit caster = scene.GetComponent<UnitComponent>().Get(message.CasterId);
        if (caster == null) return;

        // 2. 播放技能表现
        // - 技能动画
        // - 技能特效
        // - 技能音效
        // - 弹道/投射物
        
        Log.Info($"收到技能同步: {message.CasterId} 释放技能 {message.SkillId}");
        
        await ETTask.CompletedTask;
    }
}
```

#### 5.5.3 单位移动同步

```csharp
// 客户端接收其他玩家移动消息
[MessageHandler(SceneType.Demo)]
public class M2C_UnitMoveHandler : MessageHandler<Scene, M2C_UnitMove>
{
    protected override async ETTask Run(Scene scene, M2C_UnitMove message)
    {
        Unit unit = scene.GetComponent<UnitComponent>().Get(message.UnitId);
        if (unit == null) return;

        // 更新位置（可以做插值平滑）
        unit.Position = message.Position;
        unit.Rotation = message.Rotation;

        // 触发移动表现（动画、特效等）
        
        await ETTask.CompletedTask;
    }
}
```

### 5.6 定时全量状态同步（可选）

```csharp
// 定时广播 BattleRoom 内所有单位的完整状态（防止状态不一致）
[Event(SceneType.Battle)]
public class RoomUpdate_SyncFullState : AEvent<Scene, RoomUpdateEvent>
{
    private long lastSyncTime = 0;
    private const long SyncInterval = 5000; // 5秒同步一次

    protected override async ETTask Run(Scene scene, RoomUpdateEvent args)
    {
        BattleRoom battleRoom = scene as BattleRoom;
        if (room == null) return;

        long now = TimeInfo.Instance.ServerFrameTime();
        if (now - lastSyncTime < SyncInterval) return;

        lastSyncTime = now;

        // 收集所有单位状态
        M2C_RoomFullState msg = M2C_RoomFullState.Create();
        
        Scene mapScene = room.Scene();
        UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
        
        foreach (long playerId in room.PlayerIds)
        {
            Unit unit = unitComponent.Get(playerId);
            if (unit == null) continue;

            UnitState state = new UnitState
            {
                UnitId = unit.Id,
                Position = unit.Position,
                Rotation = unit.Rotation,
                Hp = unit.GetComponent<NumericComponent>().GetAsInt(NumericType.Hp),
                MaxHp = unit.GetComponent<NumericComponent>().GetAsInt(NumericType.MaxHp),
                // ... 其他状态
            };
            
            msg.UnitStates.Add(state);
        }

        // 广播完整状态
        RoomMessageHelper.BroadcastToRoom(room, msg);

        await ETTask.CompletedTask;
    }
}
```

### 5.7 状态同步消息定义

```protobuf
// 伤害同步
message M2C_UnitDamage {
    int64 AttackerId = 1;
    int64 TargetId = 2;
    int32 Damage = 3;
    bool IsCrit = 4;
    int32 DamageType = 5;
    int32 CurrentHp = 6;
    int32 MaxHp = 7;
}

// 单位死亡
message M2C_UnitDead {
    int64 UnitId = 1;
    int64 KillerId = 2;
}

// 技能释放
message M2C_SkillCast {
    int64 CasterId = 1;
    int32 SkillId = 2;
    int64 TargetId = 3;
    Vector3 TargetPosition = 4;
}

// Buff 变化
message M2C_BuffChange {
    int64 UnitId = 1;
    int32 BuffId = 2;
    bool IsAdd = 3;
}

// 单位移动
message M2C_UnitMove {
    int64 UnitId = 1;
    Vector3 Position = 2;
    Vector3 Rotation = 3;
}

// 完整状态同步（可选）
message M2C_RoomFullState {
    repeated UnitState UnitStates = 1;
}

message UnitState {
    int64 UnitId = 1;
    Vector3 Position = 2;
    Vector3 Rotation = 3;
    int32 Hp = 4;
    int32 MaxHp = 5;
    repeated int32 BuffIds = 6;
}
```

---

## 6. 战斗生命周期管理

### 6.1 生命周期状态机

```
None → Prepare → Fighting → Ended
                 ↓  ↓
               Paused
```

### 6.2 房间销毁时机

1. **战斗结束后延迟销毁**（5秒）
2. **所有玩家离开房间**
3. **服务器关闭**
4. **战斗超时**

```csharp
// 检查房间是否为空
public static bool IsEmpty(this BattleRoom battleRoom)
{
    return room.PlayerIds.Count == 0;
}

// 定时清理空房间
[Event(SceneType.Map)]
public class TimerEvent_CheckEmptyRooms : AEvent<Scene, TimerEvent>
{
    protected override async ETTask Run(Scene scene, TimerEvent args)
    {
        BattleRoomManagerComponent roomManager = scene.GetComponent<BattleRoomManagerComponent>();
        if (roomManager == null) return;

        List<long> emptyRooms = new List<long>();
        foreach (var kv in roomManager.RoomIdToRoom)
        {
            if (kv.Value.IsEmpty())
            {
                emptyRooms.Add(kv.Key);
            }
        }

        foreach (long roomId in emptyRooms)
        {
            BattleRoom battleRoom = roomManager.RoomIdToRoom[roomId];
            room.Dispose();
            roomManager.RoomIdToRoom.Remove(roomId);
            Log.Info($"清理空房间: RoomId={roomId}");
        }
    }
}
```

---

## 7. 文件结构

### 7.1 新增文件

```
Unity/Assets/Scripts/
├── Model/
│   ├── Share/
│   │   ├── BattleRoom/
│   │   │   ├── RoomType.cs (新增枚举)
│   │   │   └── RoomState.cs (新增枚举)
│   │   └── Demo/
│   │       └── Battle/
│   │           ├── BattleRoomManagerComponent.cs (新增)
│   │           └── BattleComponent.cs (调整 ComponentOf)
│   └── Server/
│       └── Demo/
│           └── Battle/
│               ├── BattleRoomManagerComponentSystem.cs (新增)
│               └── Handler/
│                   ├── C2M_StartBattleHandler.cs (调整)
│                   ├── C2M_TeamStartBattleHandler.cs (新增)
│                   ├── C2M_JoinTeamBattleHandler.cs (新增)
│                   └── C2M_ExitBattleHandler.cs (新增)
└── Hotfix/
    └── Server/
        └── Demo/
            └── Battle/
                └── Handler/
                    ├── C2M_StartBattleHandler.cs (调整实现)
                    ├── C2M_TeamStartBattleHandler.cs (新增)
                    └── C2M_JoinTeamBattleHandler.cs (新增)
```

### 7.2 修改文件

- `Model/Share/Demo/Battle/BattleComponent.cs`：修改 ComponentOf
- `Hotfix/Server/Demo/Battle/Handler/C2M_StartBattleHandler.cs`：调整实现，添加状态同步
- `Hotfix/Server/Demo/Battle/BattleComponentSystem.cs`：调整为事件驱动的状态同步
- `Hotfix/Server/Demo/Battle/Event/`：新增各类战斗事件监听器（伤害、死亡、技能、Buff等）
- `Hotfix/Client/Demo/Battle/Handler/`：新增客户端状态同步消息处理器

---

## 8. 优势总结

### 8.1 相比当前设计的优势

| 特性 | 当前设计 | BattleRoom + 状态同步设计 |
|------|---------|-----------|
| 多战斗实例 | ❌ 同一 Scene 只能有一个 | ✅ 支持多个独立 BattleRoom |
| 组队支持 | ⚠️ 需要手动管理 | ✅ BattleRoom 自动管理 |
| 逻辑隔离 | ❌ 共享 Scene 状态 | ✅ BattleRoom 独立逻辑域 |
| 事件系统 | ⚠️ 需要手动过滤 | ✅ BattleRoom 自动隔离 |
| 状态同步 | ⚠️ 手动广播消息 | ✅ 事件驱动自动同步 |
| 服务器权威 | ⚠️ 需要手动验证 | ✅ 统一验证流程 |
| 防作弊 | ⚠️ 分散的验证逻辑 | ✅ 服务器权威计算 |
| 生命周期 | ⚠️ 手动管理组件 | ✅ BattleRoom.Dispose() 自动清理 |
| 扩展性 | ❌ 难以扩展 | ✅ 易于添加新房间类型 |

### 8.2 关键优势

1. **独立逻辑域**：每个 BattleRoom 是独立的虚拟能力场景
2. **场景继承链**：BattleRoom 内实体的 IScene 指向 BattleRoom
3. **事件隔离**：事件发布到 BattleRoom 只影响 BattleRoom 内实体
4. **服务器权威**：所有战斗逻辑在服务器计算，客户端仅做表现
5. **事件驱动同步**：状态变化自动触发事件并广播给 BattleRoom 内所有玩家
6. **防作弊机制**：服务器验证所有客户端输入（移动、技能、伤害等）
7. **组件化管理**：利用 ET 的 ECS 架构
8. **易于扩展**：可以轻松添加新的房间类型（副本、竞技场等）

### 8.3 状态同步优势

1. **一致性保证**：服务器权威确保所有客户端状态一致
2. **实时性**：事件驱动的增量同步，减少网络开销
3. **可靠性**：可选的定时全量同步作为兜底机制
4. **可扩展性**：易于添加新的状态同步事件
5. **调试友好**：事件系统便于追踪状态变化

---

## 9. 后续扩展

### 9.1 副本系统

```csharp
[ComponentOf(typeof(BattleRoom))]
public class DungeonComponent : Entity
{
    public int DungeonId;
    public int Difficulty;
    public DungeonProgress Progress;
}
```

### 9.2 PvP 竞技场

```csharp
[ComponentOf(typeof(BattleRoom))]
public class PvPComponent : Entity
{
    public long TeamA_Id;
    public long TeamB_Id;
    public int ScoreA;
    public int ScoreB;
}
```

### 9.3 观战系统

```csharp
[ComponentOf(typeof(BattleRoom))]
public class SpectatorComponent : Entity
{
    public HashSet<long> SpectatorIds;
}
```

---

## 10. 总结

基于 ET 框架的 BattleRoom（虚拟能力场景）+ 服务器权威状态同步设计，我们实现了一个支持多人组队、多战斗实例的战斗系统。核心思想是：

1. **BattleRoom = Entity + IScene**：既是实体，又是虚拟能力场景
2. **场景继承链**：BattleRoom 内实体的 IScene 指向 BattleRoom，实现逻辑隔离
3. **战斗房间管理器**：统一管理所有 BattleRoom 和玩家映射
4. **组件化设计**：BattleComponent、WaveManagerComponent 等作为 BattleRoom 的子组件
5. **服务器权威**：所有战斗逻辑在服务器计算，客户端仅做表现
6. **事件驱动同步**：状态变化通过 ET 事件系统自动触发并广播给 BattleRoom 内所有玩家
7. **防作弊机制**：服务器验证所有客户端输入（移动速度、技能CD、伤害计算等）

### 10.1 状态同步流程总结

```
客户端操作 → 发送请求 → 服务器验证 → 服务器计算 → 触发事件 → 广播状态 → 客户端表现
    ↓                                                              ↓
 预表现（可选）                                              同步真实状态
```

### 10.2 关键技术点

- **事件驱动**：利用 ET 的事件系统实现状态变化的自动同步
- **BattleBattleRoom 隔离**：事件仅在 BattleBattleRoom 内传播，不同战斗互不影响
- **增量同步**：只同步变化的状态，减少网络开销
- **全量兜底**：可选的定时全量同步防止状态不一致
- **服务器验证**：所有客户端输入都经过服务器验证，防止作弊

这个设计完美解决了当前 BattleComponent 挂在 Map Scene 上的限制，支持多个独立战斗实例，并通过服务器权威的状态同步机制确保战斗的公平性和一致性，为未来的多人组队、副本、PvP 等功能打下坚实基础。

---

**文档版本**: v2.0
**创建日期**: 2026-01-20
**更新日期**: 2026-03-04
**适用ET版本**: ET 8.1+
**作者**: Droid
**更新内容**: 
- 添加服务器权威状态同步机制设计
- 将 Room 重命名为 BattleRoom，与现有代码保持一致
