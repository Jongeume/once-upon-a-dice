// RunManagerTests.cs
// feature-spec F-11 RunManager 흐름 테스트.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class RunManagerTests
    {
        // ── Helper ───────────────────────────────────────────────────────────
        private class FixedRandom : IRandom
        {
            private readonly int _value;
            public FixedRandom(int v) { _value = v; }
            public int Next(int minInclusive, int maxExclusive) => _value;
        }

        private static PlayerState NewPlayer() =>
            new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));

        private static RunManager NewManager() =>
            new RunManager(new EncounterTable(new FixedRandom(0)));

        // ── 생성자 / StartRun 가드 ───────────────────────────────────────────

        [Test]
        public void Constructor_NullEncounterTable_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RunManager(null));
        }

        [Test]
        public void StartRun_NullPlayer_Throws()
        {
            var sut = NewManager();
            Assert.Throws<ArgumentNullException>(() => sut.StartRun(null));
        }

        [Test]
        public void GetNextBattle_BeforeStartRun_Throws()
        {
            var sut = NewManager();
            Assert.Throws<InvalidOperationException>(() => sut.GetNextBattle());
        }

        // ── StartRun ─────────────────────────────────────────────────────────

        [Test]
        public void StartRun_SetsStateAtNode0_NotComplete()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            Assert.IsNotNull(sut.State);
            Assert.AreEqual(0, sut.State.CurrentNodeIndex);
            Assert.IsFalse(sut.IsRunComplete());
        }

        [Test]
        public void StartRun_AfterCompletion_ResetsState()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i <= 6; i++) sut.AdvanceNode();
            Assert.IsTrue(sut.IsRunComplete());

            // 새 플레이어로 재시작
            var newPlayer = NewPlayer();
            sut.StartRun(newPlayer);

            Assert.AreSame(newPlayer, sut.State.Player);
            Assert.AreEqual(0, sut.State.CurrentNodeIndex);
            Assert.IsFalse(sut.IsRunComplete());
        }

        // ── GetNextBattle 위임 ───────────────────────────────────────────────

        [Test]
        public void GetNextBattle_AtNode0_ReturnsEarlyEncounter()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            List<MonsterData> result = sut.GetNextBattle();

            Assert.IsNotNull(result);
            Assert.GreaterOrEqual(result.Count, 1);
            foreach (var m in result)
                Assert.AreEqual(MonsterDatabase.ID_SLIME, m.Id);
        }

        [Test]
        public void GetNextBattle_AtBossNode_ReturnsStoneGolem()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 6; i++) sut.AdvanceNode();

            List<MonsterData> result = sut.GetNextBattle();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_STONE_GOLEM, result[0].Id);
        }

        // ── PostBattleFlow: 일반 노드 ────────────────────────────────────────

        [Test]
        public void GetPostBattleFlow_Node0_RewardAndRest_NoLevelUp()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowReward);
            Assert.IsTrue(flow.ShowRest);
            Assert.IsFalse(flow.ShowLevelUp);  // 노드 0은 레벨업 체크 시점 아님
        }

        [Test]
        public void GetPostBattleFlow_Node1_LevelUp_WhenXp2()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(2);
            sut.StartRun(player);
            sut.AdvanceNode(); // 노드 1로

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowLevelUp);
        }

        [Test]
        public void GetPostBattleFlow_Node1_NoLevelUp_WhenXpBelow2()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(1);
            sut.StartRun(player);
            sut.AdvanceNode(); // 노드 1

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsFalse(flow.ShowLevelUp);
        }

        [Test]
        public void GetPostBattleFlow_Node3_LevelUp_WhenXp4()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(4);
            sut.StartRun(player);
            for (int i = 0; i < 3; i++) sut.AdvanceNode(); // 노드 3

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowLevelUp);
        }

        [Test]
        public void GetPostBattleFlow_Node5_LevelUp_WhenXp6()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(6);
            sut.StartRun(player);
            for (int i = 0; i < 5; i++) sut.AdvanceNode(); // 노드 5

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowLevelUp);
        }

        [Test]
        public void GetPostBattleFlow_Node2_NeverLevelUp_RegardlessOfXp()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(99);
            sut.StartRun(player);
            for (int i = 0; i < 2; i++) sut.AdvanceNode(); // 노드 2 (체크 시점 아님)

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsFalse(flow.ShowLevelUp);
        }

        // ── PostBattleFlow: 보스 노드 ────────────────────────────────────────

        [Test]
        public void GetPostBattleFlow_BossNode_AllFalse()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(99);
            sut.StartRun(player);
            for (int i = 0; i < 6; i++) sut.AdvanceNode(); // 노드 6 (보스)

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsFalse(flow.ShowReward);
            Assert.IsFalse(flow.ShowRest);
            Assert.IsFalse(flow.ShowLevelUp);
        }

        // ── IsRunComplete ────────────────────────────────────────────────────

        [Test]
        public void IsRunComplete_BeforeStartRun_False()
        {
            var sut = NewManager();
            Assert.IsFalse(sut.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_AfterBossAdvance_True()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i <= 6; i++) sut.AdvanceNode(); // 보스 노드 통과

            Assert.IsTrue(sut.IsRunComplete());
        }

        // ── PostBattleFlow struct 무결성 ─────────────────────────────────────

        [Test]
        public void PostBattleFlow_Constructor_StoresValues()
        {
            var flow = new PostBattleFlow(showReward: true, showLevelUp: false, showRest: true);

            Assert.IsTrue(flow.ShowReward);
            Assert.IsFalse(flow.ShowLevelUp);
            Assert.IsTrue(flow.ShowRest);
        }
    }
}
