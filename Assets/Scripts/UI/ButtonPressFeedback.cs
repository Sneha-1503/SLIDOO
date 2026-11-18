using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Slidoo
{
    [RequireComponent(typeof(Button))]
    public class ButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField, Range(.8f, 1f)] private float pressedScale = .94f;
        [SerializeField, Min(.01f)] private float responseTime = .055f;
        private Vector3 originalScale;
        private Vector3 velocity;
        private bool pressed;
        private Button button;

        private void OnEnable()
        {
            originalScale = transform.localScale;
            button = GetComponent<Button>();
            pressed = false;
        }

        private void Update()
        {
            Vector3 target = originalScale * (pressed ? pressedScale : 1);
            transform.localScale = Vector3.SmoothDamp(transform.localScale, target,
                ref velocity, responseTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && button.IsInteractable())
                pressed = true;
        }

        public void OnPointerUp(PointerEventData e) => pressed = false;
        public void OnPointerExit(PointerEventData e) => pressed = false;

        private void OnDisable()
        {
            transform.localScale = originalScale;
            velocity = Vector3.zero;
            pressed = false;
        }
    }
}
