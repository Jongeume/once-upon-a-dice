// RunStateTests.cs
// feature-spec F-11 RunState 동작 테스트 (sprint MVP — 3전투, 보스 제거).
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
            Assert.IsFalse(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void TotalNodes_Is3()
        {
            Assert.AreEqual(3, RunState.TOTAL_NODES);
            Assert.AreEqual(2, RunState.LAST_NODE);
        }

        // ── Advance ──────────────────────────────────────────────────────────

        [Test]
        public void Advance_FromNode0_GoesToNode1()
        {
            var sut = new RunState(NewPlayer());

            sut.Advance();

            Assert.AreEqual(1, sut.CurrentNodeIndex);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void Advance_AcrossAll3Nodes_EndsAtLastComplete()
        {
            var sut = new RunState(NewPlayer());

            // 0 → 1 → 2 (마지막)
            sut.Advance();
            sut.Advance();
            Assert.AreEqual(2, sut.CurrentNodeIndex);
            Assert.IsTrue(sut.IsLastNode);
            Assert.IsFalse(sut.IsRunComplete);

            // 마지막 노드 종료
            sut.Advance();
            Assert.IsTrue(sut.IsRunComplete);
        }

        [Test]
        public void Advance_AfterRunComplete_DoesNothing()
        {
            var sut = new RunState(NewPlayer());
            for (int i = 0; i < 3; i++) sut.Advance(); // 마지막까지 완료

            int idxBefore = sut.CurrentNodeIndex;
            sut.Advance();

            Assert.IsTrue(sut.IsRunComplete);
            Assert.AreEqual(idxBefore, sut.CurrentNodeIndex);
        }

        // ── Reset ────────────────────────────────────────────────────────────

        [Test]
        public void Reset_RestoresInitialState_WithNewPlayer()
        {
            var sut = new RunState(NewPlayer());
            for (int i = 0; i < 3; i++) sut.Advance();
            Assert.IsTrue(sut.IsRunComplete);

            var newPlayer = NewPlayer();
            sut.Reset(newPlayer);

            Assert.AreSame(newPlayer, sut.Player);
            Assert.AreEqual(0, sut.CurrentNodeIndex);
            Assert.IsFalse(sut.IsRunComplete);
            Assert.IsFalse(sut.IsLastNode);
        }
    }
}
