# ET框架网络消息协议转发流程图

## 整体架构图

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          ET框架网络消息协议转发流程图                              │
└─────────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────────┐
│                                   客户端层                                        │
├──────────────────────────────────────────────────────────────────────────────────┤
│  Unity WebGL / PC Client                                                         │
│  ┌────────────────────────────────────────────────────────────────────┐          │
│  │  Main Fiber (SchedulerType.Main)                                   │          │
│  │  ┌──────────────────────────────────────────────────────────┐      │          │
│  │  │ Demo Scene (SceneType.Demo)                             │      │          │
│  │  │  ├─ UIComponent (登录界面/游戏UI)                        │      │          │
│  │  │  ├─ ClientSenderComponent (消息发送入口)                 │      │          │
│  │  │  └─ EventSystem (事件系统)                              │      │          │
│  │  └──────────────────────────────────────────────────────────┘      │          │
│  │                        │                                            │          │
│  │                        │ Actor消息 (ProcessInnerSender)             │          │
│  │                        ▼                                            │          │
│  │  ┌──────────────────────────────────────────────────────────┐      │          │
│  │  │ NetClient Fiber (SchedulerType.ThreadPool)              │      │          │
│  │  │ ┌────────────────────────────────────────────────────┐   │      │          │
│  │  │ │ NetClient Scene (SceneType.NetClient)              │   │      │          │
│  │  │ │  ├─ NetComponent (网络组件)                         │   │      │          │
│  │  │ │  ├─ SessionComponent (Session管理)                  │   │      │          │
│  │  │ │  │   └─ Session (TCP/WebSocket连接)                │   │      │          │
│  │  │ │  ├─ MailBoxComponent (接收Actor消息)                │   │      │          │
│  │  │ │  └─ ProcessInnerSender (进程内通信)                 │   │      │          │
│  │  │ └────────────────────────────────────────────────────┘   │      │          │
│  │  └──────────────────────────────────────────────────────────┘      │          │
│  └────────────────────────────────────────────────────────────────────┘          │
│                                    │                                              │
│                                    │ TCP/WebSocket                                │
└────────────────────────────────────┼──────────────────────────────────────────────┘
                                     │
                    ═════════════════╪═══════════════════
                         Internet / LAN 网络传输
                    ═════════════════╪═══════════════════
                                     │
┌────────────────────────────────────┼──────────────────────────────────────────────┐
│                          服务端 - 网络接入层                                       │
├────────────────────────────────────┼──────────────────────────────────────────────┤
│                                    ▼                                              │
│  ┌─────────────────────────────────────────────────────────────┐                 │
│  │ Router (可选 - DDoS防护层)                                   │                 │
│  │  - 软路由转发                                                │                 │
│  │  - 限流保护                                                  │                 │
│  │  - 地址分发 (HTTP API: /get_router)                         │                 │
│  └─────────────────────────────────────────────────────────────┘                 │
│                                    │                                              │
│                                    ▼                                              │
│  ┌─────────────────────────────────────────────────────────────┐                 │
│  │ Realm Fiber (认证层)                                         │                 │
│  │ ┌─────────────────────────────────────────────────────┐     │                 │
│  │ │ Realm Scene (SceneType.Realm)                       │     │                 │
│  │ │  ├─ Session (接收客户端连接)                         │     │                 │
│  │ │  └─ Handlers:                                       │     │                 │
│  │ │      └─ C2R_LoginAccount (账号验证)                  │     │                 │
│  │ │          ├─ 验证账号密码                             │     │                 │
│  │ │          ├─ 创建角色/加载角色列表                     │     │                 │
│  │ │          └─ 分配Gate地址 → R2C_LoginAccount          │     │                 │
│  │ └─────────────────────────────────────────────────────┘     │                 │
│  └─────────────────────────────────────────────────────────────┘                 │
│                                    │                                              │
│                Client切换连接到Gate │                                              │
└────────────────────────────────────┼──────────────────────────────────────────────┘
                                     │
