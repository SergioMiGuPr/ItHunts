// Contrôle de la tête du traqueur dans Unity : matériaux réels (toon), pose de Repos, brouillard des niveaux (linéaire 6–45 m),
// captures de près (face, dessous) et mesure de la lueur des pupilles à 10, 30 et 60 m depuis 1.7 m (FOV 70, 320 × 180 comme
// StalkerVisibilityTests, et 1280 × 720) ; trous : arêtes de bord de la tête, volume signé.
using System.IO; using System.Linq; using System.Text; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
public static class TeteShot
{
    const string Fbx = "Assets/Art/Models/Ennemis/SK_Traqueur.fbx";
    static Texture2D Shot(Camera cam, int w, int h)
    {
        var rt = new RenderTexture(w, h, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
        RenderTexture.active = rt; var tx = new Texture2D(w, h, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, w, h), 0, 0); tx.Apply();
        RenderTexture.active = null; cam.targetTexture = null; Object.DestroyImmediate(rt); return tx;
    }
    public static string Run(string outDir)
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("ItHunts-art/Assets")) return "MAUVAIS PROJET";
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var sb = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.06f, 0.06f, 0.08f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 6; RenderSettings.fogEndDistance = 45;
        RenderSettings.fogColor = new Color(0.007f, 0.008f, 0.015f);
        var lg = new GameObject("lune").AddComponent<Light>(); lg.type = LightType.Directional; lg.intensity = 0.6f; lg.color = new Color(0.7f, 0.75f, 1f);
        lg.transform.rotation = Quaternion.Euler(35, 150, 0);
        var go = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Fbx));
        var clip = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().First(c => c.name.EndsWith("Traqueur_Repos"));
        clip.SampleAnimation(go, 0f);
        var body = go.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "SK_Traqueur");
        var mesh = body.sharedMesh;
        sb.AppendFormat("matériaux {0} ; ", string.Join(",", body.sharedMaterials.Select(m => m ? m.name : "null")));
        // arêtes de bord du sous-maillage de la tête (matériau 2), au-dessus du cou
        var tete = go.GetComponentsInChildren<Transform>().First(t => t.name == "Tete");
        var baked = new Mesh(); body.BakeMesh(baked, true);
        int sub = System.Array.FindIndex(body.sharedMaterials, m => m && m.name == "M_Traqueur_Tete");
        var tri = mesh.GetTriangles(sub); var V = mesh.vertices;
        var edges = new System.Collections.Generic.Dictionary<(int, int), int>();
        // soudure par position (les coutures d'UV dupliquent les sommets)
        var key = V.Select(v => new Vector3Int(Mathf.RoundToInt(v.x * 1000), Mathf.RoundToInt(v.y * 1000), Mathf.RoundToInt(v.z * 1000))).ToArray();
        var idOf = new System.Collections.Generic.Dictionary<Vector3Int, int>(); var wid = key.Select(k => idOf.TryGetValue(k, out var i) ? i : (idOf[k] = idOf.Count)).ToArray();
        for (int i = 0; i < tri.Length; i += 3)
            for (int k = 0; k < 3; k++)
            {
                int a = wid[tri[i + k]], b = wid[tri[i + (k + 1) % 3]]; var e = a < b ? (a, b) : (b, a);
                edges[e] = edges.TryGetValue(e, out var n) ? n + 1 : 1;
            }
        sb.AppendFormat("tête : {0} triangles, {1} arêtes de bord (tête et pieds, toutes) ; ", tri.Length / 3, edges.Count(e => e.Value == 1));
        sb.AppendFormat("volume du corps {0:0} m³ ; ", TraqShotVol(mesh));
        // maillage cuit pour les captures
        var g = new GameObject("_cuit"); g.transform.SetPositionAndRotation(body.transform.position, body.transform.rotation);
        g.AddComponent<MeshFilter>().sharedMesh = baked; g.AddComponent<MeshRenderer>().sharedMaterials = body.sharedMaterials; body.enabled = false;
        Vector3 hp = tete.position + tete.up * 1.0f;
        sb.AppendFormat("tête (os) à {0} ; ", hp.ToString("0.0"));
        var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>(); cam.farClipPlane = 500;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = RenderSettings.fogColor;
        Directory.CreateDirectory(outDir);
        Vector3 fwd = go.transform.forward;
        // gros plans (sans brouillard)
        RenderSettings.fog = false; RenderSettings.ambientLight = new Color(0.16f, 0.15f, 0.2f); lg.intensity = 1.4f;
        cam.fieldOfView = 30;
        foreach (var (n, off) in new[] { ("face", fwd * 12 + Vector3.up * 0.5f), ("dessous", fwd * 9 - Vector3.up * 7 + go.transform.right * 3), ("profil", go.transform.right * 12), ("dos", -fwd * 10 + Vector3.up * 5 + go.transform.right * 4) })
        {
            camGo.transform.position = hp + off; camGo.transform.LookAt(hp);
            File.WriteAllBytes(Path.Combine(outDir, "u_tete_" + n + ".jpg"), Shot(cam, 900, 900).EncodeToJPG(90));
        }
        // distance, depuis le sol (1.7 m), brouillard des niveaux
        RenderSettings.fog = true; RenderSettings.ambientLight = new Color(0.06f, 0.06f, 0.08f); lg.intensity = 0.6f; cam.fieldOfView = 70;
        foreach (float d in new[] { 10f, 30f, 60f })
        {
            Vector3 eye = go.transform.position + fwd * d + Vector3.up * 1.7f;
            camGo.transform.position = eye; camGo.transform.LookAt(hp);
            foreach (var (w, h) in new[] { (320, 180), (1280, 720) })
            {
                var tx = Shot(cam, w, h);
                Vector3 s = cam.WorldToScreenPoint(hp); float best = 0, red = 0; int rx = Mathf.Max(4, w / 20);
                for (int y = Mathf.Max(0, (int)(s.y * h / cam.pixelHeight) - rx); y < Mathf.Min(h, (int)(s.y * h / cam.pixelHeight) + rx); y++)
                    for (int x = Mathf.Max(0, (int)(s.x * w / cam.pixelWidth) - rx); x < Mathf.Min(w, (int)(s.x * w / cam.pixelWidth) + rx); x++)
                    { var c = tx.GetPixel(x, y); float l = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b; if (l > best) { best = l; red = c.r; } }
                float any = 0; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) { var c = tx.GetPixel(x, y); any = Mathf.Max(any, 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b); }
                sb.AppendFormat("{0} m {1}×{2} : lum. max près de la tête {3:0.000} (rouge {4:0.00}), image {5:0.000} ; ", d, w, h, best, red, any);
                if (w == 1280) File.WriteAllBytes(Path.Combine(outDir, "u_dist_" + d + ".jpg"), tx.EncodeToJPG(90));
            }
        }
        Object.DestroyImmediate(g); Object.DestroyImmediate(go);
        return sb.ToString();
    }
    static float TraqShotVol(Mesh m)
    {
        var v = m.vertices; var t = m.triangles; double s = 0;
        for (int i = 0; i < t.Length; i += 3) s += Vector3.Dot(v[t[i]], Vector3.Cross(v[t[i + 1]], v[t[i + 2]])) / 6.0;
        return (float)s;
    }
}
