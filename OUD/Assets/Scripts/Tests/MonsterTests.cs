// MonsterTests.cs
// feature-spec F-06 (몬스터 패턴 순환) + F-07 (Stone Golem 분노) 테스트.
// NUnit 기반 (Unity Test Framework).
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class MonsterTests
    {
        // ── F-06: 패턴 순환 ───────────────────────────────────────────────────

        [Test]
        public void Slime_Pattern_Cycles_Attack_Attack_Attack()
        {
            // 4턴 = 공격, 공격, 공격, (순환) 공격
            var slime = MonsterDatabase.Create(MonsterDatabase.ID_SLIME);

            Assert.AreEqual(IntentType.Attack, slime.GetCurrentIntent()); slime.AdvancePattern();
            Assert.AreEqual(IntentType.Attack, slime.GetCurrentIntent()); slime.AdvancePattern();
            Assert.AreEqual(IntentType.Attack, slime.GetCurrentIntent()); slime.AdvancePattern();
            Assert.AreEqual(IntentType.Attack, slime.GetCurrentIntent()); // 순환
        }

        [Test]
        public void Skeleton_Pattern_Cycles_Attack_Shield_Attack()
        {
            // feature-spec F-06: 1턴 공격(10), 2턴 실드(5), 3턴 공격(10), 4턴 실드(5)
            var skeleton = MonsterDatabase.Create(MonsterDatabase.ID_SKELETON);

            Assert.AreEqual(IntentType.Attack, skeleton.GetCurrentIntent());
            Assert.AreEqual(10,                skeleton.GetIntentValue());
            skeleton.AdvancePattern();

            Assert.AreEqual(IntentType.Shield, skeleton.GetCurrentIntent());
            Assert.AreEqual(5,                 skeleton.GetIntentValue());
            skeleton.AdvancePattern();

            Assert.AreEqual(IntentType.Attack, skeleton.GetCurrentIntent());
            Assert.AreEqual(10,                skeleton.GetIntentValue());
            skeleton.AdvancePattern();

            Assert.AreEqual(IntentType.Shield, skeleton.GetCurrentIntent()); // 순환
            Assert.AreEqual(5,                 skeleton.GetIntentValue());
        }

        [Test]
        public void Intent_MatchesActualAction()
        {
            // Intent와 실제 행동 수치 일치 검증
            var slime = MonsterDatabase.Create(MonsterDatabase.ID_SLIME);
            Assert.AreEqual(IntentType.Attack, slime.GetCurrentIntent());
            Assert.AreEqual(8, slime.GetIntentValue()); // ATK=8
        }

        // ── F-07: Stone Golem 기본 패턴 ─────────────────────────────────────

        [Test]
        public void Golem_BasePattern_Attack_Shield_Attack_StrongAttack()
        {
            // 기본 패턴: 공격(14) → 실드(10) → 공격(14) → 강공격(21)
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);

            Assert.AreEqual(IntentType.Attack,       golem.GetCurrentIntent());
            Assert.AreEqual(14,                      golem.GetIntentValue());
            golem.AdvancePattern();

            Assert.AreEqual(IntentType.Shield,       golem.GetCurrentIntent());
            Assert.AreEqual(10,                      golem.GetIntentValue());
            golem.AdvancePattern();

            Assert.AreEqual(IntentType.Attack,       golem.GetCurrentIntent());
            Assert.AreEqual(14,                      golem.GetIntentValue());
            golem.AdvancePattern();

            // 강공격: floor(14 × 1.5) = floor(21.0) = 21
            Assert.AreEqual(IntentType.StrongAttack, golem.GetCurrentIntent());
            Assert.AreEqual(21,                      golem.GetIntentValue());
            golem.AdvancePattern();

            // 순환
            Assert.AreEqual(IntentType.Attack,       golem.GetCurrentIntent());
        }

        // ── F-07: 분노 전환 ───────────────────────────────────────────────────

        [Test]
        public void Golem_Rage_TriggersAt_HpLessOrEqualThreshold()
        {
            // HP ≤ 40 → 적 턴 시작 시 분노 전환
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(40); // HP = 80 - 40 = 40

            Assert.IsFalse(golem.IsEnraged); // 아직 플레이어 턴 중 → 미전환

            golem.CheckRage(); // 적 턴 시작 시 체크

            Assert.IsTrue(golem.IsEnraged);
            Assert.AreEqual(18, golem.Atk); // ATK 14 + 4 = 18
        }

        [Test]
        public void Golem_Rage_NotTriggered_WhenHpAboveThreshold()
        {
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(39); // HP = 41

            golem.CheckRage();

            Assert.IsFalse(golem.IsEnraged);
            Assert.AreEqual(14, golem.Atk); // 변화 없음
        }

        [Test]
        public void Golem_Rage_PatternResets_OnTrigger()
        {
            // 분노 전환 시 패턴 인덱스가 0으로 리셋 → 분노 패턴 처음부터
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.AdvancePattern(); // 패턴 인덱스 1로 이동
            golem.TakeDamage(40);   // HP = 40

            golem.CheckRage();

            // 분노 패턴 첫 번째 = 공격(18)
            Assert.AreEqual(IntentType.Attack, golem.GetCurrentIntent());
            Assert.AreEqual(18,                golem.GetIntentValue());
        }

        [Test]
        public void Golem_Rage_Pattern_Attack_StrongAttack_Attack()
        {
            // 분노 패턴: 공격(18) → 강공격(floor(18×1.5)=27) → 공격(18) → 반복
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(40);
            golem.CheckRage();

            Assert.AreEqual(IntentType.Attack,       golem.GetCurrentIntent());
            Assert.AreEqual(18,                      golem.GetIntentValue());
            golem.AdvancePattern();

            // 강공격: floor(18 × 1.5) = 27
            Assert.AreEqual(IntentType.StrongAttack, golem.GetCurrentIntent());
            Assert.AreEqual(27,                      golem.GetIntentValue());
            golem.AdvancePattern();

            Assert.AreEqual(IntentType.Attack,       golem.GetCurrentIntent());
            Assert.AreEqual(18,                      golem.GetIntentValue());
            golem.AdvancePattern();

            // 순환
            Assert.AreEqual(IntentType.Attack,       golem.GetCurrentIntent());
        }

        [Test]
        public void Golem_Rage_IsPermanent_CannotRevert()
        {
            // 분노 전환 후 회복 불가 (영구)
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(40);
            golem.CheckRage();
            Assert.IsTrue(golem.IsEnraged);

            // HP가 다시 올라가도 분노 유지 (게임에서 HP 회복은 없지만 방어적 검증)
            golem.CheckRage(); // 두 번 호출해도 ATK 중복 증가 없음
            Assert.AreEqual(18, golem.Atk); // 14+4=18, 중복 없이 유지
        }

        [Test]
        public void Golem_PlayerTurnKill_RageTriggersNextEnemyTurn()
        {
            // 플레이어 턴에 HP 50→35로 감소 → 그 적 턴은 기본 패턴, 다음 적 턴에 분노
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(45); // HP = 35, 분노 조건 충족

            // 플레이어 턴 중 → 아직 CheckRage 미호출 → 분노 미전환
            Assert.IsFalse(golem.IsEnraged);
            Assert.AreEqual(IntentType.Attack, golem.GetCurrentIntent()); // 기본 패턴 유지

            // 다음 적 턴 시작 시 체크
            golem.CheckRage();
            Assert.IsTrue(golem.IsEnraged);
        }

        // ── F-07: 분노 경고 (IntentSystem) ───────────────────────────────────

        [Test]
        public void Golem_RageWarning_ShowsAt_HP41()
        {
            // HP 41 → 기본 패턴 유지 + 불꽃 경고 표시 (40 < 41 ≤ 48)
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(39); // HP = 41

            var display = IntentSystem.GetDisplay(golem);

            Assert.IsFalse(golem.IsEnraged);
            Assert.IsTrue(display.ShowRageWarning);
            Assert.AreEqual(IntentType.Attack, display.Type);
        }

        [Test]
        public void Golem_RageWarning_ShowsAt_HP48()
        {
            // HP 48 → 경고 구간 상한 (floor(80 × 0.6) = 48)
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(32); // HP = 48

            var display = IntentSystem.GetDisplay(golem);
            Assert.IsTrue(display.ShowRageWarning);
        }

        [Test]
        public void Golem_RageWarning_NotShownAt_HP49()
        {
            // HP 49 → 경고 구간 초과 (48보다 큰 HP)
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(31); // HP = 49

            var display = IntentSystem.GetDisplay(golem);
            Assert.IsFalse(display.ShowRageWarning);
        }

        [Test]
        public void Golem_RageWarning_NotShownWhenAlreadyEnraged()
        {
            // 분노 후 → 경고 표시 없음 (이미 분노 중)
            var golem = MonsterDatabase.Create(MonsterDatabase.ID_STONE_GOLEM);
            golem.TakeDamage(40);
            golem.CheckRage(); // 분노 전환

            var display = IntentSystem.GetDisplay(golem);
            Assert.IsFalse(display.ShowRageWarning);
        }

        [Test]
        public void Slime_NoRageWarning_Ever()
        {
            // 일반 몬스터는 분노 없음 → 항상 false
            var slime = MonsterDatabase.Create(MonsterDatabase.ID_SLIME);
            slime.TakeDamage(19); // HP 1까지 감소

            var display = IntentSystem.GetDisplay(slime);
            Assert.IsFalse(display.ShowRageWarning);
        }

        // ── 데미지 / 실드 기본 동작 ──────────────────────────────────────────

        [Test]
        public void Monster_TakeDamage_ShieldFirst_ThenHp()
        {
            var skeleton = MonsterDatabase.Create(MonsterDatabase.ID_SKELETON);
            // 수비 턴에 실드 5 획득
            skeleton.GainShield(skeleton.Data.ShieldValue); // +5

            // 데미지 8 vs 실드 5: 실드 0, HP -= 3
            skeleton.TakeDamage(8);
            Assert.AreEqual(0,  skeleton.Shield);
            Assert.AreEqual(22, skeleton.Hp); // 25 - 3
        }

        [Test]
        public void Monster_ResetShield_ClearsToZero()
        {
            var skeleton = MonsterDatabase.Create(MonsterDatabase.ID_SKELETON);
            skeleton.GainShield(5);
            Assert.AreEqual(5, skeleton.Shield);

            skeleton.ResetShield();
            Assert.AreEqual(0, skeleton.Shield);
        }

        [Test]
        public void Monster_IsDead_WhenHpZero()
        {
            var slime = MonsterDatabase.Create(MonsterDatabase.ID_SLIME);
            Assert.IsFalse(slime.IsDead);

            slime.TakeDamage(20);
            Assert.IsTrue(slime.IsDead);
        }

        [Test]
        public void Monster_HpDoesNotGoBelowZero()
        {
            var slime = MonsterDatabase.Create(MonsterDatabase.ID_SLIME);
            slime.TakeDamage(999);
            Assert.AreEqual(0, slime.Hp);
        }
    }
}
