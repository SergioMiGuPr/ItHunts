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
            if (n.StartsWith("Coffre"))
            {
                // lueur dorée discrète (« tout ce qui est interactif brille ») : rim chaud du shader toon, visible dans le noir
                mat.SetFloat("_Rim", 1f); mat.EnableKeyword("_RIM_ON");
                mat.SetColor("_RimColor", new Color(1.0f, 0.62f, 0.25f) * 1.6f);
                mat.SetFloat("_RimWidth", 0.22f); mat.SetFloat("_RimSoftness", 0.2f); mat.SetFloat("_RimIntensity", 0.65f); mat.SetFloat("_RimLitOnly", 0f);
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
        AssetDatabase.SaveAssets();
        return string.Join("\n", log);
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
            case "Prop_Tonneau": return ("Tonneau", Vector3.zero, Vector3.zero, new Vector3(1.5f, 1.22f, 1.5f));
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
            case "Prop_Sarcophage": return ("Sarcophage", Vector3.zero, new Vector3(0, 90, 0), new Vector3(1.14f, 1.25f, 1.33f));
            case "Prop_Ratelier": return ("Ratelier", Vector3.zero, Vector3.zero, new Vector3(1.625f, 1.17f, 1f));
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

    public static async Task<string> Shoot(string dossier, string acte)
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
            .Where(t => Regle(Nettoie(t.name), new Bounds()) != null).OrderBy(t => t.name).ToList();
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
            // point de vue : du côté le plus dégagé, à 2.2 m + taille (près) et 7 m (jeu), œil 1.7 m
            Vector3 centre = gb.center; float sol = gb.min.y;
            Vector3 best = Vector3.zero; float bd = -1;
            for (int i = 0; i < 12; i++)
            {
                Vector3 d = Quaternion.Euler(0, i * 30f, 0) * t.forward; d.y = 0; d.Normalize();
                float near = 1.8f + Mathf.Max(gb.extents.x, gb.extents.z) * 1.6f;
                Vector3 e = new Vector3(centre.x, sol + 1.7f, centre.z) + d * near;
                bool libre = !Physics.Linecast(e, centre, out RaycastHit h, ~0, QueryTriggerInteraction.Ignore) || h.transform.IsChildOf(t);
                float libreLoin = Physics.Raycast(new Vector3(centre.x, sol + 1.7f, centre.z), d, out RaycastHit h2, 12f, ~0, QueryTriggerInteraction.Ignore) ? h2.distance : 12f;
                if (!libre) continue;
                if (libreLoin > bd) { bd = libreLoin; best = d; }
            }
            if (bd < 0) continue;
            deja.Add(type);
            float pres = 1.8f + Mathf.Max(gb.extents.x, gb.extents.z) * 1.6f, loin = Mathf.Clamp(bd - 0.4f, pres + 1f, 7f);
            var vues = new[] { ("pres", pres), ("jeu", loin) };
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
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "P_Prop_" + regle.nom + ".prefab");
            var inst = (GameObject)Object.Instantiate(pf);
            inst.transform.SetPositionAndRotation(new Vector3(t.position.x, sol, t.position.z), t.rotation * Quaternion.Euler(regle.rot));
            inst.transform.localScale = regle.ech;
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
            log.Add($"{type} ({t.name}) greybox {gb.size.ToString("F2")} -> modèle {ib.size.ToString("F2")} ; vue {best.ToString("F2")} près {pres:F1} m, jeu {loin:F1} m");
        }
        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, $"points_{acte}.txt"), log);
        return string.Join("\n", log);
    }

    static string Nettoie(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\s*\(\d+\)$", "").Replace("(Clone)", "").Trim();
}
