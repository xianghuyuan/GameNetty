# GameNetty 网络通信架构 (ET8.1 + Fiber)

## 1. 核心模型：Fiber 与 Actor
GameNetty 完全继承了 ET8.1 的 Fiber (纤程) 模型。每个逻辑 Scene 运行在独立的 Fiber 中，互不干扰，通过 Actor 消息进行通信。

### 客户端 Fiber 分布
- **Main Fiber**: 核心逻辑、UI 管理、本地数据。
- **NetClient Fiber**: 专门处理 Socket 连接、KCP/TCP 协议解析。
- **Logic Fiber**: 处理复杂的本地逻辑（如 AI、寻路、重连逻辑）。

---

## 2. 消息传递流程 (Decoupled Flow)
在 GameNetty 中，消息的收发遵循严格的**解耦路径**。

### 发送消息 (C2S)
1. **Business Call**: 逻辑层 (如 `Hotfix`) 发起请求 `clientSender.Call(request)`。
2. **Actor Message**: 消息被包装为 Actor 消息，发送到 `NetClient` Fiber。
3. **Session Delivery**: `NetClient` 下的 `Session` 通过 KCP/TCP 发送到服务端。

### 接收消息 (S2C)
1. **Net Decode**: `NetClient` Fiber 接收并解码字节流。
2. **Dispatch to Main**: 消息被推送到主 Fiber。
3. **Hotfix Handling**: 触发注册在 `Hotfix` 层的 `MessageHandler`。

---

## 3. 协议分类与处理

### Proto 协议定义
所有的协议定义位于 `Config/Proto/`。
- `OuterMessage`: 客户端与服务端通信。
- `InnerMessage`: 服务端各进程内部通信。

### 消息处理器示例
```csharp
[MessageHandler(SceneType.Main)]
public class LoginHandler : MessageHandler<Scene, R2C_LoginAccount>
{
    protected override async ETTask Run(Scene scene, R2C_LoginAccount message)
    {
        // 处理登录逻辑
        // 1. 更新 Model 层状态
        // 2. 发送 Event 给 View 层
        await ETTask.CompletedTask;
    }
}
```

---

## 4. 路由与网关
- **Realm (认证)**: 负责账号验证，分配 Gate 地址。
- **Gate (网关)**: 维持客户端 Session，转发消息到 Map。
- **Map (地图/逻辑)**: 实际的战斗和业务逻辑处理。

---

## 5. 通信安全与重连
- **Token 校验**: 登录后获取 Token，Session 建立时进行二次校验。
- **断线重连**: GameNetty 内置了基于 `SessionComponent` 的状态机重连机制。
