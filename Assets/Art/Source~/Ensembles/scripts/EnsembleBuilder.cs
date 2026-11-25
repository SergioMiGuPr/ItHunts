// Ensembles de décor (nuit 6, lot 1) : matériaux, import des pièces (SM_Ens_*), prefabs de pièces et prefabs d'ensembles
// (assemblés d'après les plans écrits par ensembles.py), banc de captures hors jeu.
//   unity command script run --file EnsembleBuilder.cs --entry EnsembleBuilder.<Materiaux|Pieces|Construit|Banc|Mesures> --args '[…]'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

public static class EnsembleBuilder
{
    static readonly CultureInfo CI = CultureInfo.InvariantCulture;
    const string MATS = "Assets/Art/Materials/Ensembles", TEX = "Assets/Art/Textures/Ensembles/T_Ens_", MOD = "Assets/Art/Models/Ensembles",
                 PRE = "Assets/Art/Prefabs/Ensembles", ENS = "Assets/Art/Levels/Ensembles";
    static float F(string s) => float.Parse(s, CI);

    static void Dossier(string p)
    {
        if (AssetDatabase.IsValidFolder(p)) return;
        var parent = Path.GetDirectoryName(p).Replace("\\", "/");
        Dossier(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(p));
    }

    // ---------------------------------------------------------------- matériaux
    static Texture2D Tex(string nom, int max, bool clamp = false, bool alpha = false)
    {
        string p = nom.StartsWith("Assets/") ? nom : TEX + nom + "_BaseColor.png";
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        if (ti == null) return null;
        if (!nom.StartsWith("Assets/"))
        {
            ti.sRGBTexture = true; ti.mipmapEnabled = true; ti.maxTextureSize = max; ti.anisoLevel = 4;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.alphaSource = TextureImporterAlphaSource.FromInput; ti.alphaIsTransparency = false;
            ti.mipMapsPreserveCoverage = alpha; ti.alphaTestReferenceValue = 0.5f;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static Material M(string nom, string tex, Color c, float tuile = 1f, bool decor = true, bool deuxFaces = true, int max = 1024)
    {
        string p = $"{MATS}/M_Ens_{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find("ItHunts/Toon")); AssetDatabase.CreateAsset(m, p); }
        if (tex != null) { m.SetTexture("_BaseMap", Tex(tex, max)); m.SetTextureScale("_BaseMap", Vector2.one / tuile); }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_WorldUV", 0); m.DisableKeyword("_WORLD_UV");
        m.SetFloat("_Cull", deuxFaces ? 0 : 2);
        if (decor)
        {
            m.SetFloat("_Decor", 1); m.EnableKeyword("_DECOR");
            m.SetFloat("_VertexPaint", 1); m.SetFloat("_MacroVariation", 0.45f); m.SetFloat("_MacroScale", 2.5f);
            m.SetColor("_MacroTint", new Color(0.74f, 0.76f, 0.74f)); m.SetColor("_GrimeColor", new Color(0.42f, 0.38f, 0.34f));
            m.SetFloat("_GrimeStrength", 0.7f); m.SetFloat("_WetStrength", 0f); m.SetFloat("_LayerLow", 0f); m.SetFloat("_LayerAmount", 0f); m.SetFloat("_LayerTop", 0f);
            m.SetTexture("_LayerMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Materiaux/T_Mat_Mousse_BaseColor.png"));
            m.SetFloat("_LayerTile", 2f); m.SetFloat("_HeightBlend", 0.1f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Emet(Material m, Color c, Texture2D masque = null, float brouillard = 0.6f)
    {
        m.SetFloat("_Emission", 1); m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c);
        if (masque) m.SetTexture("_EmissionMap", masque);
        m.SetFloat("_EmissionThroughFog", brouillard);
    }

    static void Transparent(Material m)
    {
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0); m.renderQueue = 3000;
        foreach (var ps in new[] { "ShadowCaster", "DepthOnly", "DepthNormals" }) m.SetShaderPassEnabled(ps, false);
    }

    public static string Materiaux()
    {
        Dossier(MATS);
        Color C(float r, float g, float b) => new Color(r, g, b);
        M("Bois", "Bois", C(1.0f, 0.97f, 0.92f)); M("BoisSombre", "Bois", C(0.62f, 0.50f, 0.44f)); M("BoisGris", "Bois", C(0.80f, 0.86f, 0.92f));
        M("Bois_Brule", "Bois", C(0.2f, 0.17f, 0.16f));
        M("Fer", "Metal", C(0.36f, 0.35f, 0.38f)); M("Laiton", "Metal", C(0.95f, 0.70f, 0.36f)); M("Etain", "Metal", C(0.80f, 0.82f, 0.86f));
        M("Dorure", "Metal", C(0.88f, 0.62f, 0.28f));
        M("Pierre", "Pierre", C(0.90f, 0.90f, 0.95f)); M("PierreClaire", "Pierre", C(1.16f, 1.12f, 1.04f));
        M("PierreMoussue", "Pierre", C(0.86f, 0.9f, 0.86f)); M("Ardoise", "Pierre", C(0.5f, 0.52f, 0.6f));
        M("Lin", "Tissu", C(0.72f, 0.64f, 0.52f), 0.8f); M("Drap", "Tissu", C(0.84f, 0.81f, 0.75f), 0.8f);
        M("VeloursRouge", "Tissu", C(0.5f, 0.11f, 0.12f), 0.8f); M("VeloursBleu", "Tissu", C(0.19f, 0.23f, 0.38f), 0.8f);
        M("Cuir", "Metal", C(0.46f, 0.29f, 0.19f), 0.7f); M("Jute", "Tissu", C(0.76f, 0.62f, 0.42f), 0.4f); M("Grain", "Tissu", C(0.95f, 0.78f, 0.42f), 0.3f);
        M("Papier", "Papier", C(1, 1, 1), 0.6f); M("Livres", "Livres", C(1.1f, 1.1f, 1.1f)); M("TerreCuite", "Metal", C(0.80f, 0.43f, 0.28f), 0.6f);
        M("Faience", "Metal", C(1.15f, 1.18f, 1.25f), 1.5f); M("Terreau", "Assets/Art/Textures/Materiaux/T_Mat_Terre_BaseColor.png", C(1, 1, 1), 1.5f);
        M("Ecorce", "Assets/Art/Textures/Materiaux/T_Mat_Ecorce_BaseColor.png", C(1, 1, 1), 1f);
        M("Tapis", "Assets/Art/Textures/Materiaux/T_Mat_Tapis_BaseColor.png", C(1, 1, 1), 1f);
        M("Noir", null, C(0.05f, 0.048f, 0.052f), 1, false); M("Sang", null, C(0.20f, 0.035f, 0.03f), 1, false);
        M("Eau", null, C(0.045f, 0.065f, 0.085f), 1, false); M("Verre", null, C(0.16f, 0.22f, 0.17f), 1, false);
        M("Miroir", null, C(0.46f, 0.52f, 0.62f), 1, false); M("Champignon", "Papier", C(0.95f, 0.92f, 0.86f), 0.3f, false);
        M("Tige", "Tissu", C(0.30f, 0.38f, 0.20f), 0.5f); M("Feuillage_Coeur", null, C(0.055f, 0.075f, 0.045f), 1, false);
        foreach (var n in new[] { "Pierre", "PierreClaire", "PierreMoussue", "Ecorce", "BoisGris" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{MATS}/M_Ens_{n}.mat");
            m.SetFloat("_LayerTop", n == "PierreMoussue" ? 0.6f : 0f); m.SetFloat("_LayerLow", n == "PierreMoussue" ? 0.35f : 0.05f); EditorUtility.SetDirty(m);
        }
        Emet(M("Braise", null, C(0.5f, 0.12f, 0.03f), 1, false), new Color(1.7f, 0.36f, 0.05f), null, 0.8f);
        // flammes : hors de la passe des contours (aucune écriture de profondeur ni de normales), sinon l'encre les mange
        var fl = M("Flamme", null, C(1f, 0.35f, 0.06f), 1, false); Emet(fl, new Color(1.75f, 0.42f, 0.05f), null, 0.8f);
        var fc = M("Flamme_Coeur", null, C(1f, 0.8f, 0.45f), 1, false); Emet(fc, new Color(1.6f, 1.05f, 0.45f), null, 0.8f);
        foreach (var mf in new[] { fl, fc }) { mf.SetFloat("_ZWrite", 0); mf.renderQueue = mf == fl ? 2950 : 2951; foreach (var ps in new[] { "ShadowCaster", "DepthOnly", "DepthNormals" }) mf.SetShaderPassEnabled(ps, false); }
        // verre translucide encré (serre, lampes)
        var vs = M("Verre_Sale", null, new Color(0.35f, 0.5f, 0.48f, 0.16f), 1, false); Transparent(vs);
        var vl = M("Verre_Lampe", null, new Color(0.9f, 0.5f, 0.2f, 1f), 1, false); Emet(vl, new Color(1.15f, 0.5f, 0.12f), null, 0.8f);
        // images peintes
        M("Portraits", "Portraits", C(1.12f, 1.08f, 1.02f), 1, false, true, 2048); M("Tapisserie", "Tapisserie", C(0.9f, 0.86f, 0.82f), 1, false, true, 2048);
        Tex("Portraits", 2048, true); Tex("Tapisserie", 2048, true);
        var v = M("Vitrail", "Vitrail", C(0.10f, 0.10f, 0.11f), 1, false, true, 2048);
        Emet(v, new Color(2.3f, 2.35f, 2.6f), Tex("Vitrail", 2048, true), 0.3f);
        v.SetFloat("_LightSaturation", 0.4f);
        var f = M("Feuillage", "Feuillage", C(0.95f, 1.0f, 0.95f), 1, false, true, 2048);
        Tex("Feuillage", 2048, true, true);
        f.SetFloat("_AlphaClip", 1); f.EnableKeyword("_ALPHATEST_ON"); f.SetFloat("_Cutoff", 0.45f); f.SetFloat("_PointWrap", 0.6f); f.SetFloat("_LightSaturation", 0.45f);
        foreach (var n in new[] { "Portraits", "Tapisserie" }) { var pm = AssetDatabase.LoadAssetAtPath<Material>($"{MATS}/M_Ens_{n}.mat"); pm.SetFloat("_LightSaturation", 0.5f); EditorUtility.SetDirty(pm); }
        f.renderQueue = (int)RenderQueue.AlphaTest;
        // réglages de matériaux existants (relecture) : rouille moins saturée sous la lumière chaude, coffre décoratif aux clairs éteints
        var ro = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Bibliotheque/M_Bib_FerRouille.mat"); ro.SetFloat("_LightSaturation", 0.75f); ro.SetColor("_BaseColor", new Color(0.78f, 0.76f, 0.78f)); EditorUtility.SetDirty(ro);
        var cd = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Props/M_Prop_CoffreDeco.mat"); cd.SetColor("_BaseColor", new Color(0.92f, 0.9f, 0.86f)); cd.SetFloat("_LightSaturation", 1f); cd.SetFloat("_Emission", 0f); cd.DisableKeyword("_EMISSION"); cd.SetColor("_EmissionColor", Color.black); cd.SetFloat("_Rim", 0f); cd.DisableKeyword("_RIM_ON"); EditorUtility.SetDirty(cd);
        AssetDatabase.SaveAssets();
        return "ok";
    }

    // ---------------------------------------------------------------- pièces
    public static string Pieces(string filtre)
    {
        Dossier(PRE);
        var sb = new StringBuilder();
        var noms = filtre == "*" ? null : new HashSet<string>(filtre.Split(','));
        foreach (var f in Directory.GetFiles(MOD, "*.fbx").Select(x => x.Replace("\\", "/")))
        {
            var nom = Path.GetFileNameWithoutExtension(f).Replace("SM_Ens_", "");
            if (noms != null && !noms.Contains(nom)) continue;
            var mi = (ModelImporter)AssetImporter.GetAtPath(f);
            mi.globalScale = 1; mi.useFileScale = true; mi.importNormals = ModelImporterNormals.Import; mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None; mi.importCameras = false; mi.importLights = false; mi.isReadable = true;
            mi.importTangents = ModelImporterTangents.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.SaveAndReimport();
            mi.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
            mi.SaveAndReimport();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(f));
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            go.name = "Ens_" + nom;
            foreach (var t in go.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Col_")).ToList())
            {
                var mr = t.GetComponent<MeshRenderer>(); var mf = t.GetComponent<MeshFilter>();
                var mc = t.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = true;
                Object.DestroyImmediate(mr);
            }
            foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                foreach (var m in r.sharedMaterials)
                    if (m == null || m.shader == null || !m.shader.name.StartsWith("ItHunts")) sb.Append($"[mat? {nom}:{(m ? m.name + "/" + m.shader.name : "null")}] ");
            }
            foreach (var t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/Ens_{nom}.prefab");
            Object.DestroyImmediate(go);
            sb.Append(nom).Append(' ');
        }
        AssetDatabase.SaveAssets();
        return "ok " + sb;
    }

    // Props Tripo de la nuit 6 (mêmes réglages que Props4.cs, sans reconstruire les autres)
    public static string Props()
    {
        var modele = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Niveau1/M_Caisse.mat");
        foreach (var n in new[] { "AngePleureur", "Fauteuil" })
        {
            string tp = $"Assets/Art/Textures/Props/T_Prop_{n}_BaseColor.png", mp = $"Assets/Art/Models/Props/SM_Prop_{n}.fbx", matp = $"Assets/Art/Materials/Props/M_Prop_{n}.mat";
            var ti = (TextureImporter)AssetImporter.GetAtPath(tp); ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.anisoLevel = 4; ti.SaveAndReimport();
            var mi = (ModelImporter)AssetImporter.GetAtPath(mp);
            mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None; mi.importCameras = false; mi.importLights = false; mi.isReadable = true; mi.SaveAndReimport();
            var m = AssetDatabase.LoadAssetAtPath<Material>(matp);
            if (m == null) { m = new Material(modele); AssetDatabase.CreateAsset(m, matp); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp)); m.SetColor("_BaseColor", Color.white * (n == "Fauteuil" ? 1.15f : 1.05f)); m.SetFloat("_WorldUV", 0);
            m.SetFloat("_Emission", 0); m.DisableKeyword("_EMISSION"); m.SetColor("_EmissionColor", Color.black); EditorUtility.SetDirty(m);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(mp));
            go.name = "P_Prop_" + n;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = r.sharedMaterials.Select(_ => m).ToArray();
            foreach (var g in go.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(g);
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var lg = go.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(0.25f, rs.Where(r => r.name.EndsWith("_LOD0")).ToArray()), new LOD(0.015f, rs.Where(r => r.name.EndsWith("_LOD1")).ToArray()) }); lg.RecalculateBounds();
            var b = Boite(go);
            var bc = go.AddComponent<BoxCollider>(); bc.center = b.center; bc.size = new Vector3(b.size.x * 0.9f, b.size.y, b.size.z * 0.9f);
            PrefabUtility.SaveAsPrefabAsset(go, $"Assets/Art/Prefabs/Props/P_Prop_{n}.prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        return "ok";
    }

    // ---------------------------------------------------------------- ensembles
    static Dictionary<string, GameObject> _cache = new Dictionary<string, GameObject>();
    static GameObject Prefab(string nom)
    {
        if (_cache.TryGetValue(nom, out var c) && c) return c;
        var g = AssetDatabase.FindAssets(nom + " t:Prefab", new[] { "Assets/Art/Prefabs", "Assets/Art/VFX/Prefabs" })
            .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == nom);
        return _cache[nom] = g != null ? AssetDatabase.LoadAssetAtPath<GameObject>(g) : null;
    }

    static Vector3 V(string s) { var t = s.Split(','); return t.Length == 1 ? new Vector3(0, F(t[0]), 0) : new Vector3(F(t[0]), F(t[1]), F(t[2])); }

    static Transform Enfant(GameObject root, string nom)
    {
        var t = root.transform.Find(nom);
        if (t) return t;
        var g = new GameObject(nom); g.transform.SetParent(root.transform, false);
        return g.transform;
    }

    //  N|nom|acte   P|prefab|x|y|z|rx,ry,rz|échelle|options (D détail, X sans collision, S sans lumière)   L|x|y|z|r,g,b|intensité|portée
    //  F|x|y|z|échelle (feu VFX)   C|x|y|z|sx|sy|sz|ry (boîte de collision)   A|nom|x|y|z (repère)   E|x|z|l|p|h (emprise)
    public static string Construit(string plan)
    {
        _cache.Clear();
        var sb = new StringBuilder();
        foreach (var bloc in File.ReadAllText(plan).Split(new[] { "\n---" }, StringSplitOptions.RemoveEmptyEntries))
        {
            var lignes = bloc.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 2 && l[1] == '|').Select(l => l.Split('|')).ToList();
            var n = lignes.FirstOrDefault(t => t[0] == "N");
            if (n == null) continue;
            var root = new GameObject(n[1]);
            foreach (var t in lignes)
            {
                if (t[0] == "P")
                {
                    var pf = Prefab(t[1]);
                    if (pf == null) { sb.Append($"[manque {n[1]}:{t[1]}] "); continue; }
                    string op = t.Length > 7 ? t[7] : "";
                    bool lod = pf.GetComponentInChildren<LODGroup>() != null;
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(pf, Enfant(root, op.Contains("D") && !lod ? "Details" : "Mobilier"));
                    g.transform.localPosition = new Vector3(F(t[2]), F(t[3]), F(t[4]));
                    g.transform.localRotation = Quaternion.Euler(V(t[5]));
                    if (t.Length > 6 && t[6] != "") { var s = t[6].Split(','); g.transform.localScale = s.Length == 3 ? V(t[6]) : Vector3.one * F(s[0]); }
                    if (op.Contains("V"))   // vitres du kit (serre) : verre translucide à la place des éclats opaques
                    {
                        var verre = AssetDatabase.LoadAssetAtPath<Material>($"{MATS}/M_Ens_Verre_Sale.mat");
                        foreach (var r in g.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.name.StartsWith("M_Kit_Vitrail") ? verre : m).ToArray();
                    }
                    if (op.Contains("X")) foreach (var c in g.GetComponentsInChildren<Collider>()) c.enabled = false;
                    if (op.Contains("S")) foreach (var l in g.GetComponentsInChildren<Light>()) l.enabled = false;
                    foreach (var l in g.GetComponentsInChildren<Light>()) l.shadows = LightShadows.None;   // règle de main : aucune ombre de torche
                }
                else if (t[0] == "L")
                {
                    var l = new GameObject("Lumiere").AddComponent<Light>();
                    l.transform.SetParent(Enfant(root, "Lumieres"), false);
                    l.transform.localPosition = new Vector3(F(t[1]), F(t[2]), F(t[3]));
                    var c = t[4].Split(','); l.type = LightType.Point; l.color = new Color(F(c[0]), F(c[1]), F(c[2]));
                    l.intensity = F(t[5]); l.range = F(t[6]); l.shadows = LightShadows.None;
                }
                else if (t[0] == "F")
                {
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(Prefab("VFX_Torche_Feu"), Enfant(root, "Lumieres"));
                    g.transform.localPosition = new Vector3(F(t[1]), F(t[2]), F(t[3])); g.transform.localScale = Vector3.one * F(t[4]);
                }
                else if (t[0] == "C")
                {
                    var g = new GameObject("Col"); g.transform.SetParent(Enfant(root, "Collisions"), false);
                    g.transform.localPosition = new Vector3(F(t[1]), F(t[2]), F(t[3])); g.transform.localRotation = Quaternion.Euler(0, t.Length > 7 ? F(t[7]) : 0, 0);
                    g.AddComponent<BoxCollider>().size = new Vector3(F(t[4]), F(t[5]), F(t[6])); g.isStatic = true;
                }
                else if (t[0] == "A")
                {
                    var g = new GameObject(t[1]); g.transform.SetParent(Enfant(root, "Reperes"), false);
                    g.transform.localPosition = new Vector3(F(t[2]), F(t[3]), F(t[4]));
                }
            }
            // six lumières au plus : les sources imbriquées en trop gardent leur flamme mais n'éclairent plus
            var lums = root.GetComponentsInChildren<Light>().Where(l => l.enabled).ToList();
            foreach (var l in lums.Where(l => l.transform.parent == null || l.transform.parent.name != "Lumieres").Reverse().Take(Mathf.Max(0, lums.Count - 6))) l.enabled = false;
            // petits objets : masqués de loin (sous 1,5 % de la hauteur de l'écran) en un seul groupe
            var det = root.transform.Find("Details");
            if (det)
            {
                var rs = det.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer) && r.GetComponentInParent<LODGroup>() == null).ToArray();
                if (rs.Length > 0) { var lg = det.gameObject.AddComponent<LODGroup>(); lg.SetLODs(new[] { new LOD(0.05f, rs) }); lg.RecalculateBounds(); }
            }
            // décor enjambé par le traqueur (règle de main pour le mobilier)
            root.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
            Dossier($"{ENS}/{n[2]}");
            PrefabUtility.SaveAsPrefabAsset(root, $"{ENS}/{n[2]}/{n[1]}.prefab");
            var b = Boite(root);
            int tri = root.GetComponentsInChildren<MeshFilter>().Where(m => m.sharedMesh && m.GetComponent<MeshRenderer>() && !m.name.EndsWith("_LOD1")).Sum(m => (int)(m.sharedMesh.triangles.Length / 3));
            sb.AppendLine($"{n[1]} {b.size.x:F1} x {b.size.z:F1} x {b.size.y:F1} min {b.min.ToString("F2")} tri {tri} lum {root.GetComponentsInChildren<Light>().Count(l => l.enabled)} col {root.GetComponentsInChildren<Collider>().Count(c => c.enabled)} rend {root.GetComponentsInChildren<MeshRenderer>().Length}");
            Object.DestroyImmediate(root);
        }
        AssetDatabase.SaveAssets();
        return "ok\n" + sb;
    }

    static Bounds Boite(GameObject g)
    {
        var rs = g.GetComponentsInChildren<MeshRenderer>();
        var b = rs.Length > 0 ? rs[0].bounds : new Bounds();
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public static string Mesures(string dossiers, string sortie)
    {
        var sb = new StringBuilder();
        foreach (var d in dossiers.Split(','))
            foreach (var p in AssetDatabase.FindAssets("t:Prefab", new[] { d }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(x => x))
            {
                var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p));
                var b = Boite(g);
                sb.AppendLine($"{g.name} min {b.min.ToString("F2")} max {b.max.ToString("F2")} lum {g.GetComponentsInChildren<Light>().Length} col {g.GetComponentsInChildren<Collider>().Length}");
                Object.DestroyImmediate(g);
            }
        File.WriteAllText(sortie, sb.ToString());
        return "ok";
    }

    // ---------------------------------------------------------------- banc (hors jeu, ambiance des actes)
    static void Cube(string mat, Vector3 c, Vector3 s)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.transform.position = c; g.transform.localScale = s;
        g.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.FindAssets(mat + " t:Material").Select(AssetDatabase.GUIDToAssetPath)
            .Where(q => Path.GetFileNameWithoutExtension(q) == mat).Select(AssetDatabase.LoadAssetAtPath<Material>).FirstOrDefault();
    }

    public static string Banc(string noms, string dossier, double ambd)
    {
        float ambx = (float)ambd; Directory.CreateDirectory(dossier);
        bool asyncAvant = EditorSettings.asyncShaderCompilation; EditorSettings.asyncShaderCompilation = false;
        var sb = new StringBuilder();
        foreach (var acte in new[] { "Caves", "Chateau", "Jardin" })
        {
            if (!AssetDatabase.IsValidFolder($"{ENS}/{acte}")) continue;
            var liste = AssetDatabase.FindAssets("t:Prefab", new[] { $"{ENS}/{acte}" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(x => x)
                .Where(p => noms == "*" || noms.Split(',').Any(n => Path.GetFileNameWithoutExtension(p).Contains(n))).ToList();
            if (liste.Count == 0) continue;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false, false);
            Color amb, fog; float d0, d1, lune;
            if (acte == "Chateau") { amb = new Color(0.07f, 0.075f, 0.10f); fog = new Color(0.016f, 0.018f, 0.03f); d0 = 14; d1 = 110; lune = 0.35f; }
            else if (acte == "Jardin") { amb = new Color(0.08f, 0.09f, 0.12f); fog = new Color(0.03f, 0.035f, 0.055f); d0 = 18; d1 = 140; lune = 0.6f; }
            else { amb = new Color(0.055f, 0.058f, 0.080f); fog = new Color(0.012f, 0.014f, 0.024f); d0 = 10; d1 = 85; lune = 0f; }
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = amb * ambx; RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog; RenderSettings.fogStartDistance = d0; RenderSettings.fogEndDistance = d1; RenderSettings.skybox = null;
            if (lune > 0) { var l = new GameObject("Lune").AddComponent<Light>(); l.type = LightType.Directional; l.color = new Color(0.55f, 0.66f, 1f); l.intensity = lune * ambx; l.shadows = LightShadows.Soft; l.transform.rotation = Quaternion.Euler(acte == "Jardin" ? new Vector3(34, 145, 0) : new Vector3(40, 150, 0)); }
            var vol = new GameObject("Volume").AddComponent<Volume>(); vol.isGlobal = true;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PostProcess_TestMovement.asset");
            Cube(acte == "Caves" ? "M_Bib_Dalles" : acte == "Chateau" ? "M_Chateau_Dalles" : "M_Jardin_SolMelange", new Vector3(0, -0.5f, 10), new Vector3(60, 1, 60));
            if (acte != "Jardin") Cube(acte == "Caves" ? "M_Bib_Moellons" : "M_Chateau_PlatrePeint", new Vector3(0, 6, -0.5f), new Vector3(60, 12, 1));
            else { var ciel = Prefab("Jardin_Ciel"); if (ciel) PrefabUtility.InstantiatePrefab(ciel); foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.type == LightType.Directional && l.name != "Lune") l.intensity = 0; }
            // lanterne du joueur (halo chaud discret)
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = fog; cam.allowHDR = true; cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 900;
            foreach (var p in liste)
            {
                var nom = Path.GetFileNameWithoutExtension(p);
                var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p));
                foreach (var ps in g.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(3f, true, true);
                var b = Boite(g);
                float taille = Mathf.Max(b.size.x * 0.62f, b.size.z * 0.5f, b.size.y * 0.9f);
                Vector3 c = new Vector3(b.center.x, Mathf.Min(b.center.y, 2f) * 0.75f, b.center.z);
                var vues = new[] { (new Vector3(0.28f, 0, 1f), Mathf.Max(b.extents.x / 1.0f, b.size.y * 0.75f) + b.extents.z * 0.45f + 0.6f, 1.75f, "A"), (new Vector3(-0.95f, 0, 1f), Mathf.Max(b.extents.x * 0.95f, b.size.y * 0.7f) + b.extents.z * 0.4f + 0.2f, 2.1f, "B") };
                foreach (var (d, dist, h, e) in vues)
                {
                    var dir = d.normalized;
                    var o = new Vector3(b.center.x, h, b.center.z) + dir * dist;
                    if (o.z < 0.6f) o.z = 0.6f;
                    cam.transform.position = o; cam.transform.LookAt(c);
                    cam.targetTexture = rt; cam.Render(); cam.Render();
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply(); RenderTexture.active = null; cam.targetTexture = null;
                    File.WriteAllBytes(Path.Combine(dossier, $"{nom}_{e}.jpg"), tex.EncodeToJPG(92));
                }
                Object.DestroyImmediate(g);
                sb.Append(nom).Append(' ');
            }
        }
        EditorSettings.asyncShaderCompilation = asyncAvant;
        return "ok " + sb;
    }
}
