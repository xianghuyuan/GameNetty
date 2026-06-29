using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(BattleFlowComponent))]
    [FriendOf(typeof(BattleFlowComponent))]
    [FriendOf(typeof(Battle))]
    public static partial class BattleFlowComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleFlowComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            self.PreviousState = battle != null ? battle.State : BattleState.None;
            self.LastTransitionTime = 0;
            self.LastEndSuccess = null;
        }

        [EntitySystem]
        private static void Destroy(this BattleFlowComponent self)
        {
            self.PreviousState = BattleState.None;
            self.LastTransitionTime = 0;
            self.LastEndSuccess = null;
        }

        public static void StartBattle(this BattleFlowComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State == BattleState.Fighting)
            {
                return;
            }

            self.RecordTransition();
            battle.State = BattleState.Fighting;
            battle.StartTime = TimeInfo.Instance.ClientNow();
            battle.EndTime = 0;
            self.LastEndSuccess = null;

            BattleMoveDebugLog.StartSession(battle.BattleId, battle.BattleType);

            // 流程控制器负责调度整场战斗共享的驱动器。
            if (battle.GetComponent<ClientPlayerAITickComponent>() == null)
            {
                battle.AddComponent<ClientPlayerAITickComponent>();
            }

            Log.Info($"战斗开始: BattleId={battle.BattleId}, Type={battle.BattleType}");
            EventSystem.Instance.Publish(battle.Scene(), new BattleStart { Battle = battle });
        }

        public static void PauseBattle(this BattleFlowComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State != BattleState.Fighting)
            {
                return;
            }

            self.RecordTransition();
            battle.State = BattleState.Paused;
            Log.Info($"战斗暂停: BattleId={battle.BattleId}");
        }

        public static void ResumeBattle(this BattleFlowComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State != BattleState.Paused)
            {
                return;
            }

            self.RecordTransition();
            battle.State = BattleState.Fighting;
            Log.Info($"战斗恢复: BattleId={battle.BattleId}");
        }

        public static void EndBattle(this BattleFlowComponent self, bool success)
        {
            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State == BattleState.Ended)
            {
                return;
            }

            self.RecordTransition();
            self.LastEndSuccess = success;

            battle.State = BattleState.Ended;
            battle.EndTime = TimeInfo.Instance.ClientNow();

            int duration = (int)((battle.EndTime - battle.StartTime) / 1000);

            Log.Info($"战斗结束: BattleId={battle.BattleId}, Success={success}, Duration={duration}s");

            BattleMoveDebugLog.CleanupBattle(battle.BattleId);

            BattleResult result = new BattleResult
            {
                Success = success,
                Duration = duration,
                Exp = success ? 100 : 0,
                Drops = new List<ItemDrop>(),
                PlayerDamage = new Dictionary<long, int>()
            };

            EventSystem.Instance.Publish(battle.Scene(), new BattleEnd { Battle = battle, Result = result });
        }

        private static void RecordTransition(this BattleFlowComponent self)
        {
            Battle battle = self.GetParent<Battle>();
            self.PreviousState = battle != null ? battle.State : BattleState.None;
            self.LastTransitionTime = TimeInfo.Instance.ClientNow();
        }
    }
}
