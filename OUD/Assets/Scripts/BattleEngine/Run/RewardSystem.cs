// RewardSystem.cs
// 전투 승리 시 지급할 보상(XP + 골드) 수치 결정.
// PlayerState 변경 책임은 외부 호출자에게 있다 — 본 클래스는 수치 생성만 담당.
// 보스전(IsRunClear) 분기는 RunManager 책임 — 보스전엔 본 메서드를 호출하지 않는다.
// feature-spec F-11
using System;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Run
{
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

    public class RewardSystem
    {
        public const int XP_PER_BATTLE = 1;
        public const int GOLD_MIN      = 8;
        public const int GOLD_MAX      = 12;

        public const int ELITE_XP       = 2;
        public const int ELITE_GOLD_MIN = 18;
        public const int ELITE_GOLD_MAX = 24;

        private readonly IRandom _random;

        public RewardSystem(IRandom random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public RewardResult CalculateReward(NodeType nodeType = NodeType.Combat)
        {
            switch (nodeType)
            {
                case NodeType.Elite:
                    int eliteGold = _random.Next(ELITE_GOLD_MIN, ELITE_GOLD_MAX + 1);
                    return new RewardResult(ELITE_XP, eliteGold);
                default:
                    int gold = _random.Next(GOLD_MIN, GOLD_MAX + 1);
                    return new RewardResult(XP_PER_BATTLE, gold);
            }
        }
    }
}
