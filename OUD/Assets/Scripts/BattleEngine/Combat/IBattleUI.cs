// IBattleUI.cs
// BattleEngine → Unity 단방향 인터페이스.
// BattleEngine은 이 인터페이스만 알고, Unity 구현체는 모른다.
// 데이터 흐름: TurnManager → IBattleUI → (Unity) BattleUIAdapter → Presenter → View
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Combat
{
    /// <summary>
    /// Unity 레이어가 구현하는 전투 UI 인터페이스.
    /// 메서드 호출 순서는 TurnManager의 페이즈 순환과 일치한다.
    /// </summary>
    public interface IBattleUI
    {
        // ── 전투 시작 ─────────────────────────────────────────────────────────

        /// <summary>전투 진입 시 초기 상태 전달. 씬 셋업에 사용.</summary>
        void OnBattleStart(PlayerState player, List<MonsterInstance> enemies);

        // ── 플레이어 턴 시작 ──────────────────────────────────────────────────

        /// <summary>새 플레이어 턴 시작 시 1회 호출. UI 슬롯 초기화 트리거.</summary>
        void OnPlayerTurnStarted();

        // ── 주사위 페이즈 ─────────────────────────────────────────────────────

        /// <summary>주사위 굴림/리롤 결과 전달. 리롤 잔여 횟수 포함.</summary>
        void OnDiceRolled(int[] values, int rerollsLeft);

        // ── 족보 판정 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 달성된 족보 목록 + 현재 사용 가능한 스킬 목록 전달.
        /// 잡패(빈 목록) 시에도 호출된다 — UI는 "잡패" 메시지를 표시할 것.
        /// </summary>
        void OnHandsEvaluated(List<HandType> hands, List<SkillData> usableSkills);

        // ── 슬롯 배분 (플레이어 입력) ────────────────────────────────────────

        /// <summary>
        /// 슬롯 배분 UI를 활성화하고 플레이어 입력을 기다린다.
        /// 플레이어가 [실행]을 누르면 onComplete 콜백으로 결과를 돌려준다.
        /// 빈 슬롯 허용: slots 리스트 크기는 0~3.
        /// </summary>
        /// <param name="usableSkills">배분 가능한 스킬 목록</param>
        /// <param name="aliveEnemies">살아있는 적 목록 (대상 지정용)</param>
        /// <param name="onComplete">슬롯 배분 완료 콜백</param>
        void RequestSlotAssignment(
            List<SkillData>          usableSkills,
            List<MonsterInstance>    aliveEnemies,
            Action<List<SlotAssignment>> onComplete);

        // ── 슬롯 실행 ─────────────────────────────────────────────────────────

        /// <summary>슬롯 1개 실행 결과 전달. slotIndex는 0~2.</summary>
        void OnSlotExecuted(int slotIndex, SkillResult result);

        // ── 적 턴 ─────────────────────────────────────────────────────────────

        /// <summary>적 1체의 행동 결과 전달.</summary>
        /// <param name="enemyIndex">allEnemies 기준 인덱스</param>
        /// <param name="intent">실행된 행동 종류</param>
        /// <param name="value">데미지 또는 실드 수치</param>
        void OnEnemyAction(int enemyIndex, IntentType intent, int value);

        // ── 실드 초기화 ───────────────────────────────────────────────────────

        /// <summary>
        /// 실드 초기화 시점 알림. UI에서 실드 수치를 0으로 리셋.
        /// 플레이어 실드: 적 턴 종료 후 / 적 실드: 플레이어 턴 종료 후.
        /// </summary>
        void OnShieldsReset();

        // ── 전투 종료 ─────────────────────────────────────────────────────────

        /// <summary>적 전멸 → 승리 화면 전환.</summary>
        void OnBattleWon();

        /// <summary>플레이어 HP 0 → 패배 화면 전환.</summary>
        void OnBattleLost();
    }
}
