using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OUD.Unity.Common
{
    /// <summary>
    /// 설정 패널: 설정 버튼 클릭 시 패널 토글.
    /// 메인화면 복귀 / 게임 종료 버튼 + 모바일 뒤로가기 두번 종료 기능 포함.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button     _settingsButton;
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private Button     _quitButton;
        [SerializeField] private Button     _closeButton;
        [SerializeField] private Button     _mainMenuButton;

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

            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(GoToMainMenu);
        }

        private void Update()
        {
            // ESC 키 또는 모바일 뒤로가기 버튼 (New Input System)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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
                    ShowAndroidToast("한 번 더 누르면 종료됩니다");
                }
#endif
            }
        }

        // ── Android 네이티브 Toast ──────────────────────────────────────
#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ShowAndroidToast(string message)
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                using var toast = new AndroidJavaClass("android.widget.Toast");
                using var t     = toast.CallStatic<AndroidJavaObject>(
                                      "makeText", activity, message, 0); // 0 = LENGTH_SHORT
                t.Call("show");
            }));
        }
#endif

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

        private void GoToMainMenu()
        {
            // 게임 상태 초기화 후 타이틀 씬으로 복귀.
            // TutorialState는 PlayerPrefs 기반이라 자동 유지 — New Game 시 튜토리얼 스킵됨.
            Time.timeScale = 1f; // 일시정지 상태일 수 있으므로 복원
            SceneManager.LoadScene("TitleScene");
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
