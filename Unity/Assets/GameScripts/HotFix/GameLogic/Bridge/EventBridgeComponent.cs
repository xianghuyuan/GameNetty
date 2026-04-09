using System;
using System.Collections.Generic;

namespace ET
{
    /// <summary>
    /// 标记需要自动桥接到 TEngine 的 ET 事件 struct。
    /// 加此标记后，当 ET 事件被 Publish 时会自动同步到 TE 侧。
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public class BridgeToTEAttribute : Attribute
    {
    }

    /// <summary>
    /// 挂在 Scene 上的桥接组件，管理 ET↔TE 事件的双向互通。
    /// 生命周期与 Scene 绑定，Scene 销毁时自动清理所有 TE 侧订阅。
    /// </summary>
    [EntitySystemOf(typeof(EventBridgeComponent))]
    [FriendOf(typeof(EventBridgeComponent))]
    public static partial class EventBridgeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EventBridgeComponent self)
        {
            self._teHandlers = new Dictionary<Type, Delegate>();
            self._scene = self.Root();
        }

        [EntitySystem]
        private static void Destroy(this EventBridgeComponent self)
        {
            // 清理所有 TE→ET 的桥接订阅
            var dispatcher = TEngine.GameEvent.EventMgr.Dispatcher;
            foreach (var kv in self._teHandlers)
            {
                dispatcher.RemoveEventListener(kv.Key.GetHashCode(), kv.Value);
            }
            self._teHandlers.Clear();
            self._scene = null;
        }
    }

    /// <summary>
    /// ET↔TE 事件双向桥接组件。
    /// 使用方式：scene.AddComponent&lt;EventBridgeComponent&gt;() 挂载到 Scene 上。
    /// </summary>
    public class EventBridgeComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// TE→ET 方向：记录注册到 TE 侧的委托，用于 Destroy 时清理。
        /// Key = ET 事件 struct 的 Type。
        /// </summary>
        internal Dictionary<Type, Delegate> _teHandlers;

        internal Scene _scene;
    }
}
