using UnityEngine;
using UnityEngine.InputSystem;

namespace Slidoo
{
    /// <summary>
    /// Handles Android hardware back button / Escape key across all screens.
    /// Uses New Input System. Attach to the [MANAGERS] GameObject.
    /// </summary>
    public class BackButtonHandler : MonoBehaviour
    {
        [SerializeField] private QuitPopupUI quitPopup;

        private void Update()
        {
            // Works with both New Input System and legacy
            bool backPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

            // Also check old-style for device compatibility
#if ENABLE_LEGACY_INPUT_MANAGER
            backPressed = backPressed || Input.GetKeyDown(KeyCode.Escape);
#endif

            if (!backPressed) return;

            HandleBackPress();
        }

        private void HandleBackPress()
        {
            var ui = UIManager.Instance;
            if (ui == null) return;

            switch (ui.CurrentScreen)
            {
                case UIScreen.Splash:
                    // Do nothing during splash
                    break;

                case UIScreen.MainMenu:
                    if (quitPopup != null) quitPopup.Show();
                    break;

                case UIScreen.ModeSelect:
                    AudioManager.Instance?.PlayButtonClick();
                    ui.ShowScreen(UIScreen.MainMenu);
                    break;

                case UIScreen.LevelSelect:
                    AudioManager.Instance?.PlayButtonClick();
                    ui.ShowScreen(UIScreen.ModeSelect);
                    break;

                case UIScreen.Gameplay:
                    if (GameManager.Instance != null &&
                        GameManager.Instance.IsPlaying &&
                        !GameManager.Instance.IsPaused)
                    {
                        GameManager.Instance.PauseGame();
                        ui.ShowPausePopup();
                    }
                    break;
            }
        }
    }
}
