using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Pause popup: Resume / Restart / Menu.
    /// </summary>
    public class PausePopupUI : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Start()
        {
            resumeButton .onClick.AddListener(OnResume);
            restartButton.onClick.AddListener(OnRestart);
            menuButton   .onClick.AddListener(OnMenu);
        }

        private void OnResume()
        {
            AudioManager.Instance.PlayButtonClick();
            GameManager.Instance.ResumeGame();
            UIManager.Instance.HidePausePopup();
        }

        private void OnRestart()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HidePausePopup();
            LevelController.Instance.RestartCurrentLevel();
        }

        private void OnMenu()
        {
            AudioManager.Instance.PlayButtonClick();
            GameManager.Instance.ResumeGame();
            UIManager.Instance.ShowScreen(UIScreen.LevelSelect);
        }
    }
}
