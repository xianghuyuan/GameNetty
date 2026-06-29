namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_BatchDamageHandler : MessageHandler<Scene, M2C_BatchDamage>
    {
        protected override async ETTask Run(Scene root, M2C_BatchDamage message)
        {
            Log.Debug($"[M2C_BatchDamage] received: battleId={message.battleId} damageCount={message.damages?.Count ?? 0} deadCount={message.deadUnitIds?.Count ?? 0}");

            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            if (battle == null)
            {
                Log.Warning($"[M2C_BatchDamage] battle not found for battleId={message.battleId}");
                await ETTask.CompletedTask;
                return;
            }

            var serverAlive = new System.Collections.Generic.HashSet<long>();

            // 处理伤害列表：用服务端权威HP覆盖本地值
            if (message.damages != null)
            {
                foreach (var info in message.damages)
                {
                    BattleUnit target = battle.GetChild<BattleUnit>(info.targetId);
                    if (target == null) continue;

                    // 服务端确认该单位存活（有伤害信息）
                    serverAlive.Add(info.targetId);

                    target.GetOrCreateBattleStats().SetHpMax(info.targetCurrentHp, info.targetMaxHp, true);

                    // 仅在单位未死亡时发布受击事件（乐观击杀已发布过）
                    if (!target.IsDead)
                    {
                        EventSystem.Instance.Publish(root, new BattleUnitDamaged
                        {
                            Unit = target,
                            AttackerId = info.attackerId,
                            Damage = info.damage,
                            IsCrit = info.damageType == 1,
                        });
                    }
                }
            }

            // 处理死亡单位
            if (message.deadUnitIds != null)
            {
                foreach (long deadId in message.deadUnitIds)
                {
                    serverAlive.Remove(deadId);

                    BattleUnit deadUnit = battle.GetChild<BattleUnit>(deadId);
                    if (deadUnit == null || deadUnit.IsDead) continue;

                    deadUnit.IsDead = true;
                    EventSystem.Instance.Publish(root, new BattleUnitDead { BattleUnit = deadUnit });
                }
            }

            // 纠错：客户端乐观击杀但服务端认为存活的单位，执行复活
            foreach (long unitId in serverAlive)
            {
                BattleUnit unit = battle.GetChild<BattleUnit>(unitId);
                if (unit == null || !unit.IsDead) continue;

                Log.Warning($"[M2C_BatchDamage] Server correction: reviving unit {unitId}");
                unit.IsDead = false;
                EventSystem.Instance.Publish(root, new BattleUnitRevived { BattleUnit = unit });
            }

            await ETTask.CompletedTask;
        }
    }
}
