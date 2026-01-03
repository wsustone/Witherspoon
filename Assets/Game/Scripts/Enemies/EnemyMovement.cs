using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Map;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Handles enemy pathfinding, movement, and tower attacks.
    /// </summary>
    public class EnemyMovement : MonoBehaviour
    {
        private Vector3 _goal;
        private Vector3 _spawnPosition;
        private float _spawnDelay;
        private bool _active;
        private float _slowMultiplier = 1f;
        private float _slowTimer;
        private GridManager _grid;
        private readonly List<Vector3> _path = new();
        private int _pathIndex;
        private bool _pendingPathRefresh;
        private float _towerAttackCooldown;
        private TowerController _currentTowerTarget;

        private EnemyAgent _agent;

        public bool IsActive => _active;
        public Vector3 SpawnPosition => _spawnPosition;
        public List<Vector3> Path => _path;

        private void Awake()
        {
            _agent = GetComponent<EnemyAgent>();
        }

        public void Initialize(Vector3 goal, float spawnDelay, GridManager grid)
        {
            _goal = new Vector3(goal.x, goal.y, 0f);
            _spawnDelay = spawnDelay;
            _active = false;
            _grid = grid;
            _path.Clear();
            _pathIndex = 0;
            _pendingPathRefresh = false;

            var pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            _spawnPosition = transform.position;

            if (_grid != null)
            {
                _grid.GridChanged -= HandleGridChanged;
                _grid.GridChanged += HandleGridChanged;
                RecalculatePath();
            }
        }

        private void OnDisable()
        {
            if (_grid != null)
            {
                _grid.GridChanged -= HandleGridChanged;
                _grid = null;
            }
        }

        public void UpdateMovement()
        {
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

            bool attackingTower = TowerAttackUpdate();
            if (!attackingTower)
            {
                MoveTowardsGoal();
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
                    _agent?.TriggerReachedGoal();
                }
                else
                {
                    AdvancePathIfNeeded(forceAdvance: true);
                }
                return;
            }

            direction.Normalize();
            float effectiveSpeed = _agent.Definition.MoveSpeed * _slowMultiplier;
            float step = effectiveSpeed * Time.deltaTime;
            transform.position += direction * Mathf.Min(step, distance);
            var pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, 0f);
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
            float effectiveness = _agent?.Definition != null ? _agent.Definition.SlowEffectiveness : 1f;
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

        public void RecalculatePath()
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

        private bool TowerAttackUpdate()
        {
            if (_agent?.Definition == null || !_agent.Definition.CanAttackTowers)
            {
                _currentTowerTarget = null;
                return false;
            }

            float range = Mathf.Max(0f, _agent.Definition.AttackRange);
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
                var towerHealth = _currentTowerTarget.GetComponent<TowerHealth>();
                towerHealth?.ApplyDamage(_agent.Definition.AttackDamage, _agent);
                _towerAttackCooldown = Mathf.Max(0.05f, _agent.Definition.AttackInterval);
            }

            return true;
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
