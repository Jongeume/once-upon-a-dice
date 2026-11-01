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
        public void Constants_Define_8Node_Linear_Structure()
        {
            Assert.AreEqual(8, RunMap.TOTAL_LAYERS);
            Assert.AreEqual(7, RunMap.LAST_LAYER);
            Assert.AreEqual(0, RunMap.START_NODE_ID);
            Assert.AreEqual(7, RunMap.LAST_NODE_ID);
        }

        [Test]
        public void NodeCount_Is8()
        {
            var map = NewMap();
            Assert.AreEqual(8, map.NodeCount);
        }

        [Test]
        public void StartNode_IsCombatLayer0()
        {
            var map = NewMap();
            MapNode start = map.GetNode(RunMap.START_NODE_ID);

            Assert.AreEqual(0, start.Id);
            Assert.AreEqual(NodeType.Combat, start.Type);
            Assert.AreEqual(0, start.Layer);
        }

        [Test]
        public void Linear_EachNode_HasOneNextExceptBoss()
        {
            var map = NewMap();
            for (int i = 0; i < RunMap.LAST_NODE_ID; i++)
            {
                MapNode node = map.GetNode(i);
                Assert.AreEqual(1, node.NextNodeIds.Count, $"Node {i} should have 1 next");
                Assert.AreEqual(i + 1, node.NextNodeIds[0], $"Node {i} should point to {i + 1}");
            }
        }

        [Test]
        public void NodeTypes_MatchExpected()
        {
            var map = NewMap();
            Assert.AreEqual(NodeType.Combat, map.GetNode(0).Type);
            Assert.AreEqual(NodeType.Combat, map.GetNode(1).Type);
            Assert.AreEqual(NodeType.Shop,   map.GetNode(2).Type);
            Assert.AreEqual(NodeType.Combat, map.GetNode(3).Type);
            Assert.AreEqual(NodeType.Combat, map.GetNode(4).Type);
            Assert.AreEqual(NodeType.Elite,  map.GetNode(5).Type);
            Assert.AreEqual(NodeType.Shop,   map.GetNode(6).Type);
            Assert.AreEqual(NodeType.Boss,   map.GetNode(7).Type);
        }

        [Test]
        public void LastNode_IsBoss_HasNoNextNodes()
        {
            var map = NewMap();
            MapNode boss = map.GetNode(RunMap.LAST_NODE_ID);

            Assert.AreEqual(NodeType.Boss, boss.Type);
            Assert.AreEqual(RunMap.LAST_LAYER, boss.Layer);
            Assert.AreEqual(0, boss.NextNodeIds.Count);
        }

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
