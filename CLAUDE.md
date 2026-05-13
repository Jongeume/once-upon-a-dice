# CLAUDE.md — Once Upon a Dice
<!-- 매 세션 시작 시 필수 읽기. 프로세스/스프린트/태스크는 oud-docs/tasks.md 참조. -->

## Project
- 야추 족보 → 전투 행동 결정 로그라이크 | Unity 6 + C# | PC
- 팀: @jongeume, @how-to-song (둘 다 Claude Code) | 캡스톤 2026.03~06
- 목표: 6월 3분 시연 → "족보 선택 재미" 증명

## Repo
| 저장소 | 경로 | 용도 |
|--------|------|------|
| Unity | `D:\Unity\Unity_projects\once-upon-a-dice\OUD\` | 게임 코드 |
| Docs | `oud-docs/` | tasks.md + 설계 문서 (별도 git repo) |

## Design Docs (`oud-docs/` 기준)
| 문서 | 역할 | 참조 시점 |
|------|------|----------|
| game-design-v2.2.md | 기획 정본 (SSOT) | 설계 질문 시 |
| feature-spec-sprint-mvp.md | F-01~F-14 기능 명세 | 코드 작성 전 필수 |
| project-structure.md | 폴더 구조 + 클래스 시그니처 | 코드 작성 전 필수 |
| user-flow-sprint-mvp.md | 유저 플로우 | 흐름 확인 시 |

## Code Rules

### Hard Rules (위반 시 즉시 중단)
- BattleEngine/ 내 `using UnityEngine` 금지
- `System.Random` 직접 사용 금지 → `IRandom` 경유
- `Math.Round` 금지 → `Math.Floor`만 허용
- 매직 넘버 금지 → 상수/데이터 정의 필수
- [미정] 항목 임의 확정 금지
- 명세-코드 충돌 → 코드 패치 금지, 명세 원점 수정

### Naming
| 대상 | 규칙 |
|------|------|
| Namespace | `OUD.BattleEngine.{Subfolder}` |
| Public | PascalCase |
| Private | _camelCase |
| Constants | UPPER_SNAKE_CASE 또는 PascalCase const |
| 식별자 | English / 주석 Korean 허용 |
| 파일 | PascalCase.cs / 문서 kebab-case.md |

### Architecture
- BattleEngine = 순수 C#, Unity 의존성 0
- 데이터 흐름: View → Presenter → TurnManager → IBattleUI → Presenter → View
- 모든 데미지/실드 계산: floor(내림) | 실드: 합산, 적 턴 종료 후 초기화

### Key Game Rules
- 족보 하위 호환: Yahtzee⊃FoaK⊃Triple⊃TwoPair⊃OnePair, FoaK⊃TwoPair, FullHouse⊃Triple+TwoPair+OnePair, LargeStraight⊃SmallStraight
- 동일 족보 턴당 1회 제한 (공+수 합산)
- 사망 대상: 허공 소멸, 자동 리타겟 없음 | 전체 대상 스킬: 사망한 적 건너뜀
- HP 회복 최대 HP 초과 불가 | 빈 슬롯 허용

## Workflows

### `OUD 작업 시작`
1. `oud-docs` git pull → `git diff HEAD~1 -- tasks.md` 로 변경분 확인 → 변경된 섹션만 Read (기본: `진행 중` + `블로커` + `대기`만, `완료` 섹션은 불필요하면 읽지 않음)
2. 진행 중 / 블로커 브리핑
3. 대기 항목 선택 → 진행 중으로 이동 (담당자 표기)
4. 관련 명세 파일 읽기 → 작업 계획 요약 + 확인 요청
5. 확인 후 착수

### `OUD 작업 완료`
1. tasks.md: 해당 항목 완료 섹션 최상단으로 이동 (날짜 포함)
2. `oud-docs` commit: `docs: 작업 현황 갱신`
3. Unity repo: 변경 코드/에셋 commit (내용 기반 메시지)
4. push 대기 → 사용자가 "푸시" 명시 시 실행

### `OUD 세션 중단`
1. tasks.md 진행 중 항목 메모 갱신: 완료 / 남음 / 참고 / 마지막 커밋
2. oud-docs + Unity 양쪽 commit → push 대기

### 토큰 90% 도달 시 (자동)
1. 현재 작업 즉시 중단
2. tasks.md 진행 중 항목의 변경된 필드만 Edit (완료 / 남음 / 참고 / 마지막 커밋) — 파일 전체 재작성 금지
3. `oud-docs` commit + push
4. 사용자에게 "토큰 한계 도달, 인수인계 완료" 보고

## Git Rules
- **push 금지** — "푸시" 명시 시만 실행 (토큰 80% 룰 예외)
- 브랜치 운영
  - **oud-docs**: `main` 단일 운영 (직접 push)
  - **Unity repo**: `dev`가 통합 브랜치, **모든 변경은 feature branch + PR로 머지** (dev 직접 push 금지)
- `oud-docs`와 Unity repo 커밋/푸시 반드시 분리
- 커밋 prefix: `feat` / `fix` / `refactor` / `docs` / `chore` / `style`
- **Unity `.meta` 동반 커밋 필수** — Unity 에셋 파일을 `git add` 할 때 반드시 동일 경로의 `.meta` 파일도 함께 add
  - 대상 확장자: `.cs`, `.prefab`, `.asset`, `.unity`, `.mat`, `.controller`, `.anim`, `.png`, `.jpg`, `.ttf`, `.shader`, `.inputactions` 등 `OUD/Assets/` 하위 모든 파일
  - 예: `git add OUD/Assets/Scripts/.../Foo.cs OUD/Assets/Scripts/.../Foo.cs.meta`
  - 새 파일 add 전 `git status`에서 untracked `.meta` 동반 여부 확인 후 staging
  - `.meta` 누락 시 다른 팀원이 pull 후 GUID 불일치 → Missing (Mono Script) 에러로 씬/프리팹 손상 위험 (실제 발생 사례: ShopView, DefeatView, LevelUpStatView, LevelUpSkillView)

### Unity repo PR 워크플로 (2026-05-13~ 적용)
1. **작업 시작**: `git checkout dev && git pull origin dev && git checkout -b <prefix>/<주제>`
2. **브랜치 명명**: `feat/<기능명>`, `fix/<이슈명>`, `refactor/<영역>`, `docs/<주제>`, `chore/<주제>`
3. **작업 + 커밋**: 평소처럼 commit, `.meta` 동반 규칙 준수
4. **푸시**: `git push -u origin <branch>` (사용자 "푸시" 명시 또는 토큰 80% 룰)
5. **PR 생성**: `gh pr create --base dev --head <branch> --title "<prefix>: <요약>" --body "<상세>"`
6. **사용자 승인 후 머지**: `gh pr merge <PR번호> --merge --delete-branch` 또는 GitHub 웹에서
7. **로컬 정리**: `git checkout dev && git pull origin dev && git branch -d <branch>`

- PR 제목/본문은 commit 메시지와 동일 톤 (한국어 + prefix 사용)
- 머지 후 로컬 feature branch 삭제
- 토큰 80% 자동 정리 시점에도 PR 생성 → 사용자 승인 대기 (직접 머지 금지)

## Session Protocol
1. 이 파일 읽기 → `oud-docs/tasks.md` 변경분만 확인 (`git diff HEAD~1 -- tasks.md`), 변경 섹션만 Read → 필요 시 `oud-docs/sprint.yaml`, `oud-docs/workflow.yaml` 읽기
2. 코드 작성 전: feature-spec 해당 Feature 확인
3. 코드 작성 후: project-structure 시그니처 일치 검증
4. 수치 변경 시: 근거 분류 (data-driven / experience-based / assumption)
5. 에러 발생 시: 코드 패치 금지 → 명세 원점 수정 → 재구현

## Claude Role
- 시니어 개발자 멘토 + BattleEngine 전체 코드 구현자
- 문제점 먼저 지적 → 개선안 제시 | 수치 근거 되묻기
- Unity Layer: 시그니처 + 가이드만, 구현은 유저
- [미정] 항목 단독 확정 금지 | Stable 항목 변경: Phase C LOOP 필수
