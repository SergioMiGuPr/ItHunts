// Bibliothèque de matériaux toon peints (lot « matériaux », 2026-11-22) : import des tuiles, matériaux
// (Assets/Art/Materials/Bibliotheque), décalcomanies (maillages et prefabs), scène vitrine éclairée comme le jeu (réglage
// « intermédiaire » de RENDU.md), captures pour les planches.
// `unity cmd ... script run --file MateriauxBuilder.cs --entry MateriauxBuilder.<Construit|Vitrine|Capture>`
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

public static class MateriauxBuilder
{
    const string TEX = "Assets/Art/Textures/Materiaux";
    const string MAT = "Assets/Art/Materials/Bibliotheque";
    const string DEC = "Assets/Art/Prefabs/Decals";
    const string MESH = "Assets/Art/Models/Decals";
    const string SCENE = "Assets/Art/Scenes/Vitrine_Materiaux.unity";

    // nom, taille de tuile (m) ; même table que materiaux.py (TAILLE)
    public static readonly (string nom, float tx, float ty)[] TUILES =
    {
        ("Moellons", 4, 4), ("PierreTaillee", 4, 3), ("Dalles", 4, 4), ("RocheCave", 6, 6), ("Briques", 2, 2),
        ("Mortier", 2, 2), ("Mousse", 2, 2), ("Salpetre", 2, 2), ("Humidite", 2, 4),
        ("BoisNeuf", 1, 1), ("BoisPourri", 1, 1), ("BoisMouille", 1, 1), ("FerRouille", 1, 1),
        ("PlatreFissure", 3, 3), ("Marbre", 2, 2), ("Tapis", 2, 2), ("Tenture", 2, 4),
        ("Terre", 3, 3), ("HerbeSeche", 3, 3), ("Ecorce", 1, 2), ("Racines", 2, 2), ("Gravier", 2, 2),
    };

    public static readonly string[,] DECALS =
    {
        { "FissureLongue", "FissureEtoile", "FissuresFines", "EclatAngle" },
        { "TacheHumidite", "TacheSuie", "TacheSang", "AureoleSalpetre" },
        { "MoussePlaque", "MousseJoint", "LichenPale", "Gravats" },
        { "CoulureLongue", "CouluresMultiples", "CoulureRouille", "ToileAraignee" },
    };

    static Shader Toon => Shader.Find("ItHunts/Toon");
    static Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/T_Mat_{n}_BaseColor.png");

    static void Dossier(string p)
    {
        if (AssetDatabase.IsValidFolder(p)) return;
        var parent = Path.GetDirectoryName(p).Replace("\\", "/");
        Dossier(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(p));
    }

    static void Importe()
    {
        foreach (var f in Directory.GetFiles(TEX, "*.png"))
        {
            var p = f.Replace("\\", "/");
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            bool decal = p.Contains("Decals");
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = decal;
            ti.mipmapEnabled = true;
            ti.wrapMode = decal ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.maxTextureSize = decal ? 2048 : 1024;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;   // BC7 : la hauteur (alpha) reste propre
            ti.SaveAndReimport();
        }
    }

    static Material Mat(string nom)
    {
        var path = $"{MAT}/M_Bib_{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null) { m = new Material(Toon); AssetDatabase.CreateAsset(m, path); }
        m.shader = Toon;
        return m;
    }

    static void Monde(Material m, float tx, float ty)
    {
        m.SetFloat("_WorldUV", 1); m.EnableKeyword("_WORLD_UV");
        m.SetVector("_TileSize", new Vector4(tx, ty, 0, 0));
    }

