using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Win popup. Listens to GameManager.OnLevelWon.
    /// Shows stars, moves, time, and optional "New mode unlocked!" banner.
    /// </summary>
    public class WinPopupUI : MonoBehaviour
    {
        [Header("Star display")]
        [SerializeField] private Image[] starImages;       // 3 star images
        [SerializeField] private Sprite  starFilled;
        [SerializeField] private Sprite  starEmpty;

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
        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelWon += Populate;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnLevelWon -= Populate;
        }

        private void Start()
        {
            nextButton  .onClick.AddListener(OnNext);
            replayButton.onClick.AddListener(OnReplay);
            menuButton  .onClick.AddListener(OnMenu);
        }

        // ── Populate ─────────────────────────────────────────────────────────────

        private void Populate(int stars, int moves, float time, bool newModeUnlocked)
        {
            AudioManager.Instance.PlayWin();

            // Stars
            for (int i = 0; i < starImages.Length; i++)
                starImages[i].sprite = i < stars ? starFilled : starEmpty;

            // Stats
            movesText.text = $"{moves} Moves";
            timeText .text = FormatTime(time);

            // Best scores (read from save)
            var gm   = GameManager.Instance;
            var lvl  = SaveManager.Instance.Data.modes[(int)gm.CurrentMode].levels[gm.CurrentLevelIndex];
            if (bestMovesText != null) bestMovesText.text = $"Best: {lvl.bestMoves} moves";
            if (bestTimeText  != null) bestTimeText .text = $"Best: {FormatTime(lvl.bestTime)}";

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
