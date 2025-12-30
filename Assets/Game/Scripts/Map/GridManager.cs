using System;
using System.Collections.Generic;
using UnityEngine;

namespace Witherspoon.Game.Map
{
    /// <summary>
    /// Handles tile coordinate transforms, walkable lanes, and tower placement checks.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private Vector2Int dimensions = new Vector2Int(12, 8);
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Transform gridOrigin;

        private readonly HashSet<Vector2Int> _blockedCells = new();
        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public event Action GridChanged;

        public Vector2Int Dimensions => dimensions;
        public float CellSize => cellSize;
        public Vector3 OriginPosition => gridOrigin != null ? gridOrigin.position : transform.position;

        public void Initialize()
        {
            _blockedCells.Clear();
            GridChanged?.Invoke();
        }

        public Vector3 GridToWorld(Vector2Int cell)
        {
            var offset = new Vector3((cell.x + 0.5f) * cellSize, (cell.y + 0.5f) * cellSize, 0f);
            return OriginPosition + offset;
        }

        public Vector2Int WorldToGrid(Vector3 position)
        {
            Vector3 local = (position - OriginPosition) / cellSize;
            int x = Mathf.FloorToInt(local.x);
            int y = Mathf.FloorToInt(local.y);
            return new Vector2Int(x, y);
        }

        public bool IsInsideGrid(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < dimensions.x && cell.y < dimensions.y;
        }

        public bool IsCellFree(Vector2Int cell)
        {
            return IsInsideGrid(cell) && !_blockedCells.Contains(cell);
        }

        public void SetBlocked(Vector2Int cell, bool blocked)
        {
            if (!IsInsideGrid(cell)) return;
            bool changed = false;
            if (blocked)
            {
                changed = _blockedCells.Add(cell);
            }
            else
            {
                changed = _blockedCells.Remove(cell);
            }

            if (changed)
            {
                GridChanged?.Invoke();
            }
        }

        public bool TryFindPath(Vector3 startWorld, Vector3 goalWorld, List<Vector3> pathBuffer)
        {
            if (pathBuffer == null) return false;
            pathBuffer.Clear();

            Vector2Int startCell = ClampToGrid(WorldToGrid(startWorld));
            Vector2Int goalCell = ClampToGrid(WorldToGrid(goalWorld));

            if (!IsWalkable(startCell, goalCell) || !IsWalkable(goalCell, goalCell))
            {
                return false;
            }

            if (TryFindPathCells(startCell, goalCell, out var cells))
            {
                // Skip the starting cell because the enemy is already there.
                for (int i = 1; i < cells.Count; i++)
                {
                    pathBuffer.Add(GridToWorld(cells[i]));
                }

                if (pathBuffer.Count == 0 || Vector3.Distance(pathBuffer[^1], goalWorld) > 0.01f)
                {
                    pathBuffer.Add(new Vector3(goalWorld.x, goalWorld.y, 0f));
                }
                return true;
            }

            return false;
        }

        private bool TryFindPathCells(Vector2Int start, Vector2Int goal, out List<Vector2Int> path)
        {
            var queue = new Queue<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var visited = new HashSet<Vector2Int>();

            queue.Enqueue(start);
            visited.Add(start);

            bool found = false;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == goal)
                {
                    found = true;
                    break;
                }

                foreach (var offset in NeighborOffsets)
                {
                    var neighbor = current + offset;
                    if (visited.Contains(neighbor)) continue;
                    if (!IsWalkable(neighbor, goal)) continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }

            if (!found)
            {
                path = null;
                return false;
            }

            path = new List<Vector2Int>();
            var node = goal;
            path.Add(node);
            while (node != start)
            {
                if (!cameFrom.TryGetValue(node, out node))
                {
                    path = null;
                    return false;
                }
                path.Add(node);
            }

            path.Reverse();
            return true;
        }

        private bool IsWalkable(Vector2Int cell, Vector2Int goalCell)
        {
            if (!IsInsideGrid(cell)) return false;
            if (cell == goalCell) return true;
            return !_blockedCells.Contains(cell);
        }

        private Vector2Int ClampToGrid(Vector2Int cell)
        {
            int x = Mathf.Clamp(cell.x, 0, dimensions.x - 1);
            int y = Mathf.Clamp(cell.y, 0, dimensions.y - 1);
            return new Vector2Int(x, y);
        }
    }
}
