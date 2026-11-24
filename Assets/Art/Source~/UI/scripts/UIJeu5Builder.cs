// Interface, lot 2 de la nuit 5 (2026-11-24) : améliorations aux couleurs des personnages (cadres des lanternes, génériques en
// fer), rang en étoiles, formes (carré = amélioration, losange = passif). Colonne verticale du HUD (de bas en haut, au-dessus de
// la barre de vie), infobulle, coffre et marchand (illustration, cadre, rang actuel → suivant, prix, relance du coffre), codex
// du menu principal (onglets par personnage avec leurs vignettes de choix, « Communes », grille, fiche détaillée).
// Même famille que les panneaux des nuits 3 et 4 (cadre des menus, cartouches, EB Garamond, Grenze Gotisch).
// `unity cmd ... script run --file UIJeu5Builder.cs --entry UIJeu5Builder.<Importe|Construit>`
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ItHunts.Art.Menus;
using ItHunts.Art.UIJeu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UIJeu5Builder
{
    const string J = "Assets/Art/UI/Jeu";
    const string PRE = "Assets/Art/UI/Jeu/Prefabs";
    const string AM = "Assets/Art/UI/Ameliorations";
    const string SCENE = "Assets/Art/Scenes/Test_UI_Jeu5.unity";
    const string TSV = "Assets/Art/Source~/UI/scripts/ameliorations/fiches.tsv";
    static readonly Color OR = new Color(0.969f, 0.882f, 0.702f);
    static readonly Color DESC = new Color(0.81f, 0.776f, 0.722f);
    static readonly Color AME = new Color(0.86f, 0.9f, 0.96f);
    static readonly Color FOND = new Color(0.055f, 0.043f, 0.07f, 0.94f);
    const string CADRE = "Assets/Art/UI/Menus/Commun/Menus_Cadre.png";
    const string LUEUR = "Assets/Art/UI/Menus/Commun/Menus_Lueur.png";
    const string FLAMME = "Assets/Art/UI/HUD/Ames_Flamme.png";
    // ordre de UpgradeCharacter (main) : générique, Renard, Caméléon, Koala, Hibou
    static readonly string[] FICHIERS_PERSO = { "Neutre", "Renard", "Cameleon", "Koala", "Hibou" };
    static readonly string[] VIGNETTES = { null, "Renard", "Cameleon", "Koala", "Hibou" };

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
    static RectTransform Plein(string nom, Transform p)
    {
        var rt = N(nom, p, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }
    static Image Img(RectTransform rt, Sprite sp, Color? c = null, bool ray = false, bool aspect = true)
    {
        var im = rt.gameObject.AddComponent<Image>(); im.sprite = sp; im.raycastTarget = ray; im.color = c ?? Color.white; im.preserveAspect = aspect;
        return im;
    }
    static Text Txt(RectTransform rt, Font f, int t, Color c, TextAnchor al, string s, bool ombre = true, bool wrap = false)
    {
        var x = rt.gameObject.AddComponent<Text>(); x.font = f; x.fontSize = t; x.color = c; x.alignment = al; x.text = s; x.raycastTarget = false;
        x.supportRichText = true; x.lineSpacing = 1.05f;
        x.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow; x.verticalOverflow = VerticalWrapMode.Overflow;
        if (ombre) { var o = rt.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.04f, 0.02f, 0.04f, 0.9f); o.effectDistance = new Vector2(1.5f, -1.5f); }
        return x;
    }
    static Image Cadre(Transform p, float w, float h, string nom = "Cadre", float ppu = 1f)
    {
        var im = Img(HG(nom, p, 0, 0, w, h), Spr(CADRE), null, true, false); im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = ppu; return im;
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
    static Image Filet(Transform p, string nom, float x, float y, float w, float h, Color c)
    {
        var im = Img(HG(nom, p, x, y, w, h), Spr(LUEUR), c, false, false);
        return im;
    }
    static GameObject Sauve(GameObject go, string nom)
    {
        Directory.CreateDirectory(PRE);
        var p = PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{nom}.prefab");
        Object.DestroyImmediate(go);
        return p;
    }

    // ------------------------------------------------------------------ import des cadres, des étoiles et des fonds
    public static string Importe()
    {
        var fichiers = new List<string>();
        foreach (var n in FICHIERS_PERSO) { fichiers.Add($"{AM}/Cadres/Cadre_{n}.png"); fichiers.Add($"{AM}/Cadres/Cadre_Passif_{n}.png"); }
        fichiers.Add($"{AM}/Cadres/Etoile_Rang.png"); fichiers.Add($"{AM}/Cadres/Etoile_Rang_Vide.png");
        if (Directory.Exists($"{J}/Fonds")) fichiers.AddRange(Directory.GetFiles($"{J}/Fonds", "*.jpg").Select(f => f.Replace('\\', '/')));
        int n0 = 0;
        foreach (var f in fichiers)
        {
            AssetDatabase.ImportAsset(f, ImportAssetOptions.ForceSynchronousImport);
            var ti = (TextureImporter)AssetImporter.GetAtPath(f);
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.alphaIsTransparency = true;
            ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.CompressedHQ; ti.wrapMode = TextureWrapMode.Clamp;
            ti.spritePixelsPerUnit = 100; ti.maxTextureSize = 2048;
            ti.SaveAndReimport(); n0++;
        }
        return "importés " + n0;
    }

    // ------------------------------------------------------------------ catalogue
    static CatalogueAmeliorations Catalogue()
    {
        string p = $"{AM}/Catalogue_Ameliorations.asset";
        var c = AssetDatabase.LoadAssetAtPath<CatalogueAmeliorations>(p);
        c.cadresPersonnage = FICHIERS_PERSO.Select(n => Spr($"{AM}/Cadres/Cadre_{n}.png")).ToArray();
        c.cadresPassifsPersonnage = FICHIERS_PERSO.Select(n => Spr($"{AM}/Cadres/Cadre_Passif_{n}.png")).ToArray();
        c.etoile = Spr($"{AM}/Cadres/Etoile_Rang.png");
        c.etoileVide = Spr($"{AM}/Cadres/Etoile_Rang_Vide.png");
        var fiches = new List<CatalogueAmeliorations.Fiche>();
        foreach (var l in File.ReadAllLines(TSV))
        {
            var t = l.Split('\t');
            if (t.Length < 9) continue;
            fiches.Add(new CatalogueAmeliorations.Fiche { id = t[0], nom = t[1], effet = t[2], rangs = t.Skip(3).Take(5).ToArray(), synergies = t[8] });
        }
        c.fiches = fiches.ToArray();
        EditorUtility.SetDirty(c); AssetDatabase.SaveAssets();
        return c;
    }

    static IconeAmelioration Icone(Transform p, string nom, float x, float y, float taille, CatalogueAmeliorations cat, bool vides = false)
    {
        var rt = HG(nom, p, x, y, taille, taille);
        var ia = rt.gameObject.AddComponent<IconeAmelioration>();
        ia.catalogue = cat;
        ia.etoilesVides = vides;
        var m = Plein("Masque", rt);
        Img(m, cat.masquePassif, Color.white, false, false);
        ia.masque = m.gameObject.AddComponent<Mask>(); ia.masque.showMaskGraphic = false;
        var z = Plein("Zone", m);
        ia.zoneIcone = z;
        ia.icone = Img(Plein("Icone", z), null, Color.white, false, true);
        ia.cadre = Img(Plein("Cadre", rt), cat.cadresPersonnage[0], Color.white, false, true);
        var et = Plein("Etoiles", rt);
        ia.etoiles = Enumerable.Range(0, 5).Select(i => Img(N("Etoile_" + (i + 1), et, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12)), cat.etoile)).ToArray();
        ia.Configure("G1", 0);
        return ia;
    }

    // ------------------------------------------------------------------ infobulle
    static InfobulleAmelioration Infobulle(Transform p, CatalogueAmeliorations cat)
    {
        const float W = 560, H = 300;
        var r = Plein("Infobulle", p);
        var ib = r.gameObject.AddComponent<InfobulleAmelioration>();
        ib.catalogue = cat;
        ib.groupe = r.gameObject.AddComponent<CanvasGroup>();
        var b = N("Boite", r, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Vector2(W, H));
        ib.boite = b;
        Img(HG("Fond", b, 10, 10, W - 20, H - 20), null, FOND, false, false);
        Cadre(b, W, H, "Cadre", 1.6f);
        var filet = Filet(b, "Filet", 150, 96, W - 180, 6, new Color(1, 1, 1, 0.75f));
        var lueur = Img(HG("Lueur", b, -30, -30, 250, 250), Spr(LUEUR), new Color(1, 1, 1, 0.35f), false, false);
        ib.icone = Icone(b, "Illustration", 26, 26, 112, cat);
        ib.nom = Txt(HG("Nom", b, 154, 26, W - 180, 44), GG("SemiBold"), 32, OR, TextAnchor.MiddleLeft, "Nom");
        ib.rang = Txt(HG("Rang", b, 154, 70, W - 180, 26), EB("Italic"), 20, DESC, TextAnchor.MiddleLeft, "Bronze · Commune");
        ib.effet = Txt(HG("Effet", b, 28, 152, W - 56, 64), EB("Regular"), 22, new Color(0.9f, 0.87f, 0.82f), TextAnchor.UpperLeft, "", true, true);
        ib.effetRang = Txt(HG("EffetRang", b, 28, 222, W - 56, 56), EB("Regular"), 22, OR, TextAnchor.UpperLeft, "", true, true);
        ib.teintes = new Graphic[] { filet, lueur };
        lueur.transform.SetSiblingIndex(0);
        return ib;
    }

    static GameObject InfobulleUI(CatalogueAmeliorations cat)
    {
        var r = C("HUD_InfobulleAmelioration", null, 0, 0, 1920, 1080);
        Infobulle(r, cat).Montre("G11", 2, r);
        return r.gameObject;
    }

    // ------------------------------------------------------------------ HUD : colonne
    static GameObject ColonneUI(CatalogueAmeliorations cat)
    {
        const float T = 58, PAS = 68;
        // ancrée en bas à gauche : coin de la barre de vie (32, 32) + hauteur de la barre (131 × 0.9) + écart 14 (HudSkin_Default)
        var r = N("HUD_ColonneAmeliorations", null, Vector2.zero, Vector2.zero, new Vector2(36, 32 + 118 + 14), new Vector2(T, PAS * 10));
        var col = r.gameObject.AddComponent<ColonneAmeliorations>();
        col.catalogue = cat;
        var cases = new List<IconeAmelioration>(); var lueurs = new List<Image>();
        for (int i = 0; i < 10; i++)
        {
            var c = N("Case_" + (i + 1), r, Vector2.zero, Vector2.zero, new Vector2(0, i * PAS + 6), new Vector2(T, T));
            lueurs.Add(Img(C("Lueur", c, 0, 0, T * 2.2f, T * 2.2f), Spr(LUEUR), Color.clear, false, false));
            var ia = Icone(c, "Icone", 0, 0, T, cat);
            ia.tailleEtoile = 0.3f; ia.pasEtoile = 0.21f;              // HUD : étoiles lisibles à 58 px
            var hit = Img(Plein("Survol", c), null, new Color(0, 0, 0, 0), true, false);
            var sv = c.gameObject.AddComponent<SurvolCase>(); sv.colonne = col; sv.index = i;
            cases.Add(ia);
        }
        col.cases = cases.ToArray(); col.lueurs = lueurs.ToArray();
        col.infobulle = Infobulle(r, cat);
        col.Remplit(new[] { "R-P1", "G11", "R5", "G5", "G24", "R2" }, new[] { 1, 2, 0, 3, 4, 1 });
        return r.gameObject;
    }

    // ------------------------------------------------------------------ coffre et marchand
    static PanneauOffres.Carte Carte(Transform p, string nom, float x, float y, float w, float h, CatalogueAmeliorations cat)
    {
        var rt = HG(nom, p, x, y, w, h);
        var c = new PanneauOffres.Carte { racine = rt };
        c.lueur = Img(C("Lueur", rt, 0, 0, w + 40, h + 30), Spr(LUEUR), Color.clear, false, false);
        var hit = Img(HG("Fond", rt, 8, 8, w - 16, h - 16), null, new Color(0.03f, 0.02f, 0.04f, 0.55f), true, false);
        var cad = Img(HG("Cadre", rt, 0, 0, w, h), Spr(CADRE), null, false, false); cad.type = Image.Type.Sliced; cad.pixelsPerUnitMultiplier = 1.6f;
        c.bouton = rt.gameObject.AddComponent<Button>(); c.bouton.transition = Selectable.Transition.None; c.bouton.targetGraphic = hit;
        c.etiquette = Txt(HG("Etiquette", rt, 0, 22, w, 26), EB("Italic"), 19, DESC, TextAnchor.MiddleCenter, "Nouvelle amélioration");
        float ic = 176, ic2 = 132;
        c.avant = Icone(rt, "Illustration", (w - ic) / 2f, 58, ic, cat);
        c.apres = Icone(rt, "RangSuivant", w - 30 - ic2, 78, ic2, cat);
        c.fleche = Txt(HG("Fleche", rt, w / 2f - 22, 118, 44, 44), EB("SemiBold"), 40, OR, TextAnchor.MiddleCenter, "→");
        c.nom = Txt(HG("Nom", rt, 0, 254, w, 42), GG("SemiBold"), 31, OR, TextAnchor.MiddleCenter, "Nom");
        c.liseré = Filet(rt, "Filet", w * 0.2f, 298, w * 0.6f, 5, new Color(1, 1, 1, 0.8f));
        c.rang = Txt(HG("Rang", rt, 0, 306, w, 32), EB("SemiBold"), 23, AME, TextAnchor.MiddleCenter, "Nouvelle → Bronze");
        c.description = Txt(HG("Description", rt, 26, 344, w - 52, 120), EB("Italic"), 20, DESC, TextAnchor.UpperCenter, "", true, true);
        c.flamme = Img(HG("Flamme", rt, w / 2f - 44, h - 60, 22, 36), Spr(FLAMME));
        c.prix = Txt(HG("Prix", rt, w / 2f - 14, h - 64, 120, 44), EB("SemiBold"), 32, AME, TextAnchor.MiddleLeft, "60");
        c.corps = new Graphic[] { cad };
        return c;
    }

    static GameObject OffresUI(CatalogueAmeliorations cat, bool coffre)
    {
        float W = 1260, H = coffre ? 750 : 780, CW = 370, CH = coffre ? 490 : 520;
        var r = C(coffre ? "Panneau_Offres_Coffre" : "Panneau_Offres_Marchand", null, 0, 0, W, H);
        Img(C("Voile", r, 0, 0, 4000, 3000), null, new Color(0.02f, 0.0f, 0.01f, 0.62f), true);
        Img(HG("Fond", r, 14, 14, W - 28, H - 28), null, FOND, false, false);
        Cadre(r, W, H);
        var po = r.gameObject.AddComponent<PanneauOffres>();
        po.catalogue = cat; po.coffre = coffre;
        po.titre = Txt(HG("Titre", r, 0, 30, W, 70), GG("Bold"), 60, OR, TextAnchor.MiddleCenter, coffre ? "Coffre" : "Marchand");
        Txt(HG("SousTitre", r, 0, 96, W, 26), EB("Italic"), 20, DESC, TextAnchor.MiddleCenter, coffre ? "Choisissez une amélioration" : "Âmes contre améliorations");
        po.flammeSolde = Img(HG("Flamme_Solde", r, W - 190, 50, 24, 40), Spr(FLAMME));
        po.solde = Txt(HG("Solde", r, W - 160, 46, 120, 44), EB("SemiBold"), 34, AME, TextAnchor.MiddleLeft, "120");
        float gx = (W - 3 * CW - 2 * 36) / 2f;
        po.cartes = Enumerable.Range(0, 3).Select(i => Carte(r, "Carte_" + (i + 1), gx + i * (CW + 36), 138, CW, CH, cat)).ToArray();
        po.illustrationSoin = Spr($"{AM}/Icones/ICO_G19.png");
        if (coffre)
        {
            po.relancer = Cartouche(r, "Relancer", W / 2f - 300, H - 98, 260, 62, "Relancer");
            po.flammeRelance = Img(HG("Flamme_Relance", r, W / 2f - 300 + 270, H - 86, 20, 34), Spr(FLAMME));
            po.prixRelance = Txt(HG("Prix_Relance", r, W / 2f - 300 + 296, H - 90, 90, 40), EB("SemiBold"), 28, AME, TextAnchor.MiddleLeft, "25");
            po.fermer = Cartouche(r, "Fermer", W / 2f + 60, H - 98, 260, 62, "Fermer");
            po.apercu = new[]
            {
                new PanneauOffres.Offre { id = "K3", nom = Nom(cat, "K3"), rangActuel = -1, description = Desc(cat, "K3", 0) },
                new PanneauOffres.Offre { id = "G12", nom = Nom(cat, "G12"), rangActuel = 1, description = Desc(cat, "G12", 2) },
                new PanneauOffres.Offre { id = "K-P2", nom = Nom(cat, "K-P2"), rangActuel = -1, description = Desc(cat, "K-P2", 0) },
            };
            po.apercuAmes = 85; po.apercuRelance = 25;
        }
        else
        {
            po.fermer = Cartouche(r, "Fermer", (W - 260) / 2f, H - 98, 260, 62, "Fermer");
            po.apercu = new[]
            {
                new PanneauOffres.Offre { id = "H4", nom = Nom(cat, "H4"), rangActuel = -1, description = Desc(cat, "H4", 0), prix = 60 },
                new PanneauOffres.Offre { id = "G5", nom = Nom(cat, "G5"), rangActuel = 2, description = Desc(cat, "G5", 3), prix = 220 },
                new PanneauOffres.Offre { soin = true, nom = "Soin", soinPV = 40, description = "Referme vos plaies : 40 points de vie tout de suite.", prix = 35 },
            };
            po.apercuAmes = 150;
        }
        po.RemplitApercu();
        return r.gameObject;
    }

    static string Nom(CatalogueAmeliorations c, string id) => c.FicheDe(id)?.nom ?? id;
    static string Desc(CatalogueAmeliorations c, string id, int rang)
    {
        var f = c.FicheDe(id);
        if (f == null) return "";
        return f.effet + " : " + f.rangs[Mathf.Clamp(rang, 0, 4)] + ".";
    }

    // ------------------------------------------------------------------ codex
    static GameObject CodexUI(CatalogueAmeliorations cat)
    {
        const float W = 1760, H = 1010;
        var r = C("Menu_Codex", null, 0, 0, 1920, 1080);
        Img(Plein("Fond_Peint", r), Spr("Assets/Art/UI/Menus/Choix/Choix_Fond.png"), new Color(0.42f, 0.4f, 0.46f), false, false);
        Img(Plein("Voile", r), null, new Color(0.02f, 0.01f, 0.03f, 0.45f), true, false);
        var p = C("Panneau", r, 0, 0, W, H);
        Img(HG("Fond", p, 14, 14, W - 28, H - 28), null, FOND, false, false);
        Cadre(p, W, H);
        var cx = r.gameObject.AddComponent<CodexAmeliorations>();
        cx.catalogue = cat;
        Txt(HG("Titre", p, 0, 26, W, 72), GG("Bold"), 62, OR, TextAnchor.MiddleCenter, "Codex");
        Txt(HG("SousTitre", p, 0, 92, W, 26), EB("Italic"), 21, DESC, TextAnchor.MiddleCenter, "Les améliorations de chaque compagnon, et celles de tous");
        // onglets
        var onglets = new List<CodexAmeliorations.Onglet>();
        float ox = 80, ow = 150, og = 22;
        for (int i = 0; i < 5; i++)
        {
            var o = new CodexAmeliorations.Onglet();
            var t = HG("Onglet_" + CatalogueAmeliorations.Personnages[i], p, ox + i * (ow + og), 124, ow, 172);
            var hit = Img(t, null, new Color(0, 0, 0, 0), true, false);
            o.bouton = t.gameObject.AddComponent<Button>(); o.bouton.transition = Selectable.Transition.None; o.bouton.targetGraphic = hit;
            if (VIGNETTES[i] != null)
            {
                o.discrete = Spr($"Assets/Art/UI/Menus/Choix/Choix_Vignette_{VIGNETTES[i]}_Discrete.png");
                o.illuminee = Spr($"Assets/Art/UI/Menus/Choix/Choix_Vignette_{VIGNETTES[i]}_Illuminee.png");
                o.vignette = Img(HG("Vignette", t, 13, 0, 124, 124), o.discrete);
            }
            else
            {
                // Communes : l'orbe (Grande orbe) dans le cadre de fer, à la taille des vignettes
                var ia = Icone(t, "Vignette_Commune", 15, 2, 120, cat);
                ia.Configure("G5", 0); ia.PoseEtoiles(0);
                o.vignette = ia.cadre;
                o.discrete = o.illuminee = ia.cadre.sprite;
                o.autres = new Graphic[] { ia.icone };
            }
            o.libelle = Txt(HG("Libelle", t, 0, 128, ow, 30), EB("SemiBold"), 23, OR, TextAnchor.MiddleCenter, CatalogueAmeliorations.Personnages[i]);
            o.souligne = Filet(t, "Souligne", 20, 162, ow - 40, 6, cat.Couleur(i));
            onglets.Add(o);
        }
        cx.onglets = onglets.ToArray();
        cx.compteur = Txt(HG("Compteur", p, 80, 306, 860, 28), EB("Italic"), 21, DESC, TextAnchor.MiddleLeft, "");
        // grille
        const float T = 94, PAS = 114;
        var grille = HG("Grille", p, 96, 344, 6 * PAS, 5 * PAS);
        cx.selection = Img(HG("Selection", grille, 0, 0, T * 1.3f, T * 1.3f), Spr(LUEUR), new Color(1, 1, 1, 0.35f), false, false);
        cx.cases = Enumerable.Range(0, 30).Select(i =>
        {
            var ia = Icone(grille, "Case_" + (i + 1), (i % 6) * PAS + (PAS - T) / 2f, (i / 6) * PAS + 4, T, cat);
            Img(Plein("Survol", (RectTransform)ia.transform), null, new Color(0, 0, 0, 0), true, false);
            return ia;
        }).ToArray();
        cx.colonnes = 6;
        // séparation et fiche
        Filet(p, "Separation", 920, 150, 4, H - 260, new Color(0.6f, 0.55f, 0.62f, 0.35f));
        var f = HG("Fiche", p, 960, 140, W - 1040, H - 250);
        var fl = Img(HG("Lueur", f, -60, -50, 340, 340), Spr(LUEUR), new Color(1, 1, 1, 0.3f), false, false);
        cx.ficheIcone = Icone(f, "Illustration", 0, 10, 220, cat, true);
        float fx = 250, fw = W - 1040 - 250;
        cx.ficheNom = Txt(HG("Nom", f, fx, 20, fw, 60), GG("SemiBold"), 46, OR, TextAnchor.MiddleLeft, "Nom");
        cx.ficheType = Txt(HG("Type", f, fx, 84, fw, 30), EB("Italic"), 22, DESC, TextAnchor.MiddleLeft, "Commune · amélioration");
        var filet = Filet(f, "Filet", fx, 122, fw * 0.8f, 6, Color.white);
        cx.ficheEffet = Txt(HG("Effet", f, fx, 140, fw, 96), EB("Regular"), 24, new Color(0.92f, 0.89f, 0.84f), TextAnchor.UpperLeft, "", true, true);
        float ry = 268;
        cx.ficheRangs = new Text[5];
        for (int k = 0; k < 5; k++)
        {
            var ligne = HG("Rang_" + CatalogueAmeliorations.Rangs[k], f, 0, ry + k * 64, W - 1040, 58);
            if (k % 2 == 0) Img(HG("Bande", ligne, 0, 0, W - 1040, 58), null, new Color(0.62f, 0.55f, 0.66f, 0.045f), false, false);
            for (int s = 0; s <= k; s++) Img(HG("Etoile_" + (s + 1), ligne, 12 + s * 22, 17, 24, 24), cat.etoile);
            Txt(HG("NomRang", ligne, 132, 0, 120, 58), EB("SemiBold"), 22, k == 4 ? OR : DESC, TextAnchor.MiddleLeft, CatalogueAmeliorations.Rangs[k]);
            cx.ficheRangs[k] = Txt(HG("Texte", ligne, 252, 0, W - 1040 - 262, 58), EB("Regular"), 22, k == 4 ? OR : new Color(0.9f, 0.87f, 0.82f), TextAnchor.MiddleLeft, "", true, true);
        }
        cx.ficheSynergies = Txt(HG("Synergies", f, 0, ry + 5 * 64 + 16, W - 1040, 60), EB("Italic"), 21, DESC, TextAnchor.UpperLeft, "", true, true);
        cx.teintes = new Graphic[] { filet, fl };
        cx.fermer = Cartouche(p, "Retour", W - 330, H - 96, 250, 60, "Retour");
        Txt(HG("Aide", p, 80, H - 82, 800, 40), EB("Italic"), 20, new Color(0.6f, 0.57f, 0.55f), TextAnchor.MiddleLeft, "Q / E ou gâchettes : onglet   ·   flèches ou stick : choisir");
        cx.Remplit(cat.fiches);
        cx.ChoisitOnglet(0);
        return r.gameObject;
    }

    public static string Construit()
    {
        var cat = Catalogue();
        var pf = new[]
        {
            Sauve(ColonneUI(cat), "HUD_ColonneAmeliorations"), Sauve(InfobulleUI(cat), "HUD_InfobulleAmelioration"),
            Sauve(OffresUI(cat, true), "Panneau_Offres_Coffre"), Sauve(OffresUI(cat, false), "Panneau_Offres_Marchand"),
            Sauve(CodexUI(cat), "Menu_Codex"),
        };
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

    /// <summary>Aperçu : visibles = noms d'enfants du canvas séparés par « + » ; fond = sprite de Actes (Acte_I…) ou chemin.</summary>
    public static string Capture(string visibles, string fond, string sortie)
    {
        EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var cv = Object.FindFirstObjectByType<Canvas>();
        var vis = visibles.Split('+');
        foreach (Transform t in cv.transform)
            if (t.name != "Fond_Jeu") t.gameObject.SetActive(vis.Contains(t.name));
        var sp = fond.Contains("/") ? Spr(fond) : Spr($"{J}/Actes/{fond}.png");
        cv.transform.Find("Fond_Jeu").GetComponent<Image>().sprite = sp;
        foreach (var po in Object.FindObjectsByType<PanneauOffres>(FindObjectsSortMode.None)) po.RemplitApercu();
        var col = Object.FindFirstObjectByType<ColonneAmeliorations>();
        if (col) { col.Remplit(new[] { "R-P1", "G11", "R5", "G5", "G24", "R2", "G13", "R7" }, new[] { 1, 2, 0, 3, 4, 1, 0, 2 }); col.Survole(1); if (col.infobulle) col.infobulle.Immediat(); }
        var ib = Object.FindObjectsByType<InfobulleAmelioration>(FindObjectsSortMode.None).FirstOrDefault(b => b.GetComponentInParent<ColonneAmeliorations>() == null);
        if (ib) { ib.Montre("C3", 2, (RectTransform)ib.transform); ib.Immediat(); }
        var cx = Object.FindFirstObjectByType<CodexAmeliorations>();
        if (cx) { cx.Remplit(Catalogue().fiches); cx.ChoisitOnglet(1); cx.Choisit(4); }
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
