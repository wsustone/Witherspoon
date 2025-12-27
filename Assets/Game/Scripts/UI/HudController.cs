using UnityEngine;
using TMPro;
using Witherspoon.Game.Core;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Basic HUD showing gold and wave countdown.
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private WaveManager waveManager;

        [Header("UI")]
        [SerializeField] private TMP_Text goldLabel;
        [SerializeField] private TMP_Text waveLabel;

        private void OnEnable()
        {
            if (economyManager != null)
            {
                economyManager.OnGoldChanged += HandleGoldChanged;
            }
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.OnGoldChanged -= HandleGoldChanged;
            }
        }

        private void Update()
        {
            if (waveManager != null && waveLabel != null)
            {
                waveLabel.text = $"Wave {waveManager.CurrentWave + 1} in {waveManager.TimeUntilNextWave:0.0}s";
            }
        }

        private void HandleGoldChanged(int amount)
        {
            if (goldLabel != null)
            {
                goldLabel.text = $"Gold: {amount}";
            }
        }
    }
}
