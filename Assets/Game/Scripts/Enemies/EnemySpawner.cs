using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Map;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Simple spawner that instantiates enemy prefabs along a predefined lane.
    /// </summary>
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

        [Header("Start / Goal Markers")]
        [SerializeField] private bool showMarkers = true;
        [SerializeField] private float markerInset = 0.1f;
        [SerializeField] private float markerHeight = 0.02f;
        [SerializeField] private Color startColor = new Color(0.1f, 0.85f, 0.6f, 0.65f);
        [SerializeField] private Color goalColor = new Color(0.95f, 0.3f, 0.2f, 0.65f);

        [Header("Path Preview")]
        [SerializeField] private bool showPathPreview = true;
        [SerializeField] private float pathWidth = 0.12f;
        [SerializeField] private Color pathColor = new Color(0.4f, 0.9f, 1f, 0.6f);

        [Header("Debug")]
        [SerializeField] private bool debugWaveSelection = false;

        private const float DefaultSpawnSpacing = 0.75f;
        private Transform _spawnOverride;
        private Transform _goalOverride;

        public void SpawnWave(int waveNumber, GridManager grid, WaveSpawnConfig? config = null)
        {
            var activeSpawn = GetActiveSpawnAnchor();
            var activeGoal = GetActiveGoalAnchor();
            if (activeSpawn == null || activeGoal == null)
            {
                Debug.LogWarning("EnemySpawner cannot spawn wave because spawn or goal anchor is missing.");
                return;
            }

            int count = config?.EnemyCountOverride ?? Mathf.Clamp(3 + waveNumber, 3, 25);
            float spacing = Mathf.Max(0.05f, config?.SpawnIntervalOverride ?? DefaultSpawnSpacing);
            var waveEnemy = SelectSpawnableEnemy(config?.ForcedEnemy, waveNumber);
            if (!IsSpawnable(waveEnemy))
            {
                Debug.LogWarning($"Wave {waveNumber} could not find a spawnable enemy definition (missing prefab).");
                return;
            }
            if (debugWaveSelection && waveEnemy != null)
            {
                Debug.Log($"[EnemySpawner] Wave {waveNumber} -> {waveEnemy.EnemyName}", this);
            }
            Vector3 startPos = activeSpawn.position;
            Vector3 goalPos = activeGoal.position;
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(waveEnemy, startPos, goalPos, i * spacing);
            }
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
            EnsureMarkers();
            UpdateMarkerTransforms();
            _pathDirty = true;
        }

        private Transform GetActiveSpawnAnchor() => _spawnOverride != null ? _spawnOverride : spawnPoint;
        private Transform GetActiveGoalAnchor() => _goalOverride != null ? _goalOverride : goalPoint;

        private EnemyDefinition SelectSpawnableEnemy(EnemyDefinition forced, int waveNumber)
        {
            if (IsSpawnable(forced)) return forced;

            var resolved = ResolveEnemyForWave(waveNumber);
            if (IsSpawnable(resolved)) return resolved;

            if (IsSpawnable(defaultEnemy)) return defaultEnemy;
            if (enemyFamilies != null && IsSpawnable(enemyFamilies.DefaultEnemy))
            {
                return enemyFamilies.DefaultEnemy;
            }

            return null;
        }

        private static bool IsSpawnable(EnemyDefinition candidate) =>
            candidate != null && candidate.Prefab != null;

        private EnemyDefinition ResolveEnemyForWave(int waveNumber)
        {
            if (enemyFamilies == null)
            {
                return IsSpawnable(defaultEnemy) ? defaultEnemy : null;
            }

            EnemyDefinition FirstSpawnable(params EnemyDefinition[] candidates)
            {
                foreach (var candidate in candidates)
                {
                    if (IsSpawnable(candidate)) return candidate;
                }
                return null;
            }

            EnemyDefinition PickFamilyEnemy()
            {
                if (waveNumber <= 2)
                {
                    return FirstSpawnable(enemyFamilies.Shades, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber <= 4)
                {
                    return FirstSpawnable(enemyFamilies.Glimmers, enemyFamilies.Shades, enemyFamilies.DefaultEnemy);
                }
                if (waveNumber <= 6)
                {
                    return FirstSpawnable(enemyFamilies.Husks, enemyFamilies.Glimmers, enemyFamilies.DefaultEnemy);
                }
                // Prioritize boss waves first so they are not shadowed by other modulo checks
                if (waveNumber >= 10 && waveNumber % 10 == 0)
                {
                    return FirstSpawnable(enemyFamilies.NightmareOfDread, enemyFamilies.NightmareOfStagnation, enemyFamilies.NightmareOfRuin, enemyFamilies.NightmareOfDiscord, enemyFamilies.DefaultEnemy);
                }
                // Objective waves (anchor/shard/path) every 7th wave; prefer Anchor Breaker so Hunger Essence appears reliably
                if (waveNumber % 7 == 0)
                {
                    return FirstSpawnable(enemyFamilies.AnchorBreaker, enemyFamilies.ShardThief, enemyFamilies.Pathforger, enemyFamilies.DefaultEnemy);
                }
                // Elite waves every 5th wave otherwise
                if (waveNumber % 5 == 0)
                {
                    return FirstSpawnable(enemyFamilies.Nightglass, enemyFamilies.Dreadbound, enemyFamilies.Riftrunner, enemyFamilies.DefaultEnemy);
                }

                return FirstSpawnable(enemyFamilies.Shades, enemyFamilies.Glimmers, enemyFamilies.Husks, enemyFamilies.DefaultEnemy);
            }

            var pick = PickFamilyEnemy();
            if (pick != null) return pick;

            return IsSpawnable(defaultEnemy) ? defaultEnemy : null;
        }

        private void SpawnEnemy(EnemyDefinition definition, Vector3 start, Vector3 goal, float delay)
        {
            if (definition?.Prefab == null) return;
            var enemy = Instantiate(definition.Prefab, start, Quaternion.identity);
            if (enemy.TryGetComponent(out EnemyAgent agent))
            {
                agent.Initialize(definition, goal, delay, _attachedGrid);
            }
        }

        private GridManager _attachedGrid;

        private GameObject _startMarker;
        private GameObject _goalMarker;
        private LineRenderer _pathPreviewRenderer;
        private readonly List<Vector3> _gridPathBuffer = new();
        private readonly List<Vector3> _pathPreviewPoints = new();
        private Vector3 _lastStartPosition;
        private Vector3 _lastGoalPosition;
        private bool _pathDirty = true;
        private float _cellSize = 1f;

        private void Awake()
        {
            EnsureMarkers();
            EnsurePathRenderer();
        }

        private void OnEnable()
        {
            _pathDirty = true;
        }

        private void OnDisable()
        {
            DetachGrid();
        }

        private void OnDestroy()
        {
            DetachGrid();
        }

        private void Update()
        {
            if (showPathPreview)
            {
                TrackAnchorMovement();
                if (_pathDirty)
                {
                    RecalculatePathPreview();
                }
                UpdatePathPreviewVisibility();
            }
            else if (_pathPreviewRenderer != null)
            {
                _pathPreviewRenderer.gameObject.SetActive(false);
            }
        }

        public void AttachGrid(GridManager grid)
        {
            if (_attachedGrid == grid) return;
            DetachGrid();
            _attachedGrid = grid;
            if (_attachedGrid != null)
            {
                _attachedGrid.GridChanged += HandleGridChanged;
                _cellSize = Mathf.Max(0.1f, _attachedGrid.CellSize);
            }
            UpdateMarkerTransforms();
            _pathDirty = true;
        }

        private void DetachGrid()
        {
            if (_attachedGrid != null)
            {
                _attachedGrid.GridChanged -= HandleGridChanged;
            }
            _attachedGrid = null;
        }

        private void HandleGridChanged()
        {
            _pathDirty = true;
        }

        private void EnsureMarkers()
        {
            if (!showMarkers) return;
            var activeSpawn = GetActiveSpawnAnchor();
            if (activeSpawn != null)
            {
                _startMarker = CreateOrUpdateMarker(_startMarker, activeSpawn, "CreepStartMarker", startColor);
            }
            var activeGoal = GetActiveGoalAnchor();
            if (activeGoal != null)
            {
                _goalMarker = CreateOrUpdateMarker(_goalMarker, activeGoal, "CreepGoalMarker", goalColor);
            }
        }

        private GameObject CreateOrUpdateMarker(GameObject existing, Transform anchor, string name, Color color)
        {
            var marker = existing;
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Quad);
                marker.name = name;
                ApplyMarkerMaterial(marker, color);
                RemoveCollider(marker);
            }

            marker.transform.SetParent(anchor, worldPositionStays: false);
            marker.transform.localPosition = new Vector3(0f, 0f, markerHeight);
            marker.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            float size = Mathf.Max(0.2f, _cellSize - markerInset);
            marker.transform.localScale = new Vector3(size, size, 1f);
            marker.SetActive(showMarkers);

            return marker;
        }

        private static void ApplyMarkerMaterial(GameObject go, Color color)
        {
            if (!go.TryGetComponent(out MeshRenderer renderer)) return;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader)
            {
                color = color
            };
            renderer.material = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void RemoveCollider(GameObject go)
        {
            if (go.TryGetComponent<Collider>(out var collider))
            {
                Object.Destroy(collider);
            }
        }

        private void TrackAnchorMovement()
        {
            var activeSpawn = GetActiveSpawnAnchor();
            var activeGoal = GetActiveGoalAnchor();
            if (activeSpawn == null || activeGoal == null) return;
            Vector3 start = activeSpawn.position;
            Vector3 goal = activeGoal.position;
            if ((start - _lastStartPosition).sqrMagnitude > 0.001f ||
                (goal - _lastGoalPosition).sqrMagnitude > 0.001f)
            {
                _lastStartPosition = start;
                _lastGoalPosition = goal;
                _pathDirty = true;
            }
        }

        private void UpdateMarkerTransforms()
        {
            if (!showMarkers) return;
            var activeSpawn = GetActiveSpawnAnchor();
            if (_startMarker != null && activeSpawn != null)
            {
                CreateOrUpdateMarker(_startMarker, activeSpawn, _startMarker.name, startColor);
            }
            var activeGoal = GetActiveGoalAnchor();
            if (_goalMarker != null && activeGoal != null)
            {
                CreateOrUpdateMarker(_goalMarker, activeGoal, _goalMarker.name, goalColor);
            }
        }

        private void RecalculatePathPreview()
        {
            _pathDirty = false;
            var activeSpawn = GetActiveSpawnAnchor();
            var activeGoal = GetActiveGoalAnchor();
            if (!showPathPreview || activeSpawn == null || activeGoal == null) return;

            EnsurePathRenderer();
            if (_pathPreviewRenderer == null) return;

            Vector3 start = activeSpawn.position;
            Vector3 goal = activeGoal.position;

            _gridPathBuffer.Clear();
            bool found = _attachedGrid != null && _attachedGrid.TryFindPath(start, goal, _gridPathBuffer);

            _pathPreviewPoints.Clear();
            _pathPreviewPoints.Add(new Vector3(start.x, start.y, 0f));
            if (found)
            {
                for (int i = 0; i < _gridPathBuffer.Count; i++)
                {
                    var p = _gridPathBuffer[i];
                    _pathPreviewPoints.Add(new Vector3(p.x, p.y, 0f));
                }
            }
            else
            {
                _pathPreviewPoints.Add(new Vector3(goal.x, goal.y, 0f));
            }

            _pathPreviewRenderer.positionCount = _pathPreviewPoints.Count;
            _pathPreviewRenderer.SetPositions(_pathPreviewPoints.ToArray());
        }

        private void EnsurePathRenderer()
        {
            if (_pathPreviewRenderer != null) return;
            var go = new GameObject("CreepPathPreview");
            go.transform.SetParent(transform, false);
            _pathPreviewRenderer = go.AddComponent<LineRenderer>();
            _pathPreviewRenderer.useWorldSpace = true;
            _pathPreviewRenderer.loop = false;
            _pathPreviewRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _pathPreviewRenderer.widthMultiplier = pathWidth;
            _pathPreviewRenderer.numCapVertices = 2;
            _pathPreviewRenderer.sortingOrder = 950;
            _pathPreviewRenderer.startColor = pathColor;
            _pathPreviewRenderer.endColor = pathColor;
            _pathPreviewRenderer.gameObject.SetActive(false);
        }

        private void UpdatePathPreviewVisibility()
        {
            if (_pathPreviewRenderer == null) return;
            bool shouldShow = showPathPreview && EnemyAgent.PathsVisible && _pathPreviewRenderer.positionCount >= 2;
            if (shouldShow)
            {
                _pathPreviewRenderer.startColor = pathColor;
                _pathPreviewRenderer.endColor = pathColor;
            }
            _pathPreviewRenderer.gameObject.SetActive(shouldShow);
        }
    }
}
