namespace ET.Server
{
    [EntitySystemOf(typeof(BattleAIComponent))]
    [FriendOf(typeof(BattleAIComponent))]
    public static partial class BattleAIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleAIComponent self)
        {
        }
        
        [EntitySystem]
        private static void Destroy(this BattleAIComponent self)
        {
        }
        
        public static void Update(this BattleAIComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                return;
            }

            if (owner.Camp == UnitCamp.Friend)
            {
                PlayerCombatModeComponent modeComponent = owner.GetComponent<PlayerCombatModeComponent>();
                if (modeComponent == null || !modeComponent.IsAutoBattle)
                {
                    owner.GetComponent<BattleMoveComponent>()?.StopMove();
                    owner.GetComponent<BattleActionDecisionComponent>()?.Reset();
                    return;
                }
            }

            BattleActionDecisionComponent decisionComponent = owner.GetComponent<BattleActionDecisionComponent>();
            decisionComponent?.Update();
        }
    }
}
