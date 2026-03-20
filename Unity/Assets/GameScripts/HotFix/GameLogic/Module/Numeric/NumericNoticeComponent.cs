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
