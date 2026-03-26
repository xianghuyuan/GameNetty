namespace ET.Server
{
    [Event(SceneType.Battle)]
    [FriendOf(typeof(CastingComponent))]
    public class CombatComponent_OnRequestCast : AEvent<BattleRoom, RequestCastEvent>
    {
        protected override async ETTask Run(BattleRoom scene,
        RequestCastEvent args)
        {
            BattleUnit unit = args.Unit;

            // 冻结或施法中，不执行
            FreezeComponent freeze = unit.GetComponent<FreezeComponent>();
            if (freeze != null && freeze.IsFrozen)
            {
                await ETTask.CompletedTask;
                return;
            }

            CastingComponent casting = unit.GetComponent<CastingComponent>();
            if (casting != null && casting.IsCasting)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!BattleSkillHelper.CanAutoCastSkill(unit, args.SkillId))
            {
                await ETTask.CompletedTask;
                return;
            }

            // 调用技能执行
            if (BattleSkillHelper.TryExecuteSkill(unit, args.SkillId,
                args.TargetId, out BattleSkillHelper.SkillExecutionResult result, true))
            {
                // 技能成功，挂载施法锁定
                if (casting == null)
                {
                    casting = unit.AddComponent<CastingComponent>();
                }

                SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(args.SkillId);
                int lockDuration = CastingComponentSystem.GetCastLockDuration(skillConfig?.CooldownMs ?? 1000);
                casting.ApplyCasting(args.SkillId, lockDuration);
            }

            await ETTask.CompletedTask;
        }
    }
    
    [EntitySystemOf(typeof(BattleUnitCombatComponent))]
    [FriendOf(typeof(BattleUnitCombatComponent))]
    public static partial class BattleUnitCombatComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleUnitCombatComponent self)
        {
            self.AttackCooldown = 1000;
            self.LastAttackTime = 0;
            self.AttackRange = 2.0f;
            self.CanAttack = true;
            self.SkillCooldownEnds.Clear();
        }
        
        [EntitySystem]
        private static void Destroy(this BattleUnitCombatComponent self)
        {
            self.LastAttackTime = 0;
            self.CanAttack = false;
            self.SkillCooldownEnds.Clear();
        }
        
        public static bool IsAttackReady(this BattleUnitCombatComponent self)
        {
            if (!self.CanAttack)
            {
                return false;
            }
            
            long currentTime = TimeInfo.Instance.ServerFrameTime();
            return currentTime >= self.LastAttackTime + self.AttackCooldown;
        }
        
        public static void StartAttackCooldown(this BattleUnitCombatComponent self)
        {
            self.LastAttackTime = TimeInfo.Instance.ServerFrameTime();
        }
        
        public static void SetAttackCooldown(this BattleUnitCombatComponent self, int cooldownMs)
        {
            self.AttackCooldown = cooldownMs;
        }
        
        public static void SetAttackRange(this BattleUnitCombatComponent self, float range)
        {
            self.AttackRange = range;
        }

        public static bool IsSkillReady(this BattleUnitCombatComponent self, SkillConfig skillConfig)
        {
            if (skillConfig == null || !self.CanAttack)
            {
                return false;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            int cooldownKey = skillConfig.CooldownGroupId != 0 ? skillConfig.CooldownGroupId : skillConfig.Id;
            if (!self.SkillCooldownEnds.TryGetValue(cooldownKey, out long cooldownEnd))
            {
                return true;
            }

            return currentTime >= cooldownEnd;
        }

        public static long StartSkillCooldown(this BattleUnitCombatComponent self, SkillConfig skillConfig)
        {
            long cooldownEnd = TimeInfo.Instance.ServerFrameTime() + (skillConfig?.CooldownMs ?? self.AttackCooldown);
            if (skillConfig != null)
            {
                int cooldownKey = skillConfig.CooldownGroupId != 0 ? skillConfig.CooldownGroupId : skillConfig.Id;
                self.SkillCooldownEnds[cooldownKey] = cooldownEnd;
                self.AttackCooldown = skillConfig.CooldownMs;
            }

            self.LastAttackTime = TimeInfo.Instance.ServerFrameTime();
            return cooldownEnd;
        }
    }
}
