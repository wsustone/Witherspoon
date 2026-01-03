using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Enemies;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Core tower controller: targeting, firing, and stat management.
    /// </summary>
    [RequireComponent(typeof(TowerHealth))]
    [RequireComponent(typeof(TowerVisuals))]
    [RequireComponent(typeof(TowerAudio))]
    [RequireComponent(typeof(TowerUpgrade))]
    [RequireComponent(typeof(TowerRepairAura))]
    public class TowerController : MonoBehaviour
    {
        [SerializeField] private TowerDefinition definition;

        private static readonly HashSet<TowerController> ActiveSet = new();
        public static IReadOnlyCollection<TowerController> ActiveTowers => ActiveSet;

        private float _fireCooldown;
        private int _kills;
        private float _currentRange;
        private float _currentFireRate;
        private float _currentDamage;
        private float _currentSlowPercent;
        private float _currentRepairPerSecond;
        private float _currentRepairGoldPerHP;
        private float _currentRepairPerAllyCap;

        private TowerHealth _health;
        private TowerVisuals _visuals;
        private TowerAudio _audio;
        private TowerUpgrade _upgrade;
        private TowerRepairAura _repairAura;

        private Coroutine _auraRoutine;
        private float _lastAuraPulseTime;

        public TowerDefinition Definition => definition;
        public int KillCount => _kills;
        public int UpgradeTier => _upgrade?.UpgradeTier ?? 0;
        public bool IsUpgrading => _upgrade?.IsUpgrading ?? false;
        public bool IsRepairing => _upgrade?.IsRepairing ?? false;
        public float UpgradeTimer => _upgrade?.UpgradeTimer ?? 0f;
        public float CurrentRange => _currentRange;
        public float CurrentFireRate => _currentFireRate;
        public float CurrentDamage => _currentDamage;
        public float CurrentSlowPercent => _currentSlowPercent;
        public float CurrentHealth => _health?.CurrentHealth ?? 0f;
        public float MaxHealth => _health?.MaxHealth ?? 0f;
        public TowerDefinition.TowerUpgradeTier NextTier => _upgrade?.NextTier;
        public bool HasRepairAura => _repairAura?.HasRepairAura ?? false;
        public float CurrentRepairPerSecond => _currentRepairPerSecond;
        public float CurrentRepairGoldPerHP => _currentRepairGoldPerHP;
        public float CurrentRepairPerAllyCap => _currentRepairPerAllyCap;

        private void Awake()
        {
            _health = GetComponent<TowerHealth>();
            _visuals = GetComponent<TowerVisuals>();
            _audio = GetComponent<TowerAudio>();
            _upgrade = GetComponent<TowerUpgrade>();
            _repairAura = GetComponent<TowerRepairAura>();
        }

        private void Start()
        {
            _visuals?.Initialize(definition);
            CacheBaseStats();
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

            _upgrade?.UpdateUpgradeProgress(Time.deltaTime);
            if (IsUpgrading) return;

            // Always tick repair aura regardless of firing cadence
            _repairAura?.UpdateRepairAura(Time.deltaTime);

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
            Vector3 origin = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
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
                        Vector3 spawnPos = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
                        if (target != null && definition.ProjectileSpawnOffset > 0f)
                        {
                            var toward = (target.transform.position - spawnPos).normalized;
                            spawnPos += toward * definition.ProjectileSpawnOffset;
                        }

                        var projectile = Instantiate(definition.ProjectilePrefab, spawnPos, Quaternion.identity);
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

        private System.Collections.IEnumerator FireBeamFx(EnemyAgent target)
        {
            var beamRenderer = _visuals.BeamRenderer;
            if (beamRenderer == null) yield break;

            beamRenderer.startColor = definition.AttackColor;
            beamRenderer.endColor = definition.AttackColor;
            beamRenderer.positionCount = 2;
            beamRenderer.SetPosition(0, _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position);
            beamRenderer.SetPosition(1, target != null ? target.transform.position : beamRenderer.GetPosition(0));
            beamRenderer.enabled = true;
            yield return new WaitForSeconds(_visuals.FxDuration);
            beamRenderer.enabled = false;
        }

        private System.Collections.IEnumerator FireConeFx(EnemyAgent target)
        {
            var coneRenderer = _visuals?.ConeRenderer;
            if (coneRenderer == null)
            {
                Debug.LogWarning($"[TowerController] {name}: ConeRenderer is null, cannot show cone effect");
                yield break;
            }
            if (target == null) yield break;

            coneRenderer.color = definition.AttackColor;
            Vector3 origin = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
            Vector3 dir = (target.transform.position - origin).normalized;
            coneRenderer.transform.position = origin;
            Quaternion facing = Quaternion.FromToRotation(Vector3.up, dir);
            coneRenderer.transform.rotation = facing * Quaternion.Euler(0f, 0f, definition.ConeRotationOffset);
            coneRenderer.transform.localScale = new Vector3(definition.ConeAngle, definition.Range, 1f);
            coneRenderer.enabled = true;
            yield return new WaitForSeconds(_visuals.FxDuration);
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
                if (_visuals.ConeRenderer != null)
                {
                    _visuals.ConeRenderer.enabled = false;
                }
            }

            _auraRoutine = null;
        }

        private void PulseAuraFx()
        {
            var coneRenderer = _visuals.ConeRenderer;
            if (coneRenderer == null) return;

            Vector3 origin = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
            coneRenderer.color = definition.AttackColor;
            coneRenderer.transform.position = origin;
            coneRenderer.transform.rotation = Quaternion.identity;
            coneRenderer.transform.localScale = Vector3.one * (definition.Range * 2f);
            coneRenderer.enabled = true;
        }

        private void ApplyAuraSlow()
        {
            Vector3 origin = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
            float radiusSq = definition.Range * definition.Range;
            foreach (var enemy in EnemyAgent.ActiveAgents.ToList())
            {
                if (enemy == null) continue;
                float distSq = (enemy.transform.position - origin).sqrMagnitude;
                if (distSq <= radiusSq)
                {
                    enemy.ApplySlow(_currentSlowPercent, definition.EffectDuration);
                }
            }
        }

        private void SpawnWallZone(EnemyAgent target)
        {
            if (definition.ProjectilePrefab == null) return;

            Vector3 origin = _visuals.FirePoint != null ? _visuals.FirePoint.position : transform.position;
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
                zone.Initialize(_currentSlowPercent, definition.EffectDuration);
            }
            else
            {
                var newZone = wall.AddComponent<SlowZone>();
                newZone.Initialize(_currentSlowPercent, definition.EffectDuration);
            }
        }

        internal void RegisterKill()
        {
            _kills++;
        }

        public void CacheBaseStats()
        {
            if (definition == null)
            {
                _currentRange = 3f;
                _currentFireRate = 1f;
                _currentDamage = 10f;
                _currentSlowPercent = 0.1f;
                _currentRepairPerSecond = 0f;
                _currentRepairGoldPerHP = 0f;
                _currentRepairPerAllyCap = 0f;
                return;
            }

            _currentRange = definition.Range;
            _currentFireRate = definition.FireRate;
            _currentDamage = definition.Damage;
            _currentSlowPercent = Mathf.Clamp01(definition.SlowPercent);
            _currentRepairPerSecond = Mathf.Max(0f, definition.RepairPerSecond);
            _currentRepairGoldPerHP = Mathf.Max(0f, definition.RepairGoldPerHP);
            _currentRepairPerAllyCap = Mathf.Max(0f, definition.RepairPerAllyCap);
        }

        public void RecalculateStats()
        {
            CacheBaseStats();
            if (definition?.UpgradeTiers == null) return;

            int tier = _upgrade?.UpgradeTier ?? 0;
            for (int i = 0; i < tier && i < definition.UpgradeTiers.Length; i++)
            {
                var upgradeTier = definition.UpgradeTiers[i];
                _currentRange *= upgradeTier.RangeMultiplier;
                _currentFireRate *= upgradeTier.FireRateMultiplier;
                _currentDamage *= upgradeTier.DamageMultiplier;
                _currentSlowPercent *= upgradeTier.SlowMultiplier;
                _currentSlowPercent = Mathf.Clamp01(_currentSlowPercent);
                _currentRepairPerSecond *= upgradeTier.RepairRateMultiplier;
                _currentRepairGoldPerHP *= upgradeTier.RepairCostMultiplier;
                _currentRepairPerAllyCap *= upgradeTier.RepairCapMultiplier;
                _currentRepairPerSecond = Mathf.Max(0f, _currentRepairPerSecond);
                _currentRepairGoldPerHP = Mathf.Max(0f, _currentRepairGoldPerHP);
                _currentRepairPerAllyCap = Mathf.Max(0f, _currentRepairPerAllyCap);
            }
        }

        public void TransformTo(TowerDefinition newDefinition)
        {
            if (newDefinition == null) return;
            definition = newDefinition;
            _upgrade?.ResetUpgrades();
            CacheBaseStats();
            _visuals?.RefreshForDefinition(definition);
        }

        private void OnDrawGizmosSelected()
        {
            if (_visuals?.ConeRenderer == null)
            {
                Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
                Vector3 origin = _visuals?.FirePoint != null ? _visuals.FirePoint.position : transform.position;
                Gizmos.DrawWireSphere(origin, _currentRange);
                return;
            }

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(_visuals.ConeRenderer.transform.position, _currentRange);
        }

        // Public API for upgrades
        public bool CanUpgrade() => _upgrade?.CanUpgrade() ?? false;
        public int GetUpgradeCost() => _upgrade?.GetUpgradeCost() ?? int.MaxValue;
        public float GetUpgradeDuration() => _upgrade?.GetUpgradeDuration() ?? 0f;
        public void BeginUpgrade() => _upgrade?.BeginUpgrade();
        public void BeginMorph(float seconds) => _upgrade?.BeginMorph(seconds);

        // Public API for repair
        public bool CanRepair() => _upgrade?.CanRepair() ?? false;
        public int GetRepairCost() => _upgrade?.GetRepairCost() ?? 0;
        public float GetRepairDuration() => _upgrade?.GetRepairDuration() ?? 0f;
        public void BeginRepair() => _upgrade?.BeginRepair();
    }
}
