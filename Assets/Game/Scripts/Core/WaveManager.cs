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
        [SerializeField] private float timeBetweenWaves = 15f;

        [Header("Control")]
        [SerializeField] private bool manualWaveAdvance = true;

        [Header("References")]
        [SerializeField] private EnemySpawner enemySpawner;

        public enum WaveState
        {
            WaitingForInput,
            Countdown,
            Spawning,
            Intermission
        }

        private GridManager _gridManager;
        private float _timer;
        private int _currentWave;
        private int _enemiesAlive;
        private WaveState _state = WaveState.WaitingForInput;
        private bool _active;

        public int CurrentWave => Mathf.Max(0, _currentWave - 1);
        public int NextWave => _currentWave + 1;
        public float TimeUntilNextWave => _state == WaveState.Countdown ? Mathf.Max(0f, _timer) : 0f;
        public float IntermissionTimeRemaining => _state == WaveState.Intermission ? Mathf.Max(0f, _timer) : 0f;
        public bool ManualAdvanceEnabled => manualWaveAdvance;
        public WaveState State => _state;

        public void Initialize(GridManager gridManager)
        {
            _gridManager = gridManager;
            if (enemySpawner != null && gridManager != null)
            {
                enemySpawner.AttachGrid(gridManager);
            }
            EnemyAgent.OnAnyKilled += HandleEnemyRemoved;
            EnemyAgent.OnAnyReachedGoal += HandleEnemyRemoved;

            _timer = manualWaveAdvance ? 0f : preWaveDelay;
            _currentWave = 0;
            _enemiesAlive = 0;
            _state = manualWaveAdvance ? WaveState.WaitingForInput : WaveState.Countdown;
            _active = true;
        }

        public void Tick(float deltaTime)
        {
            if (!_active) return;
            switch (_state)
            {
                case WaveState.WaitingForInput:
                    break;
                case WaveState.Countdown:
                    _timer -= deltaTime;
                    if (_timer <= 0f)
                    {
                        StartWave();
                    }
                    break;
                case WaveState.Spawning:
                    if (_enemiesAlive <= 0)
                    {
                        BeginIntermission();
                    }
                    break;
                case WaveState.Intermission:
                    if (_enemiesAlive > 0)
                    {
                        _state = WaveState.Spawning;
                        break;
                    }
                    if (_timer > 0f)
                    {
                        _timer -= deltaTime;
                    }
                    if (_timer <= 0f)
                    {
                        if (manualWaveAdvance)
                        {
                            _state = WaveState.WaitingForInput;
                        }
                        else
                        {
                            BeginCountdown();
                        }
                    }
                    break;
            }
        }

        public void RequestStartNextWave()
        {
            if (!manualWaveAdvance && _state != WaveState.WaitingForInput) return;
            if (_state == WaveState.WaitingForInput || (manualWaveAdvance && _state == WaveState.Intermission && _enemiesAlive <= 0 && _timer <= 0f))
            {
                BeginCountdown();
            }
        }

        private void BeginCountdown()
        {
            _state = WaveState.Countdown;
            _timer = Mathf.Max(0.25f, preWaveDelay);
        }

        private void StartWave()
        {
            _currentWave++;
            _state = WaveState.Spawning;
            if (enemySpawner != null && _gridManager != null)
            {
                int spawned = enemySpawner.SpawnWave(_currentWave, _gridManager);
                _enemiesAlive += Mathf.Max(0, spawned);
            }
            if (_enemiesAlive <= 0)
            {
                BeginIntermission();
            }
        }

        private void BeginIntermission()
        {
            _state = WaveState.Intermission;
            _timer = Mathf.Max(1.5f, timeBetweenWaves);
        }

        private void HandleEnemyRemoved(EnemyAgent agent)
        {
            if (_enemiesAlive > 0)
            {
                _enemiesAlive = Mathf.Max(0, _enemiesAlive - 1);
                if (_enemiesAlive == 0 && _state == WaveState.Spawning)
                {
                    BeginIntermission();
                }
            }
        }

        public string GetPreviewEnemyNameForWave(int waveNumber)
        {
            if (enemySpawner == null) return null;
            EnemyDefinition preview = enemySpawner.PreviewEnemyForWave(waveNumber);
            return preview != null ? preview.EnemyName : null;
        }

        private void OnDestroy()
        {
            EnemyAgent.OnAnyKilled -= HandleEnemyRemoved;
            EnemyAgent.OnAnyReachedGoal -= HandleEnemyRemoved;
        }
    }
}