    static void Decor(Material m, string couche, float coucheTile, float bas, float dessus, float partout, float macro,
                      Color teinte, float sale = 0.75f, float salH = 1.8f, float humide = 0.5f)
    {
        m.SetFloat("_Decor", 1); m.EnableKeyword("_DECOR");
        m.SetFloat("_MacroVariation", macro);
        m.SetColor("_MacroTint", teinte);
        m.SetFloat("_GrimeStrength", sale);
        m.SetFloat("_GrimeHeight", salH);
        m.SetFloat("_WetStrength", humide);
        if (couche != null) m.SetTexture("_LayerMap", T(couche));
        m.SetFloat("_LayerTile", coucheTile);
        m.SetFloat("_LayerLow", bas);
        m.SetFloat("_LayerTop", dessus);
        m.SetFloat("_LayerAmount", partout);
    }

    public static string Construit()
    {
        Dossier(MAT); Dossier(DEC); Dossier(MESH);
        Importe();
        var sb = new StringBuilder();
        // matériaux simples : une tuile, UV monde, variation légère
        foreach (var (nom, tx, ty) in TUILES)
        {
            var m = Mat(nom);
            m.SetTexture("_BaseMap", T(nom));
            Monde(m, tx, ty);
            m.SetFloat("_Decor", 1); m.EnableKeyword("_DECOR");
            m.SetFloat("_MacroVariation", 0.3f);
            m.SetColor("_MacroTint", new Color(0.8f, 0.84f, 0.78f));
            m.SetFloat("_GrimeStrength", 0.55f);
            m.SetFloat("_WetStrength", 0.35f);
            m.SetFloat("_LayerLow", 0); m.SetFloat("_LayerTop", 0); m.SetFloat("_LayerAmount", 0);
            EditorUtility.SetDirty(m);
            sb.Append(nom).Append(' ');
        }
        // variantes « prêtes à poser » : mousse et salpêtre mélangés en hauteur, humidité en bas
        var v = new (string nom, string base_, string couche, float ct, float bas, float dessus, float partout, Color t, float sale, float salH)[]
        {
            ("Moellons_Moussus", "Moellons", "Mousse", 2, 0.55f, 0.6f, 0.0f, new Color(0.78f, 0.86f, 0.74f), 0.8f, 2.2f),
            ("Moellons_Salpetre", "Moellons", "Salpetre", 2, 0.7f, 0f, 0.0f, new Color(0.86f, 0.84f, 0.80f), 0.7f, 1.8f),
            ("PierreTaillee_Sale", "PierreTaillee", "Humidite", 2, 0.5f, 0f, 0f, new Color(0.84f, 0.82f, 0.86f), 0.75f, 2.4f),
            ("PierreTaillee_Salpetre", "PierreTaillee", "Salpetre", 2, 0.6f, 0f, 0f, new Color(0.86f, 0.84f, 0.80f), 0.7f, 1.6f),
            ("RocheCave_Humide", "RocheCave", "Mousse", 2, 0.5f, 0.75f, 0.0f, new Color(0.74f, 0.84f, 0.80f), 0.85f, 3.0f),
            ("Dalles_Sales", "Dalles", "Terre", 3, 0f, 0f, 0.25f, new Color(0.84f, 0.80f, 0.76f), 0.5f, 1.0f),
            ("Briques_Salpetre", "Briques", "Salpetre", 2, 0.5f, 0f, 0.1f, new Color(0.86f, 0.8f, 0.78f), 0.6f, 1.6f),
            ("PlatreFissure_Sale", "PlatreFissure", "Humidite", 2, 0.7f, 0f, 0f, new Color(0.86f, 0.84f, 0.78f), 0.85f, 2.2f),
            ("Terre_Racines", "Terre", "Racines", 2, 0f, 0f, 0.35f, new Color(0.84f, 0.80f, 0.74f), 0.4f, 1f),
            ("Gravier_Herbe", "Gravier", "HerbeSeche", 3, 0f, 0f, 0.3f, new Color(0.84f, 0.86f, 0.76f), 0.4f, 1f),
        };
        foreach (var x in v)
        {
            var m = Mat(x.nom);
            m.SetTexture("_BaseMap", T(x.base_));
            var t = TUILES.First(q => q.nom == x.base_);
            Monde(m, t.tx, t.ty);
            Decor(m, x.couche, x.ct, x.bas, x.dessus, x.partout, 0.4f, x.t, x.sale, x.salH);
            EditorUtility.SetDirty(m);
            sb.Append(x.nom).Append(' ');
        }
        // variante à peinture de sommets (kits des lots 2 à 4 : R saleté, G mousse, B teinte par pièce)
        foreach (var (nom, base_, couche) in new[] { ("Moellons_Peint", "Moellons", "Mousse"), ("RocheCave_Peint", "RocheCave", "Mousse"),
                                                     ("PierreTaillee_Peint", "PierreTaillee", "Salpetre"), ("Briques_Peint", "Briques", "Salpetre") })
        {
            var m = Mat(nom);
            m.SetTexture("_BaseMap", T(base_));
            var t = TUILES.First(q => q.nom == base_);
            Monde(m, t.tx, t.ty);
            Decor(m, couche, 2, 0.35f, 0.3f, 0f, 0.4f, new Color(0.8f, 0.84f, 0.78f), 0.8f, 2f);
            m.SetFloat("_VertexPaint", 1);
            EditorUtility.SetDirty(m);
            sb.Append(nom).Append(' ');
        }
        // trim sheet : UV du modèle (bandes), pas d'UV monde
        {
            var m = Mat("Trim");
            m.SetTexture("_BaseMap", T("Trim"));
            m.SetFloat("_WorldUV", 0); m.DisableKeyword("_WORLD_UV");
            m.SetFloat("_Decor", 1); m.EnableKeyword("_DECOR");
            m.SetFloat("_MacroVariation", 0.3f);
            m.SetColor("_MacroTint", new Color(0.82f, 0.84f, 0.8f));
            m.SetFloat("_GrimeStrength", 0.5f);
            m.SetFloat("_LayerLow", 0);
            EditorUtility.SetDirty(m);
        }
        // décalcomanies : un matériau translucide (atlas), un maillage et un prefab par case
        var dm = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_Bib_Decals.mat");
        if (dm == null) { dm = new Material(Toon); AssetDatabase.CreateAsset(dm, $"{MAT}/M_Bib_Decals.mat"); }
        dm.shader = Toon;
        dm.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/T_Mat_Decals.png"));
        dm.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        dm.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        dm.SetFloat("_ZWrite", 0);
        dm.SetFloat("_ReceiveShadows", 1);
        dm.SetShaderPassEnabled("ShadowCaster", false);
        dm.SetShaderPassEnabled("DepthOnly", false);
        dm.SetShaderPassEnabled("DepthNormals", false);
        dm.renderQueue = 2460;   // après l'opaque et la découpe, avant le ciel et l'eau
        dm.SetOverrideTag("RenderType", "Transparent");
        EditorUtility.SetDirty(dm);
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                string n = DECALS[i, j];
                var mesh = new Mesh { name = "SM_Decal_" + n };
                float u0 = j / 4f, u1 = (j + 1) / 4f, v1 = 1 - i / 4f, v0 = 1 - (i + 1) / 4f;
                // quad de 1 × 1 m, face vers -Z (se pose contre un mur, normale vers la pièce = -Z local) ; décalé de 1 cm
                mesh.vertices = new[] { new Vector3(-0.5f, -0.5f, -0.01f), new Vector3(0.5f, -0.5f, -0.01f), new Vector3(0.5f, 0.5f, -0.01f), new Vector3(-0.5f, 0.5f, -0.01f) };
                mesh.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1) };
                mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
                mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                mesh.RecalculateBounds();
                var mp = $"{MESH}/SM_Decal_{n}.asset";
                var old = AssetDatabase.LoadAssetAtPath<Mesh>(mp);
                if (old != null) { old.Clear(); EditorUtility.CopySerialized(mesh, old); mesh = old; }
                else AssetDatabase.CreateAsset(mesh, mp);
                var go = new GameObject("Decal_" + n);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = dm;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                PrefabUtility.SaveAsPrefabAsset(go, $"{DEC}/Decal_{n}.prefab");
                Object.DestroyImmediate(go);
            }
        AssetDatabase.SaveAssets();
        return "ok " + sb;
    }

    // ---------------------------------------------------------------- scène vitrine
    static GameObject Boite(string nom, Transform parent, Vector3 pos, Vector3 taille, Material m)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nom;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = taille;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        g.GetComponent<MeshRenderer>().sharedMaterial = m;
        g.isStatic = true;
        return g;
    }

    // mur subdivisé avec peinture de sommets : R saleté (bas), G mousse (taches), B teinte de la pièce
    static GameObject MurPeint(string nom, Transform parent, Vector3 pos, float w, float h, Material m, int seed)
    {
        var rnd = new System.Random(seed);
        int nx = 24, ny = 24;
        var verts = new List<Vector3>(); var cols = new List<Color>(); var nrm = new List<Vector3>(); var tri = new List<int>();
        float b = (float)rnd.NextDouble();
        float ox = (float)rnd.NextDouble() * 10, oy = (float)rnd.NextDouble() * 10;
        for (int y = 0; y <= ny; y++)
            for (int x = 0; x <= nx; x++)
            {
                float px = -w / 2 + w * x / nx, py = h * y / ny;
                verts.Add(new Vector3(px, py, -0.5f));
                nrm.Add(Vector3.back);
                float sal = Mathf.Clamp01(1f - (py - 0.4f * Mathf.PerlinNoise(px * 0.6f + ox, 3)) / 2.2f);
                float mo = Mathf.Clamp01((Mathf.PerlinNoise(px * 0.35f + ox, py * 0.35f + oy) - 0.55f) * 3f) * Mathf.Clamp01(1.2f - py / h);
                cols.Add(new Color(sal, mo, b, 1));
            }
        for (int y = 0; y < ny; y++)
            for (int x = 0; x < nx; x++)
            {
                int i = y * (nx + 1) + x;
                tri.AddRange(new[] { i, i + nx + 1, i + 1, i + 1, i + nx + 1, i + nx + 2 });
            }
        var mesh = new Mesh { name = nom };
        mesh.SetVertices(verts); mesh.SetColors(cols); mesh.SetNormals(nrm); mesh.SetTriangles(tri, 0);
        mesh.RecalculateBounds();
        var g = new GameObject(nom);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        g.AddComponent<MeshRenderer>().sharedMaterial = m;
        return g;
    }

    static Light Torche(Transform parent, Vector3 pos, float intens = 1.8f, float portee = 11f)
    {
        var g = new GameObject("Torche");
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        var l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.62f, 0.32f);
        l.intensity = intens;
        l.range = portee;
        l.shadows = LightShadows.Soft;
        return l;
    }

    static Material M(string n) => AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_Bib_{n}.mat");

    // une « cellule » de présentation : mur (6 × 7 m), sol (6 × 5 m), torche ; renvoie l'origine
    static void Panneau(Transform root, string nom, int k, Material mur, Material sol)
    {
        var p = new GameObject("Panneau_" + nom).transform;
        p.SetParent(root, false);
        p.localPosition = new Vector3(k * 16f, 0, 0);
        Boite("Mur", p, new Vector3(0, 4f, 0.5f), new Vector3(10, 8, 1), mur);
        Boite("Sol", p, new Vector3(0, -0.5f, -3.5f), new Vector3(10, 1, 8), sol);
        // torche fixée au mur, à 4 m, comme dans les couloirs du jeu : le mur passe de la lumière chaude au noir
        Torche(p, new Vector3(-3.2f, 4f, -0.45f));
    }

    public static string Vitrine()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // éclairage « intermédiaire » (RENDU.md § 3)
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.055f, 0.058f, 0.080f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.012f, 0.014f, 0.024f);
        RenderSettings.fogStartDistance = 10; RenderSettings.fogEndDistance = 85;
        RenderSettings.skybox = null;
        var lune = new GameObject("Lune").AddComponent<Light>();
        lune.type = LightType.Directional; lune.color = new Color(0.55f, 0.66f, 1f); lune.intensity = 0.25f;
        lune.shadows = LightShadows.Soft;
        lune.transform.rotation = Quaternion.Euler(38, 25, 0);   // éclaire les murs tournés vers -Z
        var root = new GameObject("Vitrine").transform;
        // rangée 1 : chaque tuile sur un mur, avec un sol assorti
        var sols = new Dictionary<string, string> { { "RocheCave", "Terre" }, { "BoisNeuf", "BoisNeuf" }, { "Tapis", "Tapis" }, { "Marbre", "Marbre" },
                                                    { "Terre", "Terre" }, { "HerbeSeche", "HerbeSeche" }, { "Gravier", "Gravier" }, { "Racines", "Racines" } };
        int k = 0;
        foreach (var (nom, _, _) in TUILES)
        {
            var sol = sols.TryGetValue(nom, out var s) ? s : "Dalles";
            Panneau(root, nom, k++, M(nom), M(sol));
        }
        // rangée 2 : variantes mélangées en hauteur
        var rang2 = new GameObject("Variantes").transform; rang2.SetParent(root, false); rang2.localPosition = new Vector3(0, 0, 30);
        string[] var2 = { "Moellons_Moussus", "Moellons_Salpetre", "PierreTaillee_Sale", "PierreTaillee_Salpetre", "RocheCave_Humide", "Briques_Salpetre", "PlatreFissure_Sale" };
        string[] sol2 = { "Dalles_Sales", "Dalles_Sales", "Dalles_Sales", "Marbre", "Terre_Racines", "Dalles_Sales", "Tapis" };
        for (int i = 0; i < var2.Length; i++) Panneau(rang2, var2[i], i, M(var2[i]), M(sol2[i]));
        // rangée 3 : même matériau, cinq murs côte à côte (peinture de sommets différente) ; décalcomanies ; trim
        var rang3 = new GameObject("Voisins").transform; rang3.SetParent(root, false); rang3.localPosition = new Vector3(0, 0, 60);
        Boite("Sol", rang3, new Vector3(10, -0.5f, -6), new Vector3(32, 1, 14), M("Dalles_Sales"));
        for (int i = 0; i < 5; i++)
            MurPeint("MurPeint_" + i, rang3, new Vector3(i * 6f - 2f, 0, 0), 6, 7, M("Moellons_Peint"), 11 + i * 7);
        for (int i = 0; i < 3; i++) Torche(rang3, new Vector3(i * 12f - 1f, 3.4f, -0.8f));
        // mur des décalcomanies
        var rang4 = new GameObject("Decalcomanies").transform; rang4.SetParent(root, false); rang4.localPosition = new Vector3(0, 0, 90);
        Boite("Mur", rang4, new Vector3(6, 3.5f, 0.5f), new Vector3(20, 7, 1), M("PierreTaillee"));
        Boite("Sol", rang4, new Vector3(6, -0.5f, -3), new Vector3(20, 1, 8), M("Dalles"));
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{DEC}/Decal_{DECALS[i, j]}.prefab");
                var d = (GameObject)PrefabUtility.InstantiatePrefab(pf, rang4);
                d.transform.localPosition = new Vector3(-1.5f + j * 4f, 5.6f - i * 1.6f, 0);
                d.transform.localScale = Vector3.one * 1.9f;
            }
        Torche(rang4, new Vector3(0, 3.5f, -1.2f), 2.2f, 12f); Torche(rang4, new Vector3(12, 3.5f, -1.2f), 2.2f, 12f);
        // trim : colonne et linteau dont les UV prennent les bandes de la trim sheet
        var rang5 = new GameObject("Trim").transform; rang5.SetParent(root, false); rang5.localPosition = new Vector3(40, 0, 90);
        Boite("Mur", rang5, new Vector3(0, 3.5f, 0.5f), new Vector3(10, 7, 1), M("PierreTaillee"));
        Boite("Sol", rang5, new Vector3(0, -0.5f, -3), new Vector3(10, 1, 8), M("Dalles"));
        string[] bandes = { "Corniche", "Bandeau", "Plinthe", "Poutre", "Fer", "Briques", "Marche", "Galon", "Corde" };
        float[] v0 = { 0, 176, 336, 464, 592, 688, 784, 896, 960 }, v1 = { 176, 336, 464, 592, 688, 784, 896, 960, 1024 };
        for (int i = 0; i < bandes.Length; i++)
        {
            float hh = (v1[i] - v0[i]) / 1024f * 2f;   // la tuile fait 2 m de large : hauteur réelle de la bande
            var mesh = new Mesh { name = "Trim_" + bandes[i] };
            float w = 8f;
            mesh.vertices = new[] { new Vector3(-w / 2, 0, 0), new Vector3(w / 2, 0, 0), new Vector3(w / 2, hh, 0), new Vector3(-w / 2, hh, 0) };
            float a = 1 - v1[i] / 1024f, b = 1 - v0[i] / 1024f;
            mesh.uv = new[] { new Vector2(0, a), new Vector2(w / 2, a), new Vector2(w / 2, b), new Vector2(0, b) };
            mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            var g = new GameObject("Trim_" + bandes[i]);
            g.transform.SetParent(rang5, false);
            g.transform.localPosition = new Vector3(0, 0.3f + i * 0.72f, -0.02f);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = M("Trim");
        }
        Torche(rang5, new Vector3(-3, 3.5f, -1.5f), 2.2f, 12f); Torche(rang5, new Vector3(3, 3.5f, -1.5f), 2.2f, 12f);
        // caméra
        var vol = new GameObject("Post-process (bloom du jeu)").AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PostProcess_TestMovement.asset");
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor;
        cam.allowHDR = true;
        cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 300;
        cam.tag = "MainCamera";
        EditorSceneManager.SaveScene(scene, SCENE);
        return "ok " + SCENE;
    }

    // ---------------------------------------------------------------- captures
    static string Shot(Camera cam, string sortie, int w, int h)
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

    // vues : "panneaux" (une par panneau, rangée 1 et 2), "voisins", "decals", "trim"
    public static string Capture(string dossier)
    {
        EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        bool asyncAvant = EditorSettings.asyncShaderCompilation;
        EditorSettings.asyncShaderCompilation = false;   // sinon la première image d'une variante sort en blanc
        var cam = Object.FindFirstObjectByType<Camera>();
        var sb = new StringBuilder();
        foreach (var p in GameObject.Find("Vitrine").GetComponentsInChildren<Transform>())
            {
                if (!p.name.StartsWith("Panneau_")) continue;
                cam.transform.position = p.position + new Vector3(0, 1.6f, -8.5f);
                cam.transform.rotation = Quaternion.Euler(-12, 0, 0);
                Shot(cam, Path.Combine(dossier, p.name.Replace("Panneau_", "") + ".jpg"), 640, 640);
                sb.Append(p.name).Append(' ');
            }
        var v = GameObject.Find("Voisins").transform;
        cam.transform.position = v.position + new Vector3(10, 2.6f, -11f); cam.transform.rotation = Quaternion.Euler(-6, 0, 0);
        Shot(cam, Path.Combine(dossier, "_Voisins.jpg"), 1280, 640);
        var d = GameObject.Find("Decalcomanies").transform;
        cam.transform.position = d.position + new Vector3(4.5f, 3.4f, -8.2f); cam.transform.rotation = Quaternion.Euler(-3, 0, 0);
        Shot(cam, Path.Combine(dossier, "_Decalcomanies.jpg"), 1280, 760);
        var t = GameObject.Find("Trim").transform;
        cam.transform.position = t.position + new Vector3(0, 3.4f, -7.5f); cam.transform.rotation = Quaternion.Euler(-2, 0, 0);
        Shot(cam, Path.Combine(dossier, "_Trim.jpg"), 1000, 760);
        EditorSettings.asyncShaderCompilation = asyncAvant;
        return "ok " + sb;
    }
}