┌────────────────────────────────────┼──────────────────────────────────────────────┐
│                          服务端 - 网关转发层                                       │
├────────────────────────────────────┼──────────────────────────────────────────────┤
│                                    ▼                                              │
│  ┌───────────────────────────────────────────────────────────────────────┐       │
│  │ Gate Fiber (网关层 - 消息路由中心)                                     │       │
│  │ ┌───────────────────────────────────────────────────────────────┐     │       │
│  │ │ Gate Scene (SceneType.Gate)                                   │     │       │
│  │ │  ├─ NetComponent (监听客户端连接)                              │     │       │
│  │ │  ├─ PlayerComponent (管理在线玩家)                             │     │       │
│  │ │  │   └─ Player Entity                                         │     │       │
│  │ │  │       ├─ PlayerSessionComponent (关联Session)              │     │       │
│  │ │  │       │   ├─ Session (客户端连接)                          │     │       │
│  │ │  │       │   └─ MailBoxComponent (GateSession类型)            │     │       │
│  │ │  │       ├─ MailBoxComponent (接收Actor消息)                  │     │       │
│  │ │  │       └─ Location注册 (LocationType.Player)                │     │       │
│  │ │  ├─ MessageLocationSenderComponent (Location消息发送器)        │     │       │
│  │ │  ├─ MessageSender (Actor消息发送器)                           │     │       │
│  │ │  └─ GateSessionKeyComponent (登录密钥验证)                    │     │       │
│  │ │                                                                │     │       │
│  │ │  核心转发逻辑: NetOnReadInvoker_Gate                          │     │       │
│  │ │  ┌──────────────────────────────────────────────────────┐    │     │       │
│  │ │  │ switch (message接口类型)                             │    │     │       │
│  │ │  │                                                       │    │     │       │
│  │ │  │ ① ISessionMessage                                    │    │     │       │
│  │ │  │    → Gate本地处理                                    │    │     │       │
│  │ │  │    → C2G_LoginGameGate, C2G_Ping                     │    │     │       │
│  │ │  │                                                       │    │     │       │
│  │ │  │ ② ILocationRequest/ILocationMessage                  │    │     │       │
│  │ │  │    → Location转发到Map (Unit)                        │    │     │       │
│  │ │  │    → C2M_AttackTarget, C2M_Move                      │    │     │       │
│  │ │  │                                                       │    │     │       │
│  │ │  │ ③ IRoomMessage/FrameMessage                          │    │     │       │
│  │ │  │    → 转发到Room Scene (帧同步)                       │    │     │       │
│  │ │  │                                                       │    │     │       │
│  │ │  │ ④ IMailInfoRequest/IMailInfoMessage                  │    │     │       │
│  │ │  │    → Location转发到Mail Scene                        │    │     │       │
│  │ │  │                                                       │    │     │       │
│  │ │  │ ⑤ IRankInfoRequest/IRankInfoMessage                  │    │     │       │
│  │ │  │    → 固定ActorId转发到Rank Scene                     │    │     │       │
│  │ │  └──────────────────────────────────────────────────────┘    │     │       │
│  │ └───────────────────────────────────────────────────────────────┘     │       │
│  └───────────────────────────────────────────────────────────────────────┘       │
│                       │           │           │           │                       │
│          ─────────────┴───────────┴───────────┴───────────┴─────────────          │
│          │            │           │           │           │            │          │
└──────────┼────────────┼───────────┼───────────┼───────────┼────────────┼──────────┘
           │            │           │           │           │            │
           │    Actor消息(通过MessageSender/LocationSender)  │            │
           │            │           │           │           │            │
