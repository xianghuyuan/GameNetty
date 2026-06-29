namespace ET
{
    [EntitySystemOf(typeof(BattleAttackInstance))]
    [FriendOf(typeof(BattleAttackInstance))]
    public static partial class BattleAttackInstanceSystem
    {
        [EntitySystem]
        private static void Awake(this BattleAttackInstance self, long casterId, long targetId, BattleAttackRuntime attackRuntime)
        {
            self.CasterId = casterId;
            self.TargetId = targetId;
            self.AttackRuntime = attackRuntime;
            self.DeliveryType = attackRuntime?.DeliveryType ?? BattleAttackDeliveryType.Instant;
            self.State = BattleAttackInstanceState.Created;
            self.StartTimeMs = TimeInfo.Instance.ClientNow();
            self.ResolveTimeMs = 0;
            self.FinishTimeMs = 0;
        }

        [EntitySystem]
        private static void Destroy(this BattleAttackInstance self)
        {
            self.CasterId = 0;
            self.TargetId = 0;
            self.AttackRuntime = null;
            self.DeliveryType = BattleAttackDeliveryType.Instant;
            self.State = BattleAttackInstanceState.None;
            self.StartTimeMs = 0;
            self.ResolveTimeMs = 0;
            self.FinishTimeMs = 0;
        }

        public static void MarkWaitingForHit(this BattleAttackInstance self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.State = BattleAttackInstanceState.WaitingForHit;
        }

        public static void ResolveHit(this BattleAttackInstance self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            switch (self.DeliveryType)
            {
                case BattleAttackDeliveryType.Instant:
                    self.ResolveInstantHit();
                    break;

                default:
                    Log.Error($"BattleAttackInstance unsupported delivery type: {self.DeliveryType}");
                    self.Finish();
                    break;
            }
        }

        private static void ResolveInstantHit(this BattleAttackInstance self)
        {
            if (self == null || self.IsDisposed || self.State == BattleAttackInstanceState.Finished)
            {
                return;
            }

            Battle battle = self.GetParent<Battle>();
            BattleUnit caster = battle?.GetChild<BattleUnit>(self.CasterId);
            BattleUnit target = battle?.GetChild<BattleUnit>(self.TargetId);
            if (battle == null || caster == null || caster.IsDisposed || caster.IsDead ||
                target == null || target.IsDisposed || target.IsDead)
            {
                self.Finish();
                return;
            }

            self.State = BattleAttackInstanceState.Resolved;
            self.ResolveTimeMs = TimeInfo.Instance.ClientNow();
            BattleAttackExecutor.Execute(new BattleHitContext
            {
                Battle = battle,
                AttackInstance = self,
                Attacker = caster,
                Target = target,
                AttackRuntime = self.AttackRuntime,
            });
            self.Finish();
        }

        private static void Finish(this BattleAttackInstance self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.State = BattleAttackInstanceState.Finished;
            self.FinishTimeMs = TimeInfo.Instance.ClientNow();
            self.Dispose();
        }
    }
}
