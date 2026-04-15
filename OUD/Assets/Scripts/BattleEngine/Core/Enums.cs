// Enums.cs
// BattleEngine 전역에서 사용하는 열거형 정의.
// 새 족보/카테고리 추가 시 이 파일만 수정하면 된다.
namespace OUD.BattleEngine.Core
{
    /// <summary>
    /// 야추 족보 종류.
    /// 순서는 강도 오름차순 (하위 호환 구현 시 참고용, 실제 판정은 HandEvaluator가 담당).
    /// </summary>
    public enum HandType
    {
        None,
        OnePair,
        TwoPair,
        Triple,
        FullHouse,
        SmallStraight,
        LargeStraight,
        FourOfAKind,
        Yahtzee
    }

    /// <summary>
    /// 스킬 분류: 공격 or 수비.
    /// 슬롯 배분 시 공수 제약 계산에 사용.
    /// </summary>
    public enum SkillCategory
    {
        Attack,
        Defense
    }

    /// <summary>
    /// 스킬 대상 타입.
    /// Single은 대상 지정 필요, AllEnemies는 전체(사망 적 건너뜀), Self는 자기 자신.
    /// </summary>
    public enum TargetType
    {
        Single,       // 단일 대상 (대상 지정 필요)
        AllEnemies,   // 전체 적 (사망한 적 건너뜀)
        Self          // 자기 자신 (수비 스킬)
    }

    /// <summary>
    /// 적 인텐트(예고 행동) 종류. UI 아이콘 표시에 사용.
    /// </summary>
    public enum IntentType
    {
        Attack,       // 칼 아이콘
        Shield,       // 방패 아이콘
        StrongAttack, // 강공격 (보스)
        RageWarning   // 분노 예고 (보스 분노 예고 아이콘)
    }

    /// <summary>
    /// 전투 진행 단계. TurnManager가 상태 머신처럼 관리.
    /// </summary>
    public enum BattlePhase
    {
        BattleStart,
        DiceRoll,
        HandEvaluation,
        SlotAssignment,
        SlotExecution,
        EnemyTurn,
        TurnEnd,
        BattleWon,
        BattleLost
    }
}
