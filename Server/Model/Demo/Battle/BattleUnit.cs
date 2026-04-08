using System.Numerics;

namespace ET
{
    public enum UnitCamp
    {
        Friend = 1,  // 友方
        Enemy = 2,   // 敌方
    }
    // 战斗单位
    [ChildOf(typeof(BattleRoom))]
    public partial class BattleUnit : Entity, IAwake<int>, IDestroy
    {
        // 配置ID
        public int ConfigId { get; set; }
    
        // 阵营
        public UnitCamp Camp { get; set; }  // Friend/Enemy
    
        // 所属玩家（英雄用，怪物为0）
        public long OwnerId { get; set; }
    
        // 位置信息
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        
        // 是否死亡
        public bool IsDead { get; set; }

        // 是否Boss单位（创建时根据 MonsterUnitConfig.Type 标记）
        public bool IsBoss { get; set; }
    }
}