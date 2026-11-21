// Clips du traqueur v4 (2026-11-21) : import des FBX d'animation (armature seule) en rig générique, avatar copié du
// modèle SK_Traqueur_v4, nom du clip Traqueur_<Action>, boucle, root motion sur l'os Racine ; mesures (durée, vitesse
// moyenne de la racine) et captures de contrôle dans la vitrine.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TraqueurAnimBuilder
{
    const string MODELE = "Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx";
    const string DOSSIER = "Assets/Art/Animations/Traqueur_v4";
    const string SCENE = "Assets/Art/Scenes/Vitrine_Traqueur_v4.unity";
    const string RACINE = "Traqueur/Racine";     // nœud du root motion (chemin depuis la racine du modèle)

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    // nom ; boucle ; root motion (déplacement de Racine dans le clip)
    public static string Importe(string clipsCsv)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        var mi = (ModelImporter)AssetImporter.GetAtPath(MODELE);
        if (mi.motionNodeName != RACINE || mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            mi.animationType = ModelImporterAnimationType.Generic;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.motionNodeName = RACINE;
            mi.SaveAndReimport();
        }
        var avatar = AssetDatabase.LoadAllAssetsAtPath(MODELE).OfType<Avatar>().FirstOrDefault();
        var res = new List<string>();
        foreach (var item in clipsCsv.Split(';'))
        {
            var p = item.Split(',');
            string nom = p[0]; bool boucle = p[1] == "1"; bool root = p[2] == "1";
            string path = $"{DOSSIER}/{nom}.fbx";
            var ci = (ModelImporter)AssetImporter.GetAtPath(path);
            if (ci == null) { res.Add(nom + " : FBX absent"); continue; }
            ci.animationType = ModelImporterAnimationType.Generic;
            ci.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            ci.sourceAvatar = avatar;
            ci.motionNodeName = RACINE;
            ci.preserveHierarchy = true;               // garde le nœud « Traqueur » : mêmes chemins que le modèle
            ci.importAnimation = true;
            ci.animationCompression = ModelImporterAnimationCompression.Off;   // une clé par image, telles qu'exportées
            ci.resampleCurves = true;                  // rotations en quaternions (pas de retournement d'Euler)
            var src = ci.defaultClipAnimations;
            if (src.Length == 0) { ci.SaveAndReimport(); src = ci.defaultClipAnimations; }
            var c = src[0];
            c.name = nom;
            c.loopTime = boucle; c.loopPose = false;
            c.lockRootRotation = true; c.keepOriginalOrientation = true;
            c.lockRootHeightY = false; c.keepOriginalPositionY = true;
            c.lockRootPositionXZ = !root; c.keepOriginalPositionXZ = !root;
            ci.clipAnimations = new[] { c };
            ci.SaveAndReimport();
            var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(x => x.name == nom);
            if (clip == null) { res.Add(nom + " : clip introuvable"); continue; }
            var vit = clip.averageSpeed;
            res.Add($"{nom} : {clip.length:0.000} s, {clip.frameRate} i/s, boucle {clip.isLooping}, vitesse moyenne {vit.magnitude:0.00} m/s ({vit.x:0.00}, {vit.y:0.00}, {vit.z:0.00})");
        }
        AssetDatabase.SaveAssets();
        return string.Join(" | ", res);
    }

    // capture du modèle de la vitrine posé par un clip à l'instant t (s), vue de profil
    public static string Capture(string nom, float t, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        if (SceneManager.GetActiveScene().path != SCENE) EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var go = GameObject.Find("SK_Traqueur_v4");
        var clip = AssetDatabase.LoadAllAssetsAtPath($"{DOSSIER}/{nom}.fbx").OfType<AnimationClip>().First(x => x.name == nom);
        var root = go.transform.Find("Traqueur");
        clip.SampleAnimation(go, t);
        var cam = Object.FindFirstObjectByType<Camera>();
        var racine = go.GetComponentsInChildren<Transform>().First(x => x.name == "Racine");
        Vector3 c = new Vector3(0, 5.5f, racine.position.z);
        cam.transform.position = c + Vector3.right * 34f; cam.transform.LookAt(c);
        var temp = new List<GameObject>();
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var m = new Mesh(); r.BakeMesh(m, true);
            var tg = new GameObject("_cuit"); tg.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
            tg.AddComponent<MeshFilter>().sharedMesh = m; tg.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
            r.enabled = false; temp.Add(tg);
        }
        int W = 960, H = 540;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        Directory.CreateDirectory(Path.GetDirectoryName(sortie));
        File.WriteAllBytes(sortie, tex.EncodeToPNG());
        Object.DestroyImmediate(tex); rt.Release();
        foreach (var x in temp) Object.DestroyImmediate(x);
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.enabled = true;
        // remise au repos (la scène n'est pas sauvegardée)
        EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        return sortie;
    }
}
