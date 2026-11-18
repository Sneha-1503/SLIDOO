using System.Collections;
using UnityEngine;

namespace Slidoo
{
    public enum UIScreen { Splash, MainMenu, ModeSelect, LevelSelect, Gameplay }

    /// <summary>
    /// Single-scene screen manager. Activates/deactivates panels.
    /// All references assigned by SlidooSceneBuilder or manually in Inspector.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("── Screens ──────────────────────────")]
        [SerializeField] private GameObject splashScreen;
        [SerializeField] private GameObject mainMenuScreen;
        [SerializeField] private GameObject modeSelectScreen;
        [SerializeField] private GameObject levelSelectScreen;
        [SerializeField] private GameObject gameplayScreen;

        [Header("── Popups ──────────────────────────")]
        [SerializeField] private GameObject winPopup;
        [SerializeField] private GameObject pausePopup;
        [SerializeField] private GameObject settingsPopup;
        [SerializeField] private GameObject controlSettingsPopup;
        [SerializeField] private GameObject quitPopup;

        [Header("── Splash ──────────────────────────")]
        [SerializeField] private float splashDuration = 2f;

        public UIScreen CurrentScreen { get; private set; }

        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            ValidateReferences();
        }

        private void Start()
        {
            if (splashScreen == null)
            {
                var splash = FindFirstObjectByType<SplashScreenUI>(FindObjectsInactive.Include);
                if (splash != null) splashScreen = splash.gameObject;
            }
            ShowScreen(UIScreen.Splash);
            var loading = splashScreen != null ? splashScreen.GetComponent<SplashScreenUI>() : null;
            if (loading != null)
            {
                loading.enabled = true;
                loading.BeginLoading();
            }
            else
                StartCoroutine(SplashTimer());
        }

        private IEnumerator SplashTimer()
        {
            yield return new WaitForSecondsRealtime(splashDuration);

            if (mainMenuScreen != null)
                ShowScreen(UIScreen.MainMenu);
            else
                Debug.LogError("[UIManager] mainMenuScreen is NULL! " +
                               "Run 'Slidoo → Build Scene' from the menu bar first.");
        }

        // ── Screen switching ─────────────────────────────────────────────────

        public void ShowLevelSelect(GameMode mode)
        {
            levelSelectScreen.GetComponent<LevelSelectUI>().ShowForMode(mode);
        }

        public void ShowScreen(UIScreen screen)
        {
            SetActive(splashScreen,       screen == UIScreen.Splash);
            SetActive(mainMenuScreen,     screen == UIScreen.MainMenu);
            SetActive(modeSelectScreen,   screen == UIScreen.ModeSelect);
            SetActive(levelSelectScreen,  screen == UIScreen.LevelSelect);
            SetActive(gameplayScreen,     screen == UIScreen.Gameplay);
            CurrentScreen = screen;
            HideAllPopups();
        }

        // ── Popup helpers ────────────────────────────────────────────────────

        public void ShowWinPopup(int stars, int moves, float time, bool newModeUnlocked)
        {
            SetActive(winPopup, true);
            if (winPopup != null)
                winPopup.GetComponent<WinPopupUI>().Populate(stars, moves, time, newModeUnlocked);
        }
        public void ShowPausePopup()      => SetActive(pausePopup,          true);
        public void ShowSettingsPopup()   => SetActive(settingsPopup,       true);
        public void ShowControlSettings() => SetActive(controlSettingsPopup,true);
        public void ShowQuitPopup()       => SetActive(quitPopup,           true);

        public void HideWinPopup()        => SetActive(winPopup,            false);
        public void HidePausePopup()      => SetActive(pausePopup,          false);
        public void HideSettingsPopup()   => SetActive(settingsPopup,       false);
        public void HideControlSettings() => SetActive(controlSettingsPopup,false);
        public void HideQuitPopup()       => SetActive(quitPopup,           false);

        public void HideAllPopups()
        {
            SetActive(winPopup,            false);
            SetActive(pausePopup,          false);
            SetActive(settingsPopup,       false);
            SetActive(controlSettingsPopup,false);
            SetActive(quitPopup,           false);
        }

        // ── Null-safe helper ─────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        // ── Reference validation (runs in Awake, logs missing refs) ──────────

        private void ValidateReferences()
        {
            Check(splashScreen,        "splashScreen");
            Check(mainMenuScreen,      "mainMenuScreen");
            Check(modeSelectScreen,    "modeSelectScreen");
            Check(levelSelectScreen,   "levelSelectScreen");
            Check(gameplayScreen,      "gameplayScreen");
            Check(winPopup,            "winPopup");
            Check(pausePopup,          "pausePopup");
            Check(settingsPopup,       "settingsPopup");
            Check(controlSettingsPopup,"controlSettingsPopup");
            Check(quitPopup,           "quitPopup");
        }

        private static void Check(GameObject go, string name)
        {
            if (go == null)
                Debug.LogWarning($"[UIManager] '{name}' is not assigned! " +
                                 "Run Slidoo → Build Scene to auto-wire all references.");
        }
    }
}
