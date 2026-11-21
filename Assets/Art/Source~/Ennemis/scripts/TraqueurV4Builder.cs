// Traqueur v4 (version b, 2026-11-21) : import, matériaux validés (texture d'origine, ombres neutres, saturation 1, liseré froid),
// dents et intérieur de la bouche, vitrine et captures face / profil / dos / trois quarts / gros plan du visage.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TraqueurV4Builder
{
    const string FBX = "Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx";
    const string TEX = "Assets/Art/Textures/Ennemis";
    const string MAT = "Assets/Art/Materials/Ennemis";
    const string SCENE = "Assets/Art/Scenes/Vitrine_Traqueur_v4.unity";

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    static Material Toon(string nom, string baseMap, Color teinte)
    {
        string p = $"{MAT}/{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh = Shader.Find("ItHunts/Toon");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        if (baseMap != null) m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/{baseMap}.png"));
        m.SetColor("_BaseColor", teinte);
        m.SetColor("_ShadowColor", new Color(0.05f, 0.05f, 0.055f));       // ombres neutres (c)
        m.SetFloat("_LightSaturation", 1f);                                // saturation 1 (c)
        m.SetFloat("_Rim", 1); m.SetKeyword(new LocalKeyword(sh, "_RIM_ON"), true);   // liseré froid (d)
        m.SetColor("_RimColor", new Color(0.45f, 0.6f, 0.95f)); m.SetFloat("_RimIntensity", 0.55f); m.SetFloat("_RimWidth", 0.22f);
        EditorUtility.SetDirty(m);
        return m;
    }

    public static string Construit()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        foreach (var n in new[] { "Corps", "Tete" })
        {
            var tb = (TextureImporter)AssetImporter.GetAtPath($"{TEX}/T_Traqueur_v4_{n}_BaseColor.png"); tb.maxTextureSize = 2048; tb.SaveAndReimport();
            var tn = (TextureImporter)AssetImporter.GetAtPath($"{TEX}/T_Traqueur_v4_{n}_Normal.png"); tn.textureType = TextureImporterType.NormalMap; tn.maxTextureSize = 2048; tn.SaveAndReimport();
        }
        var mc = Toon("M_Traqueur_v4_Corps", "T_Traqueur_v4_Corps_BaseColor", Color.white);
        var mt = Toon("M_Traqueur_v4_Tete", "T_Traqueur_v4_Tete_BaseColor", Color.white);
        var mo = Toon("M_Traqueur_v4_Orbite", null, new Color(0.004f, 0.004f, 0.005f));
        mo.SetFloat("_Rim", 0); mo.SetKeyword(new LocalKeyword(mo.shader, "_RIM_ON"), false); mo.SetColor("_ShadowColor", Color.black);
        var md = Toon("M_Traqueur_v4_Dents", null, new Color(0.62f, 0.57f, 0.46f));
        md.SetColor("_ShadowColor", new Color(0.16f, 0.14f, 0.11f)); md.SetFloat("_RimIntensity", 0.2f);
        var mh = Toon("M_Traqueur_v4_Cheveux", null, new Color(0.02f, 0.021f, 0.026f));
        mh.SetColor("_RimColor", new Color(0.42f, 0.48f, 0.62f)); mh.SetFloat("_RimIntensity", 0.35f); mh.SetFloat("_Cull", 0);   // reflet gris-bleu, double face
        var mi = (ModelImporter)AssetImporter.GetAtPath(FBX);
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.importBlendShapes = true;
        mi.importAnimation = false;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        foreach (var (a, b) in new[] { ("M_Traqueur_v4_Corps", mc), ("M_Traqueur_v4_Tete", mt), ("M_Traqueur_v4_Orbite", mo), ("M_Traqueur_v4_Dents", md), ("M_Traqueur_v4_Cheveux", mh) })
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), a), b);
        mi.SaveAndReimport();
        // vitrine : sol gris, fond gris clair (comme les vidéos), lumière douce et liseré
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.32f, 0.32f, 0.34f);
        var sol = GameObject.CreatePrimitive(PrimitiveType.Plane); sol.name = "Sol"; sol.transform.localScale = Vector3.one * 6;
        var ms = new Material(Shader.Find("ItHunts/Toon")); ms.SetColor("_BaseColor", new Color(0.55f, 0.55f, 0.56f)); sol.GetComponent<Renderer>().sharedMaterial = ms;
        var key = new GameObject("Lumiere").AddComponent<Light>(); key.type = LightType.Directional; key.intensity = 1.1f;
        key.transform.rotation = Quaternion.Euler(35, 155, 0); key.shadows = LightShadows.Soft;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FBX));
        go.name = "SK_Traqueur_v4";
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.78f, 0.78f, 0.77f); cam.fieldOfView = 30; cam.farClipPlane = 300;
        EditorSceneManager.SaveScene(sc, SCENE);
        AssetDatabase.SaveAssets();
        var smr = go.GetComponentsInChildren<SkinnedMeshRenderer>();
        return string.Join(" | ", smr.Select(r => r.name + " " + r.sharedMesh.triangles.Length / 3 + " tris, formes " + r.sharedMesh.blendShapeCount + ", os " + r.bones.Length));
    }

    // vue : face, profil, dos, visage, troisquarts ; forme : nom d'une forme à 100 (ou vide)
    public static string Capture(string vue, string forme, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        if (SceneManager.GetActiveScene().path != SCENE) EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var cam = Object.FindFirstObjectByType<Camera>();
        var go = GameObject.Find("SK_Traqueur_v4");
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            for (int i = 0; i < r.sharedMesh.blendShapeCount; i++)
                r.SetBlendShapeWeight(i, r.sharedMesh.GetBlendShapeName(i) == forme ? 100 : 0);
        Vector3 c = new Vector3(0, 7f, 0); float d = 34f; Vector3 dir = Vector3.forward;
        switch (vue)
        {
            case "face": dir = Vector3.forward; break;
            case "profil": dir = Vector3.right; break;
            case "dos": dir = Vector3.back; break;
            case "troisquarts": dir = new Vector3(0.7f, 0.15f, 1f).normalized; break;
            case "visage": dir = new Vector3(0.0f, 0.02f, 1f).normalized; c = new Vector3(0, 12.95f, 0); d = 6.0f; break;
        }
        cam.transform.position = c + dir * d;
        cam.transform.LookAt(c);
        // maillages cuits (les SkinnedMeshRenderer ne se recalculent pas en mode édition avant le rendu)
        var temp = new System.Collections.Generic.List<GameObject>();
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var m = new Mesh(); r.BakeMesh(m, true);
            var t = new GameObject("_cuit"); t.transform.SetPositionAndRotation(r.transform.position, r.transform.rotation);
            t.AddComponent<MeshFilter>().sharedMesh = m; t.AddComponent<MeshRenderer>().sharedMaterials = r.sharedMaterials;
            r.enabled = false; temp.Add(t);
        }
        int W = 1200, H = 1200;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        string p = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(p));
        File.WriteAllBytes(p, tex.EncodeToPNG());
        Object.DestroyImmediate(tex); rt.Release();
        foreach (var t in temp) Object.DestroyImmediate(t);
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>(true)) r.enabled = true;
        return sortie;
    }
}
