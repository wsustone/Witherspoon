using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Manages tower visual effects including placeholder meshes, beam/cone renderers, and materials.
    /// </summary>
    public class TowerVisuals : MonoBehaviour
    {
        [Header("Placeholder 3D Visuals")]
        [SerializeField] private bool usePlaceholderMesh = true;
        [SerializeField] private float placeholderRadius = 0.45f;
        [SerializeField] private float placeholderHeight = 1.6f;
        [SerializeField] private float placeholderGlowHeight = 0.35f;

        [Header("Effect Renderers")]
        [SerializeField] private LineRenderer beamRenderer;
        [SerializeField] private SpriteRenderer coneRenderer;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fxDuration = 0.3f;

        private TowerController _controller;

        public LineRenderer BeamRenderer => beamRenderer;
        public SpriteRenderer ConeRenderer => coneRenderer;
        public Transform FirePoint => firePoint;
        public float FxDuration => fxDuration;

        private void Awake()
        {
            _controller = GetComponent<TowerController>();
        }

        public void Initialize(TowerDefinition definition)
        {
            if (usePlaceholderMesh)
            {
                BuildPlaceholderMesh(definition);
            }

            EnsureBeamRendererDefaults(definition);
            EnsureConeRendererDefaults(definition);
        }

        public void RefreshForDefinition(TowerDefinition definition)
        {
            RefreshPlaceholderColors(definition);
            EnsureBeamRendererDefaults(definition);
            EnsureConeRendererDefaults(definition);
        }

        private void EnsureBeamRendererDefaults(TowerDefinition definition)
        {
            if (definition == null || definition.AttackMode != TowerDefinition.AttackStyle.Beam)
            {
                if (beamRenderer != null)
                {
                    beamRenderer.enabled = false;
                }
                return;
            }

            if (beamRenderer == null)
            {
                beamRenderer = GetComponent<LineRenderer>();
            }

            if (beamRenderer == null)
            {
                beamRenderer = gameObject.AddComponent<LineRenderer>();
            }

            beamRenderer.useWorldSpace = true;
            beamRenderer.loop = false;
            beamRenderer.widthMultiplier = 0.2f;
            beamRenderer.numCapVertices = 4;
            beamRenderer.numCornerVertices = 2;
            beamRenderer.alignment = LineAlignment.View;
            beamRenderer.textureMode = LineTextureMode.Stretch;
            if (beamRenderer.material == null || beamRenderer.material.shader == null)
            {
                beamRenderer.material = new Material(Shader.Find("Sprites/Default"));
            }
            beamRenderer.startColor = definition.AttackColor;
            beamRenderer.endColor = definition.AttackColor;
            beamRenderer.enabled = false;
        }

        private void EnsureConeRendererDefaults(TowerDefinition definition)
        {
            if (definition == null || definition.AttackMode != TowerDefinition.AttackStyle.Cone)
            {
                if (coneRenderer != null)
                {
                    coneRenderer.enabled = false;
                }
                return;
            }

            if (coneRenderer == null)
            {
                coneRenderer = GetComponent<SpriteRenderer>();
            }

            if (coneRenderer == null)
            {
                var coneObj = new GameObject("ConeRenderer");
                coneObj.transform.SetParent(transform, false);
                coneRenderer = coneObj.AddComponent<SpriteRenderer>();
                coneRenderer.sprite = Resources.Load<Sprite>("Sprites/cone") ?? Resources.Load<Sprite>("Sprites/Circle");
                coneRenderer.color = definition.AttackColor;
            }
            coneRenderer.enabled = false;
        }

        private void BuildPlaceholderMesh(TowerDefinition definition)
        {
            DisableSpriteRenderers();

            var existingMesh = GetComponentInChildren<MeshRenderer>();
            if (existingMesh != null) return;

            Color color = definition != null ? definition.HighlightColor : new Color(0.2f, 0.9f, 1f, 0.7f);
            Color glowColor = definition != null ? definition.AttackColor : new Color(1f, 0.85f, 0.35f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "TowerBody3D";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(placeholderRadius, placeholderHeight * 0.5f, placeholderRadius);
            body.transform.localPosition = new Vector3(0f, 0f, placeholderHeight * 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyMaterial(body, color);

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "TowerFocus3D";
            glow.transform.SetParent(transform, false);
            glow.transform.localScale = Vector3.one * (placeholderRadius * 0.9f);
            glow.transform.localPosition = new Vector3(0f, 0f, placeholderHeight + placeholderGlowHeight);
            ApplyMaterial(glow, glowColor);

            RemoveCollider(body);
            RemoveCollider(glow);
        }

        private void RefreshPlaceholderColors(TowerDefinition definition)
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers == null || renderers.Length == 0) return;
            Color color = definition != null ? definition.HighlightColor : new Color(0.2f, 0.9f, 1f, 0.7f);
            Color glowColor = definition != null ? definition.AttackColor : new Color(1f, 0.85f, 0.35f);

            foreach (var r in renderers)
            {
                if (r.gameObject.name.Contains("TowerBody3D"))
                {
                    if (r.material != null) r.material.color = color;
                }
                else if (r.gameObject.name.Contains("TowerFocus3D"))
                {
                    if (r.material != null) r.material.color = glowColor;
                }
            }
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
