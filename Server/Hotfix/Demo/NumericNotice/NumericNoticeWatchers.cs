namespace ET.Server
{
    public static class NumericNoticeWatcherHelper
    {
        public static void Notice(BattleUnit unit, NumbericChange args)
        {
            BattleRoom battleRoom = unit.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return;
            }

            Scene mapScene = battleRoom.Root();
            UnitComponent unitComponent = mapScene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return;
            }

            M2C_NoticeUnitNumeric noticeMessage = M2C_NoticeUnitNumeric.Create();
            noticeMessage.UnitId = unit.Id;
            noticeMessage.NumericType = args.NumericType;
            noticeMessage.NewValue = args.New;

            foreach (long playerId in battleRoom.PlayerIds)
            {
                Unit player = unitComponent.Get(playerId);
                if (player != null)
                {
                    MapMessageHelper.SendToClient(player, noticeMessage);
                }
            }
        }
    }

    [NumericWatcher(SceneType.Battle, NumericType.Hp)]
    public class HpNumericWatcher : INumericWatcher
    {
        public void Run(BattleUnit unit, NumbericChange args)
        {
            NumericNoticeWatcherHelper.Notice(unit, args);
        }
    }

    [NumericWatcher(SceneType.Battle, NumericType.MaxHp)]
    public class MaxHpNumericWatcher : INumericWatcher
    {
        public void Run(BattleUnit unit, NumbericChange args)
        {
            NumericNoticeWatcherHelper.Notice(unit, args);
        }
    }

    [NumericWatcher(SceneType.Battle, NumericType.Mp)]
    public class MpNumericWatcher : INumericWatcher
    {
        public void Run(BattleUnit unit, NumbericChange args)
        {
            NumericNoticeWatcherHelper.Notice(unit, args);
        }
    }

    [NumericWatcher(SceneType.Battle, NumericType.MaxMp)]
    public class MaxMpNumericWatcher : INumericWatcher
    {
        public void Run(BattleUnit unit, NumbericChange args)
        {
            NumericNoticeWatcherHelper.Notice(unit, args);
        }
    }

    [NumericWatcher(SceneType.Battle, NumericType.Attack)]
    public class AttackNumericWatcher : INumericWatcher
    {
        public void Run(BattleUnit unit, NumbericChange args)
        {
            NumericNoticeWatcherHelper.Notice(unit, args);
        }
    }
}
