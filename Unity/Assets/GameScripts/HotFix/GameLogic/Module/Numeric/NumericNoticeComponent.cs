namespace ET
{
    /// <summary>
    /// 数值变化通知组件
    /// 用于通知 UI 数值变化（血量、蓝量等）
    /// MVP 版本暂时空实现，预留接口
    /// </summary>
    [ComponentOf()]
    public class NumericNoticeComponent : Entity, IAwake
    {
    }
    
    [EntitySystemOf(typeof(NumericNoticeComponent))]
    public static partial class NumericNoticeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this NumericNoticeComponent self)
        {
            // 暂时空实现
        }
    }
}
