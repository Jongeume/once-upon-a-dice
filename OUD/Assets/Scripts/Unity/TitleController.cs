using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OUD.Unity
{
    public class TitleController : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;

        private void Awake()
        {
            if (_newGameButton != null)
                _newGameButton.onClick.AddListener(OnNewGameClicked);
        }

        private void OnNewGameClicked()
        {
            SceneManager.LoadScene("BattleScene");
        }
    }
}
