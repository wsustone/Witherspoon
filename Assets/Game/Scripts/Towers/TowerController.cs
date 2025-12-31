using System.Collections.Generic;
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
        [Header("Placeholder 3D Visuals")]
        [SerializeField] private bool usePlaceholderMesh = true;
        [SerializeField] private float placeholderRadius = 0.45f;
        [SerializeField] private float placeholderHeight = 1.6f;
        [SerializeField] private float placeholderGlowHeight = 0.35f;

        [Header("Upgrades")]
        [SerializeField] private AnimationCurve upgradeRangeSmoothing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private static readonly HashSet<TowerController> ActiveSet = new();
        public static IReadOnlyCollection<TowerController> ActiveTowers => ActiveSet;

        private float _fireCooldown;
        private int _kills;
        private int _upgradeTier;
        private float _upgradeTimer;
        private bool _isUpgrading;
        private bool _isMorphing;
        private float _currentRange;
        private float _currentFireRate;
        private float _currentDamage;

        public TowerDefinition Definition => definition;
        public int KillCount => _kills;
        public int UpgradeTier => _upgradeTier;
        public bool IsUpgrading => _isUpgrading;
        public float UpgradeTimer => _upgradeTimer;
        public float CurrentRange => _currentRange;
        public float CurrentFireRate => _currentFireRate;
        public float CurrentDamage => _currentDamage;
        public TowerDefinition.TowerUpgradeTier NextTier => definition != null && definition.UpgradeTiers != null && _upgradeTier < definition.UpgradeTiers.Length ? definition.UpgradeTiers[_upgradeTier] : null;

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

            CacheBaseStats();

            if (usePlaceholderMesh)
            {
                BuildPlaceholderMesh();
            }
        }

        private void OnEnable()
        {
            ActiveSet.Add(this);
        }

        private void OnDisable()
        {
            ActiveSet.Remove(this);
        }

        private void Update()
        {
            if (definition == null) return;
            if (_isUpgrading)
            {
                _upgradeTimer -= Time.deltaTime;
                if (_upgradeTimer <= 0f)
                {
                    if (_isMorphing)
                    {
                        _isMorphing = false;
                        _isUpgrading = false;
                    }
                    else
                    {
                        CompleteUpgrade();
                    }
                }
                return;
            }

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
            float rangeSq = _currentRange * _currentRange;
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
                    target.ApplyDamage(_currentDamage, this);
                    StartCoroutine(FireBeamFx(target));
                    break;
                case TowerDefinition.AttackStyle.Cone:
                    target.ApplyDamage(_currentDamage, this);
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
                            behaviour.Initialize(target, _currentDamage, definition.ProjectileSpeed, definition.AttackColor, this);
                        }
                        else
                        {
                            var pb = projectile.AddComponent<ProjectileBehaviour>();
                            pb.Initialize(target, _currentDamage, definition.ProjectileSpeed, definition.AttackColor, this);
                        }
                    }
                    else
                    {
                        target.ApplyDamage(_currentDamage, this);
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
                Gizmos.DrawWireSphere(origin, _currentRange);
                return;
            }

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(coneRenderer.transform.position, _currentRange);
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

        internal void RegisterKill()
        {
            _kills++;
        }

        private void BuildPlaceholderMesh()
        {
            DisableSpriteRenderers();

            var existingMesh = GetComponentInChildren<MeshRenderer>();
            if (existingMesh != null) return;

            Color color = definition != null ? definition.HighlightColor : new Color(0.2f, 0.9f, 1f, 0.7f);
            Color glowColor = definition != null ? definition.AttackColor : new Color(1f, 0.85f, 0.35f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "TowerBody3D";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(placeholderRadius, placeholderHeight * 0.5f, placeholderRadius);
            body.transform.localPosition = new Vector3(0f, 0f, placeholderHeight * 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyMaterial(body, color);

            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.name = "TowerFocus3D";
            glow.transform.SetParent(transform, false);
            glow.transform.localScale = Vector3.one * (placeholderRadius * 0.9f);
            glow.transform.localPosition = new Vector3(0f, 0f, placeholderHeight + placeholderGlowHeight);
            ApplyMaterial(glow, glowColor);

            RemoveCollider(body);
            RemoveCollider(glow);
        }

        private void DisableSpriteRenderers()
        {
            foreach (var sprite in GetComponentsInChildren<SpriteRenderer>())
            {
                sprite.enabled = false;
            }
        }

        private void ApplyMaterial(GameObject target, Color color)
        {
            if (target.TryGetComponent(out MeshRenderer renderer))
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                material.color = color;
                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private void RemoveCollider(GameObject target)
        {
            if (target.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }
        }

        private void CacheBaseStats()
        {
            if (definition == null)
            {
                _currentRange = 3f;
                _currentFireRate = 1f;
                _currentDamage = 10f;
                return;
            }

            _currentRange = definition.Range;
            _currentFireRate = definition.FireRate;
            _currentDamage = definition.Damage;
        }

        public bool CanUpgrade()
        {
            if (definition == null || definition.UpgradeTiers == null) return false;
            if (_upgradeTier >= definition.UpgradeTiers.Length) return false;
            return !_isUpgrading;
        }

        public int GetUpgradeCost()
        {
            var tier = NextTier;
            return tier != null ? tier.Cost : int.MaxValue;
        }

        public float GetUpgradeDuration()
        {
            var tier = NextTier;
            return tier != null ? tier.UpgradeTime : 0f;
        }

        public void BeginUpgrade()
        {
            if (!CanUpgrade()) return;
            var tier = NextTier;
            if (tier == null) return;

            _isUpgrading = true;
            _upgradeTimer = tier.UpgradeTime;
        }

        private void CompleteUpgrade()
        {
            _isUpgrading = false;
            _upgradeTier++;
            ApplyUpgradeStats();
        }

        private void ApplyUpgradeStats()
        {
            CacheBaseStats();
            if (definition?.UpgradeTiers == null) return;

            for (int i = 0; i < _upgradeTier && i < definition.UpgradeTiers.Length; i++)
            {
                var tier = definition.UpgradeTiers[i];
                _currentRange *= tier.RangeMultiplier;
                _currentFireRate *= tier.FireRateMultiplier;
                _currentDamage *= tier.DamageMultiplier;
            }
        }

        public void TransformTo(TowerDefinition newDefinition)
        {
            if (newDefinition == null) return;
            definition = newDefinition;
            _upgradeTier = 0;
            _upgradeTimer = 0f;
            _isUpgrading = false;
            _isMorphing = false;
            CacheBaseStats();
            RefreshPlaceholderColors();
        }

        public void BeginMorph(float seconds)
        {
            _isMorphing = true;
            _isUpgrading = true;
            _upgradeTimer = Mathf.Max(0f, seconds);
        }

        private void RefreshPlaceholderColors()
        {
            var renderers = GetComponentsInChildren<MeshRenderer>();
            if (renderers == null || renderers.Length == 0) return;
            Color color = definition != null ? definition.HighlightColor : new Color(0.2f, 0.9f, 1f, 0.7f);
            Color glowColor = definition != null ? definition.AttackColor : new Color(1f, 0.85f, 0.35f);

            foreach (var r in renderers)
            {
                if (r.gameObject.name.Contains("TowerBody3D"))
                {
                    if (r.material != null) r.material.color = color;
                }
                else if (r.gameObject.name.Contains("TowerFocus3D"))
                {
                    if (r.material != null) r.material.color = glowColor;
                }
            }
        }
    }
}
