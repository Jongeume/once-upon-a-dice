# Dice Roll Animation (Phase 1) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** DiceArea의 5개 주사위에 스프라이트 프레임 교체 기반 롤링 애니메이션을 추가한다.

**Architecture:** 기존 MVP 아키텍처 유지. BattleEngine 변경 없음. IDiceEntryView 인터페이스에 PlayRoll/SetResultImmediate 추가 → DicePresenter가 순차 정지 오케스트레이션 → DiceEntryView가 Coroutine으로 스프라이트 순환.

**Tech Stack:** Unity 6, C#, Coroutine, Sprite Sheet

---

## File Map

| 파일 | 작업 | 역할 |
|------|------|------|
| `OUD/Assets/Scripts/Unity/Battle/BattleViewInterfaces.cs` | 수정 | IDiceEntryView에 PlayRoll, SetResultImmediate 추가 |
| `OUD/Assets/Scripts/Unity/Battle/Presenter/DicePresenter.cs` | 수정 | 롤링 오케스트레이션 (stagger, pending count, 버튼 잠금) |
| `OUD/Assets/Scripts/Unity/Battle/View/DiceEntryView.cs` | 수정 | TMP_Text → Image 스프라이트, Coroutine 롤링 구현 |
| `OUD/Assets/Scripts/Tests/DicePresenterTests.cs` | 생성 | DicePresenter 오케스트레이션 로직 테스트 |
| `OUD/Assets/Art/Dice/` | 생성 | DiceSpread.png + Results/1~6.png 에셋 임포트 |

---

### Task 1: IDiceEntryView 인터페이스 변경

**Files:**
- Modify: `OUD/Assets/Scripts/Unity/Battle/BattleViewInterfaces.cs:38-42`

- [ ] **Step 1: IDiceEntryView 인터페이스 업데이트**

`BattleViewInterfaces.cs`의 IDiceEntryView를 아래와 같이 변경:

```csharp
public interface IDiceEntryView
{
    void PlayRoll(int resultValue, float stopDelay, System.Action onComplete);
    void SetResultImmediate(int value);
    void SetKept(bool kept);
}
```

- `UpdateValue(int)` 제거 → `SetResultImmediate(int)` 로 대체
- `PlayRoll` 추가: 롤링 애니메이션 후 결과 표시, 완료 시 onComplete 호출

- [ ] **Step 2: 컴파일 확인**

Run: Unity Console에서 컴파일 에러 확인
Expected: `DiceEntryView`와 `DicePresenter`에서 `UpdateValue` 참조 에러 (예상대로 — 이후 Task에서 해결)

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Unity/Battle/BattleViewInterfaces.cs OUD/Assets/Scripts/Unity/Battle/BattleViewInterfaces.cs.meta
git commit -m "refactor: IDiceEntryView에 PlayRoll/SetResultImmediate 추가, UpdateValue 제거"
```

---

### Task 2: DicePresenter 오케스트레이션 테스트 작성

**Files:**
- Create: `OUD/Assets/Scripts/Tests/DicePresenterTests.cs`

- [ ] **Step 1: Mock 클래스 + 테스트 파일 생성**

```csharp
using System;
using System.Collections.Generic;
using NUnit.Framework;
using OUD.Unity.Battle;
using OUD.Unity.Battle.Presenter;

namespace OUD.Tests
{
    internal class MockDiceEntryView : IDiceEntryView
    {
        public int LastResultValue;
        public float LastStopDelay;
        public Action LastOnComplete;
        public int ImmediateValue;
        public bool Kept;
        public int PlayRollCallCount;
        public int SetResultImmediateCallCount;

        public void PlayRoll(int resultValue, float stopDelay, Action onComplete)
        {
            LastResultValue = resultValue;
            LastStopDelay = stopDelay;
            LastOnComplete = onComplete;
            PlayRollCallCount++;
        }

        public void SetResultImmediate(int value)
        {
            ImmediateValue = value;
            SetResultImmediateCallCount++;
        }

        public void SetKept(bool kept) { Kept = kept; }

        public void SimulateRollComplete() => LastOnComplete?.Invoke();
    }

