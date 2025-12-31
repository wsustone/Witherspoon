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
        [SerializeField] private Vector2 itemSize = new Vector2(28, 28);
        [SerializeField] private float spacing = 8f;
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
            if (container != null) return;
            var go = new GameObject("EssenceHUD", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            container = go.GetComponent<RectTransform>();
            var horiz = go.AddComponent<HorizontalLayoutGroup>();
            horiz.childAlignment = TextAnchor.MiddleLeft;
            horiz.spacing = spacing;
            horiz.childForceExpandHeight = false;
            horiz.childForceExpandWidth = false;
            var fitter = go.AddComponent<ContentSizeFitter>();
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
                var item = new GameObject(essence.DisplayName, typeof(RectTransform));
                item.transform.SetParent(container, false);
                var itemRect = item.GetComponent<RectTransform>();
                itemRect.sizeDelta = itemSize;

                // Icon
                var iconGo = new GameObject("Icon", typeof(RectTransform));
                iconGo.transform.SetParent(item.transform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0f);
                iconRect.anchorMax = new Vector2(0f, 1f);
                iconRect.sizeDelta = new Vector2(itemSize.y, 0f);
                var iconImage = iconGo.AddComponent<Image>();
                iconImage.sprite = essence.Icon;
                iconImage.color = Color.white;

                // Label
                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(item.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.offsetMin = new Vector2(itemSize.y + 4f, 0f);
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = "0";
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
                kvp.Value.text = count.ToString();
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
