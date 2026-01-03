using UnityEngine;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.Towers
{
    /// <summary>
    /// Manages tower upgrades, morphing, and repair progression.
    /// </summary>
    public class TowerUpgrade : MonoBehaviour
    {
        [Header("Repair")]
        [SerializeField] private bool repairEnabled = true;
        [SerializeField] private float repairCostPerHP = 0.5f;
        [SerializeField] private float baseRepairTime = 1.5f;

        [Header("Upgrades")]
        [SerializeField] private AnimationCurve upgradeRangeSmoothing = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private int _upgradeTier;
        private float _upgradeTimer;
        private bool _isUpgrading;
        private bool _isMorphing;
        private bool _isRepairing;

        private TowerController _controller;
        private TowerHealth _health;

        public int UpgradeTier => _upgradeTier;
        public bool IsUpgrading => _isUpgrading;
        public bool IsRepairing => _isRepairing;
        public float UpgradeTimer => _upgradeTimer;
        public TowerDefinition.TowerUpgradeTier NextTier
        {
            get
            {
                var def = _controller?.Definition;
                return def != null && def.UpgradeTiers != null && _upgradeTier < def.UpgradeTiers.Length
                    ? def.UpgradeTiers[_upgradeTier]
                    : null;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<TowerController>();
            _health = GetComponent<TowerHealth>();
        }

        public void UpdateUpgradeProgress(float deltaTime)
        {
            if (!_isUpgrading) return;

            _upgradeTimer -= deltaTime;
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
        }

        public bool CanUpgrade()
        {
            var def = _controller?.Definition;
            if (def == null || def.UpgradeTiers == null) return false;
            if (_upgradeTier >= def.UpgradeTiers.Length) return false;
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
            _controller?.RecalculateStats();
        }

        public void BeginMorph(float seconds)
        {
            _isMorphing = true;
            _isUpgrading = true;
            _upgradeTimer = Mathf.Max(0f, seconds);
        }

        public void ResetUpgrades()
        {
            _upgradeTier = 0;
            _upgradeTimer = 0f;
            _isUpgrading = false;
            _isMorphing = false;
            _isRepairing = false;
        }

        public bool CanRepair()
        {
            if (!repairEnabled) return false;
            if (_isUpgrading) return false;
            if (_health == null) return false;
            return _health.CurrentHealth < _health.MaxHealth - 0.01f;
        }

        public int GetRepairCost()
        {
            if (_health == null) return 0;
            float missing = Mathf.Max(0f, _health.MaxHealth - _health.CurrentHealth);
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
            _health?.FullRepair();
        }

        public float GetUpgradeMultiplier(int tier, System.Func<TowerDefinition.TowerUpgradeTier, float> selector)
        {
            var def = _controller?.Definition;
            if (def?.UpgradeTiers == null) return 1f;

            float multiplier = 1f;
            for (int i = 0; i < tier && i < def.UpgradeTiers.Length; i++)
            {
                multiplier *= selector(def.UpgradeTiers[i]);
            }
            return multiplier;
        }
    }
}
