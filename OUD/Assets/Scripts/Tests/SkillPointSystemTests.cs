// SkillPointSystemTests.cs
// feature-spec F-09 족보 해금 규칙 테스트.
using System;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class SkillPointSystemTests
    {
        private SkillPointSystem _sut;
        private PlayerStats _defaultStats;

        [SetUp]
        public void SetUp()
        {
            _sut = new SkillPointSystem();
            _defaultStats = new PlayerStats(maxHp: 60, atk: 6, def: 5);
        }

        private PlayerState MakePlayerWithSp(int sp)
        {
            var player = new PlayerState(_defaultStats);
            // SP는 LevelUp을 통해서만 증가하므로, 필요한 만큼 레벨업 시뮬레이션
            for (int i = 0; i < sp; i++)
                player.LevelUp(player.BaseStats);
            return player;
        }

        // ── GetUnlockCost ─────────────────────────────────────────────────────

        [Test]
        public void GetUnlockCost_SmallStraight_Returns1()
        {
            Assert.AreEqual(1, _sut.GetUnlockCost(HandType.SmallStraight));
        }

        [Test]
        public void GetUnlockCost_FourOfAKind_Returns2()
        {
            Assert.AreEqual(2, _sut.GetUnlockCost(HandType.FourOfAKind));
        }

        [Test]
        public void GetUnlockCost_LargeStraight_Returns2()
        {
            Assert.AreEqual(2, _sut.GetUnlockCost(HandType.LargeStraight));
        }

        [Test]
        public void GetUnlockCost_Yahtzee_Returns3()
        {
            Assert.AreEqual(3, _sut.GetUnlockCost(HandType.Yahtzee));
        }

        [Test]
        public void GetUnlockCost_DefaultHand_ReturnsNegative()
        {
            Assert.AreEqual(-1, _sut.GetUnlockCost(HandType.OnePair));
        }

        // ── CanUnlock ─────────────────────────────────────────────────────────

        [Test]
        public void CanUnlock_SufficientSp_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanUnlock(HandType.SmallStraight, currentSp: 1));
        }

        [Test]
        public void CanUnlock_InsufficientSp_ReturnsFalse()
        {
            Assert.IsFalse(_sut.CanUnlock(HandType.Yahtzee, currentSp: 2));
        }

        [Test]
        public void CanUnlock_DefaultHand_ReturnsFalse()
        {
            Assert.IsFalse(_sut.CanUnlock(HandType.OnePair, currentSp: 99));
        }

        // ── Unlock ────────────────────────────────────────────────────────────

        [Test]
        public void Unlock_AddsToUnlockedHands()
        {
            var player = MakePlayerWithSp(1);

            _sut.Unlock(player, HandType.SmallStraight);

            Assert.IsTrue(player.UnlockedHands.Contains(HandType.SmallStraight));
        }

        [Test]
        public void Unlock_DeductsSp()
        {
            var player = MakePlayerWithSp(2);
            int spBefore = player.Sp;

            _sut.Unlock(player, HandType.FourOfAKind);

            Assert.AreEqual(spBefore - 2, player.Sp);
        }

        [Test]
        public void Unlock_AlreadyUnlocked_Throws()
        {
            var player = MakePlayerWithSp(2);
            _sut.Unlock(player, HandType.SmallStraight);

            Assert.Throws<InvalidOperationException>(
                () => _sut.Unlock(player, HandType.SmallStraight));
        }

        [Test]
        public void Unlock_DefaultHand_Throws()
        {
            var player = MakePlayerWithSp(3);

            Assert.Throws<InvalidOperationException>(
                () => _sut.Unlock(player, HandType.OnePair));
        }

        [Test]
        public void Unlock_InsufficientSp_Throws()
        {
            var player = MakePlayerWithSp(1);

            Assert.Throws<InvalidOperationException>(
                () => _sut.Unlock(player, HandType.Yahtzee));
        }

        // ── GetUnlockableHands ────────────────────────────────────────────────

        [Test]
        public void GetUnlockableHands_Sp1_ContainsSmallStraightOnly()
        {
            var player = MakePlayerWithSp(1);

            var result = _sut.GetUnlockableHands(player);

            Assert.Contains(HandType.SmallStraight, result);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetUnlockableHands_ExcludesAlreadyUnlocked()
        {
            var player = MakePlayerWithSp(3);
            _sut.Unlock(player, HandType.SmallStraight);

            var result = _sut.GetUnlockableHands(player);

            Assert.IsFalse(result.Contains(HandType.SmallStraight));
        }
    }
}
