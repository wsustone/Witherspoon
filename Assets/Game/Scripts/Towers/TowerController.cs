using System.Linq;
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
        [SerializeField] private LineRenderer beamRenderer;
        [SerializeField] private SpriteRenderer coneRenderer;
        [SerializeField] private float fxDuration = 0.12f;

        private float _fireCooldown;

        private void Start()
        {
            if (beamRenderer != null)
            {
                beamRenderer.enabled = false;
            }
            if (coneRenderer != null)
            {
                coneRenderer.enabled = false;
            }
        }

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
            float rangeSq = definition.Range * definition.Range;
            EnemyAgent best = null;
            float bestDist = float.MaxValue;

            foreach (var enemy in EnemyAgent.ActiveAgents.ToList())
            {
                if (enemy == null) continue;
                float dist = (enemy.transform.position - origin).sqrMagnitude;
                if (dist <= rangeSq && dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }

            return best;
        }

        private void Fire(EnemyAgent target)
        {
            switch (definition.AttackMode)
            {
                case TowerDefinition.AttackStyle.Beam:
                    target.ApplyDamage(definition.Damage);
                    StartCoroutine(FireBeamFx(target));
                    break;
                case TowerDefinition.AttackStyle.Cone:
                    target.ApplyDamage(definition.Damage);
                    StartCoroutine(FireConeFx(target));
                    break;
                case TowerDefinition.AttackStyle.Aura:
                    if (_auraRoutine == null)
                    {
                        _auraRoutine = StartCoroutine(AuraPulseRoutine());
                    }
                    break;
                case TowerDefinition.AttackStyle.Wall:
                    SpawnWallZone(target);
                    break;
                default:
                    if (definition.ProjectilePrefab != null)
                    {
                        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
                        if (target != null && definition.ProjectileSpawnOffset > 0f)
                        {
                            var toward = (target.transform.position - spawnPos).normalized;
                            spawnPos += toward * definition.ProjectileSpawnOffset;
                        }

                        var projectile = Instantiate(definition.ProjectilePrefab, spawnPos, Quaternion.identity);
#if UNITY_EDITOR
                        Debug.Log($"[{definition.TowerName}] spawned projectile '{definition.ProjectilePrefab.name}' targeting {(target != null ? target.name : "null")}", this);
#endif
                        if (projectile.TryGetComponent(out ProjectileBehaviour behaviour))
                        {
                            behaviour.Initialize(target, definition.Damage, definition.ProjectileSpeed, definition.AttackColor);
                        }
                        else
                        {
                            var pb = projectile.AddComponent<ProjectileBehaviour>();
                            pb.Initialize(target, definition.Damage, definition.ProjectileSpeed, definition.AttackColor);
                        }
                    }
                    else
                    {
                        target.ApplyDamage(definition.Damage);
                    }
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (coneRenderer == null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
                Vector3 origin = firePoint != null ? firePoint.position : transform.position;
                Gizmos.DrawWireSphere(origin, definition.Range);
                return;
            }

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(coneRenderer.transform.position, definition.Range);
        }

        private Coroutine _auraRoutine;
        private float _lastAuraPulseTime;

        private System.Collections.IEnumerator FireBeamFx(EnemyAgent target)
        {
            if (beamRenderer == null) yield break;

            beamRenderer.startColor = definition.AttackColor;
            beamRenderer.endColor = definition.AttackColor;
            beamRenderer.positionCount = 2;
            beamRenderer.SetPosition(0, firePoint != null ? firePoint.position : transform.position);
            beamRenderer.SetPosition(1, target != null ? target.transform.position : beamRenderer.GetPosition(0));
            beamRenderer.enabled = true;
            yield return new WaitForSeconds(fxDuration);
            beamRenderer.enabled = false;
        }

        private System.Collections.IEnumerator FireConeFx(EnemyAgent target)
        {
            if (coneRenderer == null || target == null) yield break;

            coneRenderer.color = definition.AttackColor;
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 dir = (target.transform.position - origin).normalized;
            coneRenderer.transform.position = origin;
            Quaternion facing = Quaternion.FromToRotation(Vector3.up, dir);
            coneRenderer.transform.rotation = facing * Quaternion.Euler(0f, 0f, definition.ConeRotationOffset);
            coneRenderer.transform.localScale = new Vector3(definition.ConeAngle, definition.Range, 1f);
            coneRenderer.enabled = true;
            yield return new WaitForSeconds(fxDuration);
            coneRenderer.enabled = false;
        }

        private System.Collections.IEnumerator AuraPulseRoutine()
        {
            const float pulseInterval = 3f;
            const float pulseDuration = 0.5f;

            while (definition.AttackMode == TowerDefinition.AttackStyle.Aura)
            {
                float timeSinceLast = Time.time - _lastAuraPulseTime;
                if (timeSinceLast < pulseInterval)
                {
                    yield return new WaitForSeconds(pulseInterval - timeSinceLast);
                }

                _lastAuraPulseTime = Time.time;
                PulseAuraFx();
                ApplyAuraSlow();

                yield return new WaitForSeconds(pulseDuration);
                if (coneRenderer != null)
                {
                    coneRenderer.enabled = false;
                }
            }

            _auraRoutine = null;
        }

        private void PulseAuraFx()
        {
            if (coneRenderer == null)
            {
                return;
            }

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            coneRenderer.color = definition.AttackColor;
            coneRenderer.transform.position = origin;
            coneRenderer.transform.rotation = Quaternion.identity;
            coneRenderer.transform.localScale = Vector3.one * (definition.Range * 2f);
            coneRenderer.enabled = true;
        }

        private void ApplyAuraSlow()
        {
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            float radiusSq = definition.Range * definition.Range;
            foreach (var enemy in EnemyAgent.ActiveAgents.ToList())
            {
                if (enemy == null) continue;
                float distSq = (enemy.transform.position - origin).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    enemy.ApplySlow(definition.SlowPercent, definition.EffectDuration);
                }
            }
        }

        private void SpawnWallZone(EnemyAgent target)
        {
            if (definition.ProjectilePrefab == null) return;

            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            Vector3 spawnPos = origin;
            if (target != null)
            {
                Vector3 toTarget = target.transform.position - origin;
                float distance = toTarget.magnitude;
                Vector3 dir = distance > 0.001f ? toTarget / distance : Vector3.up;
                float clamped = Mathf.Min(distance, definition.Range);
                spawnPos = origin + dir * clamped;
            }
            spawnPos.z = 0f;

            var wall = Instantiate(definition.ProjectilePrefab, spawnPos, Quaternion.identity);
            foreach (var sprite in wall.GetComponentsInChildren<SpriteRenderer>())
            {
                sprite.color = definition.AttackColor;
            }
            if (wall.TryGetComponent(out SlowZone zone))
            {
                zone.Initialize(definition.SlowPercent, definition.EffectDuration);
            }
            else
            {
                var newZone = wall.AddComponent<SlowZone>();
                newZone.Initialize(definition.SlowPercent, definition.EffectDuration);
            }
        }
    }
}
