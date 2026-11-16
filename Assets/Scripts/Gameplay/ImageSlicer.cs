using UnityEngine;

namespace Slidoo
{
    /// <summary>
    /// Provides UV rects for slicing a texture into gridSize×gridSize tiles.
    /// We use UV rects (not pixel copies) so we only need ONE RawImage texture.
    /// tileIndex = row * gridSize + col  (row 0 = top-left).
    /// </summary>
    public static class ImageSlicer
    {
        /// <summary>
        /// Returns an array of UV rects, one per tile, in row-major order
        /// (tileIndex 0 = top-left, last = bottom-right).
        /// </summary>
        public static Rect[] GetUVRects(int gridSize)
        {
            Rect[] rects = new Rect[gridSize * gridSize];
            float size = 1f / gridSize;

            for (int row = 0; row < gridSize; row++)
            {
                for (int col = 0; col < gridSize; col++)
                {
                    // Unity UV origin is bottom-left; row 0 (top) → highest UV y
                    float uvX = col * size;
                    float uvY = (gridSize - 1 - row) * size;
                    rects[row * gridSize + col] = new Rect(uvX, uvY, size, size);
                }
            }
            return rects;
        }
    }
}
