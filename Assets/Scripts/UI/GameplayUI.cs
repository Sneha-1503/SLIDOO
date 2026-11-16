using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// In-game HUD: move counter, timer, pause button, preview hold-button.
    /// </summary>
    public class GameplayUI : MonoBehaviour
    {
        [Header("HUD")]
        [SerializeField] private TMP_Text  moveCountText;
        [SerializeField] private TMP_Text  timerText;
        [SerializeField] private Button    pauseButton;

        [Header("Preview")]
        [SerializeField] private PreviewHoldButton previewButton;   // custom hold-button
        [SerializeField] private RawImage          previewImage;
        [SerializeField] private GameObject        previewPanel;

        // ─────────────────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            GameManager.Instance.OnMoveCountChanged += RefreshMoves;
            GameManager.Instance.OnTimeChanged      += RefreshTimer;
            GameManager.Instance.OnLevelWon         += OnWin;
        }

        private void OnDisable()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnMoveCountChanged -= RefreshMoves;
            GameManager.Instance.OnTimeChanged      -= RefreshTimer;
            GameManager.Instance.OnLevelWon         -= OnWin;
        }

        private void Start()
        {
            pauseButton.onClick.AddListener(OnPause);
            previewPanel.SetActive(false);

            if (previewButton != null)
            {
                previewButton.OnHoldStart += () => previewPanel.SetActive(true);
                previewButton.OnHoldEnd   += () => previewPanel.SetActive(false);
            }
        }

        // ── Public ───────────────────────────────────────────────────────────────

        public void SetPreviewTexture(Texture2D tex) => previewImage.texture = tex;

        public void ResetUI()
        {
            RefreshMoves(0);
            RefreshTimer(0f);
            previewPanel.SetActive(false);
        }

        // ── Callbacks ────────────────────────────────────────────────────────────

        private void RefreshMoves(int moves) =>
            moveCountText.text = $"Moves: {moves}";

        private void RefreshTimer(float time)
        {
            int m = (int)(time / 60), s = (int)(time % 60);
            timerText.text = $"{m:D2}:{s:D2}";
        }

        private void OnPause()
        {
            AudioManager.Instance.PlayButtonClick();
            GameManager.Instance.PauseGame();
            UIManager.Instance.ShowPausePopup();
        }

        private void OnWin(int stars, int moves, float time, bool newMode) =>
            UIManager.Instance.ShowWinPopup();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    /// <summary>Button that fires hold-start and hold-end events.</summary>
    public class PreviewHoldButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public event System.Action OnHoldStart;
        public event System.Action OnHoldEnd;

        public void OnPointerDown(PointerEventData e) => OnHoldStart?.Invoke();
        public void OnPointerUp  (PointerEventData e) => OnHoldEnd  ?.Invoke();
        public void OnPointerExit(PointerEventData e) => OnHoldEnd  ?.Invoke();
    }
}
