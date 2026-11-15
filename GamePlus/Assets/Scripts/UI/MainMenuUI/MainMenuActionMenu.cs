using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace UI.MainMenuUI
{
    [System.Serializable]
    public class ActionMenuOption
    {
        public string title;
        [TextArea(2, 4)]
        public string description;
        public UnityEvent onExecute;
    }

    /// <summary>
    /// Hi-Fi Rush styled Main Menu Action Screen.
    /// Implements:
    /// - Angled action card container with custom UI assets (Action Menu.png).
    /// - Dynamic sliding selection highlight banner (Select.png).
    /// - Active text color shift (selected = comic black, unselected = punchy white with dark outline).
    /// - Dynamic bottom description ticker box for each option.
    /// - Complete Gamepad (D-pad/Stick/A/B), Keyboard (WASD/Arrows/Enter/Esc), and Mouse (Hover/Click) navigation.
    /// - Seamless transition back to Title Screen via MainMenuUITransition.
    /// </summary>
    public class MainMenuActionMenu : MonoBehaviour
    {
        [Header("Menu Configuration")]
        public List<ActionMenuOption> options = new List<ActionMenuOption>
        {
            new ActionMenuOption
            {
                title = "Continue",
                description = "Resume beat synchronization from checkpoint.",
                onExecute = new UnityEvent()
            },
            new ActionMenuOption
            {
                title = "New Game",
                description = "Start a new adventure!",
                onExecute = new UnityEvent()
            },
            new ActionMenuOption
            {
                title = "Options",
                description = "Gameplay, visual, sound and accessibility adjustments!",
                onExecute = new UnityEvent()
            },
            new ActionMenuOption
            {
                title = "Bethesda.net",
                description = "Online features and community network.",
                onExecute = new UnityEvent()
            },
            new ActionMenuOption
            {
                title = "Exit Game",
                description = "Close the application.",
                onExecute = new UnityEvent()
            }
        };

        [Header("UI Visual Elements")]
        public RectTransform cardContainer;
        public RectTransform selectionHighlight;
        public TextMeshProUGUI descriptionText;
        public List<RectTransform> buttonTransforms = new List<RectTransform>();
        public List<TextMeshProUGUI> buttonTexts = new List<TextMeshProUGUI>();

        [Header("Appearance & Colors")]
        public Color textNormalColor = Color.white;
        public Color textSelectedColor = new Color(0.08f, 0.04f, 0.16f, 1f); // Deep obsidian black
        public float highlightMoveSpeed = 24f;
        public float punchScaleMultiplier = 1.05f;

        [Header("Audio & Transitions")]
        public MainMenuUIAudioSynthesizer audioSynth;
        public MainMenuUITransition transition;

        [Header("State")]
        [SerializeField] private int _currentIndex = 1; // Default to "New Game"
        public int CurrentIndex => _currentIndex;

        private Vector2 _targetHighlightPos;
        private Vector3 _highlightTargetScale = Vector3.one;
        private Vector3 _highlightVelocity = Vector3.zero;
        private Vector2 _highlightVel2D = Vector2.zero;

        // Input stick debounce
        private float _lastNavTime = 0f;
        private const float NAV_REPEAT_DELAY = 0.22f;
        private bool _stickInUse = false;

        private void Awake()
        {
            if (transition == null)
            {
                transition = GetComponentInParent<MainMenuUITransition>();
                if (transition == null)
                {
                    transition = FindFirstObjectByType<MainMenuUITransition>();
                }
            }

            if (audioSynth == null)
            {
                audioSynth = GetComponentInParent<MainMenuUIAudioSynthesizer>();
                if (audioSynth == null)
                {
                    audioSynth = FindFirstObjectByType<MainMenuUIAudioSynthesizer>();
                }
            }
        }

        private void OnEnable()
        {
            // Initial positioning
            if (_currentIndex < 0 || _currentIndex >= options.Count)
            {
                _currentIndex = 1;
            }

            SnapToSelection(_currentIndex);
        }

        private void Start()
        {
            // Setup default callbacks if not wired in inspector
            SetupDefaultCallbacks();
            SnapToSelection(_currentIndex);
        }

        private void SetupDefaultCallbacks()
        {
            if (options.Count > 0 && options[0].onExecute.GetPersistentEventCount() == 0)
                options[0].onExecute.AddListener(() => Debug.Log("[ActionMenu] Continue selected."));

            if (options.Count > 1 && options[1].onExecute.GetPersistentEventCount() == 0)
                options[1].onExecute.AddListener(() => Debug.Log("[ActionMenu] New Game selected."));

            if (options.Count > 2 && options[2].onExecute.GetPersistentEventCount() == 0)
                options[2].onExecute.AddListener(() => Debug.Log("[ActionMenu] Options selected."));

            if (options.Count > 3 && options[3].onExecute.GetPersistentEventCount() == 0)
                options[3].onExecute.AddListener(() => Debug.Log("[ActionMenu] Bethesda.net selected."));

            if (options.Count > 4 && options[4].onExecute.GetPersistentEventCount() == 0)
            {
                options[4].onExecute.AddListener(() =>
                {
                    Debug.Log("[ActionMenu] Exit Game selected.");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                });
            }
        }

        private void Update()
        {
            if (transition != null && transition.IsTransitioning) return;

            HandleInput();
            AnimateHighlight();
        }

        private void HandleInput()
        {
            float navVertical = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame) navVertical = 1f;
                else if (Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame) navVertical = -1f;
            }

            if (Gamepad.current != null)
            {
                if (Gamepad.current.dpad.up.wasPressedThisFrame) navVertical = 1f;
                else if (Gamepad.current.dpad.down.wasPressedThisFrame) navVertical = -1f;
                else
                {
                    float stickY = Gamepad.current.leftStick.y.ReadValue();
                    if (Mathf.Abs(stickY) > 0.5f)
                    {
                        if (!_stickInUse || Time.unscaledTime - _lastNavTime > NAV_REPEAT_DELAY)
                        {
                            navVertical = stickY > 0 ? 1f : -1f;
                            _stickInUse = true;
                            _lastNavTime = Time.unscaledTime;
                        }
                    }
                    else
                    {
                        _stickInUse = false;
                    }
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) navVertical = 1f;
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) navVertical = -1f;
            else
            {
                float axis = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(axis) > 0.5f)
                {
                    if (!_stickInUse || Time.unscaledTime - _lastNavTime > NAV_REPEAT_DELAY)
                    {
                        navVertical = axis > 0 ? 1f : -1f;
                        _stickInUse = true;
                        _lastNavTime = Time.unscaledTime;
                    }
                }
                else
                {
                    _stickInUse = false;
                }
            }
