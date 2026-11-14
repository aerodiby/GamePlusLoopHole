using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI.MainMenuUI
{
    /// Master controller for the MainMenuUI Title Screen.
    /// Handles "Press Any Button" input, audio triggers, and click effects.
    public class MainMenuUIController : MonoBehaviour
    {
        [Header("References")]
        public GameObject pressAnyButtonObject;
        public MainMenuUIAudioSynthesizer audioSynth;
        public MainMenuUITransition transition;

        [Header("Events")]
        public UnityEvent onButtonPressed;

        private MainMenuUIButton _buttonComponent;

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
            if (transition != null && transition.isInOptionScreen) return;

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
            if (transition != null && transition.isInOptionScreen) return;

            Debug.Log("[MainMenuUI] 'Press Any Button' activated!");

            if (transition != null)
            {
                transition.StartTransitionToOption();
            }
            else
            {
                if (_buttonComponent != null)
                {
                    _buttonComponent.TriggerClickEffect();
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
