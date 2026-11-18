using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Win popup. Receives the result when UIManager opens it.
    /// Shows stars, moves, time, and optional "New mode unlocked!" banner.
    /// </summary>
    public class WinPopupUI : MonoBehaviour
    {
        [Header("Star display")]
        [SerializeField] private Image[] starImages;       // 3 star images
        [SerializeField] private Sprite  starFilled;
        [SerializeField] private Sprite  starEmpty;

        [SerializeField] private Color earnedStarColor = Color.white;
        [SerializeField] private Color emptyStarColor = new Color(.3f, .3f, .3f, 1f);

        [Header("Stats")]
        [SerializeField] private TMP_Text movesText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text bestMovesText;   // "Best: X moves"
        [SerializeField] private TMP_Text bestTimeText;    // "Best: MM:SS"

        [Header("Mode unlock banner")]
        [SerializeField] private GameObject unlockBanner;
        [SerializeField] private TMP_Text   unlockBannerText;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button replayButton;
        [SerializeField] private Button menuButton;

        // ─────────────────────────────────────────────────────────────────────────
        private void Start()
        {
            nextButton  .onClick.AddListener(OnNext);
            replayButton.onClick.AddListener(OnReplay);
            menuButton  .onClick.AddListener(OnMenu);
        }

        // ── Populate ─────────────────────────────────────────────────────────────

        public void Populate(int stars, int moves, float time, bool newModeUnlocked)
        {
            // Stars
            for (int i = 0; i < starImages.Length; i++)
            {
                var image = starImages[i];
                if (image == null) continue;
                bool earned = i < stars;
                var sprite = earned ? starFilled : starEmpty;
                if (sprite != null) image.sprite = sprite;
                image.color = earned ? earnedStarColor : emptyStarColor;
            }

            // Stats
            if (movesText != null) movesText.text = $"{moves} Moves";
            if (timeText != null) timeText.text = FormatTime(time);

            // Best scores (read from save)
            var gm   = GameManager.Instance;
            var lvl  = SaveManager.Instance.Data.modes[(int)gm.CurrentMode].levels[gm.CurrentLevelIndex];
            if (bestMovesText != null) bestMovesText.text = $"Best: {lvl.bestMoves} moves";
            if (bestTimeText  != null) bestTimeText .text = $"Best: {FormatTime(lvl.bestTime)}";

            AudioManager.Instance.PlayWin();

            // Mode unlock banner
            if (unlockBanner != null)
            {
                unlockBanner.SetActive(newModeUnlocked);
                if (newModeUnlocked && unlockBannerText != null)
                {
                    int nextModeIdx = (int)gm.CurrentMode + 1;
                    if (nextModeIdx < 4)
                    {
                        unlockBannerText.text =
                            $"🎉 {ModeConfig.ModeNames[nextModeIdx]} Mode Unlocked!";
                        AudioManager.Instance.PlayUnlock();
                    }
                }
            }
        }

        // ── Button handlers ──────────────────────────────────────────────────────

        private void OnNext()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HideWinPopup();

            var  gm        = GameManager.Instance;
            int  nextLevel = gm.CurrentLevelIndex + 1;
            var  modeSave  = SaveManager.Instance.Data.modes[(int)gm.CurrentMode];

            if (nextLevel < ModeConfig.LevelsPerMode && modeSave.levels[nextLevel].unlocked)
                LevelController.Instance.LoadLevel(gm.CurrentMode, nextLevel);
            else
                UIManager.Instance.ShowScreen(UIScreen.LevelSelect);
        }

        private void OnReplay()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HideWinPopup();
            LevelController.Instance.RestartCurrentLevel();
        }

        private void OnMenu()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.ShowScreen(UIScreen.LevelSelect);
        }

        private static string FormatTime(float t)
        {
            int m = (int)(t / 60), s = (int)(t % 60);
            return $"{m:D2}:{s:D2}";
        }
    }
}
