// LevelUpSystemTests.cs
// feature-spec F-08 레벨업 규칙 테스트.
using System;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class LevelUpSystemTests
    {
        private LevelUpSystem _sut;
        private PlayerStats _defaultStats;

        [SetUp]
        public void SetUp()
        {
            _sut = new LevelUpSystem();
            _defaultStats = new PlayerStats(maxHp: 60, atk: 6, def: 5);
        }

        private PlayerState MakePlayer(int xp = 0, int levelUps = 0)
        {
            var player = new PlayerState(_defaultStats);
            player.AddXp(xp);
            for (int i = 0; i < levelUps; i++)
                player.LevelUp(player.BaseStats);
            return player;
        }

        // ── CanLevelUp ────────────────────────────────────────────────────────

        [Test]
        public void CanLevelUp_XpBelow2_Level0_ReturnsFalse()
        {
            Assert.IsFalse(_sut.CanLevelUp(totalXp: 1, currentLevel: 0));
        }

        [Test]
        public void CanLevelUp_XpEquals2_Level0_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanLevelUp(totalXp: 2, currentLevel: 0));
        }

        [Test]
        public void CanLevelUp_XpEquals4_Level1_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanLevelUp(totalXp: 4, currentLevel: 1));
        }

        [Test]
        public void CanLevelUp_XpEquals3_Level1_ReturnsFalse()
        {
            Assert.IsFalse(_sut.CanLevelUp(totalXp: 3, currentLevel: 1));
        }

        [Test]
        public void CanLevelUp_XpEquals6_Level2_ReturnsTrue()
        {
            Assert.IsTrue(_sut.CanLevelUp(totalXp: 6, currentLevel: 2));
        }

        [Test]
        public void CanLevelUp_Level3_AlwaysFalse()
        {
            Assert.IsFalse(_sut.CanLevelUp(totalXp: 99, currentLevel: 3));
        }

        // ── ApplyLevelUp — 스탯 반영 ──────────────────────────────────────────

        [Test]
        public void ApplyLevelUp_AtkUp_IncreasesAtk()
        {
            var player = MakePlayer(xp: 2);
            int originalAtk = player.Atk;

            _sut.ApplyLevelUp(player, StatChoice.AtkUp);

            Assert.AreEqual(originalAtk + LevelUpSystem.ATK_BONUS, player.Atk);
            Assert.AreEqual(1, player.Level);
        }

        [Test]
        public void ApplyLevelUp_DefUp_IncreasesDef()
        {
            var player = MakePlayer(xp: 2);
            int originalDef = player.Def;

            _sut.ApplyLevelUp(player, StatChoice.DefUp);

            Assert.AreEqual(originalDef + LevelUpSystem.DEF_BONUS, player.Def);
        }

        [Test]
        public void ApplyLevelUp_HpUp_IncreasesMaxHpAndCurrentHp()
        {
            var player = MakePlayer(xp: 2);
            int originalMaxHp = player.MaxHp;
            int originalHp = player.Hp;

            _sut.ApplyLevelUp(player, StatChoice.HpUp);

            Assert.AreEqual(originalMaxHp + LevelUpSystem.HP_BONUS, player.MaxHp);
            Assert.AreEqual(originalHp + LevelUpSystem.HP_BONUS, player.Hp);
        }

        // ── ApplyLevelUp — SP 자동 지급 ───────────────────────────────────────

        [Test]
        public void ApplyLevelUp_GrantsOneSp()
        {
            var player = MakePlayer(xp: 2);
            int originalSp = player.Sp;

            _sut.ApplyLevelUp(player, StatChoice.AtkUp);

            Assert.AreEqual(originalSp + 1, player.Sp);
        }

        // ── ApplyLevelUp — 예외 ───────────────────────────────────────────────

        [Test]
        public void ApplyLevelUp_InsufficientXp_Throws()
        {
            var player = MakePlayer(xp: 1);

            Assert.Throws<InvalidOperationException>(
                () => _sut.ApplyLevelUp(player, StatChoice.AtkUp));
        }

        [Test]
        public void ApplyLevelUp_NullPlayer_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => _sut.ApplyLevelUp(null, StatChoice.AtkUp));
        }
    }
}
