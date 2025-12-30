using UnityEngine;
using Witherspoon.Game.Enemies;

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
        [Header("Debug Hotkeys")]
        [SerializeField] private KeyCode togglePathKey = KeyCode.BackQuote;
        [SerializeField] private bool pathVisualizationOnStart;
        private bool _initialized;
        private bool _pathVisualizationEnabled;

        private void Start()
        {
            if (autoStart)
            {
                Initialize();
            }

            if (!_initialized) return;
            _pathVisualizationEnabled = pathVisualizationOnStart;
            EnemyAgent.SetPathVisualization(_pathVisualizationEnabled);
        }

        private void Update()
        {
            if (!_initialized) return;

            waveManager?.Tick(Time.deltaTime);
            economyManager?.Tick(Time.deltaTime);
            HandlePathHotkey();
        }

        public void Initialize()
        {
            if (_initialized) return;

            gridManager?.Initialize();
            economyManager?.Initialize();
            waveManager?.Initialize(gridManager);

            _initialized = true;
            _pathVisualizationEnabled = pathVisualizationOnStart;
            EnemyAgent.SetPathVisualization(_pathVisualizationEnabled);
        }

        private void HandlePathHotkey()
        {
            if (togglePathKey == KeyCode.None) return;
            if (Input.GetKeyDown(togglePathKey))
            {
                _pathVisualizationEnabled = !_pathVisualizationEnabled;
                EnemyAgent.SetPathVisualization(_pathVisualizationEnabled);
            }
        }
    }
}
