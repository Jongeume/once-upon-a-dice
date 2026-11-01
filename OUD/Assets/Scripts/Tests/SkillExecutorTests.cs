// SkillExecutorTests.cs
// feature-spec F-04 스킬 실행 테스트.
// NUnit 기반 (Unity Test Framework).
using System.Collections.Generic;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class SkillExecutorTests
    {
        private SkillExecutor _executor;

        // 테스트 기준 스탯: B-1 [가정 기반] HP60/ATK6/DEF5
        private const int TEST_ATK = 6;
        private const int TEST_DEF = 5;

        [SetUp]
        public void SetUp()
        {
            _executor = new SkillExecutor();
        }

        // ── 공용 헬퍼 ─────────────────────────────────────────────────────────

        private static MonsterInstance MakeEnemy(int hp = 20, int shield = 0)
        {
            var data = new MonsterData(
                id: "Slime", name: "Slime", maxHp: hp, baseAtk: 8, shieldValue: 0,
                pattern: new[] { IntentType.Attack });
            var inst = new MonsterInstance(data);
            if (shield > 0) inst.GainShield(shield);
            return inst;
        }

        private static PlayerState MakePlayer()
        {
            var stats = new PlayerStats(maxHp: 60, atk: TEST_ATK, def: TEST_DEF);
            return new PlayerState(stats);
        }

        // ── Quick Strike (OnePair, 1.0x, Single) ─────────────────────────────

        [Test]
        public void QuickStrike_ATK6_Mult1_0_Deals6Damage()
        {
            // ATK=6, 배율 1.0 → floor(6) = 6
            var skill   = SkillDatabase.Get(HandType.OnePair, SkillCategory.Attack);
            var enemy   = MakeEnemy(hp: 20);
            var enemies = new List<MonsterInstance> { enemy };

            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, enemy, enemies);

            Assert.AreEqual(1,  result.Damages.Count);
            Assert.AreEqual(6,  result.Damages[0].Damage);
            Assert.AreEqual(14, enemy.Hp);
        }

        [Test]
        public void QuickStrike_TargetDead_NoEffect()
        {
            var skill   = SkillDatabase.Get(HandType.OnePair, SkillCategory.Attack);
            var enemy   = MakeEnemy(hp: 20);
            enemy.TakeDamage(999); // 사망
            var enemies = new List<MonsterInstance> { enemy };

            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, enemy, enemies);

            Assert.AreEqual(0, result.Damages.Count); // 허공 소멸
        }

        // ── Twin Slash (TwoPair, 0.8x ×2, Single) ────────────────────────────

        [Test]
        public void TwinSlash_ATK6_TwoHits_EachFloor()
        {
            // floor(6 × 0.8) = 4. 2회 → 총 8 데미지
            var skill   = SkillDatabase.Get(HandType.TwoPair, SkillCategory.Attack);
            var enemy   = MakeEnemy(hp: 20);
            var enemies = new List<MonsterInstance> { enemy };

            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, enemy, enemies);

            Assert.AreEqual(2,  result.Damages.Count);
            Assert.AreEqual(4,  result.Damages[0].Damage);
            Assert.AreEqual(4,  result.Damages[1].Damage);
            Assert.AreEqual(12, enemy.Hp); // 20 - 4 - 4
        }

        [Test]
        public void TwinSlash_Shield5_IndependentHitJudgment()
        {
            // 실드 5 vs 4+4: 1타 실드 1 남음, 2타 실드 0 + HP -= 3
            var skill   = SkillDatabase.Get(HandType.TwoPair, SkillCategory.Attack);
            var enemy   = MakeEnemy(hp: 20, shield: 5);
            var enemies = new List<MonsterInstance> { enemy };

            _executor.ExecuteAttack(skill, 0, TEST_ATK, enemy, enemies);

            Assert.AreEqual(0,  enemy.Shield);
            Assert.AreEqual(17, enemy.Hp); // 20 - 3
        }

        // ── Heavy Blow (Triple, 1.5x, Single) ────────────────────────────────

        [Test]
        public void HeavyBlow_ATK6_Deals9Damage()
        {
            // floor(6 × 1.5) = 9
            var skill   = SkillDatabase.Get(HandType.Triple, SkillCategory.Attack);
            var enemy   = MakeEnemy(hp: 20);
            var enemies = new List<MonsterInstance> { enemy };

            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, enemy, enemies);

            Assert.AreEqual(9,  result.Damages[0].Damage);
            Assert.AreEqual(11, enemy.Hp);
        }

        // ── Crushing Wave (FullHouse, 1.2x+0.5x, 주+스플래시) ────────────────

        [Test]
        public void CrushingWave_MainPlusSplash()
        {
            // ATK=6: 주 대상 floor(6×1.2)=7, 스플래시 floor(6×0.5)=3
            var skill    = SkillDatabase.Get(HandType.FullHouse, SkillCategory.Attack);
            var main     = MakeEnemy(hp: 20);
            var splashA  = MakeEnemy(hp: 20);
            var splashB  = MakeEnemy(hp: 20);
            var enemies  = new List<MonsterInstance> { main, splashA, splashB };

            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, main, enemies);

            // 주 대상 7 데미지, 스플래시 2명 각 3 데미지
            Assert.AreEqual(3,  result.Damages.Count);
            Assert.AreEqual(7,  result.Damages[0].Damage);
            Assert.AreEqual(3,  result.Damages[1].Damage);
            Assert.AreEqual(3,  result.Damages[2].Damage);
            Assert.AreEqual(13, main.Hp);    // 20 - 7
            Assert.AreEqual(17, splashA.Hp); // 20 - 3
            Assert.AreEqual(17, splashB.Hp); // 20 - 3
        }

        [Test]
        public void CrushingWave_DeadSplashTarget_Skipped()
        {
            // 스플래시 대상 중 사망한 적은 건너뜀
            var skill   = SkillDatabase.Get(HandType.FullHouse, SkillCategory.Attack);
            var main    = MakeEnemy(hp: 20);
            var dead    = MakeEnemy(hp: 20);
            dead.TakeDamage(999);
            var alive   = MakeEnemy(hp: 20);
            var enemies = new List<MonsterInstance> { main, dead, alive };

            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, main, enemies);

            // 주 대상 + 살아있는 스플래시 1명 = 2개 히트
            Assert.AreEqual(2, result.Damages.Count);
            Assert.AreEqual(0, result.Damages[0].TargetIndex); // main
            Assert.AreEqual(2, result.Damages[1].TargetIndex); // alive
        }

        // ── Sweeping Edge (SmallStraight, 0.8x, AllEnemies) ──────────────────

        [Test]
        public void SweepingEdge_HitsAllAliveEnemies()
        {
            // floor(6 × 0.8) = 4, 전체 적
            var skill   = SkillDatabase.Get(HandType.SmallStraight, SkillCategory.Attack);
            var enemyA  = MakeEnemy(hp: 20);
            var enemyB  = MakeEnemy(hp: 20);
            var dead    = MakeEnemy(hp: 20);
            dead.TakeDamage(999);
            var enemies = new List<MonsterInstance> { enemyA, enemyB, dead };

            // AllEnemies 스킬은 target=null
            var result = _executor.ExecuteAttack(skill, 0, TEST_ATK, null, enemies);

            Assert.AreEqual(2,  result.Damages.Count); // 사망한 적 건너뜀
            Assert.AreEqual(4,  result.Damages[0].Damage);
            Assert.AreEqual(16, enemyA.Hp);
            Assert.AreEqual(16, enemyB.Hp);
        }

        // ── 강화 레벨 반영 ────────────────────────────────────────────────────

        [Test]
        public void EnhanceLevel1_AddsPointTwo_ToMultiplier()
        {
            // QuickStrike 기본 1.0x + 강화 Lv2(enhanceLevel=1) → 1.2x
            // floor(6 × 1.2) = 7
            var skill   = SkillDatabase.Get(HandType.OnePair, SkillCategory.Attack);
            var enemy   = MakeEnemy(hp: 20);
            var enemies = new List<MonsterInstance> { enemy };

            _executor.ExecuteAttack(skill, 1, TEST_ATK, enemy, enemies);

            Assert.AreEqual(13, enemy.Hp); // 20 - 7
        }

        // ── 수비 스킬 ─────────────────────────────────────────────────────────

        [Test]
        public void Brace_DEF5_Mult1_0_Gives5Shield()
        {
            // floor(5 × 1.0) = 5 실드
            var skill   = SkillDatabase.Get(HandType.OnePair, SkillCategory.Defense);
            var player  = MakePlayer();

            var result = _executor.ExecuteDefense(skill, 0, TEST_DEF, player);

            Assert.AreEqual(5, result.ShieldGained);
            Assert.AreEqual(5, player.Shield);
        }

        [Test]
        public void DualGuard_DEF5_Mult1_4_Gives7Shield()
        {
            // Two Pair 수비: floor(5 × 1.4) = 7 실드 (OnePair 5 < TwoPair 7 < Triple 9)
            var skill   = SkillDatabase.Get(HandType.TwoPair, SkillCategory.Defense);
            var player  = MakePlayer();

            var result = _executor.ExecuteDefense(skill, 0, TEST_DEF, player);

            Assert.AreEqual(7, result.ShieldGained);
            Assert.AreEqual(7, player.Shield);
        }

        [Test]
        public void IronWall_GivesShieldAndHpRecover()
        {
            // Iron Wall: floor(5 × 2.0) = 10 실드 + HP 3 회복
            var skill   = SkillDatabase.Get(HandType.FullHouse, SkillCategory.Defense);
            var player  = MakePlayer();
            player.TakeDamage(10); // HP 50

            var result = _executor.ExecuteDefense(skill, 0, TEST_DEF, player);

            Assert.AreEqual(10, result.ShieldGained);
            Assert.AreEqual(3,  result.HpRecovered);
            Assert.AreEqual(53, player.Hp); // 50 + 3
        }

        [Test]
        public void Defense_HpRecoverClampsToMaxHp()
        {
            // HP 59/60에서 Iron Wall HP+3 → HP 60 (1 소멸)
            var skill   = SkillDatabase.Get(HandType.FullHouse, SkillCategory.Defense);
            var player  = MakePlayer();
            player.TakeDamage(1); // HP 59

            _executor.ExecuteDefense(skill, 0, TEST_DEF, player);

            Assert.AreEqual(60, player.Hp); // 최대 HP 초과 불가
        }

        [Test]
        public void Defense_ShieldStacks_SameTurn()
        {
            // 같은 턴 수비 2번 → 실드 합산
            var brace  = SkillDatabase.Get(HandType.OnePair,  SkillCategory.Defense);
            var fortify = SkillDatabase.Get(HandType.Triple, SkillCategory.Defense);
            var player  = MakePlayer();

            _executor.ExecuteDefense(brace,   0, TEST_DEF, player); // 5
            _executor.ExecuteDefense(fortify, 0, TEST_DEF, player); // floor(5×1.8)=9

            Assert.AreEqual(14, player.Shield); // 5 + 9
        }
    }
}
