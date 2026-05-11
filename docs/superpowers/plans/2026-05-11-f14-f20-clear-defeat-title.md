# F-14 Clear/Defeat UI + F-20 Title Screen Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Complete the game loop by adding Victory/Defeat screens that return to a new title screen.

**Architecture:** Three new ViewBase scripts (ClearView, DefeatView, TitleController), modifications to BattleUIAdapter (route Boss clear/defeat to new views) and BattleBootstrapper (rollDice guard). MCP for Unity used for all scene work. No BattleEngine changes.

**Tech Stack:** Unity 6 / C# / TextMeshPro / UnityEngine.SceneManagement

**Design spec:** `docs/superpowers/specs/2026-05-11-f14-f20-clear-defeat-title-design.md`

---

## File Map

| File | Action | Responsibility |
|------|--------|----------------|
| `OUD/Assets/Scripts/Unity/Battle/View/ClearView.cs` | Create | VICTORY panel — summary display + restart event |
| `OUD/Assets/Scripts/Unity/Battle/View/DefeatView.cs` | Create | DEFEAT panel — reached node display + restart event |
| `OUD/Assets/Scripts/Unity/TitleController.cs` | Create | Title scene — '새 게임' button → BattleScene |
| `OUD/Assets/Scripts/Unity/Adapter/BattleUIAdapter.cs` | Modify | Add ClearView/DefeatView SerializeFields, wiring, ShowClearScreen/ShowDefeatScreen, modify FinishPostBattle + OnBattleLost |
| `OUD/Assets/Scripts/Unity/BattleBootstrapper.cs` | Modify | Add `_rollDiceWired` guard |
| BattleScene.unity (via MCP) | Modify | Add ClearPanel + DefeatPanel to BattleCanvas |
| TitleScene.unity (via MCP) | Create | New scene with Canvas + title + button |

---

## Task 1: Create ClearView.cs

**Files:**
- Create: `OUD/Assets/Scripts/Unity/Battle/View/ClearView.cs`

- [ ] **Step 1: Create ClearView.cs**

```csharp
using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class ClearView : ViewBase
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _goldText;
        [SerializeField] private Button   _restartButton;

        public event Action OnRestartClicked;

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        public void SetSummary(int level, int gold)
        {
            if (_levelText != null) _levelText.text = $"Level: {level}";
            if (_goldText != null)  _goldText.text  = $"Gold: {gold}";
        }
    }
}
```

- [ ] **Step 2: Verify compilation via Unity console**

Use `mcp__UnityMCP__read_console` to check for errors. Expected: no compilation errors.

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Unity/Battle/View/ClearView.cs OUD/Assets/Scripts/Unity/Battle/View/ClearView.cs.meta
git commit -m "feat(ui): ClearView — VICTORY 패널 ViewBase 구현"
```

---

## Task 2: Create DefeatView.cs

**Files:**
- Create: `OUD/Assets/Scripts/Unity/Battle/View/DefeatView.cs`

- [ ] **Step 1: Create DefeatView.cs**

```csharp
using System;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    public class DefeatView : ViewBase
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _reachedNodeText;
        [SerializeField] private Button   _restartButton;

        public event Action OnRestartClicked;

        private void Awake()
        {
            if (_restartButton != null)
                _restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        }

        public void SetDefeatInfo(int reachedNode, int totalNodes)
        {
            if (_reachedNodeText != null)
                _reachedNodeText.text = $"도달 노드: {reachedNode}/{totalNodes}";
        }
    }
}
```

- [ ] **Step 2: Verify compilation via Unity console**

Use `mcp__UnityMCP__read_console` to check for errors. Expected: no compilation errors.

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Unity/Battle/View/DefeatView.cs OUD/Assets/Scripts/Unity/Battle/View/DefeatView.cs.meta
git commit -m "feat(ui): DefeatView — DEFEAT 패널 ViewBase 구현"
```

---

## Task 3: Create TitleController.cs

**Files:**
- Create: `OUD/Assets/Scripts/Unity/TitleController.cs`

