using UnityEngine;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Simple ephemeral line effect linking healer to a repaired tower.
    /// Destroys itself after the specified lifetime.
    /// </summary>
    public class RepairLinkFx : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.2f;
        [SerializeField] private float lineWidth = 0.04f;
        [SerializeField] private Color color = new Color(0.3f, 0.9f, 0.6f, 0.9f);

        private LineRenderer _lr;
        private float _timer;

        public void Initialize(Vector3 from, Vector3 to, Color lineColor, float width, float seconds)
        {
            color = lineColor;
            lineWidth = width;
            lifetime = seconds;
            EnsureLR();
            if (_lr == null) return;
            _lr.startColor = color;
            _lr.endColor = color;
            _lr.widthMultiplier = lineWidth;
            _lr.positionCount = 2;
            _lr.SetPosition(0, new Vector3(from.x, from.y, 0f));
            _lr.SetPosition(1, new Vector3(to.x, to.y, 0f));
            _lr.enabled = true;
            _timer = lifetime;
        }

        private void EnsureLR()
        {
            if (_lr != null) return;
            _lr = gameObject.GetComponent<LineRenderer>();
            if (_lr == null)
            {
                _lr = gameObject.AddComponent<LineRenderer>();
                _lr.useWorldSpace = true;
                _lr.loop = false;
                _lr.numCapVertices = 2;
                _lr.material = new Material(Shader.Find("Sprites/Default"));
                _lr.sortingOrder = 1200;
            }
        }

        private void Update()
        {
            if (lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
