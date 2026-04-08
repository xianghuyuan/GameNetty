using System.Numerics;

namespace ET.Server
{
    [MessageLocationHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_SkillTimelineAttackHandler : MessageLocationHandler<Unit, C2M_SkillTimelineAttack>
    {
        protected override async ETTask Run(Unit unit, C2M_SkillTimelineAttack message)
        {
            Scene mapScene = unit.Scene();

            BattleRoomManagerComponent roomManager = mapScene.GetComponent<BattleRoomManagerComponent>();
            if (roomManager == null)
            {
                return;
            }

            BattleRoom battleRoom = roomManager.GetBattleRoomByUnitId(unit.Id);
            if (battleRoom == null)
            {
                return;
            }

            BattleUnit caster = BattleSkillHelper.FindPlayerBattleUnit(battleRoom, unit.Id);
            if (caster == null || caster.IsDead)
            {
                return;
            }

            CastingComponent casting = caster.GetComponent<CastingComponent>();
            if (casting != null && casting.IsCasting)
            {
                return;
            }

            BattleUnitCombatComponent combat = caster.GetComponent<BattleUnitCombatComponent>();
            if (combat == null)
            {
                return;
            }

            SkillConfig skillConfig = SkillConfigCategory.Instance.GetOrDefault(message.skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return;
            }

            if (!combat.IsSkillReady(skillConfig))
            {
                return;
            }

            float faceDir = message.faceDir != 0 ? message.faceDir : 1f;
            float casterX = caster.Position.X;

            float hitBoxMinX = message.hitBoxMinX;
            float hitBoxMaxX = message.hitBoxMaxX;

            if (hitBoxMinX == 0 && hitBoxMaxX == 0)
            {
                SkillTargetingConfig targetingConfig = skillConfig.TargetingConfigIdConfig;
                if (targetingConfig != null)
                {
                    float castRange = targetingConfig.CastRange;
                    if (faceDir > 0)
                    {
                        hitBoxMinX = casterX;
                        hitBoxMaxX = casterX + castRange;
                    }
                    else
                    {
                        hitBoxMinX = casterX - castRange;
                        hitBoxMaxX = casterX;
                    }
                }
            }

            SkillTimelineComponent timeline = battleRoom.GetComponent<SkillTimelineComponent>();
            if (timeline == null)
            {
                return;
            }

            long currentTime = TimeInfo.Instance.ServerFrameTime();
            long hitStartMs = currentTime + (long)(message.hitStartTick * 1000f);
            long hitEndMs = currentTime + (long)(message.hitEndTick * 1000f);

            timeline.RegisterHitBox(
                caster.Id,
                message.skillId,
                message.targetId,
                hitStartMs,
                hitEndMs,
                hitBoxMinX,
                hitBoxMaxX
            );

            BattleUnitHelper.BroadcastSkillCast(caster, message.skillId, message.targetId,
                new Vector3(casterX + faceDir * 2f, caster.Position.Y, caster.Position.Z));

            combat.StartSkillCooldown(skillConfig);

            NetworkStateMachineComponent networkState = caster.GetComponent<NetworkStateMachineComponent>();
            networkState?.OnSkillRequestSent();

            await ETTask.CompletedTask;
        }
    }
}
