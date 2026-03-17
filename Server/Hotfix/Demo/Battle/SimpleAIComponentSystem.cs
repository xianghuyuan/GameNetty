using System.Collections.Generic;
using System.Numerics;

namespace ET.Server
{
    [EntitySystemOf(typeof(SimpleAIComponent))]
    [FriendOf(typeof(SimpleAIComponent))]
    [FriendOf(typeof(BattleUnit))]
    public static partial class SimpleAIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SimpleAIComponent self)
        {
            self.State = AIState.Idle;
            self.DetectRange = 10.0f;
            self.MoveSpeed = 3.0f;
            self.LastUpdateTime = TimeInfo.Instance.ServerFrameTime();
        }
        
        [EntitySystem]
        private static void Destroy(this SimpleAIComponent self)
        {
            self.CurrentTarget = null;
            self.State = AIState.Idle;
        }
        
        public static void Update(this SimpleAIComponent self)
        {
            BattleUnit owner = self.GetParent<BattleUnit>();
            if (owner == null || owner.IsDead)
            {
                return;
            }
            
            BattleUnitCombatComponent combat = owner.GetComponent<BattleUnitCombatComponent>();
            if (combat == null)
            {
                return;
            }
            
            BattleUnit target = self.CurrentTarget;
            if (target == null || target.IsDead)
            {
                target = self.FindNearestEnemy(owner);
                self.CurrentTarget = target;
                
                if (target == null)
                {
                    self.State = AIState.Idle;
                    return;
                }
            }
            
            float distance = self.GetDistance(owner.Position, target.Position);
            
            if (distance <= combat.AttackRange)
            {
                self.State = AIState.Attacking;
                self.TryAttack(owner, target, combat);
            }
            else
            {
                self.State = AIState.Chasing;
                self.MoveToward(owner, target.Position);
            }
        }
        
        private static BattleUnit FindNearestEnemy(this SimpleAIComponent self, BattleUnit owner)
        {
            BattleRoom battleRoom = owner.GetParent<BattleRoom>();
            if (battleRoom == null)
            {
                return null;
            }
            
            UnitCamp enemyCamp = owner.Camp == UnitCamp.Friend ? UnitCamp.Enemy : UnitCamp.Friend;
            
            BattleUnit nearestEnemy = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var kv in battleRoom.Units)
            {
                BattleUnit enemy = kv.Value;
                if (enemy == null || enemy.IsDead || enemy.Camp != enemyCamp)
                {
                    continue;
                }
                
                float distance = self.GetDistance(owner.Position, enemy.Position);
                if (distance < nearestDistance && distance <= self.DetectRange)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
            
            return nearestEnemy;
        }
        
        private static float GetDistance(this SimpleAIComponent self, Vector3 pos1, Vector3 pos2)
        {
            float dx = pos2.X - pos1.X;
            float dz = pos2.Z - pos1.Z;
            return (float)System.Math.Sqrt(dx * dx + dz * dz);
        }
        
        private static void MoveToward(this SimpleAIComponent self, BattleUnit owner, Vector3 targetPos)
        {
            Vector3 direction = targetPos - owner.Position;
            float distance = (float)System.Math.Sqrt(direction.X * direction.X + direction.Z * direction.Z);
            
            if (distance < 0.01f)
            {
                return;
            }
            
            direction = direction / distance;
            
            long currentTime = TimeInfo.Instance.ServerFrameTime();
            long deltaTime = currentTime - self.LastUpdateTime;
            self.LastUpdateTime = currentTime;
            
            float moveDistance = self.MoveSpeed * deltaTime / 1000f;
            if (moveDistance > distance)
            {
                moveDistance = distance;
            }
            
            owner.Position = owner.Position + direction * moveDistance;
        }
        
        private static void TryAttack(this SimpleAIComponent self, BattleUnit attacker, BattleUnit target, BattleUnitCombatComponent combat)
        {
            if (!combat.IsAttackReady())
            {
                return;
            }
            
            int damage = self.CalculateDamage(attacker, target);
            target.TakeDamage(damage);
            
            combat.StartAttackCooldown();
            
            Log.Debug($"AI攻击: Attacker={attacker.Id}, Target={target.Id}, Damage={damage}");
        }
        
        private static int CalculateDamage(this SimpleAIComponent self, BattleUnit attacker, BattleUnit target)
        {
            NumericComponent attackerNumeric = attacker.GetComponent<NumericComponent>();
            NumericComponent targetNumeric = target.GetComponent<NumericComponent>();
            
            if (attackerNumeric == null || targetNumeric == null)
            {
                return 1;
            }
            
            int attack = attackerNumeric.GetAsInt(NumericType.Attack);
            int defense = targetNumeric.GetAsInt(NumericType.Defense);
            
            int damage = attack - defense;
            if (damage < 1)
            {
                damage = 1;
            }
            
            return damage;
        }
    }
}
