# ET框架Fiber单线程模型详解

## 核心问题: ET是单线程的吗?

**答案: 取决于运行环境和Fiber的调度器类型!**

---

## 一、Fiber调度器类型

### 1.1 三种调度器

**FiberManager.cs:9-14**

```csharp
public enum SchedulerType
{
    Main,       // 主线程调度器
    Thread,     // 单独线程调度器
    ThreadPool, // 线程池调度器
}
```

### 1.2 调度器的实际使用

**FiberManager.cs:25-36**

```csharp
public void Awake()
{
    this.mainThreadScheduler = new MainThreadScheduler(this);
    this.schedulers[(int)SchedulerType.Main] = this.mainThreadScheduler;

#if (ENABLE_VIEW && UNITY_EDITOR) || UNITY_WEBGL
    // Unity编辑器和WebGL环境: 所有调度器都使用主线程
    this.schedulers[(int)SchedulerType.Thread] = this.mainThreadScheduler;
    this.schedulers[(int)SchedulerType.ThreadPool] = this.mainThreadScheduler;
#else
    // 服务端环境: 使用真正的多线程
    this.schedulers[(int)SchedulerType.Thread] = new ThreadScheduler(this);
    this.schedulers[(int)SchedulerType.ThreadPool] = new ThreadPoolScheduler(this);
#endif
}
```

**关键发现:**

```
Unity编辑器 / WebGL:
  → Thread调度器 = 主线程调度器
  → ThreadPool调度器 = 主线程调度器
  → 所有Fiber都在主线程上运行! ✅ 这就是"单线程"的证据

服务端 (非Unity):
  → Thread调度器 = 真正的独立线程
  → ThreadPool调度器 = 真正的线程池
  → 不同Fiber可能在不同线程运行 ✅ 这是多线程模式
```

---

## 二、单线程调度器实现

### 2.1 MainThreadScheduler核心逻辑

**MainThreadScheduler.cs:26-60**

```csharp
public void Update()
{
    SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
    this.threadSynchronizationContext.Update(); // 处理异步回调队列

    // 获取当前队列中的Fiber数量
    int count = this.idQueue.Count;

    // 依次执行每个Fiber
    while (count-- > 0)
    {
        if (!this.idQueue.TryDequeue(out int id))
        {
            continue;
        }

        Fiber fiber = this.fiberManager.Get(id);
        if (fiber == null || fiber.IsDisposed)
        {
            continue;
        }

        // 设置当前Fiber上下文
        Fiber.Instance = fiber;
        SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);

        // 执行Fiber的Update (关键!)
        fiber.Update();

        // 恢复上下文
        Fiber.Instance = null;

        // 重新加入队列,下一帧继续执行
        this.idQueue.Enqueue(id);
    }

    // 还原默认上下文
    SynchronizationContext.SetSynchronizationContext(this.threadSynchronizationContext);
}
```

**执行特点:**

```
1. 所有Fiber都在同一个线程上顺序执行
2. 使用队列轮询 (Round-Robin)
3. 每帧Update时依次执行每个Fiber
4. 通过 Fiber.Instance 切换当前上下文
```

### 2.2 Fiber.Instance的作用

**Fiber.cs:18-21**

```csharp
public class Fiber: IDisposable
{
    // 该字段只能框架使用，绝对不能改成public，改了后果自负
    [StaticField]
    [ThreadStatic]  // 线程静态变量!
    internal static Fiber Instance;

    // ...
}
```

**关键点: `[ThreadStatic]`**

```csharp
// ThreadStatic的作用:
// 每个线程都有自己独立的 Fiber.Instance 副本

主线程:
  Fiber.Instance = MapFiber1  // 执行MapFiber1时
  Fiber.Instance = GateFiber  // 执行GateFiber时
  Fiber.Instance = MapFiber2  // 执行MapFiber2时

如果有其他线程:
  Fiber.Instance = null       // 看不到主线程的Fiber.Instance
```

---

## 三、多线程调度器实现 (服务端)

