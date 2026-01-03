using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Handles auto-creation of SelectionPanel UI hierarchy.
    /// </summary>
    public class SelectionPanelBuilder : MonoBehaviour
    {
        [Header("Auto Overlay")]
        [SerializeField] private bool autoCreateOverlay = true;
        [SerializeField] private Vector2 overlayAnchorMin = new(0.68f, 0.12f);
        [SerializeField] private Vector2 overlayAnchorMax = new(0.92f, 0.88f);
        [SerializeField] private Color overlayBackground = new(0.04f, 0.07f, 0.12f, 0.92f);
        [SerializeField] private Color overlayAccent = new(0.56f, 0.89f, 0.97f, 0.9f);

        public GameObject PanelRoot { get; private set; }
        public TMP_Text TitleLabel { get; private set; }
        public TMP_Text SubtitleLabel { get; private set; }
        public TMP_Text StatsLabel { get; private set; }
        public Button UpgradeButton { get; private set; }
        public TMP_Text UpgradeButtonLabel { get; private set; }
        public TMP_Text UpgradeButtonCostLabel { get; private set; }
        public TMP_Text UpgradeStatusLabel { get; private set; }
        public Button RepairButton { get; private set; }
        public TMP_Text RepairButtonLabel { get; private set; }
        public TMP_Text RepairButtonCostLabel { get; private set; }

        public void BuildUI(GameObject existingRoot, TMP_Text existingTitle, TMP_Text existingSubtitle, 
            TMP_Text existingStats, Button existingUpgrade, TMP_Text existingUpgradeLabel, 
            TMP_Text existingUpgradeCost, TMP_Text existingUpgradeStatus, Button existingRepair,
            TMP_Text existingRepairLabel, TMP_Text existingRepairCost)
        {
            if (existingRoot != null && existingTitle != null && existingSubtitle != null && 
                existingStats != null && existingUpgrade != null && existingUpgradeLabel != null && 
                existingUpgradeCost != null)
            {
                PanelRoot = existingRoot;
                TitleLabel = existingTitle;
                SubtitleLabel = existingSubtitle;
                StatsLabel = existingStats;
                UpgradeButton = existingUpgrade;
                UpgradeButtonLabel = existingUpgradeLabel;
                UpgradeButtonCostLabel = existingUpgradeCost;
                UpgradeStatusLabel = existingUpgradeStatus;
                RepairButton = existingRepair;
                RepairButtonLabel = existingRepairLabel;
                RepairButtonCostLabel = existingRepairCost;
                return;
            }

            if (!autoCreateOverlay)
            {
                PanelRoot = existingRoot ?? gameObject;
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("SelectionPanel.AutoCreateOverlay is enabled, but no Canvas was found in parents.", this);
                PanelRoot = gameObject;
                return;
            }

            CreateOverlay(canvas);
        }

        private void CreateOverlay(Canvas canvas)
        {
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

            TitleLabel = CreateTextElement("Title", overlayGo.transform, 26, FontStyles.UpperCase | FontStyles.Bold, overlayAccent);
            SubtitleLabel = CreateTextElement("Subtitle", overlayGo.transform, 18, FontStyles.Italic, new Color(1f, 1f, 1f, 0.68f));
            StatsLabel = CreateTextElement("Stats", overlayGo.transform, 18, FontStyles.Normal, Color.white, lineSpacing: 1.15f);
            StatsLabel.enableWordWrapping = true;

            UpgradeButton = CreateButtonElement("UpgradeButton", overlayGo.transform, overlayAccent, new Color(0.06f, 0.09f, 0.12f, 0.95f));
            UpgradeStatusLabel = CreateTextElement("UpgradeStatus", overlayGo.transform, 16, FontStyles.Italic, new Color(1f, 1f, 1f, 0.7f));
            UpgradeStatusLabel.enableWordWrapping = true;

            RepairButton = CreateButtonElement("RepairButton", overlayGo.transform, new Color(0.3f, 0.85f, 0.5f, 0.95f), new Color(0.04f, 0.07f, 0.12f, 0.95f));
            var repairCost = RepairButton.transform.Find("Content/CostLabel")?.GetComponent<TMP_Text>();
            var repairAction = RepairButton.transform.Find("Content/ActionLabel")?.GetComponent<TMP_Text>();
            if (repairCost != null) RepairButtonCostLabel = repairCost;
            if (repairAction != null)
            {
                RepairButtonLabel = repairAction;
                RepairButtonLabel.text = "Repair";
            }

            PanelRoot = overlayGo;
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
            vertical.spacing = 2f;
            vertical.padding = new RectOffset(12, 12, 8, 8);

            var actionLabel = CreateTextElement("ActionLabel", contentGo.transform, 18, FontStyles.Bold, textColor);
            var costLabel = CreateTextElement("CostLabel", contentGo.transform, 14, FontStyles.Normal, new Color(textColor.r, textColor.g, textColor.b, 0.8f));

            UpgradeButtonLabel = actionLabel;
            UpgradeButtonCostLabel = costLabel;

            return button;
        }
    }
}
