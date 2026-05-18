using System;
using System.Collections.Generic;
using OUD.BattleEngine.Run;
using OUD.Unity.Common;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>연결선 하나를 나타낸다. Inspector에서 From/To nodeId와 Image를 바인딩.</summary>
    [System.Serializable]
    public struct ConnectionLineData
    {
        public int   FromNodeId;
        public int   ToNodeId;
        public Image LineImage;
    }

    public class NodeMapView : ViewBase
    {
        [Header("노드 (RunMap nodeId 0~12 순서로 바인딩 — 인덱스 = nodeId)")]
        [SerializeField] private List<NodeView> _nodes;

        [Header("연결선 18개 (ConnectionLineData)")]
        [SerializeField] private List<ConnectionLineData> _connections;

        private static readonly Color LINE_ACTIVE   = new Color(0.83f, 0.63f, 0.09f, 1f);
        private static readonly Color LINE_INACTIVE = new Color(0.2f,  0.2f,  0.2f,  0.5f);

        public event Action<int> OnNodeClicked;

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

        /// <summary>
        /// 노드맵 상태를 바인딩한다.
        /// currentNodeId = -1이면 아직 아무 노드도 진행하지 않은 상태(게임 시작 직후).
        /// </summary>
        public void Bind(RunMap map, int currentNodeId, IReadOnlyList<int> availableIds)
        {
            if (map == null || _nodes == null) return;
            if (availableIds == null) availableIds = Array.Empty<int>();

            bool    hasCurrentNode = map.ContainsNode(currentNodeId);
            MapNode currentNode    = hasCurrentNode ? map.GetNode(currentNodeId) : default;

            for (int i = 0; i < _nodes.Count; i++)
            {
                NodeView nv = _nodes[i];
                if (nv == null) continue;

                if (!map.ContainsNode(i))
                {
                    nv.gameObject.SetActive(false);
                    continue;
                }

                MapNode mapNode = map.GetNode(i);
                nv.gameObject.SetActive(true);
                nv.SetNode(mapNode);
                nv.SetState(ResolveState(mapNode, hasCurrentNode, currentNode, availableIds));
            }

            UpdateConnectionColors(map, hasCurrentNode, currentNode, availableIds);
        }

        private static NodeVisualState ResolveState(
            MapNode node,
            bool hasCurrentNode,
            MapNode currentNode,
            IReadOnlyList<int> availableIds)
        {
            if (hasCurrentNode && node.Id == currentNode.Id)
                return NodeVisualState.Current;

            for (int i = 0; i < availableIds.Count; i++)
                if (availableIds[i] == node.Id) return NodeVisualState.Available;

            if (hasCurrentNode && node.Layer < currentNode.Layer)
                return NodeVisualState.Cleared;

            return NodeVisualState.Locked;
        }

        private void UpdateConnectionColors(
            RunMap map,
            bool hasCurrentNode,
            MapNode currentNode,
            IReadOnlyList<int> availableIds)
        {
            if (_connections == null) return;

            foreach (ConnectionLineData conn in _connections)
            {
                if (conn.LineImage == null) continue;

                bool active = false;

                if (hasCurrentNode)
                {
                    int fromLayer = map.ContainsNode(conn.FromNodeId)
                        ? map.GetNode(conn.FromNodeId).Layer : int.MaxValue;
                    int toLayer = map.ContainsNode(conn.ToNodeId)
                        ? map.GetNode(conn.ToNodeId).Layer : int.MaxValue;

                    bool bothCleared = fromLayer < currentNode.Layer
                                    && toLayer  <= currentNode.Layer;

                    bool fromIsCurrent = conn.FromNodeId == currentNode.Id;
                    bool toIsAvailable = IsInList(conn.ToNodeId, availableIds);

                    active = bothCleared || (fromIsCurrent && toIsAvailable);
                }

                conn.LineImage.color = active ? LINE_ACTIVE : LINE_INACTIVE;
            }
        }

        private static bool IsInList(int value, IReadOnlyList<int> list)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == value) return true;
            return false;
        }
    }
}
