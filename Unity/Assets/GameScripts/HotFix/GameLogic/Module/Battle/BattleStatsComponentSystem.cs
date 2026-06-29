namespace ET
{
    [EntitySystemOf(typeof(BattleStatsComponent))]
    [FriendOf(typeof(BattleStatsComponent))]
    public static partial class BattleStatsComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleStatsComponent self)
        {
            self.Clear();
        }

        [EntitySystem]
        private static void Destroy(this BattleStatsComponent self)
        {
            self.Clear();
        }

        public static BattleStatsComponent GetOrCreateBattleStats(this BattleUnit unit)
        {
            if (unit == null)
            {
                return null;
            }

            BattleStatsComponent stats = unit.GetComponent<BattleStatsComponent>() ?? unit.AddComponent<BattleStatsComponent>();
            NumericComponent numeric = unit.GetComponent<NumericComponent>();
            // 兼容旧创建链路：部分 BattleUnit 仍先写 NumericComponent，再延迟补 BattleStatsComponent。
            if (numeric != null && stats.MaxHp == 0 && stats.Hp == 0)
            {
                stats.LoadFromNumeric(numeric);
            }

            return stats;
        }

        public static void LoadFromNumeric(this BattleStatsComponent self, NumericComponent numeric)
        {
            if (self == null || numeric == null)
            {
                return;
            }

            self.Hp = numeric.GetAsInt(NumericType.Hp);
            self.MaxHp = numeric.GetAsInt(NumericType.MaxHp);
            self.Attack = numeric.GetAsInt(NumericType.Attack);
            self.Defense = numeric.GetAsInt(NumericType.Defense);
            self.Speed = numeric.GetAsFloat(NumericType.Speed);
            self.CritRate = numeric.GetAsInt(NumericType.CritRate);
            self.CritDamage = numeric.GetAsInt(NumericType.CritDamage);
            self.AttackSpeed = numeric.GetAsInt(NumericType.AttackSpeed);
            self.LifeSteal = numeric.GetAsInt(NumericType.LifeSteal);
            self.DamageReduction = numeric.GetAsInt(NumericType.DamageReduction);
            self.Penetration = numeric.GetAsInt(NumericType.Penetration);
            self.HpRegen = numeric.GetAsInt(NumericType.HpRegen);
            self.MpRegen = numeric.GetAsInt(NumericType.MpRegen);
        }

        public static void SetCore(this BattleStatsComponent self, int hp, int maxHp, int attack, int defense, float speed, bool mirrorNumeric)
        {
            if (self == null)
            {
                return;
            }

            self.Hp = hp;
            self.MaxHp = maxHp;
            self.Attack = attack;
            self.Defense = defense;
            self.Speed = speed;

            if (!mirrorNumeric)
            {
                return;
            }

            // 过渡期保留 Numeric 镜像，现有 UI 和事件监听仍依赖 NumericComponent。
            BattleUnit unit = self.GetParent<BattleUnit>();
            unit?.SetNumeric(NumericType.Hp, hp);
            unit?.SetNumeric(NumericType.MaxHp, maxHp);
            unit?.SetNumeric(NumericType.Attack, attack);
            unit?.SetNumeric(NumericType.Defense, defense);
            unit?.GetComponent<NumericComponent>()?.Set(NumericType.Speed, speed);
        }

        public static void SetHpMax(this BattleStatsComponent self, int hp, int maxHp, bool mirrorNumeric)
        {
            if (self == null)
            {
                return;
            }

            self.MaxHp = maxHp;
            self.Hp = hp < 0 ? 0 : hp;
            if (self.MaxHp > 0 && self.Hp > self.MaxHp)
            {
                self.Hp = self.MaxHp;
            }

            if (!mirrorNumeric)
            {
                return;
            }

            // 服务端血量同步要继续触发旧的 BattleUnitNumericChange，保证血条刷新路径不变。
            BattleUnit unit = self.GetParent<BattleUnit>();
            unit?.SetNumeric(NumericType.MaxHp, self.MaxHp);
            unit?.SetNumeric(NumericType.Hp, self.Hp);
        }

        public static void SetHp(this BattleStatsComponent self, int hp, bool mirrorNumeric)
        {
            if (self == null)
            {
                return;
            }

            self.Hp = hp < 0 ? 0 : hp;
            if (self.MaxHp > 0 && self.Hp > self.MaxHp)
            {
                self.Hp = self.MaxHp;
            }

            if (mirrorNumeric)
            {
                self.GetParent<BattleUnit>()?.SetNumeric(NumericType.Hp, self.Hp);
            }
        }

        private static void Clear(this BattleStatsComponent self)
        {
            self.Hp = 0;
            self.MaxHp = 0;
            self.Attack = 0;
            self.Defense = 0;
            self.Speed = 0f;
            self.CritRate = 0;
            self.CritDamage = 0;
            self.AttackSpeed = 0;
            self.LifeSteal = 0;
            self.DamageReduction = 0;
            self.Penetration = 0;
            self.HpRegen = 0;
            self.MpRegen = 0;
            self.Shield = 0;
        }
    }
}
