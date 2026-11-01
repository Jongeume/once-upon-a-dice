using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public readonly struct PostBattleFlow
    {
        public bool ShowReward   { get; }
        public bool ShowLevelUp  { get; }

        public PostBattleFlow(bool showReward, bool showLevelUp)
        {
            ShowReward  = showReward;
            ShowLevelUp = showLevelUp;
        }
    }

    public class RunManager
    {
        private readonly EncounterTable _encounterTable;
        private readonly IRandom        _random;
        private readonly LevelUpSystem  _levelUpSystem;
        private RunState _state;
        private RunMap   _map;

        public RunState State => _state;
        public RunMap   Map   => _map;

        public RunManager(EncounterTable encounterTable, IRandom random)
            : this(encounterTable, random, new LevelUpSystem()) { }

        public RunManager(EncounterTable encounterTable, IRandom random, LevelUpSystem levelUpSystem)
        {
            _encounterTable = encounterTable ?? throw new ArgumentNullException(nameof(encounterTable));
            _random         = random         ?? throw new ArgumentNullException(nameof(random));
            _levelUpSystem  = levelUpSystem  ?? throw new ArgumentNullException(nameof(levelUpSystem));
        }

        public void StartRun(PlayerState player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (_state == null) _state = new RunState(player);
            else                _state.Reset(player);

            _map = new RunMap(_random);
        }

        public List<MonsterData> GetNextBattle()
        {
            EnsureStarted();
            MapNode current = _map.GetNode(_state.CurrentNodeId);
            return _encounterTable.GenerateEncounter(current);
        }

        public PostBattleFlow GetPostBattleFlow()
        {
            EnsureStarted();

            MapNode current = _map.GetNode(_state.CurrentNodeId);

            if (current.Type == NodeType.Boss)
                return new PostBattleFlow(showReward: true, showLevelUp: false);

            bool levelUp = _levelUpSystem.CanLevelUp(_state.Player.Xp, _state.Player.Level);
            return new PostBattleFlow(showReward: true, showLevelUp: levelUp);
        }

        public MapNode GetCurrentNode()
        {
            EnsureStarted();
            return _map.GetNode(_state.CurrentNodeId);
        }

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

        public void AdvanceNode()
        {
            EnsureStarted();

            if (_state.IsRunComplete) return;

            MapNode current = _map.GetNode(_state.CurrentNodeId);
            if (current.NextNodeIds.Count == 0)
            {
                _state.MarkComplete();
                return;
            }

            int firstNextId = current.NextNodeIds[0];
            MapNode target  = _map.GetNode(firstNextId);
            _state.MoveTo(target.Id, target.Layer);
        }

        public bool IsRunComplete()
        {
            return _state != null && _state.IsRunComplete;
        }

        private void EnsureStarted()
        {
            if (_state == null || _map == null)
                throw new InvalidOperationException("StartRun()이 호출되지 않았습니다.");
        }
    }
}
