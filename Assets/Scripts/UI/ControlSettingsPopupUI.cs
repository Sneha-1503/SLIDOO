using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Control Settings popup: lets player pick Swipe / Tap / Drag.
    /// (Tap and Drag fully wired in a future sprint; Swipe is active now.)
    /// </summary>
    public class ControlSettingsPopupUI : MonoBehaviour
    {
        [SerializeField] private Button swipeButton;
        [SerializeField] private Button tapButton;
        [SerializeField] private Button dragButton;
        [SerializeField] private Button closeButton;

        [Header("Selection highlight colours")]
        [SerializeField] private Color selectedColor   = new Color(0.2f, 0.7f, 0.2f, 1f);
        [SerializeField] private Color unselectedColor = Color.white;

        private Image _swipeBg, _tapBg, _dragBg;

        private void Awake()
        {
            _swipeBg = swipeButton.GetComponent<Image>();
            _tapBg   = tapButton  .GetComponent<Image>();
            _dragBg  = dragButton .GetComponent<Image>();
        }

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
            var ct = (ControlType)SaveManager.Instance.Data.controlType;
            if (_swipeBg) _swipeBg.color = ct == ControlType.Swipe ? selectedColor : unselectedColor;
            if (_tapBg)   _tapBg  .color = ct == ControlType.Tap   ? selectedColor : unselectedColor;
            if (_dragBg)  _dragBg .color = ct == ControlType.Drag  ? selectedColor : unselectedColor;
        }

        private void OnClose()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HideControlSettings();
        }
    }
}
