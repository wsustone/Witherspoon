using UnityEngine;

namespace Witherspoon.Game.FX
{
    /// <summary>
    /// Handles wall-based attack visual effect that creates a barrier.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class WallEffect : MonoBehaviour
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

        public void Initialize(float width, Color color)
        {
            if (_material != null)
            {
                color.a = 0.6f;
                _material.color = color;
            }

            transform.localScale = new Vector3(width, 2f, 0.2f);

            if (_renderer != null)
            {
                _renderer.enabled = true;
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
