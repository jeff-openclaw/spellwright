using System.Collections;
using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Run
{
    /// <summary>
    /// Displays the run end screen with ASCII banner, staged stats reveal,
    /// and a "Play Again" button that scales in last.
    /// </summary>
    public class RunEndUI : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button playAgainButton;

        [Header("Theme")]
        [SerializeField] private TerminalThemeSO theme;

        private Coroutine _revealCoroutine;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<RunEndedEvent>(OnRunEnded);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            RefreshDisplay();
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<RunEndedEvent>(OnRunEnded);

            if (playAgainButton != null)
                playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);

            if (_revealCoroutine != null)
            {
                StopCoroutine(_revealCoroutine);
                _revealCoroutine = null;
            }

            if (bannerText != null) DOTween.Kill(bannerText);
            if (titleText != null) DOTween.Kill(titleText);
            if (statsText != null) DOTween.Kill(statsText);
        }

        private void OnRunEnded(RunEndedEvent evt)
        {
            RefreshDisplay(evt);
        }

        private void RefreshDisplay(RunEndedEvent evt = null)
        {
            bool won = evt?.Won ?? false;

            // ASCII Banner
            if (bannerText != null)
            {
                bannerText.text = ASCIIBanners.GetRunEndBanner(won);
                bannerText.color = won
                    ? (theme != null ? theme.amberBright : new Color(1f, 0.75f, 0f))
                    : (theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f));
            }

            if (titleText != null)
            {
                titleText.text = won ? "VICTORY!" : "DEFEAT";
                titleText.color = won
                    ? (theme != null ? theme.amberBright : new Color(1f, 0.75f, 0f))
                    : (theme != null ? theme.damageColor : new Color(1f, 0.15f, 0.1f));
            }

            // Staged stats reveal
            if (_revealCoroutine != null)
                StopCoroutine(_revealCoroutine);
            _revealCoroutine = StartCoroutine(StagedStatsReveal(evt));
        }

        private IEnumerator StagedStatsReveal(RunEndedEvent evt)
        {
            if (statsText == null) yield break;

            int score = evt?.FinalScore ?? (RunManager.Instance?.Score ?? 0);
            int encountersWon = RunManager.Instance?.EncountersWon ?? 0;
            int tomesCollected = Tomes.TomeManager.Instance?.TomeSystem?.Count ?? 0;
            int wordsUsed = RunManager.Instance?.UsedWords?.Count ?? 0;

            string[] lines = new[]
            {
                FormatStatLine("Final Score", score.ToString()),
                FormatStatLine("Encounters Won", encountersWon.ToString()),
                FormatStatLine("Tomes Collected", tomesCollected.ToString()),
                FormatStatLine("Words Used", wordsUsed.ToString())
            };

            // Reveal stats one by one
            statsText.text = "";
            for (int i = 0; i < lines.Length; i++)
            {
                yield return new WaitForSeconds(0.3f);
                if (i > 0) statsText.text += "\n";
                statsText.text += lines[i];
            }

            // Play Again button scales in last
            if (playAgainButton != null)
            {
                var btnRT = playAgainButton.GetComponent<RectTransform>();
                if (btnRT != null)
                {
                    btnRT.localScale = Vector3.zero;
                    yield return new WaitForSeconds(0.3f);
                    btnRT.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }

            _revealCoroutine = null;
        }

        private static string FormatStatLine(string label, string value)
        {
            const int totalWidth = 30;
            int dotsNeeded = totalWidth - label.Length - value.Length;
            if (dotsNeeded < 2) dotsNeeded = 2;
            string dots = new string('.', dotsNeeded);
            return $"{label} {dots} {value}";
        }

        private void OnPlayAgainClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }
    }
}
