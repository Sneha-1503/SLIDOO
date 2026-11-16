using UnityEngine;

namespace Slidoo
{
    /// <summary>
    /// Central audio hub: SFX one-shots + looping background music.
    /// Assign clips in the Inspector. Call Play* methods from anywhere.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip tileSlideClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip unlockClip;
        [SerializeField] private AudioClip errorClip;        // tap on non-moveable tile

        [Header("Music")]
        [SerializeField] private AudioClip backgroundMusic;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ApplySettings();
            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
        }

        // ── Apply saved settings ─────────────────────────────────────────────────
        public void ApplySettings()
        {
            var d = SaveManager.Instance.Data;
            sfxSource.mute   = !d.soundEnabled;
            musicSource.mute = !d.musicEnabled;
        }

        // ── Setters (also persist immediately) ───────────────────────────────────
        public void SetSoundEnabled(bool on)
        {
            SaveManager.Instance.Data.soundEnabled = on;
            sfxSource.mute = !on;
            SaveManager.Instance.SaveData();
        }

        public void SetMusicEnabled(bool on)
        {
            SaveManager.Instance.Data.musicEnabled = on;
            musicSource.mute = !on;
            SaveManager.Instance.SaveData();
        }

        // ── Play helpers ─────────────────────────────────────────────────────────
        public void PlayTileSlide()   => Play(tileSlideClip);
        public void PlayWin()         => Play(winClip);
        public void PlayButtonClick() => Play(buttonClickClip);
        public void PlayUnlock()      => Play(unlockClip);
        public void PlayError()       => Play(errorClip);

        private void Play(AudioClip clip)
        {
            if (clip != null) sfxSource.PlayOneShot(clip);
        }
    }
}

