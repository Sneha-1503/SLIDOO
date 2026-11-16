using UnityEngine;

namespace Slidoo
{
    /// <summary>
    /// Central singleton. Owns per-session state (move count, timer, stars).
    /// Communicates results to UI via C# events – no direct UI references needed.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // ── Current session context ──────────────────────────────────────────────
        public GameMode CurrentMode        { get; private set; }
        public int      CurrentLevelIndex  { get; private set; }  // 0-29
        public int      CurrentShuffleDepth{ get; private set; }

        // ── Runtime state ────────────────────────────────────────────────────────
        public int   MoveCount   { get; private set; }
        public float ElapsedTime { get; private set; }
        public bool  IsPlaying   { get; private set; }
        public bool  IsPaused    { get; private set; }

        // ── Events ───────────────────────────────────────────────────────────────
        public event System.Action<int>          OnMoveCountChanged;   // moves
        public event System.Action<float>        OnTimeChanged;         // seconds
        public event System.Action<int,int,float,bool> OnLevelWon;     // stars,moves,time,newModeUnlocked

        // ─────────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (IsPlaying && !IsPaused)
            {
                ElapsedTime += Time.deltaTime;
                OnTimeChanged?.Invoke(ElapsedTime);
            }
        }

        // ── Level lifecycle ──────────────────────────────────────────────────────

        public void StartLevel(GameMode mode, int levelIndex)
        {
            CurrentMode         = mode;
            CurrentLevelIndex   = levelIndex;
            CurrentShuffleDepth = ModeConfig.GetShuffleDepth(mode, levelIndex);
            MoveCount           = 0;
            ElapsedTime         = 0f;
            IsPlaying           = true;
            IsPaused            = false;
        }

        public void RegisterMove()
        {
            if (!IsPlaying || IsPaused) return;
            MoveCount++;
            OnMoveCountChanged?.Invoke(MoveCount);
        }

        public void PauseGame()
        {
            if (!IsPlaying) return;
            IsPaused = true;
        }

        public void ResumeGame()
        {
            IsPaused = false;
        }

        public void RestartLevel()
        {
            MoveCount   = 0;
            ElapsedTime = 0f;
            IsPlaying   = true;
            IsPaused    = false;
        }

        /// <summary>Called by BoardManager when win condition is detected.</summary>
        public void OnPuzzleSolved()
        {
            IsPlaying = false;
            int stars = ModeConfig.GetStars(MoveCount, CurrentShuffleDepth);

            var save      = SaveManager.Instance.Data;
            var modeSave  = save.modes[(int)CurrentMode];
            var levelSave = modeSave.levels[CurrentLevelIndex];

            // Update stars / best scores (never downgrade stars)
            if (stars > levelSave.stars)              levelSave.stars     = stars;
            if (levelSave.bestMoves == 0 || MoveCount   < levelSave.bestMoves) levelSave.bestMoves = MoveCount;
            if (levelSave.bestTime  == 0 || ElapsedTime < levelSave.bestTime)  levelSave.bestTime  = ElapsedTime;

            // Unlock next level in same mode
            int nextLevel = CurrentLevelIndex + 1;
            if (nextLevel < ModeConfig.LevelsPerMode)
                modeSave.levels[nextLevel].unlocked = true;

            // Check whether the next mode unlocks
            bool newModeUnlocked = false;
            int  nextModeIdx     = (int)CurrentMode + 1;
            if (nextModeIdx < 4)
            {
                int completed = modeSave.CompletedCount();
                if (completed >= ModeConfig.UnlockRequirement
                    && !save.modes[nextModeIdx].modeUnlocked)
                {
                    save.modes[nextModeIdx].modeUnlocked         = true;
                    save.modes[nextModeIdx].levels[0].unlocked   = true;
                    newModeUnlocked = true;
                }
            }

            SaveManager.Instance.SaveData();
            OnLevelWon?.Invoke(stars, MoveCount, ElapsedTime, newModeUnlocked);
        }

        // ── Debug ────────────────────────────────────────────────────────────────
        [ContextMenu("DEBUG – Unlock Everything")]
        public void DEBUG_UnlockAll()
        {
            var save = SaveManager.Instance.Data;
            foreach (var mode in save.modes)
            {
                mode.modeUnlocked = true;
                foreach (var level in mode.levels)
                    level.unlocked = true;
            }
            SaveManager.Instance.SaveData();
            Debug.Log("[DEBUG] All modes and levels unlocked.");
        }
    }
}
