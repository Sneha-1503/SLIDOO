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
        private ScrollRect levelScroll;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            levelScroll = GetComponentInChildren<ScrollRect>(true);
            if (levelScroll != null)
            {
                var viewport = levelScroll.viewport != null
                    ? levelScroll.viewport : (RectTransform)levelScroll.transform;
                var hitArea = viewport.GetComponent<Image>();
                if (hitArea == null)
                {
                    hitArea = viewport.gameObject.AddComponent<Image>();
                    hitArea.color = Color.clear;
                }
                hitArea.raycastTarget = true;
                levelScroll.horizontal = false;
                levelScroll.vertical = true;
                levelScroll.inertia = true;
                levelScroll.decelerationRate = 0.135f;
                levelScroll.elasticity = 0.12f;
                levelScroll.scrollSensitivity = 45f;
                levelScroll.movementType = ScrollRect.MovementType.Elastic;
            }
        }

        private void Start()
        {
            if (backButton     != null) backButton    .onClick.AddListener(OnBack);
            if (controlsButton != null) controlsButton.onClick.AddListener(OnControls);
        }

        public void ShowForMode(GameMode mode)
        {
            _currentMode = mode;
            UIManager.Instance.ShowScreen(UIScreen.LevelSelect);
            RefreshButtons();
            if (levelScroll != null)
            {
                Canvas.ForceUpdateCanvases();
                levelScroll.StopMovement();
                levelScroll.verticalNormalizedPosition = 1f;
            }
        }

        private void OnEnable() => RefreshButtons();

        private void RefreshButtons()
        {
            // Guard — SaveManager may not be ready on first OnEnable
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;

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
                int highest = System.Array.FindLastIndex(modeSave.levels, level => level.unlocked);
                levelCountText.text = $"Level {Mathf.Max(1, highest + 1)} / {ModeConfig.LevelsPerMode}";
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
