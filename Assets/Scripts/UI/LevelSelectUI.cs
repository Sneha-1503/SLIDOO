using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Level Select panel: 30 buttons for one mode.
    /// Call ShowForMode(mode) to populate.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        public static LevelSelectUI Instance { get; private set; }

        [SerializeField] private TMP_Text        modeTitleText;
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
            modeTitleText.text = ModeConfig.ModeNames[(int)_currentMode];
            var modeSave = SaveManager.Instance.Data.modes[(int)_currentMode];

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
