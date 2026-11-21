// Eau toon (lot F, 2026-11-21) : matériau, éclaboussure, scène d'essai Test_Eau, captures.
// `unity cmd ... script run --file EauBuilder.cs --entry EauBuilder.<Construit|Capture>` (éditeur du projet art).
using System.IO;
using System.Linq;
using ItHunts.Art.Eau;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class EauBuilder
{
    const string E = "Assets/Art/Eau";
    const string SCENE = "Assets/Art/Scenes/Test_Eau.unity";

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    static Material Mat(string chemin, string shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        var sh = Shader.Find(shader);
        if (m == null) { Directory.CreateDirectory(Path.GetDirectoryName(chemin)); m = new Material(sh); AssetDatabase.CreateAsset(m, chemin); }
        m.shader = sh;
        return m;
    }

    static Material Particule(string nom, string tex)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath($"{E}/Textures/{tex}.png");
        ti.alphaIsTransparency = true; ti.mipmapEnabled = true; ti.wrapMode = TextureWrapMode.Clamp; ti.SaveAndReimport();
        var m = Mat($"{E}/Materiaux/{nom}.mat", "Universal Render Pipeline/Particles/Unlit");
        m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{E}/Textures/{tex}.png"));
        m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    public static string Construit()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        var eau = Mat($"{E}/Materiaux/M_Eau_Toon.mat", "ItHunts/EauToon");
        eau.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Niveau1/texture_eau_egout_v01_tuile.png"));
        EditorUtility.SetDirty(eau);
        var mc = Particule("M_Eau_Eclaboussure", "T_Eau_Eclaboussure_4x4");
        var mo = Particule("M_Eau_Onde", "T_Eau_Onde");
        // éclaboussure : couronne (planche 4 x 4, 0,7 s) + onde horizontale qui s'élargit ; taille de base 1 m
        var root = new GameObject("VFX_Eau_Eclaboussure");
        var cour = new GameObject("Couronne"); cour.transform.SetParent(root.transform, false);
        var ps = cour.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main; m.duration = 0.8f; m.loop = false; m.startLifetime = 0.7f; m.startSpeed = 0; m.startSize = 1.6f; m.maxParticles = 2; m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var em = ps.emission; em.rateOverTime = 0; em.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
        var sh = ps.shape; sh.enabled = false;
        var ts = ps.textureSheetAnimation; ts.enabled = true; ts.numTilesX = 4; ts.numTilesY = 4;
        var r = cour.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = mc; r.renderMode = ParticleSystemRenderMode.VerticalBillboard; r.pivot = new Vector3(0, 0.45f, 0);
        var onde = new GameObject("Onde"); onde.transform.SetParent(root.transform, false); onde.transform.localPosition = new Vector3(0, 0.02f, 0);
        var po = onde.AddComponent<ParticleSystem>(); po.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m2 = po.main; m2.duration = 1.2f; m2.loop = false; m2.startLifetime = 1.1f; m2.startSpeed = 0; m2.startSize = 0.6f; m2.maxParticles = 3; m2.scalingMode = ParticleSystemScalingMode.Hierarchy;
        var em2 = po.emission; em2.rateOverTime = 0; em2.SetBursts(new[] { new ParticleSystem.Burst(0, 1), new ParticleSystem.Burst(0.25f, 1) });
        var sh2 = po.shape; sh2.enabled = false;
        var sz = po.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, 0.4f, 1, 3.2f));
        var col = po.colorOverLifetime; col.enabled = true; var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 0.6f), new GradientAlphaKey(0, 1) }); col.color = g;
        var r2 = onde.GetComponent<ParticleSystemRenderer>(); r2.sharedMaterial = mo; r2.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        Directory.CreateDirectory($"{E}/Prefabs");
        var pf = PrefabUtility.SaveAsPrefabAsset(root, $"{E}/Prefabs/VFX_Eau_Eclaboussure.prefab");
        Object.DestroyImmediate(root);

        // scène d'essai : canal d'égout (couloir de 14 m, canal de 6 m), piliers dans l'eau, caisse, deux torches
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.04f, 0.05f, 0.07f);
        Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/Niveau1/{n}.mat");
        GameObject Boite(string n, Vector3 c, Vector3 s, Material mat)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube); b.name = n; b.transform.position = c; b.transform.localScale = s;
            if (mat != null) b.GetComponent<Renderer>().sharedMaterial = mat; return b;
        }
        Boite("Fond_Canal", new Vector3(0, -1.6f, 0), new Vector3(6, 0.2f, 30), M("M_Egout_Monde") ?? M("M_Egout"));
        Boite("Pente_G", new Vector3(-2.4f, -1.35f, 0), new Vector3(1.4f, 1.0f, 30), M("M_Egout_Monde"));
        Boite("Quai_G", new Vector3(-5f, -0.5f, 0), new Vector3(4, 1, 30), M("M_Dalles_Monde"));
        Boite("Quai_D", new Vector3(5f, -0.5f, 0), new Vector3(4, 1, 30), M("M_Dalles_Monde"));
        Boite("Mur_G", new Vector3(-7.2f, 4, 0), new Vector3(0.4f, 10, 30), M("M_Pierre_Monde"));
        Boite("Mur_D", new Vector3(7.2f, 4, 0), new Vector3(0.4f, 10, 30), M("M_Pierre_Monde"));
        Boite("Pilier_1", new Vector3(-0.8f, -0.4f, 6), new Vector3(0.9f, 3, 0.9f), M("M_Pierre_Monde"));
        Boite("Pilier_2", new Vector3(1.2f, -0.4f, 11), new Vector3(0.9f, 3, 0.9f), M("M_Pierre_Monde"));
        var caisse = Boite("Caisse", new Vector3(0.6f, -0.35f, 4f), new Vector3(1, 1, 1), M("M_Caisse"));
        caisse.transform.rotation = Quaternion.Euler(8, 30, 5);
        var surf = GameObject.CreatePrimitive(PrimitiveType.Quad); surf.name = "Eau";
        surf.transform.position = new Vector3(0, -0.55f, 0); surf.transform.rotation = Quaternion.Euler(90, 0, 0); surf.transform.localScale = new Vector3(6, 30, 1);
        Object.DestroyImmediate(surf.GetComponent<Collider>());
        surf.GetComponent<Renderer>().sharedMaterial = eau;
        var trig = new GameObject("Eau_Declencheur"); trig.transform.position = new Vector3(0, -1.0f, 0);
        var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(6, 0.9f, 30);
        var ec = trig.AddComponent<EauEclaboussure>(); ec.eclaboussure = pf; ec.surface = -0.55f;
        foreach (var (n, p) in new[] { ("Torche_G", new Vector3(-6.8f, 1.6f, 7)), ("Torche_D", new Vector3(6.8f, 1.6f, 13)), ("Torche_Fond", new Vector3(0.5f, 1.8f, 19)) })
        {
            var l = new GameObject(n).AddComponent<Light>(); l.transform.position = p; l.type = LightType.Point;
            l.color = new Color(1f, 0.55f, 0.25f); l.intensity = 6f; l.range = 14f;
        }
        var spl = (GameObject)PrefabUtility.InstantiatePrefab(pf); spl.name = "Apercu_Eclaboussure"; spl.transform.position = new Vector3(-1.2f, -0.55f, 4.5f); spl.transform.localScale = Vector3.one * 1.6f;
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.transform.position = new Vector3(0.4f, 2.6f, -3f); cam.transform.rotation = Quaternion.Euler(22, 0, 0);
        cam.fieldOfView = 70; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var add = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        add.requiresDepthTexture = true; add.requiresColorTexture = true;
        EditorSceneManager.SaveScene(sc, SCENE);
        AssetDatabase.SaveAssets();
        return "fait";
    }

    public static string Capture(float tEclaboussure, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        if (SceneManager.GetActiveScene().path != SCENE) EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var cam = Object.FindFirstObjectByType<Camera>();
        var spl = GameObject.Find("Apercu_Eclaboussure");
        foreach (var p in spl.GetComponentsInChildren<ParticleSystem>()) { p.useAutoRandomSeed = false; p.Simulate(tEclaboussure, true, true, true); }
        int W = 1600, H = 900;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        string ps = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(ps));
        File.WriteAllBytes(ps, tex.EncodeToPNG());
        Object.DestroyImmediate(tex); rt.Release();
        return sortie;
    }
}
