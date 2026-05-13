# F-14 Clear/Defeat UI + F-20 Title Screen Design

**Date:** 2026-05-11  
**Scope:** F-14 (Clear/Defeat UI), F-20 (Title Screen)  
**Excluded:** F-07 Boss Rage UI (deferred)

---

## 1. Overview

Fill the two missing holes in the game loop:
- **Run clear (Boss victory):** Currently logs to console and stops
- **Run defeat (player death):** Shows a bare `_loseScreen` text with no restart

Add a title screen as the game's entry point and unify all end-of-run flows to return to it.

### Game Flow After Implementation

```
TitleScene ['새 게임'] → BattleScene (battle loop)
  → Normal victory → Reward → LevelUp/Rest → NodeMap → next battle
  → Boss victory   → Reward → ClearPanel ("VICTORY" + summary + [재시작]) → TitleScene
  → Defeat         → DefeatPanel ("DEFEAT" + reached node + [재시작])     → TitleScene
```

---

## 2. F-20: Title Screen

### TitleScene.unity (new scene, Build Settings index 0)

Minimal scene:
- Canvas with game title image ("Once Upon a Dice")
- Single '새 게임' button
- Camera + Directional Light

### TitleController.cs (new MonoBehaviour)

```
Namespace: OUD.Unity
Path: Assets/Scripts/Unity/TitleController.cs

Responsibilities:
- Wire '새 게임' button → SceneManager.LoadScene("BattleScene")
- That's it. No state management, no persistence.

SerializeFields:
- Button _newGameButton

using UnityEngine.SceneManagement required.
```

### Build Settings

- TitleScene = index 0
- BattleScene = index 1

---

## 3. F-14: Clear/Defeat UI

### 3a. ClearView.cs (new, ViewBase subclass)

```
Namespace: OUD.Unity.Battle.View
Path: Assets/Scripts/Unity/Battle/View/ClearView.cs

Responsibilities:
- Display "VICTORY" title
- Show run summary (level, gold)
- [재시작] button → event

SerializeFields:
- TMP_Text _titleText        ("VICTORY")
- TMP_Text _levelText        ("Level: 2")
- TMP_Text _goldText         ("Gold: 32")
- Button   _restartButton

Public API:
- event Action OnRestartClicked
- void SetSummary(int level, int gold)

Awake: wire _restartButton.onClick → OnRestartClicked
```

### 3b. DefeatView.cs (new, ViewBase subclass)

```
Namespace: OUD.Unity.Battle.View
Path: Assets/Scripts/Unity/Battle/View/DefeatView.cs

Responsibilities:
- Display "DEFEAT" title
- Show reached node info
- [재시작] button → event

SerializeFields:
- TMP_Text _titleText        ("DEFEAT")
- TMP_Text _reachedNodeText  ("도달 노드: 2/3")
- Button   _restartButton

Public API:
- event Action OnRestartClicked
- void SetDefeatInfo(int reachedNode, int totalNodes)

Awake: wire _restartButton.onClick → OnRestartClicked
```

### 3c. BattleUIAdapter.cs modifications

New fields:
```csharp
[Header("End-of-Run Views (F-14)")]
[SerializeField] private ClearView  _clearView;
[SerializeField] private DefeatView _defeatView;
```

New wiring guard:
```csharp
private bool _endRunViewsWired;
```

Wire in Initialize():
```
if (!_endRunViewsWired)
{
    _clearView.OnRestartClicked  += HandleRestartClicked
    _defeatView.OnRestartClicked += HandleRestartClicked
    _endRunViewsWired = true
}
```

HandleRestartClicked:
```
SceneManager.LoadScene("TitleScene")
```

Modify FinishPostBattle() — Boss branch:
```
Before: _runManager.AdvanceNode(); _onContinueRequested?.Invoke();
After:  _runManager.AdvanceNode(); ShowClearScreen();
```

New ShowClearScreen():
```
PlayerState player = _playerPresenter.Player;
_clearView.SetSummary(player.Level + 1, player.Gold);  // Level is 0-indexed
_clearView.Show();
```

