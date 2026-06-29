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

            List<BattleUnit> tickUnits = new();
            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsDead) continue;
                if (unit.Camp != UnitCamp.Enemy) continue;

                ClientMinionAIComponent ai = unit.GetComponent<ClientMinionAIComponent>();
                if (ai == null) continue;

                tickUnits.Add(unit);
            }

            // 先收集单位快照，再执行 AI。AI 可能创建 AttackInstance 子实体。
            foreach (BattleUnit unit in tickUnits)
            {
                if (unit == null || unit.IsDisposed || unit.IsDead) continue;

                ClientMinionAIComponent ai = unit.GetComponent<ClientMinionAIComponent>();
                if (ai == null) continue;

                ai.Tick(battle, now);

                // tick 载具 Buff（过期清理和 DOT tick）
                VehicleBuffComponent vehicleBuff = unit.GetComponent<VehicleBuffComponent>();
                vehicleBuff?.OnTick(now);
            }
        }
    }
}
