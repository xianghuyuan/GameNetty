namespace ET.Server
{
    [EntitySystemOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleActionDecisionComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class BattleActionDecisionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleActionDecisionComponent self)
        {
            self.State = BattleActionState.Idle;
        }

        [EntitySystem]
        private static void Destroy(this BattleActionDecisionComponent self)
        {
            self.CurrentTarget = null;
            self.State = BattleActionState.Idle;
        }

        public static void Reset(this BattleActionDecisionComponent self)
        {
            self.CurrentTarget = null;
            self.State = BattleActionState.Idle;
        }

        public static void Update(this BattleActionDecisionComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                return;
            }

            BattleUnitCombatComponent combat = owner.GetComponent<BattleUnitCombatComponent>();
            if (combat == null)
            {
                return;
            }

            if (!BattleSkillHelper.TrySelectBestAutoSkillPlan(owner, out BattleSkillHelper.AutoCastPlan autoCastPlan))
            {
                self.Reset();
                owner.GetComponent<BattleMoveComponent>()?.StopMove();
                return;
            }

            self.CurrentTarget = autoCastPlan.Target;

            if (BattleSkillHelper.IsInSkillRange(owner, autoCastPlan.Target, autoCastPlan.TargetingConfig))
            {
                owner.GetComponent<BattleMoveComponent>()?.StopMove();
                self.State = BattleActionState.Attacking;
                self.TryAttack(owner, autoCastPlan);
                return;
            }

            self.State = BattleActionState.Chasing;
            owner.GetComponent<BattleMoveComponent>()?.StartMove(autoCastPlan.DesiredCastPosition);
        }

        private static void TryAttack(this BattleActionDecisionComponent self, BattleUnit attacker, BattleSkillHelper.AutoCastPlan autoCastPlan)
        {
            if (!BattleSkillHelper.TryExecuteSkill(attacker, autoCastPlan.SkillId, autoCastPlan.Target.Id, out BattleSkillHelper.SkillExecutionResult executionResult))
            {
                return;
            }

            Log.Debug($"自动技能释放: Attacker={attacker.Id}, SkillId={executionResult.SkillId}, Target={autoCastPlan.Target.Id}, Damage={executionResult.TotalDamage}");
        }
    }
}