Modify OnBattleLost():
```
Before: _battleLogPresenter.ShowBattleLost();
After:  _battleLogPresenter.ShowBattleLost();
        ShowDefeatScreen();
```

New ShowDefeatScreen():
```
int reached = _runManager?.State?.CurrentNodeIndex ?? 0;
int total = RunState.TOTAL_NODES;
_defeatView.SetDefeatInfo(reached + 1, total);
_defeatView.Show();
```

### 3d. BattleBootstrapper.cs modifications

Add `_rollDiceWired` guard:
```csharp
private bool _rollDiceWired;
```

In InitRun(), change button registration:
```
Before: if (_rollDiceButton != null)
            _rollDiceButton.onClick.AddListener(OnRollDiceClicked);

After:  if (_rollDiceButton != null && !_rollDiceWired)
        {
            _rollDiceButton.onClick.AddListener(OnRollDiceClicked);
            _rollDiceWired = true;
        }
```

OnContinueAfterReward() — clear branch:
```
Before: Debug.Log("런 클리어!"); return;
After:  (remove or keep log) — Adapter now handles ClearView display.
        The _onContinueRequested callback from Adapter no longer fires
        for Boss clear, so this code path becomes unreachable for clear.
        Keep the guard for safety but it's effectively dead code.
```

### 3e. Scene Work — BattleScene

Add two panels to BattleCanvas (same structure as RewardPanel):

**ClearPanel (700x520, center):**
- Background: Image, Color(0,0,0,0.85)
- TitleText: TMP_Text, "VICTORY", 36pt, gold color (#FFD700)
- LevelText: TMP_Text, 20pt
- GoldText: TMP_Text, 20pt
- RestartButton: Button + TMP_Text "재시작"
- ClearView component attached, SerializeFields bound
- Default: inactive

**DefeatPanel (700x520, center):**
- Background: Image, Color(0,0,0,0.85)
- TitleText: TMP_Text, "DEFEAT", 36pt, red color (#FF4444)
- ReachedNodeText: TMP_Text, 20pt
- RestartButton: Button + TMP_Text "재시작"
- DefeatView component attached, SerializeFields bound
- Default: inactive

Font: NotoSansKR-Regular SDF (same as existing panels)

---

## 4. Files Changed

| File | Change |
|------|--------|
| `Scripts/Unity/TitleController.cs` | **New** — '새 게임' → BattleScene |
| `Scripts/Unity/Battle/View/ClearView.cs` | **New** — ViewBase, VICTORY + summary + restart |
| `Scripts/Unity/Battle/View/DefeatView.cs` | **New** — ViewBase, DEFEAT + reached node + restart |
| `Scripts/Unity/Adapter/BattleUIAdapter.cs` | +SerializeFields, +wiring, +ShowClearScreen, +ShowDefeatScreen, modify FinishPostBattle/OnBattleLost |
| `Scripts/Unity/BattleBootstrapper.cs` | +_rollDiceWired guard, simplify OnContinueAfterReward |
| `TitleScene.unity` | **New** — title scene |
| `BattleScene.unity` | +ClearPanel, +DefeatPanel |

---

## 5. What This Does NOT Change

- **IBattleUI interface** — no new methods. Clear/Defeat handled inside Adapter.
- **DebugBattleUI** — no sync needed.
- **BattleEngine (pure C#)** — no changes.
- **IEnemyEntryView** — F-07 rage UI deferred.
- **MonsterSpriteMap** — unchanged.

---

## 6. Verification Checklist

- [ ] TitleScene → [새 게임] → BattleScene transition
- [ ] Normal battle flow unchanged (victory → reward → levelup/rest → nodemap → next)
- [ ] Boss victory → Reward → [계속] → ClearPanel ("VICTORY" + level/gold)
- [ ] ClearPanel [재시작] → TitleScene
- [ ] Defeat → DefeatPanel ("DEFEAT" + reached node)
- [ ] DefeatPanel [재시작] → TitleScene
- [ ] Restart from title → fresh run (HP 60, ATK 6, DEF 5)
- [ ] Roll Dice button works correctly after restart (no duplicate listeners)
