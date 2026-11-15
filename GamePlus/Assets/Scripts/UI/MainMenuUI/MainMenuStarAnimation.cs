using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenuUI
{
    /// <summary>
    /// Animates the Title Screen button's star mascot.
    /// Provides:
    /// 1. Lively organic idle animation (bobbing, breathing scale, subtle wobble).
    /// 2. Smooth comic hover glide from left home position to button center with arc and squash/stretch.
    /// 3. Energetic hover idle bobbing while waiting in the center.
    /// 4. Dynamic comic slam / stamp click effect where the star physically stomps down onto the button.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MainMenuStarAnimation : MonoBehaviour
    {
        [Header("References")]
        public RectTransform rectTransform;
        public Image starImage;
        public Image flashOverlay;
        public MainMenuUIButton parentButton;

        [Header("Positions & Anchors (Relative to Button Center)")]
        [Tooltip("Resting position at the left end of the button.")]
        public Vector2 homePosition = new Vector2(-261.33f, -13.72f);

        [Tooltip("Target position when hovered (center along slanted bar).")]
        public Vector2 centerPosition = new Vector2(0f, -4f);

        [Tooltip("Slant angle of the comic button (degrees).")]
        public float baseRotationAngle = 2.0f;

        [Header("Scales")]
        public Vector3 homeScale = Vector3.one;
        public Vector3 hoverScale = new Vector3(1.18f, 1.18f, 1f);

        [Header("Idle Animation (When at Home)")]
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

        [Header("Click Slam ('Star Clicks the Button')")]
        public float windupDuration = 0.06f;
        public float windupLiftY = 24f;
        public Vector3 windupScale = new Vector3(0.82f, 1.28f, 1f);

        public float slamDuration = 0.05f;
        public float slamDropY = -14f;
        public Vector3 slamSquashScale = new Vector3(1.42f, 0.62f, 1f);

        public float reboundDuration = 0.15f;
        public Vector3 reboundBounceScale = new Vector3(1.18f, 1.18f, 1f);

        // State tracking
        private bool _isHovered = false;
        private bool _isSlamming = false;
        private bool _isLocked = false;
        private Vector2 _currentBasePos;
        private Coroutine _activeTransitionRoutine;
        private Coroutine _slamRoutine;
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
            _isSlamming = false;
            _isLocked = false;
            _isHovered = false;
            _currentBasePos = homePosition;
            ResetToHome();
        }

        private void Update()
        {
            // Only apply ambient bobbing/breathing when not actively slamming or transitioning
            if (_isSlamming || _activeTransitionRoutine != null) return;

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
            if (_isLocked || _isSlamming) return;
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

                // Smooth comic ease-out back curve for landing
                float ease = 1f - Mathf.Pow(1f - t, 2.8f);

                // Upward hop arc while travelling
                float arc = Mathf.Sin(t * Mathf.PI) * hoverArcHeight;

                // Position interpolation with arc
                Vector2 curPos = Vector2.Lerp(startPos, targetPos, ease);
                curPos.y += arc;
                rectTransform.anchoredPosition = curPos;

                // Scale interpolation with subtle squash/stretch on takeoff/landing
                float stretchX = 1f;
                float stretchY = 1f;
                if (toCenter)
                {
                    // Stretch along flight direction then cushion squash
                    if (t < 0.5f)
                    {
                        float midT = t / 0.5f;
                        stretchX = Mathf.Lerp(1f, 1.15f, midT);
                        stretchY = Mathf.Lerp(1f, 0.90f, midT);
                    }
                    else
                    {
                        float settleT = (t - 0.5f) / 0.5f;
                        stretchX = Mathf.Lerp(1.15f, 1f, settleT);
                        stretchY = Mathf.Lerp(0.90f, 1f, settleT);
                    }
                }
                Vector3 currentTargetScale = Vector3.Lerp(startScale, targetScale, ease);
                rectTransform.localScale = new Vector3(currentTargetScale.x * stretchX, currentTargetScale.y * stretchY, 1f);

                // Dynamic comic tilt during movement
                float targetAngle = baseRotationAngle;
                if (toCenter)
                {
                    // Lean into movement, overshoot, and settle
                    float lean = Mathf.Sin(t * Mathf.PI) * hoverLeanAngle;
                    rectTransform.localEulerAngles = new Vector3(0f, 0f, Mathf.Lerp(startAngle, targetAngle, ease) + lean);
                }
                else
                {
                    // Return tilt
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
        /// Executes the dynamic comic click slam where the star physically stomps the button down.
        /// </summary>
        public void PlayClickSlam(Action onImpact = null, Action onComplete = null)
        {
            if (_isSlamming) return;

            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }
            if (_slamRoutine != null)
            {
                StopCoroutine(_slamRoutine);
            }

            _isSlamming = true;
            _isLocked = true;
            _slamRoutine = StartCoroutine(ClickSlamRoutine(onImpact, onComplete));
        }

        private IEnumerator ClickSlamRoutine(Action onImpact, Action onComplete)
        {
            // 0. Quick pre-position: If not already near center (e.g. keyboard press while not hovered),
            // dash to center fast first so player sees the star smash it!
            if (Vector2.Distance(rectTransform.anchoredPosition, centerPosition) > 50f)
            {
                float dashElapsed = 0f;
                float dashDuration = 0.08f;
                Vector2 fromPos = rectTransform.anchoredPosition;
                Vector3 fromScale = rectTransform.localScale;

                while (dashElapsed < dashDuration)
                {
                    dashElapsed += Time.unscaledDeltaTime;
                    float dt = Mathf.Clamp01(dashElapsed / dashDuration);
                    rectTransform.anchoredPosition = Vector2.Lerp(fromPos, centerPosition, dt);
                    rectTransform.localScale = Vector3.Lerp(fromScale, hoverScale, dt);
                    yield return null;
                }
            }

            _currentBasePos = centerPosition;

            // Phase 1: Wind-up (Anticipation hop & vertical stretch)
            Vector2 basePos = centerPosition;
            Vector2 windupPos = basePos + new Vector2(0f, windupLiftY);
            Vector3 startScale = hoverScale;
            float elapsed = 0f;

            while (elapsed < windupDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / windupDuration);
                float easeIn = t * t; // Acceleration upward

                rectTransform.anchoredPosition = Vector2.Lerp(basePos, windupPos, easeIn);
                rectTransform.localScale = Vector3.Lerp(startScale, windupScale, easeIn);
                rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle - (10f * easeIn));
                yield return null;
            }

            // Phase 2: Downward Slam (Lightning speed downward stomp onto button)
            elapsed = 0f;
            Vector2 slamPos = basePos + new Vector2(0f, slamDropY);

            while (elapsed < slamDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slamDuration);
                float easeDown = t * t * t; // Snappy downward plunge

                rectTransform.anchoredPosition = Vector2.Lerp(windupPos, slamPos, easeDown);
                rectTransform.localScale = Vector3.Lerp(windupScale, slamSquashScale, easeDown);
                rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);
                yield return null;
            }

            // Impact Moment! Flash, button physical depression, and impact callback
            TriggerFlash();
            onImpact?.Invoke();

            // Phase 3: Elastic Rebound / Settle
            elapsed = 0f;
            Vector3 postImpactScale = slamSquashScale;

            while (elapsed < reboundDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / reboundDuration);

                // Comic spring settle
                float spring = Mathf.Sin(t * Mathf.PI * 1.5f);
                float easeOut = 1f - Mathf.Pow(1f - t, 2.5f);

                rectTransform.anchoredPosition = Vector2.Lerp(slamPos, basePos, easeOut);

                // Bounce from squash -> overshoot bounce -> settle
                if (t < 0.45f)
                {
                    float bounceT = t / 0.45f;
                    rectTransform.localScale = Vector3.Lerp(postImpactScale, reboundBounceScale, bounceT);
                }
                else
                {
                    float settleT = (t - 0.45f) / 0.55f;
                    rectTransform.localScale = Vector3.Lerp(reboundBounceScale, hoverScale, settleT);
                }

                yield return null;
            }

            rectTransform.anchoredPosition = basePos;
            rectTransform.localScale = hoverScale;
            rectTransform.localEulerAngles = new Vector3(0f, 0f, baseRotationAngle);

            _isSlamming = false;
            _slamRoutine = null;
            onComplete?.Invoke();
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
            flashOverlay.color = new Color(flashCol.r, flashCol.g, flashCol.b, 0.9f);

            float elapsed = 0f;
            float duration = 0.09f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(0.9f, 0f, t);
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
            if (_activeTransitionRoutine != null)
            {
                StopCoroutine(_activeTransitionRoutine);
                _activeTransitionRoutine = null;
            }
            if (_slamRoutine != null)
            {
                StopCoroutine(_slamRoutine);
                _slamRoutine = null;
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

            _isSlamming = false;
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
