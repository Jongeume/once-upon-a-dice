// RunMapTests.cs
// feature-spec F-11 RunMap 동작 테스트 (Phase D-2 — 1-2-1 + Boss 합류).
using System;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;

namespace OUD.Tests
{
    [TestFixture]
    public class RunMapTests
    {
        private class FixedRandom : IRandom
        {
            private readonly int _value;
            public FixedRandom(int v) { _value = v; }
            public int Next(int minInclusive, int maxExclusive) => _value;
        }

        private static RunMap NewMap() => new RunMap(new FixedRandom(0));

        // ── 구조 상수 ────────────────────────────────────────────────────────

        [Test]
        public void Constants_Define_1_2_1_Structure()
        {
            Assert.AreEqual(3, RunMap.TOTAL_LAYERS);
            Assert.AreEqual(2, RunMap.LAST_LAYER);
            Assert.AreEqual(0, RunMap.START_NODE_ID);
            Assert.AreEqual(3, RunMap.LAST_NODE_ID);
        }

        // ── 노드 개수 ────────────────────────────────────────────────────────

        [Test]
        public void NodeCount_Is4()
        {
            // 1-2-1 = 4 노드
            var map = NewMap();
            Assert.AreEqual(4, map.NodeCount);
        }

        // ── 시작 노드 ────────────────────────────────────────────────────────

        [Test]
        public void StartNode_IsCombatLayer0()
        {
            var map = NewMap();
            MapNode start = map.GetNode(RunMap.START_NODE_ID);

            Assert.AreEqual(0, start.Id);
            Assert.AreEqual(NodeType.Combat, start.Type);
            Assert.AreEqual(0, start.Layer);
            Assert.AreEqual(0, start.Column);
        }

        [Test]
        public void StartNode_BranchesToLayer1Nodes()
        {
            var map = NewMap();
            MapNode start = map.GetNode(RunMap.START_NODE_ID);

            // layer 1의 두 분기 (id=1, id=2)로 연결
            Assert.AreEqual(2, start.NextNodeIds.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 2 },
                new[] { start.NextNodeIds[0], start.NextNodeIds[1] });
        }

        // ── Layer 1 분기 노드들 ──────────────────────────────────────────────

        [Test]
        public void Layer1_Nodes_AreCombat_DifferentColumns()
        {
            var map = NewMap();
            MapNode n1 = map.GetNode(1);
            MapNode n2 = map.GetNode(2);

            Assert.AreEqual(NodeType.Combat, n1.Type);
            Assert.AreEqual(NodeType.Combat, n2.Type);
            Assert.AreEqual(1, n1.Layer);
            Assert.AreEqual(1, n2.Layer);
            Assert.AreNotEqual(n1.Column, n2.Column);
        }

        [Test]
        public void Layer1_BothBranches_LeadToBoss()
        {
            // 합류 구조: layer 1의 두 노드 모두 Boss(id=3)로 연결
            var map = NewMap();
            MapNode n1 = map.GetNode(1);
            MapNode n2 = map.GetNode(2);

            Assert.AreEqual(1, n1.NextNodeIds.Count);
            Assert.AreEqual(RunMap.LAST_NODE_ID, n1.NextNodeIds[0]);

            Assert.AreEqual(1, n2.NextNodeIds.Count);
            Assert.AreEqual(RunMap.LAST_NODE_ID, n2.NextNodeIds[0]);
        }

        // ── Boss (마지막) 노드 ──────────────────────────────────────────────

        [Test]
        public void LastNode_IsBoss_AtLastLayer()
        {
            var map = NewMap();
            MapNode boss = map.GetNode(RunMap.LAST_NODE_ID);

            Assert.AreEqual(NodeType.Boss, boss.Type);
            Assert.AreEqual(RunMap.LAST_LAYER, boss.Layer);
        }

        [Test]
        public void LastNode_HasNoNextNodes()
        {
            var map = NewMap();
            MapNode boss = map.GetNode(RunMap.LAST_NODE_ID);

            Assert.AreEqual(0, boss.NextNodeIds.Count);
        }

        // ── 조회 가드 ────────────────────────────────────────────────────────

        [Test]
        public void GetNode_UnknownId_Throws()
        {
            var map = NewMap();
            Assert.Throws<ArgumentException>(() => map.GetNode(99));
        }

        [Test]
        public void ContainsNode_KnownIds_True()
        {
            var map = NewMap();
            for (int i = 0; i <= RunMap.LAST_NODE_ID; i++)
                Assert.IsTrue(map.ContainsNode(i), $"missing nodeId {i}");
        }

        [Test]
        public void ContainsNode_UnknownId_False()
        {
            var map = NewMap();
            Assert.IsFalse(map.ContainsNode(99));
        }

        // ── MapNode struct 무결성 ────────────────────────────────────────────

        [Test]
        public void MapNode_NullNextNodeIds_DefaultsToEmpty()
        {
            var node = new MapNode(id: 0, type: NodeType.Boss, layer: 0, column: 0,
                                   nextNodeIds: null);

            Assert.IsNotNull(node.NextNodeIds);
            Assert.AreEqual(0, node.NextNodeIds.Count);
        }

        // ── 동시 인스턴스 안정성 ─────────────────────────────────────────────

        [Test]
        public void MultipleInstances_ShareIdenticalStructure()
        {
            // sprint MVP는 고정 구조 — 매 생성마다 동일한 그래프
            var a = NewMap();
            var b = NewMap();

            Assert.AreEqual(a.NodeCount, b.NodeCount);
            for (int i = 0; i <= RunMap.LAST_NODE_ID; i++)
            {
                MapNode na = a.GetNode(i);
                MapNode nb = b.GetNode(i);
                Assert.AreEqual(na.Type,   nb.Type,   $"nodeId {i} type mismatch");
                Assert.AreEqual(na.Layer,  nb.Layer,  $"nodeId {i} layer mismatch");
                Assert.AreEqual(na.Column, nb.Column, $"nodeId {i} column mismatch");
            }
        }
    }
}
