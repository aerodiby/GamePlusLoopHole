using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenuUI
{
    /// <summary>
    /// Adds a clean, simple, and stylish sparkling/glint effect to Title Screen slash stars (Slash_1 and Slash_2).
    /// Features:
    /// 1. Gentle ambient breathing and subtle tilt sway.
    /// 2. Periodic sparkle twinkle (quick scale squeeze, bloom, glint flare spin, and spring settle).
    /// 3. Alternating/staggered timing so Slash_1 and Slash_2 sparkle across the title rhythmically.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MainMenuSlashSparkle : MonoBehaviour
    {
        [Header("References")]
        public RectTransform rectTransform;
        public Image mainImage;
        public Image glintImage;

        [Header("Ambient Motion")]
        public bool enableBreathing = true;
        public float breatheSpeed = 2.0f;
        public float breatheAmount = 0.035f;
        public float swaySpeed = 1.5f;
        public float swayAngle = 1.8f;
        [Tooltip("Time offset so multiple stars do not breathe or sparkle identically in unison.")]
        public float phaseOffset = 0.0f;

        [Header("Sparkle / Twinkle Settings")]
        public bool enableSparkle = true;
        public float sparkleInterval = 2.8f;
        public float intervalVariance = 0.4f;
        public float sparkleDuration = 0.38f;
        public float starPopScale = 1.15f;
        public float glintSize = 400f;
        public float glintSpinAngle = 40f;

        private Vector3 _baseScale;
        private Vector3 _baseRotation;
        private Coroutine _sparkleLoopRoutine;
        private Coroutine _activeSparkleRoutine;

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }
            if (mainImage == null)
            {
                mainImage = GetComponent<Image>();
            }

            _baseScale = rectTransform.localScale;
            _baseRotation = rectTransform.localEulerAngles;

            SetupGlintChild();
        }

        private void OnEnable()
        {
            _baseScale = rectTransform.localScale;
            _baseRotation = rectTransform.localEulerAngles;

            if (glintImage != null)
            {
                glintImage.gameObject.SetActive(false);
            }

            if (_sparkleLoopRoutine != null)
            {
                StopCoroutine(_sparkleLoopRoutine);
            }
            _sparkleLoopRoutine = StartCoroutine(SparkleLoopRoutine());
        }

        private void OnDisable()
        {
            if (_sparkleLoopRoutine != null)
            {
                StopCoroutine(_sparkleLoopRoutine);
                _sparkleLoopRoutine = null;
            }
            if (_activeSparkleRoutine != null)
            {
                StopCoroutine(_activeSparkleRoutine);
                _activeSparkleRoutine = null;
            }
            if (glintImage != null)
            {
                glintImage.gameObject.SetActive(false);
            }
            rectTransform.localScale = _baseScale;
            rectTransform.localEulerAngles = _baseRotation;
        }

        private void SetupGlintChild()
        {
            if (glintImage != null) return;

            Transform child = transform.Find("SparkleGlint");
            if (child != null)
            {
                glintImage = child.GetComponent<Image>();
                return;
            }

            // Create SparkleGlint child
            GameObject glintObj = new GameObject("SparkleGlint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glintObj.transform.SetParent(transform, false);

            var rt = glintObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(glintSize, glintSize);

            // Counteract parent negative scale if applicable so the glint remains upright
            float signX = Mathf.Sign(_baseScale.x);
            float signY = Mathf.Sign(_baseScale.y);
            rt.localScale = new Vector3(signX < 0 ? -1f : 1f, signY < 0 ? -1f : 1f, 1f);

            glintImage = glintObj.GetComponent<Image>();
            glintImage.raycastTarget = false;
            glintImage.color = new Color(1f, 1f, 1f, 0f);

#if UNITY_EDITOR
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Packages/com.unity.render-pipelines.universal/Textures/2D/Sparkle.png");
            if (sprite != null) glintImage.sprite = sprite;
#endif

            glintObj.SetActive(false);
        }

        private void Update()
        {
            if (!enableBreathing) return;

            // When a sparkle event is playing, the sparkle routine takes priority over scale/rot
            if (_activeSparkleRoutine != null) return;

            float time = Time.unscaledTime + phaseOffset;
            float breathe = 1f + Mathf.Sin(time * breatheSpeed) * breatheAmount;
            float sway = Mathf.Sin(time * swaySpeed) * swayAngle;

            rectTransform.localScale = _baseScale * breathe;
            rectTransform.localEulerAngles = _baseRotation + new Vector3(0f, 0f, sway);
        }

        private IEnumerator SparkleLoopRoutine()
        {
            // Initial phase stagger
            if (phaseOffset > 0f)
            {
                yield return new WaitForSecondsRealtime(phaseOffset);
            }

            while (true)
            {
                float waitTime = sparkleInterval + Random.Range(-intervalVariance, intervalVariance);
                if (waitTime < 0.5f) waitTime = 0.5f;

                yield return new WaitForSecondsRealtime(waitTime);

                if (enableSparkle && gameObject.activeInHierarchy)
                {
                    if (_activeSparkleRoutine != null)
                    {
                        StopCoroutine(_activeSparkleRoutine);
                    }
                    _activeSparkleRoutine = StartCoroutine(SparkleTwinkleRoutine());
                }
            }
        }

        private IEnumerator SparkleTwinkleRoutine()
        {
            if (glintImage != null)
            {
                glintImage.gameObject.SetActive(true);
            }

            float signX = Mathf.Sign(_baseScale.x);
            float signY = Mathf.Sign(_baseScale.y);
            Vector3 uprightGlintScale = new Vector3(signX < 0 ? -1f : 1f, signY < 0 ? -1f : 1f, 1f);
            RectTransform glintRT = glintImage != null ? glintImage.rectTransform : null;

            Color baseImgColor = mainImage != null ? mainImage.color : Color.white;
            float elapsed = 0f;

            while (elapsed < sparkleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / sparkleDuration);

                // --- 1. Star Main Scale: Inhale (anticipation) -> Pop -> Spring settle ---
                float scaleMul;
                if (t < 0.20f)
                {
                    // Anticipation dip
                    float dipT = t / 0.20f;
                    scaleMul = Mathf.Lerp(1f, 0.94f, dipT);
                }
                else if (t < 0.50f)
                {
                    // Pop bloom
                    float popT = (t - 0.20f) / 0.30f;
                    float easeOut = 1f - Mathf.Pow(1f - popT, 2.5f);
                    scaleMul = Mathf.Lerp(0.94f, starPopScale, easeOut);
                }
                else
                {
                    // Spring cushion back to 1.0
                    float settleT = (t - 0.50f) / 0.50f;
                    float spring = Mathf.Sin(settleT * Mathf.PI * 1.5f);
                    scaleMul = Mathf.Lerp(starPopScale, 1f, settleT) + (spring * 0.03f * (1f - settleT));
                }
                rectTransform.localScale = _baseScale * scaleMul;

                // --- 2. Glint Flare Animation (Expand, Spin 45 deg, Fade) ---
                if (glintRT != null && glintImage != null)
                {
                    float glintScale;
                    float glintAlpha;

                    if (t < 0.40f)
                    {
                        // Rapid flare expansion
                        float inT = t / 0.40f;
                        float ease = 1f - Mathf.Pow(1f - inT, 2f);
                        glintScale = Mathf.Lerp(0f, 1.15f, ease);
                        glintAlpha = Mathf.Lerp(0f, 0.95f, ease);
                    }
                    else
                    {
                        // Smooth dissipation
                        float outT = (t - 0.40f) / 0.60f;
                        glintScale = Mathf.Lerp(1.15f, 0.4f, outT);
                        glintAlpha = Mathf.Lerp(0.95f, 0f, outT * outT);
                    }

                    glintRT.localScale = uprightGlintScale * glintScale;
                    glintRT.localEulerAngles = new Vector3(0f, 0f, t * glintSpinAngle);
                    glintImage.color = new Color(1f, 1f, 1f, glintAlpha);
                }

                yield return null;
            }

            // Cleanup sparkle
            if (glintImage != null)
            {
                glintImage.gameObject.SetActive(false);
            }
            if (mainImage != null)
            {
                mainImage.color = baseImgColor;
            }

            rectTransform.localScale = _baseScale;
            rectTransform.localEulerAngles = _baseRotation;
            _activeSparkleRoutine = null;
        }

        public void TriggerManualSparkle()
        {
            if (_activeSparkleRoutine != null)
            {
                StopCoroutine(_activeSparkleRoutine);
            }
            _activeSparkleRoutine = StartCoroutine(SparkleTwinkleRoutine());
        }
    }
}
