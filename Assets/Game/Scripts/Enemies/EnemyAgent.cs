using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Map;

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

        private float _health;
        private Vector3 _goal;
        private float _spawnDelay;
        private bool _active;
        private float _slowMultiplier = 1f;
        private float _slowTimer;
        private GridManager _grid;
        private readonly List<Vector3> _path = new();
        private int _pathIndex;
        private bool _pendingPathRefresh;

        public System.Action<EnemyAgent> OnReachedGoal;
        public System.Action<EnemyAgent> OnKilled;

        public EnemyDefinition Definition => definition;

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

            if (_grid != null)
            {
                _grid.GridChanged -= HandleGridChanged;
                _grid.GridChanged += HandleGridChanged;
                RecalculatePath();
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
            MoveTowardsGoal();
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

        public void ApplyDamage(float amount)
        {
            _health -= amount;
            if (_health <= 0f)
            {
                OnKilled?.Invoke(this);
                OnAnyKilled?.Invoke(this);
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
            float candidateMultiplier = 1f - clampedPercent;

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
    }
}
