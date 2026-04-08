using System.Collections.Generic;

namespace ET
{
    [EntitySystemOf(typeof(ClientMinionAITickComponent))]
    [FriendOf(typeof(ClientMinionAITickComponent))]
    [FriendOf(typeof(BattleUnit))]
    [FriendOf(typeof(ClientMinionAIComponent))]
    public static partial class ClientMinionAITickComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ClientMinionAITickComponent self)
        {
            self.LastTickTime = 0;
        }

        [EntitySystem]
        private static void Destroy(this ClientMinionAITickComponent self)
        {
            self.LastTickTime = 0;
        }

        [EntitySystem]
        private static void Update(this ClientMinionAITickComponent self)
        {
            long now = TimeInfo.Instance.ClientNow();
            if (self.LastTickTime == 0)
            {
                self.LastTickTime = now;
            }

            if (now - self.LastTickTime < ClientMinionAITickComponent.TICK_INTERVAL)
            {
                return;
            }

            self.LastTickTime = now;

            Battle battle = self.GetParent<Battle>();
            if (battle == null || battle.State != BattleState.Fighting)
            {
                return;
            }

            // 遍历所有带 ClientMinionAIComponent 的敌方 BattleUnit，执行 AI Tick
            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsDead) continue;
                if (unit.Camp != UnitCamp.Enemy) continue;

                ClientMinionAIComponent ai = unit.GetComponent<ClientMinionAIComponent>();
                if (ai == null) continue;

                ai.Tick(battle, now);
            }
        }
    }
}
