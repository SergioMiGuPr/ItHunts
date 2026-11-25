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
            WireNight5Lot2();
            WireDay5();
            WireNight5Lot4();
            AssetDatabase.SaveAssets();
            Debug.Log("Illustrations des améliorations : " + icons + " branchées, " + missing + " manquantes ; cadres de rang dans HudSkin_Default.");
        }

        private const string UiJeu = "Assets/Art/UI/Jeu/Prefabs/";

        /// <summary>
        /// Nuit 5, lot 2 de la DA (PR #55, UI-HANDOFF § 10) : colonne d'améliorations du HUD, infobulle, panneaux d'offres du coffre et
        /// du marchand dans HudSkin_Default ; page Codex du menu principal et ses tables d'améliorations dans MenuSkin_Default.
        /// </summary>
        public static void WireNight5Lot2()
        {
            GameObject Prefab(string name)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(UiJeu + name + ".prefab");
                if (go == null)
                    Debug.LogWarning("Prefab de la DA manquant : " + UiJeu + name + ".prefab");
                return go;
            }

            var skin = AssetDatabase.LoadAssetAtPath<HudSkinSettings>("Assets/_Project/Settings/HudSkin_Default.asset");
            if (skin != null)
            {
                skin.upgradeColumnPrefab = Prefab("HUD_ColonneAmeliorations");
                skin.upgradeTooltipPrefab = Prefab("HUD_InfobulleAmelioration");
                skin.chestOffersPrefab = Prefab("Panneau_Offres_Coffre");
                skin.merchantOffersPrefab = Prefab("Panneau_Offres_Marchand");
                EditorUtility.SetDirty(skin);
            }

            var menu = AssetDatabase.LoadAssetAtPath<MenuSkinSettings>("Assets/_Project/Settings/MenuSkin_Default.asset");
            if (menu != null)
            {
                menu.codexPrefab = Prefab("Menu_Codex");
                var pools = new System.Collections.Generic.List<UpgradePool>();
                foreach (string n in new[] { "Default", "Renard", "Cameleon", "Koala", "Hibou" })
                {
                    var pool = AssetDatabase.LoadAssetAtPath<UpgradePool>("Assets/_Project/Settings/UpgradePool_" + n + ".asset");
                    if (pool != null)
                        pools.Add(pool);
                }

                menu.codexPools = pools.ToArray();
                EditorUtility.SetDirty(menu);
            }
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

        /// <summary>
        /// Jour 5, point 4 : contour discret de l'objet tenu (M_VFX_Prise_Contour) dans TelekinesisFeedback_Default ; sons de l'épée du
        /// koala dans GameAudio_Default (nuit 5, lot 4) : Sfx_Epee_Vide_01 à 03 dans swordSwings, Sfx_Epee_Impact_01 à 03 dans swordHits,
        /// cherchés par leur nom dans Assets/Art/Audio/Armes. Un son absent est sauté ; aucun son trouvé : le champ reste tel quel.
        /// Les anciens champs uniques (swordSwing, swordSwingHeavy, swordHit) restent en repli et ne sont plus remplis.
        /// </summary>
        public static void WireDay5()
        {
            var feedback = AssetDatabase.LoadAssetAtPath<TelekinesisFeedbackSettings>("Assets/_Project/Settings/TelekinesisFeedback_Default.asset");
            var outline = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Materiaux/M_VFX_Prise_Contour.mat");
            if (feedback != null && outline != null && feedback.heldOutline != outline)
            {
                feedback.heldOutline = outline;
                EditorUtility.SetDirty(feedback);
            }

            var audio = AssetDatabase.LoadAssetAtPath<GameAudioSettings>("Assets/_Project/Resources/GameAudio_Default.asset");
            if (audio == null)
                return;
            AudioClip[] Clips(string prefix)
            {
                var list = new System.Collections.Generic.List<AudioClip>();
                for (int i = 1; i <= 9; i++)
                {
                    var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Art/Audio/Armes/{prefix}_{i:00}.wav");
                    if (c != null)
                        list.Add(c);
                }

                return list.ToArray();
            }

            AudioClip[] swings = Clips("Sfx_Epee_Vide"), hits = Clips("Sfx_Epee_Impact");
            if (swings.Length > 0)
                audio.swordSwings = swings;
            if (hits.Length > 0)
                audio.swordHits = hits;
            EditorUtility.SetDirty(audio);
            Debug.Log($"Sons de l'épée du koala : {swings.Length} coups dans le vide, {hits.Length} impacts (Assets/Art/Audio/Armes).");
        }

        /// <summary>
        /// Nuit 5, lot 4 de la DA (PR #57, ART-HANDOFF « plus de rouge ») : indices sombres des annonces (goule, traqueur), contour du volant,
        /// trait de l'objet tenu et effets d'apparition des goules dans les réglages ; « Annonce du bond » de Goule.prefab sur
        /// M_VFX_Indice_Cercle et plus de sphère d'annonce dans Volant.prefab. Relançable sans risque.
        /// </summary>
        public static void WireNight5Lot4()
        {
            Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Materiaux/" + n + ".mat");
            GameObject P(string n) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Prefabs/" + n + ".prefab");
            Material circle = M("M_VFX_Indice_Cercle"), rect = M("M_VFX_Indice_Rect");

            var ghoul = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            if (ghoul != null)
            {
                ghoul.leapTelegraphMaterial = circle;
                EditorUtility.SetDirty(ghoul);
            }

            var flyer = AssetDatabase.LoadAssetAtPath<FlyerSettings>("Assets/_Project/Settings/Flyer_Default.asset");
            if (flyer != null)
            {
                flyer.contourAnnonce = M("M_VFX_Volant_Annonce");
                flyer.contourPrise = M("M_VFX_Prise_Contour");
                EditorUtility.SetDirty(flyer);
            }

            var stalker = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            if (stalker != null)
            {
                stalker.grabWarningMaterial = rect;
                stalker.backhandZoneMaterial = circle;
                stalker.grabColumn = false;
                EditorUtility.SetDirty(stalker);
            }

            var vfx = AssetDatabase.LoadAssetAtPath<GameVfxSettings>("Assets/_Project/Resources/GameVfx_Default.asset");
            if (vfx != null)
            {
                vfx.heldTrait = P("VFX_Trait_Prise");
                vfx.spawnHole = P("VFX_Apparition_Trou");
                vfx.spawnShadow = P("VFX_Apparition_Ombre");
                vfx.spawnGrate = P("VFX_Apparition_Grille");
                EditorUtility.SetDirty(vfx);
            }

            const string ghoulPrefab = "Assets/_Project/Prefabs/Niveau/Goule.prefab";
            GameObject g = circle != null ? PrefabUtility.LoadPrefabContents(ghoulPrefab) : null;
            if (g != null)
            {
                Transform t = g.transform.Find("Annonce du bond");
                if (t != null && t.TryGetComponent(out Renderer r))
                    r.sharedMaterial = circle;
                PrefabUtility.SaveAsPrefabAsset(g, ghoulPrefab);
                PrefabUtility.UnloadPrefabContents(g);
            }

            const string flyerPrefab = "Assets/_Project/Prefabs/Niveau/Volant.prefab";
            GameObject f = PrefabUtility.LoadPrefabContents(flyerPrefab);
            if (f != null)
            {
                Transform sphere = f.transform.Find("Annonce du piqué");
                if (sphere != null)
                    Object.DestroyImmediate(sphere.gameObject);
                PrefabUtility.SaveAsPrefabAsset(f, flyerPrefab);
                PrefabUtility.UnloadPrefabContents(f);
            }
        }
    }
}
