using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Towers;

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
        [SerializeField] private KeyCode clearSelectionKey = KeyCode.Escape;

        private TowerController _selectedTower;
        private EnemyAgent _selectedEnemy;

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

            if (placementController != null && placementController.IsPlacing)
            {
                ClearSelection();
                return;
            }

            if (clearSelectionKey != KeyCode.None && Input.GetKeyDown(clearSelectionKey))
            {
                ClearSelection();
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TrySelectAtCursor();
            }
        }

        private void TrySelectAtCursor()
        {
            if (worldCamera == null) return;

            Vector3 worldPoint = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            worldPoint.z = 0f;

            if (TryHitWithPhysics(worldPoint, out TowerController tower, out EnemyAgent enemy))
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

            // Physics fallback: choose nearest logical entity.
            if (TryFindNearest(worldPoint, out tower, out enemy))
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

        private static bool TryHitWithPhysics(Vector3 point, out TowerController tower, out EnemyAgent enemy)
        {
            tower = null;
            enemy = null;

            bool found = false;

#if UNITY_2D
            var hits2D = Physics2D.OverlapPointAll(point);
            foreach (var hit in hits2D)
            {
                if (hit == null) continue;
                if (tower == null && hit.TryGetComponent(out TowerController towerComp))
                {
                    tower = towerComp;
                    found = true;
                }
                if (enemy == null && hit.TryGetComponent(out EnemyAgent enemyComp))
                {
                    enemy = enemyComp;
                    found = true;
                }
            }
#endif

            if (!found)
            {
                Ray ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : new Ray(point, Vector3.forward);
                if (Physics.Raycast(ray, out var hit3D, 100f))
                {
                    if (hit3D.collider != null)
                    {
                        tower = hit3D.collider.GetComponentInParent<TowerController>();
                        enemy = hit3D.collider.GetComponentInParent<EnemyAgent>();
                        if (tower != null || enemy != null)
                        {
                            found = true;
                        }
                    }
                }
            }

            return found;
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
    }
}
