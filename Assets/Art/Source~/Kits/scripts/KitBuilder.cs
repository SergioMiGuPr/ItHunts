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
        Plat("M_Kit_Vitrail", new Color(0.026f, 0.029f, 0.036f), new Color(0.02f, 0.024f, 0.036f), true);   // vitraux : verre vieilli aux teintes sourdes (nuit 4, lot 3 : écarts de teinte faibles, le toon les accentue)
        Plat("M_Kit_Feuillage", new Color(0.10f, 0.13f, 0.09f), null, true);
        Plat("M_Kit_Toile_Peinte", new Color(0.16f, 0.12f, 0.12f));
        Plat("M_Kit_Lune", new Color(0.9f, 0.92f, 1f), new Color(2.2f, 2.3f, 2.6f));
        Plat("M_Kit_Os", new Color(0.81f, 0.77f, 0.65f));
        // nuit 4, lot 3 : vitraux sombres colorés, sceau d'âme (cyan des âmes, réservé au jeu), surface de collision seule
        Plat("M_Kit_Vitrail_Rouge", new Color(0.036f, 0.024f, 0.026f), new Color(0.03f, 0.014f, 0.014f), true);
        Plat("M_Kit_Vitrail_Ambre", new Color(0.036f, 0.031f, 0.024f), new Color(0.03f, 0.022f, 0.012f), true);
        Plat("M_Kit_Sceau_Ame", new Color(0.3f, 0.9f, 1f), new Color(0.4f, 2.6f, 3.2f));
        Plat("M_Collision_Invisible", new Color(1f, 0f, 1f));
        // relecture du lot 3 : gouttes du lustre à pampilles (cristal clair, léger reflet froid)
        Plat("M_Kit_Cristal", new Color(0.62f, 0.68f, 0.76f), new Color(0.06f, 0.07f, 0.09f), true);
        // vitraux : la lumière des torches ne les sature pas (verre épais et sale, vu de l'intérieur la nuit)
        foreach (var v in new[] { "M_Kit_Vitrail", "M_Kit_Vitrail_Rouge", "M_Kit_Vitrail_Ambre" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{MATKIT}/{v}.mat");
            m.SetFloat("_LightSaturation", 0.4f); m.SetFloat("_AmbientInfluence", 0.4f); EditorUtility.SetDirty(m);
        }
        AssetDatabase.SaveAssets();
    }

    static bool EstLumiere(string nom) => nom.Contains("Torche") || nom.Contains("Applique") || nom.Contains("Chandelle") ||
                                          nom.Contains("Candelabre") || nom.Contains("Brasero") || nom.Contains("Chandelier") ||
                                          nom.Contains("Lanterne") || nom.Contains("Bougeoir") || nom.Contains("Lustre");

    // nuit 4, lot 3 : parties mobiles (pivots à l'axe, voir INTEGRATION-PROPS « Nuit 4, lot 3 »), jamais statiques
    static bool EstMobile(string n) => n.StartsWith("Battant") || n.StartsWith("Vantail") || n.StartsWith("Serrure") ||
                                       n.StartsWith("Herse") || n.StartsWith("Trappe");

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
            mi.isReadable = true;   // nuit 4 : le NavMesh est construit au chargement (lot 2 : Read/Write coché sur tous les FBX des kits)
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
            // nuit 4, lot 3 : la serrure de la porte verrouillée suit le vantail (exportée à côté de lui, rangée dessous ici)
            var serrure = go.transform.Find("Serrure"); var vantail = go.transform.Find("Vantail");
            if (serrure && vantail) serrure.SetParent(vantail, true);
            // nuit 4, lot 3 : « Col_* » = collision seule (rampes lisses sous les marches) : rendu retiré, MeshCollider
            foreach (var t in go.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Col_")).ToList())
            {
                var mr = t.GetComponent<MeshRenderer>();
                if (mr) Object.DestroyImmediate(mr);
                var mfc = t.GetComponent<MeshFilter>();
                if (mfc && !t.GetComponent<MeshCollider>()) t.gameObject.AddComponent<MeshCollider>().sharedMesh = mfc.sharedMesh;
            }
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
                        if (!mf.GetComponent<MeshCollider>() && !mf.name.StartsWith("Deco_")) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                foreach (var t in go.GetComponentsInChildren<Transform>())
                    t.gameObject.isStatic = !t.GetComponentsInParent<Transform>(true).Any(p => EstMobile(p.name));
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
                // « Feu », « Feu_3 » ou « Feu_Torche » (relecture du lot 3 : repère nommé des torches du château)
                var feuIdx = t.name.Length > 4 && int.TryParse(t.name.Substring(4), out var fi) ? fi : 1;
                if (grand || feuIdx == 1)
                {
                    var l = new GameObject("Lumiere").AddComponent<Light>();
                    l.transform.SetParent(t, false);
                    l.type = LightType.Point;
                    l.color = new Color(1f, 0.6f, 0.32f);
                    // torches : ambiance intermédiaire du jeu (1.35 / 15.4 m, Ambiance_Intermediaire) ; bougies : petit halo chaud
                    bool lustre = nom.Contains("Chandelier") || nom.Contains("Lustre");
                    l.intensity = grand ? (nom.Contains("Brasero") ? 1.8f : 1.35f) : lustre ? (nom.Contains("Grand") ? 2.4f : 1.6f) : 0.6f;
                    l.range = grand ? (nom.Contains("Brasero") ? 16f : 15.4f) : lustre ? (nom.Contains("Grand") ? 26f : 18f) : 5f;
                    if (lustre) l.transform.position = go.transform.position + new Vector3(0, t.position.y - go.transform.position.y - 0.3f, 0);
                    l.shadows = grand ? LightShadows.Soft : LightShadows.None;
                    l.renderMode = LightRenderMode.Auto;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{nom.Replace("SM_", "")}.prefab");
            Object.DestroyImmediate(go);
            sb.Append(nom.Replace("SM_", "")).Append(' ');
        }
        if (kit == "KitChateau") sb.Append(RayonLune());
        if (kit == "KitJardin") sb.Append(CielEtBrume());
        AssetDatabase.SaveAssets();
        return "ok " + sb;
    }

    // ---------------------------------------------------------------- rayon de lune et poussière
    static Texture2D Point()
    {
        Dossier("Assets/Art/Textures/Kits");
        const string p = "Assets/Art/Textures/Kits/T_Kit_Point.png";
        if (!File.Exists(p))
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32, 32)) / 32f;
                    float a = d < 0.45f ? 1f : d < 0.8f ? 0.45f : d < 1f ? 0.12f : 0f;   // disque en trois aplats
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            File.WriteAllBytes(p, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    public static Material Lumiere(string nom, Color c, float intens, bool rayon, Texture2D tex)
    {
        Dossier(MATKIT);
        var path = $"{MATKIT}/{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Shader.Find("ItHunts/Lumiere")); AssetDatabase.CreateAsset(m, path); }
        m.shader = Shader.Find("ItHunts/Lumiere");
        m.SetColor("_Color", c); m.SetFloat("_Intensity", intens); m.SetFloat("_Rayon", rayon ? 1 : 0);
        if (tex) m.SetTexture("_MainTex", tex);
        m.SetFloat("_Steps", rayon ? 3 : 0);
        EditorUtility.SetDirty(m);
        return m;
    }

    // Poussière blanc-gris en suspension dans un volume (boîte locale « taille »), qui dérive lentement.
    public static ParticleSystem Poussiere(Transform parent, Vector3 centre, Vector3 taille, Quaternion rot, int nombre)
    {
        var go = new GameObject("Poussiere");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = centre; go.transform.localRotation = rot;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.82f, 0.82f, 0.8f, 0.9f), new Color(0.62f, 0.62f, 0.6f, 0.6f));
        main.maxParticles = nombre;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission; em.rateOverTime = nombre / 8f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = taille;
        var no = ps.noise; no.enabled = true; no.strength = 0.08f; no.frequency = 0.25f; no.scrollSpeed = 0.05f;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f); vel.y = new ParticleSystem.MinMaxCurve(-0.03f, 0.01f); vel.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.8f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Lumiere("M_Kit_Poussiere", new Color(0.85f, 0.85f, 0.82f), 1.2f, false, Point());
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return ps;
    }

    // Tache de brume : disque doux en paliers, bord bruité (128 px)
    static Texture2D Nuage()
    {
        Dossier("Assets/Art/Textures/Kits");
        const string p = "Assets/Art/Textures/Kits/T_Kit_Brume.png";
        if (!File.Exists(p))
        {
            var t = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 63.5f) / 64f, dy = (y - 63.5f) / 64f;
                    float ang = Mathf.Atan2(dy, dx);
                    float r = Mathf.Sqrt(dx * dx + dy * dy) * (1f + 0.18f * Mathf.Sin(ang * 5f) + 0.1f * Mathf.Sin(ang * 11f + 1.3f));
                    float a = Mathf.Clamp01(1f - r);
                    a = Mathf.Floor(a * a * 4f) / 4f;   // trois aplats
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            File.WriteAllBytes(p, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.alphaIsTransparency = true; ti.wrapMode = TextureWrapMode.Clamp; ti.mipmapEnabled = true;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    // Brume au sol : nappes basses qui dérivent (boîte de 40 × 40 m, sous 1.6 m), additif très faible gris-bleu.
    public static GameObject Brume(Transform parent, Vector3 taille, int nombre)
    {
        var go = new GameObject("Brume_Sol");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, taille.y * 0.5f, 0);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.startLifetime = new ParticleSystem.MinMaxCurve(18f, 28f);
        main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(6f, 12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.66f, 0.78f, 1f), new Color(0.45f, 0.5f, 0.6f, 1f));
        main.maxParticles = nombre; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = nombre / 22f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = taille;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(0.15f, 0.35f); vel.y = new ParticleSystem.MinMaxCurve(0f, 0f); vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.25f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        var m = Lumiere("M_Kit_Brume", new Color(0.5f, 0.55f, 0.66f), 0.09f, false, Nuage());
        r.sharedMaterial = m;
        r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return go;
    }

    static string CielEtBrume()
    {
        // ciel : sphère de 900 m vue de l'intérieur, lune dans la direction _LuneDir ; lumière de lune qui en vient
        var path = $"{MATKIT}/M_Kit_Ciel.mat";
        var mc = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mc == null) { mc = new Material(Shader.Find("ItHunts/Ciel")); AssetDatabase.CreateAsset(mc, path); }
        mc.shader = Shader.Find("ItHunts/Ciel");
        var luneDir = new Vector3(-0.35f, 0.42f, 0.84f).normalized;
        mc.SetVector("_LuneDir", luneDir);
        // nuit 4, lot 3 : la lune peinte du menu principal, halo et dégradé continus (plus de paliers)
        mc.SetTexture("_LuneTex", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Ciel/T_Ciel_Lune.png"));
        mc.SetFloat("_LuneTexOn", 1f); mc.SetFloat("_Paliers", 0f);
        EditorUtility.SetDirty(mc);
        var go = new GameObject("Jardin_Ciel");
        var sph = new GameObject("Voute_Celeste");
        sph.transform.SetParent(go.transform, false);
        sph.transform.localScale = Vector3.one * 900f;
        sph.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        var mr = sph.AddComponent<MeshRenderer>(); mr.sharedMaterial = mc;
        mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
        var l = new GameObject("Lune").AddComponent<Light>();
        l.transform.SetParent(go.transform, false);
        l.transform.rotation = Quaternion.LookRotation(-luneDir);
        l.type = LightType.Directional; l.color = new Color(0.6f, 0.7f, 1f); l.intensity = 0.6f; l.shadows = LightShadows.Soft;
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Art/Prefabs/KitJardin/Jardin_Ciel.prefab");
        Object.DestroyImmediate(go);
        var b = new GameObject("Jardin_Brume");
        Brume(b.transform, new Vector3(40, 1.2f, 40), 90);
        PrefabUtility.SaveAsPrefabAsset(b, "Assets/Art/Prefabs/KitJardin/Jardin_Brume.prefab");
        Object.DestroyImmediate(b);
        return "Jardin_Ciel Jardin_Brume ";
    }

    // Rayon de lune : volume en tronc de pyramide de la fenêtre (origine) vers le sol, projecteur de lune, poussière.
    static string RayonLune()
    {
        Dossier("Assets/Art/Models/KitChateau");
        float L = 18f;
        var dir = new Vector3(0, -0.82f, 0.57f).normalized;
        Vector3 bas = dir * L;
        // tronc de cône elliptique à 20 côtés : le fondu des bords (normale vue de biais) adoucit le volume sans arête
        float w0 = 1.5f, d0 = 0.4f, w1 = 2.8f, d1 = 2.2f;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>(); var nr = new List<Vector3>();
        int n = 20;
        for (int i = 0; i <= n; i++)
        {
            float a = 2 * Mathf.PI * i / n, c = Mathf.Cos(a), s = Mathf.Sin(a);
            var pt = new Vector3(c * w0, 0, s * d0);
            var pb = bas + new Vector3(c * w1, 0, s * d1);
            var nn = new Vector3(c / w1, 0, s / d1).normalized;
            v.Add(pt); v.Add(pb); nr.Add(nn); nr.Add(nn);
            uv.Add(new Vector2((float)i / n, 0)); uv.Add(new Vector2((float)i / n, 1));
        }
        for (int i = 0; i < n; i++)
        {
            int k = i * 2;
            tri.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
        }
        var mesh = new Mesh { name = "SM_Chateau_RayonLune" };
        mesh.SetVertices(v); mesh.SetUVs(0, uv); mesh.SetNormals(nr); mesh.SetTriangles(tri, 0); mesh.RecalculateBounds();
        const string mp = "Assets/Art/Models/KitChateau/SM_Chateau_RayonLune.asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
        if (old != null) { old.Clear(); EditorUtility.CopySerialized(mesh, old); mesh = old; } else AssetDatabase.CreateAsset(mesh, mp);
        var go = new GameObject("Chateau_RayonLune");
        var vol = new GameObject("Rayon");
        vol.transform.SetParent(go.transform, false);
        vol.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = vol.AddComponent<MeshRenderer>();
        var mrl = Lumiere("M_Kit_RayonLune", new Color(0.62f, 0.66f, 0.74f), 0.16f, true, null);
        mrl.SetFloat("_Steps", 0); mrl.SetFloat("_EdgeSoft", 3f); mrl.SetFloat("_FadeLength", 0.8f);
        mr.sharedMaterial = mrl;
        mr.shadowCastingMode = ShadowCastingMode.Off; mr.receiveShadows = false;
        var spot = new GameObject("Lune_Projecteur").AddComponent<Light>();
        spot.transform.SetParent(go.transform, false);
        spot.transform.localRotation = Quaternion.LookRotation(dir);
        spot.type = LightType.Spot; spot.color = new Color(0.62f, 0.7f, 1f); spot.intensity = 4f; spot.range = L * 1.5f;
        spot.spotAngle = 22f; spot.innerSpotAngle = 12f; spot.shadows = LightShadows.Soft;
        Poussiere(go.transform, bas * 0.5f, new Vector3(3.5f, 1.6f, L * 0.9f), Quaternion.LookRotation(dir), 260);
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Art/Prefabs/KitChateau/Chateau_RayonLune.prefab");
        Object.DestroyImmediate(go);
        return "Chateau_RayonLune ";
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
        // acte I : réglage intermédiaire ; acte II un peu plus éclairé (lune par les fenêtres) ; acte III dehors sous la lune
        if (kit == "KitChateau") Eclairage(new Color(0.07f, 0.075f, 0.10f), new Color(0.016f, 0.018f, 0.03f), 14, 110, 0.35f, new Vector3(40, 30, 0));
        else if (kit == "KitJardin") Eclairage(new Color(0.08f, 0.09f, 0.12f), new Color(0.03f, 0.035f, 0.055f), 18, 140, 0.55f, new Vector3(32, 35, 0));
        else Eclairage(new Color(0.055f, 0.058f, 0.080f), new Color(0.012f, 0.014f, 0.024f), 10, 85, 0.25f, new Vector3(40, 30, 0));
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
            if (pf.name.Contains("Ciel") || pf.name.Contains("Brume"))
            {   // ciel et brume : posés au centre de la vitrine, hors de la grille
                var c = (GameObject)PrefabUtility.InstantiatePrefab(pf, root);
                c.transform.position = new Vector3(maxW / 2, 0, -30);
                continue;
            }
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
        if (kit == "KitJardin")
        {   // dehors : la lune éclaire (lumière froide de capture), pas une torche
            lum.color = new Color(0.62f, 0.7f, 1f); lum.intensity = 1.1f;
        }
        foreach (Transform t in root)
        {
            if (t.name == "Sol" || t.name == "Torche" || t.name.Contains("Ciel") || t.name.Contains("Brume")) continue;
            var mrs = t.GetComponentsInChildren<MeshRenderer>();
            if (mrs.Length == 0) continue;
            var b = mrs[0].bounds;
            foreach (var r in mrs) b.Encapsulate(r.bounds);
            // pièces qui plongent sous le sol (gouffre, piles du pont) : on cadre la partie au-dessus de -4 m
            if (b.max.y - b.min.y > 14f && b.min.y < 1f && t.name.Contains("Gouffre")) { var mx = b.max; var mn = b.min; mn.y = mx.y - 12f; b.SetMinMax(mn, mx); }
            float s = Mathf.Max(b.size.x, b.size.y * 1.1f, b.size.z);
            var dir = new Vector3(-0.55f, 0.35f, 1f).normalized;   // les pièces regardent vers +Z (avant Blender -Y)
            cam.transform.position = b.center + dir * (s * 1.15f + 1.2f);
            cam.transform.LookAt(b.center);
            // une torche du jeu près de la pièce, côté caméra (comme une torche murale voisine)
            lum.transform.position = b.center + new Vector3(-s * 0.5f - 1f, s * 0.35f + 1.5f, s * 0.6f + 1f);
            lum.range = Mathf.Max(15.4f, s * 1.6f);
            froid.transform.position = b.center + new Vector3(s * 0.7f + 1f, s * 0.6f + 2f, -s * 0.9f - 2f);
            froid.range = Mathf.Max(14f, s * 2.2f);
            // les autres pièces sont masquées (la caméra ne tombe jamais dans une pièce voisine)
            var autres = root.Cast<Transform>().Where(o => o != t && o.name != "Sol" && o.gameObject.activeSelf).ToList();
            foreach (var o in autres) o.gameObject.SetActive(false);
            Shot(cam, Path.Combine(dossier, t.name + ".jpg"), 480, 480);
            foreach (var o in autres) o.gameObject.SetActive(true);
            sb.Append(t.name).Append(' ');
        }
        Object.DestroyImmediate(lum);
        Object.DestroyImmediate(froid.gameObject);
        foreach (var l in torches) l.enabled = true;
        EditorSettings.asyncShaderCompilation = asyncAvant;
        return "ok " + sb;
    }
}
