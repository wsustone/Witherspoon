using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Witherspoon.Game.Core;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Simple launch overlay that lists available GameModes and lets the player pick one before play begins.
    /// Drop this on a Canvas in the gameplay scene. Provide GameMode assets in the inspector.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class GameModeMenu : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameSession session;
        [SerializeField] private RectTransform listContainer;

        [Header("Mode Library")]
        [SerializeField] private List<GameModeDefinition> availableModes = new();

        [Header("Behavior")]
        [SerializeField] private bool showOnStart = true;
        [SerializeField] private bool pauseWhileVisible = true;
        [SerializeField] private bool hideOnSelect = true;

        [Header("Visuals")]
        [SerializeField] private Color optionNormal = new(0.22f, 0.26f, 0.33f, 0.95f);
        [SerializeField] private Color optionHover = new(0.32f, 0.36f, 0.47f, 0.95f);
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color descriptionColor = new(0.78f, 0.83f, 0.94f, 0.95f);
        [SerializeField] private float optionMinHeight = 110f;

        private CanvasGroup _canvasGroup;
        private readonly List<Button> _createdButtons = new();
        public event Action<GameModeDefinition> OnModeSelected;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (session == null)
            {
                session = FindObjectOfType<GameSession>();
            }

            EnsureContainer();
            BuildModeButtons();

            if (showOnStart)
            {
                ShowMenu(true);
            }
            else
            {
                ShowMenu(false);
            }
        }

        private void EnsureContainer()
        {
            if (listContainer != null) return;

            var go = new GameObject("ModeList", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(560f, 480f);
            rect.anchoredPosition = Vector2.zero;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            listContainer = rect;
        }

        private void BuildModeButtons()
        {
            foreach (var button in _createdButtons)
            {
                if (button != null)
                {
                    Destroy(button.gameObject);
                }
            }
            _createdButtons.Clear();

            if (availableModes == null || availableModes.Count == 0)
            {
                Debug.LogWarning("GameModeMenu has no available modes configured.", this);
                return;
            }

            foreach (var mode in availableModes)
            {
                if (mode == null) continue;
                var option = CreateModeOption(mode);
                _createdButtons.Add(option);
            }
        }

        private Button CreateModeOption(GameModeDefinition mode)
        {
            var row = new GameObject(mode.DisplayName ?? "Mode", typeof(RectTransform));
            row.transform.SetParent(listContainer, false);
            var rect = row.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(520f, optionMinHeight);

            var bg = row.AddComponent<Image>();
            bg.color = optionNormal;
            bg.raycastTarget = true;

            var button = row.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            button.colors = colors;
            button.onClick.AddListener(() => HandleModeSelected(mode));
            var trigger = row.AddComponent<EventTriggerHover>();
            trigger.Initialize(bg, optionNormal, optionHover);

            var layout = row.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 6f;
            layout.padding = new RectOffset(18, 18, 16, 16);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;

            var title = CreateLabel(row.transform, mode.DisplayName, titleColor, 30f, FontStyles.Bold);
            title.enableWordWrapping = true;
            var desc = CreateLabel(row.transform, mode.Description, descriptionColor, 20f, FontStyles.Normal);
            desc.enableWordWrapping = true;

            var layoutElement = row.AddComponent<LayoutElement>();
            layoutElement.minHeight = optionMinHeight;
            layoutElement.preferredWidth = 520f;

            return button;
        }

        private TMP_Text CreateLabel(Transform parent, string text, Color color, float fontSize, FontStyles style)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.raycastTarget = false;
            return tmp;
        }

        private void HandleModeSelected(GameModeDefinition mode)
        {
            if (session != null)
            {
                session.SetMode(mode);
            }
            GameModeSelection.Set(mode);
            OnModeSelected?.Invoke(mode);
            if (hideOnSelect)
            {
                ShowMenu(false);
            }
            else if (pauseWhileVisible)
            {
                Time.timeScale = 1f;
            }
        }

        public void ShowMenu(bool show)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = show ? 1f : 0f;
            _canvasGroup.interactable = show;
            _canvasGroup.blocksRaycasts = show;

            if (pauseWhileVisible)
            {
                Time.timeScale = show ? 0f : 1f;
            }
        }

        /// <summary>
        /// Helper component to tint buttons on hover without needing a custom sprite.
        /// </summary>
        private class EventTriggerHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
        {
            private Image _target;
            private Color _normal;
            private Color _hover;

            public void Initialize(Image target, Color normal, Color hover)
            {
                _target = target;
                _normal = normal;
                _hover = hover;
            }

            public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (_target != null) _target.color = _hover;
            }

            public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
            {
                if (_target != null) _target.color = _normal;
            }
        }
    }
}
