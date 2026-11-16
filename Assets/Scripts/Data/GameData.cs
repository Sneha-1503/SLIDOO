using System;
using UnityEngine;

namespace Slidoo
{
    // ─── Enums ──────────────────────────────────────────────────────────────────
    public enum GameMode    { Easy = 0, Medium = 1, Hard = 2, Master = 3 }
    public enum ControlType { Swipe = 0, Tap = 1, Drag = 2 }

    // ─── Static Mode Configuration ──────────────────────────────────────────────
    public static class ModeConfig
    {
        public static readonly int[]    GridSizes  = { 3, 4, 5, 6 };
        public static readonly string[] ModeNames  = { "Easy", "Medium", "Hard", "Master" };
        public const  int LevelsPerMode      = 30;
        public const  int UnlockRequirement  = 15;   // completed levels needed to unlock next mode
        public const  int ImagePoolSize      = 30;

        public static int GetGridSize(GameMode mode) => GridSizes[(int)mode];

        /// <summary>Returns shuffle depth for a given mode and level (0-indexed).</summary>
        public static int GetShuffleDepth(GameMode mode, int levelIndex)
        {
            int   g = GetGridSize(mode);
            float t = Mathf.Clamp01(levelIndex / 29f);
            return Mathf.Max(5, Mathf.RoundToInt(g * g * (0.3f + 0.7f * t)));
        }

        /// <summary>Returns 1–3 stars given move count vs shuffle depth.</summary>
        public static int GetStars(int moves, int shuffleDepth)
        {
            if (moves <= shuffleDepth)                                return 3;
            if (moves <= Mathf.RoundToInt(shuffleDepth * 1.5f))      return 2;
            return 1;
        }
    }

    // ─── Save Data Structures ───────────────────────────────────────────────────
    [Serializable]
    public class LevelSave
    {
        public bool  unlocked;
        public int   stars;      // 0 = not completed yet
        public int   bestMoves;
        public float bestTime;

        public LevelSave() { }
        public LevelSave(bool u) { unlocked = u; }
    }

    [Serializable]
    public class ModeSave
    {
        public bool       modeUnlocked;
        public LevelSave[] levels;

        public ModeSave() { }
        public ModeSave(bool startUnlocked)
        {
            modeUnlocked = startUnlocked;
            levels = new LevelSave[ModeConfig.LevelsPerMode];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = new LevelSave(i == 0 && startUnlocked);
        }

        public int CompletedCount()
        {
            int c = 0;
            foreach (var l in levels) if (l.stars > 0) c++;
            return c;
        }

        public int TotalStars()
        {
            int s = 0;
            foreach (var l in levels) s += l.stars;
            return s;
        }
    }

    [Serializable]
    public class GameSaveData
    {
        public ModeSave[] modes;
        public bool soundEnabled  = true;
        public bool musicEnabled  = true;
        public int  controlType   = 0;   // ControlType enum index

        public GameSaveData()
        {
            modes    = new ModeSave[4];
            modes[0] = new ModeSave(true);   // Easy always unlocked
            for (int i = 1; i < 4; i++)
                modes[i] = new ModeSave(false);
        }
    }
}
