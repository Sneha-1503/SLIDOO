using System.Collections;
using UnityEngine;

namespace Slidoo
{
    public enum UIScreen { Splash, MainMenu, ModeSelect, LevelSelect, Gameplay }

    /// <summary>
    /// Manages which screen/popup is visible. All screens are children of one
    /// Canvas – we simply activate/deactivate the right panel.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screens")]
        [SerializeField] private GameObject splashScreen;
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject modeSelectScreen;
        [SerializeField] private GameObject levelSelectScreen;
        [SerializeField] private GameObject gameplayScreen;

        [Header("Popups")]
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private GameObject controlSettingsPopup;
        [SerializeField] private GameObject quitPopup;

        public UIScreen CurrentScreen { get; private set; }

        // ─────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            ShowScreen(UIScreen.Splash);
            StartCoroutine(SplashTimer());
        }

        private IEnumerator SplashTimer()
        {
            yield return new WaitForSeconds(2f);
            ShowScreen(UIScreen.MainMenu);
        }

        // ── Screen switching ─────────────────────────────────────────────────────

        public void ShowScreen(UIScreen screen)
        {
            splashScreen    .SetActive(screen == UIScreen.Splash);
            mainMenuScreen  .SetActive(screen == UIScreen.MainMenu);
            modeSelectScreen.SetActive(screen == UIScreen.ModeSelect);
            levelSelectScreen.SetActive(screen == UIScreen.LevelSelect);
            gameplayScreen  .SetActive(screen == UIScreen.Gameplay);
            CurrentScreen = screen;
            HideAllPopups();
        }

        // ── Popup helpers ────────────────────────────────────────────────────────

        public void ShowWinPopup()            => winPopup            .SetActive(true);
        public void ShowPausePopup()          => pausePopup          .SetActive(true);
        public void ShowSettingsPopup()       => settingsPopup       .SetActive(true);
        public void ShowControlSettings()     => controlSettingsPopup.SetActive(true);

        public void HideWinPopup()            => winPopup            .SetActive(false);
        public void HidePausePopup()          => pausePopup          .SetActive(false);
        public void HideSettingsPopup()       => settingsPopup       .SetActive(false);
        public void HideControlSettings()     => controlSettingsPopup.SetActive(false);

        public void ShowQuitPopup()           => quitPopup           .SetActive(true);
        public void HideQuitPopup()           => quitPopup           .SetActive(false);

        public void HideAllPopups()
        {
            winPopup            .SetActive(false);
            pausePopup          .SetActive(false);
            settingsPopup       .SetActive(false);
            controlSettingsPopup.SetActive(false);
            quitPopup           .SetActive(false);
        }
    }
}
