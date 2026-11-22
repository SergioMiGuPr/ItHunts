// Interface en jeu, lot 7 de la nuit du 2026-11-22 : écran de fin de run, menu d'interaction de porte, choix de l'acte,
// panneau admin de playtest, barre de vie du boss, marqueur de critique. Même famille que les maquettes : cadre des menus
// (9 tranches), cartouches, barre de vie et cartouche des âmes du HUD, flamme des âmes, étoile du réticule, lueurs ;
// textes en EB Garamond, titres en Grenze Gotisch.
// `unity cmd ... script run --file UIJeu3Builder.cs --entry UIJeu3Builder.<Construit|Capture>`
using System.IO;
using System.Linq;
using ItHunts.Art.Menus;
using ItHunts.Art.UIJeu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIJeu3Builder
{
    const string J = "Assets/Art/UI/Jeu";
    const string PRE = "Assets/Art/UI/Jeu/Prefabs";
    const string SCENE = "Assets/Art/Scenes/Test_UI_Jeu3.unity";
    static readonly Color OR = new Color(0.969f, 0.882f, 0.702f);
    static readonly Color DESC = new Color(0.81f, 0.776f, 0.722f);
    static readonly Color AME = new Color(0.86f, 0.9f, 0.96f);

    static Sprite Spr(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
    static Font EB(string s) => AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Polices/EBGaramond/EBGaramond-" + s + ".ttf");
    static Font GG(string s) => AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Polices/GrenzeGotisch/GrenzeGotisch-" + s + ".ttf");
    const string CADRE = "Assets/Art/UI/Menus/Commun/Menus_Cadre.png";
    const string LUEUR = "Assets/Art/UI/Menus/Commun/Menus_Lueur.png";
    const string FLAMME = "Assets/Art/UI/HUD/Ames_Flamme.png";

    static RectTransform N(string nom, Transform p, Vector2 ancre, Vector2 pivot, Vector2 pos, Vector2 taille)
    {
        var go = new GameObject(nom, typeof(RectTransform)); go.layer = 5;
        var rt = (RectTransform)go.transform; rt.SetParent(p, false);
        rt.anchorMin = rt.anchorMax = ancre; rt.pivot = pivot; rt.anchoredPosition = pos; rt.sizeDelta = taille;
        return rt;
    }
    static RectTransform HG(string nom, Transform p, float x, float y, float w, float h) => N(nom, p, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));
    static RectTransform C(string nom, Transform p, float x, float y, float w, float h) => N(nom, p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(w, h));
    static Image Img(RectTransform rt, Sprite sp, Color? c = null, bool ray = false, bool aspect = true)
    {
        var im = rt.gameObject.AddComponent<Image>(); im.sprite = sp; im.raycastTarget = ray; im.color = c ?? Color.white; im.preserveAspect = aspect;
        return im;
    }
    static Text Txt(RectTransform rt, Font f, int t, Color c, TextAnchor al, string s, bool ombre = true)
    {
        var x = rt.gameObject.AddComponent<Text>(); x.font = f; x.fontSize = t; x.color = c; x.alignment = al; x.text = s; x.raycastTarget = false;
        x.horizontalOverflow = HorizontalWrapMode.Overflow; x.verticalOverflow = VerticalWrapMode.Overflow;
        if (ombre) { var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.04f, 0.02f, 0.04f, 0.9f); o.effectDistance = new Vector2(1.5f, -1.5f); }
        return x;
    }
    static Image Cadre(Transform p, float w, float h)
    {
        var im = Img(HG("Cadre", p, 0, 0, w, h), Spr(CADRE), null, true, false); im.type = Image.Type.Sliced; return im;
    }
    // cartouche-bouton (mêmes pièces que les menus)
    static Button Cartouche(Transform parent, string nom, float x, float y, float w, float h, string libelle)
    {
        var rt = HG(nom, parent, x, y, w, h);
        float s = h / 72f;
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/UI/Menus/Materiaux/M_UI_Bouton.mat");
        string P0 = "Assets/Art/UI/Menus/Commun/Menus_Cartouche_";
        float g = 34 * s, fl = 36 * s;
        Image P(string n, float px, float pw)
        {
            var im = Img(HG(n, rt, px, 0, pw, h), Spr(P0 + n.Substring(n.Length - 1) + ".png"), new Color(0.5f, 0.5f, 0.5f, 1), false, false);
            im.material = mat; return im;
        }
        var parts = new[] { P("Bout_G", 0, g), P("Tranche_M", g - 0.5f, (w - fl) / 2f - g + 1), P("Fleuron_C", (w - fl) / 2f, fl),
                            P("Tranche2_M", (w + fl) / 2f - 0.5f, (w - fl) / 2f - g + 1), P("Bout_D", w - g, g) };
        var hit = rt.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); hit.raycastTarget = true;
        var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = hit;
        var tg = rt.gameObject.AddComponent<TeinteGroupe>(); tg.cibles = parts;
        Txt(C("Texte", rt, 0, -1, w, h), EB("SemiBold"), Mathf.RoundToInt(h * 0.42f), OR, TextAnchor.MiddleCenter, libelle);
        return b;
    }
    static GameObject Sauve(GameObject go, string nom)
    {
        Directory.CreateDirectory(PRE);
        var p = PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{nom}.prefab");
        Object.DestroyImmediate(go);
        return p;
    }

    // ------------------------------------------------------------------ éléments
    static GameObject FinDeRunUI()
    {
        const float W = 900, H = 800;
        var r = C("Panneau_FinDeRun", null, 0, 0, W, H);
        Img(C("Voile", r, 0, 0, 4000, 3000), null, new Color(0.02f, 0.0f, 0.01f, 0.72f), true);
        var lu = Img(C("Lueur_Titre", r, 0, 300, 760, 260), Spr(LUEUR), new Color(0.86f, 0.22f, 0.25f, 0.35f), false, false);
        Cadre(r, W, H);
        var f = r.gameObject.AddComponent<FinDeRun>();
        f.lueurTitre = lu;
        f.titre = Txt(HG("Titre", r, 0, 40, W, 110), GG("Bold"), 92, f.couleurMort, TextAnchor.MiddleCenter, "Tu es mort");
        f.sousTitre = Txt(HG("SousTitre", r, 0, 150, W, 40), EB("Italic"), 26, DESC, TextAnchor.MiddleCenter, "");
        // filet
        Img(HG("Filet", r, 150, 205, W - 300, 3), null, new Color(0.59f, 0.52f, 0.61f, 0.6f));
        int n = 7;
        f.libelles = new Text[n]; f.valeurs = new Text[n];
        for (int i = 0; i < n; i++)
        {
            float y = 232 + i * 52;
            if (i % 2 == 0) Img(HG("Bande_" + i, r, 130, y - 2, W - 260, 48), null, new Color(0, 0, 0, 0.28f));
            f.libelles[i] = Txt(HG("Libelle_" + i, r, 150, y, 400, 44), EB("Regular"), 28, OR, TextAnchor.MiddleLeft, "Statistique");
            f.valeurs[i] = Txt(HG("Valeur_" + i, r, W - 150 - 300, y, 300, 44), EB("SemiBold"), 28, AME, TextAnchor.MiddleRight, "0");
        }
        f.recommencer = Cartouche(r, "Recommencer", 150, 640, 280, 64, "Recommencer");
        f.campement = Cartouche(r, "Campement", W - 150 - 280, 640, 280, 64, "Campement");
        f.apercu = new[]
        {
            new FinDeRun.Stat { libelle = "Acte atteint", valeur = "II, le château" },
            new FinDeRun.Stat { libelle = "Temps de la run", valeur = "23 min 41 s" },
            new FinDeRun.Stat { libelle = "Ennemis tués", valeur = "184" },
            new FinDeRun.Stat { libelle = "Âmes collectées", valeur = "1 260" },
            new FinDeRun.Stat { libelle = "Coffres ouverts", valeur = "6" },
            new FinDeRun.Stat { libelle = "Tirs critiques", valeur = "41" },
            new FinDeRun.Stat { libelle = "Artefact", valeur = "Barillet" },
        };
        f.RemplitApercu();
        return r.gameObject;
    }

    static GameObject PorteUI()
    {
        const float W = 520, H = 190;
        var r = C("Panneau_Porte", null, 0, 150, W, H);
        var g = r.gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false;
        Img(C("Lueur", r, 0, 0, W + 120, H + 90), Spr(LUEUR), new Color(0.6f, 0.7f, 0.85f, 0.22f), false, false);
        var cadre = Cadre(r, W, H); cadre.raycastTarget = false;
        var p = r.gameObject.AddComponent<PanneauPorte>();
        p.groupe = g; p.cadre = cadre;
        p.nom = Txt(HG("Nom", r, 0, 30, W, 48), GG("SemiBold"), 40, OR, TextAnchor.MiddleCenter, "Porte scellée");
        p.flamme = Img(HG("Flamme", r, W / 2f - 62, 86, 22, 36), Spr(FLAMME));
        p.prix = Txt(HG("Prix", r, W / 2f - 32, 82, 120, 44), EB("SemiBold"), 32, AME, TextAnchor.MiddleLeft, "40");
        // touche (rond) + invite
        var t = HG("Touche", r, W / 2f - 92, 132, 34, 34);
        Img(t, Spr("Assets/Art/UI/Jeu/Charge_Vide.png"), new Color(0.85f, 0.8f, 0.85f));
        p.touche = Txt(C("Lettre", t, 0, 0, 34, 34), EB("SemiBold"), 20, OR, TextAnchor.MiddleCenter, "E");
        p.invite = Txt(HG("Invite", r, W / 2f - 48, 128, 260, 40), EB("Italic"), 26, OR, TextAnchor.MiddleLeft, "Ouvrir");
        p.MontreApercu();
        return r.gameObject;
    }

    static GameObject ChoixActeUI()
    {
        const float W = 1040, H = 820;
        var r = C("Panneau_ChoixActe", null, 0, 0, W, H);
        Img(C("Voile", r, 0, 0, 4000, 3000), null, new Color(0, 0, 0, 0.6f), true);
        Cadre(r, W, H);
        Txt(HG("Titre", r, 0, 34, W, 60), EB("Regular"), 40, DESC, TextAnchor.MiddleCenter, "Choix de l'acte (playtest)");
        var c = r.gameObject.AddComponent<ChoixActe>();
        // vignette encadrée (cadre fin 9 tranches)
        var vr = HG("Vignette", r, 130, 110, 780, 439);
        c.vignette = Img(vr, null, Color.white, false, false);
        var vc = Img(HG("Vignette_Cadre", r, 118, 98, 804, 463), Spr(CADRE), null, false, false); vc.type = Image.Type.Sliced; vc.pixelsPerUnitMultiplier = 2.2f; vc.fillCenter = false;
        c.nom = Txt(HG("Nom", r, 0, 566, W, 96), GG("Bold"), 78, OR, TextAnchor.MiddleCenter, "Acte I");
        c.sousTitre = Txt(HG("SousTitre", r, 0, 656, W, 40), EB("Italic"), 28, DESC, TextAnchor.MiddleCenter, "");
        c.compteur = Txt(HG("Compteur", r, 0, 692, W, 30), EB("Regular"), 22, new Color(0.6f, 0.56f, 0.62f), TextAnchor.MiddleCenter, "1 / 3");
        // flèches : l'étoile du réticule étirée en pointe, dans un rond de charge
        Button Fleche(string nom, float x, bool g)
        {
            var fr = HG(nom, r, x, 300, 84, 84);
            var fond = Img(fr, Spr("Assets/Art/UI/Jeu/Charge_Vide.png"), new Color(0.75f, 0.7f, 0.78f), true);
            var pt = C("Pointe", fr, g ? -4 : 4, 0, 70, 44); Img(pt, Spr("Assets/Art/UI/Jeu/Reticule_Etoile.png"), OR, false, false);
            pt.localRotation = Quaternion.Euler(0, 0, 0);
            var b = fr.gameObject.AddComponent<Button>(); b.targetGraphic = fond;
            var cs = b.colors; cs.highlightedColor = new Color(1, 0.95f, 0.8f); cs.pressedColor = new Color(0.7f, 0.65f, 0.6f); b.colors = cs;
            return b;
        }
        c.gauche = Fleche("Fleche_G", 22, true);
        c.droite = Fleche("Fleche_D", W - 22 - 84, false);
        c.lancer = Cartouche(r, "Lancer", (W - 300) / 2f, 734, 300, 64, "Lancer");
        c.actes = new[]
        {
            new ChoixActe.Acte { nom = "Acte I", sousTitre = "Les caves : cachots, citerne, puits", vignette = Spr($"{J}/Actes/Acte_I.png") },
            new ChoixActe.Acte { nom = "Acte II", sousTitre = "Le château : grands halls sous la lune", vignette = Spr($"{J}/Actes/Acte_II.png") },
            new ChoixActe.Acte { nom = "Acte III", sousTitre = "Le jardin, puis l'arène du boss", vignette = Spr($"{J}/Actes/Acte_III.png") },
        };
        c.Affiche();
        return r.gameObject;
    }

    static GameObject AdminUI()
    {
        const float W = 470, H = 600;
        var r = N("Panneau_Admin", null, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -28), new Vector2(W, H));
        Img(HG("Fond", r, 14, 14, W - 28, H - 28), null, new Color(0.055f, 0.043f, 0.07f, 0.86f));
        var cad = Cadre(r, W, H); cad.pixelsPerUnitMultiplier = 1.6f;
        Txt(HG("Titre", r, 0, 22, W, 46), GG("SemiBold"), 36, OR, TextAnchor.MiddleCenter, "Admin");
        var a = r.gameObject.AddComponent<PanneauAdmin>();
        a.touche = Txt(HG("Touche", r, 0, 64, W, 26), EB("Italic"), 19, new Color(0.6f, 0.56f, 0.62f), TextAnchor.MiddleCenter, "F1 : afficher ou masquer");
        string[] lib = { "Invincible", "Traqueur actif", "Vitesse du jeu × 0.5", "Debug du mouvement", "+100 âmes", "Tuer les ennemis", "Ouvrir les portes", "Aller au boss" };
        bool[] bascule = { true, true, true, true, false, false, false, false };
        a.libelles = new Text[lib.Length]; a.pastilles = new Image[lib.Length]; a.boutons = new Button[lib.Length];
        for (int i = 0; i < lib.Length; i++)
        {
            float y = 104 + i * 58;
            var ligne = HG("Ligne_" + i, r, 36, y, W - 72, 50);
            var hit = Img(ligne, null, new Color(1, 1, 1, i % 2 == 0 ? 0.04f : 0.0f), true, false);
            var b = ligne.gameObject.AddComponent<Button>(); b.targetGraphic = hit;
            var cs = b.colors; cs.normalColor = hit.color; cs.highlightedColor = new Color(0.6f, 0.7f, 0.85f, 0.18f); b.colors = cs;
            a.boutons[i] = b;
            if (bascule[i])
            {
                a.pastilles[i] = Img(HG("Pastille", ligne, 6, 9, 32, 32), Spr("Assets/Art/UI/Jeu/Charge_Pleine.png"), i == 1 || i == 3 ? a.allume : a.eteint);
                a.libelles[i] = Txt(HG("Libelle", ligne, 52, 0, W - 140, 50), EB("Regular"), 25, OR, TextAnchor.MiddleLeft, lib[i]);
            }
            else
            {
                Img(HG("Puce", ligne, 14, 17, 16, 16), Spr(FLAMME), new Color(0.86f, 0.9f, 0.96f, i == 4 ? 1 : 0)).preserveAspect = true;
                Img(HG("Puce2", ligne, 12, 15, 20, 20), Spr("Assets/Art/UI/Jeu/Reticule_Etoile.png"), new Color(0.97f, 0.88f, 0.7f, i == 4 ? 0 : 1));
                a.libelles[i] = Txt(HG("Libelle", ligne, 52, 0, W - 140, 50), EB("Italic"), 25, AME, TextAnchor.MiddleLeft, lib[i]);
            }
        }
        return r.gameObject;
    }

    static GameObject BarreBossUI()
    {
        float L = 1100;
        float h = 131 * 1.15f;            // cadre natif de la barre de vie (131 px en 1080p) agrandi de 15 %
        var r = N("HUD_BarreVieBoss", null, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(L, h + 56));
        var g = r.gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false;
        var b = r.gameObject.AddComponent<BarreVieBoss>(); b.groupe = g;
        var corps = HG("Corps", r, 0, 56, L, h); b.corps = corps;
        float s = h / 131f;                // échelle par rapport au natif 1080p
        Image Sl(string n, string sp)
        {
            var im = Img(HG(n, corps, 0, 0, L, h), Spr("Assets/Art/UI/HUD/" + sp + ".png"), null, false, false);
            im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1f / (s * 1f); return im;
        }
        Sl("Fond", "BarreVie_Fond");
        float cx = 100 * s, cy = 54 * s, cw = L - 100 * s - 26 * s, ch = 23 * s;
        Image F(string n, string sp, Color c)
        {
            var im = Img(HG(n, corps, cx, cy, cw, ch), Spr("Assets/Art/UI/HUD/" + sp + ".png"), c, false, false);
            im.type = Image.Type.Filled; im.fillMethod = Image.FillMethod.Horizontal; im.fillOrigin = 0; return im;
        }
        b.retard = F("Retard", "BarreVie_Remplissage_Retard", Color.white);
        b.remplissage = F("Remplissage", "BarreVie_Remplissage", Color.white);
        b.crans = new RectTransform[2];
        for (int i = 0; i < 2; i++)
        {
            var cr = HG("Cran_" + i, corps, cx + cw * (i == 0 ? 0.66f : 0.33f) - 3, cy - 6 * s, 6, ch + 12 * s);
            Img(cr, null, new Color(0.85f, 0.78f, 0.86f, 0.95f));
            b.crans[i] = cr;
        }
        Sl("Cadre", "BarreVie_Cadre");
        b.nom = Txt(HG("Nom", r, 122 * s, 6, L - 122 * s, 54), GG("Bold"), 44, OR, TextAnchor.MiddleCenter, "Le Gardien du domaine");
        b.Montre("Le Gardien du domaine");
        b.Pose(0.58f, 0.71f);
        return r.gameObject;
    }

    static GameObject CritiqueUI()
    {
        var r = C("HUD_Critique", null, 0, 0, 160, 160);
        var m = r.gameObject.AddComponent<MarqueurCritique>();
        m.lueur = Img(C("Lueur", r, 0, 0, 150, 150), Spr(LUEUR), new Color(1, 0.6f, 0.25f, 0.5f), false, false);
        m.anneau = Img(C("Anneau", r, 0, 0, 96, 96), Spr("Assets/Art/UI/Jeu/Parade_Icone.png"), new Color(1, 0.82f, 0.35f, 0.8f));
        m.eclats = new RectTransform[4];
        for (int i = 0; i < 4; i++)
        {
            var e = C("Eclat_" + i, r, 0, 0, 44, 44);
            Img(e, Spr("Assets/Art/UI/Jeu/Reticule_Etoile.png"), new Color(1, 0.82f, 0.35f));
            m.eclats[i] = e;
        }
        m.texte = Txt(C("Texte", r, 0, -46, 240, 34), GG("Bold"), 30, new Color(1, 0.82f, 0.35f), TextAnchor.MiddleCenter, "Critique");
        m.Pose(0.18f);
        return r.gameObject;
    }

    public static string Construit()
    {
        foreach (var f in Directory.GetFiles($"{J}/Actes", "*.png"))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(f.Replace("\\", "/"));
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.mipmapEnabled = false;
            ti.spritePixelsPerUnit = 100; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.maxTextureSize = 2048;
            ti.SaveAndReimport();
        }
        var pf = new[] { Sauve(FinDeRunUI(), "Panneau_FinDeRun"), Sauve(PorteUI(), "Panneau_Porte"), Sauve(ChoixActeUI(), "Panneau_ChoixActe"),
                         Sauve(AdminUI(), "Panneau_Admin"), Sauve(BarreBossUI(), "HUD_BarreVieBoss"), Sauve(CritiqueUI(), "HUD_Critique") };
        // scène d'aperçu : fond = capture de jeu, canvas 1920 × 1080 (match hauteur), caméra
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.orthographic = true;
        var cv = new GameObject("Canvas", typeof(RectTransform)).AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceCamera; cv.worldCamera = cam; cv.planeDistance = 5;
        var cs = cv.gameObject.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 1; cs.referencePixelsPerUnit = 100;
        cv.gameObject.AddComponent<GraphicRaycaster>();
        var fond = N("Fond_Jeu", cv.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920, 1080));
        Img(fond, Spr($"{J}/Actes/Acte_II.png"), Color.white, false, false);
        foreach (var p in pf) PrefabUtility.InstantiatePrefab(p, cv.transform);
        // le réticule existant, pour placer le marqueur de critique
        var ret = AssetDatabase.LoadAssetAtPath<GameObject>($"{PRE}/HUD_Reticule.prefab");
        if (ret) PrefabUtility.InstantiatePrefab(ret, cv.transform);
        EditorSceneManager.SaveScene(sc, SCENE);
        return "ok " + string.Join(" ", pf.Select(p => p.name));
    }

    // etat : liste de prefabs visibles séparés par « + » ; fond : Acte_I | Acte_II | Acte_III
    public static string Capture(string visibles, string fond, string sortie)
    {
        EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var cv = Object.FindFirstObjectByType<Canvas>();
        var vis = visibles.Split('+');
        foreach (Transform t in cv.transform)
            if (t.name != "Fond_Jeu") t.gameObject.SetActive(vis.Contains(t.name));
        cv.transform.Find("Fond_Jeu").GetComponent<Image>().sprite = Spr($"{J}/Actes/{fond}.png");
        var fin = Object.FindFirstObjectByType<FinDeRun>();
        if (fin && visibles.Contains("Victoire")) { fin.victoireApercu = true; fin.sousTitreApercu = "Le gardien du domaine est tombé"; }
        if (fin) fin.RemplitApercu();
        var ca = Object.FindFirstObjectByType<ChoixActe>();
        if (ca) { ca.index = fond == "Acte_I" ? 0 : fond == "Acte_II" ? 1 : 2; ca.Affiche(); }
        var po = Object.FindFirstObjectByType<PanneauPorte>();
        if (po) { if (visibles.Contains("Trop")) po.amesApercu = 12; po.MontreApercu(); }
        var cr = Object.FindFirstObjectByType<MarqueurCritique>();
        if (cr) cr.Pose(0.18f);
        var bb = Object.FindFirstObjectByType<BarreVieBoss>();
        if (bb) { bb.Montre("Le Gardien du domaine"); bb.Pose(0.58f, 0.71f); bb.Phase(0); bb.Pose(0.58f, 0.71f); }
        var cam = cv.worldCamera;
        int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        cam.targetTexture = rt;
        for (int i = 0; i < 3; i++) { Canvas.ForceUpdateCanvases(); cam.Render(); }
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        Directory.CreateDirectory(Path.GetDirectoryName(sortie));
        File.WriteAllBytes(sortie, tex.EncodeToJPG(92));
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
        return "ok " + sortie;
    }
}
