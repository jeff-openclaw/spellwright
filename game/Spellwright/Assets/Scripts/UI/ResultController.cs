using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spellwright.UI
{
    /// <summary>
    /// UI Toolkit-based run-end screen controller. Replaces the uGUI RunEndUI.
    /// Displays ASCII banner, victory/defeat title, staged stats reveal,
    /// and play-again button with staggered entrance animation.
    /// </summary>
    public class ResultController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        [Header("Animation")]
        [SerializeField] private float statRevealDelayMs = 300f;
        [SerializeField] private float buttonRevealDelayMs = 300f;

        private VisualElement _root;
        private Label _bannerLabel;
        private Label _titleLabel;
        private Label _scoreValue;
        private Label _encountersValue;
        private Label _tomesValue;
        private Label _wordsValue;
        private Button _playAgainButton;
        private VisualElement _statsContainer;

        private void OnEnable()
        {
            if (uiDocument == null) return;

            _root = uiDocument.rootVisualElement;
            if (_root == null) return;

            CacheElements();
            WireEvents();
            SubscribeEventBus();
        }

        private void OnDisable()
        {
            UnwireEvents();
            UnsubscribeEventBus();
        }

        private void CacheElements()
        {
            _bannerLabel = _root.Q<Label>("banner");
            _titleLabel = _root.Q<Label>("title");
            _scoreValue = _root.Q<Label>("stat-score-value");
            _encountersValue = _root.Q<Label>("stat-encounters-value");
            _tomesValue = _root.Q<Label>("stat-tomes-value");
            _wordsValue = _root.Q<Label>("stat-words-value");
            _statsContainer = _root.Q("stats-container");
            _playAgainButton = _root.Q<Button>("play-again-btn");
        }

        private void WireEvents()
        {
            if (_playAgainButton != null)
                _playAgainButton.clicked += OnPlayAgainClicked;
        }

        private void UnwireEvents()
        {
            if (_playAgainButton != null)
                _playAgainButton.clicked -= OnPlayAgainClicked;
        }

        private void SubscribeEventBus()
        {
            EventBus.Instance.Subscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Instance.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void UnsubscribeEventBus()
        {
            EventBus.Instance.Unsubscribe<RunEndedEvent>(OnRunEnded);
            EventBus.Instance.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.RunEnd)
                RefreshDisplay();
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            RefreshDisplay(evt);
        }

        // ── Display ─────────────────────────────────────────

        private void RefreshDisplay(RunEndedEvent evt = null)
        {
            bool won = evt?.Won ?? false;
            var resultRoot = _root?.Q(className: "result-screen");

            // Set win/loss modifier class
            if (resultRoot != null)
            {
                resultRoot.RemoveFromClassList("result-screen--win");
                resultRoot.RemoveFromClassList("result-screen--loss");
                resultRoot.AddToClassList(won ? "result-screen--win" : "result-screen--loss");
            }

            // ASCII Banner
            if (_bannerLabel != null)
                _bannerLabel.text = ASCIIBanners.GetRunEndBanner(won);

            // Title
            if (_titleLabel != null)
                _titleLabel.text = won ? "VICTORY!" : "DEFEAT";

            // Stats values
            int score = evt?.FinalScore ?? (RunManager.Instance?.Score ?? 0);
            int encountersWon = RunManager.Instance?.EncountersWon ?? 0;
            int tomesCollected = Tomes.TomeManager.Instance?.TomeSystem?.Count ?? 0;
            int wordsUsed = RunManager.Instance?.UsedWords?.Count ?? 0;

            if (_scoreValue != null) _scoreValue.text = score.ToString();
            if (_encountersValue != null) _encountersValue.text = encountersWon.ToString();
            if (_tomesValue != null) _tomesValue.text = tomesCollected.ToString();
            if (_wordsValue != null) _wordsValue.text = wordsUsed.ToString();

            // Staggered entrance animation
            PlayEntranceAnimation();
        }

        private void PlayEntranceAnimation()
        {
            if (_root == null) return;

            // Collect all stagger items (stat rows + play again button)
            var staggerItems = _root.Query(className: "stagger-item").ToList();

            for (int i = 0; i < staggerItems.Count; i++)
            {
                var item = staggerItems[i];
                item.RemoveFromClassList("stagger-visible");

                // Stats use statRevealDelayMs, button gets extra delay
                bool isButton = item.ClassListContains("result-screen__play-again");
                long delay = isButton
                    ? (long)((staggerItems.Count - 1) * statRevealDelayMs + buttonRevealDelayMs)
                    : (long)(i * statRevealDelayMs);

                _root.schedule.Execute(() =>
                {
                    item.AddToClassList("stagger-visible");
                }).ExecuteLater(delay);
            }
        }

        // ── Event Handlers ──────────────────────────────────

        private void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }
    }
}
