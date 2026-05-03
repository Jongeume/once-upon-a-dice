using OUD.BattleEngine.Unit;
using OUD.Unity.Battle;

namespace OUD.Unity.Battle.Presenter
{
    /// <summary>PlayerState → IPlayerView 데이터 가공.</summary>
    public class PlayerPresenter
    {
        private readonly IPlayerView _view;
        private PlayerState _state;

        /// <summary>Init 호출 후에만 유효. OnBattleWon 등 후속 이벤트에서 PlayerState 접근용.</summary>
        public PlayerState Player => _state;

        public PlayerPresenter(IPlayerView view) => _view = view;

        public void Init(PlayerState state)
        {
            _state = state;
            SyncView();
        }

        public void SyncView()
        {
            float fill = _state.MaxHp > 0 ? (float)_state.Hp / _state.MaxHp : 0f;
            _view.UpdateHp(fill, $"{_state.Hp} / {_state.MaxHp}");
            _view.UpdateShield(_state.Shield, _state.Shield > 0);
            _view.UpdateStats(_state.Atk, _state.Def);
        }

        public void SyncShield()
        {
            _view.UpdateShield(_state.Shield, _state.Shield > 0);
        }

        public void NotifyDamage()
        {
            SyncView();
            _view.PlayDamageEffect();
        }

        public void NotifyHeal()
        {
            SyncView();
            _view.PlayHealEffect();
        }
    }
}