┌──────────┼────────────┼───────────┼───────────┼───────────┼────────────┼──────────┐
│          │   服务端 - 业务逻辑层 (各个独立的Fiber)          │            │          │
├──────────┼────────────┼───────────┼───────────┼───────────┼────────────┼──────────┤
│          ▼            ▼           ▼           ▼           ▼            ▼          │
│  ┌───────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌─────────┐  │
│  │Map Fiber 1│ │Map Fiber2│ │Mail Fiber│ │Rank Fiber│ │Room Fiber│ │DB Fiber │  │
│  ├───────────┤ ├──────────┤ ├──────────┤ ├──────────┤ ├──────────┤ ├─────────┤  │
│  │Map Scene  │ │Map Scene │ │Mail Scene│ │Rank Scene│ │Room Scene│ │DB Scene │  │
│  │(Game地图) │ │(副本)    │ │(邮件系统)│ │(排行榜)  │ │(帧同步)  │ │(数据库) │  │
│  ├───────────┤ ├──────────┤ ├──────────┤ ├──────────┤ ├──────────┤ ├─────────┤  │
│  │组件:      │ │组件:     │ │组件:     │ │组件:     │ │组件:     │ │组件:    │  │
│  │├MailBox   │ │├MailBox  │ │├MailBox  │ │├MailBox  │ │├MailBox  │ │├MailBox │  │
│  │├Location  │ │├Location │ │├Location │ │├MessageS │ │├MessageS │ │├DBMgr   │  │
│  │├UnitComp  │ │├UnitComp │ │├MailUnit │ │├RankComp │ │├LockStep │ │└Cache   │  │
│  │├AOIComp   │ │├DungeonC │ │└MailComp │ │└RankData │ │└FrameSync│ │         │  │
│  │└CombatSys │ │└TeamPVE  │ │          │ │          │ │          │ │         │  │
│  ├───────────┤ ├──────────┤ ├──────────┤ ├──────────┤ ├──────────┤ ├─────────┤  │
│  │Handlers:  │ │Handlers: │ │Handlers: │ │Handlers: │ │Handlers: │ │Handlers:│  │
│  │C2M_Attack │ │M2M_Trans │ │Mail_Get  │ │Rank_Get  │ │Frame_One │ │DB_Query │  │
│  │C2M_Move   │ │Dungeon_  │ │Mail_Send │ │Rank_Upd  │ │Room_Oper │ │DB_Save  │  │
│  │M2M_Trans  │ │Create    │ │Mail_Del  │ │          │ │          │ │         │  │
│  └───────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ └─────────┘  │
│                                                                                   │
│  Location服务 (LocationProxyComponent)                                           │
│  ┌─────────────────────────────────────────────────────────────────┐             │
│  │ LocationType.Unit  → Map Fiber查找表  (unitId → MapActorId)     │             │
│  │ LocationType.Mail  → Mail Fiber查找表 (unitId → MailActorId)    │             │
│  │ LocationType.Player → Gate查找表      (playerId → GateActorId)  │             │
│  └─────────────────────────────────────────────────────────────────┘             │
└───────────────────────────────────────────────────────────────────────────────────┘
```

---

## Gate网关核心转发逻辑

Gate网关的消息转发核心代码位于：`Unity/Assets/Scripts/Hotfix/Server/Demo/Gate/NetOnReadInvoker_Gate.cs`

### 转发策略汇总

| 消息接口类型 | 转发目标 | 转发方式 | 示例消息 | 代码位置 |
|------------|---------|---------|---------|----------|
| **ISessionMessage** | Gate本地处理 | MessageSessionDispatcher | C2G_LoginGameGate, C2G_Ping | NetOnReadInvoker_Gate.cs:21-25 |
| **ILocationRequest** | Map Scene (Unit) | LocationSender.Call() | C2M_AttackTarget | NetOnReadInvoker_Gate.cs:48-61 |
| **ILocationMessage** | Map Scene (Unit) | LocationSender.Send() | C2M_Move | NetOnReadInvoker_Gate.cs:42-47 |
| **IRoomMessage** | Room Scene | MessageSender.Send() | 房间操作 | NetOnReadInvoker_Gate.cs:34-41 |
| **FrameMessage** | Room Scene | MessageSender.Send() | 帧同步数据 | NetOnReadInvoker_Gate.cs:26-33 |
| **IMailInfoRequest** | Mail Scene | LocationSender.Call() | 邮件请求 | NetOnReadInvoker_Gate.cs:82-95 |
| **IMailInfoMessage** | Mail Scene | LocationSender.Send() | 邮件操作 | NetOnReadInvoker_Gate.cs:96-102 |
| **IRankInfoRequest** | Rank Scene | MessageSender.Call() | 排行榜请求 | NetOnReadInvoker_Gate.cs:62-75 |
| **IRankInfoMessage** | Rank Scene | MessageSender.Send() | 排行榜更新 | NetOnReadInvoker_Gate.cs:76-81 |

### 关键代码片段

```csharp
// NetOnReadInvoker_Gate.cs
[Invoke((long)SceneType.Gate)]
public class NetComponentOnReadInvoker_Gate: AInvokeHandler<NetComponentOnRead>
{
    private async ETTask HandleAsync(NetComponentOnRead args)
    {
        Session session = args.Session;
        object message = args.Message;
        Scene root = args.Session.Root();

        // 根据消息接口判断转发目标
        switch (message)
        {
            // ① Gate本地处理
            case ISessionMessage:
            {
                MessageSessionDispatcher.Instance.Handle(session, message);
                break;
            }

            // ② 转发到Map (有响应)
            case ILocationRequest actorLocationRequest:
            {
                long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                int rpcId = actorLocationRequest.RpcId;

                // 调用Location服务找到Unit所在的Map并发送RPC
                IResponse iResponse = await root
                    .GetComponent<MessageLocationSenderComponent>()
                    .Get(LocationType.Unit)
                    .Call(unitId, actorLocationRequest);

                // 将响应返回给客户端
                iResponse.RpcId = rpcId;
                session.Send(iResponse);
                break;
            }

            // ③ 转发到Map (无响应)
            case ILocationMessage actorLocationMessage:
            {
                long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                root.GetComponent<MessageLocationSenderComponent>()
                    .Get(LocationType.Unit)
                    .Send(unitId, actorLocationMessage);
                break;
            }

            // 其他类型转发逻辑...
        }
    }
}
```

---

## 典型消息流程示例

### 流程1: 玩家登录

```
Client → NetClient Session → Realm Session
    C2R_LoginAccount (账号+密码)
                      ← R2C_LoginAccount (Gate地址+Key)

