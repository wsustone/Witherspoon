using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Handles enemy visual representation including placeholder meshes and path visualization.
    /// </summary>
    public class EnemyVisuals : MonoBehaviour
    {
        [Header("Placeholder 3D Visuals")]
        [SerializeField] private bool usePlaceholderMesh = true;
        [SerializeField] private float placeholderRadius = 0.35f;
        [SerializeField] private float placeholderHeight = 1.2f;
        [SerializeField] private float placeholderAccentHeight = 0.25f;

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
            if (usePlaceholderMesh)
            {
                BuildPlaceholderMesh(definition);
            }
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

        private void BuildPlaceholderMesh(EnemyDefinition definition)
        {
            DisableSpriteRenderers();

            if (GetComponentInChildren<MeshRenderer>() != null) return;

            Color bodyColor = definition != null ? definition.FactionColor : new Color(0.8f, 0.4f, 0.9f);
            Color accentColor = new Color(bodyColor.r * 0.9f, bodyColor.g * 0.9f, bodyColor.b * 1.2f, 1f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "EnemyBody3D";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(placeholderRadius, placeholderHeight * 0.5f, placeholderRadius);
            body.transform.localPosition = new Vector3(0f, 0f, placeholderHeight * 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyMaterial(body, bodyColor);

            var accent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            accent.name = "EnemyCore3D";
            accent.transform.SetParent(transform, false);
            accent.transform.localScale = Vector3.one * (placeholderRadius * 0.7f);
            accent.transform.localPosition = new Vector3(0f, 0f, placeholderHeight + placeholderAccentHeight);
            ApplyMaterial(accent, accentColor);

            RemoveCollider(body);
            RemoveCollider(accent);
        }

        private void DisableSpriteRenderers()
        {
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
            {
                sprite.enabled = false;
            }
        }

        private void ApplyMaterial(GameObject target, Color color)
        {
            if (target.TryGetComponent(out MeshRenderer renderer))
            {
                Material material = null;

                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Diffuse");
                if (shader == null) shader = Shader.Find("Unlit/Color");

                if (shader != null)
                {
                    material = new Material(shader);
                }
                else if (renderer.material != null)
                {
                    material = new Material(renderer.material);
                }

                if (material != null)
                {
                    material.color = color;
                    renderer.material = material;
                }

                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private void RemoveCollider(GameObject target)
        {
            if (target.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }
        }
    }
}
