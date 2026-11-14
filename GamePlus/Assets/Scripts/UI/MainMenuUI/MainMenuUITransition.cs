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
    /// Handles high-octane "Naik ke Atas" (Ascending) launch into the Option Screen,
    /// and dynamic "Turun ke Bawah" (Descending Slam) return to the Title Screen.
    /// Supports transparent blue Parallel V speedlines, comic overshoot bounces, and speed tuning.
    /// </summary>
    public class MainMenuUITransition : MonoBehaviour
    {
        [Header("Transition Speed & Timing Settings")]
        [Tooltip("Overall transition speed multiplier. 1.0 = normal, 1.5 = faster, 0.5 = slower.")]
        [Range(0.2f, 3.0f)]
        public float transitionSpeed = 1.0f;

        [Tooltip("Duration of the ascending speedlines rush phase in seconds (at 1x speed).")]
        public float diveDuration = 0.52f;

        [Tooltip("Duration of title & button launching downwards out of frame (at 1x speed).")]
        public float fadeOutDuration = 0.22f;

        [Tooltip("Duration of MenuOptionScreen swooping down into view (at 1x speed).")]
        public float revealDuration = 0.25f;

        [Tooltip("Duration of plummeting back down to Title Screen (at 1x speed).")]
        public float backDuration = 0.42f;

        [Tooltip("Animation speed (FPS) for cycling through the Parallel V speedline frames.")]
        [Range(10f, 60f)]
        public float speedLineFps = 35f;

        [Header("Title Screen References")]
        public RectTransform titleTransform;
        public CanvasGroup titleGroup;
        public RectTransform buttonTransform;
        public CanvasGroup buttonGroup;
        public RectTransform backgroundTransform;

        [Header("Speed Lines Overlay (Parallel V Transparent Blue)")]
        public GameObject speedLinesObject;
        public Image speedLinesImage;
        public CanvasGroup speedLinesGroup;
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

            if (speedLinesObject != null)
            {
                speedLinesObject.SetActive(false);
                if (speedLinesGroup != null) speedLinesGroup.alpha = 0f;
            }

            HookBackButton();
        }

        private void Start()
        {
            HookBackButton();
        }

        private void HookBackButton()
        {
            if (backButton == null && menuOptionScreenObject != null)
            {
                backButton = menuOptionScreenObject.GetComponentInChildren<Button>(true);
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
        /// NAIK KE ATAS:
        /// 1. Button click punch & anticipation.
        /// 2. Title & Button rocket downwards off screen (simulating rocket lift-off upwards).
        /// 3. Blue transparent Parallel V speedlines stream down with surging ascent whoosh SFX.
        ///    Background rushes downwards (-1400px) with subtle comic perspective.
        /// 4. Menu Option Screen drops down from above (+750px -> 0px) with punchy comic bounce landing!
        /// </summary>
        private IEnumerator TransitionToOptionRoutine()
        {
            _isTransitioning = true;

            float speedMult = Mathf.Max(0.05f, transitionSpeed);
            float actualFadeOut = fadeOutDuration / speedMult;
            float actualDive = diveDuration / speedMult;
            float actualReveal = revealDuration / speedMult;

            // 1. Initial click punch & sound
            if (audioSynth != null) audioSynth.PlayConfirm();
            if (buttonTransform != null) buttonTransform.localScale = _buttonInitialScale * 0.92f;

            yield return new WaitForSecondsRealtime(0.04f / speedMult);

            // 2. ASCENT LAUNCH: Title & Button rocket downwards off screen
            if (audioSynth != null) audioSynth.PlayAscendWhoosh();

            float launchElapsed = 0f;
            while (launchElapsed < actualFadeOut)
            {
                launchElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(launchElapsed / actualFadeOut);
                // Comic ease-in acceleration (falling fast)
                float easeIn = t * t * t;

                // Button drops down fast and fades
                if (buttonTransform != null)
                {
                    buttonTransform.anchoredPosition = Vector2.Lerp(_buttonInitialPos, new Vector2(_buttonInitialPos.x, _buttonInitialPos.y - 800f), easeIn);
                    buttonTransform.localScale = Vector3.Lerp(_buttonInitialScale, new Vector3(_buttonInitialScale.x * 0.8f, _buttonInitialScale.y * 1.3f, 1f), easeIn);
                }
                if (buttonGroup != null) buttonGroup.alpha = 1f - easeIn;

                // Title container shoots downwards
                if (titleTransform != null)
                {
                    titleTransform.anchoredPosition = Vector2.Lerp(_titleInitialPos, new Vector2(_titleInitialPos.x, _titleInitialPos.y - 1100f), easeIn);
                    titleTransform.localScale = Vector3.Lerp(_titleInitialScale, new Vector3(_titleInitialScale.x * 0.85f, _titleInitialScale.y * 1.4f, 1f), easeIn);
                }
                if (titleGroup != null) titleGroup.alpha = 1f - easeIn;

                yield return null;
            }

            if (buttonTransform != null) buttonTransform.gameObject.SetActive(false);
            if (titleTransform != null) titleTransform.gameObject.SetActive(false);

            // 3. HYPERSONIC ASCENT: Speed lines rush down, background drops fast
            if (speedLinesObject != null)
            {
                speedLinesObject.SetActive(true);
                if (speedLinesGroup != null) speedLinesGroup.alpha = 0f;
            }

            float diveElapsed = 0f;
            int totalSprites = (speedLineSprites != null) ? speedLineSprites.Length : 0;
            RectTransform speedLinesRect = speedLinesImage != null ? speedLinesImage.rectTransform : null;

            while (diveElapsed < actualDive)
            {
                diveElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(diveElapsed / actualDive);

                // Quick fade in for speedlines
                if (speedLinesGroup != null)
                {
                    if (progress < 0.15f)
                        speedLinesGroup.alpha = progress / 0.15f;
                    else
                        speedLinesGroup.alpha = 1f;
                }

                // Background rushes violently DOWNWARDS (-1400px) with comic ease
                // This gives the undeniable sensation of shooting UP into the sky!
                if (backgroundTransform != null)
                {
                    float bgCurve = Mathf.SmoothStep(0f, 1f, progress);
                    backgroundTransform.anchoredPosition = Vector2.Lerp(_bgInitialPos, new Vector2(_bgInitialPos.x, _bgInitialPos.y - 1350f), bgCurve);
                    backgroundTransform.localScale = Vector3.Lerp(_bgInitialScale, _bgInitialScale * 1.25f, bgCurve);
                }

                // Animated cycle through transparent blue Parallel V speedlines
                if (totalSprites > 0 && speedLinesImage != null)
                {
                    int frameIndex = Mathf.FloorToInt(diveElapsed * speedLineFps * speedMult) % totalSprites;
                    speedLinesImage.sprite = speedLineSprites[frameIndex];
                }

                // Downward streaming vertical drift of speed lines (reinforcing upward camera flight)
                if (speedLinesRect != null)
                {
                    float yOffset = -(Mathf.Repeat(diveElapsed * 1600f * speedMult, 120f) - 60f);
                    speedLinesRect.anchoredPosition = new Vector2(0f, yOffset);
                }

                // Comic hypersonic screen jitter
                if (progress > 0.10f && progress < 0.88f)
                {
                    float jitterX = Random.Range(-5f, 5f);
                    float jitterY = Random.Range(-6f, 6f);
                    transform.localPosition = new Vector3(jitterX, jitterY, 0f);
                }
                else
                {
                    transform.localPosition = Vector3.zero;
                }

                yield return null;
            }

            transform.localPosition = Vector3.zero;
            if (speedLinesRect != null) speedLinesRect.anchoredPosition = Vector2.zero;

            // 4. ARRIVAL AT UPPER TIER: MenuOptionScreen enters from TOP (+750px -> 0px) with comic overshoot bounce
            if (menuOptionScreenObject != null)
            {
                menuOptionScreenObject.SetActive(true);
                if (menuOptionGroup != null) menuOptionGroup.alpha = 0f;
            }

            float revealElapsed = 0f;
            Vector2 optionStartPos = new Vector2(_menuOptionInitialPos.x, _menuOptionInitialPos.y + 750f);

            while (revealElapsed < actualReveal)
            {
                revealElapsed += Time.deltaTime;
                float rt = Mathf.Clamp01(revealElapsed / actualReveal);

                // Speed lines fade out
                if (speedLinesGroup != null) speedLinesGroup.alpha = 1f - rt;
                if (menuOptionGroup != null) menuOptionGroup.alpha = Mathf.Clamp01(rt * 1.5f);

                // Comic overshoot curve: drops down fast, overshoots slightly below (-20px), bounces back to 0
                float posCurve;
                if (rt < 0.75f)
                {
                    float subT = rt / 0.75f;
                    // Ease out plunge
                    posCurve = 1f - Mathf.Pow(1f - subT, 2.5f);
                    if (menuOptionTransform != null)
                    {
                        menuOptionTransform.anchoredPosition = Vector2.Lerp(optionStartPos, new Vector2(_menuOptionInitialPos.x, _menuOptionInitialPos.y - 25f), posCurve);
                    }
                }
                else
                {
                    float subT = (rt - 0.75f) / 0.25f;
                    // Snappy rebound back to initial 0
                    float rebound = Mathf.Sin(subT * Mathf.PI * 0.5f);
                    if (menuOptionTransform != null)
                    {
                        menuOptionTransform.anchoredPosition = Vector2.Lerp(new Vector2(_menuOptionInitialPos.x, _menuOptionInitialPos.y - 25f), _menuOptionInitialPos, rebound);
                    }
                }

                yield return null;
            }

            if (speedLinesObject != null) speedLinesObject.SetActive(false);
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
        /// TURUN KE BAWAH:
        /// 1. Option Screen shoots upwards (+800px) out of frame.
        /// 2. Audio plays descending falling whoosh SFX.
        /// 3. Speedlines rush upwards while background rushes back UP to initial position (0px).
        /// 4. Title & Button plummet down from ABOVE (+950px) and slam down with a comic landing bounce!
        /// 5. Heavy comic impact landing sound & screen shudder!
        /// </summary>
        private IEnumerator TransitionBackToTitleRoutine()
        {
            _isTransitioning = true;
            if (audioSynth != null) audioSynth.PlayCancel();

            float speedMult = Mathf.Max(0.05f, transitionSpeed);
            float actualBack = backDuration / speedMult;

            // 1. Option Screen shoots UPWARDS (+800px) as camera falls downwards
            float exitElapsed = 0f;
            float exitDuration = 0.16f / speedMult;
            Vector2 optionExitTarget = new Vector2(_menuOptionInitialPos.x, _menuOptionInitialPos.y + 800f);

            while (exitElapsed < exitDuration)
            {
                exitElapsed += Time.deltaTime;
                float et = Mathf.Clamp01(exitElapsed / exitDuration);
                float easeIn = et * et;

                if (menuOptionTransform != null)
                {
                    menuOptionTransform.anchoredPosition = Vector2.Lerp(_menuOptionInitialPos, optionExitTarget, easeIn);
                }
                if (menuOptionGroup != null)
                {
                    menuOptionGroup.alpha = 1f - easeIn;
                }

                yield return null;
            }

            if (menuOptionScreenObject != null) menuOptionScreenObject.SetActive(false);
            if (menuOptionTransform != null) menuOptionTransform.anchoredPosition = _menuOptionInitialPos;

            // 2. Play descending plummet whoosh & activate speedlines
            if (audioSynth != null) audioSynth.PlayDescendWhoosh();

            if (speedLinesObject != null)
            {
                speedLinesObject.SetActive(true);
                if (speedLinesGroup != null) speedLinesGroup.alpha = 0.9f;
            }

            // Reactivate Title and Button at top position (above screen, ready to slam down)
            Vector2 titleTopPos = new Vector2(_titleInitialPos.x, _titleInitialPos.y + 950f);
            Vector2 buttonTopPos = new Vector2(_buttonInitialPos.x, _buttonInitialPos.y + 800f);

            if (titleTransform != null)
            {
                titleTransform.gameObject.SetActive(true);
                titleTransform.anchoredPosition = titleTopPos;
                titleTransform.localScale = new Vector3(_titleInitialScale.x * 0.9f, _titleInitialScale.y * 1.35f, 1f);
                if (titleGroup != null) titleGroup.alpha = 0.4f;
            }

            if (buttonTransform != null)
            {
                buttonTransform.gameObject.SetActive(true);
                buttonTransform.anchoredPosition = buttonTopPos;
                buttonTransform.localScale = _buttonInitialScale;
                if (buttonGroup != null) buttonGroup.alpha = 0.4f;
            }

            Vector3 currentBgScale = backgroundTransform != null ? backgroundTransform.localScale : _bgInitialScale;
            Vector2 currentBgPos = backgroundTransform != null ? backgroundTransform.anchoredPosition : _bgInitialPos;

            int totalSprites = (speedLineSprites != null) ? speedLineSprites.Length : 0;
            RectTransform speedLinesRect = speedLinesImage != null ? speedLinesImage.rectTransform : null;

            float fallElapsed = 0f;
            float fallDuration = actualBack * 0.70f;

            // 3. FALL PHASE: Background rushes back UP, Title & Button plummet down from above
            while (fallElapsed < fallDuration)
            {
                fallElapsed += Time.deltaTime;
                float ft = Mathf.Clamp01(fallElapsed / fallDuration);
                // Heavy gravity acceleration ease-in
                float fallCurve = ft * ft * ft;

                // Background rushes back UP from -1350px to 0px
                if (backgroundTransform != null)
                {
                    float bgT = Mathf.SmoothStep(0f, 1f, ft);
                    backgroundTransform.anchoredPosition = Vector2.Lerp(currentBgPos, _bgInitialPos, bgT);
                    backgroundTransform.localScale = Vector3.Lerp(currentBgScale, _bgInitialScale, bgT);
                }

                // Speed lines cycle in reverse
                if (totalSprites > 0 && speedLinesImage != null)
                {
                    int frameIndex = Mathf.FloorToInt((fallDuration - fallElapsed) * speedLineFps * speedMult) % totalSprites;
                    if (frameIndex < 0) frameIndex += totalSprites;
                    speedLinesImage.sprite = speedLineSprites[frameIndex];
                }

                // Upward drift of lines (falling down through them)
                if (speedLinesRect != null)
                {
                    float yOffset = Mathf.Repeat(fallElapsed * 1500f * speedMult, 120f) - 60f;
                    speedLinesRect.anchoredPosition = new Vector2(0f, yOffset);
                }

                // Title drops from top towards landing point
                if (titleTransform != null)
                {
                    titleTransform.anchoredPosition = Vector2.Lerp(titleTopPos, _titleInitialPos, fallCurve);
                }
                if (titleGroup != null) titleGroup.alpha = Mathf.Lerp(0.4f, 1f, ft);

                // Button drops slightly trailing behind title
                if (buttonTransform != null)
                {
                    float buttonT = Mathf.Clamp01(ft * 1.1f);
                    buttonTransform.anchoredPosition = Vector2.Lerp(buttonTopPos, _buttonInitialPos, buttonT * buttonT);
                }
                if (buttonGroup != null) buttonGroup.alpha = Mathf.Lerp(0.4f, 1f, ft);

                yield return null;
            }

            // Hide speed lines as we make touchdown
            if (speedLinesObject != null) speedLinesObject.SetActive(false);
            if (speedLinesRect != null) speedLinesRect.anchoredPosition = Vector2.zero;

            // 4. HERO SLAM LANDING: Heavy comic impact sound + squash & bounce!
            if (audioSynth != null) audioSynth.PlayLandingImpact();

            // Landing bounce routine (Title slams down past 0, squashes, bounces back up, settles)
            float bounceElapsed = 0f;
            float bounceDuration = actualBack * 0.35f;

            while (bounceElapsed < bounceDuration)
            {
                bounceElapsed += Time.deltaTime;
                float bt = Mathf.Clamp01(bounceElapsed / bounceDuration);

                // Damped harmonic comic bounce curve
                float bounceOffset = -35f * Mathf.Exp(-bt * 6f) * Mathf.Cos(bt * Mathf.PI * 3f);
                float squashX = 1f + (0.12f * Mathf.Exp(-bt * 6f) * Mathf.Cos(bt * Mathf.PI * 3f));
                float squashY = 1f - (0.15f * Mathf.Exp(-bt * 6f) * Mathf.Cos(bt * Mathf.PI * 3f));

                if (titleTransform != null)
                {
                    titleTransform.anchoredPosition = new Vector2(_titleInitialPos.x, _titleInitialPos.y + bounceOffset);
                    titleTransform.localScale = new Vector3(_titleInitialScale.x * squashX, _titleInitialScale.y * squashY, 1f);
                }

                if (buttonTransform != null)
                {
                    float btnBounce = bounceOffset * 0.6f;
                    buttonTransform.anchoredPosition = new Vector2(_buttonInitialPos.x, _buttonInitialPos.y + btnBounce);
                    buttonTransform.localScale = new Vector3(_buttonInitialScale.x * squashX, _buttonInitialScale.y * squashY, 1f);
                }

                // Comic impact landing camera shake
                if (bt < 0.4f)
                {
                    float shake = (1f - (bt / 0.4f)) * 4f;
                    transform.localPosition = new Vector3(Random.Range(-shake, shake), Random.Range(-shake, shake), 0f);
                }
                else
                {
                    transform.localPosition = Vector3.zero;
                }

                yield return null;
            }

            // Restore all initial values cleanly
            transform.localPosition = Vector3.zero;

            if (backgroundTransform != null)
            {
                backgroundTransform.localScale = _bgInitialScale;
                backgroundTransform.anchoredPosition = _bgInitialPos;
            }

            if (titleTransform != null)
            {
                titleTransform.localScale = _titleInitialScale;
                titleTransform.anchoredPosition = _titleInitialPos;
            }
            if (titleGroup != null) titleGroup.alpha = 1f;

            if (buttonTransform != null)
            {
                buttonTransform.localScale = _buttonInitialScale;
                buttonTransform.anchoredPosition = _buttonInitialPos;
            }
            if (buttonGroup != null) buttonGroup.alpha = 1f;

            isInOptionScreen = false;
            _isTransitioning = false;
        }
    }
}
