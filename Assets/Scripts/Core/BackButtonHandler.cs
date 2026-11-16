using UnityEngine;

namespace Slidoo
{
    /// <summary>
    /// Handles Android hardware back button across all screens.
    /// Attach to the [Managers] GameObject.
    /// </summary>
    public class BackButtonHandler : MonoBehaviour
    {
        [SerializeField] private QuitPopupUI quitPopup;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            var ui = UIManager.Instance;

            // If any popup is open – close it first
            // (UIManager.HideAllPopups handles this via ShowScreen, so we check manually)
            switch (ui.CurrentScreen)
            {
                case UIScreen.Splash:
                    break;

                case UIScreen.MainMenu:
                    if (quitPopup != null) quitPopup.Show();
                    break;

                case UIScreen.ModeSelect:
                    ui.ShowScreen(UIScreen.MainMenu);
                    break;

                case UIScreen.LevelSelect:
                    ui.ShowScreen(UIScreen.ModeSelect);
                    break;

                case UIScreen.Gameplay:
                    if (GameManager.Instance.IsPlaying && !GameManager.Instance.IsPaused)
                    {
                        GameManager.Instance.PauseGame();
                        ui.ShowPausePopup();
                    }
                    break;
            }
        }
    }
}
