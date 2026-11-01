// SlotManager.cs
// 슬롯 3개 배분 유효성 검증 + 배분 정보 타입 정의.
// feature-spec F-03
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Combat
{
    /// <summary>
    /// 슬롯 1개의 배분 정보.
    /// TargetIndex = -1 : 수비(Self) 또는 전체(AllEnemies) 스킬.
    /// </summary>
    public class SlotAssignment
    {
        public SkillData Skill       { get; set; }
        /// <summary>공격 시 대상 적의 allEnemies 인덱스. 수비/전체는 -1.</summary>
        public int       TargetIndex { get; set; } = -1;
    }

    /// <summary>
    /// 슬롯 배분 유효성 검증.
    /// 실제 배분 상태는 TurnManager가 보관하며, 이 클래스는 규칙 판정만 담당한다.
    /// </summary>
    public class SlotManager
    {
        public const int MAX_SLOTS = 3;

        // ── 전체 배분 유효성 검증 ────────────────────────────────────────────

        /// <summary>
        /// 슬롯 배분 목록 전체가 유효한지 검증한다.
        /// 규칙 (feature-spec F-03):
        ///   1. 슬롯 수는 0~MAX_SLOTS 이내.
        ///   2. 동일 족보 턴당 1회 (공+수 합산).
        ///   3. 공격 스킬의 대상은 생존한 적이어야 함.
        ///   4. 각 슬롯의 스킬 족보가 achievedHands에 포함되어야 함.
        /// </summary>
        /// <param name="slots">배분 목록 (빈 슬롯은 null 또는 리스트에서 제외)</param>
        /// <param name="achievedHands">이번 턴 달성한 족보</param>
        /// <param name="allEnemies">전체 적 목록 (생존 여부 확인용)</param>
        /// <returns>유효하면 true</returns>
        public bool Validate(
            List<SlotAssignment>  slots,
            List<HandType>        achievedHands,
            List<MonsterInstance> allEnemies)
        {
            if (slots == null)         return true; // 빈 배분 허용
            if (slots.Count > MAX_SLOTS) return false;

            var usedHands = new HashSet<HandType>();

            foreach (SlotAssignment slot in slots)
            {
                if (slot?.Skill == null) continue; // 빈 슬롯 허용

                SkillData skill = slot.Skill;

                // 규칙 4: 달성한 족보여야 함
                if (!achievedHands.Contains(skill.Hand))
                    return false;

                // 규칙 2: 동일 족보 1회 제한
                if (usedHands.Contains(skill.Hand))
                    return false;
                usedHands.Add(skill.Hand);

                // 규칙 3: 공격 스킬 대상 검증
                if (skill.Category == SkillCategory.Attack &&
                    skill.Target   == TargetType.Single)
                {
                    int idx = slot.TargetIndex;
                    if (idx < 0 || idx >= allEnemies.Count) return false;
                    if (allEnemies[idx].IsDead)              return false;
                }
            }

            return true;
        }

        // ── 단일 스킬 배치 가능 여부 ─────────────────────────────────────────

        /// <summary>
        /// 현재까지 배분된 상태에서 특정 스킬을 추가 배치할 수 있는지 확인.
        /// Unity SlotPresenter에서 슬롯 선택 시 실시간 피드백에 사용.
        /// </summary>
        /// <param name="currentSlots">현재까지 배분된 슬롯 목록</param>
        /// <param name="handToAdd">추가하려는 스킬의 족보</param>
        /// <param name="achievedHands">이번 턴 달성한 족보</param>
        public bool CanAssign(
            List<SlotAssignment> currentSlots,
            HandType             handToAdd,
            List<HandType>       achievedHands)
        {
            // 달성하지 못한 족보
            if (!achievedHands.Contains(handToAdd)) return false;

            // 슬롯이 꽉 찼음
            int filledCount = 0;
            foreach (var s in currentSlots)
                if (s?.Skill != null) filledCount++;
            if (filledCount >= MAX_SLOTS) return false;

            // 이미 같은 족보 사용
            foreach (var s in currentSlots)
                if (s?.Skill?.Hand == handToAdd) return false;

            return true;
        }
    }
}
