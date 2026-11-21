// Armes du koala et du hibou (lot E, 2026-11-21) : import, matériaux, prefabs, vitrine en vue FPS et captures.
// `unity cmd ... script run --file ArmesBuilder.cs --entry ArmesBuilder.<Construit|Capture>` (éditeur du projet art).
using System.IO;
using System.Linq;
using ItHunts.Art.FPS;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class ArmesBuilder
{
    const string MOD = "Assets/Art/Models/FPS";
    const string TEX = "Assets/Art/Textures/FPS";
    const string MAT = "Assets/Art/Materials/FPS";
    const string PRE = "Assets/Art/Prefabs/FPS";
    const string SCENE = "Assets/Art/Scenes/Vitrine_Armes.unity";
    // pose de repos de la main gauche (repère caméra, ART-HANDOFF § 5 bis)
    static readonly Vector3 MainG = new Vector3(-0.23f, -0.25f, 0.45f);
    static readonly Vector3 MainGRot = new Vector3(-10f, 12f, 0f);

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    static Material Mat(string nom, string shader)
    {
        Directory.CreateDirectory(MAT);
        string p = $"{MAT}/{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh = Shader.Find(shader);
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        return m;
    }

    static void Tex(string nom, bool normal = false, bool srgb = true)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath($"{TEX}/{nom}.png");
        ti.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        ti.sRGBTexture = srgb && !normal;
        ti.maxTextureSize = 2048;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
    }

    static void Modele(string nom, params (string, Material)[] remap)
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath($"{MOD}/{nom}.fbx");
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.importAnimation = false;
        mi.globalScale = 1f; mi.useFileScale = true;
        foreach (var (n, m) in remap)
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), m);
        mi.SaveAndReimport();
    }

    public static string Construit()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Tex("T_Epee_Koala_BaseColor"); Tex("T_Epee_Koala_Emission"); Tex("T_Epee_Koala_Normal", true);
        // épée : toon du projet, runes émissives vertes (masque = tons les plus clairs de la texture)
        var me = Mat("M_Epee_Koala", "ItHunts/Toon");
        me.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/T_Epee_Koala_BaseColor.png"));
        me.SetColor("_BaseColor", Color.white);
        me.SetFloat("_Emission", 1); me.SetKeyword(new LocalKeyword(me.shader, "_EMISSION"), true);
        me.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/T_Epee_Koala_Emission.png"));
        me.SetColor("_EmissionColor", new Color(0.35f, 2.2f, 0.75f) * 1.6f);
        me.SetFloat("_Rim", 1); me.SetKeyword(new LocalKeyword(me.shader, "_RIM_ON"), true);
        me.SetColor("_RimColor", new Color(0.25f, 0.9f, 0.45f)); me.SetFloat("_RimIntensity", 0.6f); me.SetFloat("_RimWidth", 0.22f);
        me.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(me);
        // livre : spectre bleu glacé (couverture sombre, pages lumineuses avec lignes, volutes)
        var mc = Mat("M_Livre_Couverture", "ItHunts/Spectre");
        mc.SetColor("_Couleur", new Color(0.18f, 0.36f, 0.7f)); mc.SetColor("_Bord", new Color(0.6f, 1.2f, 1.9f));
        mc.SetFloat("_Coeur", 0.32f); mc.SetFloat("_Lignes", 0); mc.SetVector("_Fremit", new Vector4(0, 2.3f, 0, 0));
        var mp = Mat("M_Livre_Pages", "ItHunts/Spectre");
        mp.SetColor("_Couleur", new Color(0.3f, 0.62f, 1.05f)); mp.SetColor("_Bord", new Color(0.8f, 1.5f, 2.2f));
        mp.SetFloat("_Coeur", 0.22f); mp.SetFloat("_Lignes", 22); mp.SetFloat("_LignesForce", 0.7f); mp.SetVector("_Fremit", new Vector4(0.007f, 2.3f, 0, 0));
        var mv = Mat("M_Livre_Volutes", "ItHunts/Spectre");
        mv.SetColor("_Couleur", new Color(0.3f, 0.65f, 1.1f)); mv.SetColor("_Bord", new Color(0.5f, 1.0f, 1.6f));
        mv.SetFloat("_Coeur", 0.1f); mv.SetFloat("_Lignes", 0); mv.SetVector("_Bruit", new Vector4(9, 1.1f, 0, 0)); mv.SetVector("_Defile", new Vector4(0, 0.6f, 0, 0));
        mv.SetVector("_Fremit", new Vector4(0.01f, 0.8f, 0, 0));
        foreach (var m in new[] { mc, mp, mv }) EditorUtility.SetDirty(m);
        Modele("SM_Epee_Koala", ("M_Epee_Koala", me));
        Modele("SM_Livre_Hibou", ("M_Livre_Couverture", mc), ("M_Livre_Pages", mp), ("M_Livre_Volutes", mv));
        Directory.CreateDirectory(PRE);
        // prefab de l'épée : modèle + petite lumière verte (lueur discrète)
        var ep = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{MOD}/SM_Epee_Koala.fbx"));
        PrefabUtility.UnpackPrefabInstance(ep, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        var root = new GameObject("Artefact_Epee_Koala");
        ep.transform.SetParent(root.transform, false);
        var lg = new GameObject("Lueur_Verte"); lg.transform.SetParent(root.transform, false); lg.transform.localPosition = new Vector3(0, 0.03f, 0.3f);
        var l = lg.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(0.35f, 1f, 0.5f); l.intensity = 0.35f; l.range = 0.9f; l.shadows = LightShadows.None;
        foreach (var r in root.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; }
        PrefabUtility.SaveAsPrefabAsset(root, $"{PRE}/Artefact_Epee_Koala.prefab");
        Object.DestroyImmediate(root);
        // prefab du livre : flotte (ArtefactFlottant) ; lumière bleu glacé
        var lv = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{MOD}/SM_Livre_Hibou.fbx"));
        PrefabUtility.UnpackPrefabInstance(lv, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        var rootL = new GameObject("Artefact_Livre_Hibou");
        lv.transform.SetParent(rootL.transform, false);
        var fl = lv.AddComponent<ArtefactFlottant>(); fl.repos = new Vector3(0.01f, 0.13f, 0.09f); fl.rotationRepos = new Vector3(-15f, 8f, 0); fl.echelle = 0.6f;
        var lb = new GameObject("Lueur_Bleue"); lb.transform.SetParent(lv.transform, false); lb.transform.localPosition = new Vector3(0, 0, -0.05f);
        var l2 = lb.AddComponent<Light>(); l2.type = LightType.Point; l2.color = new Color(0.5f, 0.85f, 1f); l2.intensity = 0.5f; l2.range = 1.2f; l2.shadows = LightShadows.None;
        foreach (var r in rootL.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; }
        PrefabUtility.SaveAsPrefabAsset(rootL, $"{PRE}/Artefact_Livre_Hibou.prefab");
        Object.DestroyImmediate(rootL);
        // vitrine en vue FPS : caméra FOV 60 (rendu des mains), bras gauches du koala et du hibou à la pose de repos
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.05f, 0.055f, 0.08f);
        var camGo = new GameObject("Camera_FPS"); var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.02f, 0.022f, 0.03f);
        var key = new GameObject("Lumiere_Torche"); key.transform.position = new Vector3(0.8f, 0.6f, 1.2f);
        var kl = key.AddComponent<Light>(); kl.type = LightType.Point; kl.color = new Color(0.62f, 0.72f, 1f); kl.intensity = 1.6f; kl.range = 4f;
        Bras(camGo.transform, "SK_BrasFPS_Koala_G", "Artefact_Epee_Koala", "Koala");
        Bras(camGo.transform, "SK_BrasFPS_Hibou_G", "Artefact_Livre_Hibou", "Hibou");
        EditorSceneManager.SaveScene(sc, SCENE);
        AssetDatabase.SaveAssets();
        return "fait";
    }

    static void Bras(Transform cam, string bras, string artefact, string nom)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{MOD}/{bras}.fbx");
        var b = (GameObject)PrefabUtility.InstantiatePrefab(pf, cam);
        b.name = "Bras_" + nom;
        // le pivot est au coude ; Socket_Poignet (0, 0, 0.27) doit tomber sur la main de repos
        var rot = Quaternion.Euler(MainGRot);
        b.transform.localRotation = rot;
        b.transform.localPosition = MainG - rot * new Vector3(0, 0, 0.27f);
        var socket = b.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Socket_Artefact") ?? b.transform;
        var a = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{PRE}/{artefact}.prefab"), socket);
        a.transform.localPosition = Vector3.zero; a.transform.localRotation = Quaternion.identity;
        b.SetActive(false);
    }

    public static string Capture(string nom, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        if (SceneManager.GetActiveScene().path != SCENE) EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var cam = Object.FindFirstObjectByType<Camera>();
        foreach (Transform t in cam.transform) t.gameObject.SetActive(t.name == "Bras_" + nom);
        int W = 1600, H = 900;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        string p = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(p));
        File.WriteAllBytes(p, tex.EncodeToPNG());
        Object.DestroyImmediate(tex); rt.Release();
        return sortie;
    }
}
