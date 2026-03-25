using System.Numerics;

namespace ET.Server
{
    /// <summary>
    /// 移动组件 - 纯移动执行器，只负责从当前位置移动到目标位置
    /// </summary>
    [ComponentOf(typeof(BattleUnit))]
    public class BattleMoveComponent : Entity, IAwake, IDestroy
    {
        public float MoveSpeed { get; set; } = 1.0f;
        public Vector3 TargetPosition { get; set; }
        public long LastUpdateTime { get; set; }
        public long MoveTimerId { get; set; }
    }
}
