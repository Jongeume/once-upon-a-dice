// IntentSystem.cs
// 적 Intent 표시 데이터 생성. 분노 경고 표시 여부 판단 포함.
// game-design-v2.2 §4.4, feature-spec F-07
using System;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Unit
{
    /// <summary>
    /// UI에 표시할 Intent 정보 (IntentType + 수치 + 분노 경고 여부).
    /// MonsterInstance에서 직접 읽지 않고 이 구조체를 통해 Unity에 전달한다.
    /// </summary>
    public struct IntentDisplay
    {
        /// <summary>행동 종류 (칼/방패/강공격 아이콘 선택에 사용).</summary>
        public IntentType Type;
        /// <summary>데미지 또는 실드 예정 수치.</summary>
        public int        Value;
        /// <summary>
        /// 분노 예고 불꽃 아이콘 표시 여부.
        /// HP가 분노 임박 구간(threshold 초과 ~ MaxHp×60% 이하)일 때 true.
        /// feature-spec F-07: HP 40~48 구간 (Golem MaxHp=80 기준).
        /// </summary>
        public bool       ShowRageWarning;
    }

    /// <summary>
    /// MonsterInstance → IntentDisplay 변환 순수 함수.
    /// TurnManager 또는 Unity Presenter가 호출해 UI를 갱신한다.
    /// </summary>
    public static class IntentSystem
    {
        // 분노 경고 상한 비율: MaxHp의 60%. Math.Floor 적용.
        private const double RAGE_WARNING_RATIO = 0.6;

        /// <summary>
        /// 몬스터의 현재 Intent와 분노 경고 여부를 담은 IntentDisplay를 반환한다.
        ///
        /// 분노 경고 조건 (game-design-v2.2 §4.4):
        ///   HasRage = true (보스 전용)
        ///   AND IsEnraged = false (아직 분노 미전환)
        ///   AND Hp > RageHpThreshold (분노 임계 초과 — 아직 발동 전)
        ///   AND Hp ≤ floor(MaxHp × 0.6) (경고 구간 상한 이하)
        ///
        /// 예: Golem(MaxHp=80, Threshold=40) → HP 41~48 구간에서 경고 표시.
        /// </summary>
        public static IntentDisplay GetDisplay(MonsterInstance monster)
        {
            IntentType type  = monster.GetCurrentIntent();
            int        value = monster.GetIntentValue();

            bool showRageWarning = false;
            if (monster.Data.HasRage && !monster.IsEnraged)
            {
                // 경고 상한: floor(MaxHp × 0.6)
                int warningUpperBound = (int)Math.Floor(monster.Data.MaxHp * RAGE_WARNING_RATIO);
                showRageWarning = monster.Hp > monster.Data.RageHpThreshold
                               && monster.Hp <= warningUpperBound;
            }

            return new IntentDisplay
            {
                Type            = type,
                Value           = value,
                ShowRageWarning = showRageWarning
            };
        }
    }
}
