using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(ClientPlayerAITickComponent))]
    [FriendOf(typeof(ClientPlayerAITickComponent))]
    [FriendOf(typeof(ClientPlayerAIComponent))]
    public static partial class ClientPlayerAITickComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientPlayerAITickComponent self)
        {
            self.LastTickTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this ClientPlayerAITickComponent self)
        {
            self.LastTickTime = 0;
        }

        [EntitySystem]
        private static void Update(this ClientPlayerAITickComponent self)
        {
            long now = TimeInfo.Instance.ClientNow();
            if (self.LastTickTime == 0)
            {
                self.LastTickTime = now;
            }

            if (now - self.LastTickTime < ClientPlayerAITickComponent.TICK_INTERVAL)
            {
                return;
            }

            self.LastTickTime = now;

            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State != BattleState.Fighting)
            {
                return;
            }

            List<BattleUnit> tickUnits = new();
            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsDead) continue;
                if (unit.Camp != UnitCamp.Friend) continue;

                ClientPlayerAIComponent ai = unit.GetComponent<ClientPlayerAIComponent>();
                if (ai == null) continue;

                tickUnits.Add(unit);
            }

            // 先收集单位快照，再执行 AI。AI 可能创建 AttackInstance 子实体。
            foreach (BattleUnit unit in tickUnits)
            {
                if (unit == null || unit.IsDisposed || unit.IsDead) continue;

                ClientPlayerAIComponent ai = unit.GetComponent<ClientPlayerAIComponent>();
                if (ai == null) continue;

                ai.Tick(battle, now);
            }
        }
    }
}
