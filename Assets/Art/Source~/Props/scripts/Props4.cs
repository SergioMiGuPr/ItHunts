// Lot 4 (nuit 4) : intégration Unity des props Tripo. Construit() en édition : réglages d'import, matériaux toon, prefabs
// avec colliders et LODGroup (LOD0 / LOD1 nommés *_LOD0 / *_LOD1 dans les FBX ; deuxième passe, branche art-props-3d-b).
// À relancer après la deuxième passe : les FBX ont changé de hiérarchie (SM_Prop_<Nom>_LOD0/_LOD1, Couvercle = vide sur la
// charnière avec Couvercle_LOD0/_LOD1), les prefabs P_Prop_* sont reconstruits (même chemin, même GUID). Shoot(dossier) en Play Mode (acte chargé) : chaque greybox remplacée par son prefab, captures avant / après
// de près et à distance de jeu, éclairage du jeu.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItHunts.Level.Acts;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class Props4
{
    public const string Modeles = "Assets/Art/Models/Props/", Textures = "Assets/Art/Textures/Props/",
        Mats = "Assets/Art/Materials/Props/", Prefabs = "Assets/Art/Prefabs/Props/";
    public static readonly string[] Noms = { "Tonneau", "Chaise", "Poutre", "Sarcophage", "Ratelier", "Coffre", "CoffreVerrou", "Statue",
        "Lanterne", "Etagere", "CoffreDeco", "Table", "Banc", "Paille", "Armoire", "Lit", "Chaudron" };
    // décor sans collision (catalogue des props) : aucun collider dans le prefab
    static readonly HashSet<string> SansCollision = new HashSet<string> { "Lanterne", "Paille" };

    public static string Construit()
    {
        Directory.CreateDirectory(Mats); Directory.CreateDirectory(Prefabs);
        AssetDatabase.Refresh();
        var modele = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Niveau1/M_Caisse.mat");
        var log = new List<string>();
        foreach (var n in Noms)
        {
            string tp = Textures + "T_Prop_" + n + "_BaseColor.png";
            var ti = (TextureImporter)AssetImporter.GetAtPath(tp);
            ti.sRGBTexture = true; ti.mipmapEnabled = true; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.anisoLevel = 4;
            ti.SaveAndReimport();
            string mp = Modeles + "SM_Prop_" + n + ".fbx";
            var mi = (ModelImporter)AssetImporter.GetAtPath(mp);
            mi.globalScale = 1f; mi.useFileScale = true; mi.importAnimation = false; mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.importCameras = false; mi.importLights = false; mi.isReadable = false; mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.SaveAndReimport();

            var mat = Mat("M_Prop_" + n, modele);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
            if (n == "Lanterne")
            {
                // vitres et bougie lumineuses (masque T_Prop_Lanterne_Emission, linéaire) : la lanterne se lit allumée de près
                string ep = Textures + "T_Prop_Lanterne_Emission.png";
                var ei = (TextureImporter)AssetImporter.GetAtPath(ep);
                ei.sRGBTexture = false; ei.mipmapEnabled = true; ei.textureCompression = TextureImporterCompression.CompressedHQ; ei.SaveAndReimport();
                mat.SetFloat("_Emission", 1f); mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ep));
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.55f, 0.2f) * 2.2f);
            }
            mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_WorldUV")) mat.SetFloat("_WorldUV", 0f);
            Material inter = null;
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(mp);
            bool aInterieur = fbx.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Any(m => m != null && m.name.Contains("Interieur"));
            if (aInterieur)
            {
                inter = Mat("M_Prop_" + n + "_Interieur", modele);
                inter.SetTexture("_BaseMap", null);
                inter.SetColor("_BaseColor", new Color(0.035f, 0.028f, 0.024f));
                if (inter.HasProperty("_WorldUV")) inter.SetFloat("_WorldUV", 0f);
            }
            if (n == "Coffre" || n == "CoffreVerrou")
            {
                // lueur dorée (« tout ce qui est interactif brille », ART-HANDOFF § 5) : émission chaude modulée par la texture
                // peinte (le laiton et le bois clair brillent plus que les fers), lisible à 15-20 m sans lumière de salle ; plus
                // de rim (il donnait des reflets blancs froids)
                mat.SetFloat("_Rim", 0f); mat.DisableKeyword("_RIM_ON");
                mat.SetFloat("_Emission", 1f); mat.EnableKeyword("_EMISSION");
                mat.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(tp));
                mat.SetColor("_EmissionColor", new Color(1.0f, 0.58f, 0.18f) * (n == "Coffre" ? 1.1f : 0.9f));
                if (mat.HasProperty("_EmissionThroughFog")) mat.SetFloat("_EmissionThroughFog", 0.6f);
            }
            EditorUtility.SetDirty(mat);

            // prefab : modèle + matériaux + colliders simples
            var go = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            go.name = "P_Prop_" + n;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(m => m != null && m.name.Contains("Interieur") ? inter : mat).ToArray();
            var b = new Bounds(go.transform.position, Vector3.zero); bool init = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds); }
            if (!SansCollision.Contains(n)) Colliders(n, go, b);
            Lods(go);
            if (n == "Lanterne")
            {
                // lumière chaude sur le vide « Feu » (réglages de départ, à reprendre par main avec ses lumières de niveau)
                var feu = go.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Feu");
                if (feu != null && feu.GetComponent<Light>() == null)
                {
                    var l = feu.gameObject.AddComponent<Light>();
                    l.type = LightType.Point; l.color = new Color(1.0f, 0.62f, 0.3f); l.range = 6f; l.intensity = 1.5f; l.shadows = LightShadows.None;
                }
            }
            string pp = Prefabs + "P_Prop_" + n + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(go, pp);
            Object.DestroyImmediate(go);
            int tri = fbx.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh.triangles.Length / 3);
            log.Add($"{n} : taille {b.size.ToString("F2")} pivot→bas {b.min.y:F2} tri {tri} intérieur {aInterieur} sous-maillages {string.Join(",", fbx.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Select(m => m ? m.name : "null"))}");
        }
        Variantes(log);
        AssetDatabase.SaveAssets();
        return string.Join("\n", log);
    }

    // Variantes assemblées à partir des modèles (aucune génération) pour les greybox de main qui ont d'autres formes :
    // tonneaux couchés en rang (3,1 × 0,95 × 1,2), table de banquet (6 m), banc d'église (4 m).
    static void Variantes(List<string> log)
    {
        GameObject Pf(string n) => AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "P_Prop_" + n + ".prefab");
        void Sauve(GameObject go, string nom)
        {
            var bb = new Bounds(go.transform.position, Vector3.zero); bool init = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>()) { if (!init) { bb = r.bounds; init = true; } else bb.Encapsulate(r.bounds); }
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            var bc = go.AddComponent<BoxCollider>(); bc.center = bb.center - go.transform.position; bc.size = bb.size;
            PrefabUtility.SaveAsPrefabAsset(go, Prefabs + "P_Prop_" + nom + ".prefab");
            log.Add($"{nom} (variante assemblée) : {bb.size.ToString("F2")}");
            Object.DestroyImmediate(go);
        }
        {   // quatre tonneaux couchés en rang sur deux chantiers (cales), × 1,05
            var go = new GameObject("P_Prop_Tonneaux_Couches");
            for (int k = 0; k < 4; k++)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(Pf("Tonneau"), go.transform);
                t.transform.localPosition = new Vector3(-1.155f + k * 0.77f, 0.315f, -0.47f);
                t.transform.localRotation = Quaternion.Euler(90f, 0f, k * 37f);
                t.transform.localScale = Vector3.one * 1.05f;
            }
            Sauve(go, "Tonneaux_Couches");
        }
        {   // table de banquet : deux tables bout à bout, × 1,15 (6 m)
            var go = new GameObject("P_Prop_Table_Banquet");
            for (int k = 0; k < 2; k++)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(Pf("Table"), go.transform);
                t.transform.localPosition = new Vector3((k - 0.5f) * 2.6f * 1.15f, 0f, 0f);
                t.transform.localScale = Vector3.one * 1.15f;
            }
            Sauve(go, "Table_Banquet");
        }
        {   // banc d'église : deux bancs bout à bout, × 0,81 (4 m)
            var go = new GameObject("P_Prop_Banc_Eglise");
            for (int k = 0; k < 2; k++)
            {
                var t = (GameObject)PrefabUtility.InstantiatePrefab(Pf("Banc"), go.transform);
                t.transform.localPosition = new Vector3((k - 0.5f) * 2.48f * 0.81f, 0f, 0f);
                t.transform.localScale = Vector3.one * 0.81f;
            }
            Sauve(go, "Banc_Eglise");
        }
    }

    static readonly Dictionary<string, string[]> Candidats = new Dictionary<string, string[]>
    {
        { "Tonneau", new[] { "Tonneau", "Tonneaux_Couches" } },
        { "Table", new[] { "Table", "Table_Banquet" } },
        { "Banc", new[] { "Banc", "Banc_Eglise" } },
    };

    /// <summary>Meilleur modèle pour une greybox : variante, rotation de 90° si l'axe long diffère, échelle uniforme (moyenne
    /// géométrique des rapports d'axes) et écart de forme (somme des |log| des rapports restants).</summary>
    static (string nom, float rotY, float ech, float ecart) Ajuste(string nomBase, Bounds gb, Quaternion rotGreybox)
    {
        var res = (nom: nomBase, rotY: 0f, ech: 1f, ecart: 99f);
        foreach (var nom in Candidats.TryGetValue(nomBase, out var c) ? c : new[] { nomBase })
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "P_Prop_" + nom + ".prefab");
            if (pf == null) continue;
            var rs = pf.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
            var mb = rs[0].bounds; foreach (var r in rs) mb.Encapsulate(r.bounds);
            // dimensions de la greybox dans son propre repère (approximation : boîte monde, rotation multiple de 90°)
            float gx = gb.size.x, gz = gb.size.z, gy = gb.size.y;
            foreach (float rot in new[] { 0f, 90f })
            {
                Vector3 m = rotGreybox * Quaternion.Euler(0, rot, 0) * mb.size;
                float mx = Mathf.Abs(m.x), mz = Mathf.Abs(m.z), my = mb.size.y;
                float lx = Mathf.Log(gx / mx), ly = Mathf.Log(gy / my), lz = Mathf.Log(gz / mz);
                float le = (lx + ly + lz) / 3f;
                float e = Mathf.Abs(lx - le) + Mathf.Abs(ly - le) + Mathf.Abs(lz - le);
                if (e < res.ecart) res = (nom, rot, Mathf.Exp(le), e);
            }
        }
        return res;
    }

    // un seul LODGroup sur la racine : renderers *_LOD0 (corps et couvercle) puis *_LOD1 ; ceux que l'import a pu créer
    // (racine, Couvercle) sont retirés d'abord
    static void Lods(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        var l0 = rs.Where(r => r.name.EndsWith("_LOD0")).ToArray();
        var l1 = rs.Where(r => r.name.EndsWith("_LOD1")).ToArray();
        foreach (var g in go.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(g);
        if (l0.Length == 0 || l1.Length == 0) return;
        var lg = go.AddComponent<LODGroup>();
        lg.SetLODs(new[] { new LOD(0.25f, l0), new LOD(0.015f, l1) });
        lg.RecalculateBounds();
    }

    static Material Mat(string nom, Material modele)
    {
        string p = Mats + nom + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(modele) { name = nom }; AssetDatabase.CreateAsset(m, p); }
        return m;
    }

    static void Box(GameObject go, Vector3 c, Vector3 s) { var bc = go.AddComponent<BoxCollider>(); bc.center = c; bc.size = s; }

    static void Colliders(string n, GameObject go, Bounds b)
    {
        Vector3 c = b.center - go.transform.position, s = b.size;
        switch (n)
        {
            case "Ratelier":   // une seule boîte (montants, barre et armes), un peu moins profonde que le modèle
                Box(go, new Vector3(c.x, s.y * 0.5f, c.z), new Vector3(s.x, s.y, s.z * 0.6f));
                break;
            case "Statue":     // socle + capsule
                Box(go, new Vector3(c.x, 0.5f, c.z), new Vector3(s.x, 1.0f, s.z));
                var cap = go.AddComponent<CapsuleCollider>(); cap.center = new Vector3(c.x, 1.0f + (s.y - 1.0f) * 0.5f, c.z);
                cap.height = s.y - 1.0f; cap.radius = Mathf.Min(s.x, s.z) * 0.35f;
                break;
            case "Coffre":
            case "CoffreVerrou":
                // une boîte sur la racine, à la taille du coffre fermé : elle ne suit pas le couvercle quand il s'ouvre
                // (aucun collider sur Couvercle)
                Box(go, new Vector3(c.x, s.y * 0.5f, c.z), s);
                break;
            default:
                Box(go, c, s);
                break;
        }
    }

    // ---------------- captures en jeu ----------------
    static Camera PlayerCam()
    {
        foreach (var p in ItHunts.Player.PlayerHealth.All)
        {
            var pi = p != null ? p.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            if (pi != null && pi.camera != null) return pi.camera;
        }
        return Camera.main;
    }

    // greybox -> (prefab, position locale, rotation locale, échelle) d'après INTEGRATION-PROPS (lot 4)
    static (string nom, Vector3 pos, Vector3 rot, Vector3 ech)? Regle(string greybox, Bounds gb)
    {
        float bas = gb.min.y;
        switch (greybox)
        {
            case "Prop_Tonneau_Attrapable": return ("Tonneau", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Tonneau": return ("Tonneau", Vector3.zero, Vector3.zero, Vector3.one * 1.22f);
            case "Prop_Chaise_Attrapable": return ("Chaise", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Poutre": return ("Poutre", Vector3.zero, Vector3.zero, Vector3.one);
            // deuxième passe : échelles uniformes seulement (cotes du modèle déjà proches de la greybox)
            case "Prop_Lanterne": return ("Lanterne", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Etagere": return ("Etagere", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_CoffreDeco": return ("CoffreDeco", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Table": return ("Table", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Banc": return ("Banc", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Paille": return ("Paille", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Armoire": return ("Armoire", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Lit": return ("Lit", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Chaudron": return ("Chaudron", Vector3.zero, Vector3.zero, Vector3.one);
            case "Prop_Sarcophage": return ("Sarcophage", Vector3.zero, new Vector3(0, 90, 0), Vector3.one * 1.15f);
            case "Prop_Ratelier": return ("Ratelier", Vector3.zero, Vector3.zero, Vector3.one * 1.17f);
            case "Prop_Statue": return ("Statue", Vector3.zero, Vector3.zero, Vector3.one * 1.385f);
        }
        if (greybox.StartsWith("Coffre de la salle verrouillée")) return ("CoffreVerrou", Vector3.zero, Vector3.zero, Vector3.one * 1.5f);
        if (greybox.StartsWith("Coffre : ")) return ("Coffre", Vector3.zero, Vector3.zero, Vector3.one * 1.5f);
        return null;
    }

    static Camera _cam; static RenderTexture _rt; static Texture2D _tex;

    static void Snap(string f)
    {
        _cam.targetTexture = _rt; _cam.Render(); _cam.Render();
        RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0); _tex.Apply();
        RenderTexture.active = null; _cam.targetTexture = null;
        File.WriteAllBytes(f, _tex.EncodeToJPG(92));
    }

    // shaders compilés tout de suite pendant les captures (réglage de session, jamais écrit dans ProjectSettings) ; toujours rétabli
    public static async Task<string> Shoot(string dossier, string acte)
    {
        bool asyncAvant = UnityEditor.ShaderUtil.allowAsyncCompilation;
        UnityEditor.ShaderUtil.allowAsyncCompilation = false;
        try { return await ShootCorps(dossier, acte); }
        finally { UnityEditor.ShaderUtil.allowAsyncCompilation = asyncAvant; }
    }

    static async Task<string> ShootCorps(string dossier, string acte)
    {
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 240f && (ActLevel.Current == null || ActLevel.Current.Loading || PlayerCam() == null)) await Task.Delay(500);
        await Task.Delay(1500);
        Camera src = PlayerCam();
        var player = ItHunts.Player.PlayerHealth.All.Count > 0 ? ItHunts.Player.PlayerHealth.All[0].transform : null;
        var go = new GameObject("CaptureCam");
        _cam = go.AddComponent<Camera>(); _cam.CopyFrom(src);
        var dst = go.AddComponent<UniversalAdditionalCameraData>(); var sd = src.GetComponent<UniversalAdditionalCameraData>();
        if (sd != null) { dst.renderPostProcessing = sd.renderPostProcessing; dst.antialiasing = sd.antialiasing; dst.SetRenderer(0); dst.renderShadows = sd.renderShadows; dst.volumeLayerMask = sd.volumeLayerMask; }
        _cam.cullingMask = src.cullingMask & ~LayerMask.GetMask("UI"); _cam.enabled = false;
        foreach (var l in src.GetComponentsInChildren<Light>(true)) { var lp = l.transform.localPosition; var lr = l.transform.localRotation; l.transform.SetParent(go.transform, false); l.transform.localPosition = lp; l.transform.localRotation = lr; }
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) cv.enabled = false;
        _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        Directory.CreateDirectory(dossier);
        var log = new List<string>();

        // une greybox par type (la plus dégagée : la première trouvée dont la vue de près n'est pas bouchée)
        var deja = new HashSet<string>();
        var cibles = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.parent == null || !t.parent.name.StartsWith("Prop_"))
            .Where(t => Regle(Nettoie(t.name), new Bounds()) != null).ToList();
        // instances éclairées d'abord (lumière ponctuelle du niveau la plus proche), pour des images lisibles
        var lumieres = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.type == LightType.Point && l.enabled && (player == null || !l.transform.IsChildOf(player)) && !l.transform.IsChildOf(go.transform)).ToList();
        float Proche(Transform t) => lumieres.Count == 0 ? 99f : lumieres.Min(l => Vector3.Distance(l.transform.position, t.position) / Mathf.Max(l.range, 1f));
        cibles = cibles.OrderBy(t => Proche(t)).ToList();
        Time.timeScale = 0f;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.sharedMaterial != null && r.sharedMaterial.shader != null && r.sharedMaterial.shader.name.Contains("WorldText")) r.gameObject.SetActive(false);
        if (player) foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var t in cibles)
        {
            string g = Nettoie(t.name);
            var regle = Regle(g, new Bounds()).Value;
            string type = g.StartsWith("Coffre de la salle") ? "CoffreVerrou" : g.StartsWith("Coffre : ") ? "Coffre" : g;
            if (deja.Contains(type)) continue;
            var rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0) continue;
            var gb = rs[0].bounds; foreach (var r in rs) gb.Encapsulate(r.bounds);
            // modèle ajusté à l'instance (variante, orientation, échelle uniforme) ; formes trop différentes : écartées
            var lacet = Quaternion.Euler(0, Mathf.Round(t.eulerAngles.y / 90f) * 90f, 0);   // face de la greybox (multiple de 90°)
            var aj = Ajuste(regle.nom, gb, lacet);
            if (aj.ecart > 0.9f) { log.Add($"{type} : instance {t.name} écartée (greybox {gb.size.ToString("F2")}, écart de forme {aj.ecart:F2} avec {aj.nom})"); continue; }
            // point de vue : du côté le plus dégagé, à 2.2 m + taille (près) et 7 m (jeu), œil 1.7 m
            Vector3 centre = gb.center; float sol = gb.min.y;
            Vector3 best = Vector3.zero; float bd = -1;
            for (int i = 0; i < 12; i++)
            {
                Vector3 d = Quaternion.Euler(0, i * 30f, 0) * t.forward; d.y = 0; d.Normalize();
                float near = 1.8f + Mathf.Max(gb.extents.x, gb.extents.z) * 1.6f;
                Vector3 e = new Vector3(centre.x, sol + 1.7f, centre.z) + d * near;
                // vue dégagée : au moins 4 rayons sur 5 (centre et coins hauts de la boîte) arrivent sur la greybox
                int ok = 0;
                foreach (var cible in new[] { centre, gb.center + new Vector3(gb.extents.x, gb.extents.y * 0.8f, gb.extents.z), gb.center + new Vector3(-gb.extents.x, gb.extents.y * 0.8f, gb.extents.z), gb.center + new Vector3(gb.extents.x, gb.extents.y * 0.8f, -gb.extents.z), gb.center + new Vector3(-gb.extents.x, gb.extents.y * 0.8f, -gb.extents.z) })
                    if (!Physics.Linecast(e, cible, out RaycastHit hc, ~0, QueryTriggerInteraction.Ignore) || hc.transform.IsChildOf(t) || Vector3.Distance(hc.point, cible) < 0.4f) ok++;
                bool libre = ok >= 4;
                float libreLoin = Physics.Raycast(new Vector3(centre.x, sol + 1.7f, centre.z), d, out RaycastHit h2, 12f, ~0, QueryTriggerInteraction.Ignore) ? h2.distance : 12f;
                if (!libre) continue;
                if (libreLoin > bd) { bd = libreLoin; best = d; }
            }
            if (bd < 0) continue;
            deja.Add(type);
            float pres = 1.8f + Mathf.Max(gb.extents.x, gb.extents.z) * 1.6f, loin = Mathf.Clamp(bd - 0.4f, pres + 1f, 9f);
            float tresLoin = Mathf.Max(loin, Mathf.Min(18f, bd - 0.4f));
            var vues = new[] { ("pres", pres), ("jeu", loin), ("loin", tresLoin) };
            // avant
            foreach (var (nv, dist) in vues)
            {
                Vector3 e = new Vector3(centre.x, sol + 1.7f, centre.z) + best * dist;
                if (player) { var cc = player.GetComponent<CharacterController>(); if (cc) cc.enabled = false; player.position = e - Vector3.up * 1.6f + best * 0.3f; if (cc) cc.enabled = true; }
                _cam.transform.SetPositionAndRotation(e, Quaternion.LookRotation(centre - e));
                await Task.Delay(120);
                Snap(Path.Combine(dossier, $"{acte}_{type}_{nv}_avant.jpg"));
            }
            // remplacement : renderers de la greybox masqués, prefab posé sur sa base
            foreach (var r in rs) r.enabled = false;
            if (type.StartsWith("Coffre"))       // couvercle et lueur de la greybox du coffre (objets voisins)
                foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("Economy_Chest") && Vector3.Distance(r.bounds.center, gb.center) < 2.5f) { r.enabled = false; if (r.transform != t) r.gameObject.SetActive(false); }
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "P_Prop_" + aj.nom + ".prefab");
            var inst = (GameObject)Object.Instantiate(pf);
            inst.transform.SetPositionAndRotation(new Vector3(gb.center.x, sol, gb.center.z), lacet * Quaternion.Euler(0, aj.rotY, 0));
            inst.transform.localScale = Vector3.one * aj.ech;
            // attrapables : la lueur de main (EmissionGlow, couleur de prise à portée) posée sur le modèle, comme en jeu
            if (type.Contains("Attrapable"))
                foreach (var r in inst.GetComponentsInChildren<Renderer>()) ItHunts.Combat.EmissionGlow.Set(r, new Color(0.6f, 0.25f, 1f) * 0.35f, true);
            foreach (var c in inst.GetComponentsInChildren<Collider>()) c.enabled = false;
            await Task.Delay(120);
            foreach (var (nv, dist) in vues)
            {
                Vector3 e = new Vector3(centre.x, sol + 1.7f, centre.z) + best * dist;
                if (player) { var cc = player.GetComponent<CharacterController>(); if (cc) cc.enabled = false; player.position = e - Vector3.up * 1.6f + best * 0.3f; if (cc) cc.enabled = true; }
                _cam.transform.SetPositionAndRotation(e, Quaternion.LookRotation(centre - e));
                await Task.Delay(120);
                Snap(Path.Combine(dossier, $"{acte}_{type}_{nv}_apres.jpg"));
            }
            var ib = inst.GetComponentsInChildren<Renderer>()[0].bounds; foreach (var r in inst.GetComponentsInChildren<Renderer>()) ib.Encapsulate(r.bounds);
            log.Add($"{type} ({t.name}) -> {aj.nom} × {aj.ech:F2} rot {aj.rotY:0} écart {aj.ecart:F2} ; greybox {gb.size.ToString("F2")} -> modèle {ib.size.ToString("F2")} ; vue {best.ToString("F2")} près {pres:F1} m, jeu {loin:F1} m");
        }
        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, $"points_{acte}.txt"), log);
        return string.Join("\n", log);
    }

    static string Nettoie(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\s*\(\d+\)$", "").Replace("(Clone)", "").Trim();
}
