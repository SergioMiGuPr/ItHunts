// Interface en jeu, lot 6 de la nuit 4 (2026-11-23) : écran de fin avec grille d'améliorations encadrées, panneau de la porte
// de sortie scellée (temps restant, explication), marchand à rangs (nouvelle, montée de rang, soin), catalogue des
// illustrations. Même famille que les panneaux de la nuit 3 (cadre des menus, cartouches, EB Garamond, Grenze Gotisch).
// `unity cmd ... script run --file UIJeu4Builder.cs --entry UIJeu4Builder.<Construit|Capture>`
using System.IO;
using System.Linq;
using ItHunts.Art.Menus;
using ItHunts.Art.UIJeu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIJeu4Builder
{
    const string J = "Assets/Art/UI/Jeu";
    const string PRE = "Assets/Art/UI/Jeu/Prefabs";
    const string AM = "Assets/Art/UI/Ameliorations";
    const string SCENE = "Assets/Art/Scenes/Test_UI_Jeu4.unity";
    static readonly Color OR = new Color(0.969f, 0.882f, 0.702f);
    static readonly Color DESC = new Color(0.81f, 0.776f, 0.722f);
    static readonly Color AME = new Color(0.86f, 0.9f, 0.96f);
    const string CADRE = "Assets/Art/UI/Menus/Commun/Menus_Cadre.png";
    const string LUEUR = "Assets/Art/UI/Menus/Commun/Menus_Lueur.png";
    const string FLAMME = "Assets/Art/UI/HUD/Ames_Flamme.png";
    static readonly string[] IDS = {
        "G1","G2","G3","G4","G5","G6","G7","G8","G9","G10","G11","G12","G13","G14","G15","G16","G17","G18","G19","G20","G21","G22","G23","G24","G25","G26","G27","G28","G29","G30",
        "R-P1","R-P2","R1","R2","R3","R4","R5","R6","R7","R8","C-P1","C-P2","C1","C2","C3","C4","C5","C6","C7","C8",
        "K-P1","K-P2","K1","K2","K3","K4","K5","K6","K7","K8","H-P1","H-P2","H1","H2","H3","H4","H5","H6","H7","H8" };

    static Sprite Spr(string p) => AssetDatabase.LoadAssetAtPath<Sprite>(p);
    static Font EB(string s) => AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Polices/EBGaramond/EBGaramond-" + s + ".ttf");
    static Font GG(string s) => AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Polices/GrenzeGotisch/GrenzeGotisch-" + s + ".ttf");
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
    static Text Txt(RectTransform rt, Font f, int t, Color c, TextAnchor al, string s, bool ombre = true, bool wrap = false)
    {
        var x = rt.gameObject.AddComponent<Text>(); x.font = f; x.fontSize = t; x.color = c; x.alignment = al; x.text = s; x.raycastTarget = false;
        x.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow; x.verticalOverflow = VerticalWrapMode.Overflow;
        if (ombre) { var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.04f, 0.02f, 0.04f, 0.9f); o.effectDistance = new Vector2(1.5f, -1.5f); }
        return x;
    }
    static Image Cadre(Transform p, float w, float h, string nom = "Cadre")
    {
        var im = Img(HG(nom, p, 0, 0, w, h), Spr(CADRE), null, true, false); im.type = Image.Type.Sliced; return im;
    }
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

    // ------------------------------------------------------------------ catalogue et icône encadrée
    static CatalogueAmeliorations Catalogue()
    {
        string p = $"{AM}/Catalogue_Ameliorations.asset";
        var c = AssetDatabase.LoadAssetAtPath<CatalogueAmeliorations>(p);
        if (c == null) { c = ScriptableObject.CreateInstance<CatalogueAmeliorations>(); AssetDatabase.CreateAsset(c, p); }
        c.entrees = IDS.Select(i => new CatalogueAmeliorations.Entree { id = i, icone = Spr($"{AM}/Icones/ICO_{i.Replace('-', '_')}.png") }).ToArray();
        c.cadres = CatalogueAmeliorations.Rangs.Select(r => Spr($"{AM}/Cadres/Cadre_{r}.png")).ToArray();
        c.cadresPassifs = CatalogueAmeliorations.Rangs.Select(r => Spr($"{AM}/Cadres/Cadre_Passif_{r}.png")).ToArray();
        c.masquePassif = Spr($"{AM}/Cadres/Masque_Passif_Losange.png");
        EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
        return c;
    }

    static IconeAmelioration Icone(Transform p, string nom, float x, float y, float taille, CatalogueAmeliorations cat)
    {
        var rt = HG(nom, p, x, y, taille, taille);
        var ia = rt.gameObject.AddComponent<IconeAmelioration>();
        ia.catalogue = cat;
        var m = N("Masque", rt, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        m.anchorMin = Vector2.zero; m.anchorMax = Vector2.one; m.offsetMin = m.offsetMax = Vector2.zero;
        var mi = Img(m, cat.masquePassif, Color.white, false, false);
        ia.masque = m.gameObject.AddComponent<Mask>(); ia.masque.showMaskGraphic = false;
        var z = N("Zone", m, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ia.zoneIcone = z;
        var ic = N("Icone", z, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        ic.anchorMin = Vector2.zero; ic.anchorMax = Vector2.one; ic.offsetMin = ic.offsetMax = Vector2.zero;
        ia.icone = Img(ic, null, Color.white, false, true);
        var cd = N("Cadre", rt, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        cd.anchorMin = Vector2.zero; cd.anchorMax = Vector2.one; cd.offsetMin = cd.offsetMax = Vector2.zero;
        ia.cadre = Img(cd, cat.cadres[0], Color.white, false, true);
        ia.Configure(cat.Icone("G1"), 0, false);
        return ia;
    }

    // ------------------------------------------------------------------ écran de fin
    static GameObject FinDeRunUI(CatalogueAmeliorations cat)
    {
        const float W = 1120, H = 880;
        var r = C("Panneau_FinDeRun", null, 0, 0, W, H);
        Img(C("Voile", r, 0, 0, 4000, 3000), null, new Color(0.02f, 0.0f, 0.01f, 0.72f), true);
        var lu = Img(C("Lueur_Titre", r, 0, 360, 760, 260), Spr(LUEUR), new Color(0.86f, 0.22f, 0.25f, 0.35f), false, false);
        Cadre(r, W, H);
        var f = r.gameObject.AddComponent<FinDeRun>();
        f.lueurTitre = lu;
        f.titre = Txt(HG("Titre", r, 0, 40, W, 110), GG("Bold"), 92, f.couleurMort, TextAnchor.MiddleCenter, "Tu es mort");
        f.sousTitre = Txt(HG("SousTitre", r, 0, 150, W, 40), EB("Italic"), 26, DESC, TextAnchor.MiddleCenter, "");
        Img(HG("Filet", r, 150, 205, W - 300, 3), null, new Color(0.59f, 0.52f, 0.61f, 0.6f));
        // statistiques : deux colonnes de 7 lignes (14 au plus), dans le cadre
        int n = 14; float colW = 400, x0 = 140, x1 = W - 140 - colW;
        f.libelles = new Text[n]; f.valeurs = new Text[n];
        for (int i = 0; i < n; i++)
        {
            int col = i / 7, row = i % 7;
            float x = col == 0 ? x0 : x1, y = 228 + row * 44;
            f.libelles[i] = Txt(HG("Libelle_" + i, r, x, y, 240, 38), EB("Regular"), 24, OR, TextAnchor.MiddleLeft, "Statistique");
            // la bande suit sa ligne : masquée avec elle quand il y a moins de 14 statistiques
            if (row % 2 == 0) { var bd = Img(HG("Bande", f.libelles[i].transform, -14, -1, colW + 28, 40), null, new Color(0, 0, 0, 0.28f)); bd.transform.SetAsFirstSibling(); }
            f.valeurs[i] = Txt(HG("Valeur_" + i, r, x + colW - 160, y, 160, 38), EB("SemiBold"), 24, AME, TextAnchor.MiddleRight, "0");
        }
        // améliorations : titre et grille de 10 cases (le passif en premier), dans le cadre
        Img(HG("Filet_Ameliorations", r, 150, 556, W - 300, 2), null, new Color(0.59f, 0.52f, 0.61f, 0.45f));
        f.titreAmeliorations = Txt(HG("Titre_Ameliorations", r, 0, 566, W, 40), EB("SemiBold"), 28, OR, TextAnchor.MiddleCenter, "Améliorations");
        const float T = 80, G = 14;
        float gx = (W - (10 * T + 9 * G)) / 2f;
        f.ameliorations = new IconeAmelioration[10];
        for (int i = 0; i < 10; i++)
            f.ameliorations[i] = Icone(r, "Amelioration_" + i, gx + i * (T + G), 618, T, cat);
        f.surplus = Txt(HG("Surplus", r, W - 130, 638, 80, 40), EB("SemiBold"), 26, AME, TextAnchor.MiddleLeft, "+0");
        f.recommencer = Cartouche(r, "Recommencer", 170, 740, 300, 66, "Recommencer");
        f.campement = Cartouche(r, "Campement", W - 170 - 300, 740, 300, 66, "Campement");
        f.apercu = new[]
        {
            new FinDeRun.Stat { libelle = "Acte atteint", valeur = "II, le château" }, new FinDeRun.Stat { libelle = "Temps de la run", valeur = "23 min 41 s" },
            new FinDeRun.Stat { libelle = "Goules tuées", valeur = "142" }, new FinDeRun.Stat { libelle = "Volants tués", valeur = "42" },
            new FinDeRun.Stat { libelle = "Âmes gagnées", valeur = "1 260" }, new FinDeRun.Stat { libelle = "Âmes dépensées", valeur = "980" },
            new FinDeRun.Stat { libelle = "Coffres ouverts", valeur = "6" }, new FinDeRun.Stat { libelle = "Caisses lancées", valeur = "57" },
            new FinDeRun.Stat { libelle = "Tirs critiques", valeur = "41" }, new FinDeRun.Stat { libelle = "Parades", valeur = "18" },
            new FinDeRun.Stat { libelle = "Distance", valeur = "8,4 km" }, new FinDeRun.Stat { libelle = "Vitesse max.", valeur = "19,6 m/s" },
            new FinDeRun.Stat { libelle = "Artefact", valeur = "Lanterne" },
        };
        f.RemplitApercu();
        return r.gameObject;
    }

    // ------------------------------------------------------------------ porte de sortie scellée
    static GameObject PorteSortieUI()
    {
        const float W = 660, H = 290;
        var r = C("Panneau_PorteSortie", null, 0, 150, W, H);
        var g = r.gameObject.AddComponent<CanvasGroup>(); g.blocksRaycasts = false;
        Img(C("Lueur", r, 0, 0, W + 120, H + 90), Spr(LUEUR), new Color(0.6f, 0.7f, 0.85f, 0.22f), false, false);
        var cadre = Cadre(r, W, H); cadre.raycastTarget = false;
        var p = r.gameObject.AddComponent<PanneauPorte>();
        p.groupe = g; p.cadre = cadre;
        p.nom = Txt(HG("Nom", r, 0, 28, W, 48), GG("SemiBold"), 40, OR, TextAnchor.MiddleCenter, "Sortie scellée");
        p.temps = Txt(HG("Temps", r, 0, 84, W, 40), EB("SemiBold"), 30, new Color(0.95f, 0.74f, 0.42f), TextAnchor.MiddleCenter, "S'ouvre dans 2:41");
        p.explication = Txt(HG("Explication", r, 60, 130, W - 120, 70), EB("Italic"), 22, DESC, TextAnchor.UpperCenter, "", true, true);
        p.flamme = Img(HG("Flamme", r, W / 2f - 62, 204, 22, 36), Spr(FLAMME));
        p.prix = Txt(HG("Prix", r, W / 2f - 32, 200, 120, 44), EB("SemiBold"), 32, AME, TextAnchor.MiddleLeft, "200");
        var t = HG("Touche", r, W / 2f - 92, 250, 34, 34);
        Img(t, Spr("Assets/Art/UI/Jeu/Charge_Vide.png"), new Color(0.85f, 0.8f, 0.85f));
        p.touche = Txt(C("Lettre", t, 0, 0, 34, 34), EB("SemiBold"), 20, OR, TextAnchor.MiddleCenter, "E");
        p.invite = Txt(HG("Invite", r, W / 2f - 48, 246, 260, 40), EB("Italic"), 26, OR, TextAnchor.MiddleLeft, "Scellée");
        p.MontreSortieScellee(p.secondesApercu, 200);
        if (p.groupe) p.groupe.alpha = 1f;
        return r.gameObject;
    }

    // ------------------------------------------------------------------ marchand à rangs
    static MarchandRangs.Carte Carte(Transform p, string nom, float x, float y, float w, float h, CatalogueAmeliorations cat, bool montee, bool soin)
    {
        var rt = HG(nom, p, x, y, w, h);
        var c = new MarchandRangs.Carte();
        var hit = Img(rt, null, new Color(0, 0, 0, 0.32f), true, false);
        var cad = Img(HG("Cadre", rt, 0, 0, w, h), Spr(CADRE), null, false, false); cad.type = Image.Type.Sliced; cad.pixelsPerUnitMultiplier = 1.6f;
        c.lueur = Img(C("Lueur", rt, 0, 120, w + 60, 280), Spr(LUEUR), new Color(0.62f, 0.74f, 0.95f, 0.0f), false, false);
        c.bouton = rt.gameObject.AddComponent<Button>(); c.bouton.transition = Selectable.Transition.None; c.bouton.targetGraphic = hit;
        float ic = montee ? 140 : 176;
        // montée : l'amélioration au rang actuel à gauche, une flèche, la même au rang suivant à droite (même taille)
        c.illustration = Icone(rt, "Illustration", montee ? 26 : (w - ic) / 2f, montee ? 58 : 40, ic, cat);
        c.nom = Txt(HG("Nom", rt, 0, 232, w, 40), GG("SemiBold"), 30, OR, TextAnchor.MiddleCenter, "Nom");
        c.rang = Txt(HG("Rang", rt, 0, 274, w, 30), EB("SemiBold"), 22, AME, TextAnchor.MiddleCenter, "Bronze");
        if (montee)
        {
            c.rangApres = Icone(rt, "RangApres", w - 26 - ic, 58, ic, cat);
            Txt(HG("Fleche", rt, w / 2f - 20, 106, 40, 44), EB("SemiBold"), 38, OR, TextAnchor.MiddleCenter, "\u2192");
        }
        c.description = Txt(HG("Description", rt, 28, 316, w - 56, 80), EB("Italic"), 21, DESC, TextAnchor.UpperCenter, "", true, true);
        c.flamme = Img(HG("Flamme", rt, w / 2f - 44, h - 58, 22, 36), Spr(FLAMME));
        c.prix = Txt(HG("Prix", rt, w / 2f - 14, h - 62, 120, 44), EB("SemiBold"), 32, AME, TextAnchor.MiddleLeft, "60");
        c.corps = new Graphic[] { cad };
        if (soin) { c.illustration.cadre.enabled = false; }
        return c;
    }

    static GameObject MarchandUI(CatalogueAmeliorations cat)
    {
        const float W = 1260, H = 720, CW = 360, CH = 460;
        var r = C("Panneau_MarchandRangs", null, 0, 0, W, H);
        Img(C("Voile", r, 0, 0, 4000, 3000), null, new Color(0.02f, 0.0f, 0.01f, 0.6f), true);
        Cadre(r, W, H);
        var m = r.gameObject.AddComponent<MarchandRangs>();
        m.catalogue = cat;
        m.titre = Txt(HG("Titre", r, 0, 34, W, 70), GG("Bold"), 60, OR, TextAnchor.MiddleCenter, "Marchand");
        Img(HG("Flamme_Solde", r, W - 190, 54, 24, 40), Spr(FLAMME));
        m.solde = Txt(HG("Solde", r, W - 160, 50, 120, 44), EB("SemiBold"), 34, AME, TextAnchor.MiddleLeft, "120");
        float gx = (W - 3 * CW - 2 * 40) / 2f;
        m.nouvelle = Carte(r, "Carte_Nouvelle", gx, 128, CW, CH, cat, false, false);
        m.montee = Carte(r, "Carte_Montee", gx + CW + 40, 128, CW, CH, cat, true, false);
        m.soin = Carte(r, "Carte_Soin", gx + 2 * (CW + 40), 128, CW, CH, cat, false, true);
        Txt(HG("Etiquette_Nouvelle", r, gx, 104, CW, 24), EB("Italic"), 20, DESC, TextAnchor.MiddleCenter, "Nouvelle amélioration");
        Txt(HG("Etiquette_Montee", r, gx + CW + 40, 104, CW, 24), EB("Italic"), 20, DESC, TextAnchor.MiddleCenter, "Montée de rang");
        Txt(HG("Etiquette_Soin", r, gx + 2 * (CW + 40), 104, CW, 24), EB("Italic"), 20, DESC, TextAnchor.MiddleCenter, "Soin");
        m.fermer = Cartouche(r, "Fermer", (W - 260) / 2f, H - 104, 260, 62, "Fermer");
        m.iconeSoin = Spr($"{AM}/Icones/ICO_G19.png");
        m.apercuNouvelle = new MarchandRangs.Offre { id = "G11", nom = "Harpon", rang = 0, prix = 60, description = "La traction vise aussi les goules : vous êtes tiré vers elles." };
        m.apercuMontee = new MarchandRangs.Offre { id = "R5", nom = "Feu grégeois", rang = 1, prix = 140, description = "Le jet laisse des flaques de feu plus larges, qui brûlent plus longtemps." };
        m.RemplitApercu();
        return r.gameObject;
    }

    public static string Construit()
    {
        // icônes de carte : sprites (comme les autres icônes de la mini-carte), sans mip, alpha transparent
        foreach (var f in new[] { "Assets/Art/UI/MiniCarte/MiniCarte_Goule.png", "Assets/Art/UI/MiniCarte/MiniCarte_Traqueur.png", "Assets/Art/UI/Carte/Carte_Rose.png" })
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(f);
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
        var cat = Catalogue();
        var pf = new[] { Sauve(FinDeRunUI(cat), "Panneau_FinDeRun"), Sauve(PorteSortieUI(), "Panneau_PorteSortie"), Sauve(MarchandUI(cat), "Panneau_MarchandRangs") };
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
        EditorSceneManager.SaveScene(sc, SCENE);
        return "ok " + string.Join(" ", pf.Select(p => p.name));
    }

    public static string Capture(string visibles, string fond, string sortie)
    {
        EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var cv = Object.FindFirstObjectByType<Canvas>();
        var vis = visibles.Split('+');
        foreach (Transform t in cv.transform)
            if (t.name != "Fond_Jeu") t.gameObject.SetActive(vis.Contains(t.name));
        cv.transform.Find("Fond_Jeu").GetComponent<Image>().sprite = Spr($"{J}/Actes/{fond}.png");
        var fin = Object.FindFirstObjectByType<FinDeRun>(); if (fin) fin.RemplitApercu();
        var po = Object.FindFirstObjectByType<PanneauPorte>(); if (po) { po.MontreSortieScellee(po.secondesApercu, 200); if (po.groupe) po.groupe.alpha = 1; }
        var ma = Object.FindFirstObjectByType<MarchandRangs>(); if (ma) ma.RemplitApercu();
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
