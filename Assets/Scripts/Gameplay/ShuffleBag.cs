using System.Collections.Generic;
using UnityEngine;

namespace Slidoo
{
    /// <summary>
    /// Guarantees every puzzle image appears once before any repeat.
    /// Uses a Fisher-Yates shuffled list that refills when exhausted.
    /// Images must live in Resources/Puzzles/ named puzzle_01 … puzzle_30.
    /// </summary>
    public class ShuffleBag : MonoBehaviour
    {
        public static ShuffleBag Instance { get; private set; }

        private readonly List<int> _bag = new List<int>();
        private int _cursor = 0;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Refill();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns the next Texture2D from the bag. Never returns null
        /// unless the Resources folder is empty.</summary>
        public Texture2D NextImage()
        {
            int idx = NextIndex();
            string path = $"Puzzles/puzzle_{idx + 1:D2}";
            Texture2D tex = Resources.Load<Texture2D>(path);
            if (tex == null)
                Debug.LogWarning($"[ShuffleBag] Missing image at Resources/{path}");
            return tex;
        }

        // ── Internals ────────────────────────────────────────────────────────────

        private int NextIndex()
        {
            if (_cursor >= _bag.Count) Refill();
            return _bag[_cursor++];
        }

        private void Refill()
        {
            _bag.Clear();
            for (int i = 0; i < ModeConfig.ImagePoolSize; i++) _bag.Add(i);
            for (int i = _bag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }
            _cursor = 0;
        }
    }
}

