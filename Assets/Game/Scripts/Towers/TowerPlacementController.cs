using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Core;
using Witherspoon.Game.Data;
using Witherspoon.Game.Map;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Very simple tower placement prototype: click a grid cell to place the selected tower.
    /// </summary>
    public class TowerPlacementController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Camera worldCamera;

        [Header("Build Options")]
        [SerializeField] private TowerDefinition defaultTower;
        [SerializeField] private KeyCode cancelPlacementKey = KeyCode.Escape;
        private TowerDefinition _currentTower;

        [Header("Placement Preview")]
        [SerializeField] private Color previewValidColor = new(0.4f, 1f, 0.7f, 0.65f);
        [SerializeField] private Color previewBlockedColor = new(1f, 0.35f, 0.35f, 0.65f);
        [SerializeField] private int previewSortingOrder = 2000;
        [SerializeField] private float rangeRingWidth = 0.05f;
        [SerializeField] private int rangeRingSegments = 48;

        private SpriteRenderer _previewRenderer;
        private Vector2Int _hoverCell = new(int.MinValue, int.MinValue);
        private bool _hoverCellIsBuildable;
        private readonly Dictionary<TowerDefinition, PreviewVisual> _previewCache = new();
        private LineRenderer _rangeRingRenderer;

        private struct PreviewVisual
        {
            public Sprite Sprite;
            public Vector3 Scale;
        }

        private void Reset()
        {
            worldCamera = Camera.main;
        }

        private void Start()
        {
            if (defaultTower != null)
            {
                SelectTower(defaultTower);
            }
        }

        private void Update()
        {
            HandleCancelInput();

            UpdatePreview();

            if (_currentTower == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceTowerAtHoverCell();
            }
        }

        public bool IsPlacing => _currentTower != null;

        public void SelectTower(TowerDefinition definition)
        {
            _currentTower = definition;
            if (_currentTower == null)
            {
                HidePreview();
                return;
            }
            RefreshPreviewVisual();
        }

        public void CancelPlacement()
        {
            if (_currentTower == null) return;
            _currentTower = null;
            HidePreview();
        }

        private void UpdatePreview()
        {
            if (gridManager == null || _currentTower == null)
            {
                HidePreview();
                return;
            }

            Vector3? cursorWorld = GetCursorWorldPoint();
            if (cursorWorld == null)
            {
                HidePreview();
                return;
            }

            Vector2Int cell = gridManager.WorldToGrid(cursorWorld.Value);
            bool buildable = gridManager.IsCellFree(cell);

            var visual = GetOrCreatePreviewVisual(_currentTower);
            if (visual.Sprite == null)
            {
                HidePreview();
                return;
            }

            EnsurePreviewRenderer();
            _previewRenderer.sprite = visual.Sprite;
            _previewRenderer.transform.localScale = visual.Scale;
            _previewRenderer.transform.position = gridManager.GridToWorld(cell);
            _previewRenderer.color = GetGhostColor(buildable);
            _previewRenderer.gameObject.SetActive(true);

            _hoverCell = cell;
            _hoverCellIsBuildable = buildable;

            UpdateRangeRingVisual(_previewRenderer.transform.position, _currentTower.Range, GetRangeRingColor(buildable));
        }

        private void TryPlaceTowerAtHoverCell()
        {
            if (gridManager == null || economyManager == null) return;
            if (_currentTower == null || _currentTower.TowerPrefab == null) return;
            if (_hoverCell.x == int.MinValue) return;

            if (!_hoverCellIsBuildable)
            {
                Debug.Log("Placement blocked: cell already occupied or out of bounds.");
                return;
            }

            if (!economyManager.TrySpend(_currentTower.BuildCost))
            {
                Debug.Log("Placement failed: not enough gold.");
                return;
            }

            Vector3 spawnPos = gridManager.GridToWorld(_hoverCell);
            var tower = Instantiate(_currentTower.TowerPrefab, spawnPos, Quaternion.identity);
            tower.name = $"{_currentTower.TowerName}_Tower";

            gridManager.SetBlocked(_hoverCell, true);
        }

        private void RefreshPreviewVisual()
        {
            if (_currentTower == null)
            {
                HidePreview();
                return;
            }

            var visual = GetOrCreatePreviewVisual(_currentTower);
            if (visual.Sprite == null)
            {
                HidePreview();
                return;
            }

            EnsurePreviewRenderer();
            _previewRenderer.sprite = visual.Sprite;
            _previewRenderer.transform.localScale = visual.Scale;
        }

        private PreviewVisual GetOrCreatePreviewVisual(TowerDefinition definition)
        {
            if (definition == null) return default;
            if (_previewCache.TryGetValue(definition, out var visual))
            {
                return visual;
            }

            visual = new PreviewVisual
            {
                Sprite = null,
                Scale = Vector3.one
            };

            if (definition.TowerPrefab != null)
            {
                var prefabRenderer = definition.TowerPrefab.GetComponentInChildren<SpriteRenderer>();
                if (prefabRenderer != null)
                {
                    visual.Sprite = prefabRenderer.sprite;
                    visual.Scale = prefabRenderer.transform.localScale;
                }
            }

            _previewCache[definition] = visual;
            return visual;
        }

        private void EnsurePreviewRenderer()
        {
            if (_previewRenderer != null) return;

            var previewGo = new GameObject("TowerPlacementPreview")
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            _previewRenderer = previewGo.AddComponent<SpriteRenderer>();
            _previewRenderer.sortingOrder = previewSortingOrder;
            previewGo.SetActive(false);
        }

        private void EnsureRangeRingRenderer()
        {
            if (_rangeRingRenderer != null) return;

            var ringGo = new GameObject("TowerRangeRing")
            {
                hideFlags = HideFlags.HideInHierarchy
            };

            _rangeRingRenderer = ringGo.AddComponent<LineRenderer>();
            _rangeRingRenderer.useWorldSpace = true;
            _rangeRingRenderer.loop = true;
            _rangeRingRenderer.widthMultiplier = rangeRingWidth;
            _rangeRingRenderer.numCapVertices = 2;
            _rangeRingRenderer.material = new Material(Shader.Find("Sprites/Default"));
            ringGo.SetActive(false);
        }

        private void HidePreview()
        {
            _hoverCell = new Vector2Int(int.MinValue, int.MinValue);
            _hoverCellIsBuildable = false;
            if (_previewRenderer != null)
            {
                _previewRenderer.gameObject.SetActive(false);
            }
            if (_rangeRingRenderer != null)
            {
                _rangeRingRenderer.gameObject.SetActive(false);
            }
        }

        private Vector3? GetCursorWorldPoint()
        {
            var cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (cameraToUse == null) return null;

            Vector3 worldPoint = cameraToUse.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0f;
            return worldPoint;
        }

        private Color GetGhostColor(bool buildable)
        {
            if (_currentTower == null) return buildable ? previewValidColor : previewBlockedColor;

            Color baseColor = _currentTower.HighlightColor;
            baseColor.a = previewValidColor.a;

            if (buildable)
            {
                return baseColor;
            }

            Color blocked = Color.Lerp(baseColor, previewBlockedColor, 0.65f);
            blocked.a = previewBlockedColor.a;
            return blocked;
        }

        private Color GetRangeRingColor(bool buildable)
        {
            if (_currentTower == null) return buildable ? previewValidColor : previewBlockedColor;

            Color baseColor = _currentTower.AttackColor;
            baseColor.a = previewValidColor.a * 0.85f;

            if (buildable) return baseColor;

            Color blocked = Color.Lerp(baseColor, previewBlockedColor, 0.65f);
            blocked.a = previewBlockedColor.a;
            return blocked;
        }

        private void UpdateRangeRingVisual(Vector3 center, float radius, Color color)
        {
            if (radius <= 0f)
            {
                if (_rangeRingRenderer != null)
                {
                    _rangeRingRenderer.gameObject.SetActive(false);
                }
                return;
            }

            EnsureRangeRingRenderer();
            if (_rangeRingRenderer == null) return;

            int points = Mathf.Max(8, rangeRingSegments);
            _rangeRingRenderer.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float t = (float)i / (points - 1);
                float angle = t * Mathf.PI * 2f;
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                _rangeRingRenderer.SetPosition(i, pos);
            }

            _rangeRingRenderer.startColor = color;
            _rangeRingRenderer.endColor = color;
            _rangeRingRenderer.gameObject.SetActive(true);
        }

        private void HandleCancelInput()
        {
            if (_currentTower == null) return;

            bool cancelKeyPressed = cancelPlacementKey != KeyCode.None && Input.GetKeyDown(cancelPlacementKey);
            bool rightClick = Input.GetMouseButtonDown(1);

            if (cancelKeyPressed || rightClick)
            {
                CancelPlacement();
            }
        }
    }
}
