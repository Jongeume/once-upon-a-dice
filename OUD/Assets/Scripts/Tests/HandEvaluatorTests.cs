// HandEvaluatorTests.cs
// F-02 테스트 시나리오 (feature-spec-sprint-mvp.md §F-02 기준).
// Unity Test Framework (NUnit) 스타일.
// BattleEngine 의존성만 사용하며 UnityEngine 참조 없음.
//
// 테스트 실행: Unity Editor → Window → General → Test Runner → EditMode
using System.Collections.Generic;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Dice;

namespace OUD.Tests
{
    [TestFixture]
    public class HandEvaluatorTests
    {
        // ── 헬퍼 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 결과 목록이 기대 족보를 정확히 포함하는지 검증한다 (순서 무관, 개수 일치).
        /// </summary>
        private static void AssertHands(int[] dice, params HandType[] expected)
        {
            List<HandType> result   = HandEvaluator.Evaluate(dice);
            var            expSet   = new HashSet<HandType>(expected);
            var            resSet   = new HashSet<HandType>(result);

            Assert.AreEqual(
                expSet.Count, resSet.Count,
                $"족보 개수 불일치. 기대: [{string.Join(",", expected)}], 실제: [{string.Join(",", result)}]");

            foreach (HandType hand in expSet)
                Assert.IsTrue(resSet.Contains(hand),
                    $"기대 족보 '{hand}'가 결과에 없다. 실제: [{string.Join(",", result)}]");
        }

        // ── T-02-01 ~ T-02-09: 명세 테스트 케이스 ───────────────────────────

        [Test]
        public void T_02_01_TwoPair_ReturnsTwoPairAndOnePair()
        {
            // [1,1,2,2,3] → One Pair, Two Pair
            AssertHands(new[] { 1, 1, 2, 2, 3 },
                HandType.OnePair, HandType.TwoPair);
        }

        [Test]
        public void T_02_02_FullHouse_ReturnsFullHouseAndBelow()
        {
            // [3,3,3,2,2] → One Pair, Two Pair, Triple, Full House
            AssertHands(new[] { 3, 3, 3, 2, 2 },
                HandType.OnePair, HandType.TwoPair, HandType.Triple, HandType.FullHouse);
        }

        [Test]
        public void T_02_03_FourOfAKind_ReturnsFoaKAndBelow_NoFullHouse()
        {
            // [4,4,4,4,1] → One Pair, Two Pair, Triple, Four of a Kind (Full House 없음)
            AssertHands(new[] { 4, 4, 4, 4, 1 },
                HandType.OnePair, HandType.TwoPair, HandType.Triple, HandType.FourOfAKind);
        }

        [Test]
        public void T_02_04_Yahtzee_ReturnsAllSetHands()
        {
            // [5,5,5,5,5] → One Pair, Two Pair, Triple, Full House, Four of a Kind, Yahtzee
            AssertHands(new[] { 5, 5, 5, 5, 5 },
                HandType.OnePair, HandType.TwoPair, HandType.Triple,
                HandType.FullHouse, HandType.FourOfAKind, HandType.Yahtzee);
        }

        [Test]
        public void T_02_05_SmallStraightOnly_ReturnsStraightOnly()
        {
            // [1,2,3,4,6] → Small Straight (Set 족보 없음)
            AssertHands(new[] { 1, 2, 3, 4, 6 },
                HandType.SmallStraight);
        }

        [Test]
        public void T_02_06_LargeStraight_1To5_ReturnsBothStraights()
        {
            // [1,2,3,4,5] → Small Straight, Large Straight
            AssertHands(new[] { 1, 2, 3, 4, 5 },
                HandType.SmallStraight, HandType.LargeStraight);
        }

        [Test]
        public void T_02_07_LargeStraight_2To6_ReturnsBothStraights()
        {
            // [2,3,4,5,6] → Small Straight, Large Straight
            AssertHands(new[] { 2, 3, 4, 5, 6 },
                HandType.SmallStraight, HandType.LargeStraight);
        }

        [Test]
        public void T_02_08_SmallStraight_WithPair_ReturnsBoth()
        {
            // [1,2,3,4,4] → Small Straight + One Pair (Straight와 Set 독립)
            AssertHands(new[] { 1, 2, 3, 4, 4 },
                HandType.SmallStraight, HandType.OnePair);
        }

        [Test]
        public void T_02_09_Bust_ReturnsEmpty()
        {
            // [1,3,5,2,6] = [1,2,3,5,6] → 잡패 (어떤 족보도 없음)
            List<HandType> result = HandEvaluator.Evaluate(new[] { 1, 3, 5, 2, 6 });
            Assert.AreEqual(0, result.Count,
                $"잡패여야 하지만 족보가 반환됐다: [{string.Join(",", result)}]");
        }

