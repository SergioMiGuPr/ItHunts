using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Contrôleurs d'animation des ennemis de la DA (PR #6) : un état par clip, sans transitions (le code choisit le clip et fait
    /// le fondu, Ghoul.Animate et Stalker.Animate). Traqueur : calque « Mâchoire » (Traqueur_Machoire en boucle) avec un masque
    /// limité à l'os Machoire. Idempotent : les contrôleurs sont refaits à chaque appel.
    /// </summary>
    public static class EnemyAnimationSetup
    {
        public const string Dir = "Assets/_Project/Animation";
        public const string GhoulController = Dir + "/Goule.controller";
        public const string StalkerController = Dir + "/Traqueur.controller";
        public const string GhoulModel = "Assets/Art/Models/Ennemis/SK_Goule.fbx";
        /// <summary>Traqueur v4 (PR #27, D-080) : le modèle ; ses clips Traqueur_&lt;Action&gt; arrivent par les PR art-traqueur-anim.</summary>
        public const string StalkerModel = "Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx";
        public const string StalkerModelV3 = "Assets/Art/Models/Ennemis/SK_Traqueur.fbx";

        [MenuItem("ItHunts/Animations : contrôleurs de la goule et du traqueur")]
        public static void Menu() => Debug.Log(Build());

        public static string Build()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/_Project", "Animation");
            string a = BuildController(GhoulController, GhoulModel, "Goule_Repos", null);
            string b = BuildController(StalkerController, StalkerModel, "Traqueur_Repos", "Traqueur_Machoire");
            AssetDatabase.SaveAssets();
            return a + " ; " + b;
        }

        public static AnimationClip[] Clips(string model)
        {
            if (model != StalkerModel)
                return AssetDatabase.LoadAllAssetsAtPath(model).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
            // Traqueur v4 : clips Traqueur_* du modèle et de tous les fichiers d'animation de la DA (hors ancien modèle v3).
            var found = new Dictionary<string, AnimationClip>();
            foreach (string guid in AssetDatabase.FindAssets("Traqueur", new[] { "Assets/Art" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path == StalkerModelV3 || !(path.EndsWith(".fbx") || path.EndsWith(".anim") || path.EndsWith(".FBX")))
                    continue;
                foreach (AnimationClip c in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                    if (!c.name.StartsWith("__preview") && Short(c).StartsWith("Traqueur_"))
                        found[Short(c)] = c;
            }

            return found.Values.ToArray();
        }

        public static string Short(AnimationClip c) => c.name.Contains("|") ? c.name.Substring(c.name.IndexOf('|') + 1) : c.name;

        private static string BuildController(string path, string model, string defaultState, string jawClip)
        {
            AssetDatabase.DeleteAsset(path);
            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
            int n = 0;
            foreach (AnimationClip clip in Clips(model).Where(c => Short(c) != jawClip))
            {
                AnimatorState st = sm.AddState(Short(clip));
                st.motion = clip;
                if (Short(clip) == defaultState)
                    sm.defaultState = st;
                n++;
            }

            if (jawClip != null)
            {
                AnimationClip jaw = Clips(model).FirstOrDefault(c => Short(c) == jawClip);
                var mask = new AvatarMask { name = "Masque : mâchoire" };
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(model);
                Transform bone = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Machoire");
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                {
                    if (t == go.transform)
                        continue;
                    string p = AnimationUtility.CalculateTransformPath(t, go.transform);
                    mask.AddTransformPath(t, false);
                    mask.SetTransformActive(mask.transformCount - 1, bone != null && (t == bone || t.IsChildOf(bone)));
                    _ = p;
                }

                AssetDatabase.AddObjectToAsset(mask, ctrl);
                ctrl.AddLayer("Mâchoire");
                AnimatorControllerLayer[] layers = ctrl.layers;
                layers[1].defaultWeight = 1f;
                layers[1].avatarMask = mask;
                layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
                AnimatorState js = layers[1].stateMachine.AddState(jawClip);
                js.motion = jaw;
                layers[1].stateMachine.defaultState = js;
                ctrl.layers = layers;
            }

            EditorUtility.SetDirty(ctrl);
            return $"{System.IO.Path.GetFileNameWithoutExtension(path)} : {n} états" + (jawClip != null ? " + calque Mâchoire" : "");
        }

        /// <summary>Animator sur le modèle (racine de l'instance du FBX) : contrôleur, sans racine qui bouge, mis à jour hors champ réduit.</summary>
        public static Animator AddAnimator(GameObject modelRoot, string controllerPath)
        {
            var anim = modelRoot.GetComponent<Animator>();
            if (anim == null)
                anim = modelRoot.AddComponent<Animator>();
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            string model = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(modelRoot);
            Avatar avatar = string.IsNullOrEmpty(model) ? null : AssetDatabase.LoadAllAssetsAtPath(model).OfType<Avatar>().FirstOrDefault();
            if (avatar != null)
                anim.avatar = avatar;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            return anim;
        }
    }
}
