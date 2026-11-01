using UnityEngine;

namespace OUD.Unity.Common
{
    /// <summary>Screen.safeArea를 RectTransform의 anchorMin/Max로 매핑.
    /// 부모 Canvas가 Screen Space - Overlay이거나 ScreenSpace 자식에 부착되어 있을 때
    /// 노치/홈바 영역을 피해 stretch 되도록 한다. ExecuteAlways로 에디터에서도 즉시 반영.</summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private ScreenOrientation _lastOrientation;

        private void OnEnable()
        {
            _rect = GetComponent<RectTransform>();
            Apply(force: true);
        }

        private void Update()
        {
            Apply(force: false);
        }

        private void Apply(bool force)
        {
            if (_rect == null) _rect = GetComponent<RectTransform>();

            Rect safe = Screen.safeArea;
            Vector2Int size = new Vector2Int(Screen.width, Screen.height);
            ScreenOrientation orientation = Screen.orientation;

            if (!force
                && safe == _lastSafeArea
                && size == _lastScreenSize
                && orientation == _lastOrientation)
            {
                return;
            }

            _lastSafeArea = safe;
            _lastScreenSize = size;
            _lastOrientation = orientation;

            if (size.x <= 0 || size.y <= 0) return;

            Vector2 anchorMin = safe.position;
            Vector2 anchorMax = safe.position + safe.size;
            anchorMin.x /= size.x;
            anchorMin.y /= size.y;
            anchorMax.x /= size.x;
            anchorMax.y /= size.y;

            _rect.anchorMin = anchorMin;
            _rect.anchorMax = anchorMax;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
