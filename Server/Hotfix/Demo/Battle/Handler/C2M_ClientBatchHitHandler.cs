using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 客户端批量命中请求处理器 - 杂兵系统
    /// 杂兵位置由客户端权威管理，服务端不校验碰撞，只做基础合法性检查后执行伤害。
    /// 伤害结果累积到 SkillTimelineComponent 的批量下发列表，由定时器统一推送。
    /// </summary>
    [MessageLocationHandler(SceneType.Map)]
    [FriendOf(typeof(BattleRoom))]
    [FriendOf(typeof(BattleUnit))]
    public class C2M_ClientBatchHitHandler : MessageLocationHandler<Unit, C2M_ClientBatchHit>
    {
        protected override async ETTask Run(Unit unit, C2M_ClientBatchHit message)
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

            BattleUnit caster = battleRoom.GetUnit(unit.Id);
            if (caster == null || caster.IsDead)
            {
                return;
            }

            SkillTimelineComponent timeline = battleRoom.GetComponent<SkillTimelineComponent>();
            if (timeline == null)
            {
                return;
            }

            EmitterConfig skillConfig = EmitterConfigCategory.Instance.GetOrDefault(message.skillId);
            if (skillConfig == null || !skillConfig.IsEnabled)
            {
                return;
            }

            foreach (long hitUnitId in message.hitUnitIds)
            {
                BattleUnit target = battleRoom.GetUnit(hitUnitId);
                if (target == null || target.IsDead || target.Camp == caster.Camp)
                {
                    continue;
                }

                // 不对Boss执行客户端预判（Boss走服务端权威路径）
                if (target.IsBoss)
                {
                    continue;
                }

                int damage = BattleSkillHelper.ApplyEmitterDamage(caster, target, skillConfig);

                var batchResult = new BatchDamageResult
                {
                    AttackerId = unit.Id,
                    SkillId = message.skillId,
                    TargetId = target.Id,
                    Damage = damage,
                    DamageType = skillConfig.EmitterKind == 1 ? 0 : 1,
                    TargetDead = target.IsDead,
                };

                BattleStatsComponent targetStats = target.GetOrCreateBattleStats();
                batchResult.TargetCurrentHp = targetStats?.Hp ?? 0;
                batchResult.TargetMaxHp = targetStats?.MaxHp ?? 0;

                timeline.AccumulatedResults.Add(batchResult);
            }

            await ETTask.CompletedTask;
        }
    }
}
