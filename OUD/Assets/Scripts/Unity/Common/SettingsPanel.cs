using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Common
{
    /// <summary>
    /// 설정 패널: 설정 버튼 클릭 시 패널 토글.
    /// 게임 종료 버튼 + 모바일 뒤로가기 두번 종료 기능 포함.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button     _settingsButton;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button     _quitButton;
        [SerializeField] private Button     _closeButton;

        [Header("Mobile Back Button")]
        [SerializeField] private float _backButtonInterval = 2f;

        private float _lastBackTime = -10f;

        private void Start()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(TogglePanel);

            if (_quitButton != null)
                _quitButton.onClick.AddListener(QuitGame);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(ClosePanel);
        }

        private void Update()
        {
            // ESC 키 또는 모바일 뒤로가기 버튼
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                // 패널이 열려있으면 닫기
                if (_panelRoot != null && _panelRoot.activeSelf)
                {
                    ClosePanel();
                    return;
                }

                // 모바일: 뒤로가기 두번 누르면 종료
#if UNITY_ANDROID && !UNITY_EDITOR
                if (Time.unscaledTime - _lastBackTime < _backButtonInterval)
                {
                    QuitGame();
                }
                else
                {
                    _lastBackTime = Time.unscaledTime;
                }
#endif
            }
        }

        private void TogglePanel()
        {
            if (_panelRoot == null) return;
            _panelRoot.SetActive(!_panelRoot.activeSelf);
        }

        private void ClosePanel()
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