        // ── 추가 엣지 케이스 ──────────────────────────────────────────────────

        [Test]
        public void E_02_01_TripleOnly_ReturnsTripleAndOnePair()
        {
            // [1,1,1,2,3] → Triple + One Pair (Two Pair 없음, Full House 없음)
            AssertHands(new[] { 1, 1, 1, 2, 3 },
                HandType.Triple, HandType.OnePair);
        }

        [Test]
        public void E_02_02_OnePairOnly_ReturnsOnePair()
        {
            // [1,1,2,3,5] → One Pair만 (4연속 없음 — 4가 빠져 있어 Small Straight 불성립)
            AssertHands(new[] { 1, 1, 2, 3, 5 },
                HandType.OnePair);
        }

        [Test]
        public void E_02_03_SmallStraight_3To6_ReturnsStraight()
        {
            // [3,4,5,6,1] → 3~6 Small Straight (1이 있어도 3~6이 연속이면 인정)
            AssertHands(new[] { 3, 4, 5, 6, 1 },
                HandType.SmallStraight);
        }

        [Test]
        public void E_02_04_SmallStraight_WithTwoPair_ReturnsBoth()
        {
            // [1,2,3,4,1] → Small Straight + One Pair (4연속 + 1쌍)
            // Two Pair는 아님 (쌍이 하나)
            AssertHands(new[] { 1, 2, 3, 4, 1 },
                HandType.SmallStraight, HandType.OnePair);
        }

        [Test]
        public void E_02_05_Yahtzee_AllOnes_ReturnsAllSetHands()
        {
            // [1,1,1,1,1] → 모든 Set 족보
            AssertHands(new[] { 1, 1, 1, 1, 1 },
                HandType.OnePair, HandType.TwoPair, HandType.Triple,
                HandType.FullHouse, HandType.FourOfAKind, HandType.Yahtzee);
        }

        [Test]
        public void E_02_06_FoaK_NoFullHouse_NoTwoPairByDetection()
        {
            // [6,6,6,6,3] → FoaK 체인, FullHouse 없음
            AssertHands(new[] { 6, 6, 6, 6, 3 },
                HandType.OnePair, HandType.TwoPair, HandType.Triple, HandType.FourOfAKind);
        }

        // ── 개별 판정 메서드 단위 테스트 ─────────────────────────────────────

        [Test]
        public void Unit_IsFullHouse_FoaK_ReturnsFalse()
        {
            // [4,4,4,4,1] → Full House 아님 (별도 Pair 없음)
            int[] counts = new int[7];
            counts[4] = 4; counts[1] = 1;
            Assert.IsFalse(HandEvaluator.IsFullHouse(counts),
                "FoaK는 Full House가 아니어야 한다.");
        }

        [Test]
        public void Unit_IsFullHouse_Yahtzee_ReturnsFalse()
        {
            // [5,5,5,5,5] → IsFullHouse 직접 호출은 false (별도 Pair 없음)
            // Evaluate()에서 Yahtzee 체인으로 FullHouse 추가됨
            int[] counts = new int[7];
            counts[5] = 5;
            Assert.IsFalse(HandEvaluator.IsFullHouse(counts),
                "Yahtzee의 IsFullHouse 직접 판정은 false여야 한다 (체인에서 처리).");
        }

        [Test]
        public void Unit_IsTwoPair_FoaK_ReturnsFalse()
        {
            // [4,4,4,4,1] → 눈금 4만 ≥2이므로 직접 TwoPair는 false
            int[] counts = new int[7];
            counts[4] = 4; counts[1] = 1;
            Assert.IsFalse(HandEvaluator.IsTwoPair(counts),
                "FoaK의 IsTwoPair 직접 판정은 false여야 한다 (체인에서 처리).");
        }

        [Test]
        public void Unit_IsLargeStraight_RequiresExactly5Distinct()
        {
            // [1,2,3,4,4] → 4개 distinct → Large Straight 아님
            int[] distinct = new[] { 1, 2, 3, 4 };
            Assert.IsFalse(HandEvaluator.IsLargeStraight(distinct),
                "distinct 4개는 Large Straight가 아니어야 한다.");
        }

        [Test]
        public void Unit_IsSmallStraight_GapInMiddle_ReturnsFalse()
        {
            // [1,2,3,5,6] → 4 없음 → Small Straight 아님
            int[] distinct = new[] { 1, 2, 3, 5, 6 };
            Assert.IsFalse(HandEvaluator.IsSmallStraight(distinct),
                "[1,2,3,5,6]은 Small Straight가 아니어야 한다.");
        }
    }
}
