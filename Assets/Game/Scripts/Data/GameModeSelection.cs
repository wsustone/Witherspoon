using UnityEngine;

namespace Witherspoon.Game.Data
{
    /// <summary>
    /// Simple static holder for the player's selected GameMode between scenes.
    /// </summary>
    public static class GameModeSelection
    {
        public static GameModeDefinition SelectedMode { get; private set; }
        public static bool HasSelection => SelectedMode != null;

        public static void Set(GameModeDefinition mode)
        {
            SelectedMode = mode;
        }

        public static void Clear()
        {
            SelectedMode = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void ResetOnDomainReload()
        {
            SelectedMode = null;
        }
    }
}
