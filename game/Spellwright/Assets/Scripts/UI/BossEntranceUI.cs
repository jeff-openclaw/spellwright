using System.Collections;
using DG.Tweening;
using Spellwright.Core;
using Spellwright.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.UI
{
    /// <summary>
    /// Full-screen boss intro overlay: screen darkens -> ASCII banner types in line-by-line ->
    /// boss name punch-scales in red -> glitch flicker -> fade out.
    /// </summary>
    public class BossEntranceUI : MonoBehaviour
    {
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private Image overlayBg;
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private TextMeshProUGUI bossNameText;
        [SerializeField] private TerminalThemeSO theme;

        private Coroutine _entranceCoroutine;

        private void OnEnable()
        {
            EventBus.Instance.Subscribe<BossIntroEvent>(OnBossIntro);
        }

        private void OnDisable()
        {
            EventBus.Instance.Unsubscribe<BossIntroEvent>(OnBossIntro);
            _entranceCoroutine = null;
            DOTween.Kill(bannerText);
            DOTween.Kill(bossNameText);
            DOTween.Kill(overlayBg);
        }

        private void OnBossIntro(BossIntroEvent evt)
        {
            if (_entranceCoroutine != null)
                StopCoroutine(_entranceCoroutine);
            _entranceCoroutine = StartCoroutine(PlayEntrance(evt.BossName));
        }

        private IEnumerator PlayEntrance(string bossName)
        {
            if (overlayPanel == null) yield break;

            overlayPanel.SetActive(true);
            Color bossColor = theme != null ? theme.bossAccent : new Color(0.85f, 0.1f, 0.1f);
            Color dimColor = new Color(0f, 0f, 0f, 0.85f);

            // Reset
            if (overlayBg != null) overlayBg.color = new Color(0, 0, 0, 0);
            if (bannerText != null) { bannerText.text = ""; bannerText.alpha = 0f; }
            if (bossNameText != null) { bossNameText.text = ""; bossNameText.alpha = 0f; }

            // Phase 1: Darken screen (0.5s)
            if (overlayBg != null)
            {
                overlayBg.DOColor(dimColor, 0.5f).SetUpdate(true);
                yield return new WaitForSeconds(0.5f);
            }

            // Phase 2: Type in banner line by line (1.0s)
            if (bannerText != null)
            {
                bannerText.alpha = 1f;
                bannerText.color = bossColor;
                string[] lines = ASCIIBanners.BossEntrance.Split('\n');
                bannerText.text = "";

                foreach (string line in lines)
                {
                    bannerText.text += line + "\n";
                    yield return new WaitForSeconds(1.0f / lines.Length);
                }
            }

            // Phase 3: Boss name punch-scale in red (0.5s)
            if (bossNameText != null)
            {
                bossNameText.text = bossName.ToUpperInvariant();
                bossNameText.color = bossColor;
                bossNameText.alpha = 1f;

                var rt = bossNameText.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.localScale = Vector3.zero;
                    rt.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
                }
                yield return new WaitForSeconds(0.5f);
            }

            // Phase 4: Glitch flicker (0.3s)
            if (overlayBg != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    overlayBg.color = new Color(bossColor.r, bossColor.g, bossColor.b,
                        Random.Range(0.3f, 0.8f));
                    yield return new WaitForSeconds(0.05f);
                }
            }

            // Phase 5: Fade out (0.5s)
            if (bannerText != null)
                bannerText.DOFade(0f, 0.5f).SetUpdate(true);
            if (bossNameText != null)
                bossNameText.DOFade(0f, 0.5f).SetUpdate(true);
            if (overlayBg != null)
                overlayBg.DOColor(new Color(0, 0, 0, 0), 0.5f).SetUpdate(true);

            yield return new WaitForSeconds(0.5f);

            overlayPanel.SetActive(false);
            _entranceCoroutine = null;
        }
    }
}
