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
            beamRenderer.startWidth = 1.0f;  // Much wider for visibility
            beamRenderer.endWidth = 1.0f;
            beamRenderer.numCapVertices = 5;
            beamRenderer.numCornerVertices = 5;
            beamRenderer.alignment = LineAlignment.TransformZ;  // Try different alignment
            beamRenderer.textureMode = LineTextureMode.Stretch;
            beamRenderer.sortingLayerName = "Default";
            beamRenderer.sortingOrder = 1000; // Very high sorting order
            
            // Try Sprites/Default shader which works for other 2D elements
            Material beamMaterial = new Material(Shader.Find("Sprites/Default"));
            beamMaterial.color = definition.AttackColor;
            beamRenderer.sharedMaterial = beamMaterial;
            
            // Set very bright color
            Color brightColor = definition.AttackColor;
            brightColor.a = 1f;
            beamRenderer.startColor = brightColor;
            beamRenderer.endColor = brightColor;
            beamRenderer.enabled = false;
            
        }

    }
}
