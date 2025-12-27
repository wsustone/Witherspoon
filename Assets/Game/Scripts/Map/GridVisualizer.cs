using System.Collections.Generic;
using UnityEngine;

namespace Witherspoon.Game.Map
{
    /// <summary>
    /// Renders simple grid lines using LineRenderers so players can see placement cells.
    /// </summary>
    [ExecuteAlways]
    public class GridVisualizer : MonoBehaviour
    {
        [SerializeField] private GridManager gridManager;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.2f);
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private float zOffset = -0.1f;

        private readonly List<LineRenderer> _lines = new();

        private void OnEnable()
        {
            BuildGrid();
        }

        private void OnDisable()
        {
            ClearLines();
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            BuildGrid();
        }

        private void BuildGrid()
        {
            ClearLines();

            if (gridManager == null)
            {
                gridManager = GetComponent<GridManager>();
            }

            if (gridManager == null) return;

            Vector2Int dims = gridManager.Dimensions;
            float cell = gridManager.CellSize;
            Vector3 origin = gridManager.OriginPosition;

            // Vertical lines
            for (int x = 0; x <= dims.x; x++)
            {
                Vector3 start = origin + new Vector3(x * cell, 0f, zOffset);
                Vector3 end = origin + new Vector3(x * cell, dims.y * cell, zOffset);
                CreateLine(start, end);
            }

            // Horizontal lines
            for (int y = 0; y <= dims.y; y++)
            {
                Vector3 start = origin + new Vector3(0f, y * cell, zOffset);
                Vector3 end = origin + new Vector3(dims.x * cell, y * cell, zOffset);
                CreateLine(start, end);
            }
        }

        private void CreateLine(Vector3 start, Vector3 end)
        {
            var lineObj = new GameObject("GridLine");
            lineObj.transform.SetParent(transform, false);
            var line = lineObj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPositions(new[] { start, end });
            line.widthMultiplier = lineWidth;
            line.material = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));
            line.startColor = lineColor;
            line.endColor = lineColor;
            _lines.Add(line);
        }

        private void ClearLines()
        {
            foreach (var line in _lines)
            {
                if (line != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(line.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(line.gameObject);
                    }
                }
            }
            _lines.Clear();
        }
    }
}
