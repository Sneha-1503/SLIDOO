using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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

        [Header("ON / OFF button appearance")]
        [SerializeField] private Sprite onSprite;
        [SerializeField] private Sprite offSprite;
        [SerializeField, Range(.8f, 1f)] private float pressScale = .9f;
        [SerializeField, Min(.01f)] private float animationDuration = .18f;
        private Coroutine soundAnimation, musicAnimation;
        private Vector3 soundScale, musicScale;

        private void Awake()
        {
            ConfigureInputs();
            soundScale = soundToggle.transform.localScale;
            musicScale = musicToggle.transform.localScale;
        }

        public void ConfigureInputs()
        {
#if UNITY_EDITOR
            if (onSprite == null)
                onSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/toggle_on.png");
            if (offSprite == null)
                offSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UI/Sprites/Settings/toggle_off.png");
#endif
            ConfigureToggle(soundToggle);
            ConfigureToggle(musicToggle);
        }

        private void ConfigureToggle(Toggle toggle)
        {
            if (toggle == null) return;
            var hit = toggle.GetComponent<Image>();
            if (hit == null) hit = toggle.gameObject.AddComponent<Image>();
            hit.color = Color.white;
            hit.preserveAspect = true;
            hit.raycastTarget = true;
            hit.sprite = toggle.isOn ? onSprite : offSprite;
            var background = toggle.transform.Find("Background");
            if (background != null) background.gameObject.SetActive(false);
            toggle.graphic = null;
            toggle.targetGraphic = hit;
            toggle.transition = UnityEngine.UI.Selectable.Transition.None;
            toggle.interactable = true;
        }

        private void SetStateImage(Toggle toggle)
        {
            var image = toggle.GetComponent<Image>();
            image.sprite = toggle.isOn ? onSprite : offSprite;
        }

        private IEnumerator AnimateState(Toggle toggle, Vector3 scale)
        {
            SetStateImage(toggle);
            float elapsed = 0;
            while (elapsed < animationDuration)
            {
                float amount = Mathf.Sin(Mathf.PI * elapsed / animationDuration);
                toggle.transform.localScale = scale * Mathf.Lerp(1, pressScale, amount);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            toggle.transform.localScale = scale;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            soundAnimation = musicAnimation = null;
            soundToggle.transform.localScale = soundScale;
            musicToggle.transform.localScale = musicScale;
        }

        private void OnEnable()
        {
            if (SaveManager.Instance == null) return;
            var data = SaveManager.Instance.Data;
            // Set toggle values without firing listeners
            soundToggle.SetIsOnWithoutNotify(data.soundEnabled);
            musicToggle.SetIsOnWithoutNotify(data.musicEnabled);
            SetStateImage(soundToggle);
            SetStateImage(musicToggle);
        }

        private void Start()
        {
            soundToggle.onValueChanged.AddListener(OnSoundToggle);
            musicToggle.onValueChanged.AddListener(OnMusicToggle);
            closeButton.onClick.AddListener(OnClose);
        }

        private void OnSoundToggle(bool on)
        {
            AudioManager.Instance.SetSoundEnabled(on);
            if (soundAnimation != null) StopCoroutine(soundAnimation);
            soundAnimation = StartCoroutine(AnimateState(soundToggle, soundScale));
        }
        private void OnMusicToggle(bool on)
        {
            AudioManager.Instance.SetMusicEnabled(on);
            if (musicAnimation != null) StopCoroutine(musicAnimation);
            musicAnimation = StartCoroutine(AnimateState(musicToggle, musicScale));
        }

        private void OnClose()
        {
            AudioManager.Instance.PlayButtonClick();
            UIManager.Instance.HideSettingsPopup();
        }
    }
}
