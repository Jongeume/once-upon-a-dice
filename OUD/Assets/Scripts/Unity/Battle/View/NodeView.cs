// NodeView.cs
// F-11 Phase D-2 노드맵 UI — 단일 노드 표현.
// NodeMapView가 4개를 보유. 상태(Cleared/Current/Available/Locked)에 따라
// 알파/Outline 두께/라벨 텍스트/클릭 활성 여부를 갱신한다.
// feature-spec F-11 (Phase D-2), user-flow §1
using System;
using OUD.BattleEngine.Run;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>
    /// 노드의 시각 상태. NodeMapView가 Bind 시점에 결정해 SetState로 주입한다.
    /// </summary>
    public enum NodeVisualState
    {
        Cleared,    // 이미 클리어한 노드 (알파 0.5, 흐릿)
        Current,    // 방금 클리어한 노드 = 현재 위치 (알파 1.0, 흰색 외곽)
        Available,  // 다음 진입 가능 (밝은 시안 외곽 + 두꺼움, 클릭 가능)
        Locked,     // 미해금 (알파 0.4, 어둡고 ?만 표시)
    }

    /// <summary>
    /// 단일 노드 UI. Inspector에서 Image 프레임 + Outline + TMP_Text 라벨 + Button을 바인딩.
    /// NodeMapView가 4개를 자식으로 두고 Bind 시점에 SetNode + SetState 호출.
    /// </summary>
    public class NodeView : MonoBehaviour
    {
        // ── Inspector 바인딩 ─────────────────────────────────────────────────

        [Header("프레임 (Image + Outline)")]
        [SerializeField] private Image   _frameImage;
        [SerializeField] private Outline _frameOutline;

        [Header("노드 타입 아이콘")]
        [SerializeField] private Image _iconImage;

        [Header("라벨 (중앙) — Combat='전투' / Boss='보스' / Locked='?'")]
        [SerializeField] private TMP_Text _labelText;

        [Header("클리어 오버레이")]
        [SerializeField] private TMP_Text _clearedOverlay;

        [Header("클릭 (Available 상태에서만 활성)")]
        [SerializeField] private Button _clickButton;

        // ── 상수 (CLAUDE.md hard rule: 매직 넘버 금지) ─────────────────────

        // 시각 색상 ─ 이미지 참고 (시안 글로우 + 다크 블루 베이스)
        private static readonly Color FRAME_COLOR_COMBAT  = new Color(0.20f, 0.30f, 0.50f, 1f);
        private static readonly Color FRAME_COLOR_BOSS    = new Color(0.55f, 0.10f, 0.10f, 1f);
        private static readonly Color FRAME_COLOR_SHOP    = new Color(0.10f, 0.45f, 0.20f, 1f);
        private static readonly Color FRAME_COLOR_ELITE   = new Color(0.60f, 0.15f, 0.15f, 1f);
        private static readonly Color FRAME_COLOR_START   = new Color(0.30f, 0.30f, 0.35f, 1f); // 중립 회색 (빈 시작 노드)
        private static readonly Color OUTLINE_AVAILABLE   = new Color(0.00f, 0.85f, 1.00f, 1f); // 밝은 시안 (#00D8FF)
        private static readonly Color OUTLINE_CURRENT     = new Color(1.00f, 1.00f, 1.00f, 1f); // 흰색
        private static readonly Color OUTLINE_CLEARED     = new Color(0.40f, 0.40f, 0.40f, 1f); // 회색
        private static readonly Color OUTLINE_LOCKED      = new Color(0.15f, 0.15f, 0.20f, 1f); // 어두움

        private static readonly Color LABEL_COLOR_COMBAT = Color.white;
        private static readonly Color LABEL_COLOR_BOSS   = new Color(1.00f, 0.85f, 0.20f, 1f);
        private static readonly Color LABEL_COLOR_SHOP   = new Color(0.40f, 1.00f, 0.50f, 1f);
        private static readonly Color LABEL_COLOR_ELITE  = new Color(1.00f, 0.40f, 0.40f, 1f);
        private static readonly Color LABEL_COLOR_START  = new Color(0.85f, 0.85f, 0.90f, 1f);

        // Outline 두께 (px) ─ Available 시 두껍게 강조
        private static readonly Vector2 OUTLINE_THICK = new Vector2(5f, 5f);
        private static readonly Vector2 OUTLINE_NORM  = new Vector2(2f, 2f);

        // 알파 (상태별)
        private const float ALPHA_FULL    = 1.0f;
        private const float ALPHA_CLEARED = 0.5f;
        private const float ALPHA_LOCKED  = 0.4f;

        // 라벨 텍스트
        private const string LABEL_COMBAT = "전투";
        private const string LABEL_BOSS   = "보스";
        private const string LABEL_SHOP   = "상점";
        private const string LABEL_ELITE  = "엘리트";
        private const string LABEL_START  = "시작";
        private const string LABEL_LOCKED = "?";
        private const string OVERLAY_CHECK = "V";

        // ── 상태 ─────────────────────────────────────────────────────────────

        public int NodeId { get; private set; }
        public event Action<int> OnClicked;

        private NodeType _currentNodeType;

        private void Awake()
        {
            if (_clickButton != null)
                _clickButton.onClick.AddListener(() => OnClicked?.Invoke(NodeId));

            // Bind() 전 흰 박스/텍스트 방지: 프레임 투명, 아이콘·라벨 비활성
            if (_frameImage != null)
            {
                Color c = _frameImage.color;
                c.a = 0f;
                _frameImage.color = c;
            }
            if (_iconImage != null)
                _iconImage.enabled = false;
            if (_labelText != null)
                _labelText.enabled = false;
        }

        // ── 외부 API ─────────────────────────────────────────────────────────

        /// <summary>아이콘 스프라이트를 외부에서 주입. NodeMapView가 Bind 시 호출.</summary>
        public void SetIconSprite(Sprite sprite)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = sprite;
                _iconImage.enabled = sprite != null;
                _iconImage.preserveAspect = true;
            }
        }

        /// <summary>
        /// 노드 데이터 주입. NodeId / Type 별 라벨 + 베이스 컬러 설정.
        /// SetState 호출 전 1회 사용.
        /// </summary>
        public void SetNode(MapNode node)
        {
            NodeId = node.Id;
            _currentNodeType = node.Type;

            Color frameColor;
            string label;
            Color labelColor;
            bool bold;

            switch (node.Type)
            {
                case NodeType.Boss:
                    frameColor = FRAME_COLOR_BOSS;
                    label = LABEL_BOSS;
                    labelColor = LABEL_COLOR_BOSS;
                    bold = true;
                    break;
                case NodeType.Shop:
                    frameColor = FRAME_COLOR_SHOP;
                    label = LABEL_SHOP;
                    labelColor = LABEL_COLOR_SHOP;
                    bold = false;
                    break;
                case NodeType.Elite:
                    frameColor = FRAME_COLOR_ELITE;
                    label = LABEL_ELITE;
                    labelColor = LABEL_COLOR_ELITE;
                    bold = true;
                    break;
                case NodeType.Start:
                    frameColor = FRAME_COLOR_START;
                    label = LABEL_START;
                    labelColor = LABEL_COLOR_START;
                    bold = false;
                    break;
                default:
                    frameColor = FRAME_COLOR_COMBAT;
                    label = LABEL_COMBAT;
                    labelColor = LABEL_COLOR_COMBAT;
                    bold = false;
                    break;
            }

            if (_frameImage != null) _frameImage.color = frameColor;
            if (_labelText != null)
            {
                _labelText.text = label;
                _labelText.color = labelColor;
                _labelText.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_clearedOverlay != null)
            {
                _clearedOverlay.text = OVERLAY_CHECK;
                _clearedOverlay.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 시각 상태 전환. SetNode 이후 매번 호출 가능.
        /// </summary>
        public void SetState(NodeVisualState state)
        {
            switch (state)
            {
                case NodeVisualState.Cleared:
                    ApplyAlpha(ALPHA_CLEARED);
                    SetOutline(OUTLINE_CLEARED, OUTLINE_NORM);
                    SetClickable(false);
                    SetClearedOverlay(true);
                    SetLabelTextOverride(null);  // 원래 라벨 유지
                    SetIconVisible(true);
                    break;

                case NodeVisualState.Current:
                    ApplyAlpha(ALPHA_FULL);
                    SetOutline(OUTLINE_CURRENT, OUTLINE_NORM);
                    SetClickable(false);
                    SetClearedOverlay(true);
                    SetLabelTextOverride(null);
                    SetIconVisible(true);
                    break;

                case NodeVisualState.Available:
                    ApplyAlpha(ALPHA_FULL);
                    SetOutline(OUTLINE_AVAILABLE, OUTLINE_THICK);
                    SetClickable(true);
                    SetClearedOverlay(false);
                    SetLabelTextOverride(null);
                    SetIconVisible(true);
                    break;

                case NodeVisualState.Locked:
                    ApplyAlpha(ALPHA_LOCKED);
                    SetOutline(OUTLINE_LOCKED, OUTLINE_NORM);
                    SetClickable(false);
                    SetClearedOverlay(false);
                    SetLabelTextOverride(null);  // 아이콘 저알파로 잠금 상태 표현
                    SetIconVisible(true);
                    break;
            }
        }

        // ── 내부 헬퍼 ───────────────────────────────────────────────────────

        private void ApplyAlpha(float alpha)
        {
            if (_frameImage != null)
            {
                // 프레임 배경은 투명하게 유지 (아이콘이 전체 영역을 커버)
                // 외곽선은 useGraphicAlpha=false 로 독립 렌더됨
                Color c = _frameImage.color;
                c.a = 0f;
                _frameImage.color = c;
            }
            if (_iconImage != null)
            {
                Color c = _iconImage.color;
                c.a = alpha;
                _iconImage.color = c;
            }
            if (_labelText != null)
            {
                Color c = _labelText.color;
                c.a = alpha;
                _labelText.color = c;
            }
        }

        private void SetOutline(Color color, Vector2 distance)
        {
            if (_frameOutline == null) return;
            _frameOutline.effectColor    = color;
            _frameOutline.effectDistance = distance;
        }

        private void SetClickable(bool clickable)
        {
            if (_clickButton == null) return;
            _clickButton.interactable = clickable;
        }

        /// <summary>Button 컴포넌트 자체를 enable/disable.
        /// peek 모드 등에서 클릭 모션(하이라이트/눌림)까지 완전 차단해야 할 때 사용.
        /// 비활성 시 시각(이미지/외곽선)은 그대로 유지되며 클릭만 무시됨.</summary>
        public void SetButtonEnabled(bool enabled)
        {
            if (_clickButton == null) return;
            _clickButton.enabled = enabled;
        }

        private void SetClearedOverlay(bool show)
        {
            if (_clearedOverlay == null) return;
            _clearedOverlay.gameObject.SetActive(show);
        }

        private void SetIconVisible(bool visible)
        {
            if (_iconImage != null)
                _iconImage.enabled = visible && _iconImage.sprite != null;
            // 아이콘 이미지로 타입 표현 → 라벨 텍스트 항상 숨김
            if (_labelText != null)
                _labelText.enabled = false;
        }

        /// <summary>Locked 상태에서 라벨을 "?"로 덮어쓰기. null이면 SetNode에서 설정한 원본 유지.</summary>
        private void SetLabelTextOverride(string overrideText)
        {
            if (_labelText == null) return;
            if (overrideText != null)
                _labelText.text = overrideText;
            // null인 경우 SetNode에서 설정한 텍스트 그대로 유지 (별도 작업 없음)
        }
    }
}
