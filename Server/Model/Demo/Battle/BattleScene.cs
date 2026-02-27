using System.Collections.Generic;

namespace ET
{
    [EnableClass]
    public class DropChoice
    {
        public long DropId { get; set; }
        public int ItemConfigId { get; set; }
        
        // 各玩家的选择结果：playerId -> 选择结果
        public Dictionary<long, ChoiceResult> Results { get; } = new();
    }

    [EnableClass]
    public class ChoiceResult
    {
        public bool IsEquip { get; set; }        // 是否装备
        public long TargetHeroId { get; set; }   // 装备给哪个英雄
        public int ReplaceSlot { get; set; }     // 替换哪个槽位
    }
    
    public enum BattleState
    {
        None = 0,
    
        // 准备阶段
        Prepare = 1,
    
        // 战斗中
        Fighting = 2,
    
        // 等待玩家选择（掉落装备、技能选择等）
        WaitingChoice = 3,
    
        // 结算中
        Settle = 4,
    
        // 已结束
        End = 5,
    }

    [ComponentOf]
    public class Battle : Entity, IScene, IAwake, IUpdate
    {
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

}