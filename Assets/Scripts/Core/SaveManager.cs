using UnityEngine;

namespace Slidoo
{
    /// <summary>
    /// Persists all game progress and settings via PlayerPrefs (JSON).
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string SAVE_KEY = "SLIDOO_SAVE_v1";

        public GameSaveData Data { get; private set; }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        public void LoadData()
        {
            if (PlayerPrefs.HasKey(SAVE_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(SAVE_KEY);
                    Data = JsonUtility.FromJson<GameSaveData>(json);
                }
                catch
                {
                    Debug.LogWarning("[SaveManager] Corrupt save – resetting.");
                    Data = new GameSaveData();
                }
            }
            else
            {
                Data = new GameSaveData();
            }
        }

        public void SaveData()
        {
            PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public void ClearData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY);
            Data = new GameSaveData();
        }
    }
}
