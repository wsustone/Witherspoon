using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Witherspoon.Game.Core;

namespace Witherspoon.Game.UI
{
    public class DefeatOverlay : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text reasonText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            if (session == null)
            {
                session = FindObjectOfType<GameSession>();
            }
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitClicked);
            }
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.OnDefeat += HandleDefeat;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.OnDefeat -= HandleDefeat;
            }
        }

        private void HandleDefeat(GameSession s, string reason)
        {
            if (titleText != null) titleText.text = "Defeat";
            if (reasonText != null) reasonText.text = reason;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        public void OnRestartClicked()
        {
            // Unpause and reload active scene
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnExitClicked()
        {
            // For now, restart as well; later route to main menu when implemented
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
