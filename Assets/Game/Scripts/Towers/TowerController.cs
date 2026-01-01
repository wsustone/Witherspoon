using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Witherspoon.Game.Core;
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
        [SerializeField] private float fxDuration = 0.3f;
        [Header("Placeholder 3D Visuals")]
        [SerializeField] private bool usePlaceholderMesh = true;
        [SerializeField] private float placeholderRadius = 0.45f;
        [SerializeField] private float placeholderHeight = 1.6f;
        [SerializeField] private float placeholderGlowHeight = 0.35f;

        [Header("Health")]
        [SerializeField] private float maxHealth = 200f;
        [SerializeField] private float armor = 0f;

        [Header("Repair")]
        [SerializeField] private bool repairEnabled = true;
        [SerializeField] private float repairCostPerHP = 0.5f;
        [SerializeField] private float baseRepairTime = 1.5f;

        [Header("Upgrades")]
        [SerializeField] private AnimationCurve upgradeRangeSmoothing = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private EconomyManager economyManager;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip repairTickClip;
        [SerializeField] private AudioClip damageHitClip;
        [SerializeField] [Range(0f,1f)] private float repairTickVolume = 0.55f;
        [SerializeField] [Range(0f,1f)] private float damageHitVolume = 0.65f;
        [SerializeField] private float repairSfxInterval = 0.12f;
        [SerializeField] private float hitSfxInterval = 0.08f;

        private static readonly HashSet<TowerController> ActiveSet = new();
        public static IReadOnlyCollection<TowerController> ActiveTowers => ActiveSet;

        private float _fireCooldown;
        private int _kills;
        private int _upgradeTier;
        private float _upgradeTimer;
        private bool _isUpgrading;
        private bool _isMorphing;
        private bool _isRepairing;
        private float _currentRange;
        private float _currentFireRate;
        private float _currentDamage;
        private float _currentSlowPercent;
        private float _health;
        private float _currentRepairPerSecond;
        private float _currentRepairGoldPerHP;
        private float _currentRepairPerAllyCap;
        private float _repairGoldCarry;
        private float _lastRepairFxTime;
        private float _lastRepairSfxTime;
        private float _lastHitSfxTime;

        public TowerDefinition Definition => definition;
        public int KillCount => _kills;
        public int UpgradeTier => _upgradeTier;
        public bool IsUpgrading => _isUpgrading;
        public bool IsRepairing => _isRepairing;
        public float UpgradeTimer => _upgradeTimer;
        public float CurrentRange => _currentRange;
        public float CurrentFireRate => _currentFireRate;
        public float CurrentDamage => _currentDamage;
        public float CurrentSlowPercent => _currentSlowPercent;
        public float CurrentHealth => _health;
        public float MaxHealth => maxHealth;
        public TowerDefinition.TowerUpgradeTier NextTier => definition != null && definition.UpgradeTiers != null && _upgradeTier < definition.UpgradeTiers.Length ? definition.UpgradeTiers[_upgradeTier] : null;
        public bool HasRepairAura => definition != null && definition.RepairAuraEnabled && _currentRepairPerSecond > 0f;
        public float CurrentRepairPerSecond => _currentRepairPerSecond;
        public float CurrentRepairGoldPerHP => _currentRepairGoldPerHP;
        public float CurrentRepairPerAllyCap => _currentRepairPerAllyCap;

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
            if (_health <= 0f)
            {
                _health = maxHealth;
            }

            if (usePlaceholderMesh)
            {
                BuildPlaceholderMesh();
            }

            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }

            // Ensure a health bar exists
            if (GetComponent<TowerHealthBar>() == null)
            {
                gameObject.AddComponent<TowerHealthBar>();
            }

            // Ensure AudioSource exists
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound for readability
                audioSource.loop = false;
            }

            // Ensure beam renderer exists for beam-type towers
            if (definition != null && definition.AttackMode == TowerDefinition.AttackStyle.Beam && beamRenderer == null)
            {
                beamRenderer = GetComponent<LineRenderer>();
                if (beamRenderer == null)
                {
                    beamRenderer = gameObject.AddComponent<LineRenderer>();
                    beamRenderer.useWorldSpace = true;
                    beamRenderer.widthMultiplier = 0.1f;
                    beamRenderer.numCapVertices = 2;
                    beamRenderer.material = new Material(Shader.Find("Sprites/Default"));
                }
                beamRenderer.enabled = false;
            }

            // Ensure cone renderer exists for cone-type towers
            if (definition != null && definition.AttackMode == TowerDefinition.AttackStyle.Cone && coneRenderer == null)
            {
                coneRenderer = GetComponent<SpriteRenderer>();
                if (coneRenderer == null)
                {
                    var coneObj = new GameObject("ConeRenderer");
                    coneObj.transform.SetParent(transform, false);
                    coneRenderer = coneObj.AddComponent<SpriteRenderer>();
                    coneRenderer.sprite = Resources.Load<Sprite>("Sprites/cone") ?? Resources.Load<Sprite>("Sprites/Circle");
                    coneRenderer.color = definition.AttackColor;
                }
                coneRenderer.enabled = false;
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
                    else if (_isRepairing)
                    {
                        CompleteRepair();
                    }
                    else
                    {
                        CompleteUpgrade();
                    }
                }
                return;
            }

            // Always tick repair aura regardless of firing cadence
            RepairAuraUpdate(Time.deltaTime);

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
                    enemy.ApplySlow(_currentSlowPercent, definition.EffectDuration);
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
                Material material = null;
                
                Shader shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Diffuse");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                
                if (shader != null)
                {
                    material = new Material(shader);
                }
                else if (renderer.material != null)
                {
                    material = new Material(renderer.material);
                }
                
                if (material != null)
                {
                    material.color = color;
                    renderer.material = material;
                }
                
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

        public void ApplyDamageFromEnemy(float amount, EnemyAgent source)
        {
            float final = Mathf.Max(0f, amount - armor);
            _health -= final;
            if (final > 0.01f)
            {
                PlayHitSfx();
            }
            if (_health <= 0f)
            {
                HandleDestroyed();
            }
        }

        private void HandleDestroyed()
        {
            // Free the grid cell
            var grid = FindObjectOfType<Witherspoon.Game.Map.GridManager>();
            if (grid != null)
            {
                var cell = grid.WorldToGrid(transform.position);
                grid.SetBlocked(cell, false);
            }
            Destroy(gameObject);
        }

        public float ApplyRepair(float amount)
        {
            if (amount <= 0f) return 0f;
            float before = _health;
            _health = Mathf.Min(maxHealth, _health + amount);
            return _health - before;
        }

        private void RepairAuraUpdate(float dt)
        {
            if (!HasRepairAura || economyManager == null || dt <= 0f) return;

            float radius = _currentRange; // use tower range for aura radius
            float totalBudgetHP = _currentRepairPerSecond * dt;
            if (totalBudgetHP <= 0f) return;
            float perAllyCapHP = _currentRepairPerAllyCap * dt;

            // If repair has a gold cost, require at least 1 gold capacity (carry + wallet) before healing
            if (_currentRepairGoldPerHP > 0f)
            {
                float availableFloat = _repairGoldCarry + Mathf.Max(0, economyManager.CurrentGold);
                if (availableFloat < 1f) return;
            }

            // Build eligible list
            Vector3 origin = firePoint != null ? firePoint.position : transform.position;
            var eligible = new List<TowerController>();
            foreach (var t in ActiveTowers.ToList())
            {
                if (t == null) continue;
                if (!definition.RepairAffectsSelf && t == this) continue;
                if ((t.transform.position - origin).sqrMagnitude > radius * radius) continue;
                if (t.CurrentHealth >= t.MaxHealth - 0.01f) continue;
                eligible.Add(t);
            }
            if (eligible.Count == 0) return;

            // Sort by lowest health ratio first
            eligible = eligible.OrderBy(t => t.CurrentHealth / Mathf.Max(1f, t.MaxHealth)).ToList();

            const float minMissingHp = 0.5f; // avoid micro-heals on nearly full towers
            foreach (var t in eligible)
            {
                if (totalBudgetHP <= 0f) break;
                float missing = Mathf.Max(0f, t.MaxHealth - t.CurrentHealth);
                if (missing < minMissingHp) continue;
                float maxThis = perAllyCapHP > 0f ? Mathf.Min(perAllyCapHP, missing) : missing;
                float toHeal = Mathf.Min(maxThis, totalBudgetHP);
                if (_currentRepairGoldPerHP > 0f)
                {
                    // Bound by what we can afford (carry + current gold)
                    float availableFloat = _repairGoldCarry + Mathf.Max(0, economyManager.CurrentGold);
                    float affordableHp = availableFloat / _currentRepairGoldPerHP;
                    toHeal = Mathf.Min(toHeal, affordableHp);
                }
                if (toHeal <= 0f) continue;

                // Determine gold requirement with fractional carry
                if (_currentRepairGoldPerHP > 0f)
                {
                    float requiredGoldFloat = toHeal * _currentRepairGoldPerHP;
                    _repairGoldCarry += requiredGoldFloat;
                    int spendInt = Mathf.FloorToInt(_repairGoldCarry + 0.0001f);
                    if (spendInt > 0)
                    {
                        if (!economyManager.TrySpend(spendInt))
                        {
                            // Can't afford this chunk; rollback and stop repairing this tick
                            _repairGoldCarry -= requiredGoldFloat;
                            break;
                        }
                        _repairGoldCarry -= spendInt;
                    }
                    else
                    {
                        // No full gold to spend yet; don't heal on credit. Keep carry for future, but skip healing now.
                        _repairGoldCarry -= requiredGoldFloat; // maintain carry by not advancing this tick
                        break;
                    }
                }

                float healed = t.ApplyRepair(toHeal);
                totalBudgetHP -= healed;

                if (healed > 0.01f)
                {
                    SpawnRepairFx(origin, t.transform.position);
                    PlayRepairTickSfx();
                }
            }
        }

        private void SpawnRepairFx(Vector3 from, Vector3 to)
        {
            const float fxInterval = 0.08f;
            if (Time.time - _lastRepairFxTime < fxInterval) return;
            _lastRepairFxTime = Time.time;

            var go = new GameObject("RepairLinkFx");
            go.transform.position = from;
            var fx = go.AddComponent<RepairLinkFx>();
            Color c = definition != null ? definition.AttackColor : new Color(0.3f, 0.9f, 0.6f, 0.9f);
            fx.Initialize(from, to, c, 0.035f, 0.18f);
        }

        private void PlayRepairTickSfx()
        {
            if (repairTickClip == null || audioSource == null) return;
            if (Time.time - _lastRepairSfxTime < Mathf.Max(0.02f, repairSfxInterval)) return;
            _lastRepairSfxTime = Time.time;
            audioSource.PlayOneShot(repairTickClip, repairTickVolume);
        }

        private void PlayHitSfx()
        {
            if (damageHitClip == null || audioSource == null) return;
            if (Time.time - _lastHitSfxTime < Mathf.Max(0.02f, hitSfxInterval)) return;
            _lastHitSfxTime = Time.time;
            audioSource.PlayOneShot(damageHitClip, damageHitVolume);
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
                _currentSlowPercent *= tier.SlowMultiplier;
                _currentSlowPercent = Mathf.Clamp01(_currentSlowPercent);
                _currentRepairPerSecond *= tier.RepairRateMultiplier;
                _currentRepairGoldPerHP *= tier.RepairCostMultiplier;
                _currentRepairPerAllyCap *= tier.RepairCapMultiplier;
                _currentRepairPerSecond = Mathf.Max(0f, _currentRepairPerSecond);
                _currentRepairGoldPerHP = Mathf.Max(0f, _currentRepairGoldPerHP);
                _currentRepairPerAllyCap = Mathf.Max(0f, _currentRepairPerAllyCap);
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
            _isRepairing = false;
            CacheBaseStats();
            RefreshPlaceholderColors();
            
            // Ensure beam renderer exists for beam-type towers
            if (definition.AttackMode == TowerDefinition.AttackStyle.Beam)
            {
                if (beamRenderer == null)
                {
                    beamRenderer = GetComponent<LineRenderer>();
                }
                
                if (beamRenderer == null)
                {
                    beamRenderer = gameObject.AddComponent<LineRenderer>();
                    beamRenderer.useWorldSpace = true;
                    beamRenderer.widthMultiplier = 0.2f;
                    beamRenderer.numCapVertices = 4;
                    beamRenderer.material = new Material(Shader.Find("Sprites/Default"));
                }
                
                // Ensure the material is set up properly
                if (beamRenderer.material == null || beamRenderer.material.shader == null)
                {
                    beamRenderer.material = new Material(Shader.Find("Sprites/Default"));
                }
                
                beamRenderer.enabled = false;
            }

            // Ensure cone renderer exists for cone-type towers
            if (definition.AttackMode == TowerDefinition.AttackStyle.Cone && coneRenderer == null)
            {
                coneRenderer = GetComponent<SpriteRenderer>();
                if (coneRenderer == null)
                {
                    var coneObj = new GameObject("ConeRenderer");
                    coneObj.transform.SetParent(transform, false);
                    coneRenderer = coneObj.AddComponent<SpriteRenderer>();
                    coneRenderer.sprite = Resources.Load<Sprite>("Sprites/cone") ?? Resources.Load<Sprite>("Sprites/Circle");
                    coneRenderer.color = definition.AttackColor;
                }
                coneRenderer.enabled = false;
            }
        }

        public void BeginMorph(float seconds)
        {
            _isMorphing = true;
            _isUpgrading = true;
            _upgradeTimer = Mathf.Max(0f, seconds);
        }

        public bool CanRepair()
        {
            if (!repairEnabled) return false;
            if (_isUpgrading) return false;
            return _health < maxHealth - 0.01f;
        }

        public int GetRepairCost()
        {
            float missing = Mathf.Max(0f, maxHealth - _health);
            return Mathf.CeilToInt(missing * Mathf.Max(0f, repairCostPerHP));
        }

        public float GetRepairDuration()
        {
            return baseRepairTime;
        }

        public void BeginRepair()
        {
            if (!CanRepair()) return;
            _isRepairing = true;
            _isUpgrading = true;
            _upgradeTimer = GetRepairDuration();
        }

        private void CompleteRepair()
        {
            _isRepairing = false;
            _isUpgrading = false;
            _health = maxHealth;
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
