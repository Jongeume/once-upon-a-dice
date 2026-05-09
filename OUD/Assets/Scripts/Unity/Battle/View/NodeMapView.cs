// NodeMapView.cs
// F-11 Phase D-2 노드맵 UI — NodeMapPanel 루트.
// 4개 NodeView + 4개 연결선을 보유. Bind 시점에 RunMap 데이터로 노드 상태 갱신.
// 노드 클릭 → OnNodeClicked 발화 → BattleUIAdapter가 RunManager.SelectNextNode 호출.
// feature-spec F-11 (Phase D-2), user-flow §1
using System;
using System.Collections.Generic;
using OUD.BattleEngine.Run;
using OUD.Unity.Common;
using UnityEngine;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 노드맵 화면. BattleUIAdapter가 보상 [계속] 클릭 시점에 Bind + Show 호출.
    /// 사용자가 진행 가능 노드 클릭 → OnNodeClicked(nodeId) 발화 → Adapter가 Hide + 다음 전투 트리거.
    /// <para>
    /// sprint MVP 고정 1-2-1 구조 — Inspector에 NodeView 4개를 RunMap의 nodeId 0~3 순서로 바인딩.
    /// </para>
    /// </summary>
    public class NodeMapView : ViewBase
    {
        // ── Inspector 바인딩 ─────────────────────────────────────────────────

        [Header("노드 (RunMap nodeId 0~3 순서로 바인딩)")]
        [Tooltip("Index 0=Node 0(시작) / 1=Node 1(분기 위) / 2=Node 2(분기 아래) / 3=Node 3(Boss)")]
        [SerializeField] private List<NodeView> _nodes;

        [Header("연결선 (장식 — 활성/비활성만 토글)")]
        [Tooltip("연결선 GameObject들. Cleared 노드 → 다음 노드로 향하는 선만 활성화. " +
                 "선택 시 1-2-1 구조에 맞춰 4개: 0→1, 0→2, 1→3, 2→3")]
        [SerializeField] private List<GameObject> _connectionLines;

        // ── 이벤트 ───────────────────────────────────────────────────────────

        /// <summary>노드 클릭 시 nodeId 전달.</summary>
        public event Action<int> OnNodeClicked;

        // ── 초기화 ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_nodes == null) return;
            foreach (NodeView node in _nodes)
            {
                if (node == null) continue;
                node.OnClicked += HandleNodeClicked;
            }
        }

        private void HandleNodeClicked(int nodeId)
        {
            OnNodeClicked?.Invoke(nodeId);
        }

        // ── 외부 API ─────────────────────────────────────────────────────────

        /// <summary>
        /// RunMap 데이터 주입 + 상태 결정.
        /// <para>
        /// 상태 룰:
        /// <list type="bullet">
        ///   <item>currentNodeId == nodeId → Current (방금 클리어, 흰색 외곽)</item>
        ///   <item>availableIds 포함 → Available (시안 글로우, 클릭 가능)</item>
        ///   <item>currentNode 이전 layer → Cleared (알파 0.5)</item>
        ///   <item>그 외 (현재 layer 미선택 분기 + 미해금 미래 노드) → Locked (어둡고 ?만)</item>
        /// </list>
        /// </para>
        /// 호출 측은 Bind 후 Show()를 별도 호출.
        /// </summary>
        public void Bind(RunMap map, int currentNodeId, IReadOnlyList<int> availableIds)
        {
            if (map == null || _nodes == null) return;
            if (availableIds == null) availableIds = Array.Empty<int>();

            MapNode currentNode = map.GetNode(currentNodeId);

            for (int i = 0; i < _nodes.Count; i++)
            {
                NodeView nv = _nodes[i];
                if (nv == null) continue;

                // Inspector 바인딩 인덱스 = nodeId (sprint MVP 1-2-1 고정 구조)
                if (!map.ContainsNode(i)) continue;
                MapNode mapNode = map.GetNode(i);

                nv.SetNode(mapNode);
                nv.SetState(ResolveState(mapNode, currentNode, availableIds));
            }

            UpdateConnectionLines(currentNode);
        }

        // ── 상태 결정 룰 ────────────────────────────────────────────────────

        private static NodeVisualState ResolveState(
            MapNode node,
            MapNode currentNode,
            IReadOnlyList<int> availableIds)
        {
            if (node.Id == currentNode.Id)
                return NodeVisualState.Current;

            // 진행 가능 후보
            for (int i = 0; i < availableIds.Count; i++)
                if (availableIds[i] == node.Id) return NodeVisualState.Available;

            // 이전 layer = 이미 클리어한 (다른 분기 선택지 포함 ─ sprint MVP에선 시작 노드만 해당)
            if (node.Layer < currentNode.Layer)
                return NodeVisualState.Cleared;

            // 같은 layer지만 다른 노드 = 미선택 분기 / 다음 layer 이후 = 미래 노드 → Locked
            return NodeVisualState.Locked;
        }

        // ── 연결선 시각 ──────────────────────────────────────────────────────
        // sprint MVP: 1-2-1 고정이므로 4개 연결선이 항상 같은 의미.
        // currentNode 기준으로 "지나온 길"은 진하게(활성), 그 외 미진입 분기는 어둡게.
        // 단순화 ─ 모두 활성화된 채로 두고, 알파만 currentNode가 통과한 라인만 강조.
        // 본 sprint MVP에선 _connectionLines 가 null/비어있어도 동작 (선 표시 없이도 노드만으로 충분).

        private void UpdateConnectionLines(MapNode currentNode)
        {
            // sprint MVP: 연결선은 단순 장식 ─ 모두 활성화 상태로 두고 별도 시각 갱신은 하지 않음.
            // 후속 작업(폴리시)에서 currentNode 기준 진행 경로 강조 가능.
            // 현재는 _connectionLines가 바인딩되어 있으면 모두 활성화만 보장.
            if (_connectionLines == null) return;
            for (int i = 0; i < _connectionLines.Count; i++)
            {
                GameObject line = _connectionLines[i];
                if (line == null) continue;
                if (!line.activeSelf) line.SetActive(true);
            }
        }
    }
}
