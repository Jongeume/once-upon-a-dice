using OUD.Unity.Battle;
using OUD.Unity.Common;
using TMPro;
using UnityEngine;

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
