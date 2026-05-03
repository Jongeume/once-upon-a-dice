// RunManager.cs
// 런 진행 흐름 오케스트레이션. RunState 보유 + EncounterTable에 적 구성 위임.
// 전투 종료 후 다음 화면 분기(보상/레벨업/휴식)를 PostBattleFlow로 반환한다.
// feature-spec F-11, user-flow §1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 전투 종료 후 표시할 화면 분기 (user-flow §1):
    ///   일반 전투: ShowReward=true, ShowLevelUp=조건부, ShowRest=true
    ///   보스 전투: 모두 false (승리=클리어, 패배=재시작)
    /// 호출 측은 true인 화면만 순서대로 표시: Reward → LevelUp → Rest.
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
        // ── 레벨업 임계값 (game-design-v2.2 §4.1) ──────────────────────────────
        // 노드 인덱스 1, 3, 5 (=전투 2, 4, 6) 종료 시점에 누적 XP 2/4/6 이상이면 레벨업.
        private static readonly int[] LEVELUP_NODE_INDICES = { 1, 3, 5 };
        private static readonly int[] XP_THRESHOLDS        = { 2, 4, 6 };
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
        /// 보스 노드: 모두 false (런 종료 책임은 호출자).
        /// 일반 노드: ShowReward=true, ShowRest=true, ShowLevelUp=레벨업 노드 + 임계값 도달 + 미만렙.
        /// </summary>
        public PostBattleFlow GetPostBattleFlow()
        {
            EnsureStarted();

            if (_state.IsBossNode)
                return new PostBattleFlow(showReward: false, showLevelUp: false, showRest: false);

            bool levelUp = ShouldLevelUp(_state.CurrentNodeIndex, _state.Player.Xp, _state.Player.Level);
            return new PostBattleFlow(showReward: true, showLevelUp: levelUp, showRest: true);
        }

        /// <summary>
        /// 다음 노드로 진행. 보스 노드 종료 시 IsRunComplete=true 전환.
        /// </summary>
        public void AdvanceNode()
        {
            EnsureStarted();
            _state.Advance();
        }

        /// <summary>현재 런이 보스 처치까지 완료된 상태인지.</summary>
        public bool IsRunComplete()
        {
            return _state != null && _state.IsRunComplete;
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 레벨업 가능 여부.
        /// 조건: 노드가 레벨업 체크 시점(1/3/5) + 누적 XP가 임계값(2/4/6) 도달 + Level이 MAX 미만.
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
