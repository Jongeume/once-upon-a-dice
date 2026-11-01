using System;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public readonly struct ShopResult
    {
        public int NewGold      { get; }
        public int NewHp        { get; }
        public int ActualHealed { get; }

        public ShopResult(int newGold, int newHp, int actualHealed)
        {
            NewGold      = newGold;
            NewHp        = newHp;
            ActualHealed = actualHealed;
        }
    }

    public class ShopSystem
    {
        public const int HP_GOLD_PER_UNIT = 10;
        public const int HP_PER_UNIT      = 5;

        public const int XP_GOLD_COST    = 20;
        public const int XP_PER_PURCHASE = 1;

        public int GetMaxHpInvestment(int gold, int currentHp, int maxHp)
        {
            if (currentHp >= maxHp) return 0;
            return (gold / HP_GOLD_PER_UNIT) * HP_GOLD_PER_UNIT;
        }

        public ShopResult ApplyHpRecovery(PlayerState player, int investGold)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (investGold <= 0)
                throw new ArgumentException("투자 골드는 0보다 커야 합니다.", nameof(investGold));
            if (investGold % HP_GOLD_PER_UNIT != 0)
                throw new ArgumentException(
                    $"투자 골드는 {HP_GOLD_PER_UNIT} 단위여야 합니다.", nameof(investGold));
            if (investGold > player.Gold)
                throw new InvalidOperationException(
                    $"골드 부족: 보유 {player.Gold}, 요구 {investGold}");
            if (player.Hp >= player.MaxHp)
                throw new InvalidOperationException("HP가 이미 최대입니다.");

            int units = investGold / HP_GOLD_PER_UNIT;
            int rawHeal = units * HP_PER_UNIT;

            int hpBefore = player.Hp;
            player.SpendGold(investGold);
            player.Heal(rawHeal);
            int actualHealed = player.Hp - hpBefore;

            return new ShopResult(player.Gold, player.Hp, actualHealed);
        }

        public bool CanBuyXp(int gold) => gold >= XP_GOLD_COST;

        public void BuyXp(PlayerState player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (!CanBuyXp(player.Gold))
                throw new InvalidOperationException(
                    $"골드 부족: 보유 {player.Gold}, 요구 {XP_GOLD_COST}");

            player.SpendGold(XP_GOLD_COST);
            player.AddXp(XP_PER_PURCHASE);
        }
    }
}
