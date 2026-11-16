using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Play démarre toujours depuis MainMenu, quelle que soit la scène ouverte (EditorSceneManager.playModeStartScene). Réglage
    /// par poste (EditorPrefs), activé par défaut ; à couper dans le menu ItHunts pour lancer directement la scène ouverte.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayFromMainMenu
    {
        public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string Pref = "ItHunts.PlayFromMainMenu";
        private const string MenuPath = "ItHunts/Play démarre depuis MainMenu";

        static PlayFromMainMenu()
        {
            EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.ExitingEditMode)
                    Apply();
            };
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(Pref, true);
            set
            {
                EditorPrefs.SetBool(Pref, value);
                Apply();
            }
        }

        [MenuItem(MenuPath, false, 0)]
        private static void Toggle() => Enabled = !Enabled;

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, Enabled);
            return true;
        }

        public static void Apply()
        {
            SceneAsset menu = Enabled ? AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuScenePath) : null;
            if (Enabled && menu == null)
                Debug.LogWarning($"[ItHunts] {MenuScenePath} introuvable : Play démarre depuis la scène ouverte.");
            EditorSceneManager.playModeStartScene = menu;
        }
    }
}
