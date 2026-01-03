using UnityEngine;
using Witherspoon.Game.Enemies;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Manages tower health, armor, damage handling, and destruction.
    /// </summary>
    public class TowerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 200f;
        [SerializeField] private float armor = 0f;

        private float _health;
        private TowerController _controller;

        public float CurrentHealth => _health;
        public float MaxHealth => maxHealth;
        public float Armor => armor;

        private void Awake()
        {
            _controller = GetComponent<TowerController>();
            _health = maxHealth;
        }

        public void Initialize(float initialHealth)
        {
            if (initialHealth <= 0f)
            {
                _health = maxHealth;
            }
            else
            {
                _health = initialHealth;
            }
        }

        public void ApplyDamage(float amount, EnemyAgent source)
        {
            float final = Mathf.Max(0f, amount - armor);
            _health -= final;
            
            if (final > 0.01f)
            {
                var audio = GetComponent<TowerAudio>();
                audio?.PlayHitSfx();
            }
            
            if (_health <= 0f)
            {
                HandleDestroyed();
            }
        }

        public float ApplyRepair(float amount)
        {
            if (amount <= 0f) return 0f;
            float before = _health;
            _health = Mathf.Min(maxHealth, _health + amount);
            return _health - before;
        }

        public void FullRepair()
        {
            _health = maxHealth;
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

        // Ensure health bar exists
        private void Start()
        {
            if (GetComponent<TowerHealthBar>() == null)
            {
                gameObject.AddComponent<TowerHealthBar>();
            }
        }
    }
}
