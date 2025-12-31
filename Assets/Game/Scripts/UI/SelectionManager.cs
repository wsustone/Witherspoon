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
                TryEnterFusionMode();
            }

            if (Input.GetMouseButtonDown(0))
            {
                bool pointerOverUI = (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    || (selectionPanel != null && selectionPanel.IsPointerOverSelf());
                if (pointerOverUI)
                {
                    return;
                }
                if (_isFusing)
                {
                    TryPickFusionTargetAtCursor();
                }
                else
                {
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
            if (_selectedTower == null) return;
            _isFusing = true;
            _fuseSource = _selectedTower;
            if (selectionPanel != null)
            {
                selectionPanel.ShowStatusMessage("Fusion: Click a partner tower to merge");
            }
        }

        private void ExitFusionMode()
        {
            _isFusing = false;
            _fuseSource = null;
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
                    bool merged = fusionService.TryMerge(_fuseSource, tower, economyManager);
                    if (merged)
                    {
                        _selectedTower = _fuseSource; // A persists through TransformTo
                        _selectedEnemy = null;
                        selectionPanel.ShowTower(_selectedTower);
                        ExitFusionMode();
                    }
                    else
                    {
                        if (selectionPanel != null)
                        {
                            selectionPanel.ShowStatusMessage("Fusion failed: requirements not met");
                        }
                        // remain in fusion mode
                    }
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
