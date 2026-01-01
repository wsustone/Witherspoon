using UnityEngine;
using UnityEngine.UI;

namespace Witherspoon.Game.UI
{
    /// <summary>
    /// Attach to a UI Button to quit the application (and exit Play Mode in the editor).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class QuitButton : MonoBehaviour
    {
        private void Awake()
        {
            var button = GetComponent<Button>();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
