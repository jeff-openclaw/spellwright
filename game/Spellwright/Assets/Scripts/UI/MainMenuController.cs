using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Spellwright.Run;

namespace Spellwright.UI
{
    /// <summary>
    /// UI Toolkit-based main menu controller. Replaces the uGUI MainMenuUI.
    /// Manages entrance animations (staggered reveals, typewriter subtitle, cursor blink)
    /// via USS transitions and C# scheduling.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        [Header("Entrance Timing")]
        [SerializeField] private float contentDelay = 200f;
        [SerializeField] private float buttonDelay = 1000f;
        [SerializeField] private float hintDelay = 1200f;
        [SerializeField] private float versionDelay = 1400f;
        [SerializeField] private float subtitleDelay = 600f;
        [SerializeField] private float typewriterSpeed = 40f;

        private VisualElement _root;
        private VisualElement _content;
        private Label _titleLabel;
        private Label _subtitleLabel;
        private Label _cursorLabel;
        private Button _startButton;
        private Label _hintLabel;
        private Label _versionLabel;

        private IVisualElementScheduledItem _blinkSchedule;
        private Coroutine _typewriterCoroutine;
        private bool _cursorVisible = true;

        private void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            WireEvents();
            SetInitialState();
            PlayEntranceSequence();
        }

        private void OnDisable()
        {
            if (_startButton != null)
                _startButton.clicked -= OnStartClicked;

            if (_root != null)
                _root.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            _blinkSchedule?.Pause();
            _blinkSchedule = null;

            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }

            // Reset animation states so re-enable replays entrance
            ResetAnimationState();
        }

        private void CacheElements()
        {
            _content = _root.Q<VisualElement>(className: "main-menu__content");
            _titleLabel = _root.Q<Label>("title");
            _subtitleLabel = _root.Q<Label>("subtitle");
            _cursorLabel = _root.Q<Label>("cursor");
            _startButton = _root.Q<Button>("start-button");
            _hintLabel = _root.Q<Label>("hint");
            _versionLabel = _root.Q<Label>("version");
        }

        private void WireEvents()
        {
            if (_startButton != null)
                _startButton.clicked += OnStartClicked;

            // Allow Enter key to start the game
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        private void SetInitialState()
        {
            if (_versionLabel != null)
                _versionLabel.text = $"v{Application.version}";

            if (_subtitleLabel != null)
                _subtitleLabel.text = "";

            // Hide cursor until subtitle starts
            if (_cursorLabel != null)
                _cursorLabel.RemoveFromClassList("cursor-on");
        }

        private void PlayEntranceSequence()
        {
            // Content container: fade + slide in
            _root.schedule.Execute(() =>
            {
                _content?.AddToClassList("main-menu__content--visible");
            }).ExecuteLater((long)contentDelay);

            // Subtitle typewriter
            _root.schedule.Execute(() =>
            {
                _typewriterCoroutine = StartCoroutine(TypewriterSubtitle());
            }).ExecuteLater((long)subtitleDelay);

            // Start button: scale in
            _root.schedule.Execute(() =>
            {
                _startButton?.AddToClassList("main-menu__start-btn--visible");
            }).ExecuteLater((long)buttonDelay);

            // Hint text
            _root.schedule.Execute(() =>
            {
                _hintLabel?.AddToClassList("main-menu__hint--visible");
            }).ExecuteLater((long)hintDelay);

            // Version text
            _root.schedule.Execute(() =>
            {
                _versionLabel?.AddToClassList("main-menu__version--visible");
            }).ExecuteLater((long)versionDelay);
        }

        private IEnumerator TypewriterSubtitle()
        {
            const string fullText = "A Word-Guessing Roguelike";
            float delaySeconds = typewriterSpeed / 1000f;

            // Show cursor during typing
            if (_cursorLabel != null)
                _cursorLabel.AddToClassList("cursor-on");

            for (int i = 0; i < fullText.Length; i++)
            {
                if (_subtitleLabel != null)
                    _subtitleLabel.text = fullText.Substring(0, i + 1);
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            _typewriterCoroutine = null;

            // Start cursor blink after typewriter finishes
            StartCursorBlink();
        }

        private void StartCursorBlink()
        {
            if (_cursorLabel == null) return;

            _blinkSchedule = _root.schedule.Execute(() =>
            {
                _cursorVisible = !_cursorVisible;
                if (_cursorVisible)
                {
                    _cursorLabel.RemoveFromClassList("cursor-off");
                    _cursorLabel.AddToClassList("cursor-on");
                }
                else
                {
                    _cursorLabel.RemoveFromClassList("cursor-on");
                    _cursorLabel.AddToClassList("cursor-off");
                }
            }).Every(500);
        }

        private void ResetAnimationState()
        {
            _content?.RemoveFromClassList("main-menu__content--visible");
            _startButton?.RemoveFromClassList("main-menu__start-btn--visible");
            _hintLabel?.RemoveFromClassList("main-menu__hint--visible");
            _versionLabel?.RemoveFromClassList("main-menu__version--visible");
            _cursorVisible = true;
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                OnStartClicked();
                evt.StopPropagation();
            }
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }
    }
}
