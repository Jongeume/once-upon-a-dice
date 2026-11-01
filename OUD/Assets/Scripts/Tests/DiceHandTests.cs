// DiceHandTests.cs
// F-01 테스트 시나리오 (tech-spec-sprint0.md §1.6 기준).
// Unity Test Framework (NUnit) 스타일.
// BattleEngine 의존성만 사용하며 UnityEngine 참조 없음.
//
// 테스트 실행: Unity Editor → Window → General → Test Runner → EditMode
using System.Linq;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Dice;

namespace OUD.Tests
{
    // ── Mock ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// 테스트용 결정론적 IRandom 구현체.
    /// 미리 지정한 값 배열을 순서대로 반환한다 (시드 고정 대용).
    /// 배열 소진 시 마지막 값을 반복한다.
    /// </summary>
    internal class SequenceRandom : IRandom
    {
        private readonly int[] _sequence;
        private int _index;

        public SequenceRandom(params int[] sequence)
        {
            _sequence = sequence;
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            // minInclusive/maxExclusive 범위는 무시하고 시퀀스 값을 그대로 반환.
            // 테스트 목적이므로 범위 검증 대신 값 제어에 집중.
            int value = _sequence[_index];
            if (_index < _sequence.Length - 1) _index++;
            return value;
        }

        public void Reset() => _index = 0;
    }

    // ── 테스트 클래스 ─────────────────────────────────────────────────────────
    [TestFixture]
    public class DiceHandTests
    {
        // ── T-01-01: 시드 고정 → 동일한 5개 값 ─────────────────────────────
        [Test]
        public void T_01_01_RollAll_WithFixedSeed_ReturnsSameValues()
        {
            // Arrange: 고정 시퀀스 [3,1,4,1,5]
            var random = new SequenceRandom(3, 1, 4, 1, 5);
            var hand   = new DiceHand(random);

            // Act
            hand.RollAll();
            int[] first = hand.GetValues().ToArray();

            // 같은 시퀀스로 다시 굴렸을 때 동일해야 함
            random.Reset();
            var hand2 = new DiceHand(random);
            hand2.RollAll();
            int[] second = hand2.GetValues().ToArray();

            // Assert
            Assert.AreEqual(first, second, "동일 시드에서 동일한 결과가 나와야 한다.");
        }

        // ── T-01-02: 잠금 없이 리롤 → 5개 모두 변경 가능, RerollsLeft 감소 ──
        [Test]
        public void T_01_02_Reroll_WithNoLocks_DecreasesRerollsLeft()
        {
            // Arrange
            var random = new SequenceRandom(1, 1, 1, 1, 1,  // RollAll 용
                                            6, 6, 6, 6, 6); // Reroll 용
            var hand = new DiceHand(random);
            hand.RollAll();

            int rerollsBefore = hand.RerollsLeft; // 2

            // Act
            bool success = hand.Reroll();

            // Assert
            Assert.IsTrue(success,                 "리롤 잔여가 있으면 true를 반환해야 한다.");
            Assert.AreEqual(rerollsBefore - 1, hand.RerollsLeft, "RerollsLeft가 1 감소해야 한다.");

            int[] values = hand.GetValues();
            foreach (int v in values)
                Assert.AreEqual(6, v, "잠금 없이 리롤하면 새 값(6)으로 변경되어야 한다.");
        }

        // ── T-01-03: 5개 전부 잠금 + 리롤 → true 반환, 값 불변, 카운트 감소 ─
        [Test]
        public void T_01_03_Reroll_WithAllKept_ReturnsTrueButValuesUnchanged()
        {
            // Arrange
            var random = new SequenceRandom(3, 3, 3, 3, 3,  // RollAll
                                            9, 9, 9, 9, 9); // Reroll (호출되면 안 됨)
            var hand = new DiceHand(random);
            hand.RollAll();

            // 5개 전부 잠금
            for (int i = 0; i < 5; i++)
                hand.Dices[i].SetKept(true);

            int rerollsBefore = hand.RerollsLeft;

            // Act
            bool success = hand.Reroll();

            // Assert — E-01
            Assert.IsTrue(success, "전부 잠겼어도 리롤 잔여가 있으면 true를 반환해야 한다.");
            Assert.AreEqual(rerollsBefore - 1, hand.RerollsLeft, "리롤 카운트는 소비되어야 한다.");
            int[] values = hand.GetValues();
            foreach (int v in values)
                Assert.AreEqual(3, v, "잠긴 주사위 값은 변하지 않아야 한다.");
        }

