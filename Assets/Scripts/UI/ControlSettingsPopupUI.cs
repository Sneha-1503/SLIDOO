using UnityEngine;
using UnityEngine.UI;


namespace Slidoo
{
    /// <summary>
    /// Control Settings popup: lets player pick Swipe / Tap / Drag.
    /// Selection is saved and used by puzzle tiles.
    /// </summary>
    public class ControlSettingsPopupUI : MonoBehaviour
    {
        [SerializeField] private Button swipeButton;
        [SerializeField] private Button tapButton;
        [SerializeField] private Button dragButton;
        [SerializeField] private Button closeButton;

        [Header("Editable control indicators")]
        [SerializeField] private Image[] indicatorCircles = new Image[3];
        [SerializeField] private GameObject[] indicatorTicks = new GameObject[3];
        [SerializeField] private Sprite circleSprite;
        [SerializeField] private Color selectedColor = new Color(.2f, .7f, .2f, 1f);
        [SerializeField] private Color unselectedColor = new Color(.38f, .17f, .07f, 1f);
        [SerializeField] private Color tickColor = Color.white;

        private void Awake() => EnsureIndicators();

        // Only create missing objects. Existing transforms stay editable in the scene.
        public void EnsureIndicators()
        {
#if UNITY_EDITOR
            if (circleSprite == null)
                circleSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
#endif
            var options = new[] { swipeButton, tapButton, dragButton };
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] == null) continue;
                if (indicatorCircles[i] == null)
                {
                    var existing = options[i].transform.Find("SelectionTick");
                    if (existing != null) indicatorCircles[i] = existing.GetComponent<Image>();
                    else
                    {
                        var obj = new GameObject("SelectionTick", typeof(RectTransform), typeof(Image));
                        obj.transform.SetParent(options[i].transform, false);
                        var rect = (RectTransform)obj.transform;
                        rect.anchorMin = rect.anchorMax = new Vector2(1, .5f);
                        rect.sizeDelta = new Vector2(46, 46);
                        rect.anchoredPosition = new Vector2(-35, 0);
                        indicatorCircles[i] = obj.GetComponent<Image>();
                    }
                }
                if (indicatorTicks[i] == null)
                {
                    var existing = indicatorCircles[i].transform.Find("Mark");
                    if (existing != null) indicatorTicks[i] = existing.gameObject;
                    else
                    {
                        var mark = new GameObject("Mark", typeof(RectTransform));
                        var rect = (RectTransform)mark.transform;
                        rect.SetParent(indicatorCircles[i].transform, false);
                        rect.anchorMin = Vector2.zero;
                        rect.anchorMax = Vector2.one;
                        rect.sizeDelta = Vector2.zero;
                        AddTickStroke(rect, new Vector2(-10, -2), new Vector2(8, 20), 40);
                        AddTickStroke(rect, new Vector2(5, 2), new Vector2(8, 32), -40);
                        indicatorTicks[i] = mark;
                    }
                }
            }
            RefreshHighlight();
        }

        private static void AddTickStroke(RectTransform parent, Vector2 position, Vector2 size, float angle)
        {
            var obj = new GameObject("Stroke", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            obj.GetComponent<Image>().raycastTarget = false;
        }

#if UNITY_EDITOR
        [ContextMenu("Create Editable Control Indicators")]
        private void CreateEditableIndicators()
        {
            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(gameObject, "Create control indicators");
            if (circleSprite == null)
                circleSprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            EnsureIndicators();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        [UnityEditor.InitializeOnLoadMethod]
        private static void InitializeOpenPopups()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;
                foreach (var popup in Object.FindObjectsByType<ControlSettingsPopupUI>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (popup.gameObject.scene.IsValid() && popup.circleSprite == null)
                        popup.CreateEditableIndicators();
            };
        }

        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) RefreshHighlight();
            };
        }
#endif

        private void OnEnable() => RefreshHighlight();

        private void Start()
        {
            swipeButton.onClick.AddListener(() => SelectControl(ControlType.Swipe));
            tapButton  .onClick.AddListener(() => SelectControl(ControlType.Tap));
            dragButton .onClick.AddListener(() => SelectControl(ControlType.Drag));
            closeButton.onClick.AddListener(OnClose);
        }

        private void SelectControl(ControlType type)
        {
            AudioManager.Instance.PlayButtonClick();
            SaveManager.Instance.Data.controlType = (int)type;
            SaveManager.Instance.SaveData();
            RefreshHighlight();
        }

        private void RefreshHighlight()
        {
            int selected = SaveManager.Instance != null && SaveManager.Instance.Data != null
                ? SaveManager.Instance.Data.controlType : 0;
            for (int i = 0; i < indicatorCircles.Length; i++)
            {
                if (indicatorCircles[i] == null || indicatorTicks[i] == null) continue;
                indicatorCircles[i].sprite = circleSprite;
                indicatorCircles[i].preserveAspect = true;
                indicatorCircles[i].raycastTarget = false;
                indicatorCircles[i].color = i == selected ? selectedColor : unselectedColor;
                indicatorTicks[i].SetActive(i == selected);
                foreach (var stroke in indicatorTicks[i].GetComponentsInChildren<Image>(true))
                    stroke.color = tickColor;
            }
        }

        private void OnClose()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HideControlSettings();
        }
    }
}
