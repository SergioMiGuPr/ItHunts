using ItHunts.Settings;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Branche les illustrations de la DA (PR #48) : icône ICO_code sur chacune des 70 améliorations (C-P1 donne ICO_C_P1), cadres de
    /// rang carrés et en losange et masque des passifs dans HudSkin_Default. Relançable sans risque.
    /// </summary>
    public static class UpgradeArtSetup
    {
        private const string Icons = "Assets/Art/UI/Ameliorations/Icones/";
        private const string Frames = "Assets/Art/UI/Ameliorations/Cadres/";
        private static readonly string[] Ranks = { "Bronze", "Argent", "Or", "Diamant", "Mythique" };

        [MenuItem("ItHunts/Brancher les illustrations des améliorations")]
        public static void Run()
        {
            int icons = 0, missing = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:UpgradeDefinition", new[] { "Assets/_Project/Settings/Upgrades" }))
            {
                var u = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (u == null || string.IsNullOrEmpty(u.code))
                    continue;
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(Icons + "ICO_" + u.code.Replace('-', '_') + ".png");
                if (s == null)
                {
                    missing++;
                    Debug.LogWarning("Illustration manquante pour " + u.code + " (" + u.displayName + ")");
                    continue;
                }

                if (u.icon != s)
                {
                    u.icon = s;
                    EditorUtility.SetDirty(u);
                }

                icons++;
            }

            var skin = AssetDatabase.LoadAssetAtPath<HudSkinSettings>("Assets/_Project/Settings/HudSkin_Default.asset");
            if (skin != null)
            {
                skin.rankFrames = new Sprite[Ranks.Length];
                skin.passiveFrames = new Sprite[Ranks.Length];
                for (int i = 0; i < Ranks.Length; i++)
                {
                    skin.rankFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Frames + "Cadre_" + Ranks[i] + ".png");
                    skin.passiveFrames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Frames + "Cadre_Passif_" + Ranks[i] + ".png");
                }

                skin.passiveMask = AssetDatabase.LoadAssetAtPath<Sprite>(Frames + "Masque_Passif_Losange.png");
                EditorUtility.SetDirty(skin);
            }

            WireLot7();
            AssetDatabase.SaveAssets();
            Debug.Log("Illustrations des améliorations : " + icons + " branchées, " + missing + " manquantes ; cadres de rang dans HudSkin_Default.");
        }

        /// <summary>Lot 7 de la DA (PR #50) : contour du volant sur son prefab, cris d'apparition et « je t'ai vu » dans GameAudio_Default.</summary>
        private static void WireLot7()
        {
            const string flyer = "Assets/_Project/Prefabs/Niveau/Volant.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(flyer);
            if (root != null)
            {
                var c = root.GetComponent<ItHunts.Art.VFX.ContourDiscret>() ?? root.AddComponent<ItHunts.Art.VFX.ContourDiscret>();
                c.annonce = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Materiaux/M_VFX_Volant_Annonce.mat");
                c.prise = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Materiaux/M_VFX_Prise_Contour.mat");
                PrefabUtility.SaveAsPrefabAsset(root, flyer);
                PrefabUtility.UnloadPrefabContents(root);
            }

            var audio = AssetDatabase.LoadAssetAtPath<GameAudioSettings>("Assets/_Project/Resources/GameAudio_Default.asset");
            if (audio != null)
            {
                AudioClip A(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/Traqueur/" + n + ".wav");
                audio.stalkerAppearCries = new[] { A("Sfx_Traqueur_Cri_Apparition_01"), A("Sfx_Traqueur_Cri_Apparition_02") };
                audio.stalkerSpottedCries = new[] { A("Sfx_Traqueur_Cri_Vu_01"), A("Sfx_Traqueur_Cri_Vu_02") };
                EditorUtility.SetDirty(audio);
            }
        }
    }
}
