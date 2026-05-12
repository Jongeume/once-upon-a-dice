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
        private class FixedRandom : IRandom
        {
            private readonly int _value;
            public FixedRandom(int v) { _value = v; }
            public int Next(int minInclusive, int maxExclusive) => _value;
        }

        private static PlayerState NewPlayer() =>
            new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));

        private static RunManager NewManager()
        {
            var random = new FixedRandom(0);
            return new RunManager(new EncounterTable(random), random);
        }

        [Test]
        public void Constructor_NullEncounterTable_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new RunManager(null, new FixedRandom(0)));
        }

        [Test]
        public void Constructor_NullRandom_Throws()
        {
            var et = new EncounterTable(new FixedRandom(0));
            Assert.Throws<ArgumentNullException>(() => new RunManager(et, null));
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

        [Test]
        public void StartRun_SetsStateAtStartNode_NotComplete()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            Assert.IsNotNull(sut.State);
            Assert.IsNotNull(sut.Map);
            Assert.AreEqual(0, sut.State.CurrentNodeIndex);
            Assert.AreEqual(RunMap.START_NODE_ID, sut.State.CurrentNodeId);
            Assert.IsFalse(sut.IsRunComplete());
        }

        [Test]
        public void StartRun_AfterCompletion_ResetsState()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 8; i++) sut.AdvanceNode();
            Assert.IsTrue(sut.IsRunComplete());

            var newPlayer = NewPlayer();
            sut.StartRun(newPlayer);

            Assert.AreSame(newPlayer, sut.State.Player);
            Assert.AreEqual(0, sut.State.CurrentNodeIndex);
            Assert.IsFalse(sut.IsRunComplete());
        }

        [Test]
        public void GetNextBattle_AtStart_ReturnsSlimeOnly()
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
            // Advance to node 7 (Boss) — skip shops by advancing through all
            for (int i = 0; i < 7; i++) sut.AdvanceNode();

            List<MonsterData> result = sut.GetNextBattle();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_STONE_GOLEM, result[0].Id);
        }

        [Test]
        public void GetNextBattle_AtEliteNode_ReturnsEliteGolem()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            // Advance to node 5 (Elite)
            for (int i = 0; i < 5; i++) sut.AdvanceNode();

            List<MonsterData> result = sut.GetNextBattle();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_ELITE_GOLEM, result[0].Id);
        }

        [Test]
        public void GetPostBattleFlow_CombatNode_RewardAndLevelUpConditional()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowReward);
            Assert.IsFalse(flow.ShowLevelUp);
        }

        [Test]
        public void GetPostBattleFlow_CombatNode_LevelUp_WhenXpMeetsThreshold()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(2);
            sut.StartRun(player);

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowLevelUp);
            Assert.IsTrue(flow.ShowReward);
        }

        [Test]
        public void GetPostBattleFlow_BossNode_OnlyReward()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(99);
            sut.StartRun(player);
            for (int i = 0; i < 7; i++) sut.AdvanceNode();

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowReward);
            Assert.IsFalse(flow.ShowLevelUp);
        }

        [Test]
        public void GetCurrentNode_AtStart_IsCombatLayer0()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            MapNode node = sut.GetCurrentNode();

            Assert.AreEqual(RunMap.START_NODE_ID, node.Id);
            Assert.AreEqual(NodeType.Combat, node.Type);
        }

        [Test]
        public void GetAvailableNextNodes_Linear_HasOneNext()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            IReadOnlyList<MapNode> next = sut.GetAvailableNextNodes();

            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(1, next[0].Id);
        }

        [Test]
        public void GetAvailableNextNodes_FromBoss_IsEmpty()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 7; i++) sut.AdvanceNode();

            IReadOnlyList<MapNode> next = sut.GetAvailableNextNodes();

            Assert.AreEqual(0, next.Count);
        }

        [Test]
        public void SelectNextNode_ValidNext_UpdatesState()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            sut.SelectNextNode(1);

            Assert.AreEqual(1, sut.State.CurrentNodeId);
            Assert.AreEqual(1, sut.State.CurrentNodeIndex);
        }

        [Test]
        public void SelectNextNode_InvalidNode_Throws()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            Assert.Throws<ArgumentException>(() => sut.SelectNextNode(5));
        }

        [Test]
        public void SelectNextNode_AfterRunComplete_Throws()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 8; i++) sut.AdvanceNode();

            Assert.Throws<InvalidOperationException>(() => sut.SelectNextNode(0));
        }

        [Test]
        public void AdvanceNode_Linear_ProgressesToNextNode()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            sut.AdvanceNode();

            Assert.AreEqual(1, sut.State.CurrentNodeId);
        }

        [Test]
        public void AdvanceNode_FromBoss_MarksComplete()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 7; i++) sut.AdvanceNode();
            Assert.IsFalse(sut.IsRunComplete());

            sut.AdvanceNode();

            Assert.IsTrue(sut.IsRunComplete());
        }

        [Test]
        public void AdvanceNode_AfterComplete_Idempotent()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 8; i++) sut.AdvanceNode();

            sut.AdvanceNode();
            sut.AdvanceNode();

            Assert.IsTrue(sut.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_BeforeStartRun_False()
        {
            var sut = NewManager();
            Assert.IsFalse(sut.IsRunComplete());
        }

        [Test]
        public void PostBattleFlow_Constructor_StoresValues()
        {
            var flow = new PostBattleFlow(showReward: true, showLevelUp: false);

            Assert.IsTrue(flow.ShowReward);
            Assert.IsFalse(flow.ShowLevelUp);
        }
    }
}
