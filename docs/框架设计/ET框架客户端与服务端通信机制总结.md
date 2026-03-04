# ET框架客户端与服务端通信机制总结

## 1. 整体架构概览

### 1.1 Actor模型基础
```
ET框架 = 分布式Actor系统
Actor = Scene (具有独立状态和消息处理能力的计算单元)
ActorId = Process + Fiber + InstanceId (Actor的全局唯一地址)
```

### 1.2 通信架构图
```
客户端                                    服务端
┌─────────────────┐                      ┌─────────────────┐
│ Main Scene      │                      │ Realm Scene     │
│ (Demo)          │                      │ (认证)          │
│ ├─ClientSender  │◄────Actor消息────────┤ ├─LoginHandler │
│ └─UIComponent   │                      │ └─AccountMgr    │
└─────────────────┘                      └─────────────────┘
        │                                        │
        ▼                                        ▼
┌─────────────────┐                      ┌─────────────────┐
│ NetClient Scene │                      │ Gate Scene      │
│ (网络通信)      │◄────TCP/UDP──────────┤ (网关)          │
│ ├─NetComponent  │                      │ ├─SessionMgr    │
│ ├─Session       │                      │ └─PlayerMgr     │
│ └─SessionComp   │                      └─────────────────┘
└─────────────────┘                              │
                                                 ▼
                                        ┌─────────────────┐
                                        │ Map Scene       │
                                        │ (游戏逻辑)      │
                                        │ ├─UnitComponent │
                                        │ ├─AOIComponent  │
                                        │ └─GameLogic     │
                                        └─────────────────┘
```

## 2. Fiber(纤程)机制

### 2.1 Fiber的作用
- **轻量级线程**：比Thread更轻量，支持大规模并发
- **Actor容器**：每个Scene运行在特定的Fiber中
- **调度隔离**：不同业务逻辑在不同Fiber中执行

### 2.2 客户端Fiber分布
```csharp
Main Fiber (SchedulerType.Main)
├── Demo Scene              // 主业务逻辑
├── CurrentScenesComponent  // 场景管理
└── ClientSenderComponent   // 通信管理

NetClient Fiber (SchedulerType.ThreadPool)
└── NetClient Scene         // 网络通信专用
    ├── NetComponent        // 网络组件
    ├── SessionComponent    // Session管理
    └── ProcessInnerSender  // 内部通信
```

### 2.3 Fiber创建示例
```csharp
// 动态创建NetClient Fiber
int fiberId = await FiberManager.Instance.Create(
    SchedulerType.ThreadPool,   // 线程池调度
    0,                         // 进程ID
    SceneType.NetClient,       // Scene类型
    ""                         // 名称
);

// 生成ActorId
ActorId netClientActorId = new ActorId(process, fiberId, instanceId);
```

## 3. 消息协议类型

### 3.1 按通信方向分类
```csharp
// 客户端→服务端
C2R_LoginAccount      // Client to Realm
C2G_LoginGameGate     // Client to Gate
C2M_CreateEnemy       // Client to Map

// 服务端→客户端
R2C_LoginAccount      // Realm to Client
G2C_LoginGameGate     // Gate to Client
M2C_CreateEnemy       // Map to Client

// 内部Actor通信
Main2NetClient_Login         // Main Scene → NetClient Scene
NetClient2Main_Login         // NetClient Scene → Main Scene
```

### 3.2 按响应模式分类
```csharp
// Request-Response (有响应)
IRequest / IResponse
C2R_LoginAccount → R2C_LoginAccount

// Message (无响应)
IMessage
M2C_CreateEnemy (服务端主动推送)
```

### 3.3 按路由方式分类
```csharp
// 普通消息 - 直接路由
[MessageHandler(SceneType.NetClient)]
public class Main2NetClient_LoginHandler : MessageHandler<Scene, Main2NetClient_Login>

// Location消息 - 通过Location服务定位
[MessageHandler(SceneType.Map)]
public class C2M_CreateEnemyHandler : MessageLocationHandler<Unit, C2M_CreateEnemy, M2C_CreateEnemy>
```

## 4. Actor消息路由机制

### 4.1 客户端内部路由
```csharp
// 步骤1: Main Scene发起调用
clientSender.Send(message);

// 步骤2: 包装为Actor消息
A2NetClient_Message actorMessage = A2NetClient_Message.Create();
actorMessage.MessageObject = message;

// 步骤3: 通过ProcessInnerSender发送到NetClient
ProcessInnerSender.Send(netClientActorId, actorMessage);

// 步骤4: NetClient的MessageHandler处理
[MessageHandler(SceneType.NetClient)]
public class A2NetClient_MessageHandler : MessageHandler<Scene, A2NetClient_Message>

// 步骤5: NetClient通过Session发送到服务器
session.Send(message);
```

### 4.2 消息分发过滤机制
```csharp
// MessageDispatcher的过滤逻辑
foreach (MessageDispatcherInfo dispatcherInfo in messageHandlers)
{
    // 关键过滤：SceneType必须匹配
    if (!scene.SceneType.HasSameFlag(dispatcherInfo.SceneType))
    {
        continue;  // 跳过不匹配的处理器
    }

    // 执行匹配的处理器
    await dispatcherInfo.IMHandler.Handle(entity, fromAddress, message);
}
```

## 5. 服务端消息转发机制

### 5.1 服务端架构层级
```
Router (路由层)
    ↓
Realm (认证层)
    ↓
Gate (网关层)
    ↓
Map (逻辑层)
```

