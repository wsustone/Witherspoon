using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Manages tower visual effects. Towers must have 3D meshes in their prefabs.
    /// </summary>
    public class TowerVisuals : MonoBehaviour
    {
        [Header("Fire Point")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fxDuration = 0.3f;

        [Header("Effect Renderers (Auto-created if needed)")]
        [SerializeField] private LineRenderer beamRenderer;

        private TowerController _controller;

        public LineRenderer BeamRenderer => beamRenderer;
        public Transform FirePoint => firePoint;
        public float FxDuration => fxDuration;

        private void Awake()
        {
            _controller = GetComponent<TowerController>();
        }

        public void Initialize(TowerDefinition definition)
        {
            EnsureBeamRendererDefaults(definition);
        }

        public void RefreshForDefinition(TowerDefinition definition)
        {
            EnsureBeamRendererDefaults(definition);
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
            var material = beamRenderer.sharedMaterial;
            if (material == null || material.shader == null)
            {
                beamRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            }
            beamRenderer.startColor = definition.AttackColor;
            beamRenderer.endColor = definition.AttackColor;
            beamRenderer.enabled = false;
        }

    }
}
