using UnityEngine;
using UnityEngine.UI;

namespace Spellwright.Run
{
    /// <summary>
    /// Simple main menu with a "Start Run" button.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Button startButton;

        private void OnEnable()
        {
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);

            if (titleText != null)
                titleText.text = "SPELLWRIGHT";
        }

        private void OnDisable()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStartClicked);
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }
    }
}
