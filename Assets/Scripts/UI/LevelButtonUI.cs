using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// One level button on the Level Select screen.
    /// Shows level number + stars if completed; greyed out if locked.
    /// </summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [SerializeField] private Button    button;
        [SerializeField] private TMP_Text  levelNumberText;
        [SerializeField] private Image[]   starImages;      // 3 star icons
        [SerializeField] private Color     starFilledColor  = Color.yellow;
        [SerializeField] private Color     starEmptyColor   = new Color(0.3f, 0.3f, 0.3f, 1f);
        [SerializeField] private Image     lockIcon;
        [SerializeField] private Image     buttonBg;
        [SerializeField] private Color     unlockedColor    = Color.white;
        [SerializeField] private Color     lockedColor      = new Color(0.5f, 0.5f, 0.5f, 1f);

        private int      _levelIndex;
        private GameMode _mode;

        public void Setup(int levelIndex, GameMode mode, LevelSave save)
        {
            _levelIndex = levelIndex;
            _mode       = mode;

            bool locked = !save.unlocked;
            button.interactable = !locked;
            if (buttonBg != null) buttonBg.color = locked ? lockedColor : unlockedColor;
            if (lockIcon != null) lockIcon.gameObject.SetActive(locked);

            levelNumberText.text = (levelIndex + 1).ToString();

            // Stars
            for (int s = 0; s < starImages.Length; s++)
            {
                if (starImages[s] != null)
                    starImages[s].color = s < save.stars ? starFilledColor : starEmptyColor;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            AudioManager.Instance.PlayButtonClick();
            LevelController.Instance.LoadLevel(_mode, _levelIndex);
        }
    }
}
