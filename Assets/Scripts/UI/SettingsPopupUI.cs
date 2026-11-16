using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Settings popup: sound toggle, music toggle, close button.
    /// Works from both Main Menu and Gameplay screens.
    /// </summary>
    public class SettingsPopupUI : MonoBehaviour
    {
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Button closeButton;

        private void OnEnable()
        {
            var data = SaveManager.Instance.Data;
            // Set toggle values without firing listeners
            soundToggle.SetIsOnWithoutNotify(data.soundEnabled);
            musicToggle.SetIsOnWithoutNotify(data.musicEnabled);
        }

        private void Start()
        {
            soundToggle.onValueChanged.AddListener(OnSoundToggle);
            musicToggle.onValueChanged.AddListener(OnMusicToggle);
            closeButton.onClick.AddListener(OnClose);
        }

        private void OnSoundToggle(bool on) => AudioManager.Instance.SetSoundEnabled(on);
        private void OnMusicToggle(bool on) => AudioManager.Instance.SetMusicEnabled(on);

        private void OnClose()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HideSettingsPopup();
        }
    }
}
