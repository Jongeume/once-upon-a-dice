// IRandom.cs
// 모든 랜덤 생성은 이 인터페이스를 경유한다.
// System.Random 직접 사용 금지 — 테스트 시 MockRandom으로 교체 가능하게 하기 위함.
namespace OUD.BattleEngine.Core
{
    /// <summary>
    /// 랜덤 정수 생성 인터페이스.
    /// BattleEngine 내부에서 System.Random 또는 UnityEngine.Random을
    /// 직접 참조하지 않도록 의존성을 역전시킨다 (DIP).
    /// </summary>
    public interface IRandom
    {
        /// <summary>
        /// [minInclusive, maxExclusive) 범위의 정수를 반환한다.
        /// 예: Next(1, 7) → 1~6
        /// </summary>
        int Next(int minInclusive, int maxExclusive);
    }
}
