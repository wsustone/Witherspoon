using UnityEngine;

namespace Witherspoon.Game.FX
{
    /// <summary>
    /// Handles cone-shaped attack visual effect for cone-based towers.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class ConeEffect : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Material _material;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
            }
        }

        public void SetDirection(Vector3 direction)
        {
            // Cylinder's default orientation is Y-up, so rotate 90 degrees to point forward
            transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
        }

        public void SetSize(float angleInDegrees, float range)
        {
            // Cylinder is 2 units tall by default, scale Y for length
            // Scale X and Z for width (cone spread)
            // Use minimum angle of 30 degrees for visibility
            float effectiveAngle = Mathf.Max(angleInDegrees, 30f);
            float radius = Mathf.Tan(effectiveAngle * Mathf.Deg2Rad * 0.5f) * range;
            transform.localScale = new Vector3(radius * 2f, range * 0.5f, radius * 2f);
        }

        public void SetColor(Color color)
        {
            if (_material != null)
            {
                color.a = 0.4f;
                _material.color = color;
            }
        }

        public void Show()
        {
            if (_renderer != null)
            {
                _renderer.enabled = true;
            }
        }

        public void Hide()
        {
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
        }
    }
}
