using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI.MainMenuUI
{
    /// <summary>
    /// Master transition controller for the Hi-Fi Rush comic UI.
    /// Features:
    /// - Smooth, natural choreography with continuous acceleration/deceleration.
    /// - Semi-transparent speedline intro & outro with deep dark comic peak in the middle.
    /// - 100% Canvas coverage (no 3D scene/skybox leaks).
    /// - Fluid "Naik ke Atas" (Ascending) and "Turun ke Bawah" (Descending) transitions.
    /// </summary>
    public class MainMenuUITransition : MonoBehaviour
    {
        [Header("Transition Speed & Timing Settings")]
        [Tooltip("Overall transition speed multiplier. 1.0 = normal, 1.5 = faster, 0.7 = slower.")]
        [Range(0.2f, 3.0f)]
        public float transitionSpeed = 1.0f;

        [Tooltip("Duration of the ascending launch transition in seconds (at 1x speed).")]
        public float diveDuration = 0.65f;

        [Tooltip("Duration of the descending return transition in seconds (at 1x speed).")]
        public float backDuration = 0.60f;

        [Tooltip("Animation speed (FPS) for cycling through the Parallel V speedline frames.")]
        [Range(10f, 60f)]
        public float speedLineFps = 35f;

        [Header("Title Screen References")]
        public RectTransform titleTransform;
        public CanvasGroup titleGroup;
        public RectTransform buttonTransform;
        public CanvasGroup buttonGroup;
        public RectTransform backgroundTransform;

        [Header("Speed Lines Overlay & Dark Backdrop")]
        public GameObject speedLinesObject;
        public Image speedLinesImage;
        public CanvasGroup speedLinesGroup;
        public Image darkBackdrop;
        public Sprite[] speedLineSprites;

        [Header("Menu Option Screen")]
        public GameObject menuOptionScreenObject;
        public CanvasGroup menuOptionGroup;
        public RectTransform menuOptionTransform;
        public Button backButton;

        [Header("Audio")]
        public MainMenuUIAudioSynthesizer audioSynth;

        [Header("State")]
        public bool isInOptionScreen = false;
        public bool IsTransitioning => _isTransitioning;
        private bool _isTransitioning = false;

        // Cached original transforms
        private Vector3 _titleInitialScale = Vector3.one;
        private Vector2 _titleInitialPos = Vector2.zero;

        private Vector3 _buttonInitialScale = Vector3.one;
        private Vector2 _buttonInitialPos = Vector2.zero;

        private Vector3 _bgInitialScale = Vector3.one;
        private Vector2 _bgInitialPos = Vector2.zero;

        private Vector2 _menuOptionInitialPos = Vector2.zero;

        private void Awake()
        {
            if (titleTransform != null)
            {
                _titleInitialScale = titleTransform.localScale;
                _titleInitialPos = titleTransform.anchoredPosition;
            }

            if (buttonTransform != null)
            {
                _buttonInitialScale = buttonTransform.localScale;
                _buttonInitialPos = buttonTransform.anchoredPosition;
            }

            if (backgroundTransform != null)
            {
                _bgInitialScale = backgroundTransform.localScale;
                _bgInitialPos = backgroundTransform.anchoredPosition;
            }

            if (menuOptionScreenObject != null)
            {
                if (menuOptionTransform == null)
                    menuOptionTransform = menuOptionScreenObject.GetComponent<RectTransform>();

                if (menuOptionTransform != null)
                    _menuOptionInitialPos = menuOptionTransform.anchoredPosition;

                menuOptionScreenObject.SetActive(false);
                if (menuOptionGroup != null) menuOptionGroup.alpha = 0f;
            }

            SetupSpeedLinesAndBackdrop();
            HookBackButton();
        }

        private void Start()
        {
            HookBackButton();
        }

        private void SetupSpeedLinesAndBackdrop()
        {
            if (speedLinesObject == null) return;

            if (speedLinesGroup == null)
                speedLinesGroup = speedLinesObject.GetComponent<CanvasGroup>();

            // Find or setup dark backdrop
            if (darkBackdrop == null)
            {
                Transform dbTrans = speedLinesObject.transform.Find("DarkBackdrop");
                if (dbTrans != null)
                {
                    darkBackdrop = dbTrans.GetComponent<Image>();
                }
            }

            // If not found, create DarkBackdrop as first child (drawn behind speed lines)
            if (darkBackdrop == null)
            {
                GameObject dbObj = new GameObject("DarkBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dbObj.transform.SetParent(speedLinesObject.transform, false);
                dbObj.transform.SetAsFirstSibling();

                RectTransform rt = dbObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                darkBackdrop = dbObj.GetComponent<Image>();
                darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, 0f);
                darkBackdrop.raycastTarget = false;
            }

            if (speedLinesImage == null)
            {
                Transform slTrans = speedLinesObject.transform.Find("SpeedLines");
                if (slTrans != null)
                    speedLinesImage = slTrans.GetComponent<Image>();
                else
                    speedLinesImage = speedLinesObject.GetComponent<Image>();
            }

            speedLinesObject.SetActive(false);
            if (speedLinesGroup != null) speedLinesGroup.alpha = 0f;
            if (darkBackdrop != null) darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, 0f);
        }

        private void HookBackButton()
        {
            // Only hook if an explicit "Button_Back" child exists.
            // DO NOT use GetComponentInChildren<Button> as it will accidentally grab the first menu option button (e.g. Play)!
            if (backButton == null && menuOptionScreenObject != null)
            {
                Transform bb = menuOptionScreenObject.transform.Find("Button_Back");
                if (bb != null)
                {
                    backButton = bb.GetComponent<Button>();
                }
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(StartTransitionBackToTitle);
                backButton.onClick.AddListener(StartTransitionBackToTitle);
            }
        }

        private void Update()
        {
            if (_isTransitioning) return;

            if (isInOptionScreen)
            {
                if (CheckCancelInput())
                {
                    StartTransitionBackToTitle();
                }
            }
        }

        private bool CheckCancelInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame || 
                                           Gamepad.current.selectButton.wasPressedThisFrame ||
                                           Gamepad.current.bButton.wasPressedThisFrame)) return true;
            return false;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public void StartTransitionToOption()
        {
            if (isInOptionScreen || _isTransitioning) return;
            StartCoroutine(TransitionToOptionRoutine());
        }

        /// <summary>
        /// Smooth & Natural Ascending Transition (Title -> Menu Option):
        /// - Semi-transparent at start (speed lines streak across dimmed UI).
        /// - Dark comic contrast in the middle (full canvas coverage, no skybox leakage).
        /// - Smoothly returns to semi-transparent as Menu Option screen settles from above.
        /// </summary>
        private IEnumerator TransitionToOptionRoutine()
        {
            _isTransitioning = true;

            float speedMult = Mathf.Max(0.05f, transitionSpeed);
            float duration = diveDuration / speedMult;

            // 1. Initial click punch & sound
            if (audioSynth != null) audioSynth.PlayConfirm();
            if (buttonTransform != null) buttonTransform.localScale = _buttonInitialScale * 0.94f;

            yield return new WaitForSecondsRealtime(0.04f / speedMult);

            if (audioSynth != null) audioSynth.PlayAscendWhoosh();

            // 2. Activate SpeedLines overlay & initialize backdrop
            if (speedLinesObject != null)
            {
                speedLinesObject.SetActive(true);
                if (speedLinesGroup != null) speedLinesGroup.alpha = 0f;
            }
            if (darkBackdrop != null)
            {
                darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, 0f);
            }

            float elapsed = 0f;
            int totalSprites = (speedLineSprites != null) ? speedLineSprites.Length : 0;
            RectTransform speedLinesRect = speedLinesImage != null ? speedLinesImage.rectTransform : null;

            // Unified, continuous smooth timeline
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);

                // A. Overall SpeedLines Group Alpha
                if (speedLinesGroup != null)
                {
                    float slAlpha;
                    if (p < 0.18f)
                        slAlpha = Mathf.SmoothStep(0f, 1f, p / 0.18f);
                    else if (p < 0.82f)
                        slAlpha = 1f;
                    else
                        slAlpha = Mathf.SmoothStep(1f, 0f, (p - 0.82f) / 0.18f);

                    speedLinesGroup.alpha = slAlpha;
                }

                // B. Dark Backdrop: Semi-transparent at start & end, dark in the middle
                if (darkBackdrop != null)
                {
                    float darkAlpha;
                    if (p < 0.35f)
                    {
                        // Awal: smooth rise from 0 to 0.98 (passes 0.40 semi-transparent at ~0.15)
                        darkAlpha = Mathf.SmoothStep(0f, 0.98f, p / 0.35f);
                    }
                    else if (p < 0.65f)
                    {
                        // Tengah: peak dark comic coverage
                        darkAlpha = 0.98f;
                    }
                    else
                    {
                        // Akhir: smooth fall from 0.98 back through 0.40 semi-transparent down to 0
                        darkAlpha = Mathf.SmoothStep(0.98f, 0f, (p - 0.65f) / 0.35f);
                    }
                    darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, darkAlpha);
                }

                // C. Background Parallax (scales safely to 1.18x and drifts down 45px - 100% full coverage, zero gaps)
                if (backgroundTransform != null)
                {
                    float bgT = Mathf.SmoothStep(0f, 1f, p);
                    backgroundTransform.localScale = Vector3.Lerp(_bgInitialScale, _bgInitialScale * 1.18f, bgT);
                    backgroundTransform.anchoredPosition = Vector2.Lerp(_bgInitialPos, new Vector2(_bgInitialPos.x, _bgInitialPos.y - 45f), bgT);
                }

                // D. Title & Button: Smoothly glide down and fade out during first 38%
                if (p < 0.38f)
                {
                    float outT = Mathf.SmoothStep(0f, 1f, p / 0.38f);
                    if (titleTransform != null)
                    {
                        titleTransform.anchoredPosition = Vector2.Lerp(_titleInitialPos, new Vector2(_titleInitialPos.x, _titleInitialPos.y - 450f), outT);
                        if (titleGroup != null) titleGroup.alpha = 1f - outT;
                    }
                    if (buttonTransform != null)
                    {
                        buttonTransform.anchoredPosition = Vector2.Lerp(_buttonInitialPos, new Vector2(_buttonInitialPos.x, _buttonInitialPos.y - 400f), outT);
                        if (buttonGroup != null) buttonGroup.alpha = 1f - outT;
                    }
                }
                else
                {
                    if (titleTransform != null && titleTransform.gameObject.activeSelf)
                        titleTransform.gameObject.SetActive(false);
                    if (buttonTransform != null && buttonTransform.gameObject.activeSelf)
                        buttonTransform.gameObject.SetActive(false);
                }

                // E. MenuOptionScreen: Smoothly descends from +240px and settles naturally
                if (p >= 0.55f)
                {
                    if (menuOptionScreenObject != null && !menuOptionScreenObject.activeSelf)
                        menuOptionScreenObject.SetActive(true);

                    float inT = (p - 0.55f) / 0.45f;
                    float easeOut = 1f - Mathf.Pow(1f - inT, 2.8f);

                    if (menuOptionTransform != null)
                    {
                        menuOptionTransform.anchoredPosition = Vector2.Lerp(new Vector2(_menuOptionInitialPos.x, _menuOptionInitialPos.y + 240f), _menuOptionInitialPos, easeOut);
                    }
                    if (menuOptionGroup != null)
                    {
                        menuOptionGroup.alpha = Mathf.SmoothStep(0f, 1f, inT);
                    }
                }

                // F. Speedlines animation frame cycling
                if (totalSprites > 0 && speedLinesImage != null)
                {
                    int frameIndex = Mathf.FloorToInt(elapsed * speedLineFps * speedMult) % totalSprites;
                    speedLinesImage.sprite = speedLineSprites[frameIndex];
                }

                // G. Smooth organic vertical drift
                if (speedLinesRect != null)
                {
                    float driftY = Mathf.Sin(p * Mathf.PI * 4f) * 16f;
                    speedLinesRect.anchoredPosition = new Vector2(0f, driftY);
                }

                yield return null;
            }

            // Final cleanup
            if (speedLinesObject != null) speedLinesObject.SetActive(false);
            if (darkBackdrop != null) darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, 0f);
            if (speedLinesRect != null) speedLinesRect.anchoredPosition = Vector2.zero;

            if (menuOptionScreenObject != null) menuOptionScreenObject.SetActive(true);
            if (menuOptionGroup != null) menuOptionGroup.alpha = 1f;
            if (menuOptionTransform != null) menuOptionTransform.anchoredPosition = _menuOptionInitialPos;

            isInOptionScreen = true;
            _isTransitioning = false;
        }

        public void StartTransitionBackToTitle()
        {
            if (!isInOptionScreen || _isTransitioning) return;
            StartCoroutine(TransitionBackToTitleRoutine());
        }

        /// <summary>
        /// Smooth & Natural Descending Transition (Menu Option -> Title):
        /// - Semi-transparent at start as Menu Option screen lifts up.
        /// - Dark comic contrast in the middle as camera descends through speedlines.
        /// - Smoothly returns to semi-transparent as Title & Button settle with a gentle cushion.
        /// </summary>
        private IEnumerator TransitionBackToTitleRoutine()
        {
            _isTransitioning = true;
            if (audioSynth != null) audioSynth.PlayCancel();

            float speedMult = Mathf.Max(0.05f, transitionSpeed);
            float duration = backDuration / speedMult;

            if (audioSynth != null) audioSynth.PlayDescendWhoosh();

            // 1. Activate SpeedLines overlay & initialize backdrop
            if (speedLinesObject != null)
            {
                speedLinesObject.SetActive(true);
                if (speedLinesGroup != null) speedLinesGroup.alpha = 0f;
            }
            if (darkBackdrop != null)
            {
                darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, 0f);
            }

            float elapsed = 0f;
            int totalSprites = (speedLineSprites != null) ? speedLineSprites.Length : 0;
            RectTransform speedLinesRect = speedLinesImage != null ? speedLinesImage.rectTransform : null;

            bool landingSoundPlayed = false;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = Mathf.Clamp01(elapsed / duration);

                // A. Overall SpeedLines Group Alpha
                if (speedLinesGroup != null)
                {
                    float slAlpha;
                    if (p < 0.18f)
                        slAlpha = Mathf.SmoothStep(0f, 1f, p / 0.18f);
                    else if (p < 0.80f)
                        slAlpha = 1f;
                    else
                        slAlpha = Mathf.SmoothStep(1f, 0f, (p - 0.80f) / 0.20f);

                    speedLinesGroup.alpha = slAlpha;
                }

                // B. Dark Backdrop: Semi-transparent at start & end, dark in the middle
                if (darkBackdrop != null)
                {
                    float darkAlpha;
                    if (p < 0.35f)
                    {
                        darkAlpha = Mathf.SmoothStep(0f, 0.98f, p / 0.35f);
                    }
                    else if (p < 0.65f)
                    {
                        darkAlpha = 0.98f;
                    }
                    else
                    {
                        darkAlpha = Mathf.SmoothStep(0.98f, 0f, (p - 0.65f) / 0.35f);
                    }
                    darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, darkAlpha);
                }

                // C. MenuOptionScreen: Smoothly glides up (+320px) and fades out during first 38%
                if (p < 0.38f)
                {
                    float outT = Mathf.SmoothStep(0f, 1f, p / 0.38f);
                    if (menuOptionTransform != null)
                    {
                        menuOptionTransform.anchoredPosition = Vector2.Lerp(_menuOptionInitialPos, new Vector2(_menuOptionInitialPos.x, _menuOptionInitialPos.y + 320f), outT);
                    }
                    if (menuOptionGroup != null)
                    {
                        menuOptionGroup.alpha = 1f - outT;
                    }
                }
                else
                {
                    if (menuOptionScreenObject != null && menuOptionScreenObject.activeSelf)
                        menuOptionScreenObject.SetActive(false);
                }

                // D. Background Parallax Return (from 1.18x/-45px back to 1.0x/0px)
                if (backgroundTransform != null)
                {
                    float bgT = Mathf.SmoothStep(0f, 1f, p);
                    backgroundTransform.localScale = Vector3.Lerp(_bgInitialScale * 1.18f, _bgInitialScale, bgT);
                    backgroundTransform.anchoredPosition = Vector2.Lerp(new Vector2(_bgInitialPos.x, _bgInitialPos.y - 45f), _bgInitialPos, bgT);
                }

                // E. Title & Button: Descend from +240px and settle with natural comic cushion
                if (p >= 0.55f)
                {
                    if (titleTransform != null && !titleTransform.gameObject.activeSelf)
                    {
                        titleTransform.gameObject.SetActive(true);
                        if (buttonTransform != null) buttonTransform.gameObject.SetActive(true);
                    }

                    float inT = (p - 0.55f) / 0.45f;
                    float easeOut = 1f - Mathf.Pow(1f - inT, 2.6f);

                    // Gentle cushion settle
                    float cushion = (inT < 0.75f) ? 0f : Mathf.Sin((inT - 0.75f) / 0.25f * Mathf.PI) * -8f;

                    if (titleTransform != null)
                    {
                        titleTransform.anchoredPosition = Vector2.Lerp(new Vector2(_titleInitialPos.x, _titleInitialPos.y + 240f), _titleInitialPos, easeOut) + new Vector2(0f, cushion);
                        if (titleGroup != null) titleGroup.alpha = Mathf.SmoothStep(0f, 1f, inT);
                    }

                    if (buttonTransform != null)
                    {
                        buttonTransform.anchoredPosition = Vector2.Lerp(new Vector2(_buttonInitialPos.x, _buttonInitialPos.y + 200f), _buttonInitialPos, easeOut) + new Vector2(0f, cushion * 0.7f);
                        if (buttonGroup != null) buttonGroup.alpha = Mathf.SmoothStep(0f, 1f, inT);
                    }

                    if (inT >= 0.80f && !landingSoundPlayed)
                    {
                        landingSoundPlayed = true;
                        if (audioSynth != null) audioSynth.PlayLandingImpact();
                    }
                }

                // F. Speedlines animation frame cycling in reverse
                if (totalSprites > 0 && speedLinesImage != null)
                {
                    int frameIndex = Mathf.FloorToInt((duration - elapsed) * speedLineFps * speedMult) % totalSprites;
                    if (frameIndex < 0) frameIndex += totalSprites;
                    speedLinesImage.sprite = speedLineSprites[frameIndex];
                }

                // G. Smooth organic vertical drift
                if (speedLinesRect != null)
                {
                    float driftY = Mathf.Sin(p * Mathf.PI * 4f) * -16f;
                    speedLinesRect.anchoredPosition = new Vector2(0f, driftY);
                }

                yield return null;
            }

            // Final cleanup
            if (speedLinesObject != null) speedLinesObject.SetActive(false);
            if (darkBackdrop != null) darkBackdrop.color = new Color(0.04f, 0.05f, 0.09f, 0f);
            if (speedLinesRect != null) speedLinesRect.anchoredPosition = Vector2.zero;

            if (backgroundTransform != null)
            {
                backgroundTransform.localScale = _bgInitialScale;
                backgroundTransform.anchoredPosition = _bgInitialPos;
            }

            if (titleTransform != null)
            {
                titleTransform.localScale = _titleInitialScale;
                titleTransform.anchoredPosition = _titleInitialPos;
                titleTransform.gameObject.SetActive(true);
            }
            if (titleGroup != null) titleGroup.alpha = 1f;

            if (buttonTransform != null)
            {
                buttonTransform.localScale = _buttonInitialScale;
                buttonTransform.anchoredPosition = _buttonInitialPos;
                buttonTransform.gameObject.SetActive(true);
            }
            if (buttonGroup != null) buttonGroup.alpha = 1f;

            isInOptionScreen = false;
            _isTransitioning = false;
        }
    }
}