- [ ] **Step 1: Create TitleController.cs**

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OUD.Unity
{
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;

        private void Awake()
        {
            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        private void OnNewGameClicked()
        {
            SceneManager.LoadScene("BattleScene");
        }
    }
}
```

- [ ] **Step 2: Verify compilation via Unity console**

Use `mcp__UnityMCP__read_console` to check for errors. Expected: no compilation errors.

- [ ] **Step 3: Commit**

```bash
git add OUD/Assets/Scripts/Unity/TitleController.cs OUD/Assets/Scripts/Unity/TitleController.cs.meta
git commit -m "feat(ui): TitleController — 타이틀 씬 '새 게임' 버튼 컨트롤러"
```

---

## Task 4: Modify BattleUIAdapter.cs — Clear/Defeat wiring

**Files:**
- Modify: `OUD/Assets/Scripts/Unity/Adapter/BattleUIAdapter.cs`

- [ ] **Step 1: Add imports**

Add at the top of the file, after existing using statements:

```csharp
using UnityEngine.SceneManagement;
```

- [ ] **Step 2: Add SerializeFields for ClearView and DefeatView**

After the existing `[Header("Growth Views (F-10/F-08/F-09)")]` block (after line 38 `private RestView _restView;`), add:

```csharp
[Header("End-of-Run Views (F-14)")]
[SerializeField] private ClearView  _clearView;
[SerializeField] private DefeatView _defeatView;
```

- [ ] **Step 3: Add wiring guard**

After the existing `private bool _growthViewsWired;` field (line 91), add:

```csharp
private bool _endRunViewsWired;
```

- [ ] **Step 4: Wire events in Initialize()**

At the end of the `Initialize()` method, after the `WireGrowthViews();` call (line 141), add:

```csharp
if (!_endRunViewsWired)
{
    if (_clearView != null)
        _clearView.OnRestartClicked += HandleRestartClicked;
    if (_defeatView != null)
        _defeatView.OnRestartClicked += HandleRestartClicked;
    _endRunViewsWired = true;
}
```

- [ ] **Step 5: Add HandleRestartClicked, ShowClearScreen, ShowDefeatScreen methods**

Add these methods after the `HandleRestSkipped()` method (after line 277):

```csharp
private void HandleRestartClicked()
{
    SceneManager.LoadScene("TitleScene");
}

private void ShowClearScreen()
{
    if (_clearView == null) return;
    PlayerState player = _playerPresenter.Player;
    _clearView.SetSummary(player.Level + 1, player.Gold);
    _clearView.Show();
}

private void ShowDefeatScreen()
{
    if (_defeatView == null) return;
    int reached = _runManager?.State?.CurrentNodeIndex ?? 0;
    int total = RunState.TOTAL_NODES;
    _defeatView.SetDefeatInfo(reached + 1, total);
    _defeatView.Show();
}
```

- [ ] **Step 6: Modify FinishPostBattle() — Boss branch shows ClearScreen**

Replace the Boss branch in `FinishPostBattle()`. Current code (lines 284-289):

```csharp
if (isBossNode)
{
    _runManager.AdvanceNode();
    _onContinueRequested?.Invoke();
    return;
}
```

Replace with:

```csharp
if (isBossNode)
{
    _runManager.AdvanceNode();
    ShowClearScreen();
    return;
}
```

- [ ] **Step 7: Modify OnBattleLost() — show DefeatScreen**

Replace the existing `OnBattleLost()` (line 558):

```csharp
public void OnBattleLost() => _battleLogPresenter.ShowBattleLost();
```

With:

```csharp
public void OnBattleLost()
{
    _battleLogPresenter.ShowBattleLost();
    ShowDefeatScreen();
}
```

- [ ] **Step 8: Verify compilation via Unity console**

Use `mcp__UnityMCP__read_console` to check for errors. Expected: no compilation errors.

- [ ] **Step 9: Commit**

```bash
git add OUD/Assets/Scripts/Unity/Adapter/BattleUIAdapter.cs
git commit -m "feat(ui): BattleUIAdapter — ClearView/DefeatView 배선 + Boss클리어/패배 화면 연동"
```

---

## Task 5: Modify BattleBootstrapper.cs — rollDice guard

**Files:**
- Modify: `OUD/Assets/Scripts/Unity/BattleBootstrapper.cs`

- [ ] **Step 1: Add `_rollDiceWired` field**

After the existing field declarations (after line 42 `private BattleState _state;`), add:

```csharp
private bool _rollDiceWired;
```

- [ ] **Step 2: Modify InitRun() — guard the button listener**

Replace the button registration block in `InitRun()`. Current code (lines 72-76):

```csharp
if (_rollDiceButton != null)
    _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
else
    Debug.LogWarning("[BattleBootstrapper] Roll Dice 버튼이 바인딩되지 않았습니다.");
