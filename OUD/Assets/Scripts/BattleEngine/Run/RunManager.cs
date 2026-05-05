// RunManager.cs
// 런 진행 흐름 오케스트레이션. RunState + RunMap 보유 + EncounterTable에 적 구성 위임.
// 전투 종료 후 다음 화면 분기(보상/레벨업/휴식)를 PostBattleFlow로 반환한다.
// Phase D-2 (sprint MVP — 노드맵 UI 도입): 1-2-1 + Boss 합류, 분기 선택 API 추가.
//   - 신규: GetCurrentNode / GetAvailableNextNodes / SelectNextNode
//   - 호환: AdvanceNode (다음 세션 UI 작업 전까지 임시 — 첫 번째 가능 노드 자동 선택)
// feature-spec F-11 (Phase D-2 갱신), user-flow §1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 전투 종료 후 표시할 화면 분기:
    ///   일반 노드(layer 0,1): ShowReward=true, ShowLevelUp=조건부, ShowRest=true
    ///   Boss 노드(layer 2)  : ShowReward=true (XP/Gold 표시), ShowRest=false, ShowLevelUp=false
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
    /// 책임: 노드 진행 / 적 구성 위임 / 전투 후 화면 분기 결정 / 분기 노드 선택 / 클리어 판정.
    /// PlayerState 변경(AddXp/AddGold/Heal/LevelUp 등)은 외부 호출자 책임.
    /// </summary>
    public class RunManager
    {
        // ── 레벨업 임계값 (sprint MVP 데모 — Phase D-1과 동일) ────────────────
        // layer 1(분기) 종료 시점에 누적 XP 2 이상이면 레벨업. layer 2(Boss)는 레벨업 분기 없음.
        private static readonly int[] LEVELUP_LAYER_INDICES = { 1 };
        private static readonly int[] XP_THRESHOLDS         = { 2 };
        private const int MAX_LEVEL = 3;

        private readonly EncounterTable _encounterTable;
        private readonly IRandom        _random;
        private RunState _state;
        private RunMap   _map;

        public RunState State => _state;
        public RunMap   Map   => _map;

        public RunManager(EncounterTable encounterTable, IRandom random)
        {
            _encounterTable = encounterTable ?? throw new ArgumentNullException(nameof(encounterTable));
            _random         = random         ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// 새 런 시작. PlayerState를 RunState에 연결, RunMap 신규 생성.
        /// 기존 RunState가 있으면 Reset.
        /// </summary>
        public void StartRun(PlayerState player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (_state == null) _state = new RunState(player);
            else                _state.Reset(player);

            _map = new RunMap(_random);
        }

        /// <summary>
        /// 현재 노드의 적 구성 생성. EncounterTable에 위임.
        /// StartRun() 미호출 상태에서 호출 시 예외.
        /// </summary>
        public List<MonsterData> GetNextBattle()
        {
            EnsureStarted();
            MapNode current = _map.GetNode(_state.CurrentNodeId);
            return _encounterTable.GenerateEncounter(current);
        }

        /// <summary>
        /// 방금 끝난 전투의 후처리 화면 분기.
        /// Boss 노드(마지막 layer): 보상만 표시. 휴식/레벨업 없음.
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

        // ── 노드맵 API (Phase D-2 신규) ──────────────────────────────────────

        /// <summary>현재 진입 중인 노드.</summary>
        public MapNode GetCurrentNode()
        {
            EnsureStarted();
            return _map.GetNode(_state.CurrentNodeId);
        }

        /// <summary>
        /// 현재 노드에서 진입 가능한 다음 노드 목록.
        /// Boss 노드(마지막)는 빈 배열.
        /// </summary>
        public IReadOnlyList<MapNode> GetAvailableNextNodes()
        {
            EnsureStarted();
            MapNode current = _map.GetNode(_state.CurrentNodeId);
            var nextIds = current.NextNodeIds;
            var result  = new MapNode[nextIds.Count];
            for (int i = 0; i < nextIds.Count; i++)
                result[i] = _map.GetNode(nextIds[i]);
            return result;
        }

        /// <summary>
        /// 분기 노드 중 하나를 선택해 진입. nodeId가 현재 노드의 NextNodeIds에 없으면 예외.
        /// 마지막 노드(Boss) 클리어 시점에는 호출 불가 — IsRunComplete 사용.
        /// </summary>
        public void SelectNextNode(int nodeId)
        {
            EnsureStarted();

            if (_state.IsRunComplete)
                throw new InvalidOperationException("런이 이미 완료되었습니다.");

            MapNode current = _map.GetNode(_state.CurrentNodeId);
            bool valid = false;
            for (int i = 0; i < current.NextNodeIds.Count; i++)
            {
                if (current.NextNodeIds[i] == nodeId) { valid = true; break; }
            }
            if (!valid)
                throw new ArgumentException(
                    $"nodeId {nodeId}는 현재 노드(id={current.Id})의 분기 후보가 아닙니다. " +
                    $"가능 후보: [{string.Join(", ", current.NextNodeIds)}]",
                    nameof(nodeId));

            MapNode target = _map.GetNode(nodeId);
            _state.MoveTo(target.Id, target.Layer);
        }

        // ── 호환 API (다음 세션 UI 작업 전까지 임시) ─────────────────────────

        /// <summary>
        /// 다음 노드로 자동 진행. 호환 메서드 — UI에서 분기 선택 도입 전까지 사용.
        ///   - 마지막 노드(Boss): IsRunComplete=true 전환
        ///   - 일반 노드: 가능한 다음 노드 중 첫 번째 자동 선택
        /// </summary>
        public void AdvanceNode()
        {
            EnsureStarted();

            if (_state.IsRunComplete) return;

            MapNode current = _map.GetNode(_state.CurrentNodeId);
            if (current.NextNodeIds.Count == 0)
            {
                // Boss 노드 클리어
                _state.MarkComplete();
                return;
            }

            // 분기 자동 선택: 첫 번째 후보
            int firstNextId = current.NextNodeIds[0];
            MapNode target  = _map.GetNode(firstNextId);
            _state.MoveTo(target.Id, target.Layer);
        }

        /// <summary>현재 런이 마지막 노드까지 완료된 상태인지.</summary>
        public bool IsRunComplete()
        {
            return _state != null && _state.IsRunComplete;
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 레벨업 가능 여부.
        /// 조건: 현재 layer가 레벨업 체크 시점(MVP에선 layer 1만) + 누적 XP가 임계값 도달 + Level이 MAX 미만.
        /// PlayerState.Level은 0=Lv1, 최대 3=Lv4.
        /// </summary>
        private static bool ShouldLevelUp(int layer, int totalXp, int currentLevel)
        {
            if (currentLevel >= MAX_LEVEL) return false;

            for (int i = 0; i < LEVELUP_LAYER_INDICES.Length; i++)
            {
                if (LEVELUP_LAYER_INDICES[i] == layer)
                    return totalXp >= XP_THRESHOLDS[i];
            }
            return false;
        }

        private void EnsureStarted()
        {
            if (_state == null || _map == null)
                throw new InvalidOperationException("StartRun()이 호출되지 않았습니다.");
        }
    }
}
