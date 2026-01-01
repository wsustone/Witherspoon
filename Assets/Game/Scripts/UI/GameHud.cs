using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Witherspoon.Game.Core;
using Witherspoon.Game.Data;

namespace Witherspoon.Game.UI
{
    public class GameHud : MonoBehaviour
    {
        [SerializeField] private GameSession session;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text livesText;
        [SerializeField] private TMP_Text escapesText;
        [SerializeField] private TMP_Text damageText;

        private void Awake()
        {
            if (session == null)
            {
                session = FindObjectOfType<GameSession>();
            }
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

        private void HandleStatsChanged(GameSession _)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            if (session == null) return;

            var gm = session.Mode;
            if (modeText != null)
            {
                string modeLabel = gm != null ? gm.DisplayName : "Custom";
                modeText.text = modeLabel;
            }

            if (livesText != null)
            {
                if (gm != null && gm.StartingLives > 0)
                {
                    livesText.text = $"Lives: {session.LivesRemaining}";
                    livesText.enabled = true;
                }
                else
                {
                    livesText.enabled = false;
                }
            }

            if (escapesText != null)
            {
                bool showEscapes = gm == null || gm.MaxEscapes > 0 || !gm.DefeatOnFirstLeak;
                escapesText.text = $"Escapes: {session.Escapes}";
                escapesText.enabled = showEscapes;
            }

            if (damageText != null)
            {
                bool showDamage = gm != null && gm.MaxDamage > 0f;
                damageText.text = $"Damage: {session.DamageAccumulated:0}";
                damageText.enabled = showDamage;
            }
        }
    }
}
