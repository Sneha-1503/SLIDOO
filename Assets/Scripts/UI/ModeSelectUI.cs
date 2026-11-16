using UnityEngine;
using UnityEngine.UI;

namespace Slidoo
{
    /// <summary>
    /// Mode Select panel. Instantiates or updates four ModeButtonUI components.
    /// Assign the four ModeButtonUI references in the Inspector.
    /// </summary>
    public class ModeSelectUI : MonoBehaviour
    {
        [SerializeField] private ModeButtonUI[] modeButtons;   // length 4, Easy→Master
        [SerializeField] private Button backButton;

        private void OnEnable()   => RefreshButtons();

        private void Start()
        {
            backButton.onClick.AddListener(() =>
            {
                AudioManager.Instance.PlayButtonClick();
                UIManager.Instance.ShowScreen(UIScreen.MainMenu);
            });
        }

        private void RefreshButtons()
        {
            var save = SaveManager.Instance.Data;
            for (int i = 0; i < 4; i++)
            {
                GameMode mode = (GameMode)i;
                var modeSave  = save.modes[i];
                modeButtons[i].Setup(mode, modeSave);
            }
        }
    }
}
