using System;
using System.Collections.Generic;
using OUD.BattleEngine.Run;
using OUD.Unity.Common;
using UnityEngine;

namespace OUD.Unity.Battle.View
{
    public class NodeMapView : ViewBase
    {
        [Header("노드 (RunMap nodeId 0~7 순서로 바인딩)")]
        [SerializeField] private List<NodeView> _nodes;

        [Header("연결선 (장식 — 활성/비활성만 토글)")]
        [SerializeField] private List<GameObject> _connectionLines;

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

        public void Bind(RunMap map, int currentNodeId, IReadOnlyList<int> availableIds)
        {
            if (map == null || _nodes == null) return;
            if (availableIds == null) availableIds = Array.Empty<int>();

            MapNode currentNode = map.GetNode(currentNodeId);

            for (int i = 0; i < _nodes.Count; i++)
            {
                NodeView nv = _nodes[i];
                if (nv == null) continue;

                if (!map.ContainsNode(i)) continue;
                MapNode mapNode = map.GetNode(i);

                nv.SetNode(mapNode);
                nv.SetState(ResolveState(mapNode, currentNode, availableIds));
            }

            UpdateConnectionLines(currentNode);
        }

        private static NodeVisualState ResolveState(
            MapNode node,
            MapNode currentNode,
            IReadOnlyList<int> availableIds)
        {
            if (node.Id == currentNode.Id)
                return NodeVisualState.Current;

            for (int i = 0; i < availableIds.Count; i++)
                if (availableIds[i] == node.Id) return NodeVisualState.Available;

            if (node.Layer < currentNode.Layer)
                return NodeVisualState.Cleared;

            return NodeVisualState.Locked;
        }

        private void UpdateConnectionLines(MapNode currentNode)
        {
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