#endif

            if (navVertical > 0.5f)
            {
                Navigate(-1);
            }
            else if (navVertical < -0.5f)
            {
                Navigate(1);
            }

            // Confirm
            if (CheckConfirmInput())
            {
                ExecuteCurrentOption();
            }

            // Back / Cancel
            if (CheckBackInput())
            {
                OnBackRequested();
            }
        }

        private bool CheckConfirmInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame ||
                                             Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                                             Keyboard.current.spaceKey.wasPressedThisFrame ||
                                             Keyboard.current.eKey.wasPressedThisFrame)) return true;

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
#else
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E)) return true;
            if (Input.GetKeyDown(KeyCode.JoystickButton0)) return true;
            return false;
#endif
        }

        private bool CheckBackInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonEast.wasPressedThisFrame ||
                                           Gamepad.current.bButton.wasPressedThisFrame)) return true;
            return false;
#else
            if (Input.GetKeyDown(KeyCode.Escape)) return true;
            if (Input.GetKeyDown(KeyCode.JoystickButton1)) return true;
            return false;
#endif
        }

        public void Navigate(int direction)
        {
            if (options.Count == 0) return;
            int newIndex = (_currentIndex + direction + options.Count) % options.Count;
            SelectIndex(newIndex, playSound: true, punch: true);
        }

        public void SelectIndex(int index, bool playSound = true, bool punch = true)
        {
            if (index < 0 || index >= options.Count) return;

            bool changed = (_currentIndex != index);
            _currentIndex = index;

            UpdateTargetHighlightPosition(punch);
            UpdateVisualTexts();
            UpdateDescription();

            if (changed && playSound && audioSynth != null)
            {
                audioSynth.PlayHover();
            }
        }

        public void SnapToSelection(int index)
        {
            _currentIndex = Mathf.Clamp(index, 0, Mathf.Max(0, options.Count - 1));

            if (buttonTransforms.Count > _currentIndex && buttonTransforms[_currentIndex] != null)
            {
                _targetHighlightPos = buttonTransforms[_currentIndex].anchoredPosition;
                if (selectionHighlight != null)
                {
                    selectionHighlight.anchoredPosition = _targetHighlightPos;
                    selectionHighlight.localScale = Vector3.one;
                }
            }

            UpdateVisualTexts();
            UpdateDescription();
        }

        private void UpdateTargetHighlightPosition(bool punch)
        {
            if (buttonTransforms.Count > _currentIndex && buttonTransforms[_currentIndex] != null)
            {
                _targetHighlightPos = buttonTransforms[_currentIndex].anchoredPosition;

                if (punch && selectionHighlight != null)
                {
                    selectionHighlight.localScale = new Vector3(punchScaleMultiplier, punchScaleMultiplier, 1f);
                }
            }
        }

        private void AnimateHighlight()
        {
            if (selectionHighlight == null) return;

            // Smooth position interpolation
            selectionHighlight.anchoredPosition = Vector2.Lerp(
                selectionHighlight.anchoredPosition,
                _targetHighlightPos,
                Time.unscaledDeltaTime * highlightMoveSpeed
            );

            // Smooth spring back to scale 1.0
            selectionHighlight.localScale = Vector3.Lerp(
                selectionHighlight.localScale,
                _highlightTargetScale,
                Time.unscaledDeltaTime * 18f
            );
        }

        private void UpdateVisualTexts()
        {
            for (int i = 0; i < buttonTexts.Count; i++)
            {
                if (buttonTexts[i] == null) continue;

                bool isSelected = (i == _currentIndex);
                buttonTexts[i].color = isSelected ? textSelectedColor : textNormalColor;

                // Subtle scale for selected text
                Transform t = buttonTexts[i].transform;
                t.localScale = isSelected ? new Vector3(1.05f, 1.05f, 1f) : Vector3.one;
            }
        }

        private void UpdateDescription()
        {
            if (descriptionText == null) return;

            if (_currentIndex >= 0 && _currentIndex < options.Count)
            {
                descriptionText.text = options[_currentIndex].description;
            }
        }

        public void ExecuteCurrentOption()
        {
            if (audioSynth != null)
            {
                audioSynth.PlayConfirm();
            }

            // Punch selection highlight
            if (selectionHighlight != null)
            {
                selectionHighlight.localScale = new Vector3(1.12f, 1.12f, 1f);
            }

            if (_currentIndex >= 0 && _currentIndex < options.Count)
            {
                Debug.Log($"[ActionMenu] Activated: {options[_currentIndex].title}");
                options[_currentIndex].onExecute?.Invoke();
            }
        }

        public void OnBackRequested()
        {
            if (audioSynth != null)
            {
                audioSynth.PlayCancel();
            }

            if (transition != null)
            {
                transition.StartTransitionBackToTitle();
            }
        }

        /// <summary>
        /// Called when the mouse hovers over a button item.
        /// </summary>
        public void OnButtonHover(int index)
        {
            if (transition != null && transition.IsTransitioning) return;
            if (_currentIndex == index) return;

            SelectIndex(index, playSound: true, punch: true);
        }

        /// <summary>
        /// Called when a button item is clicked.
        /// </summary>
        public void OnButtonClick(int index)
        {
            if (transition != null && transition.IsTransitioning) return;

            if (_currentIndex != index)
            {
                SelectIndex(index, playSound: false, punch: true);
            }
            ExecuteCurrentOption();
        }
    }
}
