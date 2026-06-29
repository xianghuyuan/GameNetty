namespace ET.Server
{
    [Invoke(TimerInvokeType.ShieldEnd)]
    public class ShieldEndTimer : ATimer<ShieldComponent>
    {
        protected override void Run(ShieldComponent self)
        {
            self.RemoveShield();
        }
    }

    [EntitySystemOf(typeof(ShieldComponent))]
    [FriendOf(typeof(ShieldComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class ShieldComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ShieldComponent self)
        {
            self.ShieldCurrentAmount = 0;
            self.ShieldTimerId = 0;
        }

        [EntitySystem]
        private static void Destroy(this ShieldComponent self)
        {
            self.RemoveShield();
        }

        /// <summary>
        /// 应用护盾效果。shieldAmount = 护盾吸收量，durationMs = 持续时间(ms)。
        /// 护盾通过独立字段管理，不修改 MaxHp，在 TakeDamage 时先扣护盾。
        /// 已有护盾时叠加（增加剩余护盾量并刷新时长）。
        /// </summary>
        public static void ApplyShield(this ShieldComponent self, int shieldAmount, int durationMs)
        {
            if (shieldAmount <= 0) return;

            BattleUnit unit = self.GetParent<BattleUnit>();

            // 移除旧定时器
            if (self.ShieldTimerId != 0)
            {
                long oldTimerId = self.ShieldTimerId;
                unit.Root().GetComponent<TimerComponent>()?.Remove(ref oldTimerId);
            }

            self.ShieldCurrentAmount += shieldAmount;

            // 注册定时器，护盾到期清除
            long endTime = TimeInfo.Instance.ServerFrameTime() + durationMs;
            self.ShieldTimerId = unit.Root().GetComponent<TimerComponent>()
                ?.NewOnceTimer(endTime, TimerInvokeType.ShieldEnd, self) ?? 0;
        }

        /// <summary>
        /// 尝试用护盾吸收伤害。返回实际穿透到HP的伤害值。
        /// </summary>
        public static int AbsorbDamage(this ShieldComponent self, int damage)
        {
            if (!self.IsActive || damage <= 0)
            {
                return damage;
            }

            if (damage >= self.ShieldCurrentAmount)
            {
                int penetrating = damage - self.ShieldCurrentAmount;
                self.ShieldCurrentAmount = 0;
                return penetrating;
            }

            self.ShieldCurrentAmount -= damage;
            return 0;
        }

        /// <summary>
        /// 移除护盾（到期或组件销毁时调用）
        /// </summary>
        public static void RemoveShield(this ShieldComponent self)
        {
            self.ShieldCurrentAmount = 0;

            if (self.ShieldTimerId != 0)
            {
                BattleUnit unit = self.GetParent<BattleUnit>();
                if (unit != null && !unit.IsDisposed)
                {
                    long oldTimerId = self.ShieldTimerId;
                    unit.Root().GetComponent<TimerComponent>()?.Remove(ref oldTimerId);
                }
                self.ShieldTimerId = 0;
            }
        }
    }
}