    internal class MockDiceView : IDiceView
    {
        public int LastRerollsLeft;
        public bool LastCanReroll;
        public bool ConfirmActive;
        public bool Visible;
        public int UpdateRerollInfoCallCount;

        public void UpdateRerollInfo(int rerollsLeft, bool canReroll)
        {
            LastRerollsLeft = rerollsLeft;
            LastCanReroll = canReroll;
            UpdateRerollInfoCallCount++;
        }

        public void SetConfirmButtonActive(bool active) { ConfirmActive = active; }
        public void SetVisible(bool visible) { Visible = visible; }
    }

    [TestFixture]
    public class DicePresenterTests
    {
        private MockDiceView _diceView;
        private List<MockDiceEntryView> _entries;
        private List<IDiceEntryView> _entryInterfaces;
        private bool[] _lastRerollKeepMask;
        private DicePresenter _presenter;

        [SetUp]
        public void SetUp()
        {
            _diceView = new MockDiceView();
            _entries = new List<MockDiceEntryView>();
            _entryInterfaces = new List<IDiceEntryView>();
            for (int i = 0; i < 5; i++)
            {
                var entry = new MockDiceEntryView();
                _entries.Add(entry);
                _entryInterfaces.Add(entry);
            }
            _lastRerollKeepMask = null;
            _presenter = new DicePresenter(
                _diceView,
                _entryInterfaces,
                mask => _lastRerollKeepMask = mask);
        }

        [Test]
        public void UpdateDice_FirstRoll_AllDicePlayRoll()
        {
            int[] values = { 3, 1, 5, 2, 6 };
            _presenter.UpdateDice(values, 3);

            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(1, _entries[i].PlayRollCallCount,
                    $"Dice {i} should have PlayRoll called once");
                Assert.AreEqual(values[i], _entries[i].LastResultValue);
                Assert.AreEqual(0, _entries[i].SetResultImmediateCallCount);
            }
        }

        [Test]
        public void UpdateDice_FirstRoll_StaggeredDelays()
        {
            _presenter.UpdateDice(new[] { 1, 2, 3, 4, 5 }, 3);

            float prevDelay = 0f;
            for (int i = 0; i < 5; i++)
            {
                float delay = _entries[i].LastStopDelay;
                Assert.Greater(delay, 0f, $"Dice {i} delay should be positive");
                if (i > 0)
                    Assert.Greater(delay, prevDelay,
                        $"Dice {i} delay should be greater than Dice {i - 1}");
                prevDelay = delay;
            }
        }

        [Test]
        public void UpdateDice_WithKeep_KeptDiceUseSetResultImmediate()
        {
            _presenter.UpdateDice(new[] { 3, 1, 5, 2, 6 }, 3);
            foreach (var e in _entries) e.SimulateRollComplete();

            _presenter.OnDieToggleKeep(1);
            _presenter.OnDieToggleKeep(3);

            foreach (var e in _entries)
            {
                e.PlayRollCallCount = 0;
                e.SetResultImmediateCallCount = 0;
            }

            _presenter.UpdateDice(new[] { 4, 1, 2, 2, 3 }, 2);

            Assert.AreEqual(1, _entries[0].PlayRollCallCount);
            Assert.AreEqual(0, _entries[1].PlayRollCallCount);
            Assert.AreEqual(1, _entries[1].SetResultImmediateCallCount);
            Assert.AreEqual(1, _entries[2].PlayRollCallCount);
            Assert.AreEqual(0, _entries[3].PlayRollCallCount);
            Assert.AreEqual(1, _entries[3].SetResultImmediateCallCount);
            Assert.AreEqual(1, _entries[4].PlayRollCallCount);
        }

        [Test]
        public void UpdateDice_ButtonsLockedDuringRoll()
        {
            _presenter.UpdateDice(new[] { 1, 2, 3, 4, 5 }, 3);

            Assert.IsFalse(_diceView.LastCanReroll,
                "Reroll should be disabled during rolling");
            Assert.IsFalse(_diceView.ConfirmActive,
                "Confirm should be disabled during rolling");
        }

