using System.Text;
using TMPro;
using UnityEngine;
using Witherspoon.Game.Core;
using Witherspoon.Game.Enemies;
using Witherspoon.Game.Towers;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Handles refreshing tower and enemy stats display.
    /// </summary>
    public class SelectionPanelUpdater : MonoBehaviour
    {
        private readonly StringBuilder _builder = new();
        private EconomyManager _economyManager;

        public void Initialize(EconomyManager economy)
        {
            _economyManager = economy;
        }

        public void RefreshTowerStats(TowerController tower, TMP_Text titleLabel, TMP_Text subtitleLabel, TMP_Text statsLabel)
        {
            if (tower == null || tower.Definition == null) return;

            var def = tower.Definition;
            SetText(titleLabel, def.TowerName);
            SetText(subtitleLabel, $"Tier {def.Tier}");

            _builder.Clear();
            var health = tower.GetComponent<TowerHealth>();
            if (health != null)
            {
                _builder.AppendLine($"<b>Health:</b> {health.CurrentHealth:0}/{health.MaxHealth:0}");
                _builder.AppendLine($"<b>Armor:</b> {health.Armor:0}");
            }
            _builder.AppendLine($"<b>Damage:</b> {tower.CurrentDamage:0.0}");
            _builder.AppendLine($"<b>Range:</b> {def.Range:0.0}");
            _builder.AppendLine($"<b>Fire Rate:</b> {def.FireRate:0.0}/s");
            _builder.AppendLine($"<b>Kills:</b> {tower.Kills}");

            SetText(statsLabel, _builder.ToString());
        }

        public void RefreshEnemyStats(EnemyAgent enemy, TMP_Text titleLabel, TMP_Text subtitleLabel, TMP_Text statsLabel)
        {
            if (enemy == null || enemy.Definition == null) return;

            var def = enemy.Definition;
            SetText(titleLabel, def.EnemyName);
            SetText(subtitleLabel, "Enemy");

            _builder.Clear();
            _builder.AppendLine($"<b>Health:</b> {enemy.CurrentHealth:0}/{enemy.MaxHealth:0}");
            _builder.AppendLine($"<b>Armor:</b> {def.Armor:0}");
            _builder.AppendLine($"<b>Speed:</b> {def.MoveSpeed:0.0}");

            SetText(statsLabel, _builder.ToString());
        }

        public void RefreshUpgradeUI(TowerController tower, TMP_Text buttonLabel, TMP_Text costLabel, TMP_Text statusLabel, UnityEngine.UI.Button button)
        {
            if (tower == null) return;

            var upgrade = tower.GetComponent<TowerUpgrade>();
            if (upgrade == null) return;

            bool canUpgrade = upgrade.CanUpgrade();
            bool canMorph = upgrade.CanMorph();
            int gold = _economyManager != null ? _economyManager.Gold : 0;

            if (canUpgrade)
            {
                int cost = upgrade.UpgradeCost;
                bool affordable = gold >= cost;
                SetText(buttonLabel, "Upgrade");
                SetText(costLabel, $"Cost: {cost} gold");
                if (button != null) button.interactable = affordable;
                SetText(statusLabel, affordable ? "" : "Not enough gold");
            }
            else if (canMorph)
            {
                int cost = upgrade.MorphCost;
                bool affordable = gold >= cost;
                SetText(buttonLabel, "Morph");
                SetText(costLabel, $"Cost: {cost} gold");
                if (button != null) button.interactable = affordable;
                SetText(statusLabel, affordable ? "" : "Not enough gold");
            }
            else
            {
                SetText(buttonLabel, "Max Level");
                SetText(costLabel, "");
                if (button != null) button.interactable = false;
                SetText(statusLabel, "");
            }
        }

        public void RefreshRepairUI(TowerController tower, TMP_Text buttonLabel, TMP_Text costLabel, UnityEngine.UI.Button button)
        {
            if (tower == null) return;

            var upgrade = tower.GetComponent<TowerUpgrade>();
            if (upgrade == null) return;

            bool canRepair = upgrade.CanRepair();
            int gold = _economyManager != null ? _economyManager.Gold : 0;

            if (canRepair)
            {
                int cost = upgrade.RepairCost;
                bool affordable = gold >= cost;
                SetText(buttonLabel, "Repair");
                SetText(costLabel, $"Cost: {cost} gold");
                if (button != null) button.interactable = affordable;
            }
            else
            {
                SetText(buttonLabel, "Full Health");
                SetText(costLabel, "");
                if (button != null) button.interactable = false;
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
