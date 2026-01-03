using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Witherspoon.Game.Core;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Handles the Start Wave button: initializes the game loop on first press,
    /// triggers the next wave, and disables itself while waves are in progress.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class WaveStartButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameLoop gameLoop;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        [Header("Labels")]
        [SerializeField] private string readyLabel = "Start Wave";
        [SerializeField] private string inProgressLabel = "Wave In Progress";
        [SerializeField] private string countdownFormat = "Starting in {0:0.0}s";
        [SerializeField] private string intermissionFormat = "Next wave in {0:0.0}s";

        private void Reset()
        {
            button = GetComponent<Button>();
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }
        }

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>();
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleButtonClicked);
            }
            UpdateVisualState();
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleButtonClicked);
            }
        }

        private void Update()
        {
            UpdateVisualState();
        }

        private void HandleButtonClicked()
        {
            Debug.Log("[WaveStartButton] Button clicked!");
            if (gameLoop != null && !gameLoop.Initialized)
            {
                gameLoop.Initialize();
                Debug.Log("[WaveStartButton] GameLoop initialized");
            }

            if (waveManager != null)
            {
                Debug.Log("[WaveStartButton] Calling RequestStartNextWave");
                waveManager.RequestStartNextWave();
            }
            else
            {
                Debug.LogWarning("[WaveStartButton] WaveManager is null!");
            }
        }

        private void UpdateVisualState()
        {
            if (button == null) return;

            if (waveManager == null)
            {
                button.interactable = false;
                if (label != null)
                {
                    label.text = "Waiting for WaveManager...";
                }
                return;
            }

            var state = waveManager.State;
            bool enable = state == WaveManager.WaveState.WaitingForInput;
            button.interactable = enable;
            
            // Ensure the button GameObject is active when waiting for input
            if (state == WaveManager.WaveState.WaitingForInput && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Debug.Log("[WaveStartButton] Re-enabled button for next wave");
            }

            if (label == null) return;

            switch (state)
            {
                case WaveManager.WaveState.WaitingForInput:
                    label.text = readyLabel;
                    break;
                case WaveManager.WaveState.Countdown:
                    label.text = string.Format(countdownFormat, waveManager.TimeUntilNextWave);
                    break;
                case WaveManager.WaveState.Intermission:
                    label.text = string.Format(intermissionFormat, waveManager.IntermissionTimeRemaining);
                    break;
                default:
                    label.text = inProgressLabel;
                    break;
            }
        }
    }
}
