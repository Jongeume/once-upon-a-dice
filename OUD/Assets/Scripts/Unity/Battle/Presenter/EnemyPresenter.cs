using System;
using System.Collections.Generic;
using OUD.BattleEngine.Core;
using OUD.BattleEngine.Unit;
using OUD.Unity.Battle;
using UnityEngine;

namespace OUD.Unity.Battle.Presenter
{
    /// <summary>
    /// MonsterInstance 목록 → EnemyEntryView 관리.
    /// 동적 생성/제거, HP/Shield/Intent 갱신, 사망 처리.
    /// </summary>
    public class EnemyPresenter
    {
        private readonly Func<IEnemyEntryView>   _entryFactory;
        private readonly MonsterSpriteMap        _spriteMap;
        private List<MonsterInstance>            _enemies;
        private List<IEnemyEntryView>            _entryViews = new();

        public event Action<int> OnEnemyClicked;

        public EnemyPresenter(Func<IEnemyEntryView> entryFactory, MonsterSpriteMap spriteMap)
        {
            _entryFactory = entryFactory;
            _spriteMap    = spriteMap;
        }

        public void Init(List<MonsterInstance> enemies)
        {
            _enemies = enemies;
            // 기존 뷰 제거 후 재생성
            foreach (var v in _entryViews)
                if (v is MonoBehaviour mb) UnityEngine.Object.Destroy(mb.gameObject);
            _entryViews.Clear();

            for (int i = 0; i < enemies.Count; i++)
            {
                var view = _entryFactory();
                var m = enemies[i];
                Sprite sprite = _spriteMap?.GetSprite(m.Data.Id);
                float fill = m.Data.MaxHp > 0 ? (float)m.Hp / m.Data.MaxHp : 0f;
                view.Setup(m.Data.Name, sprite, fill, $"{m.Hp} / {m.Data.MaxHp}");
                view.UpdateIntent(m.GetCurrentIntent(), m.GetIntentValue());
                _entryViews.Add(view);

                int idx = i;
                view.OnClicked += () => NotifyEnemyClicked(idx);
            }
        }

        public void RefreshAll()
        {
            for (int i = 0; i < _enemies.Count; i++)
                RefreshEntry(i);
        }

        public void RefreshAllShields()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (i >= _entryViews.Count) break;
                _entryViews[i].UpdateShield(_enemies[i].Shield, _enemies[i].Shield > 0);
            }
        }

        public void RefreshEntry(int index)
        {
            if (index < 0 || index >= _entryViews.Count) return;
            var m    = _enemies[index];
            float fill = m.Data.MaxHp > 0 ? (float)m.Hp / m.Data.MaxHp : 0f;
            _entryViews[index].UpdateHp(fill, $"{m.Hp} / {m.Data.MaxHp}");
            _entryViews[index].UpdateShield(m.Shield, m.Shield > 0);
        }

        public void ShowAction(int enemyIndex, IntentType intent, int value)
        {
            if (enemyIndex < 0 || enemyIndex >= _entryViews.Count) return;
            _entryViews[enemyIndex].UpdateIntent(intent, value);
        }

        public void HandleDeath(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= _entryViews.Count) return;
            _entryViews[enemyIndex].PlayDeathEffect();
        }

        /// <summary>화면 C 타겟팅 활성화 여부 전파.</summary>
        public void SetTargetSelectable(bool selectable)
        {
            for (int i = 0; i < _entryViews.Count; i++)
            {
                bool alive = !_enemies[i].IsDead;
                _entryViews[i].SetTargetSelectable(selectable && alive);
            }
        }

        public void NotifyEnemyClicked(int index) => OnEnemyClicked?.Invoke(index);

        /// <summary>
        /// 현재 살아있는 entry view들의 Transform 배열.
        /// BattleLogPresenter의 데미지 팝업 위치 계산에 사용.
        /// EnemyView.Container의 자식을 직접 잡으면 Destroy 마킹된 이전 entry까지 포함되어
        /// MissingReferenceException 발생 — Init이 정리한 _entryViews 기반으로 잡는다.
        /// </summary>
        public Transform[] GetEntryTransforms()
        {
            var transforms = new Transform[_entryViews.Count];
            for (int i = 0; i < _entryViews.Count; i++)
            {
                if (_entryViews[i] is MonoBehaviour mb && mb != null)
                    transforms[i] = mb.transform;
            }
            return transforms;
        }
    }
}
