using UnityEngine;

namespace OUD.Unity.Common
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioClip _bgmClip;

        [Header("SFX")]
        [SerializeField] private AudioClip _diceRollClip;
        [SerializeField] private AudioClip _playerAttackClip;
        [SerializeField] private AudioClip _monsterAttackClip;
        [SerializeField] private AudioClip _shieldClip;
        [SerializeField] private AudioClip _victoryClip;
        [SerializeField] private AudioClip _defeatClip;
        [SerializeField] private AudioClip _coinClip;
        [SerializeField] private AudioClip _mapClip;
        [SerializeField] private AudioClip _levelUpClip;

        [Header("Volume")]
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 1.0f;

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;
            _bgmSource.volume = _bgmVolume;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.volume = _sfxVolume;
        }

        public void PlayBGM()
        {
            if (_bgmClip == null) return;
            if (_bgmSource.clip == _bgmClip && _bgmSource.isPlaying) return;
            _bgmSource.clip = _bgmClip;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            _bgmSource.Stop();
        }

        public void PlayDiceRoll()      => PlaySFX(_diceRollClip);
        public void PlayPlayerAttack()  => PlaySFX(_playerAttackClip);
        public void PlayMonsterAttack() => PlaySFX(_monsterAttackClip);
        public void PlayShield()        => PlaySFX(_shieldClip);
        public void PlayVictory()       => PlaySFX(_victoryClip);
        public void PlayDefeat()        => PlaySFX(_defeatClip);
        public void PlayCoin()          => PlaySFX(_coinClip);
        public void PlayMapClick()      => PlaySFX(_mapClip);
        public void PlayLevelUp()       => PlaySFX(_levelUpClip);

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                if (_bgmSource != null) _bgmSource.volume = _bgmVolume;
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }
    }
}
