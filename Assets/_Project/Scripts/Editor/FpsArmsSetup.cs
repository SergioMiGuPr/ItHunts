using System.Collections.Generic;
using System.Linq;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Bras FPS de la DA (lot du 2026-11-16) : cuit les poses (clips BrasFPS|Pose_* de SK_BrasFPS_Renard_D et _G : mêmes os et
    /// mêmes clips pour les 4 personnages) dans FpsArmPoses, donne leurs bras au renard et au caméléon, et pose FpsArms sur le
    /// prefab du joueur. Idempotent.
    /// </summary>
    public static class FpsArmsSetup
    {
        public const string PosesPath = "Assets/_Project/Settings/FpsArmPoses.asset";
        private const string Fps = "Assets/Art/Models/FPS/";

        [MenuItem("ItHunts/Vue FPS : bras de la DA (poses, personnages, prefab du joueur)")]
        public static void Menu() => Debug.Log(Setup());

        public static string Setup()
        {
            var set = AssetDatabase.LoadAssetAtPath<FpsArmPoseSet>(PosesPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<FpsArmPoseSet>();
                AssetDatabase.CreateAsset(set, PosesPath);
            }

            string r = Bake(Fps + "SK_BrasFPS_Renard_D.fbx", set.right) + " ; " + Bake(Fps + "SK_BrasFPS_Renard_G.fbx", set.left);
            EditorUtility.SetDirty(set);

            int chars = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterDefinition"))
            {
                var c = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                // Les 4 personnages (koala et hibou : bras livrés avec la PR #8, même armature, même cuisson des poses).
                string id = c.id.ToLowerInvariant();
                string who = id.Contains("renard") ? "Renard" : id.Contains("cam") ? "Cameleon" : id.Contains("koala") ? "Koala" : id.Contains("hibou") ? "Hibou" : null;
                if (who == null || AssetDatabase.LoadAssetAtPath<GameObject>(Fps + $"SK_BrasFPS_{who}_D.fbx") == null)
                    continue;
                c.fpsArmRight = AssetDatabase.LoadAssetAtPath<GameObject>(Fps + $"SK_BrasFPS_{who}_D.fbx");
                c.fpsArmLeft = AssetDatabase.LoadAssetAtPath<GameObject>(Fps + $"SK_BrasFPS_{who}_G.fbx");
                EditorUtility.SetDirty(c);
                chars++;
            }

            const string playerPath = "Assets/_Project/Prefabs/Player.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(playerPath);
            try
            {
                var view = root.GetComponentInChildren<FirstPersonView>(true);
                var arms = view.GetComponent<FpsArms>();
                if (arms == null)
                    arms = view.gameObject.AddComponent<FpsArms>();
                var so = new SerializedObject(arms);
                var vso = new SerializedObject(view);
                so.FindProperty("view").objectReferenceValue = view;
                so.FindProperty("poses").objectReferenceValue = set;
                so.FindProperty("rightHand").objectReferenceValue = vso.FindProperty("rightHand").objectReferenceValue;
                so.FindProperty("leftHand").objectReferenceValue = vso.FindProperty("leftHand").objectReferenceValue;
                so.FindProperty("outline").objectReferenceValue = root.GetComponentInChildren<HandsOutline>(true);
                so.ApplyModifiedPropertiesWithoutUndo();
                BarrelWithoutCanon(root);
                PrefabUtility.SaveAsPrefabAsset(root, playerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            return $"poses : {r} ; {chars} personnages avec bras ; FpsArms sur le joueur";
        }

        /// <summary>
        /// Barillet sans canon (retours du lot, point 6) : seul le barillet flotte dans la main ; le canon greybox est retiré et la
        /// bouche (départ de la traînée) se pose sur la face avant de la chambre du haut, la chambre active (le barillet tourne
        /// sous elle). Le tir reste sous le viseur (il part de la caméra). Renvoie la position locale de la bouche.
        /// </summary>
        public static Vector3 BarrelWithoutCanon(GameObject player)
        {
            var view = player.GetComponent<ItHunts.Artifacts.BarrelView>();
            if (view == null)
                return Vector3.zero;
            var so = new SerializedObject(view);
            var root = so.FindProperty("barrelRoot").objectReferenceValue as Transform;
            var muzzle = so.FindProperty("muzzle").objectReferenceValue as Transform;
            if (root == null)
                return Vector3.zero;
            Transform canon = root.Find("Canon");
            if (canon != null)
                Object.DestroyImmediate(canon.gameObject);
            if (muzzle == null)
                return Vector3.zero;
            // Chambre du haut au repos (SM_Barillet_Chambre_01) et avant du modèle (bornes du maillage, repère de la racine).
            Transform top = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("Chambre_01"));
            float front = 0f;
            foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null)
                    continue;
                Bounds b = mf.sharedMesh.bounds;
                Matrix4x4 m = root.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                    front = Mathf.Max(front, m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1))).z);
            }

            float y = top != null ? root.InverseTransformPoint(top.position).y : 0.031f;
            muzzle.localPosition = new Vector3(0f, y, front + 0.005f);
            muzzle.name = "Bouche (chambre active)";
            return muzzle.localPosition;
        }

        /// <summary>Rotation locale de chaque os pour chaque clip (Pose_Attraper : 3 clés).</summary>
        private static string Bake(string fbx, FpsArmPoseSet.Side side)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            GameObject inst = Object.Instantiate(model);
            inst.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Transform rig = inst.transform.Find("BrasFPS");
                List<Transform> bones = rig.GetComponentsInChildren<Transform>(true)
                    .Where(t => t != rig && !t.name.StartsWith("Socket_")).ToList();
                side.bones = bones.Select(b => b.name).ToArray();
                side.fromClips = clips.Any(c => !c.empty);
                side.poses.Clear();
                side.animations.Clear();
                foreach (AnimationClip clip in clips)
                {
                    // Animations de la DA (clips « Anim_* » : repos animé, gestes) : cuites à 30 images/s, jouées en boucle par FpsArms.
                    string clipName = clip.name.Contains("|") ? clip.name.Substring(clip.name.IndexOf('|') + 1) : clip.name;
                    if ((clipName.StartsWith("Anim_") || clipName.StartsWith("Boucle_")) && !clip.empty)
                    {
                        var anim = new FpsArmPoseSet.Animation { name = clipName, frameRate = 30f };
                        int n = Mathf.Max(1, Mathf.RoundToInt(clip.length * anim.frameRate));
                        for (int f = 0; f < n; f++)
                        {
                            clip.SampleAnimation(inst, f / anim.frameRate);
                            anim.frames.Add(new FpsArmPoseSet.Pose { name = $"{clipName}_{f}", rotations = bones.Select(b => b.localRotation).ToArray(), fromClip = true });
                        }

                        side.animations.Add(anim);
                        continue;
                    }

                    string name = clip.name.Contains("|") ? clip.name.Substring(clip.name.IndexOf('|') + 1) : clip.name;
                    float[] times = name == "Pose_Attraper" ? new[] { 0f, clip.length * 0.5f, clip.length } : new[] { 0f };
                    for (int k = 0; k < times.Length; k++)
                    {
                        clip.SampleAnimation(inst, times[k]);
                        side.poses.Add(new FpsArmPoseSet.Pose
                        {
                            name = times.Length > 1 ? $"{name}_{k}" : name,
                            rotations = bones.Select(b => b.localRotation).ToArray(),
                            fromClip = !clip.empty,
                        });
                    }
                }

                // Pose_Attraper : sa clé du milieu (main ouverte) sert aussi de pose « Pose_Attraper ».
                return $"{System.IO.Path.GetFileNameWithoutExtension(fbx)} : {side.bones.Length} os, {side.poses.Count} poses{(side.fromClips ? "" : " (clips vides : repli)")}";
            }
            finally
            {
                Object.DestroyImmediate(inst);
            }
        }
    }
}
