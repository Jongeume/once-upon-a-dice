// DamageCalculatorTests.cs
// feature-spec F-04 테스트 케이스 전체 커버.
// NUnit 기반 (Unity Test Framework).
using NUnit.Framework;
using OUD.BattleEngine.Combat;

namespace OUD.Tests
{
    [TestFixture]
    public class DamageCalculatorTests
    {
        // ── CalcValue ─────────────────────────────────────────────────────────

        [Test]
        public void CalcValue_ATK6_Mult1_0_Returns6()
        {
            Assert.AreEqual(6, DamageCalculator.CalcValue(6, 1.0));
        }

        [Test]
        public void CalcValue_ATK6_Mult1_5_Returns9()
        {
            // floor(6 × 1.5) = floor(9.0) = 9
            Assert.AreEqual(9, DamageCalculator.CalcValue(6, 1.5));
        }

        [Test]
        public void CalcValue_ATK6_Mult0_8_Returns4()
        {
            // floor(6 × 0.8) = floor(4.8) = 4
            Assert.AreEqual(4, DamageCalculator.CalcValue(6, 0.8));
        }

        [Test]
        public void CalcValue_DEF5_Mult1_3_Returns6()
        {
            // floor(5 × 1.3) = floor(6.5) = 6
            Assert.AreEqual(6, DamageCalculator.CalcValue(5, 1.3));
        }

        [Test]
        public void CalcValue_ZeroStat_Returns0()
        {
            Assert.AreEqual(0, DamageCalculator.CalcValue(0, 2.0));
        }

        [Test]
        public void CalcValue_ZeroMultiplier_Returns0()
        {
            Assert.AreEqual(0, DamageCalculator.CalcValue(6, 0.0));
        }

        // ── ApplyDamage: 실드 초과 ─────────────────────────────────────────────

        [Test]
        public void ApplyDamage_DamageLessThanShield_ReducesShieldOnly()
        {
            // 데미지 4 vs 실드 5 → 실드 1, HP 변화 없음
            int shield = 5, hp = 20;
            var (shieldAbs, hpDmg) = DamageCalculator.ApplyDamage(4, ref shield, ref hp);

            Assert.AreEqual(1,  shield);
            Assert.AreEqual(20, hp);
            Assert.AreEqual(4,  shieldAbs);
            Assert.AreEqual(0,  hpDmg);
        }

        [Test]
        public void ApplyDamage_DamageGreaterThanShield_ReducesBoth()
        {
            // 데미지 8 vs 실드 5 → 실드 0, HP -= 3
            int shield = 5, hp = 20;
            var (shieldAbs, hpDmg) = DamageCalculator.ApplyDamage(8, ref shield, ref hp);

            Assert.AreEqual(0,  shield);
            Assert.AreEqual(17, hp);
            Assert.AreEqual(5,  shieldAbs);
            Assert.AreEqual(3,  hpDmg);
        }

        [Test]
        public void ApplyDamage_NoShield_AllGoesToHp()
        {
            int shield = 0, hp = 20;
            var (shieldAbs, hpDmg) = DamageCalculator.ApplyDamage(6, ref shield, ref hp);

            Assert.AreEqual(0,  shield);
            Assert.AreEqual(14, hp);
            Assert.AreEqual(0,  shieldAbs);
            Assert.AreEqual(6,  hpDmg);
        }

        [Test]
        public void ApplyDamage_ZeroDamage_NoChange()
        {
            int shield = 5, hp = 20;
            var (shieldAbs, hpDmg) = DamageCalculator.ApplyDamage(0, ref shield, ref hp);

            Assert.AreEqual(5,  shield);
            Assert.AreEqual(20, hp);
            Assert.AreEqual(0,  shieldAbs);
            Assert.AreEqual(0,  hpDmg);
        }

        [Test]
        public void ApplyDamage_HpDoesNotGoBelowZero()
        {
            // 데미지가 HP보다 클 때 HP가 음수가 되면 안 됨
            int shield = 0, hp = 3;
            DamageCalculator.ApplyDamage(10, ref shield, ref hp);

            Assert.AreEqual(0, hp);
        }

        // ── Twin Slash 다중 히트 (히트별 독립 판정) ────────────────────────────

        [Test]
        public void ApplyDamage_TwinSlash_IndependentHitJudgment()
        {
            // Twin Slash: floor(6 × 0.8) = 4, 2회
            // 실드 5 → 1타: 실드 4→1, HP 변화 없음 / 2타: 실드 1→0, HP -= 3
            int shield = 5, hp = 20;
            int hit = DamageCalculator.CalcValue(6, 0.8); // 4

            DamageCalculator.ApplyDamage(hit, ref shield, ref hp);
            Assert.AreEqual(1, shield); // 1타 후 실드 1

            DamageCalculator.ApplyDamage(hit, ref shield, ref hp);
            Assert.AreEqual(0,  shield); // 2타 후 실드 0
            Assert.AreEqual(17, hp);     // HP -= 3
        }

        // ── ApplyHeal ─────────────────────────────────────────────────────────

        [Test]
        public void ApplyHeal_NormalHeal_IncreasesHp()
        {
            int hp = 55;
            int actual = DamageCalculator.ApplyHeal(ref hp, 3, 60);

            Assert.AreEqual(58, hp);
            Assert.AreEqual(3,  actual);
        }

        [Test]
        public void ApplyHeal_ExceedsMaxHp_ClampsToMax()
        {
            // HP 59/60에서 HP 3 회복 → HP 60 (1 소멸)
            int hp = 59;
            int actual = DamageCalculator.ApplyHeal(ref hp, 3, 60);

            Assert.AreEqual(60, hp);
            Assert.AreEqual(1,  actual); // 실제 회복량 1
        }

        [Test]
        public void ApplyHeal_AlreadyMaxHp_NoChange()
        {
            int hp = 60;
            int actual = DamageCalculator.ApplyHeal(ref hp, 5, 60);

            Assert.AreEqual(60, hp);
            Assert.AreEqual(0,  actual);
        }

        [Test]
        public void ApplyHeal_ZeroAmount_NoChange()
        {
            int hp = 50;
            int actual = DamageCalculator.ApplyHeal(ref hp, 0, 60);

            Assert.AreEqual(50, hp);
            Assert.AreEqual(0,  actual);
        }
    }
}
