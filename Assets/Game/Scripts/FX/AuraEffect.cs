using System.Collections;
using UnityEngine;

namespace Witherspoon.Game.FX
{
    /// <summary>
    /// Handles aura-based attack visual effect that pulses outward from towers.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class AuraEffect : MonoBehaviour
    {
        private MeshRenderer _renderer;
        private Material _material;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
                _renderer.enabled = false;
            }
        }

        public void Pulse(float range, Color color, float duration = 0.5f)
        {
            StartCoroutine(PulseCoroutine(range, color, duration));
        }

        private IEnumerator PulseCoroutine(float range, Color color, float duration)
        {
            if (_renderer == null || _material == null) yield break;

            color.a = 0.5f;
            _renderer.enabled = true;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float scale = Mathf.Lerp(0.5f, range * 2f, t);
                transform.localScale = Vector3.one * scale;

                Color c = color;
                c.a = Mathf.Lerp(0.5f, 0f, t);
                _material.color = c;

                yield return null;
            }

            _renderer.enabled = false;
        }
    }
}
