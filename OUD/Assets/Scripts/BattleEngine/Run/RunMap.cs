// RunMap.cs
// 런 1회의 노드맵 데이터 모델 + 분기 구조 정의.
// Phase D-2 (sprint MVP — 노드맵 UI 도입): 1-2-1 구조 + Boss 합류.
// 분기 알고리즘: layer 1에서 2개 노드 중 1개 선택, layer 2(Boss)로 합류.
// IRandom은 향후 동적 맵 생성을 위한 자리만 잡아둠 (현재 구조는 고정 1-2-1).
// feature-spec F-11 (Phase D-2 갱신), game-design-v2.2 §5.1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;

namespace OUD.BattleEngine.Run
{
    /// <summary>
    /// 노드 종류. sprint MVP 한정 — Combat/Boss 2종.
    /// 후속 작업: Rest(F-10), Elite, Treasure 등 확장 가능.
    /// </summary>
    public enum NodeType
    {
        Combat,
        Boss,
    }

    /// <summary>
    /// 맵의 단일 노드. 불변 readonly struct — RunMap 생성 후 연결관계 변경 불가.
    /// Id는 RunMap 내 유일 식별자 (0-based 직렬 번호).
    /// Layer는 진행 단계 (0=시작, MaxLayer=Boss). Column은 같은 layer 내 위치(0-based).
    /// NextNodeIds는 이 노드 클리어 후 진입 가능한 다음 노드 Id 목록 (Boss는 빈 배열).
    /// </summary>
    public readonly struct MapNode
    {
        public int      Id          { get; }
        public NodeType Type        { get; }
        public int      Layer       { get; }
        public int      Column      { get; }

        // 외부 노출은 IReadOnlyList로 캡슐화 (배열 직접 노출 시 외부에서 변경 가능).
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

    /// <summary>
    /// 런 노드맵. sprint MVP 고정 구조: 1-2-1 (시작 → 분기 2 → 보스 합류).
    /// <para>
    /// 노드 ID 배치:
    /// <code>
    ///   Layer 0      Layer 1            Layer 2
    ///                ┌── Node 1 (C) ──┐
    ///   Node 0 (C) ──┤                ├── Node 3 (Boss)
    ///                └── Node 2 (C) ──┘
    /// </code>
    /// </para>
    /// IRandom은 향후 동적 맵 생성용 — 현재 sprint MVP에서는 고정 구조라 사용하지 않음.
    /// </summary>
    public class RunMap
    {
        // ── 1-2-1 구조 상수 ──────────────────────────────────────────────────
        public const int TOTAL_LAYERS  = 3;   // layer 0/1/2
        public const int LAST_LAYER    = 2;   // 보스 layer
        public const int START_NODE_ID = 0;
        public const int LAST_NODE_ID  = 3;   // 보스 노드 ID

        private readonly Dictionary<int, MapNode> _nodes;

        public IReadOnlyCollection<MapNode> AllNodes => _nodes.Values;

        public int NodeCount => _nodes.Count;

        /// <summary>
        /// sprint MVP 고정 1-2-1 구조 생성. random은 향후 동적 생성용 자리만 잡음.
        /// </summary>
        public RunMap(IRandom random)
        {
            // sprint MVP 한정 — random 미사용. null 허용 (향후 시그니처 호환 위해 인자만 받음).
            _ = random;

            _nodes = new Dictionary<int, MapNode>
            {
                // Node 0: Layer 0, 시작 — layer 1의 두 노드(1,2)로 분기
                [0] = new MapNode(
                    id: 0, type: NodeType.Combat, layer: 0, column: 0,
                    nextNodeIds: new[] { 1, 2 }),

                // Node 1: Layer 1, 첫 번째 분기 — Boss(3)로 합류
                [1] = new MapNode(
                    id: 1, type: NodeType.Combat, layer: 1, column: 0,
                    nextNodeIds: new[] { 3 }),

                // Node 2: Layer 1, 두 번째 분기 — Boss(3)로 합류
                [2] = new MapNode(
                    id: 2, type: NodeType.Combat, layer: 1, column: 1,
                    nextNodeIds: new[] { 3 }),

                // Node 3: Layer 2, Boss — 마지막 노드
                [3] = new MapNode(
                    id: 3, type: NodeType.Boss, layer: LAST_LAYER, column: 0,
                    nextNodeIds: Array.Empty<int>()),
            };
        }

        /// <summary>nodeId로 MapNode 조회. 없으면 예외.</summary>
        public MapNode GetNode(int nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out MapNode node))
                throw new ArgumentException($"존재하지 않는 nodeId: {nodeId}", nameof(nodeId));
            return node;
        }

        /// <summary>nodeId가 맵에 존재하는지.</summary>
        public bool ContainsNode(int nodeId) => _nodes.ContainsKey(nodeId);
    }
}
