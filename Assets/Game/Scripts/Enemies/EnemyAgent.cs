using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;

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
        }

        public void Initialize(EnemyDefinition def, Vector3 goal, float spawnDelay)
        {
            definition = def;
            _goal = new Vector3(goal.x, goal.y, 0f);
            _spawnDelay = spawnDelay;
            _health = definition != null ? definition.BaseHealth : 100f;
            _active = false;
            // Ensure enemies start on the 2D plane (z = 0)
            var pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y, 0f);
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

            MoveTowardsGoal();
        }

        private void MoveTowardsGoal()
        {
            Vector3 direction = _goal - transform.position;
            direction.z = 0f;
            float distance = direction.magnitude;
            if (distance < 0.05f)
            {
                OnReachedGoal?.Invoke(this);
                OnAnyReachedGoal?.Invoke(this);
                Destroy(gameObject);
                return;
            }

            direction.Normalize();
            transform.position += direction * (definition.MoveSpeed * Time.deltaTime);
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
    }
}
