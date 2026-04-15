// HandEvaluator.cs
// F-02 족보 판정 엔진.
// 주사위 5개 값 → 달성된 모든 족보 목록 반환 (하위 호환 포함).
// 순수 정적 클래스 (상태 없음, UnityEngine 미사용).
//
// 하위 호환 체인 (game-design-v2.2 §2.3):
//   Yahtzee ⊃ FoaK ⊃ Triple ⊃ TwoPair ⊃ OnePair
//   FoaK    ⊃ TwoPair  (게임플레이 직관 우선)
//   FullHouse ⊃ Triple + TwoPair + OnePair
//   LargeStraight ⊃ SmallStraight
// Straight 계열과 Set 계열은 독립적으로 판정된다.
// 잡패(어떤 족보도 달성 못함) → 빈 리스트 반환.
namespace OUD.BattleEngine.Dice
{
    using System.Collections.Generic;
    using OUD.BattleEngine.Core;

    /// <summary>
    /// 주사위 5개 값을 받아 달성된 모든 족보를 반환하는 순수 정적 클래스.
    /// 개별 판정 메서드(IsXxx)는 public이므로 외부 테스트/조회에 사용 가능.
    /// </summary>
    public static class HandEvaluator
    {
        // ── 공개 API ───────────────────────────────────────────────────────

        /// <summary>
        /// 달성된 모든 족보를 반환한다 (하위 호환 포함).
        /// 잡패 시 빈 리스트를 반환한다.
        /// </summary>
        /// <param name="diceValues">주사위 5개 값 배열 (각 1~6)</param>
        /// <returns>달성된 HandType 목록 (순서 미보장)</returns>
        public static List<HandType> Evaluate(int[] diceValues)
        {
            // counts[i] = 눈금 i가 나온 횟수 (i: 1~6, 인덱스 0 미사용)
            int[] counts        = BuildCounts(diceValues);
            // 중복 제거 + 오름차순 정렬 배열 (Straight 판정용)
            int[] sortedDistinct = BuildSortedDistinct(diceValues);

            // ── Set 계열 개별 판정 ─────────────────────────────────────
            bool yahtzee   = IsYahtzee(counts);
            bool foaK      = IsFourOfAKind(counts);
            bool fullHouse = IsFullHouse(counts);
            bool triple    = IsTriple(counts);
            bool twoPair   = IsTwoPair(counts);
            bool onePair   = IsOnePair(counts);

            // ── Straight 계열 개별 판정 ────────────────────────────────
            bool largeStraight = IsLargeStraight(sortedDistinct);
            bool smallStraight = IsSmallStraight(sortedDistinct);

            var result = new HashSet<HandType>();

            // ── Set 계열 하위 호환 체인 적용 ───────────────────────────
            if (yahtzee)
            {
                // Yahtzee는 FullHouse를 포함한 모든 Set 족보를 달성한다.
                // (feature-spec §F-02 테스트 케이스: [5,5,5,5,5] 기준)
                result.Add(HandType.Yahtzee);
                result.Add(HandType.FourOfAKind);
                result.Add(HandType.FullHouse);
                result.Add(HandType.Triple);
                result.Add(HandType.TwoPair);
                result.Add(HandType.OnePair);
            }
            else if (foaK)
            {
                // FoaK ⊃ Triple ⊃ TwoPair ⊃ OnePair, FoaK ⊃ TwoPair
                // FullHouse는 미포함 (별도 Pair가 없으므로)
                result.Add(HandType.FourOfAKind);
                result.Add(HandType.Triple);
                result.Add(HandType.TwoPair);
                result.Add(HandType.OnePair);
            }
            else
            {
                // FullHouse ⊃ Triple + TwoPair + OnePair
                if (fullHouse)
                {
                    result.Add(HandType.FullHouse);
                    result.Add(HandType.Triple);
                    result.Add(HandType.TwoPair);
                    result.Add(HandType.OnePair);
                }
                else if (triple)
                {
                    // Triple이 있으면 OnePair 포함
                    result.Add(HandType.Triple);
                    result.Add(HandType.OnePair);
                }

                // TwoPair는 Triple/FullHouse와 독립적으로 추가
                // (triple이 없는 순수 TwoPair, 또는 이미 추가된 경우 HashSet이 중복 처리)
                if (twoPair)
                {
                    result.Add(HandType.TwoPair);
                    result.Add(HandType.OnePair);
                }
                else if (onePair && !triple)
                {
                    // Triple 없는 단독 OnePair만 추가
                    result.Add(HandType.OnePair);
                }
            }

            // ── Straight 계열 (Set 계열과 독립) ──────────────────────────
            if (largeStraight)
            {
                result.Add(HandType.LargeStraight);
                result.Add(HandType.SmallStraight); // LargeStraight ⊃ SmallStraight
            }
            else if (smallStraight)
            {
                result.Add(HandType.SmallStraight);
            }

            return new List<HandType>(result);
        }

        // ── 개별 판정 메서드 (public — 외부 테스트 가능) ──────────────────

