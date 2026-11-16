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
    public class Tile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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

        public void OnPointerDown(PointerEventData e) => _pointerDown = e.position;

        public void OnPointerUp(PointerEventData e)
        {
            Vector2 delta = e.position - _pointerDown;
            if (delta.magnitude < MIN_SWIPE_PX) return;      // too short – ignore

            Vector2 dir = CardinalDir(delta);
            BoardManager.Instance.TryMoveTile(TileIndex, dir);
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
