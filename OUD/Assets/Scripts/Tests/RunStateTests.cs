using System.Linq;
using NUnit.Framework;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Run;
using OUD.BattleEngine.Unit;

namespace OUD.Tests
{
    [TestFixture]
    public class RunStateTests
    {
        private static PlayerState NewPlayer() =>
            new PlayerState(new PlayerStats(maxHp: 60, atk: 6, def: 5));

        [Test]
        public void Constructor_StartsAtNode0_NotComplete()
        {
            var sut = new RunState(NewPlayer());

            Assert.AreEqual(0, sut.CurrentNodeIndex);
            Assert.AreEqual(RunMap.START_NODE_ID, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
            Assert.IsTrue(sut.VisitedNodeIds.Contains(RunMap.START_NODE_ID));
            Assert.AreEqual(1, sut.VisitedNodeIds.Count);
        }

        [Test]
        public void TotalNodes_Is13()
        {
            Assert.AreEqual(13, RunState.TOTAL_NODES);
            Assert.AreEqual(12, RunState.LAST_NODE);
        }

        [Test]
        public void MoveTo_UpdatesIndexAndId()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 1, layer: 1);

            Assert.AreEqual(1, sut.CurrentNodeIndex);
            Assert.AreEqual(1, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsLastNode);
            Assert.IsTrue(sut.VisitedNodeIds.Contains(RunMap.START_NODE_ID));
            Assert.IsTrue(sut.VisitedNodeIds.Contains(1));
            Assert.AreEqual(2, sut.VisitedNodeIds.Count);
        }

        [Test]
        public void MoveTo_ToLastNode_IsLastNodeTrue()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 12, layer: 8);

            Assert.AreEqual(8,  sut.CurrentNodeIndex);
            Assert.AreEqual(12, sut.CurrentNodeId);
            Assert.IsTrue(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void MarkComplete_SetsIsRunComplete()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(12, 8);

            sut.MarkComplete();

            Assert.IsTrue(sut.IsRunComplete);
        }

        [Test]
        public void MoveTo_AfterMarkComplete_DoesNothing()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(12, 8);
            sut.MarkComplete();

            int idBefore = sut.CurrentNodeId;
            sut.MoveTo(99, 0);

            Assert.IsTrue(sut.IsRunComplete);
            Assert.AreEqual(idBefore, sut.CurrentNodeId);
        }

        [Test]
        public void Reset_RestoresInitialState_WithNewPlayer()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(12, 8);
            sut.MarkComplete();

            var newPlayer = NewPlayer();
            sut.Reset(newPlayer);

            Assert.AreSame(newPlayer, sut.Player);
            Assert.AreEqual(0, sut.CurrentNodeIndex);
            Assert.AreEqual(RunMap.START_NODE_ID, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsRunComplete);
            Assert.IsFalse(sut.IsLastNode);
            Assert.AreEqual(1, sut.VisitedNodeIds.Count);
            Assert.IsTrue(sut.VisitedNodeIds.Contains(RunMap.START_NODE_ID));
        }

        [Test]
        public void VisitedNodeIds_TracksFullPath()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(2, 1);
            sut.MoveTo(4, 2);
            sut.MoveTo(5, 3);

            Assert.AreEqual(4, sut.VisitedNodeIds.Count);
            Assert.IsTrue(sut.VisitedNodeIds.Contains(RunMap.START_NODE_ID));
            Assert.IsTrue(sut.VisitedNodeIds.Contains(2));
            Assert.IsTrue(sut.VisitedNodeIds.Contains(4));
            Assert.IsTrue(sut.VisitedNodeIds.Contains(5));
            Assert.IsFalse(sut.VisitedNodeIds.Contains(1));
            Assert.IsFalse(sut.VisitedNodeIds.Contains(3));
        }
    }
}
