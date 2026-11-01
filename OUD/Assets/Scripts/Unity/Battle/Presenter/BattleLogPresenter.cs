using OUD.BattleEngine.Combat;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Skill;
using OUD.Unity.Battle;
using UnityEngine;

namespace OUD.Unity.Battle.Presenter
{
    /// <summary>슬롯 실행/적 행동 결과 → 데미지 팝업 + 승패 화면.</summary>
    public class BattleLogPresenter
    {
        private readonly IBattleLogView _view;
        private readonly Transform[]    _enemyTransforms;
        private readonly Transform      _playerTransform;

        public BattleLogPresenter(
            IBattleLogView view,
            Transform playerTransform,
            Transform[] enemyTransforms)
        {
            _view             = view;
            _playerTransform  = playerTransform;
            _enemyTransforms  = enemyTransforms;
        }

        public void ShowSlotResult(int slotIndex, SkillResult result)
        {
            foreach (var d in result.Damages)
            {
                if (d.TargetIndex >= 0 && d.TargetIndex < _enemyTransforms.Length)
                    _view.ShowDamagePopup(GetAbovePosition(_enemyTransforms[d.TargetIndex]), d.HpDamage);
            }
            if (result.HpRecovered > 0 && _playerTransform != null)
                _view.ShowHealPopup(GetAbovePosition(_playerTransform), result.HpRecovered);
        }

        public void ShowEnemyAction(int enemyIndex, IntentType intent, int value)
        {
            if (intent == IntentType.Attack && _playerTransform != null)
                _view.ShowDamagePopup(GetAbovePosition(_playerTransform), value);
        }

        /// <summary>RectTransform 상단 바로 위. 카드 프레임 직상단에서 팝업 시작.</summary>
        private const float POPUP_EXTRA_OFFSET = 5f;

        private static Vector3 GetAbovePosition(Transform t)
        {
            if (t == null) return Vector3.zero;
            RectTransform rt = t as RectTransform;
            if (rt != null)
            {
                Vector3 pos = rt.position;
                pos.y += rt.rect.height * rt.lossyScale.y * (1f - rt.pivot.y) + POPUP_EXTRA_OFFSET;
                return pos;
            }
            return t.position;
        }

        public void ShowBattleWon()  => _view.ShowWinScreen();
        public void ShowBattleLost() => _view.ShowLoseScreen();
    }
}