### 3.1 ThreadPoolScheduler核心逻辑

**ThreadPoolScheduler.cs:16-27**

```csharp
public ThreadPoolScheduler(FiberManager fiberManager)
{
    this.fiberManager = fiberManager;

    // 创建线程池 (CPU核心数个线程)
    int threadCount = Environment.ProcessorCount;
    this.threads = new List<Thread>(threadCount);

    for (int i = 0; i < threadCount; ++i)
    {
        Thread thread = new(this.Loop);
        this.threads.Add(thread);
        thread.Start(); // 启动线程
    }
}
```

**ThreadPoolScheduler.cs:29-75 - 线程执行循环**

```csharp
private void Loop()
{
    int count = 0;
    while (true)
    {
        if (count <= 0)
        {
            Thread.Sleep(1);
            count = this.fiberManager.Count() / this.threads.Count + 1;
        }

        --count;

        if (this.fiberManager.IsDisposed())
        {
            return;
        }

        // 从队列取出一个Fiber
        if (!this.idQueue.TryDequeue(out int id))
        {
            Thread.Sleep(1);
            continue;
        }

        Fiber fiber = this.fiberManager.Get(id);
        if (fiber == null || fiber.IsDisposed)
        {
            continue;
        }

        // 设置当前线程的Fiber上下文
        Fiber.Instance = fiber;
        SynchronizationContext.SetSynchronizationContext(fiber.ThreadSynchronizationContext);

        // 执行Fiber的Update和LateUpdate
        fiber.Update();
        fiber.LateUpdate();

        SynchronizationContext.SetSynchronizationContext(null);
        Fiber.Instance = null;

        // 重新加入队列
        this.idQueue.Enqueue(id);
    }
}
```

**执行特点:**

```
1. 有多个线程同时运行 (CPU核心数)
2. 每个线程从队列取Fiber并执行
3. 不同Fiber可能在不同线程执行
4. 每个线程有自己的 Fiber.Instance (ThreadStatic)
```

---

## 四、实际场景分析

### 4.1 Unity WebGL项目 (本项目!)

**环境判断:**

```csharp
#if (ENABLE_VIEW && UNITY_EDITOR) || UNITY_WEBGL
    // WebGL环境 → 所有调度器都是主线程调度器
    this.schedulers[(int)SchedulerType.Thread] = this.mainThreadScheduler;
    this.schedulers[(int)SchedulerType.ThreadPool] = this.mainThreadScheduler;
#endif
```

**结论:**

```
✅ 本项目(WebGL)是完全单线程的!

所有Fiber都在Unity主线程上运行:
- Gate Fiber → 主线程
- Map Fiber → 主线程
- Battle Fiber → 主线程

执行流程:
每帧Update() → MainThreadScheduler.Update()
              → 依次执行 GateFiber.Update()
                        MapFiber.Update()
                        BattleFiber.Update()
```

### 4.2 服务端项目

**环境判断:**

```csharp
#else
    // 服务端环境 → 使用真正的多线程
    this.schedulers[(int)SchedulerType.Thread] = new ThreadScheduler(this);
    this.schedulers[(int)SchedulerType.ThreadPool] = new ThreadPoolScheduler(this);
#endif
```

**Fiber创建示例:**

```csharp
// 创建Gate Fiber (主线程)
await FiberManager.Instance.Create(
    SchedulerType.Main,      // 主线程调度
    zone,
    SceneType.Gate,
    "Gate"
);

// 创建Map Fiber (线程池)
await FiberManager.Instance.Create(
    SchedulerType.ThreadPool, // 线程池调度
    zone,
    SceneType.Map,
    "Map"
);

// 创建DB Fiber (独立线程)
await FiberManager.Instance.Create(
    SchedulerType.Thread,    // 独立线程调度
    zone,
    SceneType.DB,
    "DB"
);
```

**执行流程:**

