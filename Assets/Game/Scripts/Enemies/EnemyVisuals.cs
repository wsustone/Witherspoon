using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Handles enemy visual representation. Enemies must have 3D meshes in their prefabs.
    /// </summary>
    public class EnemyVisuals : MonoBehaviour
    {

        private LineRenderer _pathRenderer;
        private readonly List<Vector3> _pathPreviewPoints = new();
        private static bool _pathsVisible;

        private EnemyAgent _agent;
        private EnemyMovement _movement;

        public static bool PathsVisible => _pathsVisible;

        private void Awake()
        {
            _agent = GetComponent<EnemyAgent>();
            _movement = GetComponent<EnemyMovement>();
        }

        public void Initialize(EnemyDefinition definition)
        {
            RefreshPathRendererVisibility();
        }

        private void OnDisable()
        {
            if (_pathRenderer != null)
            {
                Destroy(_pathRenderer.gameObject);
                _pathRenderer = null;
            }
        }

        public void UpdatePathVisualization()
        {
            if (_pathsVisible)
            {
                UpdatePathRendererPositions();
            }
        }

        public void RefreshPathRendererVisibility()
        {
            if (!_pathsVisible)
            {
                if (_pathRenderer != null)
                {
                    _pathRenderer.gameObject.SetActive(false);
                }
                return;
            }

            UpdatePathRendererPositions();
        }

        private void EnsurePathRenderer()
        {
            if (_pathRenderer != null) return;

            var go = new GameObject("EnemyPathRenderer")
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            go.transform.SetParent(transform, worldPositionStays: false);
            _pathRenderer = go.AddComponent<LineRenderer>();
            _pathRenderer.useWorldSpace = true;
            _pathRenderer.loop = false;
            _pathRenderer.widthMultiplier = 0.05f;
            _pathRenderer.numCapVertices = 2;
            _pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _pathRenderer.sortingOrder = 900;
            _pathRenderer.gameObject.SetActive(false);
        }

        private void UpdatePathRendererPositions()
        {
            EnsurePathRenderer();
            if (_pathRenderer == null || _movement == null) return;

            _pathPreviewPoints.Clear();
            _pathPreviewPoints.Add(_movement.SpawnPosition);

            var path = _movement.Path;
            if (path.Count > 0)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    _pathPreviewPoints.Add(path[i]);
                }
            }

            _pathRenderer.positionCount = _pathPreviewPoints.Count;
            _pathRenderer.SetPositions(_pathPreviewPoints.ToArray());

            Color color = _agent?.Definition != null ? _agent.Definition.FactionColor : new Color(0.2f, 0.9f, 1f, 0.7f);
            color.a = 0.7f;
            _pathRenderer.startColor = color;
            _pathRenderer.endColor = color;
            _pathRenderer.gameObject.SetActive(true);
        }

        public static void SetPathVisualization(bool visible)
        {
            if (_pathsVisible == visible) return;
            _pathsVisible = visible;

            foreach (var agent in EnemyAgent.ActiveAgents)
            {
                var visuals = agent.GetComponent<EnemyVisuals>();
                visuals?.RefreshPathRendererVisibility();
            }
        }

    }
}
