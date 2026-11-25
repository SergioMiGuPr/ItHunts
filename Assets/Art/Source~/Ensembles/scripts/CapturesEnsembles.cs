// Nuit 6, lot 1 : captures en jeu des ensembles, posés dans une vraie salle générée de l'acte (éclairage du jeu : ambiance,
// brouillard, post-process, lanterne du joueur ; plus les lumières propres de l'ensemble).
//  1) CapturesJeu.Prep("<scène de l'acte>", <graine>) ; playmode start
//  2) unity command script run --file CapturesEnsembles.cs --entry CapturesEnsembles.Shoot --args '["<dossier>", "Caves|Chateau|Jardin", "*"]'
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItHunts.Level.Acts;
using ItHunts.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CapturesEnsembles
{
    static Camera _cam; static RenderTexture _rt; static Texture2D _tex; static string _dir; static Transform _player;
    static readonly List<string> _log = new List<string>();

    static Camera PlayerCam()
    {
        foreach (var p in PlayerHealth.All)
        {
            var pi = p != null ? p.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            if (pi != null && pi.camera != null) return pi.camera;
        }
        return Camera.main;
    }

    static async Task Snap(string nom, Vector3 oeil, Vector3 regard)
    {
        if (_player != null)
        {
            var cc = _player.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            _player.position = oeil - Vector3.up * 1.6f; if (cc) cc.enabled = true;
        }
        _cam.transform.SetPositionAndRotation(oeil, Quaternion.LookRotation(regard - oeil));
        Time.timeScale = 1f; await Task.Delay(300); Time.timeScale = 0f; await Task.Delay(60);
        _cam.targetTexture = _rt; _cam.Render(); _cam.Render();
        RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0); _tex.Apply();
        RenderTexture.active = null; _cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(_dir, nom + ".jpg"), _tex.EncodeToJPG(92));
        _log.Add($"{nom} : {oeil.ToString("F1")} -> {regard.ToString("F1")}");
    }

    static bool Mur(Vector3 o, Vector3 d, out RaycastHit h, float max = 70f) =>
        Physics.Raycast(o, d, out h, max, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.normal.y) < 0.3f;

    static Bounds Boite(GameObject g)
    {
        var rs = g.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled).ToArray();
        var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public static async Task<string> Shoot(string dossier, string acte, string noms, string fonction = "")
    {
        _log.Clear();
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 240f && (ActLevel.Current == null || ActLevel.Current.Loading || ActLevel.Current.Plan == null || PlayerCam() == null)) await Task.Delay(500);
        await Task.Delay(1500);
        var src = PlayerCam();
        _player = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0].transform : null;
        var go = new GameObject("CaptureCam");
        _cam = go.AddComponent<Camera>(); _cam.CopyFrom(src);
        var dst = go.AddComponent<UniversalAdditionalCameraData>(); var sd = src.GetComponent<UniversalAdditionalCameraData>();
        if (sd != null) { dst.renderPostProcessing = sd.renderPostProcessing; dst.antialiasing = sd.antialiasing; dst.SetRenderer(0); dst.renderShadows = sd.renderShadows; dst.volumeLayerMask = sd.volumeLayerMask; }
        int masque = src.cullingMask & ~LayerMask.GetMask("UI");
        for (int i = 0; i < 32; i++) if (LayerMask.LayerToName(i).StartsWith("FirstPerson")) masque &= ~(1 << i);
        _cam.cullingMask = masque; _cam.enabled = false; _cam.fieldOfView = 60f;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string tn = mb.GetType().Name;
            if (tn.Contains("StalkerPassesThrough")) continue;   // décor, pas un ennemi
            if (tn.Contains("Ghoul") || tn.Contains("Goule") || tn.Contains("Stalker") || tn.Contains("Enemy") || tn.Contains("Flyer") || tn.Contains("Volant") || tn.Contains("WaveDirector") || tn.Contains("Spawner"))
                mb.gameObject.SetActive(false);
        }
        foreach (var ag in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None)) ag.gameObject.SetActive(false);
        foreach (var l in src.GetComponentsInChildren<Light>(true)) { var lp = l.transform.localPosition; var lr = l.transform.localRotation; l.transform.SetParent(go.transform, false); l.transform.localPosition = lp; l.transform.localRotation = lr; }
        foreach (var r in src.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        if (_player) foreach (var r in _player.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) cv.enabled = false;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) if (r.name.Contains("Halo")) r.enabled = false;
        _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        _dir = dossier; Directory.CreateDirectory(dossier);
        Time.timeScale = 0f;

        _log.Add("salles : " + string.Join(", ", ActLevel.Current.Plan.Rooms.Select(r => r.Function + " " + r.Cells.Count)));
        // salle : la plus grande qui a un mur plan de 16 m et 16 m de recul
        var plan = ActLevel.Current.Plan;
        Vector3 pf = default, n = default; float sol = 0; bool ok = false; string desc = "";
        // jardin : grande pelouse plane sans mur proche (les ensembles de l'acte III sont des îlots de plein air)
        if (acte == "Jardin")
        {
            float best = -1f;
            foreach (var salle in plan.Rooms.Where(r => !r.Special && (fonction == "" || fonction.Split('|').Any(f => r.Function.ToString().Contains(f)))))
                foreach (var cellule in salle.Cells)
                {
                    Vector3 c = plan.CellCenter(cellule, salle.Floor + 1.7f);
                    if (!Physics.Raycast(c + Vector3.up * 1.5f, Vector3.down, out RaycastHit g0, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    int bons = 0;
                    for (int ix = -2; ix <= 2; ix++) for (int iz = -2; iz <= 2; iz++)
                        if (Physics.Raycast(new Vector3(c.x + ix * 5f, g0.point.y + 3f, c.z + iz * 5f), Vector3.down, out RaycastHit hs, 6f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(hs.point.y - g0.point.y) < 0.25f) bons++;
                    float note = bons;
                    for (int k = 0; k < 8; k++) if (Physics.Raycast(g0.point + Vector3.up * 2f, Quaternion.Euler(0, k * 45, 0) * Vector3.forward, out RaycastHit hm, 30f, ~0, QueryTriggerInteraction.Ignore)) note += hm.distance / 60f; else note += 0.5f;
                    if (note > best) { best = note; ok = true; n = -Vector3.forward; sol = g0.point.y; pf = new Vector3(c.x, sol, c.z) + Vector3.forward * 8f; desc = $"salle {salle.Function} ({salle.Cells.Count} cases), {bons}/25 points plans"; }
                }
        }
        else foreach (var large in new[] { 8f, 6.5f, 5f })
        {
            foreach (var salle in plan.Rooms.Where(r => !r.Special && (fonction == "" || fonction.Split('|').Any(f => r.Function.ToString().Contains(f)))).OrderByDescending(r => r.Cells.Count))
            {
                foreach (var cellule in salle.Cells.OrderBy(c => (c - salle.CenterCell).sqrMagnitude).Take(40))
                {
                    Vector3 c = plan.CellCenter(cellule, salle.Floor + 1.7f);
                    if (!Physics.Raycast(c + Vector3.up * 1.5f, Vector3.down, out RaycastHit g0, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                    c.y = g0.point.y + 2.2f;
                    for (int k = 0; k < 4 && !ok; k++)
                    {
                        var d = Quaternion.Euler(0, k * 90f, 0) * Vector3.forward;
                        if (!Mur(c, d, out RaycastHit h) || h.distance < 14f || h.distance > 45f) continue;
                        if (Physics.OverlapBox(h.point + Vector3.up * 2f, new Vector3(9f, 5f, 2f), Quaternion.LookRotation(d)).Any(x => { var q = x.transform; while (q != null) { if (q.name.Contains("Porte") || q.name.Contains("Gate") || q.name.Contains("Battant") || q.name.Contains("Sortie") || q.name.Contains("Arriv") || q.name.Contains("Vestibule") || q.name.Contains("Grille") || q.name.Contains("Verrou") || q.name.Contains("Locked") || q.name.Contains("Trone")) return true; q = q.parent; } return false; })) continue;
                        var lat = Vector3.Cross(Vector3.up, d);
                        bool plat = true;
                        foreach (var s in new[] { -large, -large * 0.5f, large * 0.5f, large })
                            foreach (var y in new[] { 0f, 2.5f })
                                if (!Mur(c + lat * s + Vector3.up * y, d, out RaycastHit h2) || Mathf.Abs(h2.distance - h.distance) > 0.45f) plat = false;
                        // sol plat devant le mur
                        foreach (var s in new[] { -large * 0.8f, 0f, large * 0.8f })
                            foreach (var r in new[] { 1f, 5f, 10f })
                                if (!Physics.Raycast(h.point - d * r + lat * s + Vector3.up, Vector3.down, out RaycastHit hs, 6f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(hs.point.y - g0.point.y) > 0.6f) plat = false;
                        if (!plat) continue;
                        ok = true; pf = new Vector3(h.point.x, g0.point.y, h.point.z); n = -d; sol = g0.point.y; desc = $"salle {salle.Function} ({salle.Cells.Count} cases), recul {h.distance:F1} m, mur plan de {2 * large} m";
                    }
                    if (ok) break;
                }
                if (ok) break;
            }
            if (ok) break;
        }
        _log.Add($"{acte} : {(ok ? desc : "aucun mur plan")} pied {pf.ToString("F1")} normale {n.ToString("F1")}");
        if (!ok) { File.WriteAllLines(Path.Combine(dossier, $"points_{acte}.txt"), _log); return string.Join("\n", _log); }
        Quaternion face = Quaternion.LookRotation(n);
        Vector3 L = face * Vector3.right;
        // décor de main dans la zone de pose : masqué le temps des captures (meubles, props, gravats, torches)
        var zone = new Bounds(pf + n * 6f + Vector3.up * 3f, Vector3.one * 2f); zone.Encapsulate(pf - L * 9f); zone.Encapsulate(pf + L * 9f + n * 15f + Vector3.up * 6f); zone.Encapsulate(pf - L * 9f + n * 15f);
        int masques = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if (t == null || t.parent == null || t.parent.name != "Habillage" && t.parent.name != "Props" && !t.name.StartsWith("Prop_") && !t.name.Contains("Torche") && !t.name.Contains("Attrapable") && !t.name.StartsWith("Jardin_") ) continue;
            if (zone.Contains(t.position)) { t.gameObject.SetActive(false); masques++; }
        }
        _log.Add($"décor de main masqué dans la zone : {masques}");
        var liste = AssetDatabase.FindAssets("t:Prefab", new[] { $"Assets/Art/Levels/Ensembles/{acte}" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(p => p)
            .Where(p => noms == "*" || noms.Split(',').Any(x => Path.GetFileNameWithoutExtension(p).Contains(x))).ToList();
        await Snap($"{acte}_00_avant", pf + n * 11f + Vector3.up * 1.7f, pf + Vector3.up * 2f);
        foreach (var p in liste)
        {
            string nom = Path.GetFileNameWithoutExtension(p);
            var g = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(p));
            g.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var b0 = Boite(g);
            bool ilot = b0.min.z < -1.2f;
            float recul = ilot ? -b0.min.z + 0.6f : 0.02f;
            if (nom.Contains("Clairiere")) recul = 12f;
            g.transform.SetPositionAndRotation(pf + n * recul, face);
            foreach (var c in g.GetComponentsInChildren<Collider>()) c.enabled = false;
            foreach (var ps in g.GetComponentsInChildren<ParticleSystem>()) ps.Simulate(3f, true, true);
            Vector3 W(float x, float y, float z) => g.transform.TransformPoint(new Vector3(x, y, z));
            float ex = Mathf.Min(b0.extents.x, 7.5f), h = b0.size.y, zf = b0.max.z, zc = b0.center.z;
            float dist = Mathf.Max(ex * 0.98f, Mathf.Min(h, 7f) * 0.8f) + (zf - zc) * 0.35f + 0.8f;
            float yc = Mathf.Clamp(h * 0.36f, 0.9f, 2.4f);
            if (nom.Contains("Clairiere"))
            {
                await Snap(nom + "_loin", W(1.5f, 1.75f, 9.0f), W(0, 2.2f, -2f));
                await Snap(nom + "_pres", W(-4.5f, 1.75f, 4.5f), W(1.5f, 1.4f, -2f));
            }
            else
            {
                await Snap(nom + "_loin", W(b0.center.x + ex * 0.3f, 1.75f, zc + dist), W(b0.center.x, yc, zc * 0.8f));
                await Snap(nom + "_pres", W(b0.center.x - ex * 0.75f, 1.75f, zc + dist * 0.62f), W(b0.center.x + ex * 0.18f, yc * 0.85f, zc * 0.75f));
            }
            Object.Destroy(g);
            await Task.Delay(50);
        }
        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, $"points_{acte}.txt"), _log);
        return string.Join("\n", _log.Take(4));
    }
}
