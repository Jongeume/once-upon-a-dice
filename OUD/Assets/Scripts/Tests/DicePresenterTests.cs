// DicePresenterTests.cs
// DicePresenter 롤링 오케스트레이션 테스트.
// 첫 롤 = SetResultImmediate (애니메이션 없음), 리롤 = PlayRoll (애니메이션)
//
// 테스트 실행: Unity Editor → Window → General → Test Runner → EditMode
using System;
using System.Collections.Generic;
using NUnit.Framework;
using OUD.Unity.Battle;
using OUD.Unity.Battle.Presenter;

namespace OUD.Tests
{
    // ── Mock: IDiceEntryView ─────────────────────────────────────────────────
    internal class MockDiceEntryView : IDiceEntryView
    {
        public int    LastResultValue             { get; private set; }
        public float  LastStopDelay               { get; private set; }
        public Action LastOnComplete              { get; private set; }
        public int    ImmediateValue              { get; private set; }
        public bool   Kept                        { get; private set; }
        public int    PlayRollCallCount           { get; private set; }
        public int    SetResultImmediateCallCount { get; private set; }

        public void PlayRoll(int resultValue, float stopDelay, Action onComplete)
        {
            LastResultValue = resultValue;
            LastStopDelay   = stopDelay;
            LastOnComplete  = onComplete;
            PlayRollCallCount++;
        }

        public void SetResultImmediate(int value)
        {
            ImmediateValue = value;
            SetResultImmediateCallCount++;
        }

        public void SetKept(bool kept)
        {
            Kept = kept;
        }

        /// <summary>테스트에서 롤 애니메이션 완료를 시뮬레이션한다.</summary>
        public void SimulateRollComplete()
        {
            LastOnComplete?.Invoke();
        }
    }

    // ── Mock: IDiceView ──────────────────────────────────────────────────────
    internal class MockDiceView : IDiceView
    {
        public int  LastRerollsLeft          { get; private set; }
        public bool LastCanReroll            { get; private set; }
        public bool ConfirmActive            { get; private set; }
        public bool Visible                  { get; private set; }
        public int  UpdateRerollInfoCallCount { get; private set; }

        public void UpdateRerollInfo(int rerollsLeft, bool canReroll)
        {
            LastRerollsLeft = rerollsLeft;
            LastCanReroll   = canReroll;
            UpdateRerollInfoCallCount++;
        }

        public void SetConfirmButtonActive(bool active)
        {
            ConfirmActive = active;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
        }
    }

    // ── 테스트 클래스 ────────────────────────────────────────────────────────
    [TestFixture]
    public class DicePresenterTests
    {
        private MockDiceView            _diceView;
        private List<MockDiceEntryView> _entryMocks;
        private List<IDiceEntryView>    _entryViews;
        private DicePresenter           _presenter;
        private bool[]                  _lastRerollMask;

        [SetUp]
        public void SetUp()
        {
            _diceView   = new MockDiceView();
            _entryMocks = new List<MockDiceEntryView>();
            _entryViews = new List<IDiceEntryView>();

            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                var mock = new MockDiceEntryView();
                _entryMocks.Add(mock);
                _entryViews.Add(mock);
            }

            _lastRerollMask = null;
            _presenter = new DicePresenter(
                _diceView,
                _entryViews,
                mask => _lastRerollMask = mask);
        }

