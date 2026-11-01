// LevelUpSystem.cs
// 레벨업 판정 + 스탯 보상 적용.
// feature-spec F-08
using System;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public class LevelUpSystem
    {
        private static readonly int[] XP_THRESHOLDS = { 2, 4, 6 };
        public const int MAX_LEVEL = 3;
        public const int ATK_BONUS = 1;
        public const int DEF_BONUS = 1;
        public const int HP_BONUS  = 5;

        public bool CanLevelUp(int totalXp, int currentLevel)
        {
            if (currentLevel >= MAX_LEVEL) return false;
            return totalXp >= XP_THRESHOLDS[currentLevel];
        }

        /// <summary>지정 레벨에서 다음 레벨업에 필요한 누적 XP. 최대 레벨이면 0 반환.</summary>
        public static int GetXpThreshold(int level)
        {
            if (level < 0 || level >= MAX_LEVEL) return 0;
            return XP_THRESHOLDS[level];
        }

        /// <summary>최대 레벨 도달 여부.</summary>
        public static bool IsMaxLevel(int level) => level >= MAX_LEVEL;

        public void ApplyLevelUp(PlayerState player, StatChoice choice)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (!CanLevelUp(player.Xp, player.Level))
                throw new InvalidOperationException(
                    $"레벨업 불가: XP={player.Xp}, Level={player.Level}");

            PlayerStats newStats;
            switch (choice)
            {
                case StatChoice.AtkUp:
                    newStats = player.BaseStats.WithAtkUp(ATK_BONUS);
                    break;
                case StatChoice.DefUp:
                    newStats = player.BaseStats.WithDefUp(DEF_BONUS);
                    break;
                case StatChoice.HpUp:
                    newStats = player.BaseStats.WithHpUp(HP_BONUS);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(choice));
            }

            player.LevelUp(newStats);
        }
    }
}
