using System.Text;
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
    public class SelectionPanel : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;
        [SerializeField] private TMP_Text statsLabel;

        [Header("Auto Overlay")]
        [SerializeField] private bool autoCreateOverlay = true;
        [SerializeField] private Vector2 overlayAnchorMin = new(0.68f, 0.12f);
        [SerializeField] private Vector2 overlayAnchorMax = new(0.92f, 0.88f);
        [SerializeField] private float overlayCornerRadius = 18f;
        [SerializeField] private Color overlayBackground = new(0.04f, 0.07f, 0.12f, 0.92f);
        [SerializeField] private Color overlayAccent = new(0.56f, 0.89f, 0.97f, 0.9f);

        [Header("Economy + Upgrades")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TMP_Text upgradeButtonLabel;
        [SerializeField] private TMP_Text upgradeButtonCostLabel;
        [SerializeField] private TMP_Text upgradeStatusLabel;
        [SerializeField] private Button repairButton;
        [SerializeField] private TMP_Text repairButtonLabel;
        [SerializeField] private TMP_Text repairButtonCostLabel;

        private readonly StringBuilder _builder = new();
        private TowerController _currentTower;
        private EnemyAgent _currentEnemy;

        private void Awake()
        {
            ResolveEconomyReference();
            EnsureOverlayHierarchy();
            SetUpgradeVisibility(false);
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
                RefreshTowerStats(_currentTower);
                RefreshUpgradeUI(_currentTower);
                RefreshRepairUI(_currentTower);
            }
            else if (_currentEnemy != null)
            {
                RefreshEnemyStats(_currentEnemy);
            }
        }

        public void ShowTower(TowerController tower)
        {
            if (tower == null)
            {
                _currentTower = null;
                _currentEnemy = null;
                Hide();
                return;
            }

            var definition = tower.Definition;
            if (definition == null)
            {
                _currentTower = null;
                _currentEnemy = null;
                Hide();
                return;
            }

            _currentTower = tower;
            _currentEnemy = null;

            SetPanelActive(true);
            RefreshTowerStats(tower);
            SetUpgradeVisibility(true);
            RefreshUpgradeUI(tower);
            SetRepairVisibility(true);
            RefreshRepairUI(tower);
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

            SetPanelActive(true);
            RefreshEnemyStats(enemy);
        }

        public void Hide()
        {
            _currentTower = null;
            _currentEnemy = null;
            SetUpgradeVisibility(false);
            SetRepairVisibility(false);
            SetPanelActive(false);
        }

        private void SetPanelActive(bool active)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(active);
            }
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }

        private void EnsureOverlayHierarchy()
        {
            if (panelRoot != null && titleLabel != null && subtitleLabel != null && statsLabel != null && upgradeButton != null && upgradeButtonLabel != null && upgradeButtonCostLabel != null)
            {
                return;
            }

            if (!autoCreateOverlay)
            {
                if (panelRoot == null)
                {
                    panelRoot = gameObject;
                }
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("SelectionPanel.AutoCreateOverlay is enabled, but no Canvas was found in parents.", this);
                panelRoot = gameObject;
                return;
            }

            var overlayGo = new GameObject("SelectionOverlay", typeof(RectTransform));
            overlayGo.transform.SetParent(canvas.transform, worldPositionStays: false);
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = overlayAnchorMin;
            overlayRect.anchorMax = overlayAnchorMax;
            overlayRect.offsetMin = Vector2.zero;
            overlayRect.offsetMax = Vector2.zero;
            overlayRect.pivot = new Vector2(0.5f, 0.5f);

            var background = overlayGo.AddComponent<Image>();
            background.color = overlayBackground;
            background.raycastTarget = true;

            var layoutGroup = overlayGo.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(18, 18, 22, 22);

            var contentFitter = overlayGo.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleLabel = CreateTextElement("Title", overlayGo.transform, 26, FontStyles.UpperCase | FontStyles.Bold, overlayAccent);
            subtitleLabel = CreateTextElement("Subtitle", overlayGo.transform, 18, FontStyles.Italic, new Color(1f, 1f, 1f, 0.68f));
            statsLabel = CreateTextElement("Stats", overlayGo.transform, 18, FontStyles.Normal, Color.white, lineSpacing: 1.15f);
            statsLabel.enableWordWrapping = true;

            upgradeButton = CreateButtonElement("UpgradeButton", overlayGo.transform, overlayAccent, new Color(0.06f, 0.09f, 0.12f, 0.95f));
            upgradeStatusLabel = CreateTextElement("UpgradeStatus", overlayGo.transform, 16, FontStyles.Italic, new Color(1f, 1f, 1f, 0.7f));
            upgradeStatusLabel.enableWordWrapping = true;

            repairButton = CreateButtonElement("RepairButton", overlayGo.transform, new Color(0.3f, 0.85f, 0.5f, 0.95f), new Color(0.04f, 0.07f, 0.12f, 0.95f));
            var repairCost = repairButton.transform.Find("Content/CostLabel")?.GetComponent<TMP_Text>();
            var repairAction = repairButton.transform.Find("Content/ActionLabel")?.GetComponent<TMP_Text>();
            if (repairCost != null) repairButtonCostLabel = repairCost;
            if (repairAction != null)
            {
                repairButtonLabel = repairAction;
                repairButtonLabel.text = "Repair";
            }

            panelRoot = overlayGo;
        }

        private TMP_Text CreateTextElement(string name, Transform parent, float fontSize, FontStyles fontStyle, Color color, float lineSpacing = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(0f, 0f);
            rect.offsetMax = new Vector2(0f, 0f);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = name;
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = color;
            text.lineSpacing = lineSpacing;
            text.enableWordWrapping = true;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButtonElement(string name, Transform parent, Color backgroundColor, Color textColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = backgroundColor;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 52f;
            layout.preferredHeight = 56f;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(go.transform, false);
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var vertical = contentGo.AddComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.spacing = -4f;
            vertical.padding = new RectOffset(0, 0, 6, 6);

            var costGo = new GameObject("CostLabel", typeof(RectTransform));
            costGo.transform.SetParent(contentGo.transform, false);
            var costLabel = costGo.AddComponent<TextMeshProUGUI>();
            costLabel.text = "0g";
            costLabel.fontSize = 14f;
            costLabel.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
            costLabel.color = new Color(textColor.r, textColor.g, textColor.b, 0.9f);
            costLabel.alignment = TextAlignmentOptions.Center;
            costLabel.raycastTarget = false;
            upgradeButtonCostLabel = costLabel;

            var actionGo = new GameObject("ActionLabel", typeof(RectTransform));
            actionGo.transform.SetParent(contentGo.transform, false);
            var actionLabel = actionGo.AddComponent<TextMeshProUGUI>();
            actionLabel.text = "Upgrade";
            actionLabel.fontSize = 20f;
            actionLabel.fontStyle = FontStyles.SmallCaps | FontStyles.Bold;
            actionLabel.color = textColor;
            actionLabel.alignment = TextAlignmentOptions.Center;
            actionLabel.raycastTarget = false;
            upgradeButtonLabel = actionLabel;

            return button;
        }

        private void HandleGoldChanged(int _)
        {
            if (_currentTower != null)
            {
                RefreshUpgradeUI(_currentTower);
                RefreshRepairUI(_currentTower);
            }
        }

        private void HandleUpgradeClicked()
        {
            if (_currentTower == null || economyManager == null) return;
            if (!_currentTower.CanUpgrade()) return;

            var nextTier = _currentTower.NextTier;
            if (nextTier == null) return;

            var inv = economyManager.Essences;
            var reqEss = nextTier.RequiredEssence;
            int reqAmt = nextTier.RequiredEssenceAmount;
            var reqEssAlt = nextTier.RequiredEssenceAlt;
            int reqAmtAlt = nextTier.RequiredEssenceAltAmount;

            bool req1Valid = reqEss != null && reqAmt > 0;
            bool req2Valid = reqEssAlt != null && reqAmtAlt > 0;
            bool canUse1 = req1Valid && inv != null && inv.GetCount(reqEss) >= reqAmt;
            bool canUse2 = req2Valid && inv != null && inv.GetCount(reqEssAlt) >= reqAmtAlt;
            bool hasEssence = (!req1Valid && !req2Valid) || canUse1 || canUse2;
            if (!hasEssence)
            {
                RefreshUpgradeUI(_currentTower);
                return;
            }

            if (!economyManager.TrySpend(nextTier.Cost))
            {
                RefreshUpgradeUI(_currentTower);
                return;
            }

            if (inv != null)
            {
                bool consumed = false;
                if (canUse1)
                {
                    consumed = inv.TryConsume(reqEss, reqAmt);
                }
                else if (canUse2)
                {
                    consumed = inv.TryConsume(reqEssAlt, reqAmtAlt);
                }

                if (!consumed && (req1Valid || req2Valid))
                {
                    // Refund gold if essence consumption failed due to race conditions
                    economyManager.AddGold(nextTier.Cost);
                    RefreshUpgradeUI(_currentTower);
                    return;
                }
            }

            _currentTower.BeginUpgrade();
            RefreshUpgradeUI(_currentTower);
            RefreshRepairUI(_currentTower);
        }

        private void HandleRepairClicked()
        {
            if (_currentTower == null || economyManager == null) return;
            if (!_currentTower.CanRepair())
            {
                RefreshRepairUI(_currentTower);
                return;
            }

            int cost = _currentTower.GetRepairCost();
            if (!economyManager.TrySpend(cost))
            {
                RefreshRepairUI(_currentTower);
                return;
            }

            _currentTower.BeginRepair();
            RefreshUpgradeUI(_currentTower);
            RefreshRepairUI(_currentTower);
        }

        public void TryRepairCurrentTower()
        {
            if (_currentTower == null || economyManager == null) return;
            if (!_currentTower.CanRepair())
            {
                RefreshRepairUI(_currentTower);
                return;
            }
            int cost = _currentTower.GetRepairCost();
            if (!economyManager.TrySpend(cost))
            {
                RefreshRepairUI(_currentTower);
                return;
            }
            _currentTower.BeginRepair();
            RefreshUpgradeUI(_currentTower);
            RefreshRepairUI(_currentTower);
        }

        private void RefreshUpgradeUI(TowerController tower)
        {
            if (upgradeButton == null) return;
            if (tower == null)
            {
                upgradeButton.interactable = false;
                SetUpgradeTexts("Upgrade", "Select a tower to begin upgrading.", "--");
                return;
            }

            if (!upgradeButton.gameObject.activeSelf)
            {
                SetUpgradeVisibility(true);
            }

            if (tower.IsUpgrading)
            {
                upgradeButton.interactable = false;
                SetUpgradeTexts("Upgrading...", $"Downtime {Mathf.Max(0f, tower.UpgradeTimer):0.0}s remaining", "--");
                return;
            }

            var nextTier = tower.NextTier;
            if (nextTier == null)
            {
                upgradeButton.interactable = false;
                SetUpgradeTexts("Upgrade", "Max tier reached", "--");
                return;
            }

            int cost = nextTier.Cost;
            bool hasEconomy = economyManager != null;
            int availableGold = hasEconomy ? economyManager.CurrentGold : 0;
            bool hasGold = hasEconomy && availableGold >= cost;

            var reqEss = nextTier.RequiredEssence;
            int reqAmt = nextTier.RequiredEssenceAmount;
            var reqEssAlt = nextTier.RequiredEssenceAlt;
            int reqAmtAlt = nextTier.RequiredEssenceAltAmount;

            int haveEss1 = (reqEss != null && economyManager?.Essences != null) ? economyManager.Essences.GetCount(reqEss) : 0;
            int haveEss2 = (reqEssAlt != null && economyManager?.Essences != null) ? economyManager.Essences.GetCount(reqEssAlt) : 0;

            bool req1Valid = reqEss != null && reqAmt > 0;
            bool req2Valid = reqEssAlt != null && reqAmtAlt > 0;
            bool canUse1 = req1Valid && haveEss1 >= reqAmt;
            bool canUse2 = req2Valid && haveEss2 >= reqAmtAlt;
            bool hasEssence = (!req1Valid && !req2Valid) || canUse1 || canUse2;

            upgradeButton.interactable = hasGold && hasEssence && tower.CanUpgrade();

            string tierName = string.IsNullOrWhiteSpace(nextTier.TierName) ? $"Tier {tower.UpgradeTier + 2}" : nextTier.TierName;
            string buttonText = "Upgrade";
            string statusText;
            if (!hasGold)
            {
                statusText = $"Need {Mathf.Max(0, cost - availableGold)}g more for {tierName}";
            }
            else if (!hasEssence)
            {
                if (req1Valid && req2Valid)
                {
                    int need1 = Mathf.Max(0, reqAmt - haveEss1);
                    int need2 = Mathf.Max(0, reqAmtAlt - haveEss2);
                    statusText = $"Need {need1}x {reqEss.DisplayName} OR {need2}x {reqEssAlt.DisplayName}";
                }
                else if (req1Valid)
                {
                    int need = Mathf.Max(0, reqAmt - haveEss1);
                    statusText = $"Need {need}x {reqEss.DisplayName} for {tierName}";
                }
                else if (req2Valid)
                {
                    int need = Mathf.Max(0, reqAmtAlt - haveEss2);
                    statusText = $"Need {need}x {reqEssAlt.DisplayName} for {tierName}";
                }
                else
                {
                    statusText = $"Next: {tierName}";
                }
            }
            else
            {
                statusText = $"Next: {tierName}";
            }

            string costText;
            if (req1Valid && req2Valid)
            {
                costText = $"{cost}g + ({reqAmt}x {reqEss.DisplayName} OR {reqAmtAlt}x {reqEssAlt.DisplayName})";
            }
            else if (req1Valid)
            {
                costText = $"{cost}g + {reqAmt}x {reqEss.DisplayName}";
            }
            else if (req2Valid)
            {
                costText = $"{cost}g + {reqAmtAlt}x {reqEssAlt.DisplayName}";
            }
            else
            {
                costText = $"{cost}g";
            }

            SetUpgradeTexts(buttonText, statusText, costText);
        }

        private void RefreshRepairUI(TowerController tower)
        {
            if (repairButton == null) return;
            if (tower == null)
            {
                repairButton.interactable = false;
                SetRepairTexts("Repair", "--");
                return;
            }

            if (tower.IsUpgrading)
            {
                repairButton.interactable = false;
                if (tower.IsRepairing)
                {
                    SetRepairTexts("Repairing...", "--");
                }
                else
                {
                    SetRepairTexts("Repair", "--");
                }
                return;
            }

            int cost = tower.GetRepairCost();
            bool canRepair = tower.CanRepair();
            bool hasGold = economyManager != null && economyManager.CurrentGold >= cost;
            repairButton.interactable = canRepair && hasGold;

            string costText = canRepair ? $"{cost}g" : "--";
            SetRepairTexts("Repair", costText);
        }

        private void SetRepairVisibility(bool visible)
        {
            if (repairButton != null)
            {
                repairButton.gameObject.SetActive(visible);
            }
            if (repairButtonCostLabel != null)
            {
                repairButtonCostLabel.gameObject.SetActive(visible);
            }
        }

        private void SetRepairTexts(string actionText, string costText)
        {
            if (repairButtonLabel != null)
            {
                repairButtonLabel.text = actionText;
            }
            if (repairButtonCostLabel != null)
            {
                repairButtonCostLabel.text = costText;
            }
        }

        private void SetUpgradeVisibility(bool visible)
        {
            if (upgradeButton != null)
            {
                upgradeButton.gameObject.SetActive(visible);
            }
            if (upgradeButtonCostLabel != null)
            {
                upgradeButtonCostLabel.gameObject.SetActive(visible);
            }
            if (upgradeStatusLabel != null)
            {
                upgradeStatusLabel.gameObject.SetActive(visible);
            }
        }

        private void SetUpgradeTexts(string buttonText, string statusText, string costText)
        {
            if (upgradeButtonLabel != null)
            {
                upgradeButtonLabel.text = buttonText;
            }
            if (upgradeStatusLabel != null)
            {
                upgradeStatusLabel.text = statusText;
            }
            if (upgradeButtonCostLabel != null)
            {
                upgradeButtonCostLabel.text = costText;
            }
        }

        private void ResolveEconomyReference()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }
        }

        private void RefreshTowerStats(TowerController tower)
        {
            if (tower == null || tower.Definition == null)
            {
                Hide();
                return;
            }

            var definition = tower.Definition;
            SetText(titleLabel, definition.TowerName);
            SetText(subtitleLabel, $"{definition.AttackMode} Tower");

            _builder.Length = 0;
            _builder.AppendLine($"Health: {tower.CurrentHealth:0}/{tower.MaxHealth:0}");
            if (definition.UiShowRange) _builder.AppendLine($"Range: {tower.CurrentRange:0.0}");
            if (definition.UiShowFireRate) _builder.AppendLine($"Fire Rate: {tower.CurrentFireRate:0.0}/s");
            if (definition.UiShowDamage) _builder.AppendLine($"Damage: {tower.CurrentDamage:0}");
            if (definition.UiShowSlow && definition.SlowPercent > 0f)
            {
                _builder.AppendLine($"Slow: {(tower.CurrentSlowPercent * 100f):0}% for {definition.EffectDuration:0.0}s");
            }
            if (definition.UiShowRepair && definition.RepairAuraEnabled && tower.HasRepairAura)
            {
                _builder.AppendLine($"Repair: {tower.CurrentRepairPerSecond:0.0}/s | Cap: {tower.CurrentRepairPerAllyCap:0.0}/s/tower | Cost: {tower.CurrentRepairGoldPerHP:0.##} g/HP");
            }
            if (definition.AttackMode == Witherspoon.Game.Data.TowerDefinition.AttackStyle.Cone && definition.UiShowConeAngle)
            {
                _builder.AppendLine($"Cone Angle: {definition.ConeAngle:0}°");
            }
            if (definition.AttackMode == Witherspoon.Game.Data.TowerDefinition.AttackStyle.Projectile && definition.UiShowProjectileDetails)
            {
                _builder.AppendLine(definition.HitsInstantly ? "Hit: Instant" : $"Projectile Speed: {definition.ProjectileSpeed:0.0}");
            }
            if (definition.UiShowKills) _builder.Append($"Kills: {tower.KillCount}");
            SetText(statsLabel, _builder.ToString());
        }

        private void RefreshEnemyStats(EnemyAgent enemy)
        {
            if (enemy == null || enemy.Definition == null)
            {
                Hide();
                return;
            }

            var def = enemy.Definition;
            SetText(titleLabel, def.EnemyName);
            SetText(subtitleLabel, "Enemy");

            _builder.Length = 0;
            if (def.UiShowHealth) _builder.AppendLine($"Health: {enemy.CurrentHealth:0}/{enemy.MaxHealth:0}");
            if (def.UiShowMoveSpeed) _builder.AppendLine($"Move Speed: {def.MoveSpeed:0.0}");
            if (def.UiShowGoldReward) _builder.AppendLine($"Gold Reward: {def.GoldReward}");
            if (def.UiShowArmor && def.Armor > 0f)
            {
                _builder.AppendLine($"Armor: {def.Armor:0}");
            }
            if (def.UiShowResistances)
            {
                bool any = false;
                _builder.Append("Resistances: ");
                void AddRes(string label, float mul)
                {
                    if (Mathf.Abs(mul - 1f) > 0.01f)
                    {
                        if (any) _builder.Append(", ");
                        _builder.Append($"{label} {mul:0.##}x");
                        any = true;
                    }
                }
                AddRes("Proj", def.DmgTakenMulProjectile);
                AddRes("Beam", def.DmgTakenMulBeam);
                AddRes("Cone", def.DmgTakenMulCone);
                AddRes("Aura", def.DmgTakenMulAura);
                AddRes("Wall", def.DmgTakenMulWall);
                if (any) _builder.AppendLine();
            }
            if (def.UiShowSlowResist)
            {
                _builder.AppendLine($"Slow Taken: {(def.SlowEffectiveness * 100f):0}%");
            }
            if (def.UiShowTraits && !string.IsNullOrWhiteSpace(def.SpecialTraits))
            {
                _builder.AppendLine($"Traits: {def.SpecialTraits}");
            }
            if (def.UiShowEssenceDrop && def.DropEssence != null && def.EssenceAmount > 0)
            {
                _builder.Append($"Drops: {def.EssenceAmount}x {def.DropEssence.DisplayName}");
            }
            SetText(statsLabel, _builder.ToString());
        }

        // Public helpers for selection/fusion UX
        public void ShowStatusMessage(string message)
        {
            if (upgradeStatusLabel != null)
            {
                upgradeStatusLabel.text = message;
                upgradeStatusLabel.gameObject.SetActive(true);
            }
            SetPanelActive(true);
        }

        public bool IsPointerOverSelf()
        {
            if (panelRoot == null) return false;
            var rt = panelRoot.GetComponent<RectTransform>();
            if (rt == null) return false;
            return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition);
        }
    }
}
