// RewardSystem.cs
// 전투 승리 시 지급할 보상(XP + 골드) 수치 결정.
// PlayerState 변경 책임은 외부 호출자에게 있다 — 본 클래스는 수치 생성만 담당.
// 보스전(IsRunClear) 분기는 RunManager 책임 — 보스전엔 본 메서드를 호출하지 않는다.
// feature-spec F-11
using System;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 1회 전투 승리 보상 결과.
    /// 호출자(추후 RunManager / BattleUIAdapter)가 PlayerState.AddXp/AddGold 호출에 사용한다.
    /// </summary>
    public readonly struct RewardResult
    {
        public int Xp   { get; }
        public int Gold { get; }

        public RewardResult(int xp, int gold)
        {
            Xp   = xp;
            Gold = gold;
        }
    }

    /// <summary>
    /// 기본 보상 시스템 구현. IRandom은 생성자 주입 (Dice/DiceHand 패턴).
    /// 규칙 (feature-spec F-11, game-design-v2.2 §5.1):
    ///   - XP: 전투당 +1 고정
    ///   - 골드: GOLD_MIN ~ GOLD_MAX 범위 (양 끝 포함) 균등 랜덤
    ///   - 보스전(7번째)은 호출 자체를 생략 (RunManager 책임)
    /// </summary>
    public class RewardSystem
    {
        public const int XP_PER_BATTLE = 1;
        public const int GOLD_MIN      = 8;   // 양 끝 포함
        public const int GOLD_MAX      = 12;  // 양 끝 포함

        private readonly IRandom _random;

        public RewardSystem(IRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public RewardResult CalculateReward()
        {
            // IRandom.Next(min, max)는 max exclusive이므로 GOLD_MAX 포함시키려면 +1
            int gold = _random.Next(GOLD_MIN, GOLD_MAX + 1);
            return new RewardResult(XP_PER_BATTLE, gold);
        }
    }
}
