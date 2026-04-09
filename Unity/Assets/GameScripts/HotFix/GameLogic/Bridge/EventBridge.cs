using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// ET↔TE 事件桥接的静态 API。
    /// 
    /// 使用方式：
    /// 1. 先在 Scene 上挂载 EventBridgeComponent（通常在 EntryEvent 中）：
    ///    scene.AddComponent&lt;EventBridgeComponent&gt;()
    ///    
    /// 2. ET→TE 自动桥接：给 struct 加 [BridgeToTE] 标记，然后在 ET 的 [Event] Handler 中调用：
    ///    EventBridge.PublishToTE(args)
    ///    
    /// 3. TE→ET 桥接：在 UI/表现层中订阅：
    ///    EventBridge.SubscribeTE&lt;BattleUnitDead&gt;(OnDeadHandler)
    ///    // 然后在任意位置发布 ET 事件：
    ///    EventBridge.PublishET(scene, new BattleUnitDead { ... })
    /// </summary>
    public static class EventBridge
    {
        /// <summary>
        /// ET→TE 方向：将 ET 事件数据转发到 TE 侧。
        /// 使用 struct 的 Type 的 HashCode 作为 TE 事件 key，避免字符串分配。
        /// 调用前需确认目标 struct 标记了 [BridgeToTE]。
        /// </summary>
        public static void PublishToTE<T>(T eventData) where T : struct
        {
            int eventId = typeof(T).GetHashCode();
            TEngine.GameEvent.Send(eventId, eventData);
        }

        /// <summary>
        /// TE→ET 方向：从 TE 侧发布 ET 事件。
        /// 等价于 EventSystem.Instance.Publish(scene, args)，但封装为更便捷的静态调用。
        /// </summary>
        public static void PublishET<T>(Scene scene, T args) where T : struct
        {
            EventSystem.Instance.Publish(scene, args);
        }

        /// <summary>
        /// TE→ET 方向：注册 TE 侧监听，当触发时自动转发为 ET 事件。
        /// 使用泛型确保类型安全，struct 直传零 GC。
        /// 
        /// 注意：需要在某个 Scene 上已挂载 EventBridgeComponent。
        /// Scene 作为闭包捕获，确保 Scene 销毁后不会发布到已失效的 Scene。
        /// </summary>
        public static void SubscribeTE<T>(Scene scene, Action<T> handler) where T : struct
        {
            int eventId = typeof(T).GetHashCode();
            var bridge = scene.GetComponent<EventBridgeComponent>();
            if (bridge == null)
            {
                Log.Error($"EventBridge: Scene 上未挂载 EventBridgeComponent，无法订阅 {typeof(T).Name}");
                return;
            }

            Action<T> wrapped = data =>
            {
                if (scene.IsDisposed) return;
                EventSystem.Instance.Publish(scene, data);
            };

            TEngine.GameEvent.AddEventListener(eventId, wrapped);
            bridge._teHandlers[typeof(T)] = wrapped;
        }

        /// <summary>
        /// TE→ET 方向：注册 TE 侧监听，触发时执行自定义回调（不转发到 ET 事件系统）。
        /// 适用于 TE 侧只想监听 ET 结构化事件数据，但不需要走 ET 事件管道的场景。
        /// </summary>
        public static void SubscribeTEOnly<T>(Scene scene, Action<T> handler) where T : struct
        {
            int eventId = typeof(T).GetHashCode();
            var bridge = scene.GetComponent<EventBridgeComponent>();
            if (bridge == null)
            {
                Log.Error($"EventBridge: Scene 上未挂载 EventBridgeComponent，无法订阅 {typeof(T).Name}");
                return;
            }

            TEngine.GameEvent.AddEventListener(eventId, handler);
            bridge._teHandlers[typeof(T)] = handler;
        }

        /// <summary>
        /// 取消 TE 侧的事件订阅。
        /// </summary>
        public static void UnsubscribeTE<T>(Scene scene) where T : struct
        {
            int eventId = typeof(T).GetHashCode();
            var bridge = scene.GetComponent<EventBridgeComponent>();
            if (bridge == null) return;

            if (bridge._teHandlers.TryGetValue(typeof(T), out var handler))
            {
                TEngine.GameEvent.RemoveEventListener(eventId, handler);
                bridge._teHandlers.Remove(typeof(T));
            }
        }

        /// <summary>
        /// 在 TE 侧手动触发一个已标记 [BridgeToTE] 的 ET 事件。
        /// 适用于 TE 表现层需要主动触发 ET 逻辑事件的场景（如 UI 按钮触发技能）。
        /// </summary>
        public static void Trigger<T>(T eventData) where T : struct
        {
            int eventId = typeof(T).GetHashCode();
            TEngine.GameEvent.Send(eventId, eventData);
        }
    }
}
