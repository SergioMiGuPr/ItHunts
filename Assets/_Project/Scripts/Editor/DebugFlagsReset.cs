using ItHunts.Player;
using UnityEditor;

namespace ItHunts.EditorTools
{
    /// <summary>Remet les drapeaux de mesure (invulnérabilité de debug) à zéro à la sortie du Play Mode.</summary>
    [InitializeOnLoad]
    public static class DebugFlagsReset
    {
        static DebugFlagsReset()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode || change == PlayModeStateChange.ExitingPlayMode)
                PlayerHealth.ResetDebugFlags();
        }
    }
}
