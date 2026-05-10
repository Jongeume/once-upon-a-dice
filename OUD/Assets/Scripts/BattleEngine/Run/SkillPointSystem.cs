// SkillPointSystem.cs
// SP 기반 족보 해금 판정 + 실행.
// feature-spec F-09 (해금만 — 강화는 Post-MVP)
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public class SkillPointSystem
    {
        private static readonly Dictionary<HandType, int> UNLOCK_COSTS =
            new Dictionary<HandType, int>
        {
            { HandType.SmallStraight, 1 },
            { HandType.FourOfAKind,   2 },
            { HandType.LargeStraight, 2 },
            { HandType.Yahtzee,       3 },
        };

        private static readonly HashSet<HandType> DEFAULT_HANDS =
            new HashSet<HandType>
        {
            HandType.OnePair,
            HandType.TwoPair,
            HandType.Triple,
            HandType.FullHouse,
        };

        public int GetUnlockCost(HandType hand)
        {
            if (UNLOCK_COSTS.TryGetValue(hand, out int cost))
                return cost;
            return -1;
        }

        public bool CanUnlock(HandType hand, int currentSp)
        {
            if (DEFAULT_HANDS.Contains(hand)) return false;
            if (!UNLOCK_COSTS.TryGetValue(hand, out int cost)) return false;
            return currentSp >= cost;
        }

        public void Unlock(PlayerState player, HandType hand)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (DEFAULT_HANDS.Contains(hand))
                throw new InvalidOperationException($"{hand}은(는) 기본 족보입니다.");
            if (player.UnlockedHands.Contains(hand))
                throw new InvalidOperationException($"{hand}은(는) 이미 해금되었습니다.");
            if (!UNLOCK_COSTS.TryGetValue(hand, out int cost))
                throw new ArgumentException($"{hand}은(는) 해금 대상이 아닙니다.", nameof(hand));

            player.SpendSp(cost);
            player.UnlockHand(hand);
        }

        public List<HandType> GetUnlockableHands(PlayerState player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            var result = new List<HandType>();
            foreach (var kvp in UNLOCK_COSTS)
            {
                if (player.UnlockedHands.Contains(kvp.Key)) continue;
                if (player.Sp >= kvp.Value)
                    result.Add(kvp.Key);
            }
            return result;
        }
    }
}
