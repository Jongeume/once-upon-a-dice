// RunManagerTests.cs
// feature-spec F-11 RunManager 흐름 테스트 (Phase D-2 — 1-2-1 + Boss, 분기 선택 API).
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

        // ── 생성자 / StartRun 가드 ───────────────────────────────────────────

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
        public void GetCurrentNode_BeforeStartRun_Throws()
        {
            var sut = NewManager();
            Assert.Throws<InvalidOperationException>(() => sut.GetCurrentNode());
        }

        // ── StartRun ─────────────────────────────────────────────────────────

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
            for (int i = 0; i < 3; i++) sut.AdvanceNode();
            Assert.IsTrue(sut.IsRunComplete());

            // 새 플레이어로 재시작
            var newPlayer = NewPlayer();
            sut.StartRun(newPlayer);

            Assert.AreSame(newPlayer, sut.State.Player);
            Assert.AreEqual(0, sut.State.CurrentNodeIndex);
            Assert.AreEqual(RunMap.START_NODE_ID, sut.State.CurrentNodeId);
            Assert.IsFalse(sut.IsRunComplete());
        }

        // ── GetNextBattle (NodeType + Layer 위임) ────────────────────────────

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
            // Phase D-2: 마지막 노드는 Boss = StoneGolem 부활
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 2; i++) sut.AdvanceNode(); // layer 2 (Boss)로

            List<MonsterData> result = sut.GetNextBattle();

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(MonsterDatabase.ID_STONE_GOLEM, result[0].Id);
        }

        // ── PostBattleFlow: 일반 노드 ────────────────────────────────────────

        [Test]
        public void GetPostBattleFlow_Layer0_RewardAndRest_NoLevelUp()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowReward);
            Assert.IsTrue(flow.ShowRest);
            Assert.IsFalse(flow.ShowLevelUp);  // layer 0은 레벨업 체크 시점 아님
        }

        [Test]
        public void GetPostBattleFlow_Layer1_LevelUp_WhenXp2()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(2);
            sut.StartRun(player);
            sut.AdvanceNode(); // layer 1로

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowLevelUp);
            Assert.IsTrue(flow.ShowReward);
            Assert.IsTrue(flow.ShowRest);
        }

        [Test]
        public void GetPostBattleFlow_Layer1_NoLevelUp_WhenXpBelow2()
        {
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(1);
            sut.StartRun(player);
            sut.AdvanceNode(); // layer 1

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsFalse(flow.ShowLevelUp);
        }

        // ── PostBattleFlow: Boss 노드 (마지막) ───────────────────────────────

        [Test]
        public void GetPostBattleFlow_BossNode_OnlyReward()
        {
            // Boss 노드는 보상만 표시 (XP/Gold), 휴식/레벨업 없음
            var sut = NewManager();
            var player = NewPlayer();
            player.AddXp(99);  // XP 많아도 마지막 노드는 레벨업 없음
            sut.StartRun(player);
            for (int i = 0; i < 2; i++) sut.AdvanceNode(); // Boss layer로

            PostBattleFlow flow = sut.GetPostBattleFlow();

            Assert.IsTrue(flow.ShowReward);
            Assert.IsFalse(flow.ShowRest);
            Assert.IsFalse(flow.ShowLevelUp);
        }

        // ── 노드맵 분기 API (Phase D-2 신규) ─────────────────────────────────

        [Test]
        public void GetCurrentNode_AtStart_IsCombatLayer0()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            MapNode node = sut.GetCurrentNode();

            Assert.AreEqual(RunMap.START_NODE_ID, node.Id);
            Assert.AreEqual(NodeType.Combat, node.Type);
            Assert.AreEqual(0, node.Layer);
        }

        [Test]
        public void GetAvailableNextNodes_FromStart_HasTwoBranches()
        {
            // 1-2-1 구조: layer 0 → layer 1의 2분기 노드(1, 2)
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            IReadOnlyList<MapNode> next = sut.GetAvailableNextNodes();

            Assert.AreEqual(2, next.Count);
            var ids = new List<int> { next[0].Id, next[1].Id };
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, ids);
        }

        [Test]
        public void GetAvailableNextNodes_FromBranch_LeadsToBoss()
        {
            // layer 1의 두 분기 모두 Boss(id=3)로 합류
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            sut.SelectNextNode(1);  // 분기 1 선택

            IReadOnlyList<MapNode> next = sut.GetAvailableNextNodes();

            Assert.AreEqual(1, next.Count);
            Assert.AreEqual(RunMap.LAST_NODE_ID, next[0].Id);
            Assert.AreEqual(NodeType.Boss, next[0].Type);
        }

        [Test]
        public void GetAvailableNextNodes_FromBoss_IsEmpty()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 2; i++) sut.AdvanceNode(); // Boss로

            IReadOnlyList<MapNode> next = sut.GetAvailableNextNodes();

            Assert.AreEqual(0, next.Count);
        }

        [Test]
        public void SelectNextNode_ValidBranch_UpdatesState()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            sut.SelectNextNode(2);

            Assert.AreEqual(2, sut.State.CurrentNodeId);
            Assert.AreEqual(1, sut.State.CurrentNodeIndex);
        }

        [Test]
        public void SelectNextNode_InvalidBranch_Throws()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            // 시작 노드(0)에서 Boss(3)로 직접 갈 수 없음
            Assert.Throws<ArgumentException>(() => sut.SelectNextNode(RunMap.LAST_NODE_ID));
        }

        [Test]
        public void SelectNextNode_AfterRunComplete_Throws()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 3; i++) sut.AdvanceNode();
            Assert.IsTrue(sut.IsRunComplete());

            Assert.Throws<InvalidOperationException>(() => sut.SelectNextNode(0));
        }

        // ── AdvanceNode (호환 API) ────────────────────────────────────────────

        [Test]
        public void AdvanceNode_FromStart_AutoSelectsFirstBranch()
        {
            // 호환 API: 첫 번째 분기 후보(id=1) 자동 선택
            var sut = NewManager();
            sut.StartRun(NewPlayer());

            sut.AdvanceNode();

            Assert.AreEqual(1, sut.State.CurrentNodeId);
            Assert.AreEqual(1, sut.State.CurrentNodeIndex);
        }

        [Test]
        public void AdvanceNode_FromBoss_MarksComplete()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 2; i++) sut.AdvanceNode();
            Assert.IsFalse(sut.IsRunComplete());

            sut.AdvanceNode(); // Boss 클리어

            Assert.IsTrue(sut.IsRunComplete());
        }

        [Test]
        public void AdvanceNode_AfterComplete_Idempotent()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 3; i++) sut.AdvanceNode();
            int idBefore = sut.State.CurrentNodeId;

            sut.AdvanceNode();
            sut.AdvanceNode();

            Assert.IsTrue(sut.IsRunComplete());
            Assert.AreEqual(idBefore, sut.State.CurrentNodeId);
        }

        // ── IsRunComplete ────────────────────────────────────────────────────

        [Test]
        public void IsRunComplete_BeforeStartRun_False()
        {
            var sut = NewManager();
            Assert.IsFalse(sut.IsRunComplete());
        }

        [Test]
        public void IsRunComplete_AfterAllAdvance_True()
        {
            var sut = NewManager();
            sut.StartRun(NewPlayer());
            for (int i = 0; i < 3; i++) sut.AdvanceNode(); // layer 0 → 1 → 2 → complete

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