        [Test]
        public void UpdateDice_ButtonsRestoredAfterAllSettle()
        {
            _presenter.UpdateDice(new[] { 1, 2, 3, 4, 5 }, 2);

            _entries[0].SimulateRollComplete();
            _entries[1].SimulateRollComplete();
            _entries[2].SimulateRollComplete();
            _entries[3].SimulateRollComplete();

            Assert.IsFalse(_diceView.LastCanReroll,
                "Reroll should stay disabled until ALL dice settle");

            _entries[4].SimulateRollComplete();

            Assert.AreEqual(2, _diceView.LastRerollsLeft);
            Assert.IsTrue(_diceView.LastCanReroll,
                "Reroll should be enabled after all dice settle");
        }

        [Test]
        public void OnDieToggleKeep_IgnoredDuringRolling()
        {
            _presenter.UpdateDice(new[] { 1, 2, 3, 4, 5 }, 3);

            _presenter.OnDieToggleKeep(0);

            Assert.IsFalse(_entries[0].Kept,
                "Keep toggle should be ignored during rolling");
        }

        [Test]
        public void RequestReroll_IgnoredDuringRolling()
        {
            _presenter.UpdateDice(new[] { 1, 2, 3, 4, 5 }, 3);

            _presenter.RequestReroll();

            Assert.IsNull(_lastRerollKeepMask,
                "Reroll request should be ignored during rolling");
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 실패 확인**

Run: Unity Editor → Test Runner → EditMode → `DicePresenterTests`
Expected: 컴파일 에러 (DicePresenter에 아직 새 로직 없음) 또는 다수 FAIL

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Tests/DicePresenterTests.cs OUD/Assets/Scripts/Tests/DicePresenterTests.cs.meta
git commit -m "test: DicePresenter 롤링 오케스트레이션 테스트 추가"
```

---

### Task 3: DicePresenter 오케스트레이션 구현

**Files:**
- Modify: `OUD/Assets/Scripts/Unity/Battle/Presenter/DicePresenter.cs`

- [ ] **Step 1: DicePresenter 전체 교체**

```csharp
using System;
using System.Collections.Generic;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle.Presenter
{
    public class DicePresenter
    {
        private readonly IDiceView            _diceView;
        private readonly List<IDiceEntryView> _entryViews;
        private readonly Action<bool[]>       _onRerollRequested;

        private int[]  _values   = new int[5];
        private bool[] _keepMask = new bool[5];
        private int    _rerollsLeft;
        private bool   _isRolling;
        private int    _pendingCount;

        public const int   MAX_REROLLS         = 3;
        public const int   DICE_COUNT          = 5;
        public const float BASE_ROLL_DURATION  = 0.8f;
        public const float STOP_STAGGER        = 0.2f;

        public DicePresenter(
            IDiceView diceView,
            List<IDiceEntryView> entryViews,
            Action<bool[]> onRerollRequested)
        {
            _diceView          = diceView;
            _entryViews        = entryViews;
            _onRerollRequested = onRerollRequested;
        }

        public void UpdateDice(int[] values, int rerollsLeft)
        {
            _values      = values;
            _rerollsLeft = rerollsLeft;

            var rollingIndices = new List<int>();
            for (int i = 0; i < DICE_COUNT; i++)
            {
                if (_keepMask[i])
                    _entryViews[i].SetResultImmediate(values[i]);
                else
                    rollingIndices.Add(i);
            }

            if (rollingIndices.Count == 0)
            {
                bool canReroll = _rerollsLeft > 0;
                _diceView.UpdateRerollInfo(_rerollsLeft, canReroll);
                return;
            }

            _isRolling    = true;
            _pendingCount = rollingIndices.Count;

            _diceView.UpdateRerollInfo(_rerollsLeft, false);
            _diceView.SetConfirmButtonActive(false);

            for (int order = 0; order < rollingIndices.Count; order++)
            {
                int idx   = rollingIndices[order];
                float delay = BASE_ROLL_DURATION + order * STOP_STAGGER;
                _entryViews[idx].PlayRoll(values[idx], delay, OnDieSettled);
            }
        }

        private void OnDieSettled()
        {
            _pendingCount--;
            if (_pendingCount > 0) return;

            _isRolling = false;
            bool canReroll = _rerollsLeft > 0;
            _diceView.UpdateRerollInfo(_rerollsLeft, canReroll);
        }

        public void OnDieToggleKeep(int index)
        {
            if (_isRolling) return;
            _keepMask[index] = !_keepMask[index];
            _entryViews[index].SetKept(_keepMask[index]);
        }

        public void RequestReroll()
        {
            if (_isRolling) return;
            if (_rerollsLeft <= 0) return;
            _onRerollRequested?.Invoke(_keepMask);
        }

        public int    RerollsLeft   => _rerollsLeft;
        public bool[] GetKeepMask() => _keepMask;
        public bool   IsRolling     => _isRolling;

        public void ResetKeep()
        {
            for (int i = 0; i < DICE_COUNT; i++)
            {
                _keepMask[i] = false;
                _entryViews[i].SetKept(false);
            }
        }

        public void RefreshConfirmButton(int filledSlots)
        {
            if (_isRolling) return;
            bool slotsFull = filledSlots >= 3;
            bool noReroll  = _rerollsLeft <= 0;
            _diceView.SetConfirmButtonActive(slotsFull || noReroll);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 통과 확인**

Run: Unity Editor → Test Runner → EditMode → `DicePresenterTests`
Expected: 7개 테스트 모두 PASS

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Unity/Battle/Presenter/DicePresenter.cs OUD/Assets/Scripts/Unity/Battle/Presenter/DicePresenter.cs.meta
git commit -m "feat: DicePresenter 롤링 오케스트레이션 구현 (stagger, 버튼 잠금)"
```

---

### Task 4: 주사위 스프라이트 에셋 임포트

**Files:**
- Create: `OUD/Assets/Art/Dice/DiceSpread.png` + `.meta`
- Create: `OUD/Assets/Art/Dice/Results/1.png` ~ `6.png` + `.meta`

- [ ] **Step 1: 디렉토리 생성 및 파일 복사**

```bash
mkdir -p OUD/Assets/Art/Dice/Results
cp "D:/Unity/OUD_Assets/게임 에셋/3. Dice/DiceSpread.png" OUD/Assets/Art/Dice/DiceSpread.png
for i in 1 2 3 4 5 6; do
  cp "D:/Unity/OUD_Assets/게임 에셋/3. Dice/$i.png" OUD/Assets/Art/Dice/Results/$i.png
done
```

- [ ] **Step 2: Unity에서 에셋 갱신**

Unity Editor에서 Assets 폴더 우클릭 → Refresh (또는 자동 감지 대기).
`OUD/Assets/Art/Dice/` 하위에 파일들과 `.meta`가 생성되었는지 확인.

- [ ] **Step 3: DiceSpread.png 스프라이트 슬라이스**

Unity Editor에서:
1. `OUD/Assets/Art/Dice/DiceSpread.png` 선택
2. Inspector → Texture Type: `Sprite (2D and UI)`
3. Sprite Mode: `Multiple`
4. Pixels Per Unit: 기존 프로젝트 설정과 동일 (100 기본)
5. **Sprite Editor** 열기 → **Slice** → Type: `Grid By Cell Count` → Column: 14, Row: 7
6. 빈 셀(우하단 등)이 있으면 해당 프레임 삭제
7. Apply

- [ ] **Step 4: 결과 스프라이트(1~6.png) 설정**

각 파일 선택 → Texture Type: `Sprite (2D and UI)`, Sprite Mode: `Single`. Apply.

- [ ] **Step 5: Commit**

```bash
git add OUD/Assets/Art/Dice/ OUD/Assets/Art/Dice.meta
git commit -m "feat: 주사위 스프라이트 에셋 임포트 (DiceSpread + 결과 1~6)"
```

---

### Task 5: DiceEntryView 스프라이트 애니메이션 구현

**Files:**
- Modify: `OUD/Assets/Scripts/Unity/Battle/View/DiceEntryView.cs`

- [ ] **Step 1: DiceEntryView 전체 교체**

```csharp
using System;
using System.Collections;
using OUD.Unity.Battle;
using OUD.Unity.Common;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class DiceEntryView : ViewBase, IDiceEntryView
    {
        [SerializeField] private Image    _diceImage;
        [SerializeField] private Image    _background;
        [SerializeField] private Button   _button;

        [Header("스프라이트")]
        [SerializeField] private Sprite[] _rollingFrames;
        [SerializeField] private Sprite[] _resultSprites;

        [Header("애니메이션")]
        [SerializeField] private float _frameRate = 12f;
        [SerializeField] private float _decelerationDuration = 0.3f;

        [Header("Keep 하이라이트")]
        [SerializeField] private Color   _normalColor = new Color(0.09f, 0.06f, 0.04f);
        [SerializeField] private Color   _keptColor   = new Color(0.83f, 0.63f, 0.09f);
        [SerializeField] private Vector2 _borderThickness = new Vector2(4f, 4f);

        private bool    _kept;
        private Outline _outline;
        private Coroutine _rollCoroutine;

        public event Action OnToggled;

        private void Awake()
        {
            if (_button) _button.onClick.AddListener(() => OnToggled?.Invoke());

            if (_background)
            {
                _background.color = _normalColor;
                _outline = _background.GetComponent<Outline>();
                if (_outline == null) _outline = _background.gameObject.AddComponent<Outline>();
                _outline.effectDistance = _borderThickness;
                _outline.useGraphicAlpha = false;
                var hidden = _keptColor;
                hidden.a = 0f;
                _outline.effectColor = hidden;
            }
        }

        public void PlayRoll(int resultValue, float stopDelay, Action onComplete)
        {
            if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
            _rollCoroutine = StartCoroutine(RollRoutine(resultValue, stopDelay, onComplete));
        }

        public void SetResultImmediate(int value)
        {
            if (_rollCoroutine != null)
            {
                StopCoroutine(_rollCoroutine);
                _rollCoroutine = null;
            }

            if (_resultSprites != null && value >= 1 && value <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[value - 1];
        }

        public void SetKept(bool kept)
        {
            _kept = kept;
            if (_outline)
            {
                var c = _keptColor;
                c.a = kept ? 1f : 0f;
                _outline.effectColor = c;
            }
        }

        private IEnumerator RollRoutine(int resultValue, float stopDelay, Action onComplete)
        {
            if (_rollingFrames == null || _rollingFrames.Length == 0)
            {
                SetResultImmediate(resultValue);
                onComplete?.Invoke();
                yield break;
            }

            float elapsed = 0f;
            float frameInterval = 1f / _frameRate;
            float timer = 0f;
            int lastIndex = -1;

            while (elapsed < stopDelay)
            {
                timer += Time.deltaTime;
                if (timer >= frameInterval)
                {
                    timer -= frameInterval;
                    int index;
                    do { index = UnityEngine.Random.Range(0, _rollingFrames.Length); }
                    while (index == lastIndex && _rollingFrames.Length > 1);
                    lastIndex = index;
                    _diceImage.sprite = _rollingFrames[index];
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float decelElapsed = 0f;
            float[] decelFps = { _frameRate * 0.66f, _frameRate * 0.33f };
            float stepDuration = _decelerationDuration / decelFps.Length;

            for (int step = 0; step < decelFps.Length; step++)
            {
                float stepElapsed = 0f;
                float interval = 1f / decelFps[step];
                float stepTimer = 0f;
                while (stepElapsed < stepDuration)
                {
                    stepTimer += Time.deltaTime;
                    if (stepTimer >= interval)
                    {
                        stepTimer -= interval;
                        int index;
                        do { index = UnityEngine.Random.Range(0, _rollingFrames.Length); }
                        while (index == lastIndex && _rollingFrames.Length > 1);
                        lastIndex = index;
                        _diceImage.sprite = _rollingFrames[index];
                    }
                    stepElapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (_resultSprites != null && resultValue >= 1 && resultValue <= _resultSprites.Length)
                _diceImage.sprite = _resultSprites[resultValue - 1];

            _rollCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

Run: Unity Console (`read_console`)에서 에러 없는지 확인.
Expected: 컴파일 성공 (IDiceEntryView의 모든 메서드 구현 완료)

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Unity/Battle/View/DiceEntryView.cs OUD/Assets/Scripts/Unity/Battle/View/DiceEntryView.cs.meta
git commit -m "feat: DiceEntryView 스프라이트 롤링 애니메이션 구현"
```

---

### Task 6: 씬 구조 변경 (Dice1~5)

**Files:**
- Modify: BattleScene의 `DiceArea/Dice1~5` 하위 구조

이 Task는 Unity MCP를 사용하여 씬 오브젝트를 조작한다.

- [ ] **Step 1: Dice1~5 각각에서 Value(TMP) 자식을 DiceImage(Image)로 교체**

각 Dice (Dice1~5)에 대해:
1. 기존 자식 `Value` (TextMeshProUGUI) 삭제
2. 새 자식 `DiceImage` 생성 — `Image` 컴포넌트 부착
3. `DiceImage`의 RectTransform을 부모에 맞게 stretch (anchors 0,0 ~ 1,1)
4. `Image.raycastTarget = false` (클릭은 부모 Button이 처리)

- [ ] **Step 2: DiceEntryView 중복 컴포넌트 정리**

Dice1에 `DiceEntryView`가 2개 부착되어 있음 → 하나 제거.
다른 Dice(2~5)도 중복 여부 확인 후 정리.

- [ ] **Step 3: DiceEntryView SerializeField 바인딩**

각 Dice1~5의 `DiceEntryView` 컴포넌트에:
- `_diceImage` → 자식 `DiceImage`의 Image
- `_background` → 자기 자신(Dice1 등)의 Image
- `_button` → 자기 자신의 Button
- `_rollingFrames` → `DiceSpread.png` 슬라이스된 Sprite[] 전체
- `_resultSprites` → `Results/1.png` ~ `Results/6.png` (6개, 순서대로)

- [ ] **Step 4: BattleUIAdapter의 _diceEntries 참조 확인**

Inspector에서 `BattleUIAdapter._diceEntries` 리스트가 Dice1~5의 DiceEntryView를 올바르게 참조하는지 확인.
(중복 컴포넌트 제거 시 참조가 깨질 수 있으므로 재바인딩 필요할 수 있음)

- [ ] **Step 5: 씬 저장 및 Commit**

씬 저장 후:
```bash
git add OUD/Assets/Scenes/ OUD/Assets/Scenes/*.meta
git commit -m "feat: Dice1~5 씬 구조 변경 (TMP → Image 스프라이트, 중복 컴포넌트 정리)"
```

---

### Task 7: Play 모드 통합 검증

- [ ] **Step 1: 첫 롤 테스트**

1. Play 모드 진입
2. 전투 시작 → Roll Dice 클릭
3. 확인 항목:
   - 5개 주사위 모두 스프라이트 프레임 순환 (롤링 모션)
   - 순차적으로 하나씩 정지 (시간차 존재)
   - 정지 시 결과 스프라이트(1~6.png) 표시
   - 정지 후 리롤 버튼 활성화

- [ ] **Step 2: 리롤 + Keep 테스트**

1. 주사위 2개 클릭하여 Keep (하이라이트 확인)
2. 리롤 버튼 클릭
3. 확인 항목:
   - Keep된 2개: 정지 상태 유지, 애니메이션 없음
   - 나머지 3개: 롤링 → 순차 정지
   - 롤링 중 Keep 토글 불가
   - 롤링 중 리롤 버튼 비활성

- [ ] **Step 3: 리롤 소진 테스트**

1. 리롤 3회 모두 소진
2. 확인 항목:
   - 리롤 버튼 숨김/비활성
   - 확정 버튼 표시

- [ ] **Step 4: 전투 진행 테스트**

1. 기술 사용 → 타겟 선택 → 실행 → 다음 턴
2. 확인 항목:
   - 새 턴에서 Keep 초기화됨
   - 새 턴 롤링 정상 동작
   - 전투 승리/패배까지 진행 가능

- [ ] **Step 5: 최종 Commit (필요 시)**

테스트 중 발견한 미세 조정 사항 커밋:
```bash
git commit -m "fix: 주사위 롤링 애니메이션 미세 조정"
```
