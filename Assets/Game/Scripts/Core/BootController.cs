using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Witherspoon.Game.Data;
using Witherspoon.Game.UI;

namespace Witherspoon.Game.Core
{
    /// <summary>
    /// Lives in the Boot scene. Waits for the player to pick a GameMode and then loads the gameplay scene.
    /// </summary>
    public class BootController : MonoBehaviour
    {
        [SerializeField] private string gameplaySceneName = "Gameplay";
        [SerializeField] private GameModeMenu modeMenu;
        [SerializeField] private bool autoShowMenu = true;

        private bool _loading;

        private void Awake()
        {
            if (modeMenu == null)
            {
                modeMenu = FindObjectOfType<GameModeMenu>(includeInactive: true);
            }

            if (modeMenu == null)
            {
                Debug.LogError("BootController could not find a GameModeMenu to subscribe to.", this);
                return;
            }

            modeMenu.OnModeSelected += HandleModeSelected;

            if (autoShowMenu)
            {
                modeMenu.ShowMenu(true);
            }
        }

        private void OnDestroy()
        {
            if (modeMenu != null)
            {
                modeMenu.OnModeSelected -= HandleModeSelected;
            }
        }

        private void HandleModeSelected(GameModeDefinition mode)
        {
            if (_loading) return;
            _loading = true;
            StartCoroutine(LoadGameplayAsync());
        }

        private IEnumerator LoadGameplayAsync()
        {
            // Make sure time scale resumes before entering gameplay
            Time.timeScale = 1f;

            if (string.IsNullOrEmpty(gameplaySceneName))
            {
                Debug.LogError("BootController has no gameplay scene name configured.", this);
                yield break;
            }

            // Simple wait so UI can fade if desired
            yield return null;
            SceneManager.LoadScene(gameplaySceneName);
        }
    }
}
