using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>Main Menu panel: Play button + Settings shortcut.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button debugUnlockButton;   // hidden in production
        [SerializeField] private TMP_Text versionText;

        private void Start()
        {
            playButton    .onClick.AddListener(OnPlay);
            settingsButton.onClick.AddListener(OnSettings);

            if (debugUnlockButton != null)
                debugUnlockButton.onClick.AddListener(OnDebugUnlock);

            if (versionText != null)
                versionText.text = $"v{Application.version}";
        }

        private void OnPlay()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.ShowScreen(UIScreen.ModeSelect);
        }

        private void OnSettings()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.ShowSettingsPopup();
        }

        private void OnDebugUnlock()
        {
            GameManager.Instance.DEBUG_UnlockAll();
            Debug.Log("DEBUG: Everything unlocked!");
        }
    }
}
