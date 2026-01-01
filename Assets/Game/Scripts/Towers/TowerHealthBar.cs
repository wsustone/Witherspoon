using UnityEngine;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Minimal overhead health bar for towers using a LineRenderer.
    /// Auto-hides when at full health.
    /// </summary>
    [RequireComponent(typeof(TowerController))]
    public class TowerHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.9f, 0f);
        [SerializeField] private float width = 0.06f;
        [SerializeField] private float halfLength = 0.5f;
        [SerializeField] private bool showWhenFull = false;
        [SerializeField] private Color fullColor = new Color(0.25f, 0.95f, 0.55f, 0.95f);
        [SerializeField] private Color midColor = new Color(1f, 0.9f, 0.25f, 0.95f);
        [SerializeField] private Color lowColor = new Color(1f, 0.25f, 0.2f, 0.95f);

        private TowerController _tower;
        private LineRenderer _lr;

        private void Awake()
        {
            _tower = GetComponent<TowerController>();
            EnsureLR();
            UpdateVisual(force: true);
        }

        private void EnsureLR()
        {
            if (_lr != null) return;
            _lr = gameObject.GetComponentInChildren<LineRenderer>();
            if (_lr == null)
            {
                var go = new GameObject("HealthBarLR");
                go.transform.SetParent(transform, worldPositionStays: false);
                _lr = go.AddComponent<LineRenderer>();
            }

            _lr.useWorldSpace = true;
            _lr.loop = false;
            _lr.numCapVertices = 2;
            _lr.widthMultiplier = width;
            _lr.material = new Material(Shader.Find("Sprites/Default"));
            _lr.sortingOrder = 1100;
        }

        private void LateUpdate()
        {
            UpdateVisual();
        }

        private void UpdateVisual(bool force = false)
        {
            if (_tower == null || _lr == null) return;

            float max = Mathf.Max(1f, _tower.MaxHealth);
            float cur = Mathf.Clamp(_tower.CurrentHealth, 0f, max);
            float ratio = cur / max;

            bool visible = showWhenFull || ratio < 0.999f;
            if (!visible)
            {
                if (_lr.enabled) _lr.enabled = false;
                return;
            }

            _lr.enabled = true;
            _lr.widthMultiplier = width;

            Vector3 center = transform.position + worldOffset;
            float barHalf = Mathf.Max(0.05f, halfLength);
            // Background track (optional): we keep a single bar for simplicity
            Vector3 left = center + new Vector3(-barHalf, 0f, 0f);
            Vector3 right = center + new Vector3(-barHalf + (barHalf * 2f * ratio), 0f, 0f);
            _lr.positionCount = 2;
            _lr.SetPosition(0, left);
            _lr.SetPosition(1, right);

            Color c = Color.Lerp(lowColor, Color.Lerp(midColor, fullColor, Mathf.InverseLerp(0.5f, 1f, ratio)), ratio);
            _lr.startColor = c;
            _lr.endColor = c;
        }
    }
}
