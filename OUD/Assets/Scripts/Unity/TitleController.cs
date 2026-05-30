using OUD.Unity.Common;
using OUD.Unity.Tutorial;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OUD.Unity
{
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _tutorialButton;

        private void Awake()
        {
            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(OnNewGameClicked);
            if (_tutorialButton != null)
                _tutorialButton.onClick.AddListener(OnTutorialClicked);
        }

        private void Start()
        {
            SoundManager.Instance?.PlayBGM();
        }

        private void OnNewGameClicked()
        {
            // 완료 상태를 존중: 완료면 일반 게임, 미완료면 튜토리얼로 진입.
            TutorialEntry.ForceTutorial = false;
            SceneManager.LoadScene("BattleScene");
        }

        private void OnTutorialClicked()
        {
            // 완료 여부와 무관하게 항상 튜토리얼. 영구 완료 상태는 건드리지 않는다
            // (재생 중 종료해도 완료 기록 보존).
            TutorialEntry.ForceTutorial = true;
            SceneManager.LoadScene("BattleScene");
        }
    }
}
