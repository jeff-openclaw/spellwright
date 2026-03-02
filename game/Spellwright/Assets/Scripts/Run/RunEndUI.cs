using Spellwright.Core;
using Spellwright.Data;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Run
{
    /// <summary>
    /// Displays the run end screen with stats and a "Play Again" button.
    /// </summary>
    public class RunEndUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button playAgainButton;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<RunEndedEvent>(OnRunEnded);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            // If we're becoming visible and there's a RunEndedEvent context, refresh
            RefreshDisplay();
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<RunEndedEvent>(OnRunEnded);

            if (playAgainButton != null)
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            RefreshDisplay(evt);
        }

        private void RefreshDisplay(RunEndedEvent evt = null)
        {
            if (titleText != null)
            {
                bool won = evt?.Won ?? false;
                titleText.text = won ? "VICTORY!" : "DEFEAT";
                titleText.color = won
                    ? (theme != null ? theme.amberBright : new Color(1f, 0.75f, 0f))
                    : (theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f));
            }

            if (statsText != null)
            {
                int score = evt?.FinalScore ?? (RunManager.Instance?.Score ?? 0);
                int encountersWon = RunManager.Instance?.EncountersWon ?? 0;
                int tomesCollected = Tomes.TomeManager.Instance?.TomeSystem?.Count ?? 0;

                statsText.text = $"Final Score: {score}\n\n"
                    + $"Encounters Won: {encountersWon}\n"
                    + $"Tomes Collected: {tomesCollected}\n"
                    + $"Words Used: {RunManager.Instance?.UsedWords?.Count ?? 0}";
            }
        }

        private void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }
    }
}
