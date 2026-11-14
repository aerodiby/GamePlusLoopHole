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
    /// Prevents double-clicking or input race conditions between mouse and EventSystem.
    /// </summary>
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("References")]
        public GameObject pressAnyButtonObject;
        public MainMenuUIAudioSynthesizer audioSynth;
        public MainMenuUITransition transition;

        [Header("Events")]
        public UnityEvent onButtonPressed;

        private MainMenuUIButton _buttonComponent;
        private bool _hasTriggered = false;

        private void Start()
        {
            if (pressAnyButtonObject != null)
            {
                _buttonComponent = pressAnyButtonObject.GetComponent<MainMenuUIButton>();
            }
            if (transition == null)
            {
                transition = GetComponent<MainMenuUITransition>();
            }
        }

        private void Update()
        {
            if (transition != null && (transition.isInOptionScreen || transition.IsTransitioning))
            {
                return;
            }

            // Reset single-fire guard when safely back in Title Screen
            if (_hasTriggered && transition != null && !transition.isInOptionScreen && !transition.IsTransitioning)
            {
                _hasTriggered = false;
                if (_buttonComponent != null)
                {
                    _buttonComponent.SetLocked(false);
                }
            }

            if (!_hasTriggered && CheckAnyInputDown())
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
            if (_hasTriggered) return;
            if (transition != null && (transition.isInOptionScreen || transition.IsTransitioning)) return;

            _hasTriggered = true;

            // Lock button component so hover/exit won't fight with the transition
            if (_buttonComponent != null)
            {
                _buttonComponent.SetLocked(true);
            }

            Debug.Log("[MainMenuUI] 'Press Any Button' activated cleanly!");

            if (transition != null)
            {
                transition.StartTransitionToOption();
            }
            else
            {
                if (_buttonComponent != null)
                {
                    _buttonComponent.TriggerClickEffect(playAudio: true);
                }
                else if (audioSynth != null)
                {
                    audioSynth.PlayConfirm();
                }
            }

            onButtonPressed?.Invoke();
        }
    }
}
