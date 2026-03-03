using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using Spellwright.Run;
using Spellwright.UI;
using TMPro;
using UnityEngine;

namespace Spellwright.Encounter
{
    /// <summary>
    /// Displays an NPC ASCII art portrait that reacts to game events.
    /// Expression changes: correct guess -> Impressed (2s revert), wrong guess -> Amused (2s revert),
    /// HP < 25% -> Angry, encounter win -> Defeated, encounter loss -> Victorious, boss intro -> Angry.
    /// Uses DOTween punch-scale on expression change.
    /// </summary>
    public class NPCPortraitUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI portraitText;
        [SerializeField] private TerminalThemeSO theme;

        private string _npcId;
        private bool _isBoss;
        private NPCExpression _baseExpression = NPCExpression.Neutral;
        private NPCExpression _currentExpression = NPCExpression.Neutral;
        private Tween _revertTween;
        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Subscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Subscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Subscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<EncounterStartedEvent>(OnEncounterStarted);
            EventBus.Instance.Unsubscribe<GuessSubmittedEvent>(OnGuessSubmitted);
            EventBus.Instance.Unsubscribe<EncounterEndedEvent>(OnEncounterEnded);
            EventBus.Instance.Unsubscribe<HPChangedEvent>(OnHPChanged);
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);

            _revertTween?.Kill();
        }

        /// <summary>
        /// Called by EncounterUI when a new encounter starts.
        /// </summary>
        public void SetNPC(string npcId, bool isBoss)
        {
            _npcId = npcId;
            _isBoss = isBoss;
            _baseExpression = NPCExpression.Neutral;
            SetExpression(NPCExpression.Neutral, punch: false);
        }

        private void OnEncounterStarted(EncounterStartedEvent evt)
        {
            if (evt.NPC != null)
                SetNPC(evt.NPC.DisplayName, evt.NPC.IsBoss);
        }

        private void OnGuessSubmitted(GuessSubmittedEvent evt)
        {
            if (evt.Result.IsCorrect || (evt.Result.GuessType == GuessType.Letter && evt.Result.IsLetterInPhrase))
            {
                SetTemporaryExpression(NPCExpression.Impressed, 2f);
            }
            else if (evt.Result.IsValidWord || (evt.Result.GuessType == GuessType.Letter && !evt.Result.IsLetterAlreadyGuessed))
            {
                SetTemporaryExpression(NPCExpression.Amused, 2f);
            }
        }

        private void OnEncounterEnded(EncounterEndedEvent evt)
        {
            _revertTween?.Kill();
            SetExpression(evt.Won ? NPCExpression.Defeated : NPCExpression.Victorious, punch: true);
        }

        private void OnHPChanged(HPChangedEvent evt)
        {
            float hpPercent = evt.MaxHP > 0 ? (float)evt.NewHP / evt.MaxHP : 1f;
            if (hpPercent < 0.25f && _baseExpression != NPCExpression.Angry)
            {
                _baseExpression = NPCExpression.Angry;
                SetExpression(NPCExpression.Angry, punch: true);
            }
            else if (hpPercent >= 0.25f && _baseExpression == NPCExpression.Angry)
            {
                _baseExpression = NPCExpression.Neutral;
            }
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            _baseExpression = NPCExpression.Angry;
            SetExpression(NPCExpression.Angry, punch: true);
        }

        private void SetTemporaryExpression(NPCExpression expression, float duration)
        {
            _revertTween?.Kill();
            SetExpression(expression, punch: true);

            _revertTween = DOVirtual.DelayedCall(duration, () =>
            {
                SetExpression(_baseExpression, punch: false);
            }).SetUpdate(true);
        }

        private void SetExpression(NPCExpression expression, bool punch)
        {
            _currentExpression = expression;

            if (portraitText != null)
            {
                string art = NPCPortraitData.GetPortrait(_npcId, expression);
                portraitText.text = art;

                // Color the portrait based on expression
                Color color = GetExpressionColor(expression);
                portraitText.color = color;
            }

            if (punch && _rectTransform != null)
            {
                DOTween.Kill(_rectTransform, true);
                _rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.3f, 6, 0.5f)
                    .SetUpdate(true);
            }
        }

        private Color GetExpressionColor(NPCExpression expression)
        {
            if (theme == null) return new Color(0f, 1f, 0.33f);

            return expression switch
            {
                NPCExpression.Impressed => theme.successColor,
                NPCExpression.Angry => theme.damageColor,
                NPCExpression.Defeated => theme.warningColor,
                NPCExpression.Victorious => theme.damageColor,
                NPCExpression.Amused => theme.amberBright,
                _ => _isBoss ? theme.bossAccent : theme.phosphorGreen
            };
        }
    }
}
