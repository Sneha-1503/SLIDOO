using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Slidoo
{
    /// <summary>
    /// One puzzle tile. Renders via RawImage with a UV rect crop.
    /// Detects swipe gestures and reports them to BoardManager.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class Tile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Config ───────────────────────────────────────────────────────────────
        private const float MIN_SWIPE_PX = 20f;   // minimum drag length in pixels

        // ── State ────────────────────────────────────────────────────────────────
        public int TileIndex { get; private set; }

        private RawImage _img;
        private Vector2  _pointerDown;

        // ─────────────────────────────────────────────────────────────────────────
        private void Awake() => _img = GetComponent<RawImage>();

        public void Initialize(int tileIndex, Texture2D texture, Rect uvRect)
        {
            TileIndex   = tileIndex;
            _img.texture = texture;
            _img.uvRect  = uvRect;
        }

        public Texture2D GetTexture() => _img.texture as Texture2D;

        // ── Gesture detection ────────────────────────────────────────────────────

        private ControlType gestureControl;
        private int? pointerId;
        private bool draggingTile;
        private bool movedPointer;
        private Vector2 dragStart;
        private Vector2 dragOrigin;
        private Vector2 dropPoint;
        private RectTransform tileRect;

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || pointerId.HasValue) return;
            pointerId = e.pointerId;
            _pointerDown = e.position;
            movedPointer = false;
            gestureControl = (ControlType)(SaveManager.Instance.Data.controlType);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (pointerId != e.pointerId) return;
            if (gestureControl == ControlType.Drag)
            {
                OnDrag(e);
                FinishDrag();
            }
            else
            {
                Vector2 delta = e.position - _pointerDown;
                if (gestureControl == ControlType.Tap && !movedPointer && delta.magnitude < MIN_SWIPE_PX)
                    BoardManager.Instance.TryTapTile(TileIndex);
                else if (gestureControl == ControlType.Swipe && delta.magnitude >= MIN_SWIPE_PX)
                    BoardManager.Instance.TryMoveTile(TileIndex, CardinalDir(delta));
            }
            pointerId = null;
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (pointerId != e.pointerId) return;
            movedPointer = true;
            if (gestureControl != ControlType.Drag) return;
            tileRect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)tileRect.parent, _pointerDown, e.pressEventCamera, out dragStart);
            draggingTile = BoardManager.Instance.BeginTileDrag(TileIndex, out dragOrigin);
        }

        public void OnDrag(PointerEventData e)
        {
            if (pointerId != e.pointerId || !draggingTile) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)tileRect.parent, e.position, e.pressEventCamera, out var local);
            dropPoint = local;
            tileRect.anchoredPosition = dragOrigin + local - dragStart;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (pointerId != e.pointerId) return;
            OnDrag(e);
            FinishDrag();
            pointerId = null;
        }

        private void FinishDrag()
        {
            if (!draggingTile) return;
            draggingTile = false;
            BoardManager.Instance.EndTileDrag(TileIndex, dropPoint);
        }

        private void OnDisable()
        {
            if (draggingTile && BoardManager.Instance != null)
                BoardManager.Instance.EndTileDrag(TileIndex, Vector2.zero, true);
            draggingTile = false;
            pointerId = null;
        }

        // Map a free-direction delta to the nearest axis-aligned unit vector.
        private static Vector2 CardinalDir(Vector2 d)
        {
            if (Mathf.Abs(d.x) >= Mathf.Abs(d.y))
                return d.x > 0 ? Vector2.right : Vector2.left;
            return d.y > 0 ? Vector2.up : Vector2.down;
        }
    }
}
