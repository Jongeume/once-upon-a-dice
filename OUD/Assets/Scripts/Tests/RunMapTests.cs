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

        [Test]
        public void Constants_Define_13Node_Branching_Structure()
        {
            Assert.AreEqual(9,  RunMap.TOTAL_LAYERS);
            Assert.AreEqual(8,  RunMap.LAST_LAYER);
            Assert.AreEqual(0,  RunMap.START_NODE_ID);
            Assert.AreEqual(12, RunMap.LAST_NODE_ID);
        }

        [Test]
        public void NodeCount_Is13()
        {
            var map = NewMap();
            Assert.AreEqual(13, map.NodeCount);
        }

        [Test]
        public void StartNode_IsCombatLayer0_WithTwoBranches()
        {
            var map = NewMap();
            MapNode start = map.GetNode(0);

            Assert.AreEqual(0,              start.Id);
            Assert.AreEqual(NodeType.Combat, start.Type);
            Assert.AreEqual(0,              start.Layer);
            Assert.AreEqual(2,              start.NextNodeIds.Count);
            Assert.AreEqual(1,              start.NextNodeIds[0]);
            Assert.AreEqual(2,              start.NextNodeIds[1]);
        }

        [Test]
        public void Node1_GoesTo3_Node2_GoesTo4_NoXCross()
        {
            var map = NewMap();
            MapNode n1 = map.GetNode(1);
            MapNode n2 = map.GetNode(2);

            Assert.AreEqual(1, n1.NextNodeIds.Count);
            Assert.AreEqual(3, n1.NextNodeIds[0]);

            Assert.AreEqual(1, n2.NextNodeIds.Count);
            Assert.AreEqual(4, n2.NextNodeIds[0]);
        }

        [Test]
        public void Node3_And_Node4_MergeTo_Node5_Shop()
        {
            var map = NewMap();
            MapNode n3 = map.GetNode(3);
            MapNode n4 = map.GetNode(4);
            MapNode n5 = map.GetNode(5);

            Assert.AreEqual(1,            n3.NextNodeIds.Count);
            Assert.AreEqual(5,            n3.NextNodeIds[0]);
            Assert.AreEqual(1,            n4.NextNodeIds.Count);
            Assert.AreEqual(5,            n4.NextNodeIds[0]);
            Assert.AreEqual(NodeType.Shop, n5.Type);
            Assert.AreEqual(3,             n5.Layer);
        }

        [Test]
        public void Node5_Shop_BranchesTo_6_And_7()
        {
            var map = NewMap();
            MapNode n5 = map.GetNode(5);

            Assert.AreEqual(2, n5.NextNodeIds.Count);
            Assert.AreEqual(6, n5.NextNodeIds[0]);
            Assert.AreEqual(7, n5.NextNodeIds[1]);
        }

        [Test]
        public void Node6_GoesTo8_Node7_GoesTo9_NoXCross()
        {
            var map = NewMap();
            MapNode n6 = map.GetNode(6);
            MapNode n7 = map.GetNode(7);

            Assert.AreEqual(1, n6.NextNodeIds.Count);
            Assert.AreEqual(8, n6.NextNodeIds[0]);

            Assert.AreEqual(1, n7.NextNodeIds.Count);
            Assert.AreEqual(9, n7.NextNodeIds[0]);
        }

        [Test]
        public void Node8_And_Node9_MergeTo_Node10_Elite()
        {
            var map = NewMap();
            MapNode n8  = map.GetNode(8);
            MapNode n9  = map.GetNode(9);
            MapNode n10 = map.GetNode(10);

            Assert.AreEqual(1,             n8.NextNodeIds.Count);
            Assert.AreEqual(10,            n8.NextNodeIds[0]);
            Assert.AreEqual(1,             n9.NextNodeIds.Count);
            Assert.AreEqual(10,            n9.NextNodeIds[0]);
            Assert.AreEqual(NodeType.Elite, n10.Type);
            Assert.AreEqual(6,              n10.Layer);
        }

        [Test]
        public void EndgamePath_Elite_Shop_Boss_IsLinear()
        {
            var map = NewMap();
            MapNode n10 = map.GetNode(10);
            MapNode n11 = map.GetNode(11);
            MapNode n12 = map.GetNode(12);

            Assert.AreEqual(NodeType.Elite, n10.Type);
            Assert.AreEqual(1,              n10.NextNodeIds.Count);
            Assert.AreEqual(11,             n10.NextNodeIds[0]);

            Assert.AreEqual(NodeType.Shop, n11.Type);
            Assert.AreEqual(1,             n11.NextNodeIds.Count);
            Assert.AreEqual(12,            n11.NextNodeIds[0]);

            Assert.AreEqual(NodeType.Boss, n12.Type);
            Assert.AreEqual(8,             n12.Layer);
            Assert.AreEqual(0,             n12.NextNodeIds.Count);
        }

        [Test]
        public void AllCombatNodes_HaveCorrectTypes()
        {
            var map = NewMap();
            int[] combatIds = { 0, 1, 2, 3, 4, 6, 7, 8, 9 };
            foreach (int id in combatIds)
                Assert.AreEqual(NodeType.Combat, map.GetNode(id).Type, $"Node {id} should be Combat");
        }

        [Test]
        public void LastNode_IsBoss_HasNoNextNodes()
        {
            var map = NewMap();
            MapNode boss = map.GetNode(RunMap.LAST_NODE_ID);

            Assert.AreEqual(NodeType.Boss,     boss.Type);
            Assert.AreEqual(RunMap.LAST_LAYER, boss.Layer);
            Assert.AreEqual(0,                 boss.NextNodeIds.Count);
        }

        [Test]
        public void GetNode_UnknownId_Throws()
        {
            var map = NewMap();
            Assert.Throws<ArgumentException>(() => map.GetNode(99));
        }

        [Test]
        public void ContainsNode_AllKnownIds_True()
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

        [Test]
        public void MapNode_NullNextNodeIds_DefaultsToEmpty()
        {
            var node = new MapNode(id: 0, type: NodeType.Boss, layer: 0, column: 0,
                                   nextNodeIds: null);
            Assert.IsNotNull(node.NextNodeIds);
            Assert.AreEqual(0, node.NextNodeIds.Count);
        }
    }
}
