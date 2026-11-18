using UnityEngine;
using UnityEngine.UI;

namespace Slidoo
{
    [ExecuteAlways]
    public class PuzzleBoardLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform frame;
        [SerializeField] private RectTransform board;
        [SerializeField, Range(.5f, .95f)] private float boardFraction = .79f;
        [SerializeField, Min(0)] private float outerPadding = 12f;
        [SerializeField] private Vector2 boardOffset;
        [Header("Available screen area")]
        [SerializeField, Range(0, .4f)] private float topReservedFraction = .25f;
        [SerializeField, Range(0, .4f)] private float bottomReservedFraction = .12f;
        [SerializeField, Range(0, .15f)] private float sideMarginFraction = .025f;

        private void OnEnable() => Refresh();
        private void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (frame == null) frame = transform.Find("BoardFrame") as RectTransform;
            if (board == null) board = transform.Find("BoardContainer") as RectTransform;
            if (frame == null || board == null) return;
            // Reserve a proportion of the screen rather than fixed canvas units.
            // Fixed offsets left very little board height on shorter devices.
            var areaRect = (RectTransform)transform;
            areaRect.anchorMin = new Vector2(sideMarginFraction, bottomReservedFraction);
            areaRect.anchorMax = new Vector2(1 - sideMarginFraction, 1 - topReservedFraction);
            areaRect.offsetMin = Vector2.zero;
            areaRect.offsetMax = Vector2.zero;
            var area = areaRect.rect;
            float size = Mathf.Max(0, Mathf.Min(area.width, area.height) - outerPadding * 2);
            SetSquare(frame, size, Vector2.zero);
            SetSquare(board, size * boardFraction, boardOffset * size);
            var image = frame.GetComponent<Image>();
            if (image != null) image.preserveAspect = true;
        }

        private static void SetSquare(RectTransform rect, float size, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = offset;
        }
    }
}
