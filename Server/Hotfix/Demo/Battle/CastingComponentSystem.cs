namespace ET.Server
{
    [Invoke(TimerInvokeType.CastingEnd)]
    public class CastingEndTimer : ATimer<CastingComponent>
    {
        protected override void Run(CastingComponent self)
        {
            self.EndCasting();
        }
    }

    [EntitySystemOf(typeof(CastingComponent))]
    [FriendOf(typeof(CastingComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class CastingComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CastingComponent self)
        {
            self.IsCasting = false;
            self.CastEndTime = 0;
            self.SkillId = 0;
        }

        [EntitySystem]
        private static void Destroy(this CastingComponent self)
        {
            self.IsCasting = false;
            self.CastEndTime = 0;
            self.SkillId = 0;
        }

        /// <summary>
        /// 应用施法锁定
        /// </summary>
        public static void ApplyCasting(this CastingComponent self, int skillId, int durationMs)
        {
            if (self.IsCasting)
            {
                return;
            }

            self.IsCasting = true;
            self.SkillId = skillId;
            self.CastEndTime = TimeInfo.Instance.ServerFrameTime() + durationMs;

            self.Root().GetComponent<TimerComponent>()?.NewOnceTimer(self.CastEndTime, TimerInvokeType.CastingEnd, self);
        }

        /// <summary>
        /// 结束施法锁定
        /// </summary>
        public static void EndCasting(this CastingComponent self)
        {
            if (!self.IsCasting)
            {
                return;
            }

            self.IsCasting = false;
            self.CastEndTime = 0;
            self.SkillId = 0;

            BattleUnit unit = self.GetParent<BattleUnit>();

            EventSystem.Instance.Publish(unit.Root(), new CastingEndEvent { Unit = unit });
        }

        /// <summary>
        /// 计算施法锁定时长：min(CooldownMs / 2, 400)，至少 200ms
        /// </summary>
        public static int GetCastLockDuration(int cooldownMs)
        {
            int duration = System.Math.Min(cooldownMs / 2, 400);
            return System.Math.Max(duration, 200);
        }
    }
}
