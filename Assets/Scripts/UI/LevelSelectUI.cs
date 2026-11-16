using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Level Select panel: 30 buttons for one mode.
    /// Call ShowForMode(mode) to populate.
    /// HUD banner swaps to mode-specific sprite automatically.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        public static LevelSelectUI Instance { get; private set; }

        // Mode-specific HUD banner images (assign in Inspector or via SceneBuilder)
        // Index 0=Easy, 1=Medium, 2=Hard, 3=Master
        [SerializeField] private Image[]    modeHudImages;   // length 4, one per mode
        [SerializeField] private TMP_Text   modeTitleText;   // fallback text (hidden when image shown)
        [SerializeField] private TMP_Text   levelCountText;  // "Level 12 / 30" display

        [SerializeField] private LevelButtonUI[] levelButtons;    // 30 buttons pre-placed
        [SerializeField] private Button          backButton;
        [SerializeField] private Button          controlsButton;

        private GameMode _currentMode;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            backButton    .onClick.AddListener(OnBack);
            controlsButton.onClick.AddListener(OnControls);
        }

        public void ShowForMode(GameMode mode)
        {
            _currentMode = mode;
            UIManager.Instance.ShowScreen(UIScreen.LevelSelect);
            RefreshButtons();
        }

        private void OnEnable() => RefreshButtons();

        private void RefreshButtons()
        {
            var modeSave = SaveManager.Instance.Data.modes[(int)_currentMode];

            // ── HUD banner swap ───────────────────────────────────────────
            if (modeHudImages != null)
            {
                for (int m = 0; m < modeHudImages.Length; m++)
                    if (modeHudImages[m] != null)
                        modeHudImages[m].gameObject.SetActive(m == (int)_currentMode);
            }

            // ── Fallback text title (hidden when images exist) ────────────
            if (modeTitleText != null)
            {
                bool hasImage = modeHudImages != null && (int)_currentMode < modeHudImages.Length
                                && modeHudImages[(int)_currentMode] != null;
                modeTitleText.gameObject.SetActive(!hasImage);
                modeTitleText.text = ModeConfig.ModeNames[(int)_currentMode];
            }

            // ── Level count text: "X / 30 Completed" ─────────────────────
            if (levelCountText != null)
            {
                int completed = 0;
                for (int i = 0; i < ModeConfig.LevelsPerMode; i++)
                    if (modeSave.levels[i].stars > 0) completed++;
                levelCountText.text = $"{completed} / {ModeConfig.LevelsPerMode} Completed";
            }

            // ── Level buttons ─────────────────────────────────────────────
            for (int i = 0; i < ModeConfig.LevelsPerMode; i++)
            {
                if (i < levelButtons.Length)
                    levelButtons[i].Setup(i, _currentMode, modeSave.levels[i]);
            }
        }

        private void OnBack()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.ShowScreen(UIScreen.ModeSelect);
        }

        private void OnControls()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.ShowControlSettings();
        }
    }
}
