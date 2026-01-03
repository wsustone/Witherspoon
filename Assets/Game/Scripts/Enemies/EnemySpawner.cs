using UnityEngine;
using Witherspoon.Game.Map;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Core spawner that instantiates enemy prefabs along a predefined lane.
    /// </summary>
    [RequireComponent(typeof(WaveEnemySelector))]
    [RequireComponent(typeof(SpawnMarkers))]
    public class EnemySpawner : MonoBehaviour
    {
        public readonly struct WaveSpawnConfig
        {
            public WaveSpawnConfig(EnemyDefinition forcedEnemy, int? enemyCountOverride, float? spawnIntervalOverride)
            {
                ForcedEnemy = forcedEnemy;
                EnemyCountOverride = enemyCountOverride;
                SpawnIntervalOverride = spawnIntervalOverride;
            }

            public EnemyDefinition ForcedEnemy { get; }
            public int? EnemyCountOverride { get; }
            public float? SpawnIntervalOverride { get; }
        }

        [SerializeField] private EnemyDefinition defaultEnemy;
        [SerializeField] private EnemyFamilyLibrary enemyFamilies;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform goalPoint;

        private const float DefaultSpawnSpacing = 0.75f;
        private Transform _spawnOverride;
        private Transform _goalOverride;
        private GridManager _attachedGrid;

        private WaveEnemySelector _selector;
        private SpawnMarkers _markers;

        private void Awake()
        {
            _selector = GetComponent<WaveEnemySelector>();
            if (_selector == null)
            {
                _selector = gameObject.AddComponent<WaveEnemySelector>();
            }
            
            if (defaultEnemy == null && enemyFamilies == null)
            {
                Debug.LogError($"[EnemySpawner] {gameObject.name} has no defaultEnemy or enemyFamilies assigned! Please assign these in the Inspector.", this);
            }
            
            _selector.Initialize(defaultEnemy, enemyFamilies);
            
            _markers = GetComponent<SpawnMarkers>();
            if (_markers == null)
            {
                _markers = gameObject.AddComponent<SpawnMarkers>();
            }
        }

        private void Start()
        {
            _markers?.Initialize(GetActiveSpawnAnchor(), GetActiveGoalAnchor(), _attachedGrid);
        }

        private void Update()
        {
            _markers?.UpdateMarkers(GetActiveSpawnAnchor(), GetActiveGoalAnchor());
        }

        public int SpawnWave(int waveNumber, GridManager grid, WaveSpawnConfig? config = null)
        {
            var activeSpawn = GetActiveSpawnAnchor();
            var activeGoal = GetActiveGoalAnchor();
            if (activeSpawn == null || activeGoal == null)
            {
                Debug.LogWarning("EnemySpawner cannot spawn wave because spawn or goal anchor is missing.");
                return 0;
            }

            int count = config?.EnemyCountOverride ?? Mathf.Clamp(3 + waveNumber, 3, 25);
            float spacing = Mathf.Max(0.05f, config?.SpawnIntervalOverride ?? DefaultSpawnSpacing);
            var waveEnemy = _selector?.SelectSpawnableEnemy(config?.ForcedEnemy, waveNumber);
            if (!WaveEnemySelector.IsSpawnable(waveEnemy))
            {
                Debug.LogWarning($"Wave {waveNumber} could not find a spawnable enemy definition (missing prefab).");
                return 0;
            }

            _selector?.LogWaveSelection(waveNumber, waveEnemy);

            Vector3 startPos = activeSpawn.position;
            Vector3 goalPos = activeGoal.position;
            int spawned = 0;
            for (int i = 0; i < count; i++)
            {
                if (SpawnEnemy(waveEnemy, startPos, goalPos, i * spacing))
                {
                    spawned++;
                }
            }
            return spawned;
        }

        public void SetAnchorOverride(Transform spawnOverride, Transform goalOverride)
        {
            _spawnOverride = spawnOverride != null ? spawnOverride : _spawnOverride;
            _goalOverride = goalOverride != null ? goalOverride : _goalOverride;
            RefreshAnchorState();
        }

        public void ClearAnchorOverride()
        {
            _spawnOverride = null;
            _goalOverride = null;
            RefreshAnchorState();
        }

        private void RefreshAnchorState()
        {
            _markers?.RefreshMarkerPositions(GetActiveSpawnAnchor(), GetActiveGoalAnchor());
        }

        private Transform GetActiveSpawnAnchor() => _spawnOverride != null ? _spawnOverride : spawnPoint;
        private Transform GetActiveGoalAnchor() => _goalOverride != null ? _goalOverride : goalPoint;

        public EnemyDefinition PreviewEnemyForWave(int waveNumber)
        {
            return _selector?.PreviewEnemyForWave(waveNumber);
        }

        private bool SpawnEnemy(EnemyDefinition definition, Vector3 start, Vector3 goal, float delay)
        {
            if (definition?.Prefab == null) return false;
            var enemy = Instantiate(definition.Prefab, start, Quaternion.identity);
            if (enemy.TryGetComponent(out EnemyAgent agent))
            {
                agent.Initialize(definition, goal, delay, _attachedGrid);
                return true;
            }
            return false;
        }

        public void AttachGrid(GridManager grid)
        {
            _attachedGrid = grid;
            _markers?.Initialize(GetActiveSpawnAnchor(), GetActiveGoalAnchor(), grid);
        }
    }
}