        // ── T-01-04: Reroll 3회 소진 후 4번째 시도 → false 반환 ──────────────
        [Test]
        public void T_01_04_Reroll_AfterExhausted_ReturnsFalse()
        {
            // Arrange
            var random = new SequenceRandom(1, 1, 1, 1, 1,
                                            2, 2, 2, 2, 2,
                                            3, 3, 3, 3, 3,
                                            4, 4, 4, 4, 4);
            var hand = new DiceHand(random);
            hand.RollAll();

            hand.Reroll(); // 1번째
            hand.Reroll(); // 2번째
            hand.Reroll(); // 3번째 → RerollsLeft = 0

            // Act
            bool fourthAttempt = hand.Reroll(); // 4번째

            // Assert — E-02
            Assert.IsFalse(fourthAttempt, "리롤 소진 후 시도는 false를 반환해야 한다.");
            Assert.AreEqual(0, hand.RerollsLeft, "RerollsLeft가 0이어야 한다.");
        }

        // ── T-01-05: ResetForNewTurn → RerollsLeft=3, IsKept 전부 false ──────
        [Test]
        public void T_01_05_ResetForNewTurn_RestoresStateCorrectly()
        {
            // Arrange
            var random = new SequenceRandom(1, 1, 1, 1, 1);
            var hand   = new DiceHand(random);
            hand.RollAll();

            // 잠금하고 리롤 3회 소진
            hand.Dices[0].SetKept(true);
            hand.Reroll();
            hand.Reroll();
            hand.Reroll();

            // Act
            hand.ResetForNewTurn();

            // Assert
            Assert.AreEqual(3, hand.RerollsLeft, "RerollsLeft가 MAX_REROLLS(3)로 복구되어야 한다.");
            foreach (Dice d in hand.Dices)
                Assert.IsFalse(d.IsKept, "모든 주사위의 IsKept가 false여야 한다.");
        }

        // ── T-01-06: GetValues → 각 값이 1~6 범위 ───────────────────────────
        [Test]
        public void T_01_06_GetValues_AllInRange_1To6()
        {
            // Arrange: 시퀀스에 경계값 포함
            var random = new SequenceRandom(1, 2, 3, 5, 6);
            var hand   = new DiceHand(random);
            hand.RollAll();

            // Act
            int[] values = hand.GetValues();

            // Assert
            Assert.AreEqual(5, values.Length, "반환 배열 길이는 5여야 한다.");
            foreach (int v in values)
                Assert.IsTrue(v >= 1 && v <= 6, $"값 {v}은 1~6 범위를 벗어났다.");
        }

        // ── 추가: RollAll은 리롤 카운트를 소비하지 않아야 함 ─────────────────
        [Test]
        public void RollAll_DoesNotConsumeRerollCount()
        {
            var random = new SequenceRandom(1, 2, 3, 4, 5);
            var hand   = new DiceHand(random);

            int before = hand.RerollsLeft;
            hand.RollAll();

            Assert.AreEqual(before, hand.RerollsLeft, "RollAll()은 RerollsLeft를 소비하면 안 된다.");
        }

        // ── 추가: E-03 — RollAll 후 즉시 확정 (리롤 안 함) ──────────────────
        [Test]
        public void E_03_RollAll_ThenGetValuesDirectly_IsValid()
        {
            var random = new SequenceRandom(4, 4, 4, 4, 4);
            var hand   = new DiceHand(random);
            hand.RollAll();

            // 리롤 없이 바로 GetValues()
            int[] values = hand.GetValues();

            Assert.AreEqual(3, hand.RerollsLeft, "리롤 안 했으므로 RerollsLeft는 여전히 3이어야 한다.");
            foreach (int v in values)
                Assert.AreEqual(4, v);
        }
    }
}
