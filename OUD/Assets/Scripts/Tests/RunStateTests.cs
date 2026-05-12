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
        }

        [Test]
        public void TotalNodes_Is8()
        {
            Assert.AreEqual(8, RunState.TOTAL_NODES);
            Assert.AreEqual(7, RunState.LAST_NODE);
        }

        [Test]
        public void MoveTo_UpdatesIndexAndId()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 1, layer: 1);

            Assert.AreEqual(1, sut.CurrentNodeIndex);
            Assert.AreEqual(1, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsLastNode);
        }

        [Test]
        public void MoveTo_ToLastLayer_IsLastNodeTrue()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 7, layer: RunState.LAST_NODE);

            Assert.AreEqual(RunState.LAST_NODE, sut.CurrentNodeIndex);
            Assert.IsTrue(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void MarkComplete_SetsIsRunComplete()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(7, RunState.LAST_NODE);

            sut.MarkComplete();

            Assert.IsTrue(sut.IsRunComplete);
        }

        [Test]
        public void MoveTo_AfterMarkComplete_DoesNothing()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(7, RunState.LAST_NODE);
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
            sut.MoveTo(7, RunState.LAST_NODE);
            sut.MarkComplete();

            var newPlayer = NewPlayer();
            sut.Reset(newPlayer);

            Assert.AreSame(newPlayer, sut.Player);
            Assert.AreEqual(0, sut.CurrentNodeIndex);
            Assert.AreEqual(RunMap.START_NODE_ID, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsRunComplete);
            Assert.IsFalse(sut.IsLastNode);
        }
    }
}
