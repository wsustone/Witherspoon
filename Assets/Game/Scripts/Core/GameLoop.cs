using UnityEngine;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Entry point for the Mythic Tower Defense simulation.
    /// Holds references to the main managers and coordinates the update loop.
    /// </summary>
    public class GameLoop : MonoBehaviour
    {
        [Header("Managers")]
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Map.GridManager gridManager;

        [Header("Runtime State")]
        [SerializeField] private bool autoStart = true;
        private bool _initialized;

        private void Start()
        {
            if (autoStart)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!_initialized) return;

            waveManager?.Tick(Time.deltaTime);
            economyManager?.Tick(Time.deltaTime);
        }

        public void Initialize()
        {
            if (_initialized) return;

            gridManager?.Initialize();
            economyManager?.Initialize();
            waveManager?.Initialize(gridManager);

            _initialized = true;
        }
    }
}
