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
        [Header("맵 패널 배경")]
        [SerializeField] private Image _panelBackground;

        [Header("노드 타입별 아이콘 스프라이트")]
        [SerializeField] private Sprite _combatNodeSprite;
        [SerializeField] private Sprite _bossNodeSprite;
        [SerializeField] private Sprite _shopNodeSprite;
        [SerializeField] private Sprite _eliteNodeSprite;

        [Header("노드 (RunMap nodeId 0~12 순서로 바인딩 — 인덱스 = nodeId)")]
        [SerializeField] private List<NodeView> _nodes;

        [Header("연결선 18개 (ConnectionLineData)")]
        [SerializeField] private List<ConnectionLineData> _connections;

        private static readonly Color LINE_ACTIVE   = new Color(0.83f, 0.63f, 0.09f, 1f);
        private static readonly Color LINE_INACTIVE = new Color(0.2f,  0.2f,  0.2f,  0.5f);

        public event Action<int> OnNodeClicked;

        // 기준 크기 (Inspector 세팅값 기준)
        private const float MAP_BASE_W = 700f;
        private const float MAP_BASE_H = 520f;
        // 화면 점유 비율 상한 (가로 92%, 세로 72%)
        private const float MAP_FILL_W = 0.92f;
        private const float MAP_FILL_H = 0.72f;

        private void Awake()
        {
            FitToScreen();
            if (_nodes == null) return;
            foreach (NodeView node in _nodes)
            {
                if (node == null) continue;
                node.OnClicked += HandleNodeClicked;
            }
        }

        // 화면 해상도에 맞게 균등 스케일 자동 계산 (비율 유지 + 화면 넘침 방지)
        private void FitToScreen()
        {
            float s = Mathf.Min(
                Screen.width  * MAP_FILL_W / MAP_BASE_W,
                Screen.height * MAP_FILL_H / MAP_BASE_H);
            ((RectTransform)transform).localScale = new Vector3(s, s, 1f);
        }

        private void HandleNodeClicked(int nodeId)
        {
            OnNodeClicked?.Invoke(nodeId);
        }

        /// <summary>모든 노드의 Button 컴포넌트 enable/disable.
        /// peek 모드 진입 시 false 로 호출하여 클릭 모션까지 완전 차단.</summary>
        public void SetAllButtonsEnabled(bool enabled)
        {
            if (_nodes == null) return;
            foreach (NodeView node in _nodes)
            {
                if (node != null) node.SetButtonEnabled(enabled);
            }
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
                nv.SetIconSprite(GetNodeTypeSprite(mapNode.Type));
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

        private Sprite GetNodeTypeSprite(NodeType type)
        {
            switch (type)
            {
                case NodeType.Combat: return _combatNodeSprite;
                case NodeType.Boss:   return _bossNodeSprite;
                case NodeType.Shop:   return _shopNodeSprite;
                case NodeType.Elite:  return _eliteNodeSprite;
                default:              return _combatNodeSprite;
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