Client → 重连到Gate Session
    C2G_LoginGameGate (Key+账号)

Gate处理 (C2G_LoginGameGateHandler.cs):
    ├─ 验证Key (GateSessionKeyComponent.Get)
    ├─ 创建Player实体
    ├─ PlayerSessionComponent绑定Session
    ├─ 添加MailBoxComponent (MailBoxType.GateSession)
    ├─ 注册Location (LocationType.Player)
    └─ 返回 G2C_LoginGameGate (PlayerId)
```

**关键代码**：`Unity/Assets/Scripts/Hotfix/Server/Demo/Gate/Handler/C2G_LoginGameGateHandler.cs:59-70`

```csharp
// 创建Player实体
Player player = playerComponent.AddChildWithId<Player, string>(request.RoleId, account);

// 添加PlayerSessionComponent和MailBox
PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();
playerSessionComponent.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.GateSession);
await playerSessionComponent.AddLocation(LocationType.GateSession);

// 注册Player的Actor
player.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.UnOrderedMessage);
await player.AddLocation(LocationType.Player);

// 双向绑定Session和Player
session.AddComponent<SessionPlayerComponent>().Player = player;
playerSessionComponent.Session = session;
```

---

### 流程2: 进入地图

```
Client → Gate Session
    C2G_EnterMap

Gate处理 (C2G_EnterMapHandler.cs):
    ├─ 创建临时GateMap Scene
    ├─ 从DB加载Unit数据
    ├─ 调用TransferHelper传送到真实Map
    └─ 返回 G2C_EnterMap (MyId)

Gate → Map Scene (M2M_UnitTransferRequest)
    Map处理 (M2M_UnitTransferRequestHandler.cs):
        ├─ 反序列化Unit
        ├─ 添加MailBoxComponent (OrderedMessage)
        ├─ 添加AOI/Combat/Move等组件
        ├─ Location解锁 (LocationType.Unit)
        └─ 通知客户端 M2C_CreateMyUnit
```

**关键代码**：`Unity/Assets/Scripts/Hotfix/Server/Demo/Gate/C2G_EnterMapHandler.cs:10-23`

```csharp
// 在Gate上创建临时Map Scene
GateMapComponent gateMapComponent = player.AddComponent<GateMapComponent>();
gateMapComponent.Scene = await GateMapFactory.Create(gateMapComponent, player.Id, IdGenerater.Instance.GenerateInstanceId(), "GateMap");

Scene scene = gateMapComponent.Scene;

// 从DB加载Unit
Unit unit = await UnitFactory.Create(scene, player.Id, UnitType.Player);

// 传送到真实Map Scene
StartSceneConfig startSceneConfig = StartSceneConfigCategory.Instance.GetBySceneName(session.Zone(), "Game");
TransferHelper.TransferAtFrameFinish(unit, startSceneConfig.ActorId, startSceneConfig.Name).Coroutine();
```

---

### 流程3: 攻击怪物 (ILocationRequest)

```
Client → Gate Session
    C2M_AttackTarget { TargetId: 123456 }
    ↓
Gate转发逻辑 (NetOnReadInvoker_Gate.cs:48-61):
    long unitId = session.Player.Id;
    IResponse response = await LocationSender.Call(unitId, request);
    session.Send(response);
    ↓
