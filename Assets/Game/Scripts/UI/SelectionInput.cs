using UnityEngine;
using UnityEngine.EventSystems;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Handles input detection and raycasting for tower/enemy selection.
    /// </summary>
    public class SelectionInput : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float boardPlaneZ = 0f;

        private void Reset()
        {
            worldCamera = Camera.main;
        }

        public void EnsureCamera()
        {
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }
        }

        public bool IsPointerOverUI(SelectionPanel panel)
        {
            bool overEventSystem = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool overPanel = panel != null && panel.IsPointerOverSelf();
            return overEventSystem || overPanel;
        }

        public bool TrySelectAtCursor(out TowerController tower, out EnemyAgent enemy)
        {
            tower = null;
            enemy = null;

            if (worldCamera == null) return false;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            
            if (TryRaycastTargets(ray, out tower, out enemy))
            {
                return tower != null || enemy != null;
            }

            Vector3? worldPoint = ProjectRayToBoard(ray);
            if (worldPoint.HasValue && TryFindNearest(worldPoint.Value, out tower, out enemy))
            {
                return tower != null || enemy != null;
            }

            return false;
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
            tower = null;
            enemy = null;

            float minTowerDist = float.MaxValue;
            float minEnemyDist = float.MaxValue;

            foreach (var t in TowerController.ActiveTowers)
            {
                if (t == null) continue;
                float dist = (t.transform.position - point).sqrMagnitude;
                if (dist < minTowerDist)
                {
                    minTowerDist = dist;
                    tower = t;
                }
            }

            foreach (var e in EnemyAgent.ActiveAgents)
            {
                if (e == null) continue;
                float dist = (e.transform.position - point).sqrMagnitude;
                if (dist < minEnemyDist)
                {
                    minEnemyDist = dist;
                    enemy = e;
                }
            }

            float maxDistanceSq = 0.75f * 0.75f;
            bool towerInRange = tower != null && minTowerDist <= maxDistanceSq;
            bool enemyInRange = enemy != null && minEnemyDist <= maxDistanceSq;

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
