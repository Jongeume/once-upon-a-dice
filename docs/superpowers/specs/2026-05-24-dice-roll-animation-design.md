# Dice Roll Animation Design — Phase 1

## 개요

DiceArea의 주사위 5개에 스프라이트 프레임 교체 기반 롤링 애니메이션을 추가한다.
기존 TMP_Text 숫자 표시를 제거하고, 스프라이트 이미지로 대체한다.

- **Phase 1** (이번 작업): 제자리 스프라이트 프레임 순환 롤링 + 결과 스프라이트 정지
- **Phase 2** (후속): 물리 바운스 위치 이동 추가

## 에셋

| 에셋 | 소스 경로 | 프로젝트 경로 | 용도 |
|------|----------|--------------|------|
| `DiceSpread.png` | `D:\Unity\OUD_Assets\게임 에셋\3. Dice\DiceSpread.png` | `OUD/Assets/Art/Dice/DiceSpread.png` | 롤링 프레임 (~98프레임, Sprite Editor 슬라이스) |
| `1.png` ~ `6.png` | `D:\Unity\OUD_Assets\게임 에셋\3. Dice\1~6.png` | `OUD/Assets/Art/Dice/Results/1.png~6.png` | 결과 정지 이미지 |

### 스프라이트 슬라이싱
- `DiceSpread.png`: 1672×941px, ~14열×7행
- Sprite Editor에서 Grid by Cell Size로 슬라이스 (셀 크기: ~119×134px, 실측 후 조정)
- Sprite Mode: Multiple, Pixels Per Unit: 기존 프로젝트 설정에 맞춤

## 아키텍처

### 데이터 흐름 (변경 없음)
```
BattleEngine (값 결정) → IBattleUI → BattleUIAdapter → DicePresenter → DiceEntryView
```

BattleEngine은 순수 C#로 유지. View 레이어에서만 애니메이션 연출 추가.

### 컴포넌트 변경

#### IDiceEntryView (BattleViewInterfaces.cs)
```csharp
public interface IDiceEntryView
{
    void SetResultImmediate(int value);   // 즉시 결과 스프라이트 표시 (Keep된 주사위, 초기화 등)
    void PlayRoll(int resultValue, float stopDelay, Action onComplete);  // 롤링 애니메이션
    void SetKept(bool kept);             // Keep 하이라이트 (기존 유지)
}
```

- `UpdateValue(int)` → `SetResultImmediate(int)`로 이름 변경. 내부에서 결과 스프라이트 표시.

#### DiceEntryView.cs

**제거하는 필드:**
- `_valueText` (TMP_Text)

**추가하는 필드:**
| 필드 | 타입 | 역할 |
|------|------|------|
| `_diceImage` | `Image` | 주사위 스프라이트 표시 (자식 DiceImage GO 참조) |
| `_rollingFrames` | `Sprite[]` | DiceSpread.png 슬라이스 결과 |
| `_resultSprites` | `Sprite[6]` | 1.png~6.png 결과 정지 이미지 (인덱스 0=1면, ..., 5=6면) |
| `_frameRate` | `float` | 롤링 프레임 교체 속도 (기본 12fps) |

**메서드:**
```
PlayRoll(int resultValue, float stopDelay, Action onComplete):
  1. Coroutine 시작
  2. _rollingFrames에서 랜덤 인덱스로 프레임 순환 (12fps)
  3. stopDelay 경과 후 감속 (12fps → 8fps → 4fps, ~0.3초)
  4. _resultSprites[resultValue - 1] 표시
  5. onComplete() 호출

SetResultImmediate(int value):
  - 진행 중 Coroutine 중단
  - _resultSprites[value - 1] 즉시 표시

SetKept(bool kept):
  - 기존 Outline 하이라이트 로직 유지
```

**Keep 하이라이트:** 기존 `_background` Image의 Outline 컴포넌트 방식 유지.

#### DicePresenter.cs

**추가 상수:**
| 상수 | 값 | 의미 |
|------|-----|------|
| `BASE_ROLL_DURATION` | 0.8f | 첫 번째 주사위 최소 롤링 시간 |
| `STOP_STAGGER` | 0.2f | 주사위 간 정지 시간차 |