Location服务查找: unitId → Map Scene ActorId
    ↓
Map Scene → C2M_AttackTargetHandler
    Unit unit = (Unit)entity;
    ├─ 验证目标是否在攻击范围
    ├─ 计算伤害
    ├─ CombatComponent.AttackWithDamage()
    ├─ 广播M2C_AttackTarget给周围玩家 (AOI)
    └─ 返回 M2C_AttackTarget (结果)
    ↓
Gate收到响应 ← Map Scene
    ↓
Client ← Gate Session
    M2C_AttackTarget { Damage: 80, TargetHp: 920 }
```

**详细流程图**：

```
┌────────┐  C2M_AttackTarget  ┌──────────┐  LocationSender.Call()  ┌─────────────┐
│ Client │ ──────────────────> │   Gate   │ ─────────────────────> │  Location   │
└────────┘                     │  Scene   │                        │   Service   │
                               └──────────┘                        └─────────────┘
                                                                           │
                                                                           │ 查找unitId
                                                                           │ 对应的MapActorId
                                                                           ▼
┌────────┐  M2C_AttackTarget  ┌──────────┐  IResponse response    ┌─────────────┐
│ Client │ <────────────────── │   Gate   │ <───────────────────── │  Map Scene  │
└────────┘                     │  Scene   │                        │  (Handler)  │
                               └──────────┘                        └─────────────┘
                                                                           │
                                                                           │ 业务逻辑处理
                                                                           │ - 验证距离
                                                                           │ - 计算伤害
                                                                           │ - 扣除血量
                                                                           │ - AOI广播
                                                                           ▼
```

---

### 流程4: Map主动推送 (如怪物进入视野)

```
Map Scene → UnitEnterSightRange事件
    M2C_CreateUnits message = ...
    MapMessageHelper.SendToClient(unit, message)
    ↓
发送到Player的Actor:
    MessageLocationSenderComponent.Send(unitId, message)
    ↓
Location服务: unitId → Gate上的Player ActorId
    ↓
Gate → Player → PlayerSessionComponent
    MailBoxType_GateSessionHandler:
        playerSessionComponent.Session.Send(message)
    ↓
Client ← Gate Session
    M2C_CreateUnits (怪物信息)
```

**关键代码**：`Unity/Assets/Scripts/Hotfix/Server/Demo/Gate/MailBoxType_GateSessionHandler.cs:13-16`

```csharp
[Invoke((long)MailBoxType.GateSession)]
public class MailBoxType_GateSessionHandler: AInvokeHandler<MailBoxInvoker>
{
    public override void Handle(MailBoxInvoker args)
    {
        MailBoxComponent mailBoxComponent = args.MailBoxComponent;
        IMessage messageObject = args.MessageObject;

        // 通过PlayerSessionComponent发送给客户端
        if (mailBoxComponent.Parent is PlayerSessionComponent playerSessionComponent)
        {
            playerSessionComponent.Session?.Send(messageObject);
        }
    }
}
```

---

### 流程5: 邮件请求 (IMailInfoRequest)

```
Client → Gate Session
    C2Mail_GetMailList
    ↓
Gate转发逻辑 (NetOnReadInvoker_Gate.cs:82-95):
    Player player = session.Player;
    IMailInfoResponse response = await LocationSender
        .Get(LocationType.Mail)
        .Call(player.UnitId, request);
    session.Send(response);
    ↓
Location服务查找: unitId → Mail Scene ActorId
    ↓
Mail Scene → Handler处理
    MailUnit mailUnit = ...
    MailComponent mails = mailUnit.GetComponent<MailComponent>();
    返回邮件列表
    ↓
Client ← Gate ← Mail Scene
    Mail2C_GetMailList { Mails: [...] }
```

---

## 关键设计特点

### 1. 位置透明 (Location Transparency)

Gate不需要知道Unit在哪个Map，Location服务自动定位：

```csharp
// Gate只需要知道unitId，Location服务会找到对应的Map ActorId
IResponse response = await root
    .GetComponent<MessageLocationSenderComponent>()
    .Get(LocationType.Unit)
    .Call(unitId, request);
