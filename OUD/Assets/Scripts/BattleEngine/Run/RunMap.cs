using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Run
{
    public enum NodeType
    {
        Combat,
        Boss,
        Shop,
        Elite,
    }

    public readonly struct MapNode
    {
        public int      Id          { get; }
        public NodeType Type        { get; }
        public int      Layer       { get; }
        public int      Column      { get; }

        public IReadOnlyList<int> NextNodeIds { get; }

        public MapNode(int id, NodeType type, int layer, int column, IReadOnlyList<int> nextNodeIds)
        {
            Id          = id;
            Type        = type;
            Layer       = layer;
            Column      = column;
            NextNodeIds = nextNodeIds ?? Array.Empty<int>();
        }
    }

    public class RunMap
    {
        public const int TOTAL_LAYERS  = 8;
        public const int LAST_LAYER    = 7;
        public const int START_NODE_ID = 0;
        public const int LAST_NODE_ID  = 7;

        private readonly Dictionary<int, MapNode> _nodes;

        public IReadOnlyCollection<MapNode> AllNodes => _nodes.Values;

        public int NodeCount => _nodes.Count;

        public RunMap(IRandom random)
        {
            _ = random;

            _nodes = new Dictionary<int, MapNode>
            {
                [0] = new MapNode(id: 0, type: NodeType.Combat, layer: 0, column: 0,
                    nextNodeIds: new[] { 1 }),
                [1] = new MapNode(id: 1, type: NodeType.Combat, layer: 1, column: 0,
                    nextNodeIds: new[] { 2 }),
                [2] = new MapNode(id: 2, type: NodeType.Shop,   layer: 2, column: 0,
                    nextNodeIds: new[] { 3 }),
                [3] = new MapNode(id: 3, type: NodeType.Combat, layer: 3, column: 0,
                    nextNodeIds: new[] { 4 }),
                [4] = new MapNode(id: 4, type: NodeType.Combat, layer: 4, column: 0,
                    nextNodeIds: new[] { 5 }),
                [5] = new MapNode(id: 5, type: NodeType.Elite,  layer: 5, column: 0,
                    nextNodeIds: new[] { 6 }),
                [6] = new MapNode(id: 6, type: NodeType.Shop,   layer: 6, column: 0,
                    nextNodeIds: new[] { 7 }),
                [7] = new MapNode(id: 7, type: NodeType.Boss,   layer: LAST_LAYER, column: 0,
                    nextNodeIds: Array.Empty<int>()),
            };
        }

        public MapNode GetNode(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out MapNode node))
                throw new ArgumentException($"존재하지 않는 nodeId: {nodeId}", nameof(nodeId));
            return node;
        }

        public bool ContainsNode(int nodeId) => _nodes.ContainsKey(nodeId);
    }
}
