using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OUD.Unity.Battle.View
{
    /// <summary>데미지 팝업 풀 + 승패 화면.</summary>
    public class BattleLogView : ViewBase, IBattleLogView
    {
        [Header("데미지 팝업")]
        [SerializeField] private DamagePopup _popupPrefab;
        [SerializeField] private Transform   _popupParent;

        [Header("승패 화면")]
        [SerializeField] private GameObject _winScreen;
        [SerializeField] private GameObject _loseScreen;

        /// <summary>WinScreen 클릭 시 호출. Adapter가 구독해 보상 화면으로 즉시 전환한다.</summary>
        public event System.Action OnWinScreenClicked;

        private const int POOL_SIZE = 10;
        private DamagePopup[] _pool;
        private int           _poolIndex;

        private void Awake()
        {
            _pool = new DamagePopup[POOL_SIZE];
            if (_popupPrefab != null && _popupParent != null)
            {
                for (int i = 0; i < POOL_SIZE; i++)
                {
                    _pool[i] = Instantiate(_popupPrefab, _popupParent);
                    _pool[i].gameObject.SetActive(false);
                }
            }

            // WinScreen 자체에 Button이 붙어 있으면 클릭 이벤트로 노출.
            if (_winScreen != null)
            {
                var btn = _winScreen.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(() => OnWinScreenClicked?.Invoke());
            }
        }

        public void ShowDamagePopup(Vector3 worldPos, int damage)
        {
            var popup = GetPooled();
            if (popup == null) return;
            popup.Play(worldPos, damage.ToString(), Color.red);
        }

        public void ShowHealPopup(Vector3 worldPos, int amount)
        {
            var popup = GetPooled();
            if (popup == null) return;
            popup.Play(worldPos, $"+{amount}", Color.green);
        }

        public void ShowWinScreen()
        {
            if (_winScreen)  _winScreen.SetActive(true);
        }

        public void ShowLoseScreen()
        {
            if (_loseScreen) _loseScreen.SetActive(true);
        }

        /// <summary>
        /// 결과 화면(승/패)을 모두 숨긴다. 다음 전투 시작 시 호출해
        /// 이전 라운드의 "전투 승리" 라벨이 그대로 남는 것을 방지.
        /// </summary>
        public void HideResultScreens()
        {
            if (_winScreen)  _winScreen.SetActive(false);
            if (_loseScreen) _loseScreen.SetActive(false);
        }

        private DamagePopup GetPooled()
        {
            for (int i = 0; i < POOL_SIZE; i++)
            {
                int idx = (_poolIndex + i) % POOL_SIZE;
                if (_pool[idx] != null && !_pool[idx].gameObject.activeSelf)
                {
                    _poolIndex = idx;
                    return _pool[idx];
                }
            }
            return null;
        }
    }
}
