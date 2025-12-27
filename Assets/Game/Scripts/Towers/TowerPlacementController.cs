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

        private void Reset()
        {
            worldCamera = Camera.main;
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
            if (gridManager == null || economyManager == null || defaultTower == null || defaultTower.TowerPrefab == null)
            {
                return;
            }

            var cameraToUse = worldCamera != null ? worldCamera : Camera.main;
            if (cameraToUse == null) return;

            Vector3 worldPoint = cameraToUse.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0f;

            var cell = gridManager.WorldToGrid(worldPoint);
            if (!gridManager.IsCellFree(cell))
            {
                return;
            }

            if (!economyManager.TrySpend(defaultTower.BuildCost))
            {
                return;
            }

            Vector3 spawnPos = gridManager.GridToWorld(cell);
            var tower = Instantiate(defaultTower.TowerPrefab, spawnPos, Quaternion.identity);
            tower.name = $"{defaultTower.TowerName}_Tower";

            gridManager.SetBlocked(cell, true);
        }
    }
}
