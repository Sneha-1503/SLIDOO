using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// One mode button on the Mode Select screen.
    /// Unlocked: shows mode name + "X/30" progress.
    /// Locked: greyed out with lock icon + unlock hint.
    /// </summary>
    public class ModeButtonUI : MonoBehaviour
    {
        [Header("Unlocked state")]
        [SerializeField] private GameObject unlockedGroup;
        [SerializeField] private TMP_Text   modeNameText;
        [SerializeField] private TMP_Text   progressText;      // e.g. "12 / 30"
        [SerializeField] private TMP_Text   starsText;         // e.g. "★ 54 / 90"

        [Header("Locked state")]
        [SerializeField] private GameObject lockedGroup;
        [SerializeField] private TMP_Text   lockHintText;      // "Complete 15 Easy levels"

        [Header("Shared")]
        [SerializeField] private Button    button;
        [SerializeField] private Image     buttonBg;
        [SerializeField] private Color     unlockedColor = Color.white;
        [SerializeField] private Color     lockedColor   = new Color(0.5f, 0.5f, 0.5f, 1f);

        private GameMode _mode;

        public void Setup(GameMode mode, ModeSave save)
        {
            _mode = mode;

            bool locked = !save.modeUnlocked;
            unlockedGroup.SetActive(!locked);
            lockedGroup  .SetActive(locked);
            button.interactable = !locked;
            if (buttonBg != null)
                buttonBg.color = locked ? lockedColor : unlockedColor;

            if (!locked)
            {
                modeNameText.text = ModeConfig.ModeNames[(int)mode];
                int completed = save.CompletedCount();
                progressText.text = $"{completed} / {ModeConfig.LevelsPerMode}";
                starsText.text    = $"★ {save.TotalStars()} / {ModeConfig.LevelsPerMode * 3}";
            }
            else
            {
                int prevMode = (int)mode - 1;
                string prevName = ModeConfig.ModeNames[Mathf.Max(0, prevMode)];
                lockHintText.text = $"Complete {ModeConfig.UnlockRequirement} {prevName} levels to unlock";
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            AudioManager.Instance.PlayButtonClick();
            LevelSelectUI.Instance.ShowForMode(_mode);
        }
    }
}