        /// <summary>첫 롤을 수행하고 즉시 결과를 반영(애니메이션 없음)</summary>
        private void PerformFirstRoll(int[] values = null)
        {
            values ??= new int[] { 1, 2, 3, 4, 5 };
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);
        }

        // ── Test 1: 첫 롤은 SetResultImmediate로 즉시 표시 ──────────────────
        [Test]
        public void UpdateDice_FirstRoll_AllDiceSetResultImmediate()
        {
            // Arrange
            int[] values = { 3, 1, 4, 1, 5 };

            // Act
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);

            // Assert — 첫 롤은 애니메이션 없이 즉시 결과 표시
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                Assert.AreEqual(1, _entryMocks[i].SetResultImmediateCallCount,
                    $"Die[{i}]에 SetResultImmediate가 1회 호출되어야 한다.");
                Assert.AreEqual(values[i], _entryMocks[i].ImmediateValue,
                    $"Die[{i}]의 결과 값이 {values[i]}이어야 한다.");
                Assert.AreEqual(0, _entryMocks[i].PlayRollCallCount,
                    $"Die[{i}]에 PlayRoll이 호출되면 안 된다 (첫 롤).");
            }

            // 첫 롤은 롤링 상태가 아님
            Assert.IsFalse(_presenter.IsRolling,
                "첫 롤은 즉시 표시이므로 IsRolling이 false여야 한다.");
        }

        // ── Test 2: 리롤 시 비-Keep 주사위에 PlayRoll + 순차 지연 ────────────
        [Test]
        public void UpdateDice_Reroll_StaggeredDelays()
        {
            // Arrange — 첫 롤 수행
            PerformFirstRoll();

            // Act — 리롤 (전부 non-kept)
            int[] rerollValues = { 6, 5, 4, 3, 2 };
            _presenter.UpdateDice(rerollValues, DicePresenter.MAX_REROLLS - 1);

            // Assert — 각 다이스에 PlayRoll 호출 + 순차 지연
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                Assert.AreEqual(1, _entryMocks[i].PlayRollCallCount,
                    $"Die[{i}]에 PlayRoll이 1회 호출되어야 한다 (리롤).");
            }

            for (int i = 1; i < DicePresenter.DICE_COUNT; i++)
            {
                Assert.Greater(_entryMocks[i].LastStopDelay, _entryMocks[i - 1].LastStopDelay,
                    $"Die[{i}]의 stopDelay가 Die[{i - 1}]보다 커야 한다.");
            }

            float expectedFirst = DicePresenter.BASE_ROLL_DURATION;
            Assert.AreEqual(expectedFirst, _entryMocks[0].LastStopDelay, 0.001f,
                "첫 번째 다이스의 stopDelay는 BASE_ROLL_DURATION이어야 한다.");
        }

        // ── Test 3: 리롤 시 Keep된 다이스는 SetResultImmediate ───────────────
        [Test]
        public void UpdateDice_Reroll_KeptDiceUseSetResultImmediate()
        {
            // Arrange — 첫 롤 후 Die 1, 3을 Keep
            PerformFirstRoll();
            _presenter.OnDieToggleKeep(1);
            _presenter.OnDieToggleKeep(3);

            // 카운트 기록
            int[] prePlayRollCounts = new int[DicePresenter.DICE_COUNT];
            int[] preImmediateCounts = new int[DicePresenter.DICE_COUNT];
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                prePlayRollCounts[i]  = _entryMocks[i].PlayRollCallCount;
                preImmediateCounts[i] = _entryMocks[i].SetResultImmediateCallCount;
            }

            // Act — 리롤
            int[] secondValues = { 6, 2, 6, 4, 6 };
            _presenter.UpdateDice(secondValues, DicePresenter.MAX_REROLLS - 1);

            // Assert — Kept dice (1, 3)은 SetResultImmediate
            Assert.AreEqual(preImmediateCounts[1] + 1, _entryMocks[1].SetResultImmediateCallCount);
            Assert.AreEqual(secondValues[1], _entryMocks[1].ImmediateValue);
            Assert.AreEqual(preImmediateCounts[3] + 1, _entryMocks[3].SetResultImmediateCallCount);
            Assert.AreEqual(secondValues[3], _entryMocks[3].ImmediateValue);

            // Assert — Non-kept dice (0, 2, 4)는 PlayRoll
            Assert.AreEqual(prePlayRollCounts[0] + 1, _entryMocks[0].PlayRollCallCount);
            Assert.AreEqual(prePlayRollCounts[2] + 1, _entryMocks[2].PlayRollCallCount);
            Assert.AreEqual(prePlayRollCounts[4] + 1, _entryMocks[4].PlayRollCallCount);

            // Kept에는 추가 PlayRoll 없음
            Assert.AreEqual(prePlayRollCounts[1], _entryMocks[1].PlayRollCallCount);
            Assert.AreEqual(prePlayRollCounts[3], _entryMocks[3].PlayRollCallCount);
        }

        // ── Test 4: 리롤 중 버튼 비활성화 ───────────────────────────────────
        [Test]
        public void UpdateDice_Reroll_ButtonsLockedDuringRoll()
        {
            // Arrange — 첫 롤 수행
            PerformFirstRoll();

            // Act — 리롤 시작
            _presenter.UpdateDice(new[] { 6, 6, 6, 6, 6 }, DicePresenter.MAX_REROLLS - 1);

            // Assert — 리롤 중이므로 롤링 상태
            Assert.IsTrue(_presenter.IsRolling,
                "리롤 중에는 IsRolling이 true여야 한다.");
            Assert.IsFalse(_diceView.LastCanReroll,
                "롤 중에는 canReroll이 false여야 한다.");
            Assert.IsFalse(_diceView.ConfirmActive,
                "롤 중에는 ConfirmActive가 false여야 한다.");
        }

        // ── Test 5: 모든 onComplete 후 버튼 복원 ─────────────────────────────
        [Test]
        public void UpdateDice_Reroll_ButtonsRestoredAfterAllSettle()
        {
            // Arrange — 첫 롤 + 리롤 시작
            PerformFirstRoll();
            _presenter.UpdateDice(new[] { 6, 6, 6, 6, 6 }, DicePresenter.MAX_REROLLS - 1);

            // Act — 모든 다이스 롤 완료
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
                _entryMocks[i].SimulateRollComplete();

            // Assert
            Assert.IsFalse(_presenter.IsRolling);
            Assert.IsTrue(_diceView.LastCanReroll,
                "롤 완료 후 canReroll이 true여야 한다 (리롤 잔여 있음).");
        }

        // ── Test 6: 리롤 중 Keep 토글 무시 ───────────────────────────────────
        [Test]
        public void OnDieToggleKeep_IgnoredDuringRolling()
        {
            // Arrange — 첫 롤 + 리롤로 롤링 상태 진입
            PerformFirstRoll();
            _presenter.UpdateDice(new[] { 6, 6, 6, 6, 6 }, DicePresenter.MAX_REROLLS - 1);
            Assert.IsTrue(_presenter.IsRolling);

            bool keptBefore = _entryMocks[0].Kept;

            // Act
            _presenter.OnDieToggleKeep(0);

            // Assert
            Assert.AreEqual(keptBefore, _entryMocks[0].Kept,
                "롤 중에는 Keep 토글이 무시되어야 한다.");
        }

        // ── Test 7: 리롤 중 리롤 요청 무시 ───────────────────────────────────
        [Test]
        public void RequestReroll_IgnoredDuringRolling()
        {
            // Arrange — 첫 롤 + 리롤로 롤링 상태 진입
            PerformFirstRoll();
            _presenter.UpdateDice(new[] { 6, 6, 6, 6, 6 }, DicePresenter.MAX_REROLLS - 1);
            Assert.IsTrue(_presenter.IsRolling);

            // Act
            _presenter.RequestReroll();

            // Assert
            Assert.IsNull(_lastRerollMask,
                "롤 중에는 리롤 콜백이 호출되면 안 된다.");
        }

        // ── Test 8: ResetKeep 후 다시 첫 롤로 취급 ──────────────────────────
        [Test]
        public void ResetKeep_NextUpdateDice_TreatedAsFirstRoll()
        {
            // Arrange — 첫 롤 + 리롤 완료
            PerformFirstRoll();
            _presenter.UpdateDice(new[] { 6, 6, 6, 6, 6 }, DicePresenter.MAX_REROLLS - 1);
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
                _entryMocks[i].SimulateRollComplete();

            // Act — 새 턴 시작
            _presenter.ResetKeep();
            int[] newValues = { 2, 3, 4, 5, 6 };

            int[] preImmediate = new int[DicePresenter.DICE_COUNT];
            int[] prePlayRoll = new int[DicePresenter.DICE_COUNT];
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                preImmediate[i] = _entryMocks[i].SetResultImmediateCallCount;
                prePlayRoll[i]  = _entryMocks[i].PlayRollCallCount;
            }

            _presenter.UpdateDice(newValues, DicePresenter.MAX_REROLLS);

            // Assert — 새 턴의 첫 롤은 즉시 표시
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                Assert.AreEqual(preImmediate[i] + 1, _entryMocks[i].SetResultImmediateCallCount,
                    $"Die[{i}] ResetKeep 후 첫 롤은 SetResultImmediate여야 한다.");
                Assert.AreEqual(prePlayRoll[i], _entryMocks[i].PlayRollCallCount,
                    $"Die[{i}] ResetKeep 후 첫 롤은 PlayRoll이 호출되면 안 된다.");
            }
            Assert.IsFalse(_presenter.IsRolling);
        }
    }
}
