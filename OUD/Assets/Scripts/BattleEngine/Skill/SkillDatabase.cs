// SkillDatabase.cs
// 전체 16개 스킬 정적 데이터 보유 + 조회 메서드 제공.
// 수치 출처: feature-spec-sprint-mvp.md F-04
// [가정 기반] 해금 비용 (UnlockCost): Sprint-1 밸런스 시뮬에서 검증 예정.
using System.Collections.Generic;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Skill
{
    /// <summary>
    /// 스킬 전체 목록 보유. 정적 조회만 제공하며 상태를 보유하지 않는다.
    /// HandType + SkillCategory 조합으로 O(1) 조회 가능.
    /// </summary>
    public static class SkillDatabase
    {
        // ── 내부 테이블 ───────────────────────────────────────────────────────

        private static readonly Dictionary<(HandType, SkillCategory), SkillData> _table;
        private static readonly List<SkillData> _all;

        static SkillDatabase()
        {
            _all = BuildSkillList();
            _table = new Dictionary<(HandType, SkillCategory), SkillData>();
            foreach (var skill in _all)
                _table[(skill.Hand, skill.Category)] = skill;
        }

        // ── 공개 조회 ─────────────────────────────────────────────────────────

        /// <summary>족보 + 카테고리로 스킬 1개 조회. 없으면 null.</summary>
        public static SkillData Get(HandType hand, SkillCategory category)
        {
            _table.TryGetValue((hand, category), out SkillData skill);
            return skill;
        }

        /// <summary>전체 스킬 목록 반환 (읽기 전용).</summary>
        public static IReadOnlyList<SkillData> GetAll() => _all;

        /// <summary>
        /// 플레이어가 해금한 족보에 해당하는 모든 스킬 반환 (사용 가능 여부 무관).
        /// UI에서 "배운 스킬 전체 목록"으로 표시하는 용도.
        /// </summary>
        public static List<SkillData> GetSkillsByUnlockedHands(HashSet<HandType> unlockedHands)
        {
            var result = new List<SkillData>();
            foreach (var skill in _all)
            {
                if (unlockedHands.Contains(skill.Hand))
                    result.Add(skill);
            }
            return result;
        }

        /// <summary>
        /// 현재 턴에 사용 가능한 스킬 목록 반환.
        /// 조건: 달성 족보에 포함 AND 해금 AND 이번 턴 미사용.
        /// feature-spec F-03: 동일 족보 공격+수비 합산 1회 제한.
        /// </summary>
        /// <param name="achievedHands">이번 턴 달성한 족보 목록 (HandEvaluator 출력)</param>
        /// <param name="unlockedHands">플레이어가 해금한 족보 집합</param>
        /// <param name="usedThisTurn">이번 턴 이미 사용된 족보 집합 (공수 합산)</param>
        public static List<SkillData> GetUsableSkills(
            List<HandType>    achievedHands,
            HashSet<HandType> unlockedHands,
            HashSet<HandType> usedThisTurn)
        {
            var result = new List<SkillData>();
            foreach (HandType hand in achievedHands)
            {
                if (!unlockedHands.Contains(hand)) continue;
                if (usedThisTurn.Contains(hand))   continue;

                // 해당 족보의 공격/수비 두 스킬 모두 추가
                var atk = Get(hand, SkillCategory.Attack);
                var def = Get(hand, SkillCategory.Defense);
                if (atk != null) result.Add(atk);
                if (def != null) result.Add(def);
            }
            return result;
        }

        // ── 스킬 정의 ─────────────────────────────────────────────────────────

        private static List<SkillData> BuildSkillList()
        {
            return new List<SkillData>
            {
                // ── 기본덱 (OnePair ~ FullHouse) ─────────────────────────────

                // One Pair
                new SkillData(
                    id:          "OnePair_Attack",
                    name:        "Quick Strike",
                    hand:        HandType.OnePair,
                    category:    SkillCategory.Attack,
                    target:      TargetType.Single,
                    multipliers: new[] { 1.0 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                new SkillData(
                    id:          "OnePair_Defense",
                    name:        "Brace",
                    hand:        HandType.OnePair,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 1.0 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                // Two Pair
                new SkillData(
                    id:          "TwoPair_Attack",
                    name:        "Twin Slash",
                    hand:        HandType.TwoPair,
                    category:    SkillCategory.Attack,
                    target:      TargetType.Single,
                    multipliers: new[] { 0.8, 0.8 }, // 동일 대상 2회 타격 (HitCount=2)
                    hitCount:    2,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                new SkillData(
                    id:          "TwoPair_Defense",
                    name:        "Dual Guard",
                    hand:        HandType.TwoPair,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 0.5 }, // MVP: 현재 턴만. 다음 턴 0.5x는 Post-Sprint TODO.
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                // Triple
                new SkillData(
                    id:          "Triple_Attack",
                    name:        "Heavy Blow",
                    hand:        HandType.Triple,
                    category:    SkillCategory.Attack,
                    target:      TargetType.Single,
                    multipliers: new[] { 1.5 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                new SkillData(
                    id:          "Triple_Defense",
                    name:        "Fortify",
                    hand:        HandType.Triple,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 1.8 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                // Full House
                // Multipliers[0]=주 대상 배율, Multipliers[1]=스플래시 배율.
                // HitCount=1 이고 Multipliers.Length=2 → SkillExecutor가 주+스플래시로 처리.
                new SkillData(
                    id:          "FullHouse_Attack",
                    name:        "Crushing Wave",
                    hand:        HandType.FullHouse,
                    category:    SkillCategory.Attack,
                    target:      TargetType.Single,
                    multipliers: new[] { 1.2, 0.5 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  0,
                    isDefault:   true),

                new SkillData(
                    id:          "FullHouse_Defense",
                    name:        "Iron Wall",
                    hand:        HandType.FullHouse,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 2.0 },
                    hitCount:    1,
                    hpRecover:   3,
                    unlockCost:  0,
                    isDefault:   true),

                // ── 해금 스킬 (SmallStraight ~ Yahtzee) ──────────────────────
                // [가정 기반] unlockCost: Sprint-1 밸런스 시뮬에서 검증 예정.

                // Small Straight
                new SkillData(
                    id:          "SmallStraight_Attack",
                    name:        "Sweeping Edge",
                    hand:        HandType.SmallStraight,
                    category:    SkillCategory.Attack,
                    target:      TargetType.AllEnemies,
                    multipliers: new[] { 0.8 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  1, // [가정 기반]
                    isDefault:   false),

                new SkillData(
                    id:          "SmallStraight_Defense",
                    name:        "Flowing Dodge",
                    hand:        HandType.SmallStraight,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 1.5 },
                    hitCount:    1,
                    hpRecover:   2,
                    unlockCost:  1, // [가정 기반]
                    isDefault:   false),

                // Four of a Kind
                new SkillData(
                    id:          "FourOfAKind_Attack",
                    name:        "Demolish",
                    hand:        HandType.FourOfAKind,
                    category:    SkillCategory.Attack,
                    target:      TargetType.Single,
                    multipliers: new[] { 2.2 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  2, // [가정 기반]
                    isDefault:   false),

                new SkillData(
                    id:          "FourOfAKind_Defense",
                    name:        "Aegis",
                    hand:        HandType.FourOfAKind,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 2.5 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  2, // [가정 기반]
                    isDefault:   false),

                // Large Straight
                new SkillData(
                    id:          "LargeStraight_Attack",
                    name:        "Storm Blade",
                    hand:        HandType.LargeStraight,
                    category:    SkillCategory.Attack,
                    target:      TargetType.AllEnemies,
                    multipliers: new[] { 1.2 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  2, // [가정 기반]
                    isDefault:   false),

                new SkillData(
                    id:          "LargeStraight_Defense",
                    name:        "Barrier Field",
                    hand:        HandType.LargeStraight,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 2.0 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  2, // [가정 기반]
                    isDefault:   false),

                // Yahtzee
                new SkillData(
                    id:          "Yahtzee_Attack",
                    name:        "Annihilate",
                    hand:        HandType.Yahtzee,
                    category:    SkillCategory.Attack,
                    target:      TargetType.Single,
                    multipliers: new[] { 3.0 },
                    hitCount:    1,
                    hpRecover:   0,
                    unlockCost:  3, // [가정 기반]
                    isDefault:   false),

                new SkillData(
                    id:          "Yahtzee_Defense",
                    name:        "Divine Shield",
                    hand:        HandType.Yahtzee,
                    category:    SkillCategory.Defense,
                    target:      TargetType.Self,
                    multipliers: new[] { 3.5 },
                    hitCount:    1,
                    hpRecover:   5,
                    unlockCost:  3, // [가정 기반]
                    isDefault:   false),
            };
        }
    }
}
