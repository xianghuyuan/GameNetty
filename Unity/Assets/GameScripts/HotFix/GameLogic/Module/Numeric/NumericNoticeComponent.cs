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

    public static class NumericNoticeMessageHelper
    {
        public static string GetNumericTypeName(int numericType)
        {
            return numericType switch
            {
                NumericType.Speed => "Speed/速度",
                NumericType.Hp => "Hp/生命值",
                NumericType.MaxHp => "MaxHp/最大生命值",
                NumericType.AOI => "AOI/视野范围",
                NumericType.Level => "Level/等级",
                NumericType.Attack => "Attack/攻击力",
                NumericType.Defense => "Defense/防御力",
                NumericType.Mp => "Mp/魔法值",
                NumericType.MaxMp => "MaxMp/最大魔法值",
                NumericType.CritRate => "CritRate/暴击率",
                NumericType.CritDamage => "CritDamage/暴击伤害",
                NumericType.AttackSpeed => "AttackSpeed/攻速",
                NumericType.LifeSteal => "LifeSteal/吸血",
                NumericType.DamageReduction => "DamageReduction/伤害减免",
                NumericType.Penetration => "Penetration/穿透",
                NumericType.HpRegen => "HpRegen/生命回复",
                NumericType.MpRegen => "MpRegen/魔法回复",
                NumericType.Exp => "Exp/经验值",
                _ => $"Unknown/{numericType}",
            };
        }

        public static string FormatSingleNumericNotice(M2C_NoticeUnitNumeric message)
        {
            return $"[收到消息注释] M2C_NoticeUnitNumeric -> 单位 {message.UnitId} 的 {GetNumericTypeName(message.NumericType)}({message.NumericType}) 同步为 {message.NewValue}";
        }

        public static string FormatMultiNumericNotice(M2C_NoticeUnitNumericList message)
        {
            int count = message.NumericTypeList.Count;
            if (message.NewValueList.Count < count)
            {
                count = message.NewValueList.Count;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append($"[收到消息注释] M2C_NoticeUnitNumericList -> 单位 {message.UnitId} 数值同步:");
            for (int i = 0; i < count; ++i)
            {
                if (i > 0)
                {
                    builder.Append("; ");
                }

                int numericType = message.NumericTypeList[i];
                long newValue = message.NewValueList[i];
                builder.Append($"{GetNumericTypeName(numericType)}({numericType})={newValue}");
            }

            return builder.ToString();
        }

        public static Unit GetUnit(Scene root, long unitId)
        {
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            Scene currentScene = currentScenesComponent?.Scene;
            if (currentScene == null)
            {
                return null;
            }

            UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
            return unitComponent?.Get(unitId);
        }

        public static BattleUnit GetBattleUnit(Scene root, long unitId)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            return battle?.GetChild<BattleUnit>(unitId);
        }
    }

    [MessageHandler(SceneType.Main)]
    public class M2C_NoticeUnitNumericHandler : MessageHandler<Scene, M2C_NoticeUnitNumeric>
    {
        protected override async ETTask Run(Scene root, M2C_NoticeUnitNumeric message)
        {
            Log.Info(NumericNoticeMessageHelper.FormatSingleNumericNotice(message));

            Unit unit = NumericNoticeMessageHelper.GetUnit(root, message.UnitId);
            if (unit != null)
            {
                NumericComponent numericComponent = unit.GetComponent<NumericComponent>() ?? unit.AddComponent<NumericComponent>();
                numericComponent[message.NumericType] = message.NewValue;
                await ETTask.CompletedTask;
                return;
            }

            BattleUnit battleUnit = NumericNoticeMessageHelper.GetBattleUnit(root, message.UnitId);
            if (battleUnit != null)
            {
                battleUnit.SetNumeric(message.NumericType, message.NewValue);
            }

            await ETTask.CompletedTask;
        }
    }

    [MessageHandler(SceneType.Main)]
    public class M2C_NoticeUnitNumericListHandler : MessageHandler<Scene, M2C_NoticeUnitNumericList>
    {
        protected override async ETTask Run(Scene root, M2C_NoticeUnitNumericList message)
        {
            Log.Info(NumericNoticeMessageHelper.FormatMultiNumericNotice(message));

            Unit unit = NumericNoticeMessageHelper.GetUnit(root, message.UnitId);
            if (unit != null)
            {
                NumericComponent numericComponent = unit.GetComponent<NumericComponent>() ?? unit.AddComponent<NumericComponent>();
                int count = message.NumericTypeList.Count;
                if (message.NewValueList.Count < count)
                {
                    count = message.NewValueList.Count;
                }

                for (int i = 0; i < count; ++i)
                {
                    numericComponent[message.NumericTypeList[i]] = message.NewValueList[i];
                }

                await ETTask.CompletedTask;
                return;
            }

            BattleUnit battleUnit = NumericNoticeMessageHelper.GetBattleUnit(root, message.UnitId);
            int battleCount = message.NumericTypeList.Count;
            if (message.NewValueList.Count < battleCount)
            {
                battleCount = message.NewValueList.Count;
            }

            if (battleUnit != null)
            {
                for (int i = 0; i < battleCount; ++i)
                {
                    battleUnit.SetNumeric(message.NumericTypeList[i], message.NewValueList[i]);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
