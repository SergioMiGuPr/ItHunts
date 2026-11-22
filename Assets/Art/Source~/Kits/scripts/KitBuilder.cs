// Kits de décor des actes I à III (nuit du 2026-11-22) : import des FBX (matériaux de la bibliothèque remappés par nom),
// prefabs (collisions, lumières et feux des sources de lumière), scène vitrine et captures dans l'éclairage du jeu.
// `unity cmd ... script run --file KitBuilder.cs --entry KitBuilder.<Construit|Vitrine|Capture> --args '["KitCaves", …]'`
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class KitBuilder
{
    const string MATBIB = "Assets/Art/Materials/Bibliotheque";
    const string MATKIT = "Assets/Art/Materials/Kits";

    static void Dossier(string p)
    {
        if (AssetDatabase.IsValidFolder(p)) return;
        var parent = Path.GetDirectoryName(p).Replace("\\", "/");
        Dossier(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(p));
    }

    static Material Plat(string nom, Color c, Color? emission = null, bool doubleFace = false)
    {
        Dossier(MATKIT);
        var path = $"{MATKIT}/{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("ItHunts/Toon")); AssetDatabase.CreateAsset(m, path); }
        m.SetColor("_BaseColor", c);
        if (emission.HasValue)
        {
            m.SetFloat("_Emission", 1); m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission.Value);
            m.SetFloat("_EmissionThroughFog", 0.8f);
        }
        if (doubleFace) m.SetFloat("_Cull", 0);
        EditorUtility.SetDirty(m);
        return m;
    }

    // matériaux plats communs aux kits (les autres viennent de la bibliothèque du lot 1)
    static void MateriauxKit()
    {
        Plat("M_Kit_Cire", new Color(0.78f, 0.72f, 0.58f));
        Plat("M_Kit_Flamme", new Color(1f, 0.8f, 0.5f), new Color(4f, 1.6f, 0.45f), true);
        Plat("M_Kit_Toile", new Color(0.36f, 0.3f, 0.24f));
        Plat("M_Kit_Vitrail", new Color(0.12f, 0.14f, 0.2f), new Color(0.25f, 0.3f, 0.45f), true);
        Plat("M_Kit_Feuillage", new Color(0.10f, 0.13f, 0.09f), null, true);
        Plat("M_Kit_Toile_Peinte", new Color(0.16f, 0.12f, 0.12f));
        Plat("M_Kit_Lune", new Color(0.9f, 0.92f, 1f), new Color(2.2f, 2.3f, 2.6f));
        Plat("M_Kit_Os", new Color(0.81f, 0.77f, 0.65f));
        AssetDatabase.SaveAssets();
    }

    static bool EstLumiere(string nom) => nom.Contains("Torche") || nom.Contains("Applique") || nom.Contains("Chandelle") ||
                                          nom.Contains("Candelabre") || nom.Contains("Brasero") || nom.Contains("Chandelier") ||
                                          nom.Contains("Lanterne") || nom.Contains("Bougeoir");

    static bool EstPetit(Bounds b) => b.size.x < 2.5f && b.size.y < 3f && b.size.z < 2.5f;

    // ---------------------------------------------------------------- import et prefabs
    public static string Construit(string kit)
    {
        MateriauxKit();
        string MOD = $"Assets/Art/Models/{kit}", PRE = $"Assets/Art/Prefabs/{kit}";
        Dossier(PRE);
        var sb = new StringBuilder();
        var feu = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/VFX/Prefabs/VFX_Torche_Feu.prefab");
        foreach (var f in Directory.GetFiles(MOD, "*.fbx").Select(x => x.Replace("\\", "/")))
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(f);
            mi.globalScale = 1; mi.useFileScale = true;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.importCameras = false; mi.importLights = false;
            mi.isReadable = false;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.SaveAndReimport();
            mi.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            mi.SaveAndReimport();
            var nom = Path.GetFileNameWithoutExtension(f);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(f);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
            var inst = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            go.name = nom.Replace("SM_", "");
            // matériaux des kits en double face : les surfaces ouvertes (arches, parois, ponts) restent pleines quel que soit
            // l'ordre de leurs sommets (l'éclairage toon retourne la normale des faces arrière)
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                foreach (var m in r.sharedMaterials)
                    if (m != null && (m.name.StartsWith("M_Bib_") || m.name.StartsWith("M_Kit_")) && m.GetFloat("_Cull") != 0f)
                    { m.SetFloat("_Cull", 0f); EditorUtility.SetDirty(m); }
            // matériaux manquants : signalés
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                foreach (var m in r.sharedMaterials)
                    if (m == null || m.shader == null || m.shader.name != "ItHunts/Toon" && !m.shader.name.StartsWith("ItHunts"))
                        sb.Append($"[mat? {nom}:{(m ? m.name + "/" + m.shader.name : "null")}] ");
            // collisions : pièces d'architecture (maillage statique) ; petits objets : boîte
            var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            if (!EstLumiere(nom))
            {
                if (EstPetit(b))
                {
                    var bc = go.AddComponent<BoxCollider>();
                    bc.center = go.transform.InverseTransformPoint(b.center); bc.size = b.size;
                }
                else
                    foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                        if (!mf.GetComponent<MeshCollider>()) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = !t.name.StartsWith("Battant");
            }
            // sources de lumière : feu et lumière ponctuelle sur chaque repère « Feu* »
            foreach (var t in go.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Feu")).ToList())
            {
                bool grand = nom.Contains("Torche") || nom.Contains("Brasero") || nom.Contains("Lanterne");
                if (feu != null)
                {
                    var fx = (GameObject)PrefabUtility.InstantiatePrefab(feu, t);
                    fx.transform.localPosition = Vector3.zero;
                    fx.transform.localScale = Vector3.one * (grand ? (nom.Contains("Brasero") ? 1.6f : 1f) : 0.045f);
                }
                var feuIdx = t.name.Length > 4 ? int.Parse(t.name.Substring(4)) : 1;
                if (grand || feuIdx == 1)
                {
                    var l = new GameObject("Lumiere").AddComponent<Light>();
                    l.transform.SetParent(t, false);
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.6f, 0.32f);
                    // torches : ambiance intermédiaire du jeu (1.35 / 15.4 m, Ambiance_Intermediaire) ; bougies : petit halo chaud
                    l.intensity = grand ? (nom.Contains("Brasero") ? 1.8f : 1.35f) : 0.6f;
                    l.range = grand ? (nom.Contains("Brasero") ? 16f : 15.4f) : 5f;
                    l.shadows = grand ? LightShadows.Soft : LightShadows.None;
                    l.renderMode = LightRenderMode.Auto;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{nom.Replace("SM_", "")}.prefab");
            Object.DestroyImmediate(go);
            sb.Append(nom.Replace("SM_", "")).Append(' ');
        }
        AssetDatabase.SaveAssets();
        return "ok " + sb;
    }

    // ---------------------------------------------------------------- scène vitrine
    static void Eclairage(Color amb, Color brouillard, float debut, float fin, float lune, Vector3 luneRot)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = amb;
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = brouillard;
        RenderSettings.fogStartDistance = debut; RenderSettings.fogEndDistance = fin;
        RenderSettings.skybox = null;
        var l = new GameObject("Lune").AddComponent<Light>();
        l.type = LightType.Directional; l.color = new Color(0.55f, 0.66f, 1f); l.intensity = lune; l.shadows = LightShadows.Soft;
        l.transform.rotation = Quaternion.Euler(luneRot);
        var vol = new GameObject("Post-process (bloom du jeu)").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PostProcess_TestMovement.asset");
    }

    public static Camera Camera(Color fond)
    {
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fond;
        cam.allowHDR = true; cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400;
        cam.tag = "MainCamera";
        return cam;
    }

    // dispose toutes les pièces du kit en rangées (grandes au fond, petites devant), sol de dalles, torches entre
    public static string Vitrine(string kit, string solMat)
    {
        string PRE = $"Assets/Art/Prefabs/{kit}";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Eclairage(new Color(0.055f, 0.058f, 0.080f), new Color(0.012f, 0.014f, 0.024f), 10, 85, 0.25f, new Vector3(40, 30, 0));
        var root = new GameObject("Vitrine_" + kit).transform;
        var prefabs = Directory.GetFiles(PRE, "*.prefab").Select(p => AssetDatabase.LoadAssetAtPath<GameObject>(p.Replace("\\", "/"))).ToList();
        var items = new List<(GameObject pf, Bounds b)>();
        foreach (var pf in prefabs)
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            var mrs = g.GetComponentsInChildren<MeshRenderer>();
            var b = mrs.Length > 0 ? mrs[0].bounds : new Bounds();
            foreach (var r in mrs) b.Encapsulate(r.bounds);
            Object.DestroyImmediate(g);
            items.Add((pf, b));
        }
        items = items.OrderByDescending(i => i.b.size.y * i.b.size.x).ToList();
        float x = 0, z = 0, rowDepth = 0, maxW = 90;
        var sol = AssetDatabase.LoadAssetAtPath<Material>($"{MATBIB}/{solMat}.mat");
        foreach (var (pf, b) in items)
        {
            float w = Mathf.Max(b.size.x, 1.5f) + 3f;
            if (x + w > maxW) { x = 0; z -= rowDepth + 6f; rowDepth = 0; }
            var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, root);
            float lift = -b.min.y > 0.5f ? -b.min.y : 0f;   // pièces qui plongent (gouffre, pont, canal) : surélevées
            g.transform.position = new Vector3(x + w / 2 - b.center.x, lift, z - b.max.z);
            x += w;
            rowDepth = Mathf.Max(rowDepth, b.size.z);
        }
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Sol"; floor.transform.SetParent(root);
        floor.transform.position = new Vector3(maxW / 2, -0.5f, z / 2 - 10);
        floor.transform.localScale = new Vector3(maxW + 30, 1, -z + 50);
        floor.GetComponent<MeshRenderer>().sharedMaterial = sol;
        // torches du jeu, en quinconce sur la vitrine
        for (float tz = 5; tz > z - 20; tz -= 30)
            for (float tx = 0; tx <= maxW; tx += 30)
            {
                var l = new GameObject("Torche").AddComponent<Light>();
                l.transform.SetParent(root); l.transform.position = new Vector3(tx + (tz % 32 == 0 ? 9 : 0), 5f, tz);
                l.type = LightType.Point; l.color = new Color(1f, 0.6f, 0.32f); l.intensity = 1.35f; l.range = 15.4f; l.shadows = LightShadows.Soft;
            }
        Camera(RenderSettings.fogColor);
        EditorSceneManager.SaveScene(scene, $"Assets/Art/Scenes/Vitrine_{kit}.unity");
        return $"ok {items.Count} pièces, profondeur {-z:0}";
    }

    public static string Shot(Camera cam, string sortie, int w, int h)
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
        return sortie;
    }

    // une capture par pièce (cadrée sur ses bornes, 3/4 avant), dans la vitrine
    public static string Capture(string kit, string dossier)
    {
        EditorSceneManager.OpenScene($"Assets/Art/Scenes/Vitrine_{kit}.unity", OpenSceneMode.Single);
        bool asyncAvant = EditorSettings.asyncShaderCompilation;
        EditorSettings.asyncShaderCompilation = false;
        var cam = Object.FindFirstObjectByType<Camera>();
        var root = GameObject.Find("Vitrine_" + kit).transform;
        var sb = new StringBuilder();
        // seules la torche de capture, la lune et les lumières propres de la pièce éclairent (les torches de la vitrine
        // s'additionneraient)
        var torches = root.GetComponentsInChildren<Light>().Where(l => l.name == "Torche").ToList();
        foreach (var l in torches) l.enabled = false;
        var lum = new GameObject("Lumiere_Capture").AddComponent<Light>();
        lum.type = LightType.Point; lum.color = new Color(1f, 0.6f, 0.32f); lum.intensity = 1.35f; lum.shadows = LightShadows.Soft;
        // contre-jour froid (lampes froides du jeu, 0.75 / 0.82 / 1) : silhouette lisible
        var froid = new GameObject("Contre_jour").AddComponent<Light>();
        froid.type = LightType.Point; froid.color = new Color(0.75f, 0.82f, 1f); froid.intensity = 0.8f; froid.shadows = LightShadows.None;
        // les feux (particules) ne tournent pas hors Play Mode : on les simule avant chaque image
        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>()) { ps.Simulate(1.5f, true, true); }
        foreach (Transform t in root)
        {
            if (t.name == "Sol" || t.name == "Torche") continue;
            var mrs = t.GetComponentsInChildren<MeshRenderer>();
            if (mrs.Length == 0) continue;
            var b = mrs[0].bounds;
            foreach (var r in mrs) b.Encapsulate(r.bounds);
            // pièces qui plongent sous le sol (gouffre, piles du pont) : on cadre la partie au-dessus de -4 m
            if (b.max.y - b.min.y > 14f && b.min.y < 1f && t.name.Contains("Gouffre")) { var mx = b.max; var mn = b.min; mn.y = mx.y - 12f; b.SetMinMax(mn, mx); }
            float s = Mathf.Max(b.size.x, b.size.y * 1.1f, b.size.z);
            var dir = new Vector3(-0.55f, 0.35f, -1f).normalized;
            cam.transform.position = b.center + dir * (s * 1.15f + 1.2f);
            cam.transform.LookAt(b.center);
            // une torche du jeu près de la pièce, côté caméra (comme une torche murale voisine)
            lum.transform.position = b.center + new Vector3(-s * 0.5f - 1f, s * 0.35f + 1.5f, -s * 0.6f - 1f);
            lum.range = Mathf.Max(15.4f, s * 1.6f);
            froid.transform.position = b.center + new Vector3(s * 0.7f + 1f, s * 0.6f + 2f, s * 0.9f + 2f);
            froid.range = Mathf.Max(14f, s * 2.2f);
            Shot(cam, Path.Combine(dossier, t.name + ".jpg"), 480, 480);
            sb.Append(t.name).Append(' ');
        }
        Object.DestroyImmediate(lum);
        Object.DestroyImmediate(froid.gameObject);
        foreach (var l in torches) l.enabled = true;
        EditorSettings.asyncShaderCompilation = asyncAvant;
        return "ok " + sb;
    }
}