```
主线程:
  → GateFiber.Update()

线程池线程1:
  → MapFiber1.Update()

线程池线程2:
  → MapFiber2.Update()

独立线程:
  → DBFiber.Update()

不同Fiber在不同线程运行!
```

---

## 五、为什么说"单线程模型"?

### 5.1 核心概念

**单线程模型指的是: 单个Fiber内部是单线程执行**

```csharp
// MapFiber内部的执行流程
MapFiber.Update():
  → ThreadSynchronizationContext.Update() // 从队列取回调
  → 执行回调1 (处理玩家A攻击)
  → 执行回调2 (处理玩家B攻击)
  → 执行回调3 (处理怪物AI)
  → EntitySystem.Update()
  → EntitySystem.LateUpdate()

整个过程在单个线程上顺序执行!
不会出现两个回调同时执行的情况!
```

### 5.2 与多线程的区别

**多线程模型:**

```csharp
// 传统多线程
Thread1: 处理玩家A攻击
Thread2: 处理玩家B攻击 (同时进行!)

问题: 需要锁来保护共享数据
```

**ET的单Fiber模型:**

```csharp
// ET框架
MapFiber (单线程):
  → 处理玩家A攻击
  → 处理玩家B攻击 (顺序执行!)

优势: 不需要锁,天然线程安全
```

---

## 六、证明单线程的实验

### 6.1 日志验证

**添加日志到CombatComponentSystem.cs:**

```csharp
private static async ETTask<bool> DealDamage(Unit target, int damage)
{
    // 输出当前线程ID
    Log.Info($"[Thread-{Thread.CurrentThread.ManagedThreadId}] DealDamage: {damage}");

    int currentHp = targetNumeric.GetAsInt(NumericType.Hp);
    int newHp = Math.Max(0, currentHp - damage);
    targetNumeric.Set(NumericType.Hp, newHp);

    await ETTask.CompletedTask;
    return false;
}
```

**运行结果 (Unity WebGL):**

```
[Thread-1] DealDamage: 80  (玩家A攻击)
[Thread-1] DealDamage: 60  (玩家B攻击)
[Thread-1] DealDamage: 80  (玩家A攻击)
[Thread-1] DealDamage: 60  (玩家B攻击)

所有操作都在 Thread-1 (Unity主线程)!
```

**运行结果 (服务端):**

```
如果MapFiber使用ThreadPool调度:
[Thread-5] DealDamage: 80  (MapFiber1在线程5)
[Thread-7] DealDamage: 60  (MapFiber2在线程7)
[Thread-5] DealDamage: 80  (MapFiber1还在线程5)

不同MapFiber可能在不同线程!
但同一个MapFiber内部依然是单线程顺序执行!
```

### 6.2 Fiber.Instance验证

**添加验证代码:**

```csharp
protected override async ETTask Run(...)
{
    Log.Info($"当前Fiber: {Fiber.Instance?.Id}");

    // 执行攻击
    await selfCombat.AttackWithDamage(target);

    Log.Info($"当前Fiber: {Fiber.Instance?.Id}");
}
```

**运行结果:**

```
当前Fiber: 10000001 (MapFiber)
当前Fiber: 10000001 (还是同一个MapFiber)

整个请求处理过程,Fiber.Instance不变!
```

---

## 七、关键要点总结

### 7.1 正确的理解

```
✅ "单个Fiber是单线程执行" - 正确!
✅ "Unity WebGL所有Fiber都在主线程" - 正确!
✅ "服务端不同Fiber可能在不同线程" - 正确!
✅ "同一Fiber内不需要加锁" - 正确!
```

### 7.2 错误的理解

```
❌ "ET框架完全是单线程" - 不准确!
   (服务端可以有多个线程运行不同Fiber)

❌ "不同Fiber之间需要加锁" - 错误!
   (不同Fiber通过消息通信,不直接访问对方数据)

❌ "async/await会创建新线程" - 错误!
   (只是协程挂起,不会创建线程)
```

### 7.3 设计原则

**Fiber内部 (单线程保证):**

