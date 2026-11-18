using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Splash screen – shows logo and auto-advances to Main Menu.
    /// Works independently (does NOT require other managers at start).
    /// </summary>
    public class SplashScreenUI : MonoBehaviour
    {
        [SerializeField] private float duration = 5f;
        [SerializeField] private Image loadingFill;
        [SerializeField] private Color trackColor = new Color(.3f, .13f, .04f, 1);
        [SerializeField] private Color fillColor = new Color(.5f, .85f, .12f, 1);
        [SerializeField] private TMP_Text loadingPercent;
        [SerializeField, Min(0)] private float completedHoldDuration = 1f;
        [SerializeField, HideInInspector] private bool timingUpdated;
        private float _timer;

        public void EnsureLoadingBar()
        {
            if (loadingFill != null)
            {
                EnsurePercentage();
                return;
            }
            var track = new GameObject("LoadingBar", typeof(RectTransform), typeof(Image));
            track.transform.SetParent(transform, false);
            var rect = (RectTransform)track.transform;
            rect.anchorMin = new Vector2(.2f, .12f);
            rect.anchorMax = new Vector2(.8f, .12f);
            rect.sizeDelta = new Vector2(0, 24);
            track.GetComponent<Image>().color = trackColor;
            track.GetComponent<Image>().raycastTarget = false;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(track.transform, false);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4, 4);
            fillRect.offsetMax = new Vector2(-4, -4);
            loadingFill = fill.GetComponent<Image>();
            loadingFill.color = fillColor;
            loadingFill.raycastTarget = false;
            EnsurePercentage();
        }
        private void EnsurePercentage()
        {
            if (loadingPercent != null) return;
            var label = new GameObject("LoadingPercent", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(loadingFill.transform.parent, false);
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0, 32);
            rect.sizeDelta = new Vector2(220, 55);
            loadingPercent = label.GetComponent<TextMeshProUGUI>();
            loadingPercent.text = "0%";
            loadingPercent.fontSize = 36;
            loadingPercent.alignment = TextAlignmentOptions.Center;
            loadingPercent.color = Color.white;
            loadingPercent.raycastTarget = false;
        }

        private bool  _done;

        private void OnEnable() => BeginLoading();

        public void BeginLoading()
        {
            EnsureLoadingBar();
            loadingFill.rectTransform.localScale = new Vector3(0, 1, 1);
            loadingFill.rectTransform.pivot = new Vector2(0, .5f);
            loadingPercent.text = "0%";
            _timer = 0f;
            _done  = false;
        }

        private void Update()
        {
            if (_done) return;
            _timer += Time.unscaledDeltaTime;
            loadingFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(_timer / Mathf.Max(.1f, duration)), 1, 1);
            loadingPercent.text = $"{Mathf.FloorToInt(Mathf.Clamp01(_timer / Mathf.Max(.1f, duration)) * 100)}%";
            if (_timer >= Mathf.Max(.1f, duration) + completedHoldDuration)
            {
                _done = true;
                Advance();
            }
        }

        private void Advance()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowScreen(UIScreen.MainMenu);
            else
                Debug.LogError("[SplashScreenUI] UIManager not found in scene! " +
                               "Please run Slidoo → Build Scene from the menu bar.");
        }
    }
}
