using System.Collections.Generic;
using UnityEngine;
using Witherspoon.Game.Data;
using Witherspoon.Game.Map;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.Enemies
{
    /// <summary>
    /// Core enemy agent: health, damage, and coordination of movement/visuals.
    /// </summary>
    [RequireComponent(typeof(EnemyMovement))]
    [RequireComponent(typeof(EnemyVisuals))]
    public class EnemyAgent : MonoBehaviour
    {
        private static readonly HashSet<EnemyAgent> ActiveSet = new();
        public static IReadOnlyCollection<EnemyAgent> ActiveAgents => ActiveSet;
        public static System.Action<EnemyAgent> OnAnyKilled;
        public static System.Action<EnemyAgent> OnAnyReachedGoal;

        [SerializeField] private EnemyDefinition definition;

        private float _health;
        private EnemyMovement _movement;
        private EnemyVisuals _visuals;

        public System.Action<EnemyAgent> OnReachedGoal;
        public System.Action<EnemyAgent> OnKilled;

        public EnemyDefinition Definition => definition;
        public float CurrentHealth => _health;
        public float MaxHealth => definition != null ? definition.BaseHealth : 100f;
        public static bool PathsVisible => EnemyVisuals.PathsVisible;

        private void Awake()
        {
            _movement = GetComponent<EnemyMovement>();
            _visuals = GetComponent<EnemyVisuals>();
        }

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
            Initialize(def, goal, spawnDelay, null);
        }

        public void Initialize(EnemyDefinition def, Vector3 goal, float spawnDelay, GridManager grid)
        {
            definition = def;
            _health = definition != null ? definition.BaseHealth : 100f;

            _movement?.Initialize(goal, spawnDelay, grid);
            _visuals?.Initialize(definition);
        }

        private void Update()
        {
            if (definition == null) return;
            _movement?.UpdateMovement();
        }

        private void LateUpdate()
        {
            _visuals?.UpdatePathVisualization();
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

        public void ApplySlow(float slowPercent, float duration)
        {
            _movement?.ApplySlow(slowPercent, duration);
        }

        public void TriggerReachedGoal()
        {
            OnReachedGoal?.Invoke(this);
            OnAnyReachedGoal?.Invoke(this);
            Destroy(gameObject);
        }

        public static void SetPathVisualization(bool visible)
        {
            EnemyVisuals.SetPathVisualization(visible);
        }
    }
}
