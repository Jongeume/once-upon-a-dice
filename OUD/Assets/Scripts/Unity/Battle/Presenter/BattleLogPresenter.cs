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
                    _view.ShowDamagePopup(_enemyTransforms[d.TargetIndex].position, d.HpDamage);
            }
            if (result.HpRecovered > 0 && _playerTransform != null)
                _view.ShowHealPopup(_playerTransform.position, result.HpRecovered);
        }

        public void ShowEnemyAction(int enemyIndex, IntentType intent, int value)
        {
            if (intent == IntentType.Attack && _playerTransform != null)
                _view.ShowDamagePopup(_playerTransform.position, value);
        }

        public void ShowBattleWon()  => _view.ShowWinScreen();
        public void ShowBattleLost() => _view.ShowLoseScreen();
    }
}
