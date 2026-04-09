namespace ET
{
    [MessageHandler(SceneType.Main)]
    public class M2C_BossDamageHandler : MessageHandler<Scene, M2C_BossDamage>
    {
        protected override async ETTask Run(Scene root, M2C_BossDamage message)
        {
            BattleComponent battleComponent = root.GetComponent<BattleComponent>();
            Battle battle = battleComponent?.GetCurrentBattle();
            if (battle == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 在当前战斗中查找Boss单位
            BattleUnit boss = null;
            foreach (var child in battle.Children.Values)
            {
                if (child is not BattleUnit unit) continue;
                if (unit.IsBoss)
                {
                    boss = unit;
                    break;
                }
            }

            if (boss == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            boss.SetNumeric(NumericType.MaxHp, message.bossMaxHp);
            boss.SetNumeric(NumericType.Hp, message.bossCurrentHp);

            EventSystem.Instance.Publish(root, new BattleUnitDamaged
            {
                Unit = boss,
                AttackerId = 0,
                Damage = message.totalDamage,
                IsCrit = message.damageType == 1,
            });

            await ETTask.CompletedTask;
        }
    }
}
