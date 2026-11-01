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
        private readonly Func<MonsterTier, IEnemyEntryView> _entryFactory;
        private readonly MonsterSpriteMap        _spriteMap;
        private List<MonsterInstance>            _enemies;
        private List<IEnemyEntryView>            _entryViews = new();

        public event Action<int> OnEnemyClicked;

        public EnemyPresenter(Func<MonsterTier, IEnemyEntryView> entryFactory, MonsterSpriteMap spriteMap)
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
                var m = enemies[i];
                var view = _entryFactory(m.Data.Tier);
                Sprite sprite = _spriteMap?.GetSprite(m.Data.Id);
                float fill = m.Data.MaxHp > 0 ? (float)m.Hp / m.Data.MaxHp : 0f;
                view.Setup(m.Data.Name, sprite, fill, $"{m.Hp} / {m.Data.MaxHp}");
                view.UpdateAtk(m.Atk);
                view.UpdateDef(m.Data.ShieldValue);
                view.UpdateIntent(m.GetCurrentIntent(), m.GetIntentValue());
                view.SetRageActive(m.IsEnraged);
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
            _entryViews[index].UpdateAtk(m.Atk);
            _entryViews[index].UpdateDef(m.Data.ShieldValue);
            _entryViews[index].UpdateIntent(m.GetCurrentIntent(), m.GetIntentValue());
            _entryViews[index].SetRageActive(m.IsEnraged);

            // 사망 시 GameObject 비활성화로 화면에서 제거. 이미 비활성이면 스킵(idempotent).
            if (m.IsDead && _entryViews[index] is MonoBehaviour mb && mb != null && mb.gameObject.activeSelf)
                HandleDeath(index);
        }

        public void ShowAction(int enemyIndex, IntentType intent, int value)
        {
            if (enemyIndex < 0 || enemyIndex >= _entryViews.Count) return;
            _entryViews[enemyIndex].UpdateIntent(intent, value);
        }

        public void HandleDeath(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= _entryViews.Count) return;
            var view = _entryViews[enemyIndex];
            view.PlayDeathEffect();
            // 사망 시 GameObject 비활성화 — HorizontalLayoutGroup이 자동으로 살아있는 적만 정렬.
            // _entryViews 리스트의 인덱스는 유지(클릭 콜백/참조 보존).
            if (view is MonoBehaviour mb && mb != null) mb.gameObject.SetActive(false);
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

        public void ShowTargetBadge(int enemyIndex, string skillName, SkillCategory category, bool isAoe)
        {
            if (enemyIndex < 0 || enemyIndex >= _entryViews.Count) return;
            _entryViews[enemyIndex].ShowTargetBadge(skillName, category, isAoe);
        }

        public void ClearTargetBadge(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= _entryViews.Count) return;
            _entryViews[enemyIndex].ClearTargetBadge();
        }

        public void ClearAllTargetBadges()
        {
            for (int i = 0; i < _entryViews.Count; i++)
                _entryViews[i].ClearTargetBadge();
        }

        public void AddEntry(MonsterInstance monster, int index)
        {
            var view = _entryFactory(monster.Data.Tier);
            Sprite sprite = _spriteMap?.GetSprite(monster.Data.Id);
            float fill = monster.Data.MaxHp > 0 ? (float)monster.Hp / monster.Data.MaxHp : 0f;
            view.Setup(monster.Data.Name, sprite, fill, $"{monster.Hp} / {monster.Data.MaxHp}");
            view.UpdateAtk(monster.Atk);
            view.UpdateDef(monster.Data.ShieldValue);
            view.UpdateIntent(monster.GetCurrentIntent(), monster.GetIntentValue());
            _entryViews.Add(view);

            int idx = index;
            view.OnClicked += () => NotifyEnemyClicked(idx);
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