        /// <summary>
        /// 같은 눈 5개인지 판정한다.
        /// </summary>
        /// <param name="counts">눈금별 빈도 배열 (인덱스 1~6)</param>
        public static bool IsYahtzee(int[] counts)
        {
            for (int i = 1; i <= 6; i++)
                if (counts[i] == 5) return true;
            return false;
        }

        /// <summary>
        /// 같은 눈 4개 이상인지 판정한다.
        /// </summary>
        /// <param name="counts">눈금별 빈도 배열 (인덱스 1~6)</param>
        public static bool IsFourOfAKind(int[] counts)
        {
            for (int i = 1; i <= 6; i++)
                if (counts[i] >= 4) return true;
            return false;
        }

        /// <summary>
        /// Triple + 별도 Pair(서로 다른 눈금)인지 판정한다.
        /// Yahtzee([5,5,5,5,5])는 별도 Pair가 없으므로 false를 반환한다.
        /// Yahtzee에서의 FullHouse 포함은 Evaluate()의 체인에서 처리.
        /// </summary>
        /// <param name="counts">눈금별 빈도 배열 (인덱스 1~6)</param>
        public static bool IsFullHouse(int[] counts)
        {
            bool hasTriple       = false;
            bool hasSeparatePair = false;
            for (int i = 1; i <= 6; i++)
            {
                if      (counts[i] >= 3) hasTriple       = true;
                else if (counts[i] >= 2) hasSeparatePair = true;
            }
            return hasTriple && hasSeparatePair;
        }

        /// <summary>
        /// 같은 눈 3개 이상인지 판정한다.
        /// </summary>
        /// <param name="counts">눈금별 빈도 배열 (인덱스 1~6)</param>
        public static bool IsTriple(int[] counts)
        {
            for (int i = 1; i <= 6; i++)
                if (counts[i] >= 3) return true;
            return false;
        }

        /// <summary>
        /// 서로 다른 눈금에서 2쌍 이상인지 판정한다.
        /// FoaK([4,4,4,4,1])는 하나의 눈금(4)만 2쌍 이상이므로 false.
        /// </summary>
        /// <param name="counts">눈금별 빈도 배열 (인덱스 1~6)</param>
        public static bool IsTwoPair(int[] counts)
        {
            int pairCount = 0;
            for (int i = 1; i <= 6; i++)
                if (counts[i] >= 2) pairCount++;
            return pairCount >= 2;
        }

        /// <summary>
        /// 같은 눈 2개 이상인지 판정한다.
        /// </summary>
        /// <param name="counts">눈금별 빈도 배열 (인덱스 1~6)</param>
        public static bool IsOnePair(int[] counts)
        {
            for (int i = 1; i <= 6; i++)
                if (counts[i] >= 2) return true;
            return false;
        }

        /// <summary>
        /// 5연속 수(1-2-3-4-5 또는 2-3-4-5-6)인지 판정한다.
        /// 5개의 서로 다른 주사위가 연속이어야 하므로, sortedDistinct 길이가 정확히 5여야 한다.
        /// </summary>
        /// <param name="sortedDistinct">중복 제거 + 오름차순 정렬된 눈금 배열</param>
        public static bool IsLargeStraight(int[] sortedDistinct)
        {
            // 5개 모두 달라야 하고, 최솟값~최댓값 범위가 4이면 연속이다.
            return sortedDistinct.Length == 5 &&
                   (sortedDistinct[4] - sortedDistinct[0]) == 4;
        }

        /// <summary>
        /// 4연속 수(1~4, 2~5, 3~6 중 하나)를 포함하는지 판정한다.
        /// </summary>
        /// <param name="sortedDistinct">중복 제거 + 오름차순 정렬된 눈금 배열</param>
        public static bool IsSmallStraight(int[] sortedDistinct)
        {
            if (sortedDistinct.Length < 4) return false;

            // 정렬+중복제거된 배열에서 연속 카운트가 4 이상이면 Small Straight.
            int consecutive = 1;
            for (int i = 1; i < sortedDistinct.Length; i++)
            {
                if (sortedDistinct[i] == sortedDistinct[i - 1] + 1)
                    consecutive++;
                else
                    consecutive = 1;

                if (consecutive >= 4) return true;
            }
            return false;
        }

        // ── 내부 유틸리티 ──────────────────────────────────────────────────

        /// <summary>
        /// 눈금별 빈도 배열을 생성한다.
        /// counts[i] = 눈금 i가 나온 횟수 (i: 1~6, 인덱스 0은 미사용).
        /// </summary>
        private static int[] BuildCounts(int[] diceValues)
        {
            int[] counts = new int[7]; // 인덱스 1~6 사용
            foreach (int v in diceValues)
                counts[v]++;
            return counts;
        }

        /// <summary>
        /// 중복 제거 + 오름차순 정렬된 눈금 배열을 생성한다.
        /// IsSmallStraight / IsLargeStraight 전달 용도.
        /// </summary>
        private static int[] BuildSortedDistinct(int[] diceValues)
        {
            // counts 기반으로 생성하면 이미 정렬된 순서로 추출 가능
            var distinct = new List<int>(6);
            int[] counts = BuildCounts(diceValues);
            for (int i = 1; i <= 6; i++)
                if (counts[i] > 0) distinct.Add(i);
            return distinct.ToArray();
        }
    }
}
