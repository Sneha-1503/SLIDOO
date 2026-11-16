using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Splash screen – shows logo and auto-advances to Main Menu.
    /// Works independently (does NOT require other managers at start).
    /// </summary>
    public class SplashScreenUI : MonoBehaviour
    {
        [SerializeField] private float duration = 2f;
        private float _timer;
        private bool  _done;

        private void OnEnable()
        {
            _timer = 0f;
            _done  = false;
        }

        private void Update()
        {
            if (_done) return;
            _timer += Time.deltaTime;
            if (_timer >= duration)
            {
                _done = true;
                Advance();
            }
        }

        private void Advance()
        {
            if (UIManager.Instance != null)
                UIManager.Instance.ShowScreen(UIScreen.MainMenu);
            else
                Debug.LogError("[SplashScreenUI] UIManager not found in scene! " +
                               "Please run Slidoo → Build Scene from the menu bar.");
        }
    }
}
