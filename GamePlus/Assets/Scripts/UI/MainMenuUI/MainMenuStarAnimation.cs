using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenuUI
{
    /// <summary>
    /// Master animation controller for the Title Screen star mascot.
    /// Provides:
    /// 1. Relaxed organic idle at the left home position (bobbing, breathing, tilt wobble).
    /// 2. Smooth comic hover glide to center with arc and squash/stretch cushion.
    /// 3. Excited hover idle in center.
    /// 4. Tactile comic press sequence:
    ///    - Fly to center (if not already there).
    ///    - 360° spin leap wind-up.
    ///    - Downward plunge and shrink into the button ("mengecil seolah menekan").
    ///    - Tactile hold / pause with micro-shudder.
    ///    - Elastic release pop.
    ///    - Screen transition handoff.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MainMenuStarAnimation : MonoBehaviour
    {
        [Header("References")]
        public RectTransform rectTransform;
        public Image starImage;
        public Image flashOverlay;
        public MainMenuUIButton parentButton;

        [Header("Positions & Anchors")]
        [Tooltip("Resting position at the left end of the button.")]
        public Vector2 homePosition = new Vector2(-261.33f, -13.72f);

        [Tooltip("Target center position along the slanted button bar.")]
        public Vector2 centerPosition = new Vector2(0f, -4f);

        [Tooltip("Slant angle of the comic button (degrees).")]
        public float baseRotationAngle = 2.0f;

        [Header("Scales")]
        public Vector3 homeScale = Vector3.one;
        public Vector3 hoverScale = new Vector3(1.18f, 1.18f, 1f);

        [Header("Idle Animation (At Left Home)")]
        public bool enableIdle = true;
        public float idleBobSpeed = 2.4f;
        public float idleBobDistance = 3.5f;
        public float idleBreatheSpeed = 2.8f;
        public float idleBreatheAmount = 0.04f;
        public float idleTiltSpeed = 1.8f;
        public float idleTiltAngle = 3.5f;

        [Header("Hover Animation (Glide to Center)")]
        public float hoverMoveDuration = 0.26f;
        public float hoverReturnDuration = 0.22f;
        public float hoverArcHeight = 16f;
        public float hoverLeanAngle = -15f;

        [Header("Hover Idle (Excited Bobbing in Center)")]
        public float hoverBobSpeed = 4.2f;
        public float hoverBobDistance = 4.5f;
        public float hoverPulseAmount = 0.05f;

        [Header("Press Sequence Settings")]
        [Tooltip("Flight to center if pressed while not hovering.")]
        public float flyToCenterDuration = 0.22f;
        public float flyArcHeight = 24f;

        [Tooltip("Spin wind-up before pressing down.")]
        public float spinDuration = 0.32f;
        public float spinLiftY = 32f;
        public float spinTotalDegrees = 360f;

        [Tooltip("Down plunge and shrink into the button.")]
        public float pressDuration = 0.16f;
        public float pressDropY = -14f;
        public Vector3 pressShrinkScale = new Vector3(0.62f, 0.62f, 1f);
        public Vector3 pressSquashScale = new Vector3(0.70f, 0.52f, 1f);

        [Tooltip("Tactile hold duration before release and scene transition.")]
        public float holdDuration = 0.35f;
        public float releasePopDuration = 0.14f;

        // State tracking
        private bool _isHovered = false;
        private bool _isPressing = false;
        private bool _isLocked = false;
        private Vector2 _currentBasePos;
        private Coroutine _activeTransitionRoutine;
        private Coroutine _pressRoutine;
        private Coroutine _flashRoutine;

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            if (starImage == null)
            {
                starImage = GetComponent<Image>();
            }
            if (parentButton == null)
            {
                parentButton = GetComponentInParent<MainMenuUIButton>();
            }

            _currentBasePos = homePosition;
            ResetToHome();
        }

        private void OnEnable()
        {
            _isPressing = false;
            _isLocked = false;
            _isHovered = false;
            _currentBasePos = homePosition;
            ResetToHome();
        }

        private void Update()
        {
            // Only apply ambient idle/hover bobbing when not actively pressing or transitioning
            if (_isPressing || _activeTransitionRoutine != null) return;

            float time = Time.unscaledTime;

            if (_isHovered)
            {
                // Excited hover-idle in center
                float bobY = Mathf.Sin(time * hoverBobSpeed) * hoverBobDistance;
                float pulse = 1f + Mathf.Sin(time * hoverBobSpeed * 1.5f) * hoverPulseAmount;

                rectTransform.anchoredPosition = _currentBasePos + new Vector2(0f, bobY);
                rectTransform.localScale = Vector3.Scale(hoverScale, new Vector3(pulse, pulse, 1f));
                rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);
            }
            else if (enableIdle)
            {
                // Relaxed floating mascot idle on the left
                float bobY = Mathf.Sin(time * idleBobSpeed) * idleBobDistance;
                float pulse = 1f + Mathf.Sin(time * idleBreatheSpeed) * idleBreatheAmount;
                float tilt = Mathf.Sin(time * idleTiltSpeed) * idleTiltAngle;

                rectTransform.anchoredPosition = _currentBasePos + new Vector2(0f, bobY);
                rectTransform.localScale = homeScale * pulse;
                rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle + tilt);
            }
        }

        public void SetHover(bool hovered)
        {
            if (_isLocked || _isPressing) return;
            if (_isHovered == hovered) return;

            _isHovered = hovered;

            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
            }

            Vector2 targetPos = hovered ? centerPosition : homePosition;
            Vector3 targetScale = hovered ? hoverScale : homeScale;
            float duration = hovered ? hoverMoveDuration : hoverReturnDuration;

            _activeTransitionRoutine = StartCoroutine(GlideRoutine(targetPos, targetScale, hovered, duration));
        }

        private IEnumerator GlideRoutine(Vector2 targetPos, Vector3 targetScale, bool toCenter, float duration)
        {
            Vector2 startPos = rectTransform.anchoredPosition;
            Vector3 startScale = rectTransform.localScale;
            float startAngle = rectTransform.localEulerAngles.z;
            if (startAngle > 180f) startAngle -= 360f;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float ease = 1f - Mathf.Pow(1f - t, 2.8f);
                float arc = Mathf.Sin(t * Mathf.PI) * hoverArcHeight;

                Vector2 curPos = Vector2.Lerp(startPos, targetPos, ease);
                curPos.y += arc;
                rectTransform.anchoredPosition = curPos;

                Vector3 currentTargetScale = Vector3.Lerp(startScale, targetScale, ease);
                rectTransform.localScale = currentTargetScale;

                float targetAngle = baseRotationAngle;
                if (toCenter)
                {
                    float lean = Mathf.Sin(t * Mathf.PI) * hoverLeanAngle;
                    rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startAngle, targetAngle, ease) + lean);
                }
                else
                {
                    float returnLean = Mathf.Sin(t * Mathf.PI) * (-hoverLeanAngle * 0.7f);
                    rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startAngle, targetAngle, ease) + returnLean);
                }

                yield return null;
            }

            _currentBasePos = targetPos;
            rectTransform.anchoredPosition = targetPos;
            rectTransform.localScale = targetScale;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);
            _activeTransitionRoutine = null;
        }

        /// <summary>
        /// Plays the full tactile press sequence:
        /// 1. Smooth travel to center (if clicking from anywhere on screen when not hovered).
        /// 2. Dynamic 360° spin leap wind-up.
        /// 3. Downward press and shrink into button ("mengecil seolah menekan").
        /// 4. Tactile hold / pause with micro-shudder so the click feels weighty and real.
        /// 5. Elastic release pop.
        /// 6. Triggers onTransitionReady to hand off to screen transition.
        /// </summary>
        public void PlayPressSequence(Action onImpact = null, Action onTransitionReady = null)
        {
            if (_isPressing) return;

            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }
            if (_pressRoutine != null)
            {
                StopCoroutine(_pressRoutine);
            }

            _isPressing = true;
            _isLocked = true;
            _pressRoutine = StartCoroutine(PressSequenceRoutine(onImpact, onTransitionReady));
        }

        // Backward compatibility helper
        public void PlayClickSlam(Action onImpact = null, Action onComplete = null)
        {
            PlayPressSequence(onImpact, onComplete);
        }

        private IEnumerator PressSequenceRoutine(Action onImpact, Action onTransitionReady)
        {
            // --- Phase 1: Smooth Flight to Center (if not already there) ---
            if (Vector2.Distance(rectTransform.anchoredPosition, centerPosition) > 15f)
            {
                float flightElapsed = 0f;
                Vector2 fromPos = rectTransform.anchoredPosition;
                Vector3 fromScale = rectTransform.localScale;
                float fromAngle = rectTransform.localEulerAngles.z;
                if (fromAngle > 180f) fromAngle -= 360f;

                while (flightElapsed < flyToCenterDuration)
                {
                    flightElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(flightElapsed / flyToCenterDuration);
                    float ease = 1f - Mathf.Pow(1f - t, 2.5f);

                    Vector2 pos = Vector2.Lerp(fromPos, centerPosition, ease);
                    pos.y += Mathf.Sin(t * Mathf.PI) * flyArcHeight;
                    rectTransform.anchoredPosition = pos;

                    rectTransform.localScale = Vector3.Lerp(fromScale, hoverScale, ease);
                    rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(fromAngle, baseRotationAngle, ease) + Mathf.Sin(t * Mathf.PI) * hoverLeanAngle);
                    yield return null;
                }
            }

            _currentBasePos = centerPosition;
            rectTransform.anchoredPosition = centerPosition;
            rectTransform.localScale = hoverScale;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);

            // --- Phase 2: Acrobatic Spin Wind-up ("berputar") ---
            float spinElapsed = 0f;
            Vector2 basePos = centerPosition;
            Vector3 peakScale = hoverScale * 1.12f;

            while (spinElapsed < spinDuration)
            {
                spinElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(spinElapsed / spinDuration);
                // Smooth ease in and out for spin
                float spinEase = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

                // Lift into the air during spin
                float yLift = Mathf.Sin(t * Mathf.PI) * spinLiftY;
                rectTransform.anchoredPosition = basePos + new Vector2(0f, yLift);

                // Stretch along the leap
                float stretchY = 1f + Mathf.Sin(t * Mathf.PI) * 0.15f;
                rectTransform.localScale = new Vector3(peakScale.x, peakScale.y * stretchY, 1f);

                // Full 360° dynamic spin
                float angle = baseRotationAngle + (spinTotalDegrees * spinEase);
                rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);

                yield return null;
            }

            Vector2 peakPos = basePos + new Vector2(0f, 10f);
            rectTransform.anchoredPosition = peakPos;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);

            // --- Phase 3: Plunge Down & Shrink into Button ("mengecil seolah menekan") ---
            float pressElapsed = 0f;
            Vector2 deepPressedPos = basePos + new Vector2(0f, pressDropY);

            while (pressElapsed < pressDuration)
            {
                pressElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(pressElapsed / pressDuration);
                float easeDown = t * t * t; // Powerful downward plunge

                rectTransform.anchoredPosition = Vector2.Lerp(peakPos, deepPressedPos, easeDown);

                // Shrink and squash into the button switch
                Vector3 currentScale = Vector3.Lerp(peakScale, pressSquashScale, easeDown);
                rectTransform.localScale = currentScale;

                yield return null;
            }

            rectTransform.anchoredPosition = deepPressedPos;
            rectTransform.localScale = pressSquashScale;

            // --- Impact Trigger: Flash, Button Physical Depression, Sound ---
            TriggerFlash();
            if (parentButton != null)
            {
                parentButton.SetButtonDepressed(true);
            }
            onImpact?.Invoke();

            // --- Phase 4: Tactile Hold & Micro-Shudder ("jeda dulu agar berasa") ---
            float holdElapsed = 0f;
            while (holdElapsed < holdDuration)
            {
                holdElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(holdElapsed / holdDuration);

                // Subtle mechanical micro-shudder during the first 60% of hold
                if (t < 0.60f)
                {
                    float decay = 1f - (t / 0.60f);
                    float microShakeX = Mathf.Sin(holdElapsed * 50f) * 1.5f * decay;
                    float microShakeY = Mathf.Cos(holdElapsed * 60f) * 1.0f * decay;
                    rectTransform.anchoredPosition = deepPressedPos + new Vector2(microShakeX, microShakeY);
                    rectTransform.localScale = pressSquashScale;
                }
                else
                {
                    // Latch release: start smooth spring rebound
                    float popT = (t - 0.60f) / 0.40f;
                    float popEase = 1f - Mathf.Pow(1f - popT, 2.5f);

                    rectTransform.anchoredPosition = Vector2.Lerp(deepPressedPos, basePos, popEase);
                    rectTransform.localScale = Vector3.Lerp(pressSquashScale, hoverScale, popEase);

                    if (parentButton != null)
                    {
                        parentButton.SetButtonDepressed(false);
                    }
                }

                yield return null;
            }

            // Ensure clean final rest position before transition
            rectTransform.anchoredPosition = basePos;
            rectTransform.localScale = hoverScale;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);

            if (parentButton != null)
            {
                parentButton.SetButtonDepressed(false);
            }

            // --- Phase 5: Handoff to Screen Transition ("lalu baru pindah") ---
            onTransitionReady?.Invoke();

            _isPressing = false;
            _pressRoutine = null;
        }

        private void TriggerFlash()
        {
            if (flashOverlay == null) return;

            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            flashOverlay.gameObject.SetActive(true);
            Color flashCol = Color.white;
            flashOverlay.color = new Color(flashCol.r, flashCol.g, flashCol.b, 0.95f);

            float elapsed = 0f;
            float duration = 0.12f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(0.95f, 0f, t);
                flashOverlay.color = new Color(flashCol.r, flashCol.g, flashCol.b, a);
                yield return null;
            }

            flashOverlay.gameObject.SetActive(false);
            _flashRoutine = null;
        }

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
            if (locked && _activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }
        }

        public void ResetToHome()
        {
            if (_isPressing) return;

            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }
            if (_pressRoutine != null)
            {
                StopCoroutine(_pressRoutine);
                _pressRoutine = null;
            }
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (flashOverlay != null)
            {
                flashOverlay.gameObject.SetActive(false);
            }

            if (parentButton != null)
            {
                parentButton.SetButtonDepressed(false);
            }

            _isPressing = false;
            _isHovered = false;
            _currentBasePos = homePosition;

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = homePosition;
                rectTransform.localScale = homeScale;
                rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);
            }
        }
    }
}
