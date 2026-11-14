using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace UI.MainMenuUI
{
    /// Interactive comic button styled for MainMenuUI.
    /// Provides hover effect (scale, color swap, hover sound) and click effect (punch scale, confirm sound).
    public class MainMenuUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler
    {
        [Header("References")]
        public Image highlightBanner;
        public TextMeshProUGUI buttonText;
        public Image accentTab;
        public MainMenuUIAudioSynthesizer audioSynth;

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
        private Coroutine _clickAnimRoutine;

        private void Awake()
        {
            _baseScale = transform.localScale;
            SetHoverVisual(false, playAudio: false);
        }

        private void OnEnable()
        {
            transform.localScale = _baseScale;
            SetHoverVisual(false, playAudio: false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHoverVisual(true, playAudio: true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverVisual(false, playAudio: false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetHoverVisual(true, playAudio: true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetHoverVisual(false, playAudio: false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            TriggerClickEffect();
        }

        private void SetHoverVisual(bool active, bool playAudio)
        {
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

        public void TriggerClickEffect()
        {
            if (_clickAnimRoutine != null) StopCoroutine(_clickAnimRoutine);
            _clickAnimRoutine = StartCoroutine(ClickAnimationRoutine());

            if (audioSynth != null)
            {
                audioSynth.PlayConfirm();
            }
        }

        private IEnumerator ClickAnimationRoutine()
        {
            transform.localScale = _baseScale * clickDownScale;
            yield return new WaitForSecondsRealtime(0.06f);

            transform.localScale = _baseScale * (_isHovered ? hoverScale : 1f);
            _clickAnimRoutine = null;
        }
    }
}
