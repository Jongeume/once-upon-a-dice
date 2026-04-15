// UnityRandom.cs
// IRandom의 Unity 런타임 구현체.
// UnityEngine.Random을 경유하므로 BattleEngine은 Unity에 의존하지 않아도 된다.
using UnityEngine;
using OUD.BattleEngine.Core;

namespace OUD.Unity.Adapter
{
    /// <summary>
    /// Unity 런타임에서 사용하는 IRandom 구현체.
    /// 테스트 시에는 MockRandom으로 교체 가능.
    /// </summary>
    public class UnityRandom : IRandom
    {
        /// <summary>[minInclusive, maxExclusive) 범위의 정수 반환.</summary>
        public int Next(int minInclusive, int maxExclusive)
            => Random.Range(minInclusive, maxExclusive);
    }
}
