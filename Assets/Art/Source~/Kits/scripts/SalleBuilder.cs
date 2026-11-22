// Salles d'exemple (lot 5, nuit du 2026-11-22) : assemble les pièces des kits d'après un plan JSON (écrit par
// salles.py), ajoute l'éclairage de l'acte, enregistre le prefab dans Assets/Art/Levels/Exemples/<Acte>/ et capture les vues.
// `unity cmd ... script run --file SalleBuilder.cs --entry SalleBuilder.<Construit|Capture> --args '["<plan.json>", "<dossier>"]'`
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class SalleBuilder
{
    static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    // le plan est volontairement simple (une entrée par ligne) : lecture par expressions régulières, sans JsonUtility
    // (qui ne lit pas les types d'un script exécuté par la CLI).
    //   P|prefab|x|y|z|rotY|échelle            pièce
    //   L|type|x|y|z|r|g|b|intensité|portée|rotX|rotY|angle   lumière (Point ou Spot)
    //   V|nom|x|y|z|cx|cy|cz                   vue (position, point visé)
    //   E|acte                                 éclairage d'ambiance (caves, chateau, jardin, arene)
    //   N|nom|acte                             nom de la salle et dossier
    static float F(string s) => float.Parse(s, CI);

    static GameObject Prefab(string nom)
    {
        foreach (var d in new[] { "Assets/Art/Prefabs" })
        {
            var g = AssetDatabase.FindAssets(nom + " t:Prefab", new[] { d })
                .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == nom);
            if (g != null) return AssetDatabase.LoadAssetAtPath<GameObject>(g);
        }
        return null;
    }

    static void Dossier(string p)
    {
        if (AssetDatabase.IsValidFolder(p)) return;
        var parent = Path.GetDirectoryName(p).Replace("\\", "/");
        Dossier(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(p));
    }

    public static void Ambiance(string acte)
    {
        Color amb, fog; float d0, d1, lune; Vector3 rot;
        switch (acte)
        {
            case "chateau": amb = new Color(0.07f, 0.075f, 0.10f); fog = new Color(0.016f, 0.018f, 0.03f); d0 = 14; d1 = 110; lune = 0.35f; rot = new Vector3(40, 30, 0); break;
            case "jardin": case "arene": amb = new Color(0.08f, 0.09f, 0.12f); fog = new Color(0.03f, 0.035f, 0.055f); d0 = 18; d1 = 140; lune = 0.6f; rot = new Vector3(28, 145, 0); break;
            default: amb = new Color(0.055f, 0.058f, 0.080f); fog = new Color(0.012f, 0.014f, 0.024f); d0 = 10; d1 = 85; lune = acte == "caves" ? 0.0f : 0.25f; rot = new Vector3(40, 30, 0); break;
        }
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = amb;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = d0; RenderSettings.fogEndDistance = d1; RenderSettings.skybox = null;
        if (lune > 0)
        {
            var l = new GameObject("Lune").AddComponent<Light>();
            l.type = LightType.Directional; l.color = new Color(0.55f, 0.66f, 1f); l.intensity = lune; l.shadows = LightShadows.Soft;
            l.transform.rotation = Quaternion.Euler(rot);
        }
        var vol = new GameObject("Post-process (bloom du jeu)").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PostProcess_TestMovement.asset");
    }

    // Construit toutes les salles d'un fichier de plans (blocs séparés par une ligne « --- ») dans une scène par acte.
    public static string Construit(string plan, string scene)
    {
        var blocs = File.ReadAllText(plan).Split(new[] { "\n---" }, StringSplitOptions.RemoveEmptyEntries);
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        string acteScene = null;
        var sb = new StringBuilder();
        float decal = 0;
        foreach (var bloc in blocs)
        {
            string nom = null, acte = null;
            var lignes = bloc.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")).ToList();
            foreach (var l in lignes) { var t = l.Split('|'); if (t[0] == "N") { nom = t[1]; acte = t[2]; } }
            if (nom == null) continue;
            if (acteScene == null) { acteScene = acte; }
            var root = new GameObject(nom);
            var manque = new List<string>();
            foreach (var l in lignes)
            {
                var t = l.Split('|');
                if (t[0] == "P")
                {
                    var pf = Prefab(t[1]);
                    if (pf == null) { manque.Add(t[1]); continue; }
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
                    g.transform.localPosition = new Vector3(F(t[2]), F(t[3]), F(t[4]));
                    g.transform.localRotation = Quaternion.Euler(0, F(t[5]), 0);
                    if (t.Length > 6) { var s = t[6].Split(','); g.transform.localScale = s.Length == 3 ? new Vector3(F(s[0]), F(s[1]), F(s[2])) : Vector3.one * F(s[0]); }
                }
                else if (t[0] == "S")
                {
                    // S|matériau|x|y|z|sx|sy|sz|rotY : bloc (sol, dalle, estrade) avec collision
                    var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    g.name = "Bloc_" + t[1].Replace("M_Bib_", "");
                    g.transform.SetParent(root.transform, false);
                    g.transform.localPosition = new Vector3(F(t[2]), F(t[3]), F(t[4]));
                    g.transform.localScale = new Vector3(F(t[5]), F(t[6]), F(t[7]));
                    if (t.Length > 8) g.transform.localRotation = Quaternion.Euler(0, F(t[8]), 0);
                    g.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.FindAssets(t[1] + " t:Material").Select(AssetDatabase.GUIDToAssetPath)
                        .Where(q => Path.GetFileNameWithoutExtension(q) == t[1]).Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault();
                    g.isStatic = true;
                }
                else if (t[0] == "L")
                {
                    var lg = new GameObject("Lumiere_" + t[1]);
                    lg.transform.SetParent(root.transform, false);
                    lg.transform.localPosition = new Vector3(F(t[2]), F(t[3]), F(t[4]));
                    var li = lg.AddComponent<Light>();
                    li.type = t[1] == "Spot" ? LightType.Spot : LightType.Point;
                    li.color = new Color(F(t[5]), F(t[6]), F(t[7]));
                    li.intensity = F(t[8]); li.range = F(t[9]);
                    li.shadows = LightShadows.Soft;
                    if (li.type == LightType.Spot) { lg.transform.localRotation = Quaternion.Euler(F(t[10]), F(t[11]), 0); li.spotAngle = F(t[12]); }
                }
            }
            Dossier($"Assets/Art/Levels/Exemples/{acte}");
            var pfPath = $"Assets/Art/Levels/Exemples/{acte}/{nom}.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, pfPath, InteractionMode.AutomatedAction);
            root.transform.position = new Vector3(decal, 0, 0);
            decal += 400;
            sb.Append(nom).Append(manque.Count > 0 ? $" [manque : {string.Join(", ", manque)}]" : "").Append(' ');
        }
        Ambiance(acteScene.ToLowerInvariant());
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor;
        cam.allowHDR = true; cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 500;
        EditorSceneManager.SaveScene(sc, scene);
        return "ok " + sb;
    }

    static void Shot(Camera cam, string sortie, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        Directory.CreateDirectory(Path.GetDirectoryName(sortie));
        File.WriteAllBytes(sortie, tex.EncodeToJPG(92));
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
    }

    public static string Capture(string plan, string scene, string dossier)
    {
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        bool asyncAvant = EditorSettings.asyncShaderCompilation;
        EditorSettings.asyncShaderCompilation = false;
        var cam = Object.FindFirstObjectByType<Camera>();
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) ps.Simulate(3f, true, true);
        var sb = new StringBuilder();
        string nom = null;
        foreach (var raw in File.ReadAllLines(plan))
        {
            var l = raw.Trim(); var t = l.Split('|');
            if (t[0] == "N") nom = t[1];
            if (t[0] != "V" || nom == null) continue;
            var root = GameObject.Find(nom).transform;
            cam.transform.position = root.TransformPoint(new Vector3(F(t[2]), F(t[3]), F(t[4])));
            cam.transform.LookAt(root.TransformPoint(new Vector3(F(t[5]), F(t[6]), F(t[7]))));
            Shot(cam, Path.Combine(dossier, $"{nom}_{t[1]}.jpg"), 1280, 720);
            sb.Append($"{nom}_{t[1]} ");
        }
        EditorSettings.asyncShaderCompilation = asyncAvant;
        return "ok " + sb;
    }
}
