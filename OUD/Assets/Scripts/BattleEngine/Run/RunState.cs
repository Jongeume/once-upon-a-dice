using System.Collections.Generic;
using OUD.BattleEngine.Unit;

namespace OUD.BattleEngine.Run
{
    public class RunState
    {
        public const int TOTAL_NODES = 13;
        public const int LAST_NODE   = 12;

        public PlayerState Player           { get; private set; }
        public int         CurrentNodeIndex { get; private set; }
        public int         CurrentNodeId    { get; private set; }
        public bool        IsRunComplete    { get; private set; }

        public bool IsLastNode => CurrentNodeId == LAST_NODE;

        private readonly HashSet<int> _visitedNodeIds = new HashSet<int>();
        public IReadOnlyCollection<int> VisitedNodeIds => _visitedNodeIds;

        public RunState(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            CurrentNodeId    = RunMap.START_NODE_ID;
            IsRunComplete    = false;
            _visitedNodeIds.Add(RunMap.START_NODE_ID);
        }

        public void MoveTo(int nodeId, int layer)
        {
            if (IsRunComplete) return;
            CurrentNodeId    = nodeId;
            CurrentNodeIndex = layer;
            _visitedNodeIds.Add(nodeId);
        }

        public void MarkComplete()
        {
            IsRunComplete = true;
        }

        public void Reset(PlayerState player)
        {
            Player           = player;
            CurrentNodeIndex = 0;
            CurrentNodeId    = RunMap.START_NODE_ID;
            IsRunComplete    = false;
            _visitedNodeIds.Clear();
            _visitedNodeIds.Add(RunMap.START_NODE_ID);
        }
    }
}
