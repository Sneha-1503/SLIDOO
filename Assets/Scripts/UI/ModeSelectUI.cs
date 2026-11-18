using UnityEngine;
using UnityEngine.UI;

namespace Slidoo
{
    /// <summary>
    /// Mode Select panel. Instantiates or updates four ModeButtonUI components.
    /// Assign the four ModeButtonUI references in the Inspector.
    /// </summary>
    [ExecuteAlways]
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] private ModeButtonUI[] modeButtons;   // length 4, Easy→Master
        [SerializeField] private Button backButton;

        [Header("Responsive layout")]
        [SerializeField, Range(.1f, .4f)] private float topSpace = .22f;
        [SerializeField, Range(0, .15f)] private float bottomSpace = .04f;
        [SerializeField, Range(.5f, 1f)] private float widthFraction = .9f;
        [SerializeField, Range(0, .05f)] private float gapFraction = .012f;
        [SerializeField] private Vector2 cardReferenceSize = new Vector2(900, 355);
        [SerializeField] private Vector2 headerReferenceSize = new Vector2(1000, 300);

        private void OnEnable()
        {
            ApplyLayout();
            if (Application.isPlaying) RefreshButtons();
        }

        private void LateUpdate() => ApplyLayout();

        [SerializeField] private RectTransform listViewport;
        [SerializeField] private RectTransform listContent;
        [SerializeField] private RectTransform titleImage;

        private void ApplyLayout()
        {
            if (modeButtons == null || modeButtons.Length == 0) return;
            if (listViewport == null)
            {
                titleImage = transform.Find("Header") as RectTransform;
                var viewport = new GameObject("ModeList", typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
                viewport.transform.SetParent(transform, false);
                listViewport = (RectTransform)viewport.transform;
                var hit = viewport.GetComponent<Image>();
                hit.color = Color.clear;
                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                listContent = (RectTransform)content.transform;
                listContent.anchorMin = listContent.anchorMax = new Vector2(.5f, 1);
                listContent.pivot = new Vector2(.5f, 1);
                if (titleImage != null) titleImage.SetParent(listContent, false);
                foreach (var button in modeButtons)
                    if (button != null) button.transform.SetParent(listContent, false);
                var scroll = viewport.GetComponent<ScrollRect>();
                scroll.viewport = listViewport;
                scroll.content = listContent;
                scroll.horizontal = false;
                scroll.scrollSensitivity = 45;
                scroll.movementType = ScrollRect.MovementType.Clamped;
            }
            listViewport.anchorMin = new Vector2(0, bottomSpace);
            listViewport.anchorMax = new Vector2(1, 1 - topSpace);
            listViewport.offsetMin = listViewport.offsetMax = Vector2.zero;
            float referenceWidth = Mathf.Max(headerReferenceSize.x, cardReferenceSize.x);
            float scale = Mathf.Max(.01f, listViewport.rect.width * widthFraction / referenceWidth);
            float gap = referenceWidth * gapFraction;
            float height = headerReferenceSize.y + modeButtons.Length * (cardReferenceSize.y + gap);
            listContent.sizeDelta = new Vector2(referenceWidth, height);
            listContent.localScale = new Vector3(scale, scale, 1);
            float y = 0;
            if (titleImage != null)
            {
                Position(titleImage, headerReferenceSize, y);
                y -= headerReferenceSize.y + gap;
            }
            foreach (var button in modeButtons)
            {
                if (button == null) continue;
                Position((RectTransform)button.transform, cardReferenceSize, y);
                y -= cardReferenceSize.y + gap;
            }
        }

        private static void Position(RectTransform rect, Vector2 size, float top)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = new Vector2(0, top - size.y * .5f);
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            backButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayButtonClick();
                UIManager.Instance.ShowScreen(UIScreen.MainMenu);
            });
        }

        private void RefreshButtons()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
            var save = SaveManager.Instance.Data;
            for (int i = 0; i < 4; i++)
            {
                GameMode mode = (GameMode)i;
                var modeSave  = save.modes[i];
                modeButtons[i].Setup(mode, modeSave);
            }
        }
    }
}
