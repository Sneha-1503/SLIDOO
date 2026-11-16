using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Slidoo
{
    /// <summary>
    /// Quit confirmation popup (Android back button or quit icon).
    /// </summary>
    public class QuitPopupUI : MonoBehaviour
    {
        [SerializeField] private Button quitButton;
        [SerializeField] private Button cancelButton;

        private void Start()
        {
            quitButton  .onClick.AddListener(OnQuit);
            cancelButton.onClick.AddListener(OnCancel);
        }

        public void Show() => gameObject.SetActive(true);

        private void OnQuit()
        {
            AudioManager.Instance.PlayButtonClick();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnCancel()
        {
            AudioManager.Instance.PlayButtonClick();
            gameObject.SetActive(false);
        }
    }
}
