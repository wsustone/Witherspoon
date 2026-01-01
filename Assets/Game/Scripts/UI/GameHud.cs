using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Witherspoon.Game.Core;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.UI
{
    public class GameHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameSession session;
        [SerializeField] private RectTransform rowsContainer;

        [Header("Layout")]
        [SerializeField] private float rowSpacing = 4f;
        [SerializeField] private float defaultFontSize = 22f;
        [SerializeField] private Color defaultColor = new(1f, 1f, 1f, 0.95f);

        [Header("Rows")]
        [SerializeField] private List<HudRowConfig> rowConfigs = new()
        {
            new HudRowConfig { stat = GameStatType.Mode, label = "Mode" },
            new HudRowConfig { stat = GameStatType.Lives, label = "Lives" },
            new HudRowConfig { stat = GameStatType.Escapes, label = "Escapes" },
            new HudRowConfig { stat = GameStatType.Damage, label = "Damage" }
        };

        private readonly List<GameObject> _generatedRows = new();

        private enum GameStatType
        {
            Mode,
            Lives,
            Escapes,
            Damage
        }

        [System.Serializable]
        private class HudRowConfig
        {
            public GameStatType stat = GameStatType.Mode;
            public string label = "Label";
            public Color color = Color.white;
            public float fontSizeOverride = 0f;

            [HideInInspector] public TMP_Text runtimeText;
            [HideInInspector] public GameObject runtimeObject;
        }

        private void Awake()
        {
            if (session == null)
            {
                session = FindObjectOfType<GameSession>();
            }
            EnsureContainer();
            BuildRows();
            RefreshAll();
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.OnStatsChanged += HandleStatsChanged;
            }
            RefreshAll();
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.OnStatsChanged -= HandleStatsChanged;
            }
        }

        private void HandleStatsChanged(GameSession _) => RefreshAll();

        private void RefreshAll()
        {
            if (session == null) return;

            foreach (var config in rowConfigs)
            {
                if (config.runtimeObject == null || config.runtimeText == null) continue;

                if (TryGetRowState(config.stat, out string value, out bool visible))
                {
                    config.runtimeObject.SetActive(visible);
                    if (visible)
                    {
                        config.runtimeText.text = string.IsNullOrEmpty(config.label)
                            ? value
                            : $"{config.label}: {value}";
                    }
                }
                else
                {
                    config.runtimeObject.SetActive(false);
                }
            }
        }

        private bool TryGetRowState(GameStatType stat, out string value, out bool visible)
        {
            value = string.Empty;
            visible = true;

            var gm = session.Mode;

            switch (stat)
            {
                case GameStatType.Mode:
                    value = gm != null ? gm.DisplayName : "Custom";
                    return true;
                case GameStatType.Lives:
                    visible = gm != null && gm.StartingLives > 0;
                    value = session.LivesRemaining.ToString();
                    return true;
                case GameStatType.Escapes:
                    visible = gm == null || gm.MaxEscapes > 0 || !gm.DefeatOnFirstLeak;
                    value = session.Escapes.ToString();
                    return true;
                case GameStatType.Damage:
                    visible = gm != null && gm.MaxDamage > 0f;
                    value = session.DamageAccumulated.ToString("0");
                    return true;
                default:
                    return false;
            }
        }

        private void EnsureContainer()
        {
            if (rowsContainer == null)
            {
                var go = new GameObject("GameHudRows", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                rowsContainer = go.GetComponent<RectTransform>();
            }

            var layout = rowsContainer.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = rowsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = rowSpacing;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = false;

            var fitter = rowsContainer.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = rowsContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void BuildRows()
        {
            foreach (var go in _generatedRows)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }
            _generatedRows.Clear();

            foreach (var config in rowConfigs)
            {
                var row = new GameObject(config.label ?? config.stat.ToString(), typeof(RectTransform));
                row.transform.SetParent(rowsContainer, false);
                var hLayout = row.AddComponent<HorizontalLayoutGroup>();
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childForceExpandHeight = false;
                hLayout.childForceExpandWidth = false;

                var labelText = row.AddComponent<TextMeshProUGUI>();
                labelText.fontSize = config.fontSizeOverride > 0f ? config.fontSizeOverride : defaultFontSize;
                labelText.color = config.color.a > 0f ? config.color : defaultColor;
                labelText.alignment = TextAlignmentOptions.MidlineLeft;
                labelText.text = config.label;

                config.runtimeText = labelText;
                config.runtimeObject = row;
                _generatedRows.Add(row);
            }
        }
    }
}
