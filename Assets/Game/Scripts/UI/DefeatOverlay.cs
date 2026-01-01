using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Witherspoon.Game.Core;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Lightweight defeat overlay that auto-builds its layout so you only need to drop the component in a canvas.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class DefeatOverlay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameSession session;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Appearance")]
        [SerializeField] private Color backdropColor = new(0f, 0f, 0f, 0.75f);
        [SerializeField] private Color panelColor = new(0.14f, 0.16f, 0.2f, 0.93f);
        [SerializeField] private Vector2 panelSize = new(520f, 320f);
        [SerializeField] private float panelCornerRadius = 12f;
        [SerializeField] private float spacing = 18f;
        [SerializeField] private float titleSize = 38f;
        [SerializeField] private float bodySize = 24f;
        [SerializeField] private float buttonSize = 24f;

        [Header("Content")]
        [SerializeField] private string titleText = "Defeated";
        [SerializeField] private string restartLabel = "Restart";
        [SerializeField] private string exitLabel = "Exit";
        [SerializeField] private bool showExitButton = true;

        private TMP_Text _title;
        private TMP_Text _reason;
        private Button _restartButton;
        private Button _exitButton;
        private Image _backdropImage;
        private RectTransform _panelRect;

        private void Awake()
        {
            if (session == null)
            {
                session = FindObjectOfType<GameSession>();
            }

            EnsureCanvasAndSorting();
            EnsureCanvasGroup();
            BuildLayoutIfNeeded();

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (session != null)
            {
                session.OnDefeat += HandleDefeat;
            }
        }

        private void OnDisable()
        {
            if (session != null)
            {
                session.OnDefeat -= HandleDefeat;
            }
        }

        private void HandleDefeat(GameSession _, string reason)
        {
            if (_title != null) _title.text = titleText;
            if (_reason != null) _reason.text = reason;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        public void OnRestartClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void OnExitClicked()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void EnsureCanvasAndSorting()
        {
            var canvas = GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 2000);
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void BuildLayoutIfNeeded()
        {
            if (_backdropImage == null)
            {
                var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
                backdropGo.transform.SetParent(transform, false);
                var rect = backdropGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                _backdropImage = backdropGo.AddComponent<Image>();
                _backdropImage.color = backdropColor;
            }

            if (_panelRect == null)
            {
                var panelGo = new GameObject("Panel", typeof(RectTransform));
                panelGo.transform.SetParent(transform, false);
                _panelRect = panelGo.GetComponent<RectTransform>();
                _panelRect.sizeDelta = panelSize;
                _panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                _panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                _panelRect.anchoredPosition = Vector2.zero;

                var panelImage = panelGo.AddComponent<Image>();
                panelImage.color = panelColor;
                panelImage.raycastTarget = true;
                if (panelCornerRadius > 0f)
                {
                    panelImage.sprite = BuildRoundedSprite(panelCornerRadius);
                    panelImage.type = Image.Type.Sliced;
                }

                var vLayout = panelGo.AddComponent<VerticalLayoutGroup>();
                vLayout.childAlignment = TextAnchor.MiddleCenter;
                vLayout.spacing = spacing;
                vLayout.padding = new RectOffset(24, 24, 32, 32);
                vLayout.childForceExpandHeight = false;
                vLayout.childForceExpandWidth = false;
            }

            if (_title == null)
            {
                _title = CreateTMPLabel("Title", titleSize, FontStyles.UpperCase | FontStyles.Bold);
                _title.text = titleText;
            }

            if (_reason == null)
            {
                _reason = CreateTMPLabel("Reason", bodySize, FontStyles.Normal);
                _reason.alignment = TextAlignmentOptions.Center;
                _reason.text = "Reason";
            }

            if (_restartButton == null)
            {
                _restartButton = CreateButton("RestartButton", restartLabel, OnRestartClicked);
            }

            if (showExitButton && _exitButton == null)
            {
                _exitButton = CreateButton("ExitButton", exitLabel, OnExitClicked);
            }
            else if (!showExitButton && _exitButton != null)
            {
                Destroy(_exitButton.gameObject);
                _exitButton = null;
            }
        }

        private TMP_Text CreateTMPLabel(string name, float size, FontStyles styles)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panelRect, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = styles;
            text.alignment = TextAlignmentOptions.Center;
            text.text = name;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panelRect, false);

            var buttonImage = go.AddComponent<Image>();
            buttonImage.color = new Color(0.28f, 0.36f, 0.5f, 1f);
            buttonImage.raycastTarget = true;
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(onClick);

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var labelText = textGo.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = buttonSize;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;

            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = panelSize.x * 0.7f;
            layoutElement.preferredHeight = 52f;

            return button;
        }

        private Sprite BuildRoundedSprite(float radius)
        {
            var tex = new Texture2D(8, 8, TextureFormat.ARGB32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < tex.width; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }
            tex.Apply();

            var rect = new Rect(0, 0, tex.width, tex.height);
            var pivot = new Vector2(0.5f, 0.5f);
            var border = new Vector4(radius, radius, radius, radius);
            return Sprite.Create(tex, rect, pivot, 100f, 0, SpriteMeshType.Tight, border);
        }
    }
}
