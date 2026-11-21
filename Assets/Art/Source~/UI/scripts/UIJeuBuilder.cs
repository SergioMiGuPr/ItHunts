// Interface en jeu (lot C, 2026-11-21) : import des pièces, prefabs et scène d'aperçu, captures.
// Lancé par `unity cmd ... script run --file UIJeuBuilder.cs --entry UIJeuBuilder.<Méthode>` (éditeur du projet art).
//   Importe   : réglages d'import des sprites de Assets/Art/UI/Jeu
//   Construit : prefabs HUD_Reticule, HUD_Charges, HUD_Parade, Panneau_Marchand, Panneau_Coffre + scène Test_UI_Jeu
//   Capture   : rendu de la scène dans un état donné (docs/art/rendus/UI/Jeu/)
// Pièces de la famille des maquettes seulement : Reticule_Etoile, Charge_*, Parade_Icone (ui_jeu.py), Menus_Cadre et
// Menus_Cartouche_* (menus), Ames_Cadre / Ames_Fond / Ames_Flamme (HUD), Menus_Lueur (lueurs), icônes de la mini-carte
// (aperçu des cartes). Textes en EB Garamond.
using System;
using System.IO;
using System.Linq;
using ItHunts.Art.Menus;
using ItHunts.Art.UIJeu;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class UIJeuBuilder
{
    const string J = "Assets/Art/UI/Jeu";
    const string PRE = "Assets/Art/UI/Jeu/Prefabs";
    const string SCENE = "Assets/Art/Scenes/Test_UI_Jeu.unity";
    const string FONTS = "Assets/Art/UI/Polices/EBGaramond/";
    static readonly Color OR = new Color(0.969f, 0.882f, 0.702f);      // #F7E1B3, noms des menus
    static readonly Color DESC = new Color(0.81f, 0.776f, 0.722f);

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    public static string Importe()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        int n = 0;
        foreach (var f in Directory.GetFiles(J, "*.png"))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(f.Replace("\\", "/"));
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            n++;
        }
        return n + " sprites";
    }

    static Sprite Spr(string chemin) => AssetDatabase.LoadAssetAtPath<Sprite>(chemin);
    static Sprite SJ(string nom) => Spr($"{J}/{nom}.png");
    static Font Police(string style) => AssetDatabase.LoadAssetAtPath<Font>(FONTS + "EBGaramond-" + style + ".ttf");

    static RectTransform Noeud(string nom, Transform parent, Vector2 ancre, Vector2 pivot, Vector2 pos, Vector2 taille)
    {
        var go = new GameObject(nom, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = ancre;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = taille;
        return rt;
    }

    // position depuis le haut gauche d'un parent (x vers la droite, y vers le bas), en unités du canvas 1080p
    static RectTransform HG(string nom, Transform parent, float x, float y, float w, float h) =>
        Noeud(nom, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(w, h));

    static Image Img(RectTransform rt, Sprite sp, Color? c = null, bool raycast = false)
    {
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = sp; im.raycastTarget = raycast; im.color = c ?? Color.white; im.preserveAspect = true;
        return im;
    }

    static Text Txt(RectTransform rt, Font f, int taille, Color c, TextAnchor al, string s)
    {
        var t = rt.gameObject.AddComponent<Text>();
        t.font = f; t.fontSize = taille; t.color = c; t.alignment = al; t.text = s; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = rt.gameObject.AddComponent<Shadow>(); o.effectColor = new Color(0, 0, 0, 0.8f); o.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    // ------------------------------------------------------------------ HUD
    static GameObject Reticule()
    {
        var r = Noeud("HUD_Reticule", null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64));
        var lu = Img(Noeud("Lueur", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48, 48)), Spr("Assets/Art/UI/Menus/Commun/Menus_Lueur.png"), new Color(1, 1, 1, 0));
        var et = Img(Noeud("Etoile", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30)), SJ("Reticule_Etoile"));
        var c = r.gameObject.AddComponent<ReticuleJeu>();
        c.etoile = et; c.lueur = lu;
        return r.gameObject;
    }

    static GameObject Charges()
    {
        // 3 orbes de 24 px, espacées de 34 px, centrées ; l'ensemble est posé à 132 px du bas de l'écran
        var r = Noeud("HUD_Charges", null, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 132), new Vector2(120, 40));
        var c = r.gameObject.AddComponent<ChargesJeu>();
        c.charges = new ChargesJeu.Charge[3];
        for (int i = 0; i < 3; i++)
        {
            var slot = Noeud("Charge_" + (i + 1), r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 34f, 0), new Vector2(24, 24));
            var lu = Img(Noeud("Lueur", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52, 52)), Spr("Assets/Art/UI/Menus/Commun/Menus_Lueur.png"), new Color(0.36f, 0.72f, 1f, 0.35f));
            var vi = Img(Noeud("Vide", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26)), SJ("Charge_Vide"));
            var pl = Img(Noeud("Pleine", slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24, 24)), SJ("Charge_Pleine"));
            pl.type = Image.Type.Filled; pl.fillMethod = Image.FillMethod.Vertical; pl.fillOrigin = (int)Image.OriginVertical.Bottom;
            c.charges[i] = new ChargesJeu.Charge { vide = vi, pleine = pl, lueur = lu };
        }
        return r.gameObject;
    }

    static GameObject Parade()
    {
        // à droite des charges (même ligne), 36 px
        var r = Noeud("HUD_Parade", null, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(96, 152), new Vector2(36, 36));
        var lu = Img(Noeud("Lueur", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(64, 64)), Spr("Assets/Art/UI/Menus/Commun/Menus_Lueur.png"), new Color(1, 1, 1, 0));
        var ic = Img(Noeud("Icone", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36)), SJ("Parade_Icone"));
        var vo = Img(Noeud("Voile", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30)), Spr("Assets/Art/UI/Menus/Commun/Menus_Lueur.png"), new Color(0, 0, 0, 0.72f));
        vo.sprite = SJ("Parade_Icone");
        vo.type = Image.Type.Filled; vo.fillMethod = Image.FillMethod.Radial360; vo.fillOrigin = (int)Image.Origin360.Top; vo.fillClockwise = false;
        var c = r.gameObject.AddComponent<ParadeJeu>();
        c.icone = ic; c.voile = vo; c.lueur = lu;
        return r.gameObject;
    }

    // ------------------------------------------------------------------ panneaux
    static RectTransform Cartouche(Transform parent, string nom, float x, float y, float w, float h, string libelle, out Button bouton)
    {
        var rt = HG(nom, parent, x, y, w, h);
        float s = h / 72f;
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/UI/Menus/Materiaux/M_UI_Bouton.mat");
        string C = "Assets/Art/UI/Menus/Commun/Menus_Cartouche_";
        float g = 34 * s, fl = 36 * s;
        Image P(string n, float px, float pw)
        {
            var im = Img(HG(n, rt, px, 0, pw, h), Spr(C + n.Substring(n.Length - 1) + ".png"));
            im.preserveAspect = false; im.material = mat; im.color = new Color(0.5f, 0.5f, 0.5f, 1);
            return im;
        }
        var parts = new[] { P("Bout_G", 0, g), P("Tranche_M", g - 0.5f, (w - fl) / 2f - g + 1), P("Fleuron_C", (w - fl) / 2f, fl),
                            P("Tranche2_M", (w + fl) / 2f - 0.5f, (w - fl) / 2f - g + 1), P("Bout_D", w - g, g) };
        parts[0].raycastTarget = true;
        var hit = rt.gameObject.AddComponent<Image>(); hit.color = new Color(0, 0, 0, 0); hit.raycastTarget = true;
        bouton = rt.gameObject.AddComponent<Button>(); bouton.transition = Selectable.Transition.None; bouton.targetGraphic = hit;
        var tg = rt.gameObject.AddComponent<TeinteGroupe>(); tg.cibles = parts;
        Txt(Noeud("Texte", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -1), new Vector2(w, h)), Police("SemiBold"), Mathf.RoundToInt(h * 0.42f), OR, TextAnchor.MiddleCenter, libelle);
        return rt;
    }

    static CarteAmelioration Carte(Transform parent, string nom, float x, float y, float w, float h)
    {
        var rt = HG(nom, parent, x, y, w, h);
        float s = h / 204f;                          // échelle du cartouche des âmes (sprite 607 x 204)
        var lu = Img(Noeud("Lueur", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w + 90, h + 70)), Spr("Assets/Art/UI/Menus/Commun/Menus_Lueur.png"), new Color(1, 1, 1, 0));
        lu.preserveAspect = false;
        Image Sl(string n, string sp)
        {
            var im = Img(HG(n, rt, 0, 0, w, h), Spr("Assets/Art/UI/HUD/" + sp + ".png"));
            im.preserveAspect = false; im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 1f / s; im.raycastTarget = n == "Fond";
            return im;
        }
        var fond = Sl("Fond", "Ames_Fond");
        var cadre = Sl("Cadre", "Ames_Cadre");
        // rond de l'icône : centre (116, 102) px du sprite, diamètre intérieur 102 px
        float d = 102 * s * 0.86f;
        var ic = Img(HG("Icone", rt, 116 * s - d / 2f, 102 * s - d / 2f, d, d), null);
        var no = Txt(HG("Nom", rt, 182 * s, 0, w - 182 * s - 120, h), Police("Regular"), Mathf.RoundToInt(h * 0.33f), OR, TextAnchor.MiddleLeft, "Amélioration");
        no.horizontalOverflow = HorizontalWrapMode.Overflow;
        var fl = Img(HG("Flamme", rt, w - 112, h * 0.22f, h * 0.36f, h * 0.56f), Spr("Assets/Art/UI/HUD/Ames_Flamme.png"));
        var px = Txt(HG("Prix", rt, w - 212, 0, 92, h), Police("SemiBold"), Mathf.RoundToInt(h * 0.33f), new Color(0.86f, 0.9f, 0.96f), TextAnchor.MiddleRight, "0");
        var b = rt.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = fond;
        var c = rt.gameObject.AddComponent<CarteAmelioration>();
        c.icone = ic; c.nom = no; c.prix = px; c.flamme = fl; c.lueur = lu; c.corps = new Graphic[] { fond, cadre };
        return c;
    }

    static GameObject Panneau(string nom, string titre, int nb, PanneauBoutique.Offre[] offres, int ames, string invite)
    {
        const float W = 760, H = 720;
        var r = Noeud(nom, null, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(W, H));
        var voile = Img(Noeud("Voile", r, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 3000)), null, new Color(0, 0, 0, 0.55f), true);
        var cadre = Img(HG("Cadre", r, 0, 0, W, H), Spr("Assets/Art/UI/Menus/Commun/Menus_Cadre.png"));
        cadre.preserveAspect = false; cadre.type = Image.Type.Sliced; cadre.raycastTarget = true;
        var p = r.gameObject.AddComponent<PanneauBoutique>();
        p.titre = Txt(HG("Titre", r, 0, 46, W, 60), Police("Regular"), 44, OR, TextAnchor.MiddleCenter, titre);
        // solde d'âmes (haut droit, dans le cadre)
        Img(HG("Solde_Flamme", r, W - 176, 62, 20, 32), Spr("Assets/Art/UI/HUD/Ames_Flamme.png"));
        p.solde = Txt(HG("Solde", r, W - 152, 56, 80, 44), Police("SemiBold"), 26, new Color(0.86f, 0.9f, 0.96f), TextAnchor.MiddleLeft, ames.ToString());
        p.cartes = new CarteAmelioration[nb];
        for (int i = 0; i < nb; i++)
            p.cartes[i] = Carte(r, "Carte_" + (i + 1), 80, 128 + i * 94, 600, 82);
        float yd = 128 + nb * 94 + 6;
        p.description = Txt(HG("Description", r, 90, yd, W - 180, 610 - yd), Police("Italic"), 22, DESC, TextAnchor.MiddleCenter, invite);
        Cartouche(r, "Fermer", (W - 220) / 2f, 612, 220, 56, "Fermer", out Button bf);
        p.fermer = bf;
        p.invite = invite;
        p.apercu = offres;
        p.amesApercu = ames;
        return r.gameObject;
    }

    static PanneauBoutique.Offre O(string icone, string nom, int prix, string desc) =>
        new PanneauBoutique.Offre { icone = Spr("Assets/Art/UI/MiniCarte/MiniCarte_" + icone + ".png"), nom = nom, prix = prix, description = desc };

    static GameObject Sauve(GameObject go, string nom)
    {
        Directory.CreateDirectory(PRE);
        var p = PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{nom}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return p;
    }

    public static string Construit()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Importe();
        // textes d'aperçu : améliorations existantes du jeu (UpgradeKind), prix factices
        var offresM = new[]
        {
            O("Joueur_Bleu", "Charge de mouvement", 60, "Une charge de dash et de saut en l'air en plus."),
            O("Flamme", "Rayon de collecte", 45, "L'orbe aspire les âmes de plus loin."),
            O("Allie_Mort", "Points de vie", 150, "Plus de points de vie maximum, et autant de soin tout de suite."),
            O("Coffre_Rare", "Balles du barillet", 80, "Le barillet gagne une balle."),
        };
        var offresC = new[]
        {
            O("Joueur_Bleu", "Charge de mouvement", 0, "Une charge de dash et de saut en l'air en plus."),
            O("Flamme", "Aspiration des âmes", 0, "Les âmes rejoignent l'orbe plus vite."),
            O("Coffre_Rare", "Balles du barillet", 0, "Le barillet gagne une balle."),
        };
        var pr = Sauve(Reticule(), "HUD_Reticule");
        var pc = Sauve(Charges(), "HUD_Charges");
        var pp = Sauve(Parade(), "HUD_Parade");
        var pm = Sauve(Panneau("Panneau_Marchand", "Marchand", 4, offresM, 120, "Survole une amélioration pour la lire."), "Panneau_Marchand");
        var pk = Sauve(Panneau("Panneau_Coffre", "Coffre", 3, offresC, 120, "Choisis une amélioration."), "Panneau_Coffre");

        // scène d'aperçu
        var sc = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.035f, 0.04f, 0.06f); cam.orthographic = true;
        var can = new GameObject("Canvas", typeof(RectTransform)).AddComponent<Canvas>();
        can.gameObject.layer = 5;
        can.renderMode = RenderMode.ScreenSpaceCamera; can.worldCamera = cam; can.planeDistance = 1;
        var cs = can.gameObject.AddComponent<CanvasScaler>(); cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080); cs.matchWidthOrHeight = 1;
        can.gameObject.AddComponent<GraphicRaycaster>();
        var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();
        // fond d'aperçu : une capture sombre du niveau si elle existe, sinon la couleur de la caméra
        GameObject I(GameObject pf) => (GameObject)PrefabUtility.InstantiatePrefab(pf, can.transform);
        var ap = can.gameObject.AddComponent<UIJeuApercu>();
        ap.reticule = I(pr).GetComponent<ReticuleJeu>();
        ap.charges = I(pc).GetComponent<ChargesJeu>();
        ap.parade = I(pp).GetComponent<ParadeJeu>();
        ap.marchand = I(pm).GetComponent<PanneauBoutique>();
        ap.coffre = I(pk).GetComponent<PanneauBoutique>();
        EditorSceneManager.SaveScene(sc, SCENE);
        AssetDatabase.SaveAssets();
        return "prefabs et scène";
    }

    // état : "reticule=0|1|2;charges=3|2.5|...;parade=0..1;contre=0..1;panneau=-|M|K;survol=-1..3"
    public static string Capture(string etat, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        if (SceneManager.GetActiveScene().path != SCENE)
            EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var ap = UnityEngine.Object.FindFirstObjectByType<UIJeuApercu>();
        var can = ap.GetComponent<Canvas>();
        var d = etat.Split(';').Where(x => x.Contains("=")).ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);
        string V(string k, string def) => d.TryGetValue(k, out var v) ? v : def;
        float F(string k, float def) => float.Parse(V(k, def.ToString(System.Globalization.CultureInfo.InvariantCulture)), System.Globalization.CultureInfo.InvariantCulture);
        ap.reticule.Pose((EtatReticule)int.Parse(V("reticule", "0")));
        float ch = F("charges", 3);
        foreach (var c in ap.charges.charges) c.valeur = 0;
        ap.charges.RegleTout(Mathf.FloorToInt(ch), ch - Mathf.Floor(ch));
        foreach (var c in ap.charges.charges) c.eclat = 0;
        ap.charges.Rafraichit();
        ap.parade.Pose(F("parade", 1), F("contre", 0));
        string pan = V("panneau", "-");
        ap.marchand.gameObject.SetActive(pan == "M");
        ap.coffre.gameObject.SetActive(pan == "K");
        foreach (var p in new[] { ap.marchand, ap.coffre })
        {
            if (!p.gameObject.activeSelf) continue;
            p.Remplit(p.apercu, int.Parse(V("ames", p.amesApercu.ToString())));
            int sv = int.Parse(V("survol", "-1"));
            for (int i = 0; i < p.cartes.Length; i++) p.cartes[i].ForceSurvol(i == sv);
            if (sv >= 0 && sv < p.cartes.Length) p.cartes[sv].ForceSurvol(true);
        }
        var cam = can.worldCamera;
        int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        for (int i = 0; i < 2; i++) { cam.Render(); Canvas.ForceUpdateCanvases(); }
        cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = old;
        string ps = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(ps));
        File.WriteAllBytes(ps, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        return sortie;
    }
}