```

**优势**：
- 玩家切换地图时，Gate无感知
- 支持动态负载均衡（多个Map Scene）
- 简化Gate层逻辑

---

### 2. 协议驱动路由 (Protocol-Driven Routing)

通过消息接口类型自动路由到不同的后端服务：

```csharp
switch (message)
{
    case ILocationRequest:  // 自动路由到Map
    case IMailInfoRequest:  // 自动路由到Mail
    case IRankInfoRequest:  // 自动路由到Rank
}
```

**优势**：
- 扩展新服务只需定义新接口
- 无需修改Gate核心代码
- 协议即文档，清晰易懂

---

### 3. Actor隔离 (Actor Isolation)

每个Scene是独立Actor，状态完全隔离：

```csharp
// Map Scene 1
Unit player1 = ...  // 玩家1的状态

// Map Scene 2
Unit player2 = ...  // 玩家2的状态

// 两个Scene完全隔离，不会互相干扰
```

**优势**：
- 天然线程安全（单Fiber内单线程）
- 避免状态冲突
- 支持分布式部署

---

### 4. 会话保持 (Session Management)

Player实体在Gate上持久存在，Session断开时触发事件：

```csharp
// Player在Gate上的结构
Player
├─ PlayerSessionComponent
│   ├─ Session (客户端连接)
│   └─ MailBoxComponent (GateSession)
└─ MailBoxComponent (UnOrderedMessage)
```

**会话断开处理**：`SessionDisconnectEvent` → 通知后端清理状态

---

### 5. RpcId机制 (RpcId Preservation)

Gate转发时保持客户端RpcId，确保响应能正确匹配请求：

```csharp
case ILocationRequest actorLocationRequest:
{
    int rpcId = actorLocationRequest.RpcId;  // 保存客户端RpcId

    IResponse iResponse = await LocationSender.Call(unitId, actorLocationRequest);

    iResponse.RpcId = rpcId;  // 恢复RpcId
    session.Send(iResponse);  // 返回给客户端
    break;
}
```

**为什么需要RpcId**：
- 客户端可能同时发送多个请求
- 响应可能乱序到达
- RpcId用于匹配请求和响应

---

### 6. 双向通信 (Bidirectional Communication)

支持客户端请求和服务端推送：

```
Client → Gate → Map  (请求)
Client ← Gate ← Map  (响应)
Client ← Gate ← Map  (主动推送)
```

**实现机制**：
- **请求响应**：通过RpcId匹配
- **主动推送**：通过GateSession MailBox反向路由

---

## Player与Session的绑定机制

### 双向绑定结构

```
┌─────────────────────────────────────────────┐
│            Gate Scene                       │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │ Session (客户端TCP连接)              │   │
│  │  └─ SessionPlayerComponent          │   │
│  │      └─ Player (引用) ───────┐      │   │
│  └──────────────────────────────┼──────┘   │
│                                 │          │
│  ┌──────────────────────────────▼──────┐   │
│  │ Player (玩家实体)                   │   │
│  │  ├─ PlayerSessionComponent         │   │
│  │  │   ├─ Session (引用) ─────────┐  │   │
│  │  │   └─ MailBoxComponent        │  │   │
│  │  │       (GateSession类型)      │  │   │
│  │  └─ MailBoxComponent             │  │   │
│  │      (UnOrderedMessage)          │  │   │
│  └──────────────────────────────────┼──┘   │
│                                     │      │
└─────────────────────────────────────┼──────┘
                                      │
                          接收Map推送的消息
```

### 绑定代码

```csharp
// C2G_LoginGameGateHandler.cs:63-70

// 创建Player实体
Player player = playerComponent.AddChildWithId<Player, string>(request.RoleId, account);

// 给Player添加SessionComponent和MailBox
PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();
playerSessionComponent.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.GateSession);
await playerSessionComponent.AddLocation(LocationType.GateSession);

// 注册Player的Actor MailBox
player.AddComponent<MailBoxComponent, MailBoxType>(MailBoxType.UnOrderedMessage);
await player.AddLocation(LocationType.Player);

