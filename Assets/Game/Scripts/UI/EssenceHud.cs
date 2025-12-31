using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Witherspoon.Game.Core;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.UI
{
    public class EssenceHud : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private RectTransform container;

        [Header("Config")] 
        [SerializeField] private List<EssenceDefinition> essencesToShow = new();
        [SerializeField] private Vector2 iconSize = new Vector2(20, 20);
        [SerializeField] private float rowSpacing = 4f;
        [SerializeField] private Color textColor = new Color(1f,1f,1f,0.92f);

        private readonly Dictionary<EssenceDefinition, TMP_Text> _labels = new();
        private EssenceInventory _inv;
        private float _nextRefresh;

        private void Awake()
        {
            if (economyManager == null)
            {
                economyManager = FindObjectOfType<EconomyManager>();
            }
            _inv = economyManager != null ? economyManager.Essences : null;
            EnsureContainer();
            BuildItems();
            RefreshAll();
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextRefresh)
            {
                RefreshAll();
                _nextRefresh = Time.unscaledTime + 0.25f; // light polling until inventory has events
            }
        }

        private void EnsureContainer()
        {
            if (container == null)
            {
                var go = new GameObject("EssenceHUD", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                container = go.GetComponent<RectTransform>();
            }

            // Ensure vertical list layout on the container
            var vertical = container.GetComponent<VerticalLayoutGroup>();
            if (vertical == null) vertical = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.spacing = rowSpacing;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = false;

            var fitter = container.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = container.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void BuildItems()
        {
            // clear existing
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }
            _labels.Clear();

            foreach (var essence in essencesToShow)
            {
                if (essence == null) continue;
                var row = new GameObject(essence.DisplayName, typeof(RectTransform));
                row.transform.SetParent(container, false);
                var rowRect = row.GetComponent<RectTransform>();
                var hl = row.AddComponent<HorizontalLayoutGroup>();
                hl.childAlignment = TextAnchor.MiddleLeft;
                hl.spacing = 6f;
                hl.childForceExpandHeight = false;
                hl.childForceExpandWidth = false;

                // Icon (optional)
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(row.transform, false);
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = essence.Icon;
                iconImage.color = Color.white;
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.sizeDelta = iconSize;

                // Label "Name: count"
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(row.transform, false);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = essence.DisplayName + ": 0";
                label.color = textColor;
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.MidlineLeft;

                _labels[essence] = label;
            }
        }

        private void RefreshAll()
        {
            if (_inv == null)
            {
                if (economyManager == null) return;
                _inv = economyManager.Essences;
                if (_inv == null) return;
            }
            foreach (var kvp in _labels)
            {
                int count = _inv.GetCount(kvp.Key);
                var ess = kvp.Key;
                kvp.Value.text = (ess != null ? ess.DisplayName : "Essence") + ": " + count.ToString();
            }
        }

        // Public API to set list at runtime (optional)
        public void SetEssences(List<EssenceDefinition> essences)
        {
            essencesToShow = essences ?? new List<EssenceDefinition>();
            BuildItems();
            RefreshAll();
        }
    }
}