### 5.2 消息转发流程
```csharp
// 1. 客户端发送到Gate
C2M_CreateEnemy → Gate Scene

// 2. Gate转发到Map (通过Location服务)
[MessageHandler(SceneType.Gate)]
public class C2M_CreateEnemyHandler : MessageHandler<Session, C2M_CreateEnemy>
{
    // Gate只是转发，实际处理在Map
    await MessageLocationSender.Call(playerId, request);
}

// 3. Map Scene处理业务逻辑
[MessageHandler(SceneType.Map)]
public class C2M_CreateEnemyHandler : MessageLocationHandler<Unit, C2M_CreateEnemy, M2C_CreateEnemy>
{
    // 实际的游戏逻辑处理
    Unit enemy = await UnitFactory.Create(scene, enemyId, UnitType.Enemy);
}

// 4. Map响应返回给客户端
M2C_CreateEnemy → Gate → Client
```

### 5.3 Location服务定位
```csharp
// Location服务根据Entity ID查找所在的Map Scene
ActorId targetActorId = await LocationProxy.Get(EntityType.Player, playerId);

// 消息路由到正确的Map Scene
MessageSender.Send(targetActorId, message);
```

## 6. 网络连接管理

### 6.1 Session生命周期
```csharp
// 阶段1: 连接Realm进行认证
Session realmSession = await netComponent.CreateRouterSession(realmAddress, account, password);
root.AddComponent<SessionComponent>().Session = realmSession;

// 阶段2: 切换到Gate进行游戏
realmSession.Dispose();  // 断开Realm
Session gateSession = await netComponent.CreateRouterSession(gateAddress, account, account);
root.GetComponent<SessionComponent>().Session = gateSession;  // 替换Session
```

### 6.2 Session与Actor的关系
```csharp
NetClient Actor (容器)
└── SessionComponent (管理器)
    └── Session (网络连接)
        └── TCP/UDP Socket (传输层)
```

## 7. ActorId构成详解

### 7.1 ActorId完整结构
```csharp
public struct ActorId
{
    public Address Address;     // Process + Fiber
    public long InstanceId;     // 实例ID
}

public struct Address
{
    public int Process;         // 进程ID
    public int Fiber;          // 纤程ID
}
```

### 7.2 ActorId的作用
- **全局定位**：Process.Fiber.InstanceId 唯一标识一个Actor
- **重启识别**：InstanceId防止Actor重启后的消息混乱
- **跨进程通信**：支持分布式Actor消息路由

### 7.3 ActorId示例
```csharp
// 创建NetClient的ActorId
ActorId netClientActorId = new ActorId(process: 1, fiber: 100, instanceId: 12345);
// ToString(): "1:100:12345"

// 重启后的新ActorId
ActorId newActorId = new ActorId(process: 1, fiber: 100, instanceId: 67890);
// ToString(): "1:100:67890" (相同地址，不同实例)
```

## 8. 关键设计模式

### 8.1 Actor隔离原则
- **每个Scene是独立的Actor**
- **Actor间只能通过消息通信**
- **Actor内部状态完全隔离**
- **消息处理串行化，天然线程安全**

### 8.2 分层职责
```csharp
Main Scene:    业务逻辑、UI管理、游戏状态
NetClient:     网络通信、协议处理、连接管理
Session:       TCP/UDP传输、数据序列化
```

### 8.3 事件驱动模式
```csharp
// 场景内事件
EventSystem.Instance.Publish(scene, new LoginFinish());

// 跨Scene Actor消息
ProcessInnerSender.Send(actorId, message);

// 跨进程网络消息
session.Send(message);
```

## 9. NetClient创建与管理

### 9.1 NetClient创建流程
```csharp
// 1. 用户触发登录
LoginHelper.Login(scene, account, password);

// 2. 创建ClientSenderComponent
ClientSenderComponent clientSender = root.AddComponent<ClientSenderComponent>();

// 3. ClientSender创建NetClient Fiber
int fiberId = await FiberManager.Instance.Create(SceneType.NetClient, "");

// 4. 触发FiberInit_NetClient，为NetClient添加组件
root.AddComponent<MailBoxComponent>();
root.AddComponent<TimerComponent>();
root.AddComponent<ProcessInnerSender>();
```

### 9.2 NetClient组件配置
```csharp
NetClient Scene组件：
├── MailBoxComponent         // 消息队列
├── TimerComponent          // 定时器
├── CoroutineLockComponent  // 协程锁
├── ProcessInnerSender      // 进程内消息发送
├── FiberParentComponent    // 父纤程引用
├── NetComponent           // 网络组件
└── SessionComponent       // Session管理
    └── Session           // 实际TCP/UDP连接
```

### 9.3 Session管理策略
```csharp
// NetClient只管理1个Session，采用替换模式
public class SessionComponent: Entity
{
    private EntityRef<Session> session;  // 单个Session
    public Session Session { get; set; }
}

// Session切换流程
Realm Session → Gate Session
```

## 10. 总结

ET框架通过**Actor模型 + Fiber调度 + 分层消息协议**实现了高性能的分布式游戏通信架构：

1. **Actor提供隔离**: 每个Scene独立处理消息，避免状态冲突
2. **Fiber提供并发**: 轻量级纤程支持大规模并发处理
3. **消息提供通信**: 统一的消息协议实现跨Actor/跨进程通信
4. **Location提供定位**: 动态定位Entity所在的Actor，支持负载均衡
5. **Session提供传输**: 管理实际的网络连接和数据传输

### 核心优势
- **高性能**: Fiber并发 + 异步消息处理
- **高可靠**: Actor状态隔离 + 消息序列化处理
- **可扩展**: 分布式Actor + Location服务支持水平扩展
- **易维护**: 清晰的分层架构 + 统一的消息协议

这种架构是现代分布式游戏服务器的优秀实践，为高并发、大规模的网络游戏提供了强大的技术基础。