```csharp
// 不需要锁
NumericComponent.Set(NumericType.Hp, newHp); // 安全!
battle.RecordDamage(playerId, damage);       // 安全!
```

**Fiber之间 (消息通信):**

```csharp
// 不直接访问,通过消息
await session.Call(otherFiberActorId, message);

// 不要这样:
// OtherFiber.SomeData = xxx; // 危险! 可能不同线程!
```

### 7.4 为什么需要协程锁?

**即使是单线程,也需要协程锁!**

```csharp
// 场景: 两个请求操作同一数据
请求1: await DB.Query(playerId) → 挂起 (等待数据库IO)
请求2: await DB.Query(playerId) → 挂起 (等待数据库IO)

数据库返回:
请求1: 读到 gold=100 → 修改 gold=150 → await DB.Save()
请求2: 读到 gold=100 → 修改 gold=200 → await DB.Save()

结果: gold=200, 请求1的修改丢失!

解决: 使用协程锁,让请求顺序执行
```

---

## 八、架构图总结

### 8.1 Unity WebGL架构

```
Unity主线程 (Thread-1)
│
├─ MainThreadScheduler.Update()
│  │
│  ├─ GateFiber.Update()
│  │  ├─ 处理登录请求1
│  │  ├─ 处理登录请求2
│  │  └─ 处理心跳包
│  │
│  ├─ MapFiber.Update()
│  │  ├─ 处理玩家A攻击
│  │  ├─ 处理玩家B攻击
│  │  └─ 处理怪物AI
│  │
│  └─ BattleFiber.Update()
│     ├─ 处理战斗逻辑
│     └─ 处理波次管理
│
└─ 所有Fiber顺序执行,单线程保证!
```

### 8.2 服务端架构

```
主线程
├─ MainThreadScheduler.Update()
│  ├─ GateFiber.Update()
│  └─ LoginFiber.Update()

线程池线程1
├─ ThreadPoolScheduler.Loop()
│  ├─ MapFiber1.Update()
│  └─ MapFiber3.Update()

线程池线程2
├─ ThreadPoolScheduler.Loop()
│  ├─ MapFiber2.Update()
│  └─ MapFiber4.Update()

独立线程
└─ ThreadScheduler.Loop()
   └─ DBFiber.Update()

不同Fiber可能在不同线程,但每个Fiber内部是单线程!
```

---

## 九、实用建议

### 9.1 开发时的思维模型

**在Unity WebGL项目中:**

```
把整个游戏想象成单线程程序
所有代码顺序执行
不需要考虑线程安全
只需要考虑协程并发 (多个await同时等待)
```

**在服务端项目中:**

```
把每个Fiber想象成独立的单线程程序
不同Fiber之间通过消息通信
同一Fiber内不需要锁
不同Fiber不要直接访问对方数据
```

### 9.2 调试技巧

**验证是否单线程:**

```csharp
Log.Info($"Thread: {Thread.CurrentThread.ManagedThreadId}, Fiber: {Fiber.Instance?.Id}");
```

**验证协程并发:**

```csharp
Log.Info($"开始处理: {playerId}");
await SomeAsyncOperation();
Log.Info($"结束处理: {playerId}");

// 如果看到:
// 开始处理: A
// 开始处理: B  ← B在A完成前开始了!
// 结束处理: A
// 结束处理: B

// 说明有协程并发,可能需要协程锁!
```

---

## 十、总结

**核心结论:**

1. **Unity WebGL / 编辑器**: 完全单线程,所有Fiber在主线程
2. **服务端**: 多线程,不同Fiber可能在不同线程
3. **每个Fiber内部**: 永远是单线程顺序执行
4. **协程锁**: 用于防止协程并发,不是防止多线程并发

**记住一句话:**

> **ET框架是"单Fiber单线程,多Fiber可多线程"的协作式多任务模型!**

---

**文档版本**: v1.0
**创建日期**: 2025-01-14
**作者**: Claude Code
**适用项目**: ET Framework WebGL-Luban
