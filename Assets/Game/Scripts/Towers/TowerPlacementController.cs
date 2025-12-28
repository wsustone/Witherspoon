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
        private TowerDefinition _currentTower;

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
            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceTowerAtCursor();
            }
        }

        private void TryPlaceTowerAtCursor()
        {
            if (gridManager == null || economyManager == null)
            {
                Debug.LogWarning("Placement failed: missing GridManager or EconomyManager.");
                return;
            }

            if (_currentTower == null || _currentTower.TowerPrefab == null)
            {
                Debug.LogWarning("Placement failed: no tower selected or prefab missing.");
                return;
            }

            var cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (cameraToUse == null) return;

            Vector3 worldPoint = cameraToUse.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0f;

            var cell = gridManager.WorldToGrid(worldPoint);
            if (!gridManager.IsCellFree(cell))
            {
                Debug.Log("Placement blocked: cell already occupied or out of bounds.");
                return;
            }

            if (!economyManager.TrySpend(_currentTower.BuildCost))
            {
                Debug.Log("Placement failed: not enough gold.");
                return;
            }

            Vector3 spawnPos = gridManager.GridToWorld(cell);
            var tower = Instantiate(_currentTower.TowerPrefab, spawnPos, Quaternion.identity);
            tower.name = $"{_currentTower.TowerName}_Tower";

            gridManager.SetBlocked(cell, true);
        }

        public void SelectTower(TowerDefinition definition)
        {
            if (definition == null) return;
            _currentTower = definition;
        }
    }
}
