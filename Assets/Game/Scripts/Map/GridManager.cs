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

        public Vector2Int Dimensions => dimensions;
        public float CellSize => cellSize;
        public Vector3 OriginPosition => gridOrigin != null ? gridOrigin.position : transform.position;

        public void Initialize()
        {
            _blockedCells.Clear();
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
            if (blocked) _blockedCells.Add(cell);
            else _blockedCells.Remove(cell);
        }
    }
}
