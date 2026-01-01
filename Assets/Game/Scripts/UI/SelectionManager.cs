using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Towers;
using Witherspoon.Game.Core;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Handles click-based selection of towers or enemies and feeds data to the SelectionPanel.
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private TowerPlacementController placementController;
        [SerializeField] private SelectionPanel selectionPanel;
        [Header("Fusion")]
        [SerializeField] private FusionService fusionService;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private KeyCode fusionHotkey = KeyCode.F;
        [SerializeField] private bool debugFusion = false;
        [SerializeField] private KeyCode clearSelectionKey = KeyCode.Escape;
        [SerializeField] private float boardPlaneZ = 0f;

        private TowerController _selectedTower;
        private EnemyAgent _selectedEnemy;
        private bool _isFusing;
        private TowerController _fuseSource;

        private void Reset()
        {
            worldCamera = Camera.main;
        }

        private void Update()
        {
            if (selectionPanel == null) return;

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (fusionService == null)
            {
                fusionService = FindObjectOfType<FusionService>();
            }
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }

            if (placementController != null && placementController.IsPlacing)
            {
                ClearSelection();
                return;
            }

            if (clearSelectionKey != KeyCode.None && Input.GetKeyDown(clearSelectionKey))
            {
                if (_isFusing)
                {
                    ExitFusionMode();
                }
                else
                {
                    ClearSelection();
                }
                return;
            }

            if (fusionHotkey != KeyCode.None && Input.GetKeyDown(fusionHotkey))
            {
                if (debugFusion) Debug.Log("[SelectionManager] Fusion hotkey pressed", this);
                TryEnterFusionMode();
            }

            // Repair hotkey intentionally disabled until Generals system is introduced

            if (Input.GetMouseButtonDown(0))
            {
                if (_isFusing)
                {
                    TryPickFusionTargetAtCursor();
                }
                else
                {
                    bool pointerOverUI = (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                        || (selectionPanel != null && selectionPanel.IsPointerOverSelf());
                    if (pointerOverUI)
                    {
                        return;
                    }
                    TrySelectAtCursor();
                }
            }
        }

        private void TrySelectAtCursor()
        {
            if (worldCamera == null) return;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (TryRaycastTargets(ray, out TowerController tower, out EnemyAgent enemy))
            {
                if (tower != null)
                {
                    SelectTower(tower);
                    return;
                }

                if (enemy != null)
                {
                    SelectEnemy(enemy);
                    return;
                }
            }

            Vector3? worldPoint = ProjectRayToBoard(ray);
            if (worldPoint.HasValue && TryFindNearest(worldPoint.Value, out tower, out enemy))
            {
                if (tower != null)
                {
                    SelectTower(tower);
                    return;
                }

                if (enemy != null)
                {
                    SelectEnemy(enemy);
                    return;
                }
            }

            ClearSelection();
        }

        private void SelectTower(TowerController tower)
        {
            _selectedTower = tower;
            _selectedEnemy = null;
            selectionPanel.ShowTower(tower);
        }

        private void SelectEnemy(EnemyAgent enemy)
        {
            _selectedEnemy = enemy;
            _selectedTower = null;
            selectionPanel.ShowEnemy(enemy);
        }

        private void ClearSelection()
        {
            if (_selectedTower == null && _selectedEnemy == null) return;

            _selectedTower = null;
            _selectedEnemy = null;
            selectionPanel.Hide();
        }

        private void TryEnterFusionMode()
        {
            if (_selectedTower == null)
            {
                if (selectionPanel != null)
                {
                    selectionPanel.ShowStatusMessage("Select a tower, then press F to start fusion");
                }
                if (debugFusion) Debug.Log("[SelectionManager] Fusion requested with no selected tower", this);
                return;
            }
            if (fusionService == null || fusionService.Recipes == null || fusionService.Recipes.Count == 0)
            {
                if (selectionPanel != null)
                {
                    selectionPanel.ShowStatusMessage("No fusion recipes configured");
                }
                if (debugFusion) Debug.Log("[SelectionManager] No fusion recipes configured on FusionService", this);
                return;
            }
            _isFusing = true;
            _fuseSource = _selectedTower;
            if (debugFusion) Debug.Log($"[SelectionManager] Entered fusion mode. Source={_fuseSource.name}", this);
            if (selectionPanel != null)
            {
                selectionPanel.ShowStatusMessage("Fusion: Click a partner tower to merge");
            }
        }

        private void ExitFusionMode()
        {
            _isFusing = false;
            _fuseSource = null;
            if (debugFusion) Debug.Log("[SelectionManager] Exited fusion mode", this);
            if (_selectedTower != null)
            {
                selectionPanel.ShowTower(_selectedTower);
            }
            else
            {
                selectionPanel.Hide();
            }
        }

        private void TryPickFusionTargetAtCursor()
        {
            if (worldCamera == null || _fuseSource == null || fusionService == null || economyManager == null)
            {
                ExitFusionMode();
                return;
            }

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (TryRaycastTargets(ray, out TowerController tower, out _))
            {
                if (tower != null && tower != _fuseSource)
                {
                    if (debugFusion) Debug.Log($"[SelectionManager] Fusion click on target={tower.name}", this);
                    // Preview recipe before attempting
                    var preview = fusionService.PreviewResult(_fuseSource.Definition, tower.Definition);
                    if (preview == null)
                    {
                        if (selectionPanel != null)
                        {
                            selectionPanel.ShowStatusMessage("No fusion recipe for this pair");
                        }
                        if (debugFusion) Debug.Log("[SelectionManager] No recipe for pair", this);
                        return; // remain in fusion mode
                    }
                    bool merged = fusionService.TryMerge(_fuseSource, tower, economyManager);
                    if (merged)
                    {
                        _selectedTower = _fuseSource; // A persists through TransformTo
                        _selectedEnemy = null;
                        selectionPanel.ShowTower(_selectedTower);
                        if (debugFusion) Debug.Log("[SelectionManager] Fusion succeeded", this);
                        ExitFusionMode();
                    }
                    else
                    {
                        if (selectionPanel != null)
                        {
                            selectionPanel.ShowStatusMessage("Fusion failed: requirements not met");
                        }
                        if (debugFusion) Debug.Log("[SelectionManager] Fusion failed: requirements not met", this);
                        // remain in fusion mode
                    }
                    return;
                }
                if (tower == _fuseSource)
                {
                    if (selectionPanel != null)
                    {
                        selectionPanel.ShowStatusMessage("Pick a different tower to fuse with");
                    }
                    if (debugFusion) Debug.Log("[SelectionManager] Clicked source tower again; waiting for different partner", this);
                    return;
                }
            }

            // Raycast didn't hit a tower: try nearest tower to cursor projection
            var worldPoint = ProjectRayToBoard(ray);
            if (worldPoint.HasValue && TryFindNearest(worldPoint.Value, out var nearTower, out _))
            {
                if (nearTower != null && nearTower != _fuseSource)
                {
                    if (debugFusion) Debug.Log($"[SelectionManager] Fusion nearest target={nearTower.name}", this);
                    var preview = fusionService.PreviewResult(_fuseSource.Definition, nearTower.Definition);
                    if (preview == null)
                    {
                        selectionPanel?.ShowStatusMessage("No fusion recipe for this pair");
                        if (debugFusion) Debug.Log("[SelectionManager] No recipe for nearest pair", this);
                        return;
                    }
                    bool merged = fusionService.TryMerge(_fuseSource, nearTower, economyManager);
                    if (merged)
                    {
                        _selectedTower = _fuseSource;
                        _selectedEnemy = null;
                        selectionPanel.ShowTower(_selectedTower);
                        if (debugFusion) Debug.Log("[SelectionManager] Fusion succeeded (nearest)", this);
                        ExitFusionMode();
                    }
                    else
                    {
                        selectionPanel?.ShowStatusMessage("Fusion failed: requirements not met");
                        if (debugFusion) Debug.Log("[SelectionManager] Fusion failed (nearest): requirements not met", this);
                    }
                    return;
                }
                if (nearTower == _fuseSource)
                {
                    selectionPanel?.ShowStatusMessage("Pick a different tower to fuse with");
                    if (debugFusion) Debug.Log("[SelectionManager] Nearest is source tower; waiting for different partner", this);
                    return;
                }
            }

            // If clicked empty space while fusing, do nothing (stay in fusion mode)
        }

        private bool TryRaycastTargets(Ray ray, out TowerController tower, out EnemyAgent enemy)
        {
            tower = null;
            enemy = null;

            if (Physics.Raycast(ray, out var hit3D, 500f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit3D.collider != null)
                {
                    tower = hit3D.collider.GetComponentInParent<TowerController>();
                    enemy = hit3D.collider.GetComponentInParent<EnemyAgent>();
                    if (tower != null || enemy != null)
                    {
                        return true;
                    }
                }
            }

            Vector3? point = ProjectRayToBoard(ray);
            if (point.HasValue)
            {
#if UNITY_2D
                var hits2D = Physics2D.OverlapPointAll(point.Value);
                foreach (var hit in hits2D)
                {
                    if (hit == null) continue;
                    if (tower == null && hit.TryGetComponent(out TowerController towerComp))
                    {
                        tower = towerComp;
                        return true;
                    }
                    if (enemy == null && hit.TryGetComponent(out EnemyAgent enemyComp))
                    {
                        enemy = enemyComp;
                        return true;
                    }
                }
#endif
            }

            return false;
        }

        private static bool TryFindNearest(Vector3 point, out TowerController tower, out EnemyAgent enemy)
        {
            tower = TowerController.ActiveTowers
                .OrderBy(t => (t.transform.position - point).sqrMagnitude)
                .FirstOrDefault();

            enemy = EnemyAgent.ActiveAgents
                .OrderBy(e => (e.transform.position - point).sqrMagnitude)
                .FirstOrDefault();

            float maxDistanceSq = 0.75f * 0.75f;
            bool towerInRange = tower != null && (tower.transform.position - point).sqrMagnitude <= maxDistanceSq;
            bool enemyInRange = enemy != null && (enemy.transform.position - point).sqrMagnitude <= maxDistanceSq;

            if (!towerInRange) tower = null;
            if (!enemyInRange) enemy = null;

            return tower != null || enemy != null;
        }

        private Vector3? ProjectRayToBoard(Ray ray)
        {
            Vector3 planePoint = new Vector3(0f, 0f, boardPlaneZ);
            Plane plane = new Plane(Vector3.forward, planePoint);
            if (plane.Raycast(ray, out float enter))
            {
                Vector3 hit = ray.GetPoint(enter);
                hit.z = boardPlaneZ;
                return hit;
            }
            return null;
        }
    }
}
