// RunManager.cs
// 런 진행 흐름 오케스트레이션. RunState 보유 + EncounterTable에 적 구성 위임.
// 전투 종료 후 다음 화면 분기(보상/레벨업/휴식)를 PostBattleFlow로 반환한다.
// Phase D-1 (sprint MVP 데모): 3전투 자동 진행, 보스 제거.
// feature-spec F-11 (sprint MVP 갱신), user-flow §1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 전투 종료 후 표시할 화면 분기:
    ///   일반 노드(0,1): ShowReward=true, ShowLevelUp=조건부, ShowRest=true
    ///   마지막 노드(2): ShowReward=true (XP/Gold 표시), ShowRest=false, ShowLevelUp=false
    /// 호출 측은 true인 화면을 Reward → LevelUp → Rest 순서로 표시.
    /// </summary>
    public readonly struct PostBattleFlow
    {
        public bool ShowReward   { get; }
        public bool ShowLevelUp  { get; }
        public bool ShowRest     { get; }

        public PostBattleFlow(bool showReward, bool showLevelUp, bool showRest)
        {
            ShowReward  = showReward;
            ShowLevelUp = showLevelUp;
            ShowRest    = showRest;
        }
    }

    /// <summary>
    /// 런 1회의 진행 오케스트레이터.
    /// 책임: 노드 진행 / 적 구성 위임 / 전투 후 화면 분기 결정 / 클리어 판정.
    /// PlayerState 변경(AddXp/AddGold/Heal/LevelUp 등)은 외부 호출자 책임.
    /// </summary>
    public class RunManager
    {
        // ── 레벨업 임계값 (sprint MVP 데모: 3전투 압축) ─────────────────────────
        // 노드 1 종료 시점에 누적 XP 2 이상이면 레벨업. 노드 2(마지막)는 레벨업 분기 없음.
        private static readonly int[] LEVELUP_NODE_INDICES = { 1 };
        private static readonly int[] XP_THRESHOLDS        = { 2 };
        private const int MAX_LEVEL = 3;

        private readonly EncounterTable _encounterTable;
        private RunState _state;

        public RunState State => _state;

        public RunManager(EncounterTable encounterTable)
        {
            _encounterTable = encounterTable ?? throw new ArgumentNullException(nameof(encounterTable));
        }

        /// <summary>
        /// 새 런 시작. PlayerState를 RunState에 연결. 기존 RunState가 있으면 Reset.
        /// </summary>
        public void StartRun(PlayerState player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (_state == null) _state = new RunState(player);
            else                _state.Reset(player);
        }

        /// <summary>
        /// 현재 노드의 적 구성 생성. EncounterTable에 위임.
        /// StartRun() 미호출 상태에서 호출 시 예외.
        /// </summary>
        public List<MonsterData> GetNextBattle()
        {
            EnsureStarted();
            return _encounterTable.GenerateEncounter(_state.CurrentNodeIndex);
        }

        /// <summary>
        /// 방금 끝난 전투의 후처리 화면 분기.
        /// 마지막 노드: 보상만 표시 (XP/Gold). 휴식/레벨업 없음.
        /// 일반 노드: 보상 + 휴식 + (레벨업 조건부).
        /// </summary>
        public PostBattleFlow GetPostBattleFlow()
        {
            EnsureStarted();

            if (_state.IsLastNode)
                return new PostBattleFlow(showReward: true, showLevelUp: false, showRest: false);

            bool levelUp = ShouldLevelUp(_state.CurrentNodeIndex, _state.Player.Xp, _state.Player.Level);
            return new PostBattleFlow(showReward: true, showLevelUp: levelUp, showRest: true);
        }

        /// <summary>
        /// 다음 노드로 진행. 마지막 노드 종료 시 IsRunComplete=true 전환.
        /// </summary>
        public void AdvanceNode()
        {
            EnsureStarted();
            _state.Advance();
        }

        /// <summary>현재 런이 마지막 노드까지 완료된 상태인지.</summary>
        public bool IsRunComplete()
        {
            return _state != null && _state.IsRunComplete;
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 레벨업 가능 여부.
        /// 조건: 노드가 레벨업 체크 시점(MVP에선 노드 1만) + 누적 XP가 임계값 도달 + Level이 MAX 미만.
        /// PlayerState.Level은 0=Lv1, 최대 3=Lv4.
        /// </summary>
        private static bool ShouldLevelUp(int nodeIndex, int totalXp, int currentLevel)
        {
            if (currentLevel >= MAX_LEVEL) return false;

            for (int i = 0; i < LEVELUP_NODE_INDICES.Length; i++)
            {
                if (LEVELUP_NODE_INDICES[i] == nodeIndex)
                    return totalXp >= XP_THRESHOLDS[i];
            }
            return false;
        }

        private void EnsureStarted()
        {
            if (_state == null)
                throw new InvalidOperationException("StartRun()이 호출되지 않았습니다.");
        }
    }
}
