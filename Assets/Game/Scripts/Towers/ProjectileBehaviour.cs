using UnityEngine;
using Witherspoon.Game.Enemies;

namespace Witherspoon.Game.Towers
{
    public class ProjectileBehaviour : MonoBehaviour
    {
        private EnemyAgent _target;
        private float _damage;
        private float _speed;
        private Color _color = Color.white;

        public void Initialize(EnemyAgent target, float damage, float speed, Color color)
        {
            _target = target;
            _damage = damage;
            _speed = speed;
            _color = color;

            ApplyColor();
        }

        private void ApplyColor()
        {
            if (TryGetComponent(out SpriteRenderer sprite))
            {
                sprite.color = _color;
            }
            else if (TryGetComponent(out MeshRenderer mesh))
            {
                if (mesh.material != null)
                {
                    mesh.material.color = _color;
                }
            }
        }

        private void Update()
        {
            if (_target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 dir = _target.transform.position - transform.position;
            float distanceThisFrame = _speed * Time.deltaTime;

            if (dir.magnitude <= distanceThisFrame)
            {
                HitTarget();
                return;
            }

            transform.position += dir.normalized * distanceThisFrame;
            transform.rotation = Quaternion.LookRotation(Vector3.forward, dir);
        }

        private void HitTarget()
        {
            if (_target != null)
            {
                _target.ApplyDamage(_damage);
            }

            Destroy(gameObject);
        }
    }
}
