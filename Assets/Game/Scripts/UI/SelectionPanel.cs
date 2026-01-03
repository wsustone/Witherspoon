using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Witherspoon.Game.Core;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Displays contextual stats for the currently selected tower or enemy.
    /// </summary>
    [RequireComponent(typeof(SelectionPanelBuilder))]
    [RequireComponent(typeof(SelectionPanelUpdater))]
    public class SelectionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text statsLabel;

        [Header("Economy + Upgrades")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonLabel;
        [SerializeField] private TMP_Text upgradeButtonCostLabel;
        [SerializeField] private TMP_Text upgradeStatusLabel;
        [SerializeField] private Button repairButton;
        [SerializeField] private TMP_Text repairButtonLabel;
        [SerializeField] private TMP_Text repairButtonCostLabel;

        private TowerController _currentTower;
        private EnemyAgent _currentEnemy;
        private SelectionPanelBuilder _builder;
        private SelectionPanelUpdater _updater;

        private void Awake()
        {
            _builder = GetComponent<SelectionPanelBuilder>();
            _updater = GetComponent<SelectionPanelUpdater>();

            ResolveEconomyReference();
            
            _builder.BuildUI(panelRoot, titleLabel, subtitleLabel, statsLabel, 
                upgradeButton, upgradeButtonLabel, upgradeButtonCostLabel, upgradeStatusLabel,
                repairButton, repairButtonLabel, repairButtonCostLabel);

            panelRoot = _builder.PanelRoot;
            titleLabel = _builder.TitleLabel;
            subtitleLabel = _builder.SubtitleLabel;
            statsLabel = _builder.StatsLabel;
            upgradeButton = _builder.UpgradeButton;
            upgradeButtonLabel = _builder.UpgradeButtonLabel;
            upgradeButtonCostLabel = _builder.UpgradeButtonCostLabel;
            upgradeStatusLabel = _builder.UpgradeStatusLabel;
            repairButton = _builder.RepairButton;
            repairButtonLabel = _builder.RepairButtonLabel;
            repairButtonCostLabel = _builder.RepairButtonCostLabel;

            _updater.Initialize(economyManager);

            SetUpgradeVisibility(false);
            SetRepairVisibility(false);
            Hide();
        }

        private void OnEnable()
        {
            ResolveEconomyReference();
            if (economyManager != null)
            {
                economyManager.OnGoldChanged += HandleGoldChanged;
            }
            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(HandleUpgradeClicked);
            }
            if (repairButton != null)
            {
                repairButton.onClick.AddListener(HandleRepairClicked);
            }
        }

        private void OnDisable()
        {
            if (economyManager != null)
            {
                economyManager.OnGoldChanged -= HandleGoldChanged;
            }
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
            }
            if (repairButton != null)
            {
                repairButton.onClick.RemoveListener(HandleRepairClicked);
            }
        }

        private void Update()
        {
            if (_currentTower == null && _currentEnemy == null)
            {
                if (panelRoot != null && panelRoot.activeSelf)
                {
                    Hide();
                }
                return;
            }

            if (_currentTower != null)
            {
                _updater?.RefreshTowerStats(_currentTower, titleLabel, subtitleLabel, statsLabel);
                _updater?.RefreshUpgradeUI(_currentTower, upgradeButtonLabel, upgradeButtonCostLabel, upgradeStatusLabel, upgradeButton);
                _updater?.RefreshRepairUI(_currentTower, repairButtonLabel, repairButtonCostLabel, repairButton);
            }
            else if (_currentEnemy != null)
            {
                _updater?.RefreshEnemyStats(_currentEnemy, titleLabel, subtitleLabel, statsLabel);
            }
        }

        public void ShowTower(TowerController tower)
        {
            if (tower == null || tower.Definition == null)
            {
                _currentTower = null;
                _currentEnemy = null;
                Hide();
                return;
            }

            _currentTower = tower;
            _currentEnemy = null;

            SetPanelActive(true);
            _updater?.RefreshTowerStats(tower, titleLabel, subtitleLabel, statsLabel);
            SetUpgradeVisibility(true);
            _updater?.RefreshUpgradeUI(tower, upgradeButtonLabel, upgradeButtonCostLabel, upgradeStatusLabel, upgradeButton);
            SetRepairVisibility(true);
            _updater?.RefreshRepairUI(tower, repairButtonLabel, repairButtonCostLabel, repairButton);
        }

        public void ShowEnemy(EnemyAgent enemy)
        {
            if (enemy == null || enemy.Definition == null)
            {
                _currentTower = null;
                _currentEnemy = null;
                Hide();
                return;
            }

            _currentTower = null;
            _currentEnemy = enemy;
            SetUpgradeVisibility(false);
            SetRepairVisibility(false);

            SetPanelActive(true);
            _updater?.RefreshEnemyStats(enemy, titleLabel, subtitleLabel, statsLabel);
        }

        public void Hide()
        {
            _currentTower = null;
            _currentEnemy = null;
            SetUpgradeVisibility(false);
            SetRepairVisibility(false);
            SetPanelActive(false);
        }

        public void ShowStatusMessage(string message)
        {
            if (upgradeStatusLabel != null)
            {
                upgradeStatusLabel.text = message;
            }
        }

        public bool IsPointerOverSelf()
        {
            if (panelRoot == null || !panelRoot.activeSelf) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(
                panelRoot.GetComponent<RectTransform>(),
                Input.mousePosition,
                null
            );
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }
        }

        private void SetUpgradeVisibility(bool visible)
        {
            if (upgradeButton != null) upgradeButton.gameObject.SetActive(visible);
            if (upgradeStatusLabel != null) upgradeStatusLabel.gameObject.SetActive(visible);
        }

        private void SetRepairVisibility(bool visible)
        {
            if (repairButton != null) repairButton.gameObject.SetActive(visible);
        }

        private void HandleUpgradeClicked()
        {
            if (_currentTower == null) return;
            var upgrade = _currentTower.GetComponent<TowerUpgrade>();
            if (upgrade == null) return;

            if (upgrade.CanUpgrade())
            {
                upgrade.StartUpgrade(economyManager);
            }
            else if (upgrade.CanMorph())
            {
                upgrade.StartMorph(economyManager);
            }
        }

        private void HandleRepairClicked()
        {
            if (_currentTower == null) return;
            var upgrade = _currentTower.GetComponent<TowerUpgrade>();
            upgrade?.StartRepair(economyManager);
        }

        private void HandleGoldChanged(int newGold)
        {
            if (_currentTower != null)
            {
                _updater?.RefreshUpgradeUI(_currentTower, upgradeButtonLabel, upgradeButtonCostLabel, upgradeStatusLabel, upgradeButton);
                _updater?.RefreshRepairUI(_currentTower, repairButtonLabel, repairButtonCostLabel, repairButton);
            }
        }

        private void ResolveEconomyReference()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }
        }
    }
}
