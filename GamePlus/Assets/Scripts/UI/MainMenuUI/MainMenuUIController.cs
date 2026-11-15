using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI.MainMenuUI
{
    /// <summary>
    /// Master controller for the MainMenuUI Title Screen.
    /// Handles "Press Any Button" input, audio triggers, and clean single-fire activation.
    /// Ensures star mascot completes full press animation (spin -> shrink -> tactile hold)
    /// before launching the transition to the option screen.
    /// </summary>
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("References")]
        public GameObject pressAnyButtonObject;
        public MainMenuUIButton buttonComponent;
        public MainMenuUIAudioSynthesizer audioSynth;
        public MainMenuUITransition transition;

        [Header("Events")]
        public UnityEvent onButtonPressed;

        private bool _isSequenceActive = false;
        private bool _hasReachedOptionScreen = false;

        private void Awake()
        {
            EnsureReferences();
        }

        private void Start()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (pressAnyButtonObject == null)
            {
                var btn = GameObject.Find("Button");
                if (btn != null) pressAnyButtonObject = btn;
            }

            if (buttonComponent == null && pressAnyButtonObject != null)
            {
                buttonComponent = pressAnyButtonObject.GetComponent<MainMenuUIButton>();
            }

            if (transition == null)
            {
                transition = GetComponent<MainMenuUITransition>();
            }
            if (audioSynth == null)
            {
                audioSynth = GetComponent<MainMenuUIAudioSynthesizer>();
            }
        }

        private void Update()
        {
            EnsureReferences();

            // Track when the user has entered the option screen
            if (transition != null && transition.isInOptionScreen)
            {
                _hasReachedOptionScreen = true;
                return;
            }

            // Only reset when we have actually entered the Option screen and fully returned to Title Screen
            if (_hasReachedOptionScreen && transition != null && !transition.isInOptionScreen && !transition.IsTransitioning)
            {
                _hasReachedOptionScreen = false;
                _isSequenceActive = false;
                if (buttonComponent != null)
                {
                    buttonComponent.SetLocked(false);
                    if (buttonComponent.starAnimation != null)
                    {
                        buttonComponent.starAnimation.ResetToHome();
                    }
                }
            }

            // Do not accept input if sequence is already active or transition is running
            if (_isSequenceActive || (transition != null && transition.IsTransitioning))
            {
                return;
            }

            if (CheckAnyInputDown())
            {
                OnPressAnyButtonActivated();
            }
        }

        private bool CheckAnyInputDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                          Mouse.current.rightButton.wasPressedThisFrame ||
                                          Mouse.current.middleButton.wasPressedThisFrame)) return true;
            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                    Gamepad.current.buttonEast.wasPressedThisFrame ||
                    Gamepad.current.buttonWest.wasPressedThisFrame ||
                    Gamepad.current.buttonNorth.wasPressedThisFrame ||
                    Gamepad.current.startButton.wasPressedThisFrame) return true;
            }
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }

        public void OnPressAnyButtonActivated()
        {
            if (_isSequenceActive) return;
            if (transition != null && (transition.isInOptionScreen || transition.IsTransitioning)) return;

            _isSequenceActive = true;
            EnsureReferences();

            // Lock button component so hover/exit won't fight
            if (buttonComponent != null)
            {
                buttonComponent.SetLocked(true);
            }

            Debug.Log("[MainMenuUI] 'Press Any Button' activated cleanly!");

            if (buttonComponent != null && buttonComponent.starAnimation != null)
            {
                StartCoroutine(ExecutePressAndTransition());
            }
            else
            {
                if (transition != null)
                {
                    transition.StartTransitionToOption();
                }
                else if (audioSynth != null)
                {
                    audioSynth.PlayConfirm();
                }

                onButtonPressed?.Invoke();
            }
        }

        private IEnumerator ExecutePressAndTransition()
        {
            bool transitionTriggered = false;

            buttonComponent.starAnimation.PlayPressSequence(
                onImpact: () =>
                {
                    if (audioSynth != null)
                    {
                        audioSynth.PlayConfirm();
                    }
                    onButtonPressed?.Invoke();
                },
                onTransitionReady: () =>
                {
                    transitionTriggered = true;
                    if (transition != null)
                    {
                        transition.StartTransitionToOption();
                    }
                }
            );

            // Generous fallback timeout (2.5s) in case transition wasn't called
            yield return new WaitForSecondsRealtime(2.5f);
            if (!transitionTriggered)
            {
                if (transition != null && !transition.IsTransitioning && !transition.isInOptionScreen)
                {
                    transition.StartTransitionToOption();
                }
            }
        }
    }
}
