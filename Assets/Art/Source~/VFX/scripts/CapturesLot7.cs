// Lot 7 (nuit 4) : matériaux des annonces et captures en jeu dans l'arène du boss (Acte_4_Boss, éclairage du jeu, lanterne
// du joueur), sur de VRAIES attaques du boss (déclenchées par réflexion sur FinalBoss), avant (marques de main) / après.
//  1) unity command script run --file CapturesLot7.cs --entry CapturesLot7.Materiaux
//  2) scène Assets/Scenes/Acte_4_Boss.unity ouverte ; unity command editor.playmode start
//  3) unity command script run --file CapturesLot7.cs --entry CapturesLot7.Shoot --args '["docs/art/rendus/Nuit4/Lot7"]' --timeout 600
//  4) unity command editor.playmode stop
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ItHunts.Art.VFX;
using ItHunts.Enemies;
using ItHunts.Level.Acts;
using ItHunts.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CapturesLot7
{
    const string Dossier = "Assets/Art/VFX/Materiaux/";

    public static string Materiaux()
    {
        var coque = Shader.Find("ItHunts/VFX/ContourCoque");
        var a = Mat(Dossier + "M_VFX_Volant_Annonce.mat", coque);
        a.SetColor("_Couleur", new Color(0.62f, 0.08f, 0.06f)); a.SetFloat("_Epaisseur", 0.022f); a.SetFloat("_MinPx", 3f); a.SetFloat("_Opacite", 1f);
        a.SetFloat("_Pulsation", 2.2f); a.SetFloat("_PulsationMin", 0.35f);
        var p = Mat(Dossier + "M_VFX_Prise_Contour.mat", coque);
        p.SetColor("_Couleur", new Color(0.36f, 0.33f, 0.29f)); p.SetFloat("_Epaisseur", 0.02f); p.SetFloat("_Opacite", 0.75f);
        p.SetFloat("_Pulsation", 0f); p.SetFloat("_MinPx", 2f);
        var sol = Shader.Find("ItHunts/VFX/MarquageSol");
        var m = Mat("Assets/Art/VFX/Resources/M_VFX_Marquage_Boss.mat", sol);
        var defaut = new Material(sol); m.CopyPropertiesFromMaterial(defaut); Object.DestroyImmediate(defaut);   // une seule source : le shader
        var ap = Mat("Assets/Art/VFX/Resources/M_VFX_Apparition.mat", sol);
        ap.SetFloat("_Forme", 0); ap.SetFloat("_Progression", 1); ap.SetFloat("_Intensite", 0.2f); ap.SetFloat("_Pulsation", 1.2f);
        ap.SetFloat("_BordM", 0.06f); ap.SetFloat("_Fondu", 1);
        foreach (var x in new[] { a, p, m, ap }) EditorUtility.SetDirty(x);
        AssetDatabase.SaveAssets();
        return "ok";
    }

    static Material Mat(string chemin, Shader sh)
    {
        // matériau repris à neuf depuis le shader (aucune propriété héritée d'un ancien shader), GUID conservé
        var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
        if (m == null) { m = new Material(sh) { name = Path.GetFileNameWithoutExtension(chemin) }; AssetDatabase.CreateAsset(m, chemin); return m; }
        var propre = new Material(sh);
        m.shader = sh; m.CopyPropertiesFromMaterial(propre);
        var so = new SerializedObject(m);
        foreach (var liste in new[] { "m_SavedProperties.m_Floats", "m_SavedProperties.m_Colors", "m_SavedProperties.m_TexEnvs" })
        {
            var pr = so.FindProperty(liste);
            for (int i = pr.arraySize - 1; i >= 0; i--)
                if (!propre.HasProperty(pr.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue)) pr.DeleteArrayElementAtIndex(i);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Object.DestroyImmediate(propre);
        return m;
    }

    static Camera PlayerCam()
    {
        foreach (var p in PlayerHealth.All)
        {
            var pi = p != null ? p.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            if (pi != null && pi.camera != null) return pi.camera;
        }
        return Camera.main;
    }

    static Camera _cam; static RenderTexture _rt; static Texture2D _tex; static string _dir; static Transform _player;
    static readonly List<string> _log = new List<string>();

    static float Sol(Vector3 p) =>
        Physics.Raycast(p + Vector3.up * 10f, Vector3.down, out RaycastHit h, 40f, ~0, QueryTriggerInteraction.Ignore) ? h.point.y : p.y;

    static void Joueur(Vector3 pied, Vector3 regard)
    {
        if (_player == null) return;
        var cc = _player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        _player.position = pied;
        Vector3 f = regard - pied; f.y = 0;
        if (f.sqrMagnitude > 0.01f) _player.rotation = Quaternion.LookRotation(f);
        if (cc) cc.enabled = true;
    }

    static void Cam(Vector3 oeil, Vector3 regard) => _cam.transform.SetPositionAndRotation(oeil, Quaternion.LookRotation(regard - oeil));

    static async Task Snap(string nom)
    {
        await Task.Delay(120);
        _cam.targetTexture = _rt;
        _cam.Render(); _cam.Render();
        RenderTexture.active = _rt;
        _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0); _tex.Apply();
        RenderTexture.active = null; _cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(_dir, nom + ".jpg"), _tex.EncodeToJPG(92));
        _log.Add(nom + " : cam " + _cam.transform.position.ToString("F1") + " -> " + _cam.transform.forward.ToString("F2"));
    }

    static List<Renderer> _corps; static Camera _src;
    /// <summary>Corps et mains du joueur : visibles dans la vue du joueur, masqués pour les vues extérieures.</summary>
    static void Corps(bool visible)
    {
        if (_corps == null)
            _corps = _player.GetComponentsInChildren<Renderer>(true).Concat(_src.GetComponentsInChildren<Renderer>(true))
                .Where(r => r.enabled).Distinct().ToList();                        // corps, mains et orbe (enfants de la caméra du joueur)
        foreach (var r in _corps) if (r) r.enabled = visible;
    }

    static readonly BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Instance;

    public static async Task<string> Shoot(string dossier)
    {
        _log.Clear(); _corps = null;
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 240f && (ActLevel.Current == null || ActLevel.Current.Loading || PlayerCam() == null)) await Task.Delay(500);
        await Task.Delay(1500);
        Camera src = PlayerCam(); _src = src;
        _player = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0].transform : null;
        var target = PlayerHealth.All[0];
        var go = new GameObject("CaptureCam");
        _cam = go.AddComponent<Camera>(); _cam.CopyFrom(src);
        var dst = go.AddComponent<UniversalAdditionalCameraData>(); var sd = src.GetComponent<UniversalAdditionalCameraData>();
        if (sd != null) { dst.renderPostProcessing = sd.renderPostProcessing; dst.antialiasing = sd.antialiasing; dst.SetRenderer(0); dst.renderShadows = sd.renderShadows; dst.volumeLayerMask = sd.volumeLayerMask; }
        _cam.cullingMask = src.cullingMask & ~LayerMask.GetMask("UI"); _cam.enabled = false;
        foreach (var l in src.GetComponentsInChildren<Light>(true)) { var lp = l.transform.localPosition; var lr = l.transform.localRotation; l.transform.SetParent(go.transform, false); l.transform.localPosition = lp; l.transform.localRotation = lr; }
        _rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); _tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        _dir = dossier; Directory.CreateDirectory(dossier);

        // ---------- le joueur près d'un brasero (18°, 42 m) ; le combat démarre ----------
        Vector3 dB = Quaternion.Euler(0f, 18f, 0f) * Vector3.forward;
        Vector3 P = dB * 33f; P.y = Sol(P);
        Vector3 versCentre = -dB;
        Joueur(P, Vector3.zero);
        Time.timeScale = 1f;
        FinalBoss boss = null;
        t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 20f && (boss == null || !boss.Started)) { boss = Object.FindFirstObjectByType<FinalBoss>(); Joueur(P, Vector3.zero); await Task.Delay(200); }
        if (boss == null || !boss.Started) return "le boss n'a pas démarré";
        foreach (var cv in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) cv.enabled = false;
        var start = typeof(FinalBoss).GetMethod("StartAttack", NP);
        var clear = typeof(FinalBoss).GetMethod("ClearTelegraphs", NP);
        var tele = (List<GameObject>)typeof(FinalBoss).GetField("_telegraphs", NP).GetValue(boss);
        var pCurrent = typeof(FinalBoss).GetProperty("Current");

        Vector3 B = P + versCentre * 13f; B.y = Sol(B);
        var attaques = new[] { FinalBoss.Attack.Prise, FinalBoss.Attack.Balayage, FinalBoss.Attack.Charge, FinalBoss.Attack.Debris };
        foreach (var a in attaques)
        {
            Time.timeScale = 0f;
            clear.Invoke(boss, null); pCurrent.SetValue(boss, FinalBoss.Attack.Aucune);
            boss.transform.SetPositionAndRotation(B, Quaternion.LookRotation(P - B));
            Joueur(P, B);
            Time.timeScale = 1f;
            start.Invoke(boss, new object[] { a, target, Time.time });
            float total = boss.TelegraphLeft;
            float attente = total * 0.55f;
            float r0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - r0 < attente) { Joueur(P, B); await Task.Delay(30); }
            Time.timeScale = 0f;
            float fait = 1f - boss.TelegraphLeft / Mathf.Max(total, 0.01f);
            string nom = a.ToString();
            // marques de main -> les miennes, même géométrie
            var miennes = new List<MarquageBoss>();
            var centre = Vector3.zero; int n = 0;
            foreach (var g in tele.Where(x => x != null))
            {
                var t = g.transform; centre += t.position; n++;
                if (g.name.Contains("cercle")) miennes.Add(MarquageBoss.Cercle(t.position - Vector3.up * 0.06f, t.localScale.x * 0.5f, 1f));
                else if (g.name.Contains("couloir")) miennes.Add(MarquageBoss.Couloir(t.position - Vector3.up * 0.06f - t.forward * t.localScale.z * 0.5f, t.forward, t.localScale.z, t.localScale.x, 1f));
            }
            if (a == FinalBoss.Attack.Balayage)
            {
                var bp = boss.transform.position; bp.y = Sol(bp);
                miennes.Add(MarquageBoss.Cone(bp, boss.transform.forward, 15f, 45f, 1f));
            }
            centre = n > 0 ? centre / n : B;
            foreach (var m in miennes) { m.enabled = false; m.gameObject.SetActive(false); }
            // vues : joueur (œil 1,7 m, vers le boss) et témoin (7 m de haut, derrière le joueur)
            Vector3 oeil = P + Vector3.up * 1.7f, regardJeu = Vector3.Lerp(B + Vector3.up * 2.5f, centre, 0.5f);
            Vector3 temoin = P - versCentre * 9f + Vector3.up * 7f; temoin.y = Sol(temoin) + 7f;
            Corps(true); Cam(oeil, regardJeu); await Snap($"Boss_{nom}_jeu_avant");
            Corps(false); Cam(temoin, centre); await Snap($"Boss_{nom}_temoin_avant");
            foreach (var g in tele.Where(x => x != null)) g.GetComponent<Renderer>().enabled = false;
            foreach (var m in miennes) m.gameObject.SetActive(true);
            foreach (var (k, suf) in new[] { (fait, "annonce"), (0.92f, "montee"), (1.03f, "impact") })
            {
                foreach (var m in miennes) m.Pose(k);
                Corps(true); Cam(oeil, regardJeu); await Snap($"Boss_{nom}_jeu_{suf}");
                Corps(false); Cam(temoin, centre); await Snap($"Boss_{nom}_temoin_{suf}");
            }
            // de près : 2,2 m du bord de la première marque, vers le bord (le joueur et ses mains masqués : caméra hors de son corps)
            Corps(false);
            if (miennes.Count > 0)
            {
                var mt = miennes[0].transform;
                Vector3 bord = mt.position + (a == FinalBoss.Attack.Charge ? mt.right * mt.localScale.x * 0.5f : (oeil - mt.position).normalized * 0) ;
                if (a != FinalBoss.Attack.Charge) { Vector3 v = P - mt.position; v.y = 0; bord = mt.position + (v.sqrMagnitude > 0.01f ? v.normalized : Vector3.forward) * mt.localScale.x * 0.5f; }
                Vector3 dehors = bord - mt.position; dehors.y = 0; dehors = dehors.sqrMagnitude > 0.01f ? dehors.normalized : Vector3.back;
                Vector3 e = bord + dehors * 2.2f; e.y = Sol(e) + 1.6f;
                foreach (var m in miennes) m.gameObject.SetActive(false);
                foreach (var g in tele.Where(x => x != null)) g.GetComponent<Renderer>().enabled = true;
                Cam(e, bord - dehors * 1.5f); await Snap($"Boss_{nom}_pres_avant");
                foreach (var g in tele.Where(x => x != null)) g.GetComponent<Renderer>().enabled = false;
                foreach (var m in miennes) { m.gameObject.SetActive(true); m.Pose(0.92f); }
                Cam(e, bord - dehors * 1.5f); await Snap($"Boss_{nom}_pres_montee");
            }
            Corps(true);
            foreach (var m in miennes) Object.Destroy(m.gameObject);
            _log.Add($"{nom} : annonce {total:F2} s, capturée à {fait:P0}, {n} marques de main");
        }
        clear.Invoke(boss, null); pCurrent.SetValue(boss, FinalBoss.Attack.Aucune);
        Time.timeScale = 0f;

        // ---------- volant : annonce, prise ; de près (3 m) et à distance de jeu (12 m) ----------
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Niveau/Volant.prefab");
        var matA = AssetDatabase.LoadAssetAtPath<Material>(Dossier + "M_VFX_Volant_Annonce.mat");
        var matP = AssetDatabase.LoadAssetAtPath<Material>(Dossier + "M_VFX_Prise_Contour.mat");
        Vector3 V = P + Quaternion.Euler(0, 40, 0) * versCentre * 3.2f + Vector3.up * 2.2f;
        var vol = Object.Instantiate(prefab, V, Quaternion.LookRotation(P - V));
        await Task.Delay(60);
        foreach (var mb in vol.GetComponentsInChildren<MonoBehaviour>()) if (mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("ItHunts")) mb.enabled = false;
        foreach (var rb in vol.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        vol.transform.SetPositionAndRotation(V, Quaternion.LookRotation(new Vector3(P.x - V.x, 0, P.z - V.z)));
        var tel = vol.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("Annonce"));
        var cd = vol.AddComponent<ContourDiscret>(); cd.annonce = matA; cd.prise = matP;
        var grab = vol.GetComponent<ItHunts.Orbs.Grabbable>();
        Vector3 oeilV = P + Vector3.up * 1.7f;
        Vector3 loinV = P - (V - P).normalized * 9f; loinV.y = Sol(loinV) + 1.7f;
        Vector3 tresLoinV = P - (V - P).normalized * 17f; tresLoinV.y = Sol(tresLoinV) + 1.7f;
        Corps(false);
        foreach (var (e, d) in new[] { (oeilV, "pres"), (loinV, "jeu"), (tresLoinV, "loin") })
        {
            Cam(e, V);
            if (tel) tel.enabled = false; cd.Cache(); if (grab) grab.SetGlow(Color.black);
            await Snap($"Volant_{d}_repos");
            if (tel) tel.enabled = true; await Snap($"Volant_{d}_annonce_avant"); if (tel) tel.enabled = false;
            cd.Montre(matA); await Snap($"Volant_{d}_annonce_apres"); cd.Cache();
            if (grab) grab.SetGlow(new Color(0.6f, 0.25f, 1f) * 1.2f); await Snap($"Volant_{d}_prise_avant");
            if (grab) grab.SetGlow(Color.black); cd.Montre(matP); await Snap($"Volant_{d}_prise_apres"); cd.Cache();
        }
        Object.Destroy(vol);

        // ---------- marque d'apparition (WaveDirector.ShowMarker : cylindre 1,6 × 0,01 × 1,6) ----------
        Vector3 S = P + Quaternion.Euler(0, -35, 0) * versCentre * 5f; S.y = Sol(S);
        var marque = GameObject.CreatePrimitive(PrimitiveType.Cylinder); Object.Destroy(marque.GetComponent<Collider>());
        marque.transform.position = S + Vector3.up * 0.03f; marque.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
        var mr = marque.GetComponent<MeshRenderer>(); mr.shadowCastingMode = ShadowCastingMode.Off;
        var avantM = new Material(Shader.Find("Universal Render Pipeline/Unlit")); avantM.SetColor("_BaseColor", new Color(3f, 0.6f, 0.15f));
        mr.sharedMaterial = avantM;
        Vector3 oeilS = S + (P - S).normalized * 3.5f; oeilS.y = Sol(oeilS) + 1.7f;
        Vector3 loinS = S + (P - S).normalized * 12f; loinS.y = Sol(loinS) + 1.7f;
        Cam(oeilS, S); await Snap("Apparition_pres_avant"); Cam(loinS, S); await Snap("Apparition_jeu_avant");
        mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/VFX/Resources/M_VFX_Apparition.mat");
        Cam(oeilS, S); await Snap("Apparition_pres_apres"); Cam(loinS, S); await Snap("Apparition_jeu_apres");
        Object.Destroy(marque);

        // ---------- screamer : le traqueur remplit l'écran (2,8 m, tête en haut du cadre) ----------
        var rs = boss.GetComponentsInChildren<Renderer>(); var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
        Vector3 vers = P - boss.transform.position; vers.y = 0; vers.Normalize();
        Vector3 oe = boss.transform.position + vers * 3.2f; oe.y = Sol(oe) + 1.7f;
        Vector3 tete = new Vector3(bb.center.x, bb.min.y + bb.size.y * 0.62f, bb.center.z);
        // cadrage retenu : au pied du traqueur (3,2 m), regard vers le visage (le crâne se découpe sur le ciel) ; un cadrage à hauteur
        // de tête (os SK_Traqueur_v4_Tete) montrait l'arrière du crâne dans la pose de combat
        var os = boss.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.name.EndsWith("_Tete") || x.name.ToLower().Contains("head"));
        // l'os de la tête n'est pas toujours à jour hors caméra : repli sur la hauteur connue du modèle (~9 m debout)
        tete = os != null && os.position.y > boss.transform.position.y + 3f ? os.position : boss.transform.position + Vector3.up * 8.5f;
        oe = boss.transform.position + vers * 6f; oe.y = Sol(oe) + 1.7f;
        _log.Add("screamer : cadrage au pied (5 m), regard vers la tête " + tete.ToString("F1"));
        Joueur(oe - Vector3.up * 1.6f, boss.transform.position);
        var sgo = new GameObject("Screamer");
        var vols = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None);
        if (vols.Length > 0) sgo.layer = vols[0].gameObject.layer;
        sgo.AddComponent<Volume>();
        dst.volumeLayerMask |= 1 << sgo.layer; dst.volumeTrigger = _cam.transform;
        var scr = sgo.AddComponent<ScreamerMort>();
        Cam(oe, tete);
        await Snap("Screamer_000");
        foreach (bool reduit in new[] { false, true })
            foreach (var k in reduit ? new[] { 0.175f, 0.6f, 1f } : new[] { 0.08f, 0.175f, 0.35f, 0.6f, 1f })
            {
                scr.Pose(k, reduit);
                await Snap($"Screamer_{(reduit ? "reduit" : "fort")}_{Mathf.RoundToInt(k * 1000):000}");
            }
        scr.Arrete(); Object.Destroy(sgo);

        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, "points.txt"), _log);
        return string.Join("\n", _log);
    }
}
