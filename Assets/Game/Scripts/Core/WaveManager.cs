using UnityEngine;
using Witherspoon.Game.Map;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Coordinates enemy wave scheduling, spawns, and escalation.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Wave Timing")]
        [SerializeField] private float preWaveDelay = 3f;
        [SerializeField] private float timeBetweenWaves = 12f;

        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;

        private GridManager _gridManager;
        private float _timer;
        private int _currentWave;
        private bool _active;

        public int CurrentWave => Mathf.Max(0, _currentWave - 1);
        public float TimeUntilNextWave => Mathf.Max(0f, _timer);

        public void Initialize(GridManager gridManager)
        {
            _gridManager = gridManager;
            if (enemySpawner != null && gridManager != null)
            {
                enemySpawner.AttachGrid(gridManager);
            }
            _timer = preWaveDelay;
            _currentWave = 0;
            _active = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_active) return;

            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                StartWave();
                _timer = timeBetweenWaves;
            }
        }

        private void StartWave()
        {
            _currentWave++;
            if (enemySpawner != null && _gridManager != null)
            {
                enemySpawner.SpawnWave(_currentWave, _gridManager);
            }
        }

        public string GetPreviewEnemyNameForWave(int waveNumber)
        {
            if (enemySpawner == null) return null;
            EnemyDefinition preview = enemySpawner.PreviewEnemyForWave(waveNumber);
            return preview != null ? preview.EnemyName : null;
        }
    }
}
