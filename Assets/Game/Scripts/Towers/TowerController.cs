using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Enemies;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Basic targeting + firing loop for towers.
    /// </summary>
    public class TowerController : MonoBehaviour
    {
        [SerializeField] private TowerDefinition definition;
        [SerializeField] private Transform firePoint;
        [SerializeField] private LayerMask enemyLayer;

        private float _fireCooldown;
        private readonly Collider2D[] _results = new Collider2D[16];

        private void Update()
        {
            if (definition == null) return;

            _fireCooldown -= Time.deltaTime;
            if (_fireCooldown > 0f) return;

            EnemyAgent target = AcquireTarget();
            if (target != null)
            {
                Fire(target);
                _fireCooldown = 1f / Mathf.Max(0.01f, definition.FireRate);
            }
        }

        private EnemyAgent AcquireTarget()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            int hits = Physics2D.OverlapCircleNonAlloc(origin, definition.Range, _results, enemyLayer);
            float closestDist = float.MaxValue;
            EnemyAgent best = null;
            for (int i = 0; i < hits; i++)
            {
                var col = _results[i];
                if (col != null && col.TryGetComponent(out EnemyAgent agent))
                {
                    float dist = Vector3.SqrMagnitude(agent.transform.position - origin);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        best = agent;
                    }
                }
            }

            return best;
        }

        private void Fire(EnemyAgent target)
        {
            target.ApplyDamage(definition.Damage);
            // TODO: spawn projectile or VFX hook
        }
    }
}
