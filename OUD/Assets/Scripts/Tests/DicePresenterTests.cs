// DicePresenterTests.cs
// DicePresenter 롤링 오케스트레이션 TDD 테스트.
// Task 3에서 DicePresenter 구현이 업데이트되면 컴파일 가능해진다.
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

        // ── Test 1: 첫 롤에서 5개 모두 PlayRoll 호출 ────────────────────────
        [Test]
        public void UpdateDice_FirstRoll_AllDicePlayRoll()
        {
            // Arrange
            int[] values = { 3, 1, 4, 1, 5 };
            int rerollsLeft = DicePresenter.MAX_REROLLS;

            // Act
            _presenter.UpdateDice(values, rerollsLeft);

            // Assert — Keep 마스크가 초기 상태(모두 false)이므로 5개 모두 PlayRoll
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                Assert.AreEqual(1, _entryMocks[i].PlayRollCallCount,
                    $"Die[{i}]에 PlayRoll이 1회 호출되어야 한다.");
                Assert.AreEqual(values[i], _entryMocks[i].LastResultValue,
                    $"Die[{i}]의 resultValue가 {values[i]}이어야 한다.");
            }
        }

        // ── Test 2: 순차 지연 — 각 후속 다이스의 stopDelay가 더 큼 ───────────
        [Test]
        public void UpdateDice_FirstRoll_StaggeredDelays()
        {
            // Arrange
            int[] values = { 2, 2, 2, 2, 2 };

            // Act
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);

            // Assert — 각 다이스의 stopDelay는 이전보다 커야 함
            for (int i = 1; i < DicePresenter.DICE_COUNT; i++)
            {
                Assert.Greater(_entryMocks[i].LastStopDelay, _entryMocks[i - 1].LastStopDelay,
                    $"Die[{i}]의 stopDelay({_entryMocks[i].LastStopDelay})가 " +
                    $"Die[{i - 1}]의 stopDelay({_entryMocks[i - 1].LastStopDelay})보다 커야 한다.");
            }

            // 첫 번째 다이스의 최소 딜레이 확인
            float expectedFirst = DicePresenter.BASE_ROLL_DURATION;
            Assert.AreEqual(expectedFirst, _entryMocks[0].LastStopDelay, 0.001f,
                "첫 번째 다이스의 stopDelay는 BASE_ROLL_DURATION이어야 한다.");

            // 각 스태거 간격 확인
            for (int i = 1; i < DicePresenter.DICE_COUNT; i++)
            {
                float expectedDelay = DicePresenter.BASE_ROLL_DURATION + DicePresenter.STOP_STAGGER * i;
                Assert.AreEqual(expectedDelay, _entryMocks[i].LastStopDelay, 0.001f,
                    $"Die[{i}]의 stopDelay는 BASE_ROLL_DURATION + STOP_STAGGER*{i} 이어야 한다.");
            }
        }

        // ── Test 3: Keep된 다이스는 SetResultImmediate, 나머지는 PlayRoll ────
        [Test]
        public void UpdateDice_WithKeep_KeptDiceUseSetResultImmediate()
        {
            // Arrange — 첫 롤 수행 후 settle
            int[] firstValues = { 1, 2, 3, 4, 5 };
            _presenter.UpdateDice(firstValues, DicePresenter.MAX_REROLLS);
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
                _entryMocks[i].SimulateRollComplete();

            // Die 1, 3을 Keep (index 1, 3) — settle 후이므로 토글 가능
            _presenter.OnDieToggleKeep(1);
            _presenter.OnDieToggleKeep(3);

            // PlayRoll 카운트 리셋을 위해 새 mock으로 비교할 초기값 기록
            int[] prePlayRollCounts = new int[DicePresenter.DICE_COUNT];
            int[] preImmediateCounts = new int[DicePresenter.DICE_COUNT];
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
            {
                prePlayRollCounts[i]  = _entryMocks[i].PlayRollCallCount;
                preImmediateCounts[i] = _entryMocks[i].SetResultImmediateCallCount;
            }

            // Act — 리롤 결과로 두 번째 UpdateDice 호출
            int[] secondValues = { 6, 2, 6, 4, 6 };
            _presenter.UpdateDice(secondValues, DicePresenter.MAX_REROLLS - 1);

            // Assert — Kept dice (index 1, 3)은 SetResultImmediate
            Assert.AreEqual(preImmediateCounts[1] + 1, _entryMocks[1].SetResultImmediateCallCount,
                "Kept Die[1]은 SetResultImmediate가 호출되어야 한다.");
            Assert.AreEqual(secondValues[1], _entryMocks[1].ImmediateValue);

            Assert.AreEqual(preImmediateCounts[3] + 1, _entryMocks[3].SetResultImmediateCallCount,
                "Kept Die[3]은 SetResultImmediate가 호출되어야 한다.");
            Assert.AreEqual(secondValues[3], _entryMocks[3].ImmediateValue);

            // Assert — Non-kept dice (index 0, 2, 4)는 PlayRoll
            Assert.AreEqual(prePlayRollCounts[0] + 1, _entryMocks[0].PlayRollCallCount,
                "Non-kept Die[0]은 PlayRoll이 호출되어야 한다.");
            Assert.AreEqual(prePlayRollCounts[2] + 1, _entryMocks[2].PlayRollCallCount,
                "Non-kept Die[2]은 PlayRoll이 호출되어야 한다.");
            Assert.AreEqual(prePlayRollCounts[4] + 1, _entryMocks[4].PlayRollCallCount,
                "Non-kept Die[4]은 PlayRoll이 호출되어야 한다.");

            // Kept dice에는 추가 PlayRoll이 없어야 함
            Assert.AreEqual(prePlayRollCounts[1], _entryMocks[1].PlayRollCallCount,
                "Kept Die[1]에 PlayRoll이 추가 호출되면 안 된다.");
            Assert.AreEqual(prePlayRollCounts[3], _entryMocks[3].PlayRollCallCount,
                "Kept Die[3]에 PlayRoll이 추가 호출되면 안 된다.");
        }

        // ── Test 4: 롤 중 버튼 비활성화 ─────────────────────────────────────
        [Test]
        public void UpdateDice_ButtonsLockedDuringRoll()
        {
            // Arrange
            int[] values = { 1, 2, 3, 4, 5 };

            // Act
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);

            // Assert — 롤 중이므로 리롤 불가, 확정 버튼 비활성
            Assert.IsFalse(_diceView.LastCanReroll,
                "롤 중에는 canReroll이 false여야 한다.");
            Assert.IsFalse(_diceView.ConfirmActive,
                "롤 중에는 ConfirmActive가 false여야 한다.");
            Assert.IsTrue(_presenter.IsRolling,
                "롤 중에는 IsRolling이 true여야 한다.");
        }

        // ── Test 5: 모든 onComplete 후 버튼 복원 ─────────────────────────────
        [Test]
        public void UpdateDice_ButtonsRestoredAfterAllSettle()
        {
            // Arrange
            int[] values = { 1, 2, 3, 4, 5 };
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);

            // Act — 모든 다이스 롤 완료 시뮬레이션
            for (int i = 0; i < DicePresenter.DICE_COUNT; i++)
                _entryMocks[i].SimulateRollComplete();

            // Assert — 롤 완료 후 canReroll 복원
            Assert.IsFalse(_presenter.IsRolling,
                "모든 다이스 settle 후 IsRolling이 false여야 한다.");
            Assert.IsTrue(_diceView.LastCanReroll,
                "롤 완료 후 canReroll이 true여야 한다 (리롤 잔여 있음).");
        }

        // ── Test 6: 롤 중 Keep 토글 무시 ─────────────────────────────────────
        [Test]
        public void OnDieToggleKeep_IgnoredDuringRolling()
        {
            // Arrange — 롤 시작
            int[] values = { 1, 2, 3, 4, 5 };
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);
            Assert.IsTrue(_presenter.IsRolling, "사전 조건: 롤 중이어야 한다.");

            bool keptBefore = _entryMocks[0].Kept;

            // Act — 롤 중에 토글 시도
            _presenter.OnDieToggleKeep(0);

            // Assert — 변경 없음
            Assert.AreEqual(keptBefore, _entryMocks[0].Kept,
                "롤 중에는 Keep 토글이 무시되어야 한다.");
        }

        // ── Test 7: 롤 중 리롤 요청 무시 ─────────────────────────────────────
        [Test]
        public void RequestReroll_IgnoredDuringRolling()
        {
            // Arrange — 롤 시작
            int[] values = { 1, 2, 3, 4, 5 };
            _presenter.UpdateDice(values, DicePresenter.MAX_REROLLS);
            Assert.IsTrue(_presenter.IsRolling, "사전 조건: 롤 중이어야 한다.");

            // Act — 롤 중에 리롤 요청
            _presenter.RequestReroll();

            // Assert — 콜백이 호출되지 않아야 함
            Assert.IsNull(_lastRerollMask,
                "롤 중에는 리롤 콜백이 호출되면 안 된다.");
        }
    }
}
