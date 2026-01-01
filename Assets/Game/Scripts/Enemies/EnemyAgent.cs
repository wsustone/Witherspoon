using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Map;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Minimal enemy behaviour: moves toward a goal point and reports death.
    /// </summary>
    public class EnemyAgent : MonoBehaviour
    {
        private static readonly HashSet<EnemyAgent> ActiveSet = new();
        public static IReadOnlyCollection<EnemyAgent> ActiveAgents => ActiveSet;
        public static System.Action<EnemyAgent> OnAnyKilled;
        public static System.Action<EnemyAgent> OnAnyReachedGoal;

        [SerializeField] private EnemyDefinition definition;
        [Header("Placeholder 3D Visuals")]
        [SerializeField] private bool usePlaceholderMesh = true;
        [SerializeField] private float placeholderRadius = 0.35f;
        [SerializeField] private float placeholderHeight = 1.2f;
        [SerializeField] private float placeholderAccentHeight = 0.25f;

        private float _health;
        private Vector3 _goal;
        private Vector3 _spawnPosition;
        private float _spawnDelay;
        private bool _active;
        private float _slowMultiplier = 1f;
        private float _slowTimer;
        private GridManager _grid;
        private readonly List<Vector3> _path = new();
        private readonly List<Vector3> _pathPreviewPoints = new();
        private LineRenderer _pathRenderer;
        private int _pathIndex;
        private bool _pendingPathRefresh;
        private static bool _pathsVisible;
        private float _towerAttackCooldown;
        private TowerController _currentTowerTarget;

        public System.Action<EnemyAgent> OnReachedGoal;
        public System.Action<EnemyAgent> OnKilled;

        public EnemyDefinition Definition => definition;
        public float CurrentHealth => _health;
        public float MaxHealth => definition != null ? definition.BaseHealth : 100f;
        public static bool PathsVisible => _pathsVisible;

        private void OnEnable()
        {
            ActiveSet.Add(this);
        }

        private void OnDisable()
        {
            ActiveSet.Remove(this);
            if (_grid != null)
            {
                _grid.GridChanged -= HandleGridChanged;
                _grid = null;
            }
            if (_pathRenderer != null)
            {
                Destroy(_pathRenderer.gameObject);
                _pathRenderer = null;
            }
        }

        public void Initialize(EnemyDefinition def, Vector3 goal, float spawnDelay)
        {
            Initialize(def, goal, spawnDelay, null);
        }

        public void Initialize(EnemyDefinition def, Vector3 goal, float spawnDelay, GridManager grid)
        {
            definition = def;
            _goal = new Vector3(goal.x, goal.y, 0f);
            _spawnDelay = spawnDelay;
            _health = definition != null ? definition.BaseHealth : 100f;
            _active = false;
            _grid = grid;
            _path.Clear();
            _pathIndex = 0;
            _pendingPathRefresh = false;
            // Ensure enemies start on the 2D plane (z = 0)
            var pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _spawnPosition = transform.position;

            if (_grid != null)
            {
                _grid.GridChanged -= HandleGridChanged;
                _grid.GridChanged += HandleGridChanged;
                RecalculatePath();
            }

            RefreshPathRendererVisibility();
            if (usePlaceholderMesh)
            {
                BuildPlaceholderMesh();
            }
        }

        private void Update()
        {
            if (definition == null) return;
            if (!_active)
            {
                _spawnDelay -= Time.deltaTime;
                if (_spawnDelay <= 0f) _active = true;
                else return;
            }

            TickSlow();
            if (_pendingPathRefresh)
            {
                RecalculatePath();
            }
            // Attack towers if this creep can and one is in range; pause movement while attacking
            bool attackingTower = TowerAttackUpdate();
            if (!attackingTower)
            {
                MoveTowardsGoal();
            }
        }

        private void LateUpdate()
        {
            if (_pathsVisible)
            {
                UpdatePathRendererPositions();
            }
        }

        private void MoveTowardsGoal()
        {
            AdvancePathIfNeeded();

            Vector3 target = GetCurrentTarget();
            Vector3 direction = target - transform.position;
            direction.z = 0f;
            float distance = direction.magnitude;
            if (distance < 0.05f)
            {
                if (IsAtGoalTarget())
                {
                    OnReachedGoal?.Invoke(this);
                    OnAnyReachedGoal?.Invoke(this);
                    Destroy(gameObject);
                }
                else
                {
                    AdvancePathIfNeeded(forceAdvance: true);
                }
                return;
            }

            direction.Normalize();
            float effectiveSpeed = definition.MoveSpeed * _slowMultiplier;
            float step = effectiveSpeed * Time.deltaTime;
            transform.position += direction * Mathf.Min(step, distance);
            var pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, 0f);
        }

        public void ApplyDamage(float amount, TowerController source = null)
        {
            float finalDamage = amount;
            if (definition != null)
            {
                // Apply armor as flat reduction
                finalDamage = Mathf.Max(0f, finalDamage - definition.Armor);

                // Apply damage-type multiplier based on the source tower's attack style
                var style = source != null && source.Definition != null
                    ? source.Definition.AttackMode
                    : Witherspoon.Game.Data.TowerDefinition.AttackStyle.Projectile;
                switch (style)
                {
                    case Witherspoon.Game.Data.TowerDefinition.AttackStyle.Projectile:
                        finalDamage *= definition.DmgTakenMulProjectile;
                        break;
                    case Witherspoon.Game.Data.TowerDefinition.AttackStyle.Beam:
                        finalDamage *= definition.DmgTakenMulBeam;
                        break;
                    case Witherspoon.Game.Data.TowerDefinition.AttackStyle.Cone:
                        finalDamage *= definition.DmgTakenMulCone;
                        break;
                    case Witherspoon.Game.Data.TowerDefinition.AttackStyle.Aura:
                        finalDamage *= definition.DmgTakenMulAura;
                        break;
                    case Witherspoon.Game.Data.TowerDefinition.AttackStyle.Wall:
                        finalDamage *= definition.DmgTakenMulWall;
                        break;
                }
            }

            _health -= finalDamage;
            if (_health <= 0f)
            {
                OnKilled?.Invoke(this);
                OnAnyKilled?.Invoke(this);
                source?.RegisterKill();
                Destroy(gameObject);
            }
        }

        private void TickSlow()
        {
            if (_slowTimer <= 0f)
            {
                _slowMultiplier = 1f;
                return;
            }

            _slowTimer -= Time.deltaTime;
            if (_slowTimer <= 0f)
            {
                _slowMultiplier = 1f;
                _slowTimer = 0f;
            }
        }

        public void ApplySlow(float slowPercent, float duration)
        {
            float clampedPercent = Mathf.Clamp01(slowPercent);
            float effectiveness = definition != null ? definition.SlowEffectiveness : 1f;
            float effectivePercent = Mathf.Clamp01(clampedPercent * effectiveness);
            float candidateMultiplier = 1f - effectivePercent;

            if (candidateMultiplier < _slowMultiplier)
            {
                _slowMultiplier = candidateMultiplier;
            }

            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        private void HandleGridChanged()
        {
            _pendingPathRefresh = true;
        }

        private void RecalculatePath()
        {
            _pendingPathRefresh = false;
            if (_grid == null) return;

            if (_grid.TryFindPath(transform.position, _goal, _path))
            {
                _pathIndex = 0;
            }
            else
            {
                _path.Clear();
                _pathIndex = 0;
            }

            RefreshPathRendererVisibility();
        }

        private bool TowerAttackUpdate()
        {
            if (!definition.CanAttackTowers)
            {
                _currentTowerTarget = null;
                return false;
            }

            // Acquire nearest tower within attack range
            float range = Mathf.Max(0f, definition.AttackRange);
            float rangeSq = range * range;
            TowerController best = null;
            float bestDist = float.MaxValue;
            foreach (var t in TowerController.ActiveTowers)
            {
                if (t == null) continue;
                float d = (t.transform.position - transform.position).sqrMagnitude;
                if (d <= rangeSq && d < bestDist)
                {
                    bestDist = d;
                    best = t;
                }
            }

            _currentTowerTarget = best;
            if (_currentTowerTarget == null) return false;

            _towerAttackCooldown -= Time.deltaTime;
            if (_towerAttackCooldown <= 0f)
            {
                _currentTowerTarget.ApplyDamageFromEnemy(definition.AttackDamage, this);
                _towerAttackCooldown = Mathf.Max(0.05f, definition.AttackInterval);
            }

            return true; // pause movement while attacking
        }

        private void AdvancePathIfNeeded(bool forceAdvance = false)
        {
            if (_path.Count == 0) return;

            float thresholdSq = 0.04f;
            while (_pathIndex < _path.Count)
            {
                float distSq = (transform.position - _path[_pathIndex]).sqrMagnitude;
                if (distSq <= thresholdSq || forceAdvance)
                {
                    _pathIndex++;
                    forceAdvance = false;
                }
                else
                {
                    break;
                }
            }
        }

        private Vector3 GetCurrentTarget()
        {
            if (_path.Count > 0 && _pathIndex < _path.Count)
            {
                return _path[_pathIndex];
            }
            return _goal;
        }

        private bool IsAtGoalTarget()
        {
            if (_pathIndex < _path.Count) return false;
            Vector3 delta = _goal - transform.position;
            delta.z = 0f;
            return delta.sqrMagnitude < 0.04f;
        }

        private void RefreshPathRendererVisibility()
        {
            if (!_pathsVisible)
            {
                if (_pathRenderer != null)
                {
                    _pathRenderer.gameObject.SetActive(false);
                }
                return;
            }

            UpdatePathRendererPositions();
        }

        private void EnsurePathRenderer()
        {
            if (_pathRenderer != null) return;

            var go = new GameObject("EnemyPathRenderer")
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            go.transform.SetParent(transform, worldPositionStays: false);
            _pathRenderer = go.AddComponent<LineRenderer>();
            _pathRenderer.useWorldSpace = true;
            _pathRenderer.loop = false;
            _pathRenderer.widthMultiplier = 0.05f;
            _pathRenderer.numCapVertices = 2;
            _pathRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _pathRenderer.sortingOrder = 900;
            _pathRenderer.gameObject.SetActive(false);
        }

        private void UpdatePathRendererPositions()
        {
            EnsurePathRenderer();
            if (_pathRenderer == null) return;

            _pathPreviewPoints.Clear();
            _pathPreviewPoints.Add(_spawnPosition);

            if (_path.Count > 0)
            {
                for (int i = 0; i < _path.Count; i++)
                {
                    _pathPreviewPoints.Add(_path[i]);
                }
            }
            else
            {
                _pathPreviewPoints.Add(_goal);
            }

            _pathRenderer.positionCount = _pathPreviewPoints.Count;
            _pathRenderer.SetPositions(_pathPreviewPoints.ToArray());

            Color color = definition != null ? definition.FactionColor : new Color(0.2f, 0.9f, 1f, 0.7f);
            color.a = 0.7f;
            _pathRenderer.startColor = color;
            _pathRenderer.endColor = color;
            _pathRenderer.gameObject.SetActive(true);
        }

        public static void SetPathVisualization(bool visible)
        {
            if (_pathsVisible == visible) return;
            _pathsVisible = visible;

            foreach (var agent in ActiveSet)
            {
                agent.RefreshPathRendererVisibility();
            }
        }

        private void BuildPlaceholderMesh()
        {
            DisableSpriteRenderers();

            if (GetComponentInChildren<MeshRenderer>() != null) return;

            Color bodyColor = definition != null ? definition.FactionColor : new Color(0.8f, 0.4f, 0.9f);
            Color accentColor = new Color(bodyColor.r * 0.9f, bodyColor.g * 0.9f, bodyColor.b * 1.2f, 1f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "EnemyBody3D";
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(placeholderRadius, placeholderHeight * 0.5f, placeholderRadius);
            body.transform.localPosition = new Vector3(0f, 0f, placeholderHeight * 0.5f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyMaterial(body, bodyColor);

            var accent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            accent.name = "EnemyCore3D";
            accent.transform.SetParent(transform, false);
            accent.transform.localScale = Vector3.one * (placeholderRadius * 0.7f);
            accent.transform.localPosition = new Vector3(0f, 0f, placeholderHeight + placeholderAccentHeight);
            ApplyMaterial(accent, accentColor);

            RemoveCollider(body);
            RemoveCollider(accent);
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
    }
}
