using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace UI.MainMenuUI
{
    /// <summary>
    /// Interactive comic button styled for MainMenuUI.
    /// Provides hover effect (scale, color swap, hover sound) and click effect (punch scale, confirm sound).
    /// Prevents double-clicking and visual fighting when transitions are running.
    /// </summary>
    public class MainMenuUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
    {
        [Header("References")]
        public Image highlightBanner;
        public TextMeshProUGUI buttonText;
        public Image accentTab;
        public MainMenuUIAudioSynthesizer audioSynth;
        public MainMenuUIController mainController;

        [Header("Colors (Optional for Menu Options)")]
        public bool swapColorsOnHover = true;
        public Color normalTextColor = Color.white;
        public Color hoverTextColor = Color.black;
        public Color normalBannerColor = new Color(0f, 0f, 0f, 0.85f);
        public Color hoverBannerColor = new Color(1f, 0.88f, 0.1f, 1f); // Vibrant comic yellow

        [Header("Animation (Hover & Click Only)")]
        public float hoverScale = 1.04f;
        public float clickDownScale = 0.95f;

        private Vector3 _baseScale = Vector3.one;
        private bool _isHovered = false;
        private bool _isLocked = false;
        private Coroutine _clickAnimRoutine;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (mainController == null)
            {
                mainController = Object.FindFirstObjectByType<MainMenuUIController>();
            }
            SetHoverVisual(false, playAudio: false);
        }

        private void OnEnable()
        {
            _isLocked = false;
            transform.localScale = _baseScale;
            SetHoverVisual(false, playAudio: false);
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (_clickAnimRoutine != null)
            {
                StopCoroutine(_clickAnimRoutine);
                _clickAnimRoutine = null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_isLocked) return;
            SetHoverVisual(true, playAudio: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_isLocked) return;
            SetHoverVisual(false, playAudio: false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_isLocked) return;
            SetHoverVisual(true, playAudio: true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (_isLocked) return;
            SetHoverVisual(false, playAudio: false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_isLocked) return;

            // If this button is linked to MainMenuUIController (e.g. "Press Any Button"),
            // route click through the controller to prevent duplicate triggers
            if (mainController != null && mainController.pressAnyButtonObject == gameObject)
            {
                mainController.OnPressAnyButtonActivated();
                return;
            }

            TriggerClickEffect();
        }

        private void SetHoverVisual(bool active, bool playAudio)
        {
            if (_isLocked) return;
            _isHovered = active;

            if (swapColorsOnHover)
            {
                if (highlightBanner != null)
                {
                    highlightBanner.color = active ? hoverBannerColor : normalBannerColor;
                }

                if (buttonText != null)
                {
                    buttonText.color = active ? hoverTextColor : normalTextColor;
                }
            }

            if (accentTab != null)
            {
                accentTab.transform.localScale = active ? Vector3.one * 1.15f : Vector3.one;
            }

            transform.localScale = _baseScale * (active ? hoverScale : 1f);

            if (active && playAudio && audioSynth != null)
            {
                audioSynth.PlayHover();
            }
        }

        public void TriggerClickEffect(bool playAudio = true)
        {
            if (_isLocked) return;

            if (_clickAnimRoutine != null) StopCoroutine(_clickAnimRoutine);
            _clickAnimRoutine = StartCoroutine(ClickAnimationRoutine());

            if (playAudio && audioSynth != null)
            {
                audioSynth.PlayConfirm();
            }
        }

        private IEnumerator ClickAnimationRoutine()
        {
            transform.localScale = _baseScale * clickDownScale;
            yield return new WaitForSecondsRealtime(0.06f);

            if (!_isLocked)
            {
                transform.localScale = _baseScale * (_isHovered ? hoverScale : 1f);
            }
            _clickAnimRoutine = null;
        }
    }
}
