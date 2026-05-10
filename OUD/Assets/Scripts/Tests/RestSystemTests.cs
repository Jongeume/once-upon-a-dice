// RestSystemTests.cs
// feature-spec F-10 휴식 시스템 테스트.
using System;
using NUnit.Framework;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class RestSystemTests
    {
        private RestSystem _sut;
        private PlayerStats _defaultStats;

        [SetUp]
        public void SetUp()
        {
            _sut = new RestSystem();
            _defaultStats = new PlayerStats(maxHp: 60, atk: 6, def: 5);
        }

        private PlayerState MakePlayer(int gold = 50, int damage = 20)
        {
            var player = new PlayerState(_defaultStats);
            player.AddGold(gold);
            player.TakeDamage(damage);
            return player;
        }

        [Test]
        public void GetMaxInvestment_ReturnsMaxGoldInUnits()
        {
            int result = _sut.GetMaxInvestment(gold: 35, currentHp: 40, maxHp: 60);
            Assert.AreEqual(30, result);
        }

        [Test]
        public void GetMaxInvestment_FullHp_ReturnsZero()
        {
            int result = _sut.GetMaxInvestment(gold: 50, currentHp: 60, maxHp: 60);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetMaxInvestment_NoGold_ReturnsZero()
        {
            int result = _sut.GetMaxInvestment(gold: 5, currentHp: 40, maxHp: 60);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ApplyRest_BasicHealing()
        {
            var player = MakePlayer(gold: 50, damage: 20); // HP=40, Gold=50
            RestResult result = _sut.ApplyRest(player, 20);

            Assert.AreEqual(30, result.NewGold);
            Assert.AreEqual(50, result.NewHp);
            Assert.AreEqual(10, result.ActualHealed);
        }

        [Test]
        public void ApplyRest_HealCappedAtMaxHp()
        {
            var player = MakePlayer(gold: 100, damage: 5); // HP=55, Gold=100
            RestResult result = _sut.ApplyRest(player, 40); // 20 raw heal

            Assert.AreEqual(60, result.NewGold);
            Assert.AreEqual(60, result.NewHp);
            Assert.AreEqual(5, result.ActualHealed);
        }

        [Test]
        public void ApplyRest_FullHp_Throws()
        {
            var player = new PlayerState(_defaultStats);
            player.AddGold(50);

            Assert.Throws<InvalidOperationException>(() => _sut.ApplyRest(player, 10));
        }

        [Test]
        public void ApplyRest_NotMultipleOf10_Throws()
        {
            var player = MakePlayer(gold: 50, damage: 20);
            Assert.Throws<ArgumentException>(() => _sut.ApplyRest(player, 15));
        }

        [Test]
        public void ApplyRest_InsufficientGold_Throws()
        {
            var player = MakePlayer(gold: 10, damage: 20);
            Assert.Throws<InvalidOperationException>(() => _sut.ApplyRest(player, 20));
        }
    }
}
