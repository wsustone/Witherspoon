using UnityEngine;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Tracks currency, income ticks, and spending for towers / abilities.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [SerializeField] private int startingGold = 300;
        [SerializeField] private int goldPerWave = 50;
        [SerializeField] private float passiveIncomeInterval = 5f;
        [SerializeField] private int passiveIncomeAmount = 5;
        [SerializeField] private EssenceInventory essenceInventory;

        private int _currentGold;
        private float _incomeTimer;

        public System.Action<int> OnGoldChanged;
        public int CurrentGold => _currentGold;
        public EssenceInventory Essences => essenceInventory;

        private void OnEnable()
        {
            EnemyAgent.OnAnyKilled += HandleEnemyKilled;
        }

        private void OnDisable()
        {
            EnemyAgent.OnAnyKilled -= HandleEnemyKilled;
        }

        public void Initialize()
        {
            _currentGold = startingGold;
            _incomeTimer = passiveIncomeInterval;
            BroadcastGold();
        }

        public void Tick(float deltaTime)
        {
            _incomeTimer -= deltaTime;
            if (_incomeTimer <= 0f)
            {
                AddGold(passiveIncomeAmount);
                _incomeTimer = passiveIncomeInterval;
            }
        }

        public bool TrySpend(int amount)
        {
            if (_currentGold < amount) return false;
            _currentGold -= amount;
            BroadcastGold();
            return true;
        }

        public void AddGold(int amount)
        {
            _currentGold += amount;
            BroadcastGold();
        }

        public void OnWaveCompleted()
        {
            AddGold(goldPerWave);
        }

        private void HandleEnemyKilled(EnemyAgent enemy)
        {
            if (enemy?.Definition == null) return;
            AddGold(enemy.Definition.GoldReward);
            var drop = enemy.Definition.DropEssence;
            var amount = enemy.Definition.EssenceAmount;
            if (drop != null && amount > 0 && essenceInventory != null)
            {
                essenceInventory.AddEssence(drop, amount);
            }
        }

        private void BroadcastGold()
        {
            OnGoldChanged?.Invoke(_currentGold);
        }
    }
}
