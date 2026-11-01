// RunStateTests.cs
// feature-spec F-11 RunState 동작 테스트.
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
            Assert.IsFalse(sut.IsBossNode);
            Assert.IsFalse(sut.IsRunComplete);
        }

        [Test]
        public void TotalNodes_Is7()
        {
            Assert.AreEqual(7, RunState.TOTAL_NODES);
            Assert.AreEqual(6, RunState.BOSS_NODE);
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
        public void Advance_AcrossAll7Nodes_EndsAtBossComplete()
        {
            var sut = new RunState(NewPlayer());

            // 0 → 1 → 2 → 3 → 4 → 5 → 6 (보스)
            for (int i = 0; i < 6; i++) sut.Advance();
            Assert.AreEqual(6, sut.CurrentNodeIndex);
            Assert.IsTrue(sut.IsBossNode);
            Assert.IsFalse(sut.IsRunComplete);

            // 보스 종료
            sut.Advance();
            Assert.IsTrue(sut.IsRunComplete);
        }

        [Test]
        public void Advance_AfterRunComplete_DoesNothing()
        {
            var sut = new RunState(NewPlayer());
            for (int i = 0; i <= 6; i++) sut.Advance(); // 보스까지 완료

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
            for (int i = 0; i <= 6; i++) sut.Advance();
            Assert.IsTrue(sut.IsRunComplete);

            var newPlayer = NewPlayer();
            sut.Reset(newPlayer);

            Assert.AreSame(newPlayer, sut.Player);
            Assert.AreEqual(0, sut.CurrentNodeIndex);
            Assert.IsFalse(sut.IsRunComplete);
            Assert.IsFalse(sut.IsBossNode);
        }
    }
}
