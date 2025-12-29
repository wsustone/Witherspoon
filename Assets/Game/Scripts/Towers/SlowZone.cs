using UnityEngine;
using Witherspoon.Game.Enemies;

namespace Witherspoon.Game.Towers
{
    [RequireComponent(typeof(Collider2D))]
    public class SlowZone : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 1f;
        [SerializeField] private bool destroyOnExpire = true;

        private float _slowPercent = 0.1f;
        private float _slowDuration = 0.5f;
        private float _lifeTimer;
        private Collider2D _collider;
        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody == null)
            {
                _rigidbody = gameObject.AddComponent<Rigidbody2D>();
            }

            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
            _rigidbody.useFullKinematicContacts = false;
            _rigidbody.gravityScale = 0f;
            _rigidbody.constraints = RigidbodyConstraints2D.FreezeAll;
            _rigidbody.simulated = true;
        }

        private void OnEnable()
        {
            _lifeTimer = lifeTime;
        }

        public void Initialize(float slowPercent, float slowDuration, float lifetimeOverride = -1f)
        {
            _slowPercent = Mathf.Clamp01(slowPercent);
            _slowDuration = Mathf.Max(0f, slowDuration);
            if (lifetimeOverride > 0f)
            {
                lifeTime = lifetimeOverride;
            }
            _lifeTimer = lifeTime;
        }

        private void Update()
        {
            if (lifeTime <= 0f) return;
            _lifeTimer -= Time.deltaTime;
            if (_lifeTimer <= 0f && destroyOnExpire)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (!other.TryGetComponent(out EnemyAgent enemy)) return;
            enemy.ApplySlow(_slowPercent, _slowDuration);
        }
    }
}