```

Replace with:

```csharp
if (_rollDiceButton != null && !_rollDiceWired)
{
    _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
    _rollDiceWired = true;
}
else if (_rollDiceButton == null)
{
    Debug.LogWarning("[BattleBootstrapper] Roll Dice 버튼이 바인딩되지 않았습니다.");
}
```

- [ ] **Step 3: Verify compilation via Unity console**

Use `mcp__UnityMCP__read_console` to check for errors. Expected: no compilation errors.

- [ ] **Step 4: Commit**

```bash
git add OUD/Assets/Scripts/Unity/BattleBootstrapper.cs
git commit -m "fix(ui): BattleBootstrapper — _rollDiceWired 가드로 재시작 시 중복 리스너 방지"
```

---

## Task 6: Scene Work — Add ClearPanel + DefeatPanel to BattleScene

**Files:**
- Modify: BattleScene.unity (via MCP for Unity tools)

This task uses MCP for Unity to create UI elements in the BattleScene. All operations target the existing `BattleCanvas` GameObject.

- [ ] **Step 1: Verify BattleCanvas exists**

Use `mcp__UnityMCP__find_gameobjects` to find "BattleCanvas" and confirm it exists.

- [ ] **Step 2: Create ClearPanel hierarchy**

Using `mcp__UnityMCP__manage_ui` or equivalent MCP tools, create:

1. **ClearPanel** — Panel child of BattleCanvas
   - RectTransform: anchors (0.5, 0.5, 0.5, 0.5), sizeDelta (700, 520), anchoredPosition (0, 0)
   - Image component: Color(0, 0, 0, 0.85)
   - Add `ClearView` component (namespace: `OUD.Unity.Battle.View`)
   - Set inactive by default

2. **ClearPanel/TitleText** — TextMeshPro child
   - Text: "VICTORY"
   - Font size: 36, Color: #FFD700 (gold)
   - Alignment: center
   - Font: NotoSansKR-Regular SDF
   - RectTransform: anchored near top of panel

3. **ClearPanel/LevelText** — TextMeshPro child
   - Text: "Level: 1"
   - Font size: 20
   - Font: NotoSansKR-Regular SDF
   - RectTransform: center area

4. **ClearPanel/GoldText** — TextMeshPro child
   - Text: "Gold: 0"
   - Font size: 20
   - Font: NotoSansKR-Regular SDF
   - RectTransform: below LevelText

5. **ClearPanel/RestartButton** — Button (TextMeshPro) child
   - Button text: "재시작"
   - Font: NotoSansKR-Regular SDF
   - RectTransform: anchored near bottom of panel

6. **Bind ClearView SerializeFields** — Use MCP set_property or equivalent:
   - `_titleText` → ClearPanel/TitleText
   - `_levelText` → ClearPanel/LevelText
   - `_goldText` → ClearPanel/GoldText
   - `_restartButton` → ClearPanel/RestartButton

- [ ] **Step 3: Create DefeatPanel hierarchy**

Same structure as ClearPanel with different content:

1. **DefeatPanel** — Panel child of BattleCanvas
   - RectTransform: anchors (0.5, 0.5, 0.5, 0.5), sizeDelta (700, 520), anchoredPosition (0, 0)
   - Image component: Color(0, 0, 0, 0.85)
   - Add `DefeatView` component (namespace: `OUD.Unity.Battle.View`)
   - Set inactive by default

2. **DefeatPanel/TitleText** — TextMeshPro child
   - Text: "DEFEAT"
   - Font size: 36, Color: #FF4444 (red)
   - Alignment: center
   - Font: NotoSansKR-Regular SDF
   - RectTransform: anchored near top of panel

3. **DefeatPanel/ReachedNodeText** — TextMeshPro child
   - Text: "도달 노드: 0/3"
   - Font size: 20
   - Font: NotoSansKR-Regular SDF
   - RectTransform: center area

4. **DefeatPanel/RestartButton** — Button (TextMeshPro) child
   - Button text: "재시작"
   - Font: NotoSansKR-Regular SDF
   - RectTransform: anchored near bottom of panel

5. **Bind DefeatView SerializeFields**:
   - `_titleText` → DefeatPanel/TitleText
   - `_reachedNodeText` → DefeatPanel/ReachedNodeText
   - `_restartButton` → DefeatPanel/RestartButton

- [ ] **Step 4: Bind BattleUIAdapter SerializeFields for ClearView/DefeatView**

On the BattleCanvas GameObject (which has the BattleUIAdapter component), bind:
- `_clearView` → ClearPanel (ClearView component)
- `_defeatView` → DefeatPanel (DefeatView component)

- [ ] **Step 5: Save BattleScene**

Use `mcp__UnityMCP__manage_scene` to save the scene.

- [ ] **Step 6: Commit scene changes**

```bash
git add OUD/Assets/Scenes/BattleScene.unity
git commit -m "feat(ui): BattleScene에 ClearPanel/DefeatPanel 추가 + SerializeField 바인딩"
```

---

## Task 7: Scene Work — Create TitleScene

**Files:**
- Create: TitleScene.unity (via MCP for Unity tools)

- [ ] **Step 1: Create new scene**

Use `mcp__UnityMCP__manage_scene` to create a new scene "TitleScene" at `Assets/Scenes/TitleScene.unity`.

- [ ] **Step 2: Add Camera and Light**

Ensure the scene has:
- Main Camera
- Directional Light

- [ ] **Step 3: Create TitleCanvas hierarchy**

1. **TitleCanvas** — Canvas (Screen Space - Overlay)
   - Canvas Scaler: Scale With Screen Size, reference 1920x1080

2. **TitleCanvas/TitleImage** — Image or TextMeshPro
   - Display game title "Once Upon a Dice"
   - Centered, upper area of screen
   - Font: NotoSansKR-Regular SDF (if text), 48pt

3. **TitleCanvas/NewGameButton** — Button (TextMeshPro)
   - Text: "새 게임"
   - Font: NotoSansKR-Regular SDF
   - Centered, lower area of screen

4. **TitleCanvas** — Add `TitleController` component (namespace: `OUD.Unity`)
   - Bind `_newGameButton` → TitleCanvas/NewGameButton

- [ ] **Step 4: Save TitleScene**

Use `mcp__UnityMCP__manage_scene` to save.

- [ ] **Step 5: Add both scenes to Build Settings**

Use `mcp__UnityMCP__manage_build` or `mcp__UnityMCP__execute_code` to set Build Settings:
- TitleScene = index 0
- BattleScene = index 1

```csharp
// Execute via MCP execute_code:
var scenes = new UnityEditor.EditorBuildSettingsScene[]
{
    new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
    new UnityEditor.EditorBuildSettingsScene("Assets/Scenes/BattleScene.unity", true)
};
UnityEditor.EditorBuildSettings.scenes = scenes;
```

- [ ] **Step 6: Commit scene and meta files**

```bash
git add OUD/Assets/Scenes/TitleScene.unity OUD/Assets/Scenes/TitleScene.unity.meta
git commit -m "feat(ui): TitleScene 신규 — 게임 타이틀 + '새 게임' 버튼"
```

---

## Task 8: Verification — Play Mode Testing

- [ ] **Step 1: Enter Play Mode from TitleScene**

Use `mcp__UnityMCP__manage_editor` to load TitleScene and enter Play Mode. Verify:
- Title screen displays with game title and '새 게임' button

- [ ] **Step 2: Test '새 게임' → BattleScene transition**

Click '새 게임' button. Verify BattleScene loads and battle starts normally.

- [ ] **Step 3: Test normal battle flow**

Play through a normal battle victory → reward → continue → nodemap. Confirm existing flow is unchanged.

- [ ] **Step 4: Test Boss victory → ClearPanel**

Play through to Boss victory (or use debug tools). After reward [계속]:
- ClearPanel appears with "VICTORY", level, gold
- [재시작] button visible

- [ ] **Step 5: Test ClearPanel [재시작] → TitleScene**

Click [재시작]. Verify return to TitleScene.

- [ ] **Step 6: Test defeat → DefeatPanel**

Trigger a defeat (let player HP reach 0). Verify:
- DefeatPanel appears with "DEFEAT" and reached node info
- [재시작] returns to TitleScene

- [ ] **Step 7: Test full restart cycle**

From TitleScene after a restart, click '새 게임' again. Verify:
- Fresh run starts (HP 60, ATK 6, DEF 5)
- Roll Dice button works (no duplicate listener bug)

- [ ] **Step 8: Exit Play Mode and commit any final adjustments**

```bash
git add -A
git commit -m "fix(ui): 검증 후 수정 사항 (있는 경우)"
```
