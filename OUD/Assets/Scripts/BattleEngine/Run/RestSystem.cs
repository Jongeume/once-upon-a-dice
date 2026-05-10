// RestSystem.cs
// 휴식 시스템: 골드 → HP 회복 교환.
// feature-spec F-10
using System;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public readonly struct RestResult
    {
        public int NewGold      { get; }
        public int NewHp        { get; }
        public int ActualHealed { get; }

        public RestResult(int newGold, int newHp, int actualHealed)
        {
            NewGold      = newGold;
            NewHp        = newHp;
            ActualHealed = actualHealed;
        }
    }

    public class RestSystem
    {
        public const int GOLD_PER_UNIT = 10;
        public const int HP_PER_UNIT   = 5;

        public int GetMaxInvestment(int gold, int currentHp, int maxHp)
        {
            if (currentHp >= maxHp) return 0;

            int maxByGold = (gold / GOLD_PER_UNIT) * GOLD_PER_UNIT;
            return maxByGold;
        }

        public RestResult ApplyRest(PlayerState player, int investGold)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (investGold <= 0)
                throw new ArgumentException("투자 골드는 0보다 커야 합니다.", nameof(investGold));
            if (investGold % GOLD_PER_UNIT != 0)
                throw new ArgumentException(
                    $"투자 골드는 {GOLD_PER_UNIT} 단위여야 합니다.", nameof(investGold));
            if (investGold > player.Gold)
                throw new InvalidOperationException(
                    $"골드 부족: 보유 {player.Gold}, 요구 {investGold}");
            if (player.Hp >= player.MaxHp)
                throw new InvalidOperationException("HP가 이미 최대입니다.");

            int units = investGold / GOLD_PER_UNIT;
            int rawHeal = units * HP_PER_UNIT;

            int hpBefore = player.Hp;
            player.SpendGold(investGold);
            player.Heal(rawHeal);
            int actualHealed = player.Hp - hpBefore;

            return new RestResult(player.Gold, player.Hp, actualHealed);
        }
    }
}
