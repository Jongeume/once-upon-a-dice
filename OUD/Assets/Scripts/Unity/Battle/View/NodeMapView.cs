using System;
using System.Collections;
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

        [Header("플레이어 위치 아이콘")]
        [SerializeField] private Image _playerIcon;
        [SerializeField] private Sprite _playerIconSprite;

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
        public void Bind(RunMap map, int currentNodeId, IReadOnlyList<int> availableIds,
            IReadOnlyCollection<int> visitedNodeIds = null, bool animateIcon = false,
            Action onIconMoveComplete = null)
        {
            if (map == null || _nodes == null) return;
            if (availableIds == null) availableIds = Array.Empty<int>();

            bool    hasCurrentNode = map.ContainsNode(currentNodeId);
            MapNode currentNode    = hasCurrentNode ? map.GetNode(currentNodeId) : default;

            int iconTargetNodeId = hasCurrentNode ? currentNodeId : RunMap.START_NODE_ID;

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
                nv.SetState(ResolveState(mapNode, hasCurrentNode, currentNode, availableIds, visitedNodeIds));
            }

            UpdateConnectionColors(map, hasCurrentNode, currentNode, availableIds, visitedNodeIds);
            PositionPlayerIcon(iconTargetNodeId, animateIcon, onIconMoveComplete);
        }

        private static NodeVisualState ResolveState(
            MapNode node,
            bool hasCurrentNode,
            MapNode currentNode,
            IReadOnlyList<int> availableIds,
            IReadOnlyCollection<int> visitedNodeIds)
        {
            if (hasCurrentNode && node.Id == currentNode.Id)
                return NodeVisualState.Current;

            for (int i = 0; i < availableIds.Count; i++)
                if (availableIds[i] == node.Id) return NodeVisualState.Available;

            if (visitedNodeIds != null && IsVisited(node.Id, visitedNodeIds) && node.Id != currentNode.Id)
                return NodeVisualState.Cleared;

            return NodeVisualState.Locked;
        }

        private void UpdateConnectionColors(
            RunMap map,
            bool hasCurrentNode,
            MapNode currentNode,
            IReadOnlyList<int> availableIds,
            IReadOnlyCollection<int> visitedNodeIds)
        {
            if (_connections == null) return;

            foreach (ConnectionLineData conn in _connections)
            {
                if (conn.LineImage == null) continue;

                bool active = false;

                if (hasCurrentNode)
                {
                    bool bothVisited = visitedNodeIds != null
                        && IsVisited(conn.FromNodeId, visitedNodeIds)
                        && IsVisited(conn.ToNodeId, visitedNodeIds);

                    bool fromIsCurrent = conn.FromNodeId == currentNode.Id;
                    bool toIsAvailable = IsInList(conn.ToNodeId, availableIds);

                    active = bothVisited || (fromIsCurrent && toIsAvailable);
                }

                conn.LineImage.color = active ? LINE_ACTIVE : LINE_INACTIVE;
            }
        }

        private const float ICON_MOVE_DURATION = 0.5f;

        private int _lastIconNodeId = -1;
        private Coroutine _iconAnimCoroutine;

        private void PositionPlayerIcon(int targetNodeId, bool animate, Action onComplete = null)
        {
            if (_playerIcon == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (_playerIconSprite != null)
                _playerIcon.sprite = _playerIconSprite;

            if (targetNodeId < 0 || targetNodeId >= _nodes.Count || _nodes[targetNodeId] == null)
            {
                _playerIcon.gameObject.SetActive(false);
                _lastIconNodeId = -1;
                onComplete?.Invoke();
                return;
            }

            if (_iconAnimCoroutine != null)
            {
                StopCoroutine(_iconAnimCoroutine);
                _iconAnimCoroutine = null;
            }

            RectTransform iconRect = _playerIcon.GetComponent<RectTransform>();
            RectTransform toRect   = _nodes[targetNodeId].GetComponent<RectTransform>();
            if (iconRect == null || toRect == null)
            {
                _lastIconNodeId = targetNodeId;
                onComplete?.Invoke();
                return;
            }

            _playerIcon.gameObject.SetActive(true);

            bool canAnimate = animate
                && _lastIconNodeId >= 0
                && _lastIconNodeId != targetNodeId
                && _lastIconNodeId < _nodes.Count
                && _nodes[_lastIconNodeId] != null;

            if (canAnimate)
            {
                RectTransform fromRect = _nodes[_lastIconNodeId].GetComponent<RectTransform>();
                if (fromRect != null)
                {
                    iconRect.position = fromRect.position;
                    _iconAnimCoroutine = StartCoroutine(AnimateIconMove(iconRect, fromRect.position, toRect.position, onComplete));
                    _lastIconNodeId = targetNodeId;
                    return;
                }
            }

            iconRect.position = toRect.position;
            _lastIconNodeId = targetNodeId;
            onComplete?.Invoke();
        }

        private IEnumerator AnimateIconMove(RectTransform iconRect, Vector3 from, Vector3 to, Action onComplete = null)
        {
            float elapsed = 0f;
            while (elapsed < ICON_MOVE_DURATION)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ICON_MOVE_DURATION);
                float eased = 1f - (1f - t) * (1f - t);
                iconRect.position = Vector3.Lerp(from, to, eased);
                yield return null;
            }
            iconRect.position = to;
            _iconAnimCoroutine = null;
            onComplete?.Invoke();
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

        private static bool IsVisited(int value, IReadOnlyCollection<int> visited)
        {
            foreach (int id in visited)
                if (id == value) return true;
            return false;
        }
    }
}
