// Lot 3 (nuit 4) : captures en jeu des pièces verticales, posées dans une vraie salle générée de l'acte (seed 4242, éclairage
// du jeu, lanterne du joueur) : fenêtre et appliques scellées au mur, lustre pendu au plafond, escalier et mur d'escalade
// contre un mur, portes à 0 / 50 / 100 % d'ouverture. Avant = la salle sans les pièces, après = avec.
//  1) CapturesJeu.Prep("Assets/Scenes/Acte_2_Chateau.unity", 4242) ; playmode start
//  2) unity command script run --file CapturesLot3.cs --entry CapturesLot3.Shoot --args '["<dossier>", "Chateau"]'
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItHunts.Art.VFX;
using ItHunts.Level.Acts;
using ItHunts.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CapturesLot3
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

    static GameObject P(string acte, string nom) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Prefabs/Kit{(acte == "Cave" ? "Caves" : acte)}/{acte}_{nom}.prefab");

    static async Task Snap(string nom, Vector3 oeil, Vector3 regard)
    {
        if (_player != null)
        {
            var cc = _player.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            _player.position = oeil - Vector3.up * 1.6f; if (cc) cc.enabled = true;
        }
        _cam.transform.SetPositionAndRotation(oeil, Quaternion.LookRotation(regard - oeil));
        await Task.Delay(150);
        _cam.targetTexture = _rt; _cam.Render(); _cam.Render();
        RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0); _tex.Apply();
        RenderTexture.active = null; _cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(_dir, nom + ".jpg"), _tex.EncodeToJPG(92));
        _log.Add($"{nom} : {oeil.ToString("F1")} -> {regard.ToString("F1")}");
    }

    static bool Mur(Vector3 o, Vector3 d, out RaycastHit h) =>
        Physics.Raycast(o, d, out h, 40f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.normal.y) < 0.3f;

    static GameObject Pose(GameObject pf, Vector3 pos, Quaternion rot, List<GameObject> liste)
    {
        if (pf == null) return null;
        var g = (GameObject)Object.Instantiate(pf, pos, rot);
        foreach (var c in g.GetComponentsInChildren<Collider>()) c.enabled = false;
        liste.Add(g);
        return g;
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
        _cam.cullingMask = src.cullingMask & ~LayerMask.GetMask("UI"); _cam.enabled = false;
        foreach (var l in src.GetComponentsInChildren<Light>(true)) { var lp = l.transform.localPosition; var lr = l.transform.localRotation; l.transform.SetParent(go.transform, false); l.transform.localPosition = lp; l.transform.localRotation = lr; }
        foreach (var r in src.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        if (_player) foreach (var r in _player.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) cv.enabled = false;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            if (r.sharedMaterial && r.sharedMaterial.shader && r.sharedMaterial.shader.name.Contains("WorldText")) r.gameObject.SetActive(false);
        _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        _dir = dossier; Directory.CreateDirectory(dossier);
        Time.timeScale = 0f;

        var plan = ActLevel.Current.Plan;
        // la plus grande salle ordinaire (hors départ et sortie), puis la deuxième pour les portes
        // salles de taille moyenne (mur du fond entre 8 et 16 m du centre) : les pièces posées se voient, leurs feux éclairent
        var salles = plan.Rooms.Where(r => !r.Special).OrderByDescending(r => r.Cells.Count).ToList();
        var poses = new List<GameObject>();
        int faites = 0;
        foreach (var salle in salles)
        {
            if (faites >= 2) break;
            Vector2Int best = salle.Cells[0]; float bd = float.MaxValue;
            foreach (var c in salle.Cells) { float d = (c - salle.CenterCell).sqrMagnitude; if (d < bd) { bd = d; best = c; } }
            Vector3 centre = plan.CellCenter(best, salle.Floor + 1.7f);
            if (!Physics.Raycast(centre + Vector3.up * 1.5f, Vector3.down, out RaycastHit g0, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
            centre.y = g0.point.y + 1.7f;
            float sol = centre.y - 1.7f;
            float plafond = Physics.Raycast(centre, Vector3.up, out RaycastHit hp, 40f, ~0, QueryTriggerInteraction.Ignore) ? hp.point.y : sol + 12f;
            var murs = new List<RaycastHit>();
            for (int k = 0; k < 8; k++) if (Mur(centre, Quaternion.Euler(0, k * 45f, 0) * Vector3.forward, out RaycastHit h)) murs.Add(h);
            if (murs.Count < 3) continue;
            murs = murs.OrderByDescending(h => h.distance).ToList();
            if (murs[0].distance < 6f || murs[0].distance > 22f) continue;
            if (!murs.Any(h => Mathf.Abs(Vector3.Dot(h.normal, murs[0].normal)) < 0.3f)) continue;
            int si = faites++;
            var fond = murs[0];
            var cote = murs.First(h => Vector3.Dot(h.normal, fond.normal) < 0.3f && Vector3.Dot(h.normal, fond.normal) > -0.3f);
            string tag = si == 0 ? "salle" : "portes";
            _log.Add($"{acte} {tag} : salle {salle.Function} {salle.Cells.Count} cases, plafond {plafond - sol:F1} m, fond à {fond.distance:F1} m");
            Vector3 vueFond = new Vector3(fond.point.x, sol + 3.5f, fond.point.z);
            Vector3 vueCote = new Vector3(cote.point.x, sol + 2.5f, cote.point.z);

            if (si == 0)
            {
                await Snap($"{acte}_salle_avant", centre + fond.normal * 3f, vueFond);
                await Snap($"{acte}_cote_avant", centre, vueCote);
                Quaternion face = Quaternion.LookRotation(fond.normal);                // les pièces regardent vers l'intérieur
                Quaternion faceCote = Quaternion.LookRotation(cote.normal);
                Vector3 pf = new Vector3(fond.point.x, sol, fond.point.z);
                Vector3 pc = new Vector3(cote.point.x, sol, cote.point.z);
                Vector3 lat = Vector3.Cross(Vector3.up, fond.normal).normalized;
                Vector3 latC = Vector3.Cross(Vector3.up, cote.normal).normalized;
                if (acte == "Chateau")
                {
                    Pose(P(acte, "Fenetre_Lancette_Double"), pf + fond.normal * 0.02f + Vector3.up * 2.5f, face, poses);
                    Pose(P(acte, "Applique_2Bougies"), pf + fond.normal * 0.02f + lat * 6f + Vector3.up * 2.3f, face, poses);
                    Pose(P(acte, "Applique_2Bougies"), pf + fond.normal * 0.02f - lat * 6f + Vector3.up * 2.3f, face, poses);
                    Pose(P(acte, "Torche_Murale"), pc + cote.normal * 0.02f + latC * 4f + Vector3.up * 2.2f, faceCote, poses);
                    Pose(P(acte, "Torche_Murale"), pc + cote.normal * 0.02f - latC * 4f + Vector3.up * 2.2f, faceCote, poses);
                    Pose(P(acte, "Escalier_Droit_4m"), pc + cote.normal * 0.1f, faceCote * Quaternion.Euler(0, 90, 0), poses);
                    var ch = P(acte, "Chandelier_Moyen");
                    if (ch) { var cg = Pose(ch, new Vector3(centre.x, plafond, centre.z), Quaternion.identity, poses); }
                    Pose(P(acte, "Candelabre_Pied"), centre + fond.normal * -4f + lat * 3f - Vector3.up * 1.7f, face, poses);
                }
                else if (acte == "Caves")
                {
                    Pose(P("Cave", "MurEscalade_12m"), pf + fond.normal * 0.05f, face, poses);
                    Pose(P("Cave", "Puits"), new Vector3(centre.x, sol, centre.z) + fond.normal * -4f, Quaternion.identity, poses);
                    Pose(P("Cave", "Echelle_6m"), pc + cote.normal * 0.05f + latC * 3f, faceCote, poses);
                    Pose(P("Cave", "Balcon_Bois"), pc + cote.normal * 0.05f - latC * 3f + Vector3.up * 3.5f, faceCote, poses);
                    Pose(P("Cave", "Escalier_Colimacon"), new Vector3(centre.x, sol, centre.z) + lat * 5f, Quaternion.identity, poses);
                }
                else
                {
                    Pose(P(acte, "MurEnceinte_Tour"), pf + fond.normal * 0.05f, face, poses);
                    Pose(P(acte, "Puits"), new Vector3(centre.x, sol, centre.z) + fond.normal * -4f, Quaternion.identity, poses);
                    Pose(P(acte, "Escalier_Droit_3m"), pc + cote.normal * 0.1f, faceCote * Quaternion.Euler(0, 90, 0), poses);
                    Pose(P(acte, "Terrasse_8m"), new Vector3(centre.x, sol, centre.z) + lat * 6f, face, poses);
                }
                await Task.Delay(100);
                await Snap($"{acte}_salle_apres", centre + fond.normal * 3f, vueFond);
                await Snap($"{acte}_cote_apres", centre, vueCote);
                await Snap($"{acte}_fond_pres", new Vector3(fond.point.x, sol + 1.7f, fond.point.z) + fond.normal * 4.5f, vueFond - Vector3.up * 1f);
                await Snap($"{acte}_cote_pres", new Vector3(cote.point.x, sol + 1.7f, cote.point.z) + cote.normal * 4.5f, vueCote);
            }
            else
            {
                // portes : sortie, verrouillée, cellule du traqueur ; 0 / 50 / 100 %
                Quaternion face = Quaternion.LookRotation(fond.normal);
                Vector3 pf = new Vector3(fond.point.x, sol, fond.point.z);
                Vector3 lat = Vector3.Cross(Vector3.up, fond.normal).normalized;
                var portes = new List<GameObject>();
                string sortie = acte == "Chateau" ? "PorteSortie" : "PorteSortie";
                var ps = Pose(P(acte == "Caves" ? "Cave" : acte, sortie), pf + fond.normal * 0.1f - lat * 7f, face, poses);
                var pv = Pose(P(acte == "Caves" ? "Cave" : acte, "PorteVerrouillee"), pf + fond.normal * 0.1f + lat * 7f, face, poses);
                foreach (var k in new[] { 0f, 0.5f, 1f })
                {
                    foreach (var o in poses.Where(x => x).Select(x => x.GetComponent<OuverturePorte>()).Where(x => x)) o.Pose(k);
                    await Snap($"{acte}_portes_{Mathf.RoundToInt(k * 100):000}", centre + fond.normal * Mathf.Min(fond.distance * 0.5f, 6f), new Vector3(fond.point.x, sol + 3f, fond.point.z));
                }
                if (ps) await Snap($"{acte}_porte_sortie_pres", pf + fond.normal * 6f - lat * 7f + Vector3.up * 1.7f, pf - lat * 7f + Vector3.up * 3f);
                if (pv) await Snap($"{acte}_porte_verrouillee_pres", pf + fond.normal * 5f + lat * 7f + Vector3.up * 1.7f, pf + lat * 7f + Vector3.up * 2f);
            }
            foreach (var g in poses) if (g) Object.Destroy(g);
            poses.Clear();
        }
        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, $"points_{acte}.txt"), _log);
        return string.Join("\n", _log);
    }
}
