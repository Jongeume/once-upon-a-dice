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
        public const int TOTAL_LAYERS  = 9;
        public const int LAST_LAYER    = 8;
        public const int START_NODE_ID = 0;
        public const int LAST_NODE_ID  = 12;

        private readonly Dictionary<int, MapNode> _nodes;

        public IReadOnlyCollection<MapNode> AllNodes => _nodes.Values;

        public int NodeCount => _nodes.Count;

        public RunMap(IRandom random)
        {
            _ = random;

            _nodes = new Dictionary<int, MapNode>
            {
                // Col 0: 시작 (단일)
                [0]  = new MapNode(0,  NodeType.Combat, layer: 0, column: 0, new[] { 1, 2 }),
                // Col 1: 1차 분기 (상/하) — 선택한 행 유지
                [1]  = new MapNode(1,  NodeType.Combat, layer: 1, column: 0, new[] { 3 }),
                [2]  = new MapNode(2,  NodeType.Combat, layer: 1, column: 1, new[] { 4 }),
                // Col 2: 2연속 분기 (상/하)
                [3]  = new MapNode(3,  NodeType.Combat, layer: 2, column: 0, new[] { 5 }),
                [4]  = new MapNode(4,  NodeType.Combat, layer: 2, column: 1, new[] { 5 }),
                // Col 3: 합류 (상점)
                [5]  = new MapNode(5,  NodeType.Shop,   layer: 3, column: 0, new[] { 6, 7 }),
                // Col 4: 3차 분기 (상/하) — 선택한 행 유지
                [6]  = new MapNode(6,  NodeType.Combat, layer: 4, column: 0, new[] { 8 }),
                [7]  = new MapNode(7,  NodeType.Combat, layer: 4, column: 1, new[] { 9 }),
                // Col 5: 4연속 분기 (상/하)
                [8]  = new MapNode(8,  NodeType.Combat, layer: 5, column: 0, new[] { 10 }),
                [9]  = new MapNode(9,  NodeType.Combat, layer: 5, column: 1, new[] { 10 }),
                // Col 6: 합류 (엘리트)
                [10] = new MapNode(10, NodeType.Elite,  layer: 6, column: 0, new[] { 11 }),
                // Col 7: 상점
                [11] = new MapNode(11, NodeType.Shop,   layer: 7, column: 0, new[] { 12 }),
                // Col 8: 보스
                [12] = new MapNode(12, NodeType.Boss,   layer: LAST_LAYER, column: 0, Array.Empty<int>()),
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
