# Tutorial Stage Design Spec

## Overview

첫 실행 시 Node 0을 튜토리얼 전투로 변환하여, 허수아비 2마리와의 전투를 통해 게임의 핵심 메커니즘을 안내한다. 힌트 + 자유 플레이 방식으로 가이드 텍스트와 글로우 이펙트를 표시하되, 플레이어의 행동을 강제하지 않는다.

## Entry Conditions

- **첫 실행**: Node 0 진입 시 `TutorialState.IsCompleted == false` → 튜토리얼 모드
- **이후 실행**: Node 0은 일반 전투 (EncounterTable 기반)
- **재진입**: 타이틀 화면 "Tutorial" 버튼 → `TutorialState.Reset()` → 새 게임 시작 시 다시 튜토리얼

## Architecture

### Approach A: TutorialManager + TutorialStep

기존 전투 코드를 수정하지 않고 TutorialManager가 관찰자로 동작.

```
BattleBootstrapper
  · Node 0 + 첫 실행 감지 → isTutorial 플래그
  · 허수아비 2마리 생성
  · TutorialManager 초기화
        │
        ▼
TutorialManager (MonoBehaviour)
  · TutorialStep[] _steps (순차 실행)
  · int _currentStepIndex
  · BattleUIAdapter 이벤트 구독 (phase 변경, 버튼 클릭)
  · TutorialOverlayView 제어 (텍스트, 글로우)
  · Advance() → 다음 Step 실행
        │
        ▼
TutorialOverlayView (MonoBehaviour)
  · GuidePanel (상단 중앙, 반투명 검정 배경 + 흰 텍스트)
  · 글로우 on/off 메서드
  · 페이드인/아웃 (CanvasGroup alpha, 0.3초)
```

### TutorialStep Data Structure

```csharp
class TutorialStep
{
    string GuideText;           // 표시할 안내 문구
    TutorialTrigger Trigger;    // 다음으로 넘어가는 조건
    GlowTarget[] GlowTargets;  // 글로우 대상들
    float DelayBefore;          // Step 시작 전 대기 (연출용)
}

enum TutorialTrigger
{
    Auto,              // DelayBefore 후 자동 진행
    PhaseChanged,      // 특정 BattlePhase로 전환 시
    ButtonClicked,     // 특정 버튼 클릭 시
    DiceKept,          // 주사위 Keep 시
    SkillSelected,     // 스킬 카드 선택 시
    TargetSelected,    // 타겟 지정 시
    TurnEnded,         // 턴 종료 시
    EnemyShielded,     // 적이 쉴드 획득 시
    BattleWon,         // 전투 승리 시
}

enum GlowTarget
{
    RollDiceButton,
    RerollButton,
    UseSkillButton,
    ExecuteButton,
    DiceEntries,
    SkillList,      // 기술 목록 컨테이너 (개별 카드가 아닌 목록 전체)
    EnemyCards,
}
```

## Enemy Data: Scarecrow

MonsterDatabase에 추가:

```
ID: "Scarecrow"
Name: "Scarecrow"
Tier: Normal
MaxHp: 10
BaseAtk: 1
ShieldValue: 1
StrongAttackMultiplier: 0.0 (미사용)
HasRage: false
```

패턴 분기:
- 허수아비 A: Pattern = [Attack, Shield]
- 허수아비 B: Pattern = [Shield, Attack]

두 마리 모두 같은 MonsterData ID를 사용하되, BattleBootstrapper에서 생성 시 패턴만 다르게 처리하거나, "Scarecrow_A" / "Scarecrow_B"로 분리 등록.

## Tutorial Step Sequence

| # | 시점 | 가이드 텍스트 | 글로우 대상 | Trigger |
|---|------|-------------|-----------|---------|
| 0 | 전투 시작 | "적의 의도를 확인하세요! 검 아이콘은 공격, 방패 아이콘은 수비입니다" | EnemyCards | 2초 후 자동 |
| 1 | | "적들의 공격을 방어하거나, 먼저 물리치세요!" | — | 2초 후 자동 |
| 2 | 주사위 단계 | "Roll Dice 버튼을 눌러 주사위를 굴리세요" | RollDiceButton | RollDice 클릭 |
| 3 | 주사위 결과 | "리롤 버튼을 눌러 주사위를 다시 굴리세요" | RerollButton | Reroll 클릭 |
| 4 | 리롤 후 | "원하는 주사위를 터치해서 Keep하세요" | DiceEntries | 주사위 Keep 시 |
| 5 | | "같은 숫자가 모이면 스킬이 활성화됩니다!" | — | 1.5초 후 자동 |
| 6 | 스킬 선택 | "활성화된 기술을 선택하세요!" | SkillList | 스킬 카드 클릭 |
| 7 | 리롤/슬롯 유도 | "남은 리롤을 모두 사용하거나 기술 슬롯을 채우세요!" | RerollButton | 리롤 전부 소모 또는 슬롯 3개 채움 |
| 8 | 기술 사용 | "기술 사용 버튼을 눌러 적들을 물리치러 가세요!" | UseSkillButton | UseSkill 클릭 |
| 9 | 타겟 지정 | "공격할 적을 선택하세요!" | EnemyCards | 타겟 선택 |
| 10 | 실행 | "Execute 버튼으로 기술을 발동하세요!" | ExecuteButton | Execute 클릭 |
| 11 | End Turn 후 | "적이 방어를 올렸습니다! 쉴드를 먼저 깎아야 합니다" | — | 2초 후 자동 (조건부: End Turn 클릭 후 적이 쉴드 보유 시) |
| 12 | 2턴째~ | (가이드 없음, 자유 플레이) | — | BattleWon |
| 13 | 승리 | "축하합니다! 튜토리얼을 완료했습니다!" | — | 2초 후 자동 종료 |

