// Nuit 5, lot 4 : captures en jeu dans l'arène du boss (Acte_4_Boss, éclairage et post-process du jeu, lanterne du joueur),
// avant (marques rouges de main, lueur violette) / après (indices sombres de la DA), puis lien du livre, trait de prise et
// apparitions des goules.
//  1) VfxBuilder.ConstruitNuit5 (matériaux et prefabs)
//  2) scène Assets/Scenes/Acte_4_Boss.unity ouverte ; unity command editor.playmode start
//  3) unity command script run --file CapturesLot4N5.cs --entry CapturesLot4N5.Shoot --args '["docs/art/rendus/Nuit5/Lot4/Jeu"]' --timeout 900
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
using Object = UnityEngine.Object;

public static class CapturesLot4N5
{
    const string M = "Assets/Art/VFX/Materiaux/";
    const string PF = "Assets/Art/VFX/Prefabs/";
    const string FER = "Assets/Art/Materials/Bibliotheque/M_Bib_FerRouille.mat";

    static Camera PlayerCam()
    {
        foreach (var p in PlayerHealth.All)
        {
            var pi = p != null ? p.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            if (pi != null && pi.camera != null) return pi.camera;
        }
        return Camera.main;
    }

    static Camera _cam; static RenderTexture _rt; static Texture2D _tex; static string _dir; static Transform _player; static Camera _src;
    static readonly List<string> _log = new List<string>();
    static List<Renderer> _corps; static bool _masque;

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
        await Task.Delay(100);
        if (_masque) Corps(false);                      // ce que le jeu a rallumé entre-temps
        _cam.targetTexture = _rt;
        _cam.Render(); _cam.Render();
        RenderTexture.active = _rt;
        _tex.ReadPixels(new Rect(0, 0, _rt.width, _rt.height), 0, 0); _tex.Apply();
        RenderTexture.active = null; _cam.targetTexture = null;
        File.WriteAllBytes(Path.Combine(_dir, nom + ".jpg"), _tex.EncodeToJPG(92));
        _log.Add(nom + " : cam " + _cam.transform.position.ToString("F1") + " -> " + _cam.transform.forward.ToString("F2"));
    }

    /// <summary>Corps, mains, lanterne et orbe du joueur : masqués pour les vues extérieures (liste refaite à chaque masquage :
    /// le jeu rallume certains rendus, comme la flamme de la lanterne, entre deux vues).</summary>
    static void Corps(bool visible)
    {
        _masque = !visible;
        if (!visible)
        {
            // + le halo de la lanterne du joueur (système « Halos des lumières » de main, D-067) : sans joueur à l'image, pas de halo
            var halos = Object.FindObjectsByType<ParticleSystemRenderer>(FindObjectsSortMode.None).Where(r => r.name.StartsWith("Halos")).Cast<Renderer>();
            var neufs = _player.GetComponentsInChildren<Renderer>(true).Concat(_src.GetComponentsInChildren<Renderer>(true)).Concat(halos).Where(r => r.enabled).ToList();
            _corps = (_corps ?? new List<Renderer>()).Concat(neufs).Distinct().ToList();
            foreach (var r in neufs) r.enabled = false;
            foreach (var ps in _player.GetComponentsInChildren<ParticleSystem>(true)) ps.Clear(true);
        }
        else if (_corps != null) { foreach (var r in _corps) if (r) r.enabled = true; _corps = null; }
    }

    /// <summary>Avance le jeu de `s` secondes de temps de jeu (timeScale 1), puis fige (timeScale 0).</summary>
    static async Task Avance(float s)
    {
        Time.timeScale = 1f;
        float t0 = Time.time;
        float r0 = Time.realtimeSinceStartup;
        while (Time.time - t0 < s && Time.realtimeSinceStartup - r0 < s * 4f + 2f) await Task.Delay(5);
        Time.timeScale = 0f;
    }

    static GameObject Inerte(string chemin, Vector3 pos, Quaternion rot)
    {
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
        var g = Object.Instantiate(pf, pos, rot);
        foreach (var mb in g.GetComponentsInChildren<MonoBehaviour>()) if (mb.GetType().Namespace != null && mb.GetType().Namespace.StartsWith("ItHunts") && !(mb is ContourDiscret)) mb.enabled = false;
        foreach (var rb in g.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var c in g.GetComponentsInChildren<Collider>()) c.enabled = false;
        g.transform.SetPositionAndRotation(pos, rot);
        return g;
    }

    static Material Unlit(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Unlit")); m.SetColor("_BaseColor", c); return m; }

    static GameObject Plat(PrimitiveType t, Vector3 pos, Quaternion rot, Vector3 echelle, Material m)
    {
        var g = GameObject.CreatePrimitive(t); Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetPositionAndRotation(pos, rot); g.transform.localScale = echelle;
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
        return g;
    }

    /// <summary>Matériau des marques du boss avec les valeurs rouges de la nuit 4 (pour l'« avant »).</summary>
    static Material AncienRouge(Material baseM)
    {
        var m = new Material(baseM);
        m.SetColor("_CouleurBord", new Color(0.62f, 0.09f, 0.07f)); m.SetColor("_CouleurBordChaud", new Color(1f, 0.72f, 0.48f));
        m.SetColor("_CouleurFond", new Color(0.07f, 0.01f, 0.012f)); m.SetColor("_CouleurRempli", new Color(0.24f, 0.035f, 0.03f));
        m.SetColor("_CouleurEclair", new Color(0.42f, 0.11f, 0.05f)); m.SetVector("_OpaciteFond", new Vector4(0.38f, 0.6f, 0, 0)); m.SetFloat("_OpaciteBord", 1f);
        return m;
    }

    static readonly BindingFlags NP = BindingFlags.NonPublic | BindingFlags.Instance;
    /// <summary>Lanterne du hibou proposée par la DA (voir ART-HANDOFF, nuit 5, lot 4).</summary>
    static readonly Color LanterneHibou = new Color(0.36f, 0.44f, 0.58f);

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

        // point du joueur : le plus dégagé sur l'anneau de 30 m (rien à moins de 16 m vers le centre ni sur les côtés à 1,7 m de haut)
        Vector3 dB = Vector3.forward; float meilleur = -1f;
        for (int ang = 0; ang < 360; ang += 10)
        {
            Vector3 d = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
            Vector3 p0 = d * 30f; p0.y = Sol(p0) + 1.7f;
            float libre = 0f;
            foreach (float da in new[] { 0f, -35f, 35f, -60f, 60f })
            {
                Vector3 r = Quaternion.Euler(0f, da, 0f) * -d;
                libre += Physics.SphereCast(p0, 0.6f, r, out RaycastHit hh, 16f, ~0, QueryTriggerInteraction.Ignore) ? hh.distance : 16f;
            }
            if (libre > meilleur) { meilleur = libre; dB = d; }
        }
        _log.Add("joueur : cap " + Quaternion.LookRotation(dB).eulerAngles.y.ToString("F0") + "°, dégagement " + meilleur.ToString("F1"));
        Vector3 P = dB * 30f; P.y = Sol(P);
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
        var pCurrent = typeof(FinalBoss).GetProperty("Current");
        var baseMarq = Resources.Load<Material>(MarquageBoss.Ressource);
        var rouge = AncienRouge(baseMarq);
        Vector3 oeil = P + Vector3.up * 1.7f;

        // ---------- 1. boss : vraies attaques ; marques du jeu (matériau de la DA) avant (valeurs rouges) / après (sombres) ----------
        Vector3 B = P + versCentre * 13f; B.y = Sol(B);
        foreach (var a in new[] { FinalBoss.Attack.Prise, FinalBoss.Attack.Balayage, FinalBoss.Attack.Charge, FinalBoss.Attack.Debris })
        {
            Time.timeScale = 0f;
            clear.Invoke(boss, null); pCurrent.SetValue(boss, FinalBoss.Attack.Aucune);
            boss.transform.SetPositionAndRotation(B, Quaternion.LookRotation(P - B));
            Joueur(P, B);
            Time.timeScale = 1f;
            start.Invoke(boss, new object[] { a, target, Time.time });
            float total = boss.TelegraphLeft;
            float r0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - r0 < total * 0.55f) { Joueur(P, B); await Task.Delay(30); }
            Time.timeScale = 0f;
            var marques = Object.FindObjectsByType<MarquageBoss>(FindObjectsSortMode.None).ToList();
            Vector3 centre = marques.Count > 0 ? marques.Aggregate(Vector3.zero, (s, m) => s + m.transform.position) / marques.Count : B;
            Vector3 regardJeu = Vector3.Lerp(B + Vector3.up * 2.5f, centre, 0.5f);
            Vector3 temoin = P - versCentre * 9f + Vector3.up * 7f; temoin.y = Sol(temoin) + 7f;
            string nom = a.ToString();
            foreach (var (k, suf) in new[] { (-1f, "annonce"), (0.92f, "montee"), (1.03f, "impact") })
            {
                if (k >= 0f) foreach (var m in marques) if (m) m.Pose(k);
                foreach (var (mat, av) in new[] { (rouge, "avant"), (baseMarq, "apres") })
                {
                    foreach (var m in marques) if (m) m.GetComponent<MeshRenderer>().sharedMaterial = mat;
                    Corps(true); Cam(oeil, regardJeu); await Snap($"Boss_{nom}_jeu_{suf}_{av}");
                    if (suf == "montee") { Corps(false); Cam(temoin, centre); await Snap($"Boss_{nom}_temoin_{suf}_{av}"); }
                }
            }
            Corps(true);
            _log.Add($"{nom} : annonce {total:F2} s, {marques.Count} marques");
        }
        clear.Invoke(boss, null); pCurrent.SetValue(boss, FinalBoss.Attack.Aucune);
        foreach (var m in Object.FindObjectsByType<MarquageBoss>(FindObjectsSortMode.None)) Object.Destroy(m.gameObject);
        // le boss loin derrière et à l'arrêt (il ne cache rien, il n'attaque plus pendant les avances du temps)
        boss.enabled = false;
        foreach (var r in boss.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        Time.timeScale = 0f;
        Corps(false);

        // ---------- 2. goule qui bondit : disque rouge de main (telegraphColor 3, 0.4, 0.2) / indice sombre ----------
        Vector3 G = P + Quaternion.Euler(0, 25, 0) * versCentre * 6f; G.y = Sol(G);
        var goule = Inerte("Assets/_Project/Prefabs/Niveau/Goule.prefab", G, Quaternion.LookRotation(Flat(P - G)));
        await Task.Delay(80);
        var disque = goule.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("Annonce"));
        var disqueRouge = disque != null ? disque.sharedMaterial : null;
        var indiceC = AssetDatabase.LoadAssetAtPath<Material>(M + "M_VFX_Indice_Cercle.mat");
        var indiceR = AssetDatabase.LoadAssetAtPath<Material>(M + "M_VFX_Indice_Rect.mat");
        if (disque != null && disqueRouge == null) disqueRouge = Unlit(new Color(3f, 0.4f, 0.2f));
        Vector3 oeilG = G + Flat(P - G).normalized * 4f; oeilG.y = Sol(oeilG) + 1.7f;
        Vector3 jeuG = G + Flat(P - G).normalized * 11f; jeuG.y = Sol(jeuG) + 1.7f;
        foreach (var (e, d) in new[] { (oeilG, "pres"), (jeuG, "jeu") })
        {
            Cam(e, G + Vector3.up * 0.5f);
            if (disque) { disque.enabled = true; disque.sharedMaterial = disqueRouge; }
            await Snap($"Goule_Bond_{d}_avant");
            if (disque) disque.sharedMaterial = indiceC;
            await Snap($"Goule_Bond_{d}_apres");
        }
        _log.Add("goule : disque " + (disque != null ? disque.name + " " + disque.transform.lossyScale.ToString("F2") : "absent"));

        // ---------- 3. volant avant le piqué : sphère rouge de main / contour d'encre fumée ----------
        Vector3 V = P + Quaternion.Euler(0, -30, 0) * versCentre * 4f + Vector3.up * 2.4f;
        var vol = Inerte("Assets/_Project/Prefabs/Niveau/Volant.prefab", V, Quaternion.LookRotation(Flat(P - V)));
        await Task.Delay(80);
        var tel = vol.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("Annonce"));
        var cd = vol.AddComponent<ContourDiscret>(); cd.annonce = AssetDatabase.LoadAssetAtPath<Material>(M + "M_VFX_Volant_Annonce.mat"); cd.prise = AssetDatabase.LoadAssetAtPath<Material>(M + "M_VFX_Prise_Contour.mat");
        var grabV = vol.GetComponent<ItHunts.Orbs.Grabbable>();
        Vector3 jeuV = P - Flat(V - P).normalized * 6f; jeuV.y = Sol(jeuV) + 1.7f;
        foreach (var (e, d) in new[] { (oeil, "pres"), (jeuV, "jeu") })
        {
            Cam(e, V);
            if (tel) tel.enabled = true; await Snap($"Volant_Pique_{d}_avant");
            if (tel) tel.enabled = false; cd.Montre(cd.annonce); await Snap($"Volant_Pique_{d}_apres"); cd.Cache();
            if (grabV) grabV.SetGlow(new Color(0.6f, 0.25f, 1f) * 1.2f); await Snap($"Volant_Tenu_{d}_avant");
            if (grabV) grabV.SetGlow(Color.black); cd.Montre(cd.prise); await Snap($"Volant_Tenu_{d}_apres"); cd.Cache();
        }
        Object.Destroy(vol);

        // ---------- 4. main du traqueur : carré rouge (Lueur 4, 0.25, 0.15) et disque du revers / indices sombres ----------
        var ss = AssetDatabase.FindAssets("t:StalkerSettings").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<ItHunts.Settings.StalkerSettings>).FirstOrDefault();
        Vector3 hs = ss != null ? ss.handSize : new Vector3(4.9f, 1.5f, 4.5f);
        float backD = ss != null ? 2f * (ss.width * 0.5f + ss.backhandReach) : 21f;
        goule.SetActive(false);
        Vector3 H = P + Quaternion.Euler(0, 10, 0) * versCentre * 8f; H.y = Sol(H);
        var rot = Quaternion.LookRotation(Flat(P - H));
        var ombreM = Unlit(new Color(0.02f, 0f, 0f)); var rougeM = Unlit(new Color(4f, 0.25f, 0.15f));
        var ombre = Plat(PrimitiveType.Cube, H + Vector3.up * 0.02f, rot, new Vector3(hs.x * 1.15f, 0.02f, hs.z * 1.15f), ombreM);
        var lueur = Plat(PrimitiveType.Cube, H + Vector3.up * 0.04f, rot, new Vector3(hs.x, 0.02f, hs.z), rougeM);
        Vector3 temoinH = P + Vector3.up * 4f;
        Cam(oeil, H); await Snap("Traqueur_Main_jeu_avant"); Cam(temoinH, H); await Snap("Traqueur_Main_temoin_avant");
        Object.Destroy(ombre);
        lueur.GetComponent<MeshRenderer>().sharedMaterial = indiceR; lueur.transform.localScale = new Vector3(hs.x * 1.1f, 0.02f, hs.z * 1.1f);
        Cam(oeil, H); await Snap("Traqueur_Main_jeu_apres"); Cam(temoinH, H); await Snap("Traqueur_Main_temoin_apres");
        Object.Destroy(lueur);
        Vector3 R = P + versCentre * 9f; R.y = Sol(R);
        var revers = Plat(PrimitiveType.Cylinder, R + Vector3.up * 0.05f, Quaternion.identity, new Vector3(backD, 0.01f, backD), rougeM);
        Vector3 temoinR = P - versCentre * 6f + Vector3.up * 6f;
        Cam(oeil, R); await Snap("Traqueur_Revers_jeu_avant"); Cam(temoinR, R); await Snap("Traqueur_Revers_temoin_avant");
        revers.GetComponent<MeshRenderer>().sharedMaterial = indiceC;
        Cam(oeil, R); await Snap("Traqueur_Revers_jeu_apres"); Cam(temoinR, R); await Snap("Traqueur_Revers_temoin_apres");
        Object.Destroy(revers);

        goule.SetActive(true);
        // ---------- 5. objet tenu (vue du joueur, mains visibles) : lueur et trait violets de main / contour d'os et fil discret ----------
        Corps(true);
        _cam.transform.SetPositionAndRotation(src.transform.position, src.transform.rotation);
        Vector3 Oe = src.transform.position; Vector3 fw = Flat(src.transform.forward).normalized;
        Vector3 Cpos = Oe + fw * 3.2f + Vector3.up * -0.2f + Vector3.Cross(Vector3.up, fw) * 0.5f;
        var caisse = Inerte("Assets/Art/Prefabs/Props/P_Prop_Tonneau.prefab", Cpos, Quaternion.LookRotation(fw));
        var orbe = _src.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.ToLower().Contains("orb"));
        Vector3 depart = orbe != null ? orbe.bounds.center : Oe + fw * 0.6f + Vector3.down * 0.35f + Vector3.Cross(Vector3.up, fw) * 0.3f;
        var cdC = caisse.AddComponent<ContourDiscret>(); cdC.prise = AssetDatabase.LoadAssetAtPath<Material>(M + "M_VFX_Prise_Contour.mat");
        // avant : émission violette sur tout le modèle (grabColor 0.6, 0.25, 1 × 1.2) et trait violet (× 1.5, LineRenderer de main)
        var rends = caisse.GetComponentsInChildren<Renderer>().ToArray();
        var mpb = new MaterialPropertyBlock();
        foreach (var r in rends) { r.GetPropertyBlock(mpb); mpb.SetColor("_EmissionColor", new Color(0.6f, 0.25f, 1f) * 1.2f); r.SetPropertyBlock(mpb); }
        var tl = new GameObject("TraitMain").AddComponent<LineRenderer>();
        tl.sharedMaterial = Unlit(new Color(0.6f, 0.25f, 1f) * 1.5f); tl.widthMultiplier = 0.06f; tl.positionCount = 2; tl.SetPosition(0, depart); tl.SetPosition(1, Cpos + Vector3.up * 0.5f);
        await Snap("Prise_Objet_avant");
        Object.Destroy(tl.gameObject);
        foreach (var r in rends) { r.GetPropertyBlock(mpb); mpb.Clear(); r.SetPropertyBlock(mpb); }
        cdC.Montre(cdC.prise);
        var fil = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PF + "VFX_Trait_Prise.prefab"));
        var lf = fil.GetComponent<LienVfx>(); lf.Regle(depart, Cpos + Vector3.up * 0.5f);
        await Avance(0.05f);
        await Snap("Prise_Objet_apres");
        Object.Destroy(fil); Object.Destroy(caisse);

        // ---------- 6. livre du hibou (le hibou équipé : le livre est dans la main gauche) : lien lancé à chaque appui ;
        //            drain vers une goule, soin vers un allié (statue figée du corps du joueur), sans cible ----------
        var holder = _player.GetComponent<ItHunts.Artifacts.ArtifactHolder>();
        var hibou = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.CharacterDefinition>("Assets/_Project/Settings/Personnages/Character_hibou.asset");
        var persoAvant = holder != null ? holder.Character : null;
        // lanterne du hibou : (0.2, 0.4, 1) de main noie la salle de bleu saturé ; valeur proposée par la DA (lune froide, désaturée),
        // appliquée sur une copie en mémoire du personnage (l'asset de main n'est pas modifié)
        if (holder != null && hibou != null) { var h2 = Object.Instantiate(hibou); h2.lanternColor = LanterneHibou; holder.EquipCharacter(h2); }
        await Avance(0.4f);
        var amv = Object.FindObjectsByType<ItHunts.Artifacts.ArtifactModelView>(FindObjectsSortMode.None).FirstOrDefault();
        var fpv = amv != null ? amv.GetComponent<FirstPersonView>() : null;
        // départ du lien : centre des rendus du livre (pas le pivot du modèle, qui est au dos du livre), ramené dans le monde
        Vector3 Livre()
        {
            if (amv == null || amv.Model == null || fpv == null) return Oe + fw * 0.55f + Vector3.down * 0.3f - Vector3.Cross(Vector3.up, fw) * 0.28f;
            var rs = amv.Model.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0) return fpv.ViewToWorld(amv.Model.position);
            var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
            return fpv.ViewToWorld(bb.center);
        }
        _log.Add("livre : " + (amv != null && amv.Model != null ? amv.Model.name : "modèle absent, point de repli"));
        Vector3 droite = Vector3.Cross(Vector3.up, fw);
        Vector3 Gc = Oe + fw * 8f - droite * 1.2f; Gc.y = _player.position.y;   // même sol que le joueur (Sol() toucherait un surplomb)
        goule.transform.SetPositionAndRotation(Gc, Quaternion.LookRotation(-fw));
        if (disque) disque.enabled = false;
        // allié : le jeu n'a pas de corps de joueur vu de l'extérieur ; le soin s'accroche au point du corps d'un allié (repère vide à 1,1 m)
        Vector3 Al = Oe + fw * 7f + droite * 1.6f; Al.y = _player.position.y;
        var allie = new GameObject("Allie_Point"); allie.transform.position = Al;
        var ab = new Bounds(Al + Vector3.up * 1.1f, new Vector3(0.5f, 0.2f, 0.5f));
        var gb = goule.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds).Aggregate((x, y) => { x.Encapsulate(y); return x; });
        foreach (var (pref, nomL, cibleT, bo) in new[] { ("VFX_Lien_Drain", "Drain", goule.transform, gb), ("VFX_Lien_Soin", "Soin", allie.transform, ab) })
        {
            Vector3 touche = bo.center + Vector3.up * bo.extents.y * 0.25f;
            Vector3 dep = Livre();
            _log.Add($"{nomL} : livre {dep:F2}, cible {cibleT.position:F2}, point {touche:F2}, boîte {bo.size:F2}, caméra {_cam.transform.position:F2}");
            var l = LienVfx.Lance(AssetDatabase.LoadAssetAtPath<GameObject>(PF + pref + ".prefab"), dep, (touche - dep).normalized, 20f, cibleT, touche);
            float fait = 0f;
            foreach (var (t, suf) in new[] { (0.09f, "1_vol"), (0.21f, "2_accroche"), (0.7f, "3_tenu") })
            {
                // comme le jeu : Regle(livre, cible) à chaque image une fois le lien lancé
                Time.timeScale = 1f; float tq = Time.time; float rq = Time.realtimeSinceStartup;
                while (Time.time - tq < t - fait && Time.realtimeSinceStartup - rq < 4f) { if (l && l.Accroche) l.Regle(Livre(), touche); await Task.Delay(5); }
                Time.timeScale = 0f; fait = t;
                await Snap($"Livre_{nomL}_{suf}");
            }
            l.Ferme(); await Avance(1f);
            _log.Add($"lien {nomL} : détruit après Ferme = {l == null}");
        }
        goule.SetActive(false); allie.SetActive(false);
        {
            Vector3 dep = Livre();
            var l = LienVfx.Lance(AssetDatabase.LoadAssetAtPath<GameObject>(PF + "VFX_Lien_Drain.prefab"), dep, fw, 15f, null, Vector3.zero);
            foreach (var (t, suf) in new[] { (0.15f, "1_vol"), (0.2f, "2_bout"), (0.12f, "3_retrait") }) { await Avance(t); await Snap($"Livre_SansCible_{suf}"); }
            await Avance(1.2f);
            _log.Add("lien sans cible détruit seul : " + (l == null));
        }
        Object.Destroy(allie);
        if (holder != null && persoAvant != null) { holder.EquipCharacter(persoAvant); await Avance(0.3f); }   // lanterne d'origine pour la suite
        goule.SetActive(true);
        if (disque) disque.enabled = false;
        Corps(false);

        // ---------- 7. apparitions : marque orange de main / trou, grille, ombre (avec la goule qui sort) ----------
        Vector3 A = P + versCentre * 5f; A.y = Sol(A);
        Vector3 oeA = A + Flat(P - A).normalized * 2.6f + Quaternion.Euler(0, 90, 0) * Flat(P - A).normalized * 0.7f; oeA.y = Sol(oeA) + 1.5f;
        // avant : la marque d'apparition du jeu (M_VFX_Apparition, cercle plein pulsé) avec ses valeurs rouges de la nuit 4
        var apM = Resources.Load<Material>(MarquageBoss.RessourceApparition);
        var orange = Plat(PrimitiveType.Cylinder, A + Vector3.up * 0.03f, Quaternion.identity, new Vector3(1.6f, 0.01f, 1.6f), AncienRouge(apM));
        Cam(oeA, A + Vector3.up * 0.6f); await Snap("Apparition_avant");
        Object.Destroy(orange);
        foreach (var fx in new[] { "Trou", "Grille", "Ombre" })
        {
            GameObject grille = null;
            Vector3 pos = A;
            if (fx == "Grille")
            {
                // grille d'égout de démonstration (1,6 m, fer rouillé de la bibliothèque) posée au ras du sol : le jeu n'en a pas encore
                grille = new GameObject("Grille_Demo"); grille.transform.SetPositionAndRotation(A, Quaternion.LookRotation(Flat(P - A)));
                var fer = AssetDatabase.LoadAssetAtPath<Material>(FER);
                void Barre(Vector3 lp, Vector3 sc) { var b = Plat(PrimitiveType.Cube, Vector3.zero, Quaternion.identity, sc, fer); b.transform.SetParent(grille.transform, false); b.transform.localPosition = lp; b.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On; }
                foreach (float z in new[] { -0.8f, 0.8f }) Barre(new Vector3(0, 0.03f, z), new Vector3(1.7f, 0.06f, 0.1f));
                foreach (float x in new[] { -0.8f, 0.8f }) Barre(new Vector3(x, 0.03f, 0), new Vector3(0.1f, 0.06f, 1.5f));
                for (int i = -3; i <= 3; i++) Barre(new Vector3(i * 0.2f, 0.02f, 0), new Vector3(0.05f, 0.04f, 1.5f));
                var trou = Plat(PrimitiveType.Cube, Vector3.zero, Quaternion.identity, new Vector3(1.5f, 0.01f, 1.5f), Unlit(new Color(0.004f, 0.004f, 0.005f)));
                trou.transform.SetParent(grille.transform, false); trou.transform.localPosition = new Vector3(0, 0.004f, 0);
            }
            var inst = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PF + "VFX_Apparition_" + fx + ".prefab"), pos, Quaternion.identity);
            goule.transform.SetPositionAndRotation(pos + Vector3.down * 1.1f, Quaternion.LookRotation(Flat(P - pos)));
            float ecoule = 0f;
            foreach (var (t, sortie) in new[] { (0.25f, 0.35f), (0.5f, 0.75f), (0.95f, 1f) })
            {
                await Avance(t - ecoule); ecoule = t;
                if (fx == "Ombre") { goule.transform.position = pos; goule.SetActive(t >= 0.45f); }   // la goule paraît sous la fumée
                else goule.transform.position = pos + Vector3.down * (1.1f * (1f - sortie));
                Cam(oeA, A + Vector3.up * 0.6f);
                await Snap($"Apparition_{fx}_{Mathf.RoundToInt(t * 100):000}");
            }
            goule.SetActive(true);
            await Avance(2.5f);
            if (inst) Object.Destroy(inst);
            if (grille) Object.Destroy(grille);
        }
        Object.Destroy(goule);

        Time.timeScale = 1f;
        Object.Destroy(go); Object.Destroy(_rt); Object.Destroy(_tex);
        File.WriteAllLines(Path.Combine(dossier, "points.txt"), _log);
        return string.Join("\n", _log);
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.sqrMagnitude > 1e-4f ? v : Vector3.forward; }
}
