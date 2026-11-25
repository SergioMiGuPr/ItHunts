// Lot 3 (nuit 5) : captures en jeu des nouvelles pièces, posées dans une vraie salle générée de l'acte (seed 4242, éclairage du
// jeu, lanterne du joueur, une torche murale du jeu à côté de la pièce) : « avant » (salle vide), puis chaque pièce de loin
// (distance de jeu) et de près. Pièces murales (fenêtres, rebords, balcons) scellées au mur du fond, les autres au centre.
//  1) CapturesJeu.Prep("Assets/Scenes/Acte_2_Chateau.unity", 4242) ; playmode start
//  2) unity command script run --file CapturesLot3N5.cs --entry CapturesLot3N5.Shoot --args '["<dossier>", "Chateau", "Fenetre_Vue_Double,…"]'
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItHunts.Level.Acts;
using ItHunts.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CapturesLot3N5
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

    static string Kit(string acte) => acte == "Cave" ? "KitCaves" : "Kit" + acte;
    static GameObject P(string acte, string nom) => nom.StartsWith("Coffre")
        ? AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Prefabs/Props/P_Prop_{nom}.prefab")
        : AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Prefabs/{Kit(acte)}/{acte}_{nom}.prefab");
    static bool Murale(string nom) => nom.StartsWith("Fenetre") || nom.StartsWith("Rebord") || nom.StartsWith("Balcon") || nom.StartsWith("Lisiere") ||
                                      nom.StartsWith("PorteVerrouillee");

    static async Task Snap(string nom, Vector3 oeil, Vector3 regard)
    {
        if (_player != null)
        {
            var cc = _player.GetComponent<CharacterController>(); if (cc) cc.enabled = false;
            _player.position = oeil - Vector3.up * 1.6f; if (cc) cc.enabled = true;
        }
        _cam.transform.SetPositionAndRotation(oeil, Quaternion.LookRotation(regard - oeil));
        Time.timeScale = 1f; await Task.Delay(250); Time.timeScale = 0f; await Task.Delay(60);
        _cam.targetTexture = _rt; _cam.Render(); _cam.Render();
        RenderTexture.active = _rt; _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0); _tex.Apply();
        RenderTexture.active = null; _cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(_dir, nom + ".jpg"), _tex.EncodeToJPG(92));
        _log.Add($"{nom} : {oeil.ToString("F1")} -> {regard.ToString("F1")}");
    }

    static bool Mur(Vector3 o, Vector3 d, out RaycastHit h) =>
        Physics.Raycast(o, d, out h, 60f, ~0, QueryTriggerInteraction.Ignore) && Mathf.Abs(h.normal.y) < 0.3f;

    static Bounds Boite(GameObject g)
    {
        var rs = g.GetComponentsInChildren<MeshRenderer>().Where(r => r.enabled && !r.name.StartsWith("Col_")).ToArray();
        var b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.one);
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    public static async Task<string> Shoot(string dossier, string acte, string pieces)
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
        // mains, orbe et arme du joueur : leurs calques sont retirés du rendu (image de magasin du décor seul)
        for (int i = 0; i < 32; i++) if (LayerMask.LayerToName(i).StartsWith("FirstPerson")) masque &= ~(1 << i);
        _cam.cullingMask = masque; _cam.enabled = false;
        // ennemis et vagues coupés : rien ne passe devant les pièces
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            string tn = mb.GetType().Name;
            if (tn.Contains("Ghoul") || tn.Contains("Goule") || tn.Contains("Stalker") || tn.Contains("Enemy") || tn.Contains("Flyer") ||
                tn.Contains("Volant") || tn.Contains("WaveDirector") || tn.Contains("Spawner"))
                mb.gameObject.SetActive(false);
        }
        foreach (var ag in Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None)) ag.gameObject.SetActive(false);
        foreach (var l in src.GetComponentsInChildren<Light>(true)) { var lp = l.transform.localPosition; var lr = l.transform.localRotation; l.transform.SetParent(go.transform, false); l.transform.localPosition = lp; l.transform.localRotation = lr; }
        foreach (var r in src.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        if (_player) foreach (var r in _player.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) cv.enabled = false;
        _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        _dir = dossier; Directory.CreateDirectory(dossier);
        Time.timeScale = 0f;

        var plan = ActLevel.Current.Plan;
        var salles = plan.Rooms.Where(r => !r.Special).OrderByDescending(r => r.Cells.Count).ToList();
        foreach (var salle in salles)
        {
            Vector2Int best = salle.Cells[0]; float bd = float.MaxValue;
            foreach (var c in salle.Cells) { float d = (c - salle.CenterCell).sqrMagnitude; if (d < bd) { bd = d; best = c; } }
            Vector3 centre = plan.CellCenter(best, salle.Floor + 1.7f);
            if (!Physics.Raycast(centre + Vector3.up * 1.5f, Vector3.down, out RaycastHit g0, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
            float sol = g0.point.y; centre.y = sol + 1.7f;
            float plafond = Physics.Raycast(centre, Vector3.up, out RaycastHit hp, 60f, ~0, QueryTriggerInteraction.Ignore) ? hp.point.y : sol + 14f;
            var murs = new List<RaycastHit>();
            for (int k = 0; k < 8; k++) if (Mur(centre, Quaternion.Euler(0, k * 45f, 0) * Vector3.forward, out RaycastHit h)) murs.Add(h);
            if (murs.Count < 3) continue;
            var fond = murs.OrderByDescending(h => h.distance).First();
            if (fond.distance < 9f) continue;
            _log.Add($"{acte} : salle {salle.Function} {salle.Cells.Count} cases, plafond {plafond - sol:F1} m, fond à {fond.distance:F1} m");
            Quaternion face = Quaternion.LookRotation(fond.normal);
            Vector3 pf = new Vector3(fond.point.x, sol, fond.point.z);
            Vector3 lat = Vector3.Cross(Vector3.up, fond.normal).normalized;
            // torche du jeu (même réglage que les torches des salles : orange, 1,8, 11 m) à côté de la pièce
            var torche = new GameObject("TorcheCapture").AddComponent<Light>();
            torche.type = LightType.Point; torche.color = new Color(1f, 0.36f, 0.16f); torche.intensity = 1.8f; torche.range = 11f; torche.shadows = LightShadows.Soft;
            // deux autres torches du jeu de part et d'autre (une salle éclairée comme celles des cartes)
            var t2 = Object.Instantiate(torche.gameObject).GetComponent<Light>(); var t3 = Object.Instantiate(torche.gameObject).GetComponent<Light>();
            t2.shadows = LightShadows.None; t3.shadows = LightShadows.None;
            // relecture : sous trois torches orange, les matériaux saturaient en rouge. Une torche du jeu garde le chaud ; les deux
            // autres deviennent la lumière froide de la lune (comme le rayon de lune des fenêtres et la lune de l'acte III)
            foreach (var l in new[] { t2, t3 }) { l.color = new Color(0.55f, 0.62f, 0.85f); l.intensity = 1.4f; l.range = 26f; }
            var avantVue = centre + fond.normal * Mathf.Min(fond.distance - 8f, 4f);
            torche.transform.position = pf + fond.normal * 1.2f + lat * 4.5f + Vector3.up * 2.6f;
            await Snap($"{acte}_avant_mur", centre, pf + Vector3.up * 3f);
            foreach (var nom in pieces.Split(','))
            {
                if (nom == "SolMelange")
                {
                    // sol mélangé : dalle de 30 × 30 m posée 1 cm au-dessus du sol de la salle ; avant / après au même point
                    var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Kits/M_Jardin_SolMelange.mat");
                    Vector3 o1 = centre + fond.normal * 6f + Vector3.up * 0.3f;
                    await Snap($"{acte}_SolMelange_avant", o1, new Vector3(centre.x, sol, centre.z) - fond.normal * 6f);
                    var dalle = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    Object.Destroy(dalle.GetComponent<Collider>());
                    dalle.transform.position = new Vector3(centre.x, sol + 0.01f, centre.z); dalle.transform.localScale = new Vector3(3f, 1f, 3f);
                    dalle.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    await Snap($"{acte}_SolMelange_apres", o1, new Vector3(centre.x, sol, centre.z) - fond.normal * 6f);
                    await Snap($"{acte}_SolMelange_pres", centre + Vector3.up * 0.2f, new Vector3(centre.x, sol, centre.z) - fond.normal * 3f + lat * 1f);
                    Object.Destroy(dalle);
                    continue;
                }
                if (nom == "CompoPont")
                {
                    // verticalité : deux salles en hauteur (plancher à 6 m) reliées par le pont suspendu, vues du sol
                    var sh = P(acte, "SalleHaute_8x8"); var pt = P(acte, "PontSuspendu_12m");
                    if (sh == null || pt == null) continue;
                    Vector3 c0 = new Vector3(centre.x, sol, centre.z) - fond.normal * 2f;
                    var ga = (GameObject)Object.Instantiate(sh, c0 - lat * 10.35f, face);
                    var gb = (GameObject)Object.Instantiate(sh, c0 + lat * 10.35f, face);
                    var gp = (GameObject)Object.Instantiate(pt, c0 + Vector3.up * 6f, face);
                    foreach (var gg in new[] { ga, gb, gp }) foreach (var c in gg.GetComponentsInChildren<Collider>()) c.enabled = false;
                    torche.transform.position = c0 + fond.normal * 3f + Vector3.up * 4f;
                    await Snap($"{acte}_CompoPont_loin", c0 + fond.normal * 17f + lat * 3f + Vector3.up * 0f, c0 + Vector3.up * 5.5f);
                    await Snap($"{acte}_CompoPont_dessous", c0 + fond.normal * 5f + lat * 1f, c0 + Vector3.up * 6f - fond.normal * 1f);
                    Object.Destroy(ga); Object.Destroy(gb); Object.Destroy(gp);
                    continue;
                }
                var pfb = P(acte, nom.Trim());
                if (pfb == null) { _log.Add("absent : " + nom); continue; }
                bool murale = Murale(nom);
                Vector3 pos = murale ? pf + fond.normal * 0.03f : new Vector3(centre.x, sol, centre.z) - fond.normal * 0f;
                if (nom.StartsWith("PorteVerrouillee")) pos = pf + fond.normal * 1.02f;      // pièce de mur : son épaisseur (1 m) devant le mur
                if (nom.StartsWith("Lisiere")) pos = pf + fond.normal * 7.5f;               // la forêt s'étend derrière (6.5 m)
                Quaternion rot = face;
                if (nom.StartsWith("Rebord") || nom.StartsWith("Balcon")) pos += Vector3.up * 3.2f;
                if (nom.StartsWith("PlateformeTraction")) pos += Vector3.up * Mathf.Min(6.5f, plafond - sol - 4.6f);
                float levee = nom.StartsWith("Sol_Fosse") ? 4.15f : nom.StartsWith("Sol_Puits") ? 2.45f : 0f;   // sols percés : le trou se voit d'en haut
                pos += Vector3.up * levee;
                var g = (GameObject)Object.Instantiate(pfb, pos, rot);
                foreach (var c in g.GetComponentsInChildren<Collider>()) c.enabled = false;
                var b = Boite(g);
                float taille = Mathf.Max(b.size.x, b.size.y, b.size.z);
                Vector3 cible = b.center;
                if (!murale) torche.transform.position = b.center + fond.normal * (b.extents.z + 2.5f) + lat * (b.extents.x + 1.5f) + Vector3.up * 1.5f;
                else torche.transform.position = pf + fond.normal * 1.2f + lat * (b.extents.x + 1f) + Vector3.up * 2.6f;
                t2.transform.position = b.center - lat * (b.extents.x + 4f) + fond.normal * 4f + Vector3.up * (b.extents.y + 6f);
                t3.transform.position = new Vector3(b.center.x, sol + b.size.y + 8f, b.center.z) + fond.normal * (b.extents.z + 8f);
                // grandes pièces de plein air (lisière, arbre) : la lune porte plus loin et plus fort
                float lune = taille > 8f ? 3.2f : 1.4f;
                t2.intensity = lune; t3.intensity = lune; t2.range = t3.range = taille > 8f ? 45f : 26f;
                Vector3 axe = (fond.normal + lat * 0.35f).normalized;
                float loin = Mathf.Min(fond.distance + (murale ? 0f : -2f), taille * 0.95f + 4f);
                Vector3 oeilLoin = new Vector3(cible.x, sol + levee + 1.7f, cible.z) + axe * loin;
                if (nom.StartsWith("Fenetre"))
                {
                    // fenêtres : cadrage fixe depuis leur pied (vue de la salle, en levant les yeux vers la baie)
                    cible = pos + Vector3.up * (b.size.y * 0.55f) + fond.normal * 0.8f;
                    oeilLoin = pos + fond.normal * 12f + lat * 3f + Vector3.up * 1.7f;
                }
                await Snap($"{acte}_{nom}_loin", oeilLoin, cible);
                float pres = Mathf.Max(3f, taille * 0.55f + 1.5f);
                Vector3 oeilPres = new Vector3(cible.x, sol + levee + 1.7f, cible.z) + (fond.normal - lat * 0.4f).normalized * pres;
                if (levee > 0f) { oeilLoin = new Vector3(pos.x, pos.y + 1.7f, pos.z) + axe * 5.5f; oeilPres = new Vector3(pos.x, pos.y + 1.7f, pos.z) + fond.normal * 3.2f; cible = pos - Vector3.up * 1.5f; }
                if (nom.StartsWith("Fenetre")) oeilPres = pos + fond.normal * 5.5f - lat * 1.2f + Vector3.up * 1.7f;
                if (nom.StartsWith("PorteVerrouillee")) { oeilPres = new Vector3(cible.x, sol + 1.7f, cible.z) + fond.normal * 6.5f; cible = new Vector3(cible.x, sol + 2.4f, cible.z); }
                await Snap($"{acte}_{nom}_pres", oeilPres, cible + (nom.StartsWith("Fenetre") ? Vector3.up * 2f : Vector3.zero));
                var ouv = g.GetComponent<ItHunts.Art.VFX.OuverturePorte>();
                if (ouv != null)
                    foreach (var k in new[] { 0.2f, 0.55f, 1f })
                    {
                        ouv.Pose(k);
                        await Snap($"{acte}_{nom}_ouverture_{Mathf.RoundToInt(k * 100):000}", oeilPres + fond.normal * 1.5f, cible);
                    }
                Object.Destroy(g);
                await Task.Delay(50);
            }
            Object.Destroy(torche.gameObject); Object.Destroy(t2.gameObject); Object.Destroy(t3.gameObject);
            break;
        }
        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, $"points_{acte}.txt"), _log);
        return string.Join("\n", _log);
    }
}