### Flow Rules

- **힌트 방식**: 글로우 + 텍스트 표시, 다른 행동 차단하지 않음
- **Step 2 텍스트**: RollDice 클릭 전까지 FadeOut하지 않고 유지 (다른 Step은 정상 FadeOut)
- **Step 7 분기**: 스킬 선택 후 리롤이 남아있으면 리롤 소모 유도, 슬롯이 모두 차있으면 바로 Step 8로
- **Step 11 조건부**: End Turn 버튼 클릭 후 적이 쉴드를 보유하고 있는지 판단. 적의 수비 행동은 현재 턴에 실행되고 쉴드는 다음 턴에 적용되므로, End Turn 이후에 체크해야 정확함. 쉴드 보유 적이 없으면 스킵.
- **2턴째 이후**: 가이드 없이 자유 플레이. 승리 시 Step 13으로 점프
- **패배 시**: ATK=1이라 사실상 불가능하지만, 패배 시 튜토리얼 미완료 유지 → 재시작 시 다시 튜토리얼

## UI Components

### TutorialOverlayView

BattleScene Canvas 최상위에 배치.

```
TutorialOverlay (GameObject)
├── GuidePanel (상단 중앙)
│   └── GuideText (TMP_Text)
└── GlowEffects (글로우 제어)
```

- **GuidePanel**: 반투명 검정 배경 + 흰색 텍스트, 페이드인/아웃 (CanvasGroup alpha, 0.3초)
- **자동 진행 Step**: 텍스트 표시 → 딜레이 후 페이드아웃
- **버튼 대기 Step**: 텍스트 표시, 유저 행동 시 페이드아웃

### Glow Effect

기존 EnemyEntryView의 Outline 펄스 패턴 재사용:
- 골드색 Outline `(0.95, 0.78, 0.18)`, alpha 0.3↔1.0 코사인 펄스 (1초 주기)
- `TutorialOverlayView.SetGlow(GlowTarget, bool)` 메서드로 제어
- **글로우 크기 제약**: effectDistance를 작게 유지 (2~3px), 인접 오브젝트 영역을 침범하지 않도록. 불가피한 경우 최소한으로 허용.

| GlowTarget | 접근 경로 | 방식 |
|------------|----------|------|
| RollDiceButton | DiceView Roll 버튼 | Outline 펄스 |
| RerollButton | DiceView Reroll 버튼 | Outline 펄스 |
| UseSkillButton | SlotAssignmentView UseSkill 버튼 | Outline 펄스 |
| ExecuteButton | TargetSelectionView Execute 버튼 | Outline 펄스 |
| DiceEntries | DiceEntryView 5개 | Border 색상 펄스 |
| SkillList | 기술 목록 컨테이너 | Outline 펄스 (컨테이너 전체, 개별 카드 아님) |
| EnemyCards | EnemyEntryView들 | 기존 _targetOutline 활용 |

## Tutorial State Persistence

```csharp
static class TutorialState
{
    public static bool IsCompleted
        => PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;

    public static void SetCompleted()
        => PlayerPrefs.SetInt("TutorialCompleted", 1);

    public static void Reset()
        => PlayerPrefs.SetInt("TutorialCompleted", 0);
}
```

## Integration Points (Existing Code Modifications)

### 1. MonsterDatabase.cs — 소규모 추가
- Scarecrow MonsterData 등록 (패턴 A/B 분리 또는 단일 ID + 런타임 패턴 교체)

### 2. BattleBootstrapper.cs — 중규모 수정
- `StartNextNode()` 내부: Node 0 + `!TutorialState.IsCompleted` 감지
- 허수아비 2마리 직접 생성 (EncounterTable 우회)
- TutorialManager 생성 및 `Begin()` 호출
- 전투 승리 콜백에서 `TutorialState.SetCompleted()` 호출

### 3. BattleUIAdapter.cs — 소규모 수정
- Phase 전환 시 이벤트 발행: `event Action<BattlePhase> OnPhaseChanged`
- 버튼 클릭 시 이벤트 발행: `event Action<string> OnButtonAction`
- View getter 추가: `GetDiceView()`, `GetSlotAssignmentView()`, `GetTargetSelectionView()`, `GetEnemyPresenter()`

### 4. TitleController.cs — 소규모 수정
- "Tutorial" 버튼 추가
- 클릭 시 `TutorialState.Reset()` → 새 게임 시작

### 5. 각 View — 소규모 수정
- 글로우 대상 버튼/컴포넌트에 대한 public 접근자 추가
- 기존 Outline 펄스 패턴 재사용

## New Files

| 파일 | 위치 | 역할 |
|------|------|------|
| TutorialState.cs | Scripts/Unity/Tutorial/ | PlayerPrefs 래퍼 |
| TutorialStep.cs | Scripts/Unity/Tutorial/ | Step 데이터 구조 + enum |
| TutorialManager.cs | Scripts/Unity/Tutorial/ | 핵심 로직 (Step 순차 실행) |
| TutorialOverlayView.cs | Scripts/Unity/Tutorial/ | UI 제어 (텍스트, 글로우) |

## Modified Files Summary

| 파일 | 변경 | 규모 |
|------|------|------|
| MonsterDatabase.cs | 허수아비 MonsterData 추가 | 소 |
| BattleBootstrapper.cs | 튜토리얼 분기 + TutorialManager 초기화 | 중 |
| BattleUIAdapter.cs | 이벤트 발행 + View getter | 소 |
| TitleController.cs | Tutorial 버튼 추가 | 소 |
| 각 View (Dice/Slot/Target) | 글로우 접근자 추가 | 소 |
