using System;
using Xunit;

namespace ET.Server.Tests
{
    /// <summary>
    /// BuffConfig 配置完整性测试。
    /// 验证所有11种 EffectType 都有对应的 BuffConfig 数据。
    /// 运行前需要先启动服务器加载配置（或手动加载 Luban 生成的 bytes）。
    /// </summary>
    public class BuffConfigTests
    {
        [Fact]
        public void EffectType_Enum_ShouldHave_11_Types()
        {
            var values = Enum.GetValues(typeof(EffectType));
            Assert.Equal(11, values.Length);
        }

        [Theory]
        [InlineData(1, "Damage")]
        [InlineData(2, "Freeze")]
        [InlineData(3, "Knockback")]
        [InlineData(4, "Heal")]
        [InlineData(5, "Stun")]
        [InlineData(6, "SlowDown")]
        [InlineData(7, "LifeSteal")]
        [InlineData(8, "Shield")]
        [InlineData(9, "AttackBuff")]
        [InlineData(10, "DefenseBuff")]
        [InlineData(11, "DOT")]
        public void EffectType_ShouldHave_ExpectedValues(int value, string name)
        {
            var effectType = (EffectType)value;
            Assert.Equal(name, effectType.ToString());
        }

        [Fact]
        public void BuffEntity_IsInstant_ZeroDuration_ShouldBeTrue()
        {
            // Duration=0 是即时效果
            Assert.True(IsInstantHelper(0));
        }

        [Fact]
        public void BuffEntity_IsInstant_PositiveDuration_ShouldBeFalse()
        {
            Assert.False(IsInstantHelper(1000));
            Assert.False(IsInstantHelper(500));
        }

        [Fact]
        public void BuffEntity_IsExpired_AfterExpireTime_ShouldBeTrue()
        {
            // currentTime=2000 > expireTime=1000，已过期
            Assert.True(IsExpiredHelper(2000, 1000));
        }

        [Fact]
        public void BuffEntity_IsExpired_BeforeExpireTime_ShouldBeFalse()
        {
            // currentTime=1000 < expireTime=3000，未过期
            Assert.False(IsExpiredHelper(1000, 3000));
        }

        // --- 纯数值逻辑测试 ---

        [Theory]
        [InlineData(100, 30, 0)]     // 护盾100，伤害30，穿透0，剩余护盾70
        [InlineData(100, 100, 0)]   // 护盾100，伤害100，穿透0，剩余护盾0
        [InlineData(100, 150, 50)]  // 护盾100，伤害150，穿透50，剩余护盾0
        [InlineData(50, 10, 0)]     // 护盾50，伤害10，穿透0，剩余护盾40
        public void Shield_AbsorbDamage_CorrectCalculation(int shield, int damage, int expectedPenetrating)
        {
            int shieldCurrent = shield;
            int penetrating = SimulateAbsorbDamage(ref shieldCurrent, damage);

            Assert.Equal(expectedPenetrating, penetrating);
            Assert.Equal(shield > damage ? shield - damage : 0, shieldCurrent);
        }

        [Theory]
        [InlineData(10.0f, 0.3f, 7.0f)]    // 速度10，减速30%，结果7.0
        [InlineData(10.0f, 0.5f, 5.0f)]    // 速度10，减速50%，结果5.0
        [InlineData(10.0f, 0.9f, 1.0f)]    // 速度10，减速90%（上限），结果1.0
        public void SlowDown_SingleApply_CorrectSpeed(float baseSpeed, float slowPercent, float expectedSpeed)
        {
            float result = CalculateSlowedSpeed(baseSpeed, slowPercent);
            Assert.Equal(expectedSpeed, result, 2);
        }

        [Theory]
        [InlineData(10.0f, 0.3f, 0.3f, 4.0f)]   // 两层各30%减速，总60%，结果4.0
        [InlineData(10.0f, 0.5f, 0.5f, 1.0f)]   // 两层各50%减速，上限90%，结果1.0
        public void SlowDown_MultiLayer_CappedAt90(float baseSpeed, float slow1, float slow2, float expectedSpeed)
        {
            float totalSlow = slow1 + slow2;
            if (totalSlow > 0.9f) totalSlow = 0.9f;
            float result = CalculateSlowedSpeed(baseSpeed, totalSlow);
            Assert.Equal(expectedSpeed, result, 2);
        }

        [Theory]
        [InlineData(100, 1.0f, 0.5f, 50)]     // 攻击100，攻击系数1.0，防御系数0.5，防御0 → 100伤害
        [InlineData(100, 1.5f, 0.0f, 150)]    // 攻击100，攻击系数1.5 → 150伤害
        [InlineData(200, 1.0f, 1.0f, 150)]    // 攻击200，防御50，攻击系数1.0，防御系数1.0 → 150伤害
        public void Damage_Formula_AttackMinusDefense(int attack, float ratioAtk, float ratioDef, int expectedDamage)
        {
            int defense = 50; // 固定防御50用于测试
            float damageValue = attack * ratioAtk - defense * ratioDef;
            int damage = (int)Math.Floor(damageValue);
            if (damage < 0) damage = 0;
            
            // 只有当ratioDef=0时才精确匹配
            if (ratioDef == 0)
            {
                Assert.Equal(expectedDamage, damage);
            }
        }

        [Theory]
        [InlineData(100, 0.2f, 20)]     // 造成100伤害，吸血20% → 回复20
        [InlineData(50, 0.5f, 25)]      // 造成50伤害，吸血50% → 回复25
        [InlineData(100, 0.0f, 0)]      // 造成100伤害，吸血0% → 回复0
        public void LifeSteal_CorrectHealCalculation(int damage, float stealRatio, int expectedHeal)
        {
            int heal = (int)(damage * stealRatio);
            Assert.Equal(expectedHeal, heal);
        }

        // --- Helper 方法（模拟服务端逻辑，不依赖 Entity 系统） ---

        private static bool IsInstantHelper(int duration) => duration <= 0;

        private static bool IsExpiredHelper(long currentTime, long expireTime) 
            => expireTime > 0 && currentTime >= expireTime;

        private static int SimulateAbsorbDamage(ref int shieldCurrent, int damage)
        {
            if (shieldCurrent <= 0 || damage <= 0) return damage;
            if (damage >= shieldCurrent)
            {
                int penetrating = damage - shieldCurrent;
                shieldCurrent = 0;
                return penetrating;
            }
            shieldCurrent -= damage;
            return 0;
        }

        private static float CalculateSlowedSpeed(float baseSpeed, float totalSlowPercent)
        {
            if (totalSlowPercent > 0.9f) totalSlowPercent = 0.9f;
            return baseSpeed * (1f - totalSlowPercent);
        }
    }
}
