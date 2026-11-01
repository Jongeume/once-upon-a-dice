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
            SceneManager.LoadScene("BattleScene");
        }

        private void OnTutorialClicked()
        {
            TutorialState.Reset();
            SceneManager.LoadScene("BattleScene");
        }
    }
}