// 双向绑定
session.AddComponent<SessionPlayerComponent>().Player = player;  // Session → Player
playerSessionComponent.Session = session;  // Player → Session
```

---

## Location服务机制

### Location类型

```csharp
public enum LocationType
{
    Unit = 0,         // Unit在哪个Map Scene
    Mail = 1,         // MailUnit在哪个Mail Scene
    Player = 2,       // Player在哪个Gate Scene
    GateSession = 3,  // PlayerSessionComponent在哪个Gate Scene
}
```

### Location查找流程

```
┌────────────────────────────────────────────────────────┐
│  Location Service (LocationProxyComponent)             │
├────────────────────────────────────────────────────────┤
│                                                        │
│  LocationType.Unit查找表:                              │
│  ┌──────────────────────────────────────────────┐     │
│  │ UnitId (long) → ActorId (Map Scene地址)      │     │
│  ├──────────────────────────────────────────────┤     │
│  │ 1001 → Process:1 Fiber:100 Instance:12345    │     │
│  │ 1002 → Process:1 Fiber:101 Instance:12346    │     │
│  │ 1003 → Process:1 Fiber:100 Instance:12347    │     │
│  └──────────────────────────────────────────────┘     │
│                                                        │
│  LocationType.Mail查找表:                              │
│  ┌──────────────────────────────────────────────┐     │
│  │ UnitId (long) → ActorId (Mail Scene地址)     │     │
│  └──────────────────────────────────────────────┘     │
│                                                        │
│  LocationType.Player查找表:                            │
│  ┌──────────────────────────────────────────────┐     │
│  │ PlayerId (long) → ActorId (Gate Scene地址)   │     │
│  └──────────────────────────────────────────────┘     │
│                                                        │
└────────────────────────────────────────────────────────┘
```

### Location注册与查找

```csharp
// 注册Location (在Unit传送到Map时)
await player.AddLocation(LocationType.Unit);

// 查找Location并发送消息 (在Gate转发时)
await root.GetComponent<MessageLocationSenderComponent>()
    .Get(LocationType.Unit)
    .Call(unitId, request);

// Location内部流程:
// 1. 根据unitId查找ActorId
// 2. 通过MessageSender发送到目标Actor
// 3. 目标Actor的MailBox接收消息
// 4. 分发到对应的Handler处理
```

---

## 消息发送器对比

| 发送器类型 | 使用场景 | 是否需要Location | 示例 |
|-----------|---------|-----------------|------|
| **MessageSender** | 直接发送到已知ActorId | 否 | 发送到Rank Scene (固定ActorId) |
| **MessageLocationSender** | 发送到动态位置的Entity | 是 | 发送到Unit/Mail (需要查找Map) |
| **ProcessInnerSender** | 进程内Fiber间通信 | 否 | Main → NetClient |
| **Session.Send** | 发送到客户端 | 否 | Gate → Client |

### 代码示例

```csharp
// 1. MessageSender - 固定ActorId
ActorId rankActorId = StartSceneConfigCategory.Instance.Rank.ActorId;
IResponse response = await root.GetComponent<MessageSender>()
    .Call(rankActorId, request);

// 2. MessageLocationSender - 动态Location
IResponse response = await root.GetComponent<MessageLocationSenderComponent>()
    .Get(LocationType.Unit)
    .Call(unitId, request);

// 3. ProcessInnerSender - Fiber间通信
clientSender.GetFiber().Root.GetComponent<ProcessInnerSender>()
    .Send(netClientActorId, message);

// 4. Session.Send - 发送到客户端
session.Send(message);
```

---

## 总结

ET框架通过**Actor模型 + Fiber调度 + 分层消息协议**实现了高性能的分布式游戏通信架构：

### 核心优势

1. **位置透明**: Gate不需要知道Unit在哪个Map，Location服务自动定位
2. **协议驱动**: 通过消息接口类型自动路由到不同后端服务
3. **Actor隔离**: 每个Scene状态完全隔离，天然线程安全
4. **会话保持**: Player实体持久化，Session断开自动清理
5. **双向通信**: 支持客户端请求和服务端主动推送
6. **水平扩展**: 支持多Map/多Gate分布式部署

### 关键设计模式

- **智能路由器模式**: Gate根据消息类型自动路由
- **位置服务模式**: Location动态定位Entity所在Scene
- **双向绑定模式**: Session ↔ Player相互引用
- **MailBox模式**: Actor消息队列处理
- **RpcId匹配模式**: 异步请求响应关联

### 核心思想

**Gate作为智能路由器，根据消息接口类型自动将请求转发到正确的后端服务，并负责将响应返回给客户端！**

---

**文档版本**: v1.0
**创建日期**: 2025-01-14
**作者**: Claude Code
**适用项目**: ET Framework WebGL-Luban
