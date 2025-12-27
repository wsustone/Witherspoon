using UnityEngine;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Tracks currency, income ticks, and spending for towers / abilities.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [SerializeField] private int startingGold = 150;
        [SerializeField] private int goldPerWave = 50;
        [SerializeField] private float passiveIncomeInterval = 5f;
        [SerializeField] private int passiveIncomeAmount = 5;

        private int _currentGold;
        private float _incomeTimer;

        public System.Action<int> OnGoldChanged;

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

        private void BroadcastGold()
        {
            OnGoldChanged?.Invoke(_currentGold);
        }
    }
}
