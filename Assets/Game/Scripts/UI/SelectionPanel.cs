using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
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

        private readonly StringBuilder _builder = new();

        private void Awake()
        {
            EnsureOverlayHierarchy();
            Hide();
        }

        public void ShowTower(TowerController tower)
        {
            if (tower == null)
            {
                Hide();
                return;
            }

            var definition = tower.Definition;
            if (definition == null)
            {
                Hide();
                return;
            }

            SetPanelActive(true);
            SetText(titleLabel, definition.TowerName);
            SetText(subtitleLabel, $"{definition.AttackMode} Tower");

            _builder.Length = 0;
            _builder.AppendLine($"Range: {definition.Range:0.0}");
            _builder.AppendLine($"Fire Rate: {definition.FireRate:0.0}/s");
            _builder.AppendLine($"Damage: {definition.Damage:0}");
            _builder.Append($"Kills: {tower.KillCount}");
            SetText(statsLabel, _builder.ToString());
        }

        public void ShowEnemy(EnemyAgent enemy)
        {
            if (enemy == null || enemy.Definition == null)
            {
                Hide();
                return;
            }

            var def = enemy.Definition;

            SetPanelActive(true);
            SetText(titleLabel, def.EnemyName);
            SetText(subtitleLabel, "Enemy");

            _builder.Length = 0;
            _builder.AppendLine($"Health: {enemy.CurrentHealth:0}/{enemy.MaxHealth:0}");
            _builder.AppendLine($"Move Speed: {def.MoveSpeed:0.0}");
            _builder.Append($"Gold Reward: {def.GoldReward}");
            SetText(statsLabel, _builder.ToString());
        }

        public void Hide()
        {
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
            if (panelRoot != null && titleLabel != null && subtitleLabel != null && statsLabel != null)
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
    }
}
