using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// One level button on the Level Select screen.
    /// Shows level number + stars if completed; greyed out if locked.
    /// </summary>
    [ExecuteAlways]
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

        private static TMP_FontAsset numberFont;
        private static Material numberMaterial;

        private void OnEnable() => ApplyTileStyle();

        public void ApplyTileStyle()
        {
            if (levelNumberText == null) return;
            if (numberFont == null)
            {
                numberFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");
                if (numberFont != null) numberFont.TryAddCharacters("0123456789");
            }
            if (numberMaterial == null) numberMaterial = Resources.Load<Material>("Level Number Brown Outline");

            if (numberFont == null || numberFont.material == null) return;
            levelNumberText.font = numberFont;
            levelNumberText.fontSharedMaterial = numberMaterial != null && numberMaterial.mainTexture == numberFont.atlasTexture
                ? numberMaterial : numberFont.material;
            levelNumberText.overflowMode = TextOverflowModes.Overflow;
            levelNumberText.color = Color.white;
            levelNumberText.fontStyle = FontStyles.Normal;
            levelNumberText.alignment = TextAlignmentOptions.Center;
            levelNumberText.fontSize = 112;
            levelNumberText.enableAutoSizing = true;
            levelNumberText.fontSizeMin = 24;
            levelNumberText.fontSizeMax = 112;
            Place(levelNumberText.rectTransform, .12f, .49f, .88f, .92f);

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] == null) continue;
                float left = .16f + i * .235f;
                Place(starImages[i].rectTransform, left, .21f, left + .21f, .42f);
                starImages[i].preserveAspect = true;
            }
            if (lockIcon != null)
                Place(lockIcon.rectTransform, .36f, .17f, .64f, .45f);
        }

        private static void Place(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = new Vector2(left, bottom);
            rect.anchorMax = new Vector2(right, top);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public void Setup(int levelIndex, GameMode mode, LevelSave save)
        {
            var hitArea = button.GetComponent<Image>();
            if (hitArea == null)
            {
                hitArea = button.gameObject.AddComponent<Image>();
                hitArea.color = Color.clear;
            }
            hitArea.raycastTarget = true;
            button.targetGraphic = buttonBg != null ? buttonBg : hitArea;

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
                {
                    starImages[s].gameObject.SetActive(!locked || lockIcon == null);
                    starImages[s].color = s < save.stars ? starFilledColor : starEmptyColor;
                }
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
