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
        [SerializeField] private EnemyDefinition defaultEnemy;
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

        public void SpawnWave(int waveNumber, GridManager grid)
        {
            int count = Mathf.Clamp(3 + waveNumber, 3, 25);
            for (int i = 0; i < count; i++)
            {
                SpawnEnemy(defaultEnemy, spawnPoint.position, goalPoint.position, i * 0.75f);
            }
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
            if (spawnPoint != null)
            {
                _startMarker = CreateOrUpdateMarker(_startMarker, spawnPoint, "CreepStartMarker", startColor);
            }
            if (goalPoint != null)
            {
                _goalMarker = CreateOrUpdateMarker(_goalMarker, goalPoint, "CreepGoalMarker", goalColor);
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
            if (spawnPoint == null || goalPoint == null) return;
            Vector3 start = spawnPoint.position;
            Vector3 goal = goalPoint.position;
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
            if (_startMarker != null && spawnPoint != null)
            {
                CreateOrUpdateMarker(_startMarker, spawnPoint, _startMarker.name, startColor);
            }
            if (_goalMarker != null && goalPoint != null)
            {
                CreateOrUpdateMarker(_goalMarker, goalPoint, _goalMarker.name, goalColor);
            }
        }

        private void RecalculatePathPreview()
        {
            _pathDirty = false;
            if (!showPathPreview || spawnPoint == null || goalPoint == null) return;

            EnsurePathRenderer();
            if (_pathPreviewRenderer == null) return;

            Vector3 start = spawnPoint.position;
            Vector3 goal = goalPoint.position;

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
