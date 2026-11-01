using System;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class ShopSystemTests
    {
        private static PlayerState NewPlayer(int hp = 60, int gold = 50)
        {
            var player = new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));
            if (gold > 0) player.AddGold(gold);
            if (hp < 60) player.TakeDamage(60 - hp);
            return player;
        }

        private ShopSystem _sut;

        [SetUp]
        public void SetUp() => _sut = new ShopSystem();

        // ── HP Recovery ────────────────────────────────────────────────

        [Test]
        public void GetMaxHpInvestment_ReturnsMultipleOf10()
        {
            int result = _sut.GetMaxHpInvestment(35, 40, 60);
            Assert.AreEqual(30, result);
        }

        [Test]
        public void GetMaxHpInvestment_FullHp_ReturnsZero()
        {
            int result = _sut.GetMaxHpInvestment(50, 60, 60);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ApplyHpRecovery_Heals5PerUnit()
        {
            var player = NewPlayer(hp: 40, gold: 50);

            ShopResult result = _sut.ApplyHpRecovery(player, 20);

            Assert.AreEqual(10, result.ActualHealed);
            Assert.AreEqual(50, result.NewHp);
            Assert.AreEqual(30, result.NewGold);
        }

        [Test]
        public void ApplyHpRecovery_CapsAtMaxHp()
        {
            var player = NewPlayer(hp: 58, gold: 50);

            ShopResult result = _sut.ApplyHpRecovery(player, 10);

            Assert.AreEqual(2, result.ActualHealed);
            Assert.AreEqual(60, result.NewHp);
        }

        [Test]
        public void ApplyHpRecovery_NullPlayer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.ApplyHpRecovery(null, 10));
        }

        [Test]
        public void ApplyHpRecovery_ZeroGold_Throws()
        {
            var player = NewPlayer(hp: 40, gold: 50);
            Assert.Throws<ArgumentException>(() => _sut.ApplyHpRecovery(player, 0));
        }

        [Test]
        public void ApplyHpRecovery_NotMultipleOf10_Throws()
        {
            var player = NewPlayer(hp: 40, gold: 50);
            Assert.Throws<ArgumentException>(() => _sut.ApplyHpRecovery(player, 15));
        }

        [Test]
        public void ApplyHpRecovery_InsufficientGold_Throws()
        {
            var player = NewPlayer(hp: 40, gold: 5);
            Assert.Throws<InvalidOperationException>(() => _sut.ApplyHpRecovery(player, 10));
        }

        [Test]
        public void ApplyHpRecovery_FullHp_Throws()
        {
            var player = NewPlayer(hp: 60, gold: 50);
            Assert.Throws<InvalidOperationException>(() => _sut.ApplyHpRecovery(player, 10));
        }

        // ── XP Purchase ────────────────────────────────────────────────

        [Test]
        public void CanBuyXp_EnoughGold_True()
        {
            Assert.IsTrue(_sut.CanBuyXp(20));
            Assert.IsTrue(_sut.CanBuyXp(100));
        }

        [Test]
        public void CanBuyXp_NotEnoughGold_False()
        {
            Assert.IsFalse(_sut.CanBuyXp(19));
            Assert.IsFalse(_sut.CanBuyXp(0));
        }

        [Test]
        public void BuyXp_DeductsGold_AddsXp()
        {
            var player = NewPlayer(hp: 60, gold: 50);
            int xpBefore = player.Xp;

            _sut.BuyXp(player);

            Assert.AreEqual(xpBefore + 1, player.Xp);
            Assert.AreEqual(30, player.Gold);
        }

        [Test]
        public void BuyXp_InsufficientGold_Throws()
        {
            var player = NewPlayer(hp: 60, gold: 10);
            Assert.Throws<InvalidOperationException>(() => _sut.BuyXp(player));
        }

        [Test]
        public void BuyXp_NullPlayer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _sut.BuyXp(null));
        }

        [Test]
        public void BuyXp_MultiplePurchases()
        {
            var player = NewPlayer(hp: 60, gold: 50);

            _sut.BuyXp(player);
            _sut.BuyXp(player);

            Assert.AreEqual(2, player.Xp);
            Assert.AreEqual(10, player.Gold);
        }

        // ── Constants ──────────────────────────────────────────────────

        [Test]
        public void Constants_MatchSpec()
        {
            Assert.AreEqual(10, ShopSystem.HP_GOLD_PER_UNIT);
            Assert.AreEqual(5, ShopSystem.HP_PER_UNIT);
            Assert.AreEqual(20, ShopSystem.XP_GOLD_COST);
            Assert.AreEqual(1, ShopSystem.XP_PER_PURCHASE);
        }
    }
}