**UpdateDice 변경:**
```
UpdateDice(int[] values, int rerollsLeft):
  _values = values
  _rerollsLeft = rerollsLeft

  rollingIndices = Keep되지 않은 주사위 인덱스 목록
  _pendingCount = rollingIndices.Count

  if (_pendingCount == 0):
    즉시 완료 처리
    return

  롤링 중 버튼 비활성화

  for i in rollingIndices:
    delay = BASE_ROLL_DURATION + (순서 * STOP_STAGGER)
    _entryViews[i].PlayRoll(values[i], delay, OnDieSettled)

  Keep된 주사위:
    _entryViews[i].SetResultImmediate(values[i])
```

**OnDieSettled 콜백:**
```
OnDieSettled():
  _pendingCount--
  if (_pendingCount == 0):
    리롤 버튼 / 확정 버튼 활성화
    _diceView.UpdateRerollInfo(...)
```

**롤링 중 인터랙션 잠금:**
- 롤링 중 리롤 버튼, Keep 토글 비활성화
- 전체 정지 후 복원

## 씬 구조 변경

### 현재
```
DiceArea/ (HorizontalLayoutGroup)
  Dice1 (Image + Button + DiceEntryView)
    └── Value (TextMeshProUGUI)
  Dice2~5 (동일)
```

### 변경 후
```
DiceArea/ (HorizontalLayoutGroup)
  Dice1 (Image[배경/슬롯틀] + Button + DiceEntryView)
    └── DiceImage (Image) ← 스프라이트 애니메이션 대상
  Dice2~5 (동일)
```

- `Value` (TMP_Text) 제거 → `DiceImage` (Image) 추가
- Dice1 자체의 Image는 배경 슬롯 틀 역할 유지 가능

### 기존 구조 유지
- `ActionButtons/RerollButton` — 위치, 이벤트 연결 그대로
- `ActionButtons/UseSkillButton` — 그대로
- `ActionButtons/BackButton` — 그대로

## 애니메이션 상세

### 롤링 동작
| 구간 | 시간 | fps | 설명 |
|------|------|-----|------|
| 고속 순환 | 0 ~ stopDelay | 12 | _rollingFrames 랜덤 인덱스 점프 |
| 감속 | stopDelay ~ +0.3초 | 12→8→4 | 프레임 간격 점진 증가 |
| 정지 | 감속 종료 | - | _resultSprites[value-1] 스냅 |

### 타이밍 (5개 주사위 모두 굴릴 때)
| 주사위 | stopDelay | 총 시간 (감속 포함) |
|--------|-----------|-------------------|
| 1번째 | 0.8초 | ~1.1초 |
| 2번째 | 1.0초 | ~1.3초 |
| 3번째 | 1.2초 | ~1.5초 |
| 4번째 | 1.4초 | ~1.7초 |
| 5번째 | 1.6초 | ~1.9초 |

### Keep된 주사위
- 리롤 시 애니메이션 없음 — `SetResultImmediate`로 기존 값 유지
- Keep 하이라이트(Outline) 활성 상태 유지

### 첫 롤 vs 리롤
- Phase 1에서는 동일한 연출. 추후 차별화 가능.

## 변경하지 않는 것
- **BattleEngine 전체** — 순수 C# 로직, `using UnityEngine` 금지 규칙 준수
- **DiceView.cs** — 리롤/확정 버튼 가시성 제어 로직
- **DicePresenter의 Keep 토글 로직** — `OnDieToggleKeep`, `ResetKeep` 그대로
- **ActionButtons 씬 구조** — RerollButton, UseSkillButton, BackButton 위치

## Phase 2 확장 포인트
- `PlayRoll` Coroutine 내부에 Transform.localPosition 이동 추가 → 물리 바운스
- `DiceEntryView`에 Rigidbody2D 부착 가능
- 충돌 효과음 (`RandomAudioClipPlayer` 패턴 참고)

## 알려진 이슈
- Dice1에 `DiceEntryView` 컴포넌트 2개 중복 부착 — 이번 작업에서 정리
