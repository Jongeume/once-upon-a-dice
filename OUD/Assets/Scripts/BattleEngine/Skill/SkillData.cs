// SkillData.cs
// 스킬 1개의 정적 정의 (불변).
// 수치는 feature-spec-sprint-mvp.md F-04 기준.
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Skill
{
    /// <summary>
    /// 스킬 1개의 설계 데이터. 생성 후 변경 불가.
    /// 실제 배율은 GetMultiplier()로 강화 레벨을 반영해 조회한다.
    /// </summary>
    public class SkillData
    {
        // ── 식별자 ──────────────────────────────────────────────────────────
        /// <summary>코드 식별자. 예: "OnePair_Attack", "Triple_Defense"</summary>
        public string        Id          { get; }
        public string        Name        { get; }

        // ── 분류 ────────────────────────────────────────────────────────────
        public HandType      Hand        { get; }
        public SkillCategory Category    { get; }
        public TargetType    Target      { get; }

        // ── 배율 ────────────────────────────────────────────────────────────
        /// <summary>
        /// 기본 배율 배열.
        ///   단일 히트   : [1.0]
        ///   다중 히트   : [0.8, 0.8]  (Twin Slash — 동일 대상 2회)
        ///   주 + 스플래시: [1.2, 0.5]  (Crushing Wave — 주 대상 + 전체 스플래시)
        /// </summary>
        public double[]      Multipliers { get; }

        /// <summary>
        /// 동일 대상 반복 타격 횟수.
        /// Twin Slash = 2, 나머지는 1.
        /// Crushing Wave의 스플래시는 HitCount가 아니라 Multipliers[1]로 구분한다.
        /// </summary>
        public int           HitCount    { get; }

        // ── 부가 효과 (수비 스킬) ────────────────────────────────────────────
        /// <summary>스킬 실행 시 플레이어 HP 회복량. 0 = 회복 없음.</summary>
        public int           HpRecover   { get; }

        // ── 해금 정보 ────────────────────────────────────────────────────────
        /// <summary>0 = 기본덱 (시작부터 사용 가능), 1~3 = 해금 필요 SP.</summary>
        public int           UnlockCost  { get; }
        /// <summary>true = 기본덱 (OnePair~FullHouse), false = 해금 필요.</summary>
        public bool          IsDefault   { get; }

        // ── 생성자 ────────────────────────────────────────────────────────────
        public SkillData(
            string        id,
            string        name,
            HandType      hand,
            SkillCategory category,
            TargetType    target,
            double[]      multipliers,
            int           hitCount   = 1,
            int           hpRecover  = 0,
            int           unlockCost = 0,
            bool          isDefault  = true)
        {
            Id          = id;
            Name        = name;
            Hand        = hand;
            Category    = category;
            Target      = target;
            Multipliers = multipliers;
            HitCount    = hitCount;
            HpRecover   = hpRecover;
            UnlockCost  = unlockCost;
            IsDefault   = isDefault;
        }

        // ── 배율 조회 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 강화 레벨을 반영한 실제 배율 반환.
        /// enhanceLevel: 0=Lv1(미강화), 1=Lv2, 2=Lv3.
        /// 각 레벨당 +0.2x 가산. game-design-v2.2 §3.3.
        /// </summary>
        /// <param name="index">Multipliers 배열 인덱스</param>
        /// <param name="enhanceLevel">강화 레벨 (0~2)</param>
        public double GetMultiplier(int index, int enhanceLevel)
            => Multipliers[index] + (0.2 * enhanceLevel);
    }
}
