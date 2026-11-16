using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Slidoo
{
    /// <summary>
    /// Orchestrates starting and restarting levels.
    /// Connects: GameManager → ShuffleBag → BoardManager → UIManager.
    /// Call LoadLevel(mode, levelIndex) to begin a level.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        public static LevelController Instance { get; private set; }

        [SerializeField] private GameplayUI gameplayUI;

        private Texture2D _currentTexture;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (BoardManager.Instance != null)
                BoardManager.Instance.OnBoardSolved += HandleBoardSolved;
        }

        private void OnDisable()
        {
            if (BoardManager.Instance != null)
                BoardManager.Instance.OnBoardSolved -= HandleBoardSolved;
        }

        // ── Public API ───────────────────────────────────────────────────────────

        public void LoadLevel(GameMode mode, int levelIndex)
        {
            UIManager.Instance.ShowScreen(UIScreen.Gameplay);
            GameManager.Instance.StartLevel(mode, levelIndex);
            _currentTexture = ShuffleBag.Instance.NextImage();

            // Wait one frame so the Canvas/LayoutGroups have finished resizing
            StartCoroutine(BuildAfterLayout(mode, levelIndex));
        }

        public void RestartCurrentLevel()
        {
            GameManager.Instance.RestartLevel();
            int   gridSize    = ModeConfig.GetGridSize(GameManager.Instance.CurrentMode);
            int   shuffleDepth= GameManager.Instance.CurrentShuffleDepth;
            BoardManager.Instance.BuildBoard(gridSize, _currentTexture);
            StartCoroutine(ShuffleAfterFrame(shuffleDepth));
            gameplayUI.ResetUI();
        }

        // ── Internals ────────────────────────────────────────────────────────────

        private IEnumerator BuildAfterLayout(GameMode mode, int levelIndex)
        {
            yield return null;   // wait one frame

            int gridSize     = ModeConfig.GetGridSize(mode);
            int shuffleDepth = ModeConfig.GetShuffleDepth(mode, levelIndex);

            BoardManager.Instance.BuildBoard(gridSize, _currentTexture);

            yield return null;   // let tiles spawn

            BoardManager.Instance.ShuffleBoard(shuffleDepth);
            gameplayUI.SetPreviewTexture(_currentTexture);
            gameplayUI.ResetUI();
        }

        private IEnumerator ShuffleAfterFrame(int depth)
        {
            yield return null;
            BoardManager.Instance.ShuffleBoard(depth);
        }

        private void HandleBoardSolved()
        {
            // GameManager already fires OnLevelWon → WinPopupUI listens there
        }
    }
}
