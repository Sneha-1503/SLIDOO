using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Slidoo
{
    /// <summary>
    /// Owns the grid. Responsible for:
    ///   • Building tile GameObjects from a texture
    ///   • Shuffling (always-solvable, never undoes last move)
    ///   • Responding to swipe inputs routed from Tile components
    ///   • Win detection
    /// </summary>
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }

        // ── Inspector references ─────────────────────────────────────────────────
        [Header("Board")]
        [SerializeField] private RectTransform boardContainer;   // square panel that holds tiles
        [SerializeField] private GameObject    tilePrefab;       // has Tile + RawImage

        [Header("Visuals")]
        [SerializeField] private float animDuration = 0.15f;
        [SerializeField] private float tileGap      = 3f;

        // ── Board state ──────────────────────────────────────────────────────────
        public int GridSize { get; private set; }

        private int[]  _board;          // _board[position] = tileIndex  (-1 = empty)
        private int[]  _tilePos;        // _tilePos[tileIndex] = position
        private Tile[] _tiles;          // indexed by tileIndex (0 … n²-2)
        private int    _emptyPos;       // flat position of the empty slot
        private Rect[] _uvRects;
        private bool   _animating;

        // ── Events ───────────────────────────────────────────────────────────────
        public event System.Action OnBoardSolved;

        // ─────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Call this after the board container has its final size.</summary>
        public void BuildBoard(int gridSize, Texture2D texture)
        {
            // Clear previous tiles
            foreach (Transform t in boardContainer) Destroy(t.gameObject);
            _animating = false;

            GridSize = gridSize;
            int total = gridSize * gridSize;

            _board   = new int[total];
            _tilePos = new int[total];
            _tiles   = new Tile[total - 1];   // last slot is empty
            _uvRects = ImageSlicer.GetUVRects(gridSize);

            // Solved state: board[i] = i, empty = last cell
            for (int i = 0; i < total; i++) _board[i] = i;
            _board[total - 1] = -1;
            _emptyPos          = total - 1;
            for (int i = 0; i < total; i++)
                if (_board[i] >= 0) _tilePos[_board[i]] = i;

            float tileSize = CalcTileSize(gridSize);

            // Instantiate tiles
            for (int pos = 0; pos < total; pos++)
            {
                int ti = _board[pos];
                if (ti == -1) continue;

                GameObject go = Instantiate(tilePrefab, boardContainer);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 1); // top-left anchor
                rt.sizeDelta       = new Vector2(tileSize, tileSize);
                rt.anchoredPosition = PosToAnchor(pos, gridSize, tileSize);

                Tile tile = go.GetComponent<Tile>();
                tile.Initialize(ti, texture, _uvRects[ti]);
                _tiles[ti] = tile;
            }
        }

        /// <summary>Shuffles from solved state using N valid random moves.</summary>
        public void ShuffleBoard(int depth)
        {
            int lastEmptyPos = -1;
            float tileSize   = CalcTileSize(GridSize);

            for (int i = 0; i < depth; i++)
            {
                var neighbors = GetNeighbors(_emptyPos);
                if (lastEmptyPos >= 0) neighbors.Remove(lastEmptyPos);
                int picked = neighbors[Random.Range(0, neighbors.Count)];
                lastEmptyPos = _emptyPos;
                SwapInstant(picked, _emptyPos);
            }

            // Snap all tiles to their new positions
            for (int pos = 0; pos < _board.Length; pos++)
            {
                int ti = _board[pos];
                if (ti < 0) continue;
                _tiles[ti].GetComponent<RectTransform>().anchoredPosition =
                    PosToAnchor(pos, GridSize, tileSize);
            }
        }

        /// <summary>Called by a Tile when the player swipes on it.</summary>
        public void TryMoveTile(int tileIndex, Vector2 swipeDir)
        {
            if (_animating) return;

            int tilePos   = _tilePos[tileIndex];
            int targetPos = GetNeighborInDir(tilePos, swipeDir);

            if (targetPos != _emptyPos)
            {
                AudioManager.Instance.PlayError();
                return;
            }

            StartCoroutine(AnimateMove(tileIndex, tilePos, targetPos));
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private float CalcTileSize(int gridSize)
        {
            float board = boardContainer.rect.width;
            return (board - tileGap * (gridSize - 1)) / gridSize;
        }

        private Vector2 PosToAnchor(int pos, int gridSize, float tileSize)
        {
            int row = pos / gridSize;
            int col = pos % gridSize;
            float x = col * (tileSize + tileGap);
            float y = -(row * (tileSize + tileGap));
            return new Vector2(x, y);
        }

        private void SwapInstant(int from, int to)
        {
            int ti      = _board[from];
            _board[to]  = ti;
            _board[from] = -1;
            if (ti >= 0) _tilePos[ti] = to;
            _emptyPos   = from;
        }

        private IEnumerator AnimateMove(int tileIndex, int fromPos, int toPos)
        {
            _animating = true;
            GameManager.Instance.RegisterMove();
            AudioManager.Instance.PlayTileSlide();

            float    tileSize = CalcTileSize(GridSize);
            Vector2  startA   = PosToAnchor(fromPos, GridSize, tileSize);
            Vector2  endA     = PosToAnchor(toPos,   GridSize, tileSize);
            RectTransform rt  = _tiles[tileIndex].GetComponent<RectTransform>();

            // Update logical state right away
            _board[toPos]   = tileIndex;
            _board[fromPos] = -1;
            _tilePos[tileIndex] = toPos;
            _emptyPos       = fromPos;

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / animDuration));
                rt.anchoredPosition = Vector2.Lerp(startA, endA, t);
                yield return null;
            }
            rt.anchoredPosition = endA;

            _animating = false;

            if (CheckWin())
            {
                OnBoardSolved?.Invoke();
                GameManager.Instance.OnPuzzleSolved();
            }
        }

        private bool CheckWin()
        {
            int last = _board.Length - 1;
            if (_board[last] != -1) return false;
            for (int i = 0; i < last; i++)
                if (_board[i] != i) return false;
            return true;
        }

        private List<int> GetNeighbors(int pos)
        {
            var list = new List<int>(4);
            int row = pos / GridSize, col = pos % GridSize;
            if (row > 0)             list.Add(pos - GridSize);
            if (row < GridSize - 1)  list.Add(pos + GridSize);
            if (col > 0)             list.Add(pos - 1);
            if (col < GridSize - 1)  list.Add(pos + 1);
            return list;
        }

        private int GetNeighborInDir(int pos, Vector2 dir)
        {
            int row = pos / GridSize, col = pos % GridSize;
            int r = row, c = col;

            if      (dir.y >  0.5f) r--;   // swipe up   → move tile up
            else if (dir.y < -0.5f) r++;   // swipe down → move tile down
            else if (dir.x < -0.5f) c--;   // swipe left → move tile left
            else if (dir.x >  0.5f) c++;   // swipe right→ move tile right
            else return -1;

            if (r < 0 || r >= GridSize || c < 0 || c >= GridSize) return -1;
            return r * GridSize + c;
        }

        public Texture2D GetCurrentTexture()
        {
            // Return texture from first tile
            if (_tiles != null && _tiles.Length > 0 && _tiles[0] != null)
                return _tiles[0].GetTexture();
            return null;
        }
    }
}
