// RunStateTests.cs
// feature-spec F-11 RunState 동작 테스트 (Phase D-2 — 노드맵 도입, MoveTo/MarkComplete 분리).
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

        // ── 초기 상태 ────────────────────────────────────────────────────────

        [Test]
        public void Constructor_StartsAtNode0_NotComplete()
        {
            var sut = new RunState(NewPlayer());

            Assert.AreEqual(0,  sut.CurrentNodeIndex);
            Assert.AreEqual(RunMap.START_NODE_ID, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void TotalNodes_Is3()
        {
            // layer 수 (1-2-1 구조 = 3 layer)
            Assert.AreEqual(3, RunState.TOTAL_NODES);
            Assert.AreEqual(2, RunState.LAST_NODE);
        }

        // ── MoveTo ───────────────────────────────────────────────────────────

        [Test]
        public void MoveTo_FromLayer0ToLayer1_UpdatesIndexAndId()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 1, layer: 1);

            Assert.AreEqual(1, sut.CurrentNodeIndex);
            Assert.AreEqual(1, sut.CurrentNodeId);
            Assert.IsFalse(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void MoveTo_ToLastLayer_IsLastNodeTrue()
        {
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 3, layer: RunState.LAST_NODE);

            Assert.AreEqual(RunState.LAST_NODE, sut.CurrentNodeIndex);
            Assert.AreEqual(3, sut.CurrentNodeId);
            Assert.IsTrue(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void MoveTo_ToBranchNode2_RecordsColumnNodeId()
        {
            // 1-2-1 구조에서 layer 1의 두 분기 노드 (id=1, id=2)
            var sut = new RunState(NewPlayer());

            sut.MoveTo(nodeId: 2, layer: 1);

            Assert.AreEqual(1, sut.CurrentNodeIndex);
            Assert.AreEqual(2, sut.CurrentNodeId);
        }

        // ── MarkComplete ─────────────────────────────────────────────────────

        [Test]
        public void MarkComplete_SetsIsRunComplete()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(3, RunState.LAST_NODE);

            sut.MarkComplete();

            Assert.IsTrue(sut.IsRunComplete);
        }

        [Test]
        public void MoveTo_AfterMarkComplete_DoesNothing()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(3, RunState.LAST_NODE);
            sut.MarkComplete();

            int idBefore    = sut.CurrentNodeId;
            int layerBefore = sut.CurrentNodeIndex;
            sut.MoveTo(99, 0);

            Assert.IsTrue(sut.IsRunComplete);
            Assert.AreEqual(idBefore,    sut.CurrentNodeId);
            Assert.AreEqual(layerBefore, sut.CurrentNodeIndex);
        }

        // ── Reset ────────────────────────────────────────────────────────────

        [Test]
        public void Reset_RestoresInitialState_WithNewPlayer()
        {
            var sut = new RunState(NewPlayer());
            sut.MoveTo(3, RunState.LAST_NODE);
            sut.MarkComplete();
            Assert.IsTrue(sut.IsRunComplete);

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
