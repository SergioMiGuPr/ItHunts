// ItHunts (DA) : constructeur des menus. Lancé par la CLI Unity (unity cmd script run --file MenusBuilder.cs --entry
// MenusBuilder.<Etape>), pas compilé dans le projet (dossier Source~). Étapes :
//   Importe    : réglages d'import des textures des menus (Sprite, PPU 100, Full Rect, sans mipmaps, non compressé,
//                données linéaires pour les masques, bordures 9 tranches)
//   Construit  : matériaux, prefabs (Titre, Campement, Choix, Pause, Paramètres) et scène Test_UI_Menus
//   Capture    : rendus gelés de chaque écran à la taille de la maquette (docs/art/rendus/UI/Menus/)
// Les rects viennent de construction.txt (export.py) : pixels de la maquette, coin haut gauche.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ItHunts.Art.Menus;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MenusBuilder
{
    const string M = "Assets/Art/UI/Menus";
    const string MAT = "Assets/Art/UI/Menus/Materiaux";
    const string PRE = "Assets/Art/UI/Menus/Prefabs";
    const string SCENE = "Assets/Art/Scenes/Test_UI_Menus.unity";
    const string SRC = "Assets/Art/Source~/UI/scripts/menus/construction.txt";
    const string FONTS = "Assets/Art/UI/Polices/EBGaramond/";
    static readonly CultureInfo CI = CultureInfo.InvariantCulture;

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    // ------------------------------------------------------------------ données
    static Dictionary<string, float[]> _pieces, _valeurs, _textes;
    static List<(string perso, int place, float[] v)> _places;

    static void Lit()
    {
        _pieces = new Dictionary<string, float[]>(); _valeurs = new Dictionary<string, float[]>();
        _textes = new Dictionary<string, float[]>(); _places = new List<(string, int, float[])>();
        foreach (string l in File.ReadAllLines(Path.Combine(Directory.GetCurrentDirectory(), SRC)))
        {
            string[] t = l.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 2) continue;
            float[] F(int from) => t.Skip(from).Select(x => float.Parse(x, CI)).ToArray();
            switch (t[0])
            {
                case "piece": _pieces[t[2]] = F(3); break;
                case "valeur": _valeurs[t[1]] = F(2); break;
                case "texte": _textes[t[1] + "/" + t[2]] = F(3); break;
                case "place": _places.Add((t[1], int.Parse(t[2]), F(3))); break;
            }
        }
    }

    // ------------------------------------------------------------------ import
    public static string Importe()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { M }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        int n = 0;
        foreach (string p in paths)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            string f = Path.GetFileNameWithoutExtension(p);
            bool donnees = f.Contains("Masque") || f.Contains("Poids") || f.EndsWith("_Lueur");
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect; s.spriteGenerateFallbackPhysicsShape = false;
            ti.SetTextureSettings(s);
            ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear; ti.wrapMode = TextureWrapMode.Clamp;
            ti.sRGBTexture = !donnees;
            ti.alphaIsTransparency = !donnees;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.Uncompressed;   // pixel pour pixel (captures mesurées)
            ti.isReadable = false;
            Vector4 b = Vector4.zero;
            if (f == "Menus_Cadre") { float c = 100; b = new Vector4(c, c, c, c); }
            ti.spriteBorder = b;
            ti.SaveAndReimport();
            n++;
        }
        // audio du menu : musique en flux (2 min), feu compressé en mémoire ; boucles préparées par boucles_audio.py
        string a = "";
        foreach (var (p, flux) in new[] { ("Assets/Art/Audio/Menu/Musique_Menu_Boucle.wav", true), ("Assets/Art/Audio/Menu/Sfx_Feu_Boucle.wav", false) })
        {
            var ai = AssetImporter.GetAtPath(p) as AudioImporter;
            if (ai == null) { a += " (absent : " + p + ")"; continue; }
            ai.forceToMono = false; ai.loadInBackground = flux;
            var ss = ai.defaultSampleSettings;
            ss.loadType = flux ? AudioClipLoadType.Streaming : AudioClipLoadType.CompressedInMemory;
            ss.compressionFormat = AudioCompressionFormat.Vorbis; ss.quality = flux ? 0.7f : 0.6f;
            ss.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate; ss.preloadAudioData = !flux;
            ai.defaultSampleSettings = ss;
            ai.SaveAndReimport();
            a += " " + Path.GetFileName(p);
        }
        return n + " textures réglées ; audio :" + a;
    }

    // ------------------------------------------------------------------ outils de construction
    static Sprite Spr(string dossier, string nom)
    {
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>($"{M}/{dossier}/{nom}.png");
        if (sp == null) throw new Exception("sprite introuvable : " + dossier + "/" + nom);
        return sp;
    }

    static Texture2D Tex(string dossier, string nom) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{M}/{dossier}/{nom}.png");

    static RectTransform Noeud(string nom, Transform parent)
    {
        var go = new GameObject(nom, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static RectTransform Plein(string nom, Transform parent, Vector2 taille)
    {
        var rt = Noeud(nom, parent);
        MenuEcran.Pose(rt, 0, 0, taille.x, taille.y);
        return rt;
    }

    static Image Img(Transform parent, string nom, Sprite sp, float[] r, Material mat = null, bool raycast = false)
    {
        var rt = Noeud(nom, parent);
        MenuEcran.Pose(rt, r[0], r[1], r[2], r[3]);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = sp; im.material = mat; im.raycastTarget = raycast; im.type = Image.Type.Simple;
        return im;
    }

    static float[] R(float x, float y, float w, float h) => new[] { x, y, w, h };

    static Material Mat(string nom, string shader, Action<Material> regle)
    {
        Directory.CreateDirectory(MAT);
        string p = $"{MAT}/{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, p); }
        m.shader = Shader.Find(shader);
        regle(m);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void Mode(Material m, string prop, string[] noms, int i)
    {
        m.SetFloat(prop, i);
        for (int k = 0; k < noms.Length; k++)
        {
            string kw = prop.ToUpperInvariant() + "_" + noms[k].ToUpperInvariant();
            if (k == i) m.EnableKeyword(kw); else m.DisableKeyword(kw);
        }
    }

    static readonly string[] ModesMod = { "Vacillement", "Pulsation", "Scintillement" };
    static readonly string[] ModesAdd = { "Texture", "Brume", "Flammes", "Etincelles" };

    static Material MatModulation(string nom, Vector4 canal, int mode, float amp, float vit, float ech, float graine, Color chaleur) =>
        Mat(nom, "ItHunts/UI/Modulation", m =>
        {
            m.SetVector("_Canal", canal); Mode(m, "_Mode", ModesMod, mode);
            m.SetFloat("_Amplitude", amp); m.SetFloat("_Vitesse", vit); m.SetFloat("_Echelle", ech); m.SetFloat("_Graine", graine);
            m.SetColor("_Chaleur", chaleur);
        });

    static Font Police(string style) => AssetDatabase.LoadAssetAtPath<Font>(FONTS + "EBGaramond-" + style + ".ttf");

    // ------------------------------------------------------------------ texte : calibration sur le générateur de uGUI
    struct Mesure { public float capHaut, base_, gauche, largeur; }

    static Mesure MesureTexte(Font f, FontStyle st, int taille, string mot, string capitale = "H")
    {
        var go = new GameObject("calib", typeof(RectTransform));
        try
        {
            var t = go.AddComponent<Text>();
            t.font = f; t.fontStyle = st; t.fontSize = taille; t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = false;
            var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(4000, 1000); rt.pivot = new Vector2(0, 1);
            var gen = new TextGenerator();
            var set = t.GetGenerationSettings(rt.sizeDelta);
            set.scaleFactor = 1; set.generateOutOfBounds = true;
            gen.Populate(capitale, set);
            var v = gen.verts;
            float top = v.Max(x => x.position.y), bot = v.Min(x => x.position.y);
            gen.Populate(mot, set);
            var w = gen.verts.Take(gen.verts.Count).Where(x => x.position.x != 0 || x.position.y != 0).ToList();
            // quads non vides (les espaces n'en ont pas de visibles)
            float gx = float.MaxValue, dx = float.MinValue;
            for (int i = 0; i + 3 < gen.verts.Count; i += 4)
            {
                var q = new[] { gen.verts[i], gen.verts[i + 1], gen.verts[i + 2], gen.verts[i + 3] };
                float qw = q.Max(x => x.position.x) - q.Min(x => x.position.x);
                if (qw < 0.01f) continue;
                gx = Mathf.Min(gx, q.Min(x => x.position.x)); dx = Mathf.Max(dx, q.Max(x => x.position.x));
            }
            // repère : pivot haut gauche, y vers le haut (négatif sous le haut du rect)
            return new Mesure { capHaut = -top, base_ = -bot, gauche = gx, largeur = dx - gx };
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    /// <summary>Taille, étirement et position du rect (coin haut gauche) pour qu'un mot commence en x0, finisse en x1, ait
    /// ses capitales en haut et sa ligne de base en base (pixels de la maquette).</summary>
    static (int taille, float etire, float rx, float ry) Cale(Font f, FontStyle st, string mot, float x0, float x1, float haut, float base_,
        bool etirer = true, string capitale = "H")
    {
        Mesure m100 = MesureTexte(f, st, 100, mot, capitale);
        float cap100 = m100.base_ - m100.capHaut;
        int taille = Mathf.Max(6, Mathf.RoundToInt((base_ - haut) / cap100 * 100f));
        Mesure m = MesureTexte(f, st, taille, mot, capitale);
        float etire = etirer && x1 > x0 ? (x1 - x0) / Mathf.Max(m.largeur, 1) : 1f;
        return (taille, etire, x0 - m.gauche * etire, base_ - m.base_);
    }

    static Text Txt(Transform parent, string nom, Font f, FontStyle st, int taille, Color c, TextAnchor al, float[] r, string texte)
    {
        var rt = Noeud(nom, parent);
        MenuEcran.Pose(rt, r[0], r[1], r[2], r[3]);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = f; t.fontStyle = st; t.fontSize = taille; t.color = c; t.alignment = al; t.text = texte;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false; t.supportRichText = false;
        return t;
    }

    static Color C3(float[] t, int i) => new Color(t[i], t[i + 1], t[i + 2], 1);

    // ------------------------------------------------------------------ boutons : 4 états par teinte et luminosité
    static Material _matBouton;

    static Button Bouton(Image im)
    {
        im.material = _matBouton; im.raycastTarget = true;
        var b = im.gameObject.AddComponent<Button>();
        b.targetGraphic = im; b.transition = Selectable.Transition.ColorTint;
        var cb = b.colors;
        cb.normalColor = new Color(0.5f, 0.5f, 0.5f, 1);           // pixels d'origine (lu x2 par ItHunts/UI/Bouton)
        cb.highlightedColor = new Color(0.60f, 0.575f, 0.55f, 1);  // +20 %, légèrement chaud
        cb.selectedColor = cb.highlightedColor;
        cb.pressedColor = new Color(0.40f, 0.38f, 0.36f, 1);       // -20 %
        cb.disabledColor = new Color(0.30f, 0.29f, 0.31f, 0.85f);  // -40 %, un peu transparent
        cb.colorMultiplier = 1; cb.fadeDuration = 0.08f;
        b.colors = cb;
        im.gameObject.AddComponent<SelectionAuSurvol>();
        return b;
    }

    /// <summary>Cartouche long en 5 morceaux (bouts et fleuron gardés, tranches étirées), en pixels de la maquette.</summary>
    static RectTransform CartoucheLong(Transform parent, string nom, float x, float y, float w, float h, bool bouton, out Image principal, Type typeSel = null)
    {
        var rt = Noeud(nom, parent); MenuEcran.Pose(rt, x, y, w, h);
        float k = h / 72f; float bout = 34 * k, fl = 36 * k;
        float mid = (w - 2 * bout - fl) / 2;
        var g = Img(rt, "G", Spr("Commun", "Menus_Cartouche_G"), R(0, 0, bout, h));
        var m1 = Img(rt, "M1", Spr("Commun", "Menus_Cartouche_M"), R(bout - 0.5f, 0, mid + 1, h));
        var c = Img(rt, "C", Spr("Commun", "Menus_Cartouche_C"), R(bout + mid, 0, fl, h));
        var m2 = Img(rt, "M2", Spr("Commun", "Menus_Cartouche_M"), R(bout + mid + fl - 0.5f, 0, mid + 1, h));
        var d = Img(rt, "D", Spr("Commun", "Menus_Cartouche_D"), R(w - bout, 0, bout, h));
        principal = null;
        if (bouton)
        {
            // une image transparente reçoit le clic et porte la teinte ; les 5 morceaux suivent la teinte (CanvasRenderer)
            var hit = rt.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0); hit.raycastTarget = true;
            var b = (Selectable)rt.gameObject.AddComponent(typeSel ?? typeof(Button));
            b.transition = Selectable.Transition.None; b.targetGraphic = hit;
            var teinte = rt.gameObject.AddComponent<TeinteGroupe>();
            teinte.cibles = new Graphic[] { g, m1, c, m2, d };
            foreach (var gr in teinte.cibles) gr.material = _matBouton;
            rt.gameObject.AddComponent<SelectionAuSurvol>();
            principal = hit;
        }
        else
            foreach (var gr in new Graphic[] { g, m1, c, m2, d }) gr.color = new Color(1, 1, 1, 1);
        return rt;
    }

    // ------------------------------------------------------------------ construction
    public static string Construit()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Lit();
        Directory.CreateDirectory(PRE);
        _matBouton = Mat("M_UI_Bouton", "ItHunts/UI/Bouton", m => { });
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // caméra et canvas (Screen Space - Camera, 1920 x 1080, hauteur)
        var camGo = new GameObject("Camera_Menus");
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.orthographic = true;
        cam.cullingMask = 1 << LayerMask.NameToLayer("UI"); cam.depth = 0;
        camGo.AddComponent<AudioListener>();
        var canGo = new GameObject("Canvas_Menus", typeof(RectTransform));
        canGo.layer = LayerMask.NameToLayer("UI");
        var can = canGo.AddComponent<Canvas>();
        can.renderMode = RenderMode.ScreenSpaceCamera; can.worldCamera = cam; can.planeDistance = 10;
        var sc = canGo.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; sc.matchWidthOrHeight = 1; sc.referencePixelsPerUnit = 100;
        canGo.AddComponent<GraphicRaycaster>();
        var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();
        var fige = canGo.AddComponent<MenuFige>(); fige.fige = false;

        var titre = ConstruitTitre(canGo.transform);
        var camp = ConstruitCampement(canGo.transform);
        var choix = ConstruitChoix(canGo.transform);
        var pause = ConstruitPause(canGo.transform, out Selectable premierPause, out Button btParam, out Button btReprendre);
        var param = ConstruitParametres(canGo.transform, out Selectable premierParam, out Button btRetour);

        // aperçu : écrans, audio
        var ap = canGo.AddComponent<MenusApercu>();
        ap.titre = titre; ap.campement = camp; ap.choix = choix; ap.pause = pause; ap.parametres = param;
        ap.premierPause = premierPause; ap.premierParametres = premierParam;
        var mus = canGo.AddComponent<AudioSource>();
        mus.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/Menu/Musique_Menu_Boucle.wav");
        mus.loop = true; mus.playOnAwake = true; mus.volume = 0.7f; mus.spatialBlend = 0;
        var feuGo = new GameObject("Son_Feu"); feuGo.transform.SetParent(canGo.transform, false);
        var feu = feuGo.AddComponent<AudioSource>();
        feu.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Art/Audio/Menu/Sfx_Feu_Boucle.wav");
        feu.loop = true; feu.playOnAwake = false; feu.volume = 1f; feu.spatialBlend = 0;
        ap.musique = mus; ap.feu = feu;
        // liaisons des boutons de la pause (aperçu)
        UnityEventTools.AddBoolPersistentListener(btParam.onClick, ap.Parametres, true);
        UnityEventTools.AddBoolPersistentListener(btReprendre.onClick, ap.Pause, false);
        UnityEventTools.AddBoolPersistentListener(btRetour.onClick, ap.Parametres, false);

        // prefabs (les écrans restent liés dans la scène)
        string res = "";
        foreach (var (go, nom) in new[] { (titre, "Menu_Titre"), (camp, "Menu_Campement"), (choix, "Menu_Choix"), (pause, "Menu_Pause"), (param, "Menu_Parametres") })
        {
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{PRE}/{nom}.prefab", InteractionMode.AutomatedAction);
            res += nom + " ";
        }
        titre.SetActive(true); camp.SetActive(false); choix.SetActive(false); pause.SetActive(false); param.SetActive(false);
        EditorSceneManager.SaveScene(scene, SCENE);
        AssetDatabase.SaveAssets();
        return "prefabs : " + res + "; scène " + SCENE;
    }

    static RectTransform Ecran(Transform canvas, string nom, Vector2 taille)
    {
        var rt = Noeud(nom, canvas);
        var e = rt.gameObject.AddComponent<MenuEcran>(); e.taille = taille; e.couvrir = true;
        rt.sizeDelta = taille;
        return rt;
    }

    // ------------------------------------------------------------------ écran titre
    static GameObject ConstruitTitre(Transform canvas)
    {
        var taille = new Vector2(_valeurs["Titre_taille"][0], _valeurs["Titre_taille"][1]);
        var e = Ecran(canvas, "Menu_Titre", taille);
        Img(e, "Fond", Spr("Titre", "Titre_Fond"), _pieces["Titre_Fond"]);
        // personnages : respiration et cape par déformation sur place (poids peints à la main du script)
        float[] pr = _pieces["Titre_Personnages"];
        float pivot = (pr[1] + pr[3] - _valeurs["Titre_pieds_y"][0]) / pr[3];
        var mPers = Mat("M_Titre_Personnages", "ItHunts/UI/Deformation", m =>
        {
            m.SetFloat("_UsePoids", 1); m.EnableKeyword("_POIDS_TEXTURE"); m.SetTexture("_Poids", Tex("Titre", "Titre_Personnages_Poids"));
            m.SetFloat("_Pivot", pivot); m.SetFloat("_Respiration", 1.6f); m.SetFloat("_Rythme", 0.21f); m.SetFloat("_Cape", 1.3f);
            m.SetFloat("_Opaque", 1);
        });
        Img(e, "Personnages", Spr("Titre", "Titre_Personnages"), pr, mPers);
        // lumières peintes qui bougent (multiplication x2, au repos = maquette)
        var masques = Spr("Titre", "Titre_Effets_Masques");
        float[] full = _pieces["Titre_Effets_Masques"];
        Img(e, "Fenetres_Palpitent", masques, full, MatModulation("M_Titre_Fenetres", new Vector4(1, 0, 0, 0), 0, 0.35f, 0.55f, 0.03f, 1, new Color(1, 0.7f, 0.7f)));
        Img(e, "Flaques_Scintillent", masques, full, MatModulation("M_Titre_Flaques", new Vector4(0, 1, 0, 0), 2, 0.5f, 1f, 0.035f, 2, Color.white));
        Img(e, "Lanternes_Vacillent", masques, full, MatModulation("M_Titre_Lanternes", new Vector4(0, 0, 1, 0), 0, 0.4f, 1.1f, 0.012f, 3, new Color(1, 0.9f, 0.8f)));
        Img(e, "Lune_Halo_Pulse", masques, full, MatModulation("M_Titre_Lune", new Vector4(0, 0, 0, 1), 1, 0.22f, 0.45f, 0.01f, 4, new Color(0.9f, 0.95f, 1)));
        // brume additive qui dérive au sol et entre les arbres (deux nappes, parallaxe)
        float[] bc = _valeurs["Titre_couleur_brume"];
        var brumeSp = Spr("Titre", "Titre_Brume_Masque");
        Img(e, "Brume_Lente", brumeSp, full, Mat("M_Titre_Brume_1", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 1); m.SetColor("_Color", new Color(bc[0], bc[1], bc[2]) * 1.4f); m.SetFloat("_Intensite", 0.55f);
            m.SetFloat("_Echelle", 0.0032f); m.SetVector("_Derive", new Vector4(11, 1.2f, 0, 0)); m.SetFloat("_Seuil", 0.38f);
        }));
        Img(e, "Brume_Proche", brumeSp, full, Mat("M_Titre_Brume_2", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 1); m.SetColor("_Color", new Color(bc[0], bc[1], bc[2]) * 1.4f); m.SetFloat("_Intensite", 0.35f);
            m.SetFloat("_Echelle", 0.0055f); m.SetVector("_Derive", new Vector4(24, 2.5f, 0, 0)); m.SetFloat("_Seuil", 0.45f);
        }));
        // menu : texte Unity aux emplacements de la maquette
        var menu = Noeud("Menu", e); MenuEcran.Pose(menu, 0, 0, taille.x, taille.y);
        // curseur flamme et soulignement : lueurs découpées de la maquette, en lumière ajoutée (la flamme vacille un peu)
        var curseur = Img(menu, "Curseur", Spr("Titre", "Titre_Curseur"), _pieces["Titre_Curseur"], Mat("M_Titre_Curseur", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 0); m.SetFloat("_Intensite", 1); m.SetFloat("_Amplitude", 0.12f); m.SetFloat("_Vitesse", 1.3f); m.SetFloat("_AuRepos", 1);
        }));
        var soul = Img(menu, "Soulignement", Spr("Titre", "Titre_Soulignement"), _pieces["Titre_Soulignement"], Mat("M_Titre_Soulignement", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 0); m.SetFloat("_Intensite", 1); m.SetFloat("_Amplitude", 0.0f); m.SetFloat("_AuRepos", 1);
        }));
        Font f = Police("Regular");
        float[] tj = _textes["Titre/Jouer"], ts = _textes["Titre/Settings"], tq = _textes["Titre/Quitter"];
        var cs = Cale(f, FontStyle.Normal, "Jouer", tj[0], tj[1], tj[2], tj[3], true, "H");
        var cn = Cale(f, FontStyle.Normal, "Settings", ts[0], ts[1], ts[2], ts[3], true, "H");
        var ta = menu.gameObject.AddComponent<TitreApercu>();
        ta.selectionne = new TitreApercu.Style { taille = cs.taille, couleur = C3(tj, 4), x = cs.rx, etirement = cs.etire, baseDansRect = tj[3] - cs.ry };
        ta.normal = new TitreApercu.Style { taille = cn.taille, couleur = C3(ts, 4), x = cn.rx, etirement = cn.etire, baseDansRect = ts[3] - cn.ry };
        ta.curseur = curseur.rectTransform; ta.soulignement = soul.rectTransform;
        ta.decalageCurseur = new Vector2(_pieces["Titre_Curseur"][0] - cs.rx, _pieces["Titre_Curseur"][1] - tj[3]);
        ta.decalageSoulignement = new Vector2(_pieces["Titre_Soulignement"][0] - cs.rx, _pieces["Titre_Soulignement"][1] - tj[3]);
        string[] libelles = { "Jouer", "Paramètres", "Quitter" };
        float[] bases = { tj[3], ts[3], tq[3] };
        var els = new List<TitreApercu.Element>();
        Button prec = null;
        for (int i = 0; i < 3; i++)
        {
            var it = Noeud("Element_" + libelles[i], menu);
            MenuEcran.Pose(it, 60, bases[i] - 60, 380, 80);
            var hit = it.gameObject.AddComponent<Image>(); hit.color = new Color(1, 1, 1, 0);
            var b = it.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None; b.targetGraphic = hit;
            it.gameObject.AddComponent<SelectionAuSurvol>();
            var t = Txt(menu, "Texte_" + libelles[i], f, FontStyle.Normal, cn.taille, C3(ts, 4), TextAnchor.UpperLeft, R(0, 0, 600, 100), libelles[i]);
            els.Add(new TitreApercu.Element { bouton = b, texte = t, ligneDeBase = bases[i] });
            if (prec != null)
            {
                var n1 = prec.navigation; n1.mode = Navigation.Mode.Explicit; n1.selectOnDown = b; prec.navigation = n1;
                var n2 = b.navigation; n2.mode = Navigation.Mode.Explicit; n2.selectOnUp = prec; b.navigation = n2;
            }
            prec = b;
        }
        ta.elements = els.ToArray();
        ta.Applique(0);
        curseur.transform.SetAsLastSibling(); soul.transform.SetAsLastSibling();
        return e.gameObject;
    }

    // ------------------------------------------------------------------ campement
    static readonly string[] PERSOS = { "Renard", "Koala", "Cameleon", "Hibou" };

    static GameObject ConstruitCampement(Transform canvas)
    {
        var taille = new Vector2(_valeurs["Campement_taille"][0], _valeurs["Campement_taille"][1]);
        var e = Ecran(canvas, "Menu_Campement", taille);
        Img(e, "Fond", Spr("Campement", "Campement_Fond"), _pieces["Campement_Fond"]);
        // feu : mes flammes (passe1 - braises) en additif, ondulation et scintillement
        float[] fr = _pieces["Campement_Flammes"];
        float[] foyer = _valeurs["Campement_foyer"];
        float baseUv = (fr[1] + fr[3] - foyer[1]) / fr[3];
        Img(e, "Flammes", Spr("Campement", "Campement_Flammes"), fr, Mat("M_Campement_Flammes", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 2); m.SetFloat("_Base", baseUv); m.SetFloat("_Ondulation", 5f); m.SetFloat("_Montee", 2.1f);
            m.SetFloat("_Amplitude", 0.14f); m.SetFloat("_Vitesse", 1.2f); m.SetFloat("_Intensite", 1); m.SetColor("_Color", Color.white);
        }));
        // lumières peintes : lueur du feu au sol et sur les sièges, fenêtres du château, lanterne de la tente
        var masques = Spr("Campement", "Campement_Effets_Masques");
        float[] full = _pieces["Campement_Effets_Masques"];
        Img(e, "Lueur_Feu_Palpite", masques, full, MatModulation("M_Campement_LueurFeu", new Vector4(0, 0, 1, 0), 0, 0.14f, 0.9f, 0.0015f, 5, new Color(1, 0.8f, 0.55f)));
        Img(e, "Fenetres_Vacillent", masques, full, MatModulation("M_Campement_Fenetres", new Vector4(1, 0, 0, 0), 0, 0.25f, 0.45f, 0.03f, 6, new Color(1, 0.7f, 0.7f)));
        Img(e, "Lanterne_Tente_Vacille", masques, full, MatModulation("M_Campement_LanterneTente", new Vector4(0, 1, 0, 0), 0, 0.25f, 1.0f, 0.01f, 7, new Color(1, 0.85f, 0.6f)));
        // étincelles qui montent du foyer
        Img(e, "Etincelles", Spr("Commun", "Menus_Lueur"), R(foyer[0] - 95, foyer[1] - 330, 190, 300), Mat("M_Campement_Etincelles", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 3); m.SetColor("_Color", new Color(1f, 0.55f, 0.2f)); m.SetFloat("_Intensite", 1.3f);
            m.SetFloat("_Nombre", 9); m.SetFloat("_Taille", 1.7f); m.SetVector("_Taille2", new Vector4(190, 300, 0, 0));
        }));
        // places
        var ca = e.gameObject.AddComponent<CampementApercu>();
        ca.persos = PERSOS.Select(p => new CampementApercu.Perso
        {
            id = p, corps = Spr("Campement", "Campement_Assis_" + p), lanterne = Spr("Campement", "Campement_Assis_" + p + "_Lanterne"),
            couleur = C3(_valeurs["Campement_couleur_" + p], 0)
        }).ToArray();
        ca.placements = _places.Select(p => new CampementApercu.Placement
        {
            perso = p.perso, place = p.place, corps = new Rect(p.v[0], p.v[1], p.v[2], p.v[3]), lanterne = new Rect(p.v[4], p.v[5], p.v[6], p.v[7])
        }).ToArray();
        var mCorps = Mat("M_Campement_Assis", "ItHunts/UI/Deformation", m =>
        {
            m.SetFloat("_UsePoids", 0); m.DisableKeyword("_POIDS_TEXTURE"); m.SetFloat("_Pivot", 0.42f); m.SetFloat("_Respiration", 1.3f);
            m.SetFloat("_Rythme", 0.22f); m.SetFloat("_Cape", 0.9f); m.SetFloat("_CapeHaut", 0.35f);
        });
        var mLueur = Mat("M_Campement_LueurLanterne", "ItHunts/UI/Additif", m =>
        {
            Mode(m, "_Mode", ModesAdd, 0); m.SetFloat("_Intensite", 0.55f); m.SetFloat("_Amplitude", 0.35f); m.SetFloat("_Vitesse", 1.2f); m.SetFloat("_AuRepos", 1);
        });
        ca.places = new CampementApercu.Place[4];
        Font f = Police("Regular");
        float[] tn = _textes["Campement/RenardRouge"];
        var calNom = Cale(f, FontStyle.Normal, "RenardRouge", tn[0], tn[1], tn[2], tn[3], false, "H");
        var lanternesEteintes = new[] { null, null, "Campement_Lanterne_Banc", "Campement_Lanterne_Rocher" };
        var plus = new[] { null, null, "Campement_Plus_3", "Campement_Plus_4" };
        for (int i = 0; i < 4; i++)
        {
            var pl = new CampementApercu.Place();
            var racine = Noeud("Place_" + (i + 1), e); MenuEcran.Pose(racine, 0, 0, taille.x, taille.y);
            racine.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            pl.racine = racine;
            if (lanternesEteintes[i] != null)
                pl.lanterneEteinte = Img(e, "Lanterne_Eteinte_" + (i + 1), Spr("Campement", lanternesEteintes[i]), _pieces[lanternesEteintes[i]]).gameObject;
            var lu = Img(racine, "Lueur", Spr("Commun", "Menus_Lueur"), R(0, 0, 10, 10));
            lu.material = Mat("M_Campement_LueurLanterne_" + (i + 1), "ItHunts/UI/Additif", m =>
            {
                m.CopyPropertiesFromMaterial(mLueur); Mode(m, "_Mode", ModesAdd, 0); m.SetFloat("_Graine", 11 + i * 3.7f);
            });
            pl.lueur = lu;
            pl.corps = Img(racine, "Corps", null, R(0, 0, 10, 10), Mat("M_Campement_Assis_" + (i + 1), "ItHunts/UI/Deformation", m =>
            {
                m.CopyPropertiesFromMaterial(mCorps); m.DisableKeyword("_POIDS_TEXTURE"); m.SetFloat("_Phase", i * 0.29f);
            }));
            pl.lanterne = Img(racine, "Lanterne", null, R(0, 0, 10, 10));
            places_ui.Add((i, pl));
            ca.places[i] = pl;
        }
        // interface (au-dessus des personnages)
        var ui = Noeud("Interface", e); MenuEcran.Pose(ui, 0, 0, taille.x, taille.y);
        for (int i = 0; i < 4; i++)
        {
            var pl = ca.places[i];
            float[] cr = _valeurs["Campement_cartouche_" + (i + 1)];
            var cart = Noeud("Cartouche_" + (i + 1), ui); MenuEcran.Pose(cart, cr[0], cr[1], cr[2], cr[3]);
            var ci = cart.gameObject.AddComponent<Image>(); ci.sprite = Spr("Campement", "Campement_Cartouche_Nom"); ci.raycastTarget = false;
            // texte du nom centré, ligne de base mesurée (même écart au haut du cartouche que sur la maquette)
            float baseRel = tn[3] - 666f;   // place 1 de la maquette : haut du cartouche 666
            var nom = Txt(cart, "Nom", f, FontStyle.Normal, calNom.taille, C3(tn, 4), TextAnchor.UpperCenter,
                R(0, baseRel - (tn[3] - calNom.ry), cr[2], 60), "Joueur");
            pl.cartouche = cart.gameObject; pl.nom = nom;
            if (plus[i] != null)
            {
                var pi = Img(ui, "Plus_" + (i + 1), Spr("Campement", plus[i]), _pieces[plus[i]]);
                Bouton(pi); pl.plus = pi.gameObject;
            }
            float[] co = _pieces["Campement_Coche"];
            float dx = co[0] - 718, dy = co[1] - 663;   // position de la coche dans le cartouche du hibou (place 2)
            var coche = Img(cart, "Coche", Spr("Campement", "Campement_Coche"), R(cr[2] * (dx / 212f), dy, co[2], co[3]));
            pl.coche = coche.gameObject;
        }
        Bouton(Img(ui, "Modifier", Spr("Campement", "Campement_Modifier"), _pieces["Campement_Modifier"]));
        Bouton(Img(ui, "Pret", Spr("Campement", "Campement_Pret"), _pieces["Campement_Pret"]));
        Bouton(Img(ui, "Retour", Spr("Campement", "Campement_Retour"), _pieces["Campement_Retour"]));
        Bouton(Img(ui, "Parametres", Spr("Campement", "Campement_Parametres"), _pieces["Campement_Parametres"]));
        Bouton(Img(ui, "Amis", Spr("Campement", "Campement_Amis"), _pieces["Campement_Amis"]));
        // sélecteur d'acte (haut gauche) et emplacement du chat (bas gauche), avec les cartouches découpés
        var acte = CartoucheLong(ui, "Selecteur_Acte", 70, 10, 230, 72, true, out Image hitActe, typeof(OptionDefilante));
        var ta = Txt(acte, "Acte", Police("SemiBold"), FontStyle.Normal, calNom.taille + 2, C3(tn, 4), TextAnchor.MiddleCenter, R(0, -2, 230, 72), "‹  ACTE I  ›");
        var oc = acte.GetComponent<OptionDefilante>(); oc.valeur = ta; oc.valeurs = new[] { "ACTE I", "ACTE II", "ACTE III" }; oc.index = 0;
        var chat = CartoucheLong(ui, "Chat", 24, 852, 420, 64, false, out _);
        float[] td = _textes["Choix/Desc1a"];
        Txt(chat, "Invite", Police("Italic"), FontStyle.Normal, 16, C3(td, 4), TextAnchor.MiddleLeft, R(40, -1, 340, 64), "Appuie sur Entrée pour écrire…");
        places_ui.Clear();
        CompleteCampement(e.gameObject, ca);
        for (int i = 0; i < 4; i++)
        {
            int dep = i < ca.departOccupants.Length ? ca.departOccupants[i] : -1;
            if (dep >= 0) ca.Occupe(i, dep, ca.nomsApercu[i], false); else ca.Libere(i);
            ca.Pret(i, i < ca.departPrets.Length && ca.departPrets[i]);
        }
        return e.gameObject;
    }

    // ------------------------------------------------------------------ campement : retouches (lot A, 2026-11-21)
    // Valeur et saturation par personnage (mesurées : personnages de la maquette), éclairage de scène des corps, ombre de
    // contact, place 2 (la pierre) vide = bouton + et lanterne éteinte comme les places 3 et 4. Idempotent (trouve par nom).
    // mesuré (mesure de la capture gelée contre la maquette : luminance p10/p50/p90 et saturation médiane du personnage ;
    // renard et hibou visés sur ceux de la maquette, koala et caméléon sur leur moyenne)
    static readonly Dictionary<string, (float lum, float sat, float contraste)> VALEURS_ASSIS = new Dictionary<string, (float, float, float)>
    {
        { "Renard", (1.15f, 0.78f, 1.3f) }, { "Koala", (1.05f, 0.8f, 1.1f) }, { "Cameleon", (1.35f, 0.72f, 1.25f) }, { "Hibou", (1.3f, 0.5f, 1.3f) },
    };

    static void ReglesEclairage(Material m)
    {
        m.SetFloat("_Eclairage", 1);
        m.SetFloat("_OmbreForce", 0.5f); m.SetFloat("_OmbreSeuil", 0.12f); m.SetColor("_OmbreCouleur", new Color(0.34f, 0.33f, 0.62f));
        m.SetColor("_ChaudCouleur", new Color(1.25f, 1.02f, 0.82f)); m.SetColor("_FroidCouleur", new Color(0.62f, 0.66f, 0.86f));
        m.SetFloat("_Transition", 0.7f); m.SetColor("_LisereCouleur", new Color(1f, 0.48f, 0.16f)); m.SetFloat("_LisereForce", 0.6f);
        m.SetFloat("_LisereLargeur", 1.0f); m.SetFloat("_Palpite", 0.35f); m.SetFloat("_PalpiteVitesse", 1.1f); m.SetFloat("_LanterneForce", 0.8f);
        // 2026-11-22 : liseré du feu fin et seulement sur les bords tournés vers le feu ; plus de liseré ailleurs
        m.SetFloat("_LisereForce", 0.6f); m.SetFloat("_LisereFace", 3f); m.SetFloat("_GardeLumiere", 2f);   // nuit 4 : liseré du feu plus marqué
    }

    const string FONDU = "Assets/Art/Source~/UI/scripts/menus/fondu_reglages.json";

    // fondu_reglages.json (écrit par fondu.py --ajuste) : une ligne par personnage et par place, luminosité, saturation et
    // netteté égales à celles du décor peint autour de la place
    public class ReglageFondu { public string perso; public int place; public float lum, sat, douceur, detail = 1, lanterneLum = 1, lanterneSat = 1, contraste; }

    // lecture simple (JsonUtility ne lit pas les types d'un script chargé à la volée) : un objet { } par réglage
    static List<ReglageFondu> LitFondu()
    {
        var res = new List<ReglageFondu>();
        if (!File.Exists(FONDU)) return res;
        string t = File.ReadAllText(FONDU);
        foreach (System.Text.RegularExpressions.Match o in System.Text.RegularExpressions.Regex.Matches(t, @"\{[^{}]*\}"))
        {
            string c = o.Value;
            string Brut(string cle)
            {
                var m = System.Text.RegularExpressions.Regex.Match(c, "\"" + cle + @"""\s*:\s*""?([^"",}\s]+)");
                return m.Success ? m.Groups[1].Value : null;
            }
            float F(string cle, float def) { var b = Brut(cle); return b != null ? float.Parse(b, CI) : def; }
            if (Brut("perso") == null) continue;
            res.Add(new ReglageFondu
            {
                perso = Brut("perso"), place = (int)F("place", 0), lum = F("lum", 0), sat = F("sat", 0), douceur = F("douceur", 1), detail = F("detail", 1),
                lanterneLum = F("lanterneLum", 1), lanterneSat = F("lanterneSat", 1), contraste = F("contraste", 0)
            });
        }
        return res;
    }

    static void CompleteCampement(GameObject e, CampementApercu ca)
    {
        foreach (var p in ca.persos)
            if (VALEURS_ASSIS.TryGetValue(p.id, out var v)) { p.luminosite = v.lum; p.saturation = v.sat; p.contraste = v.contraste; }
        float[] foyer = _valeurs["Campement_foyer"];
        ca.foyer = new Vector2(foyer[0], foyer[1] - 72f);   // centre des flammes, au-dessus de la base du foyer
        foreach (var pc in ca.placements)
        {
            var src = _places.FirstOrDefault(x => x.perso == pc.perso && x.place == pc.place);
            if (src.v == null) continue;
            pc.corps = new Rect(src.v[0], src.v[1], src.v[2], src.v[3]); pc.lanterne = new Rect(src.v[4], src.v[5], src.v[6], src.v[7]);
            if (src.v.Length >= 10) pc.pieds = new Vector2(src.v[8], src.v[9]);
            pc.miroir = src.v.Length >= 11 && src.v[10] > 0.5f;
        }
        // fondu (2026-11-22) : ombres et lumière de lanterne (valeurs écrites ici : le constructeur fait foi)
        ca.ombresPlaces = new[] { new Color(0.89f, 0.58f, 1f), new Color(0.46f, 0.46f, 1f), new Color(0.52f, 0.44f, 1f), new Color(0.31f, 0.39f, 1f) };
        ca.margeOmbre = new Vector3(0.9f, 0.5f, 0.12f);
        ca.valeurOmbre = 0.3f;
        ca.cisaillement = 1.1f;
        ca.eclairageLanterne = new Vector3(1.1f, 0.4f, 0.2f);   // nuit 4 : lueur au sol serrée au pied de la lanterne, couleur adoucie (plus de taches)
        var fondu = LitFondu();
        foreach (var pc in ca.placements)
            foreach (var f in fondu)
                if (f.perso == pc.perso && f.place == pc.place)
                {
                    pc.luminosite = f.lum; pc.saturation = f.sat; pc.douceur = f.douceur; pc.detail = f.detail; pc.reglageLanterne = new Vector2(f.lanterneLum, f.lanterneSat); pc.contraste = f.contraste;
                }
        // nuit 4 : la place 4 (rocher) se dessine derrière la place 3 (banc) : la lanterne posée au bout du banc passe devant
        // la queue d'un caméléon assis sur le rocher, au lieu d'être masquée par elle
        // lumières et ombres au sol (lueur de lanterne sur le décor, ombres portées) : calque commun sous tous les personnages,
        // sinon la lueur d'une place passe par-dessus le personnage de la place voisine
        // nuit 5 : le calque est créé ici, rempli après la boucle des places (avant, les calques déplacés n'étaient plus
        // retrouvés sous leur place et chaque reconstruction en recréait : les anciens restaient orphelins dans le calque
        // commun, toujours visibles, et dessinaient des silhouettes noires derrière les « + » et autour des personnages)
        var solParent = ca.places[0].racine != null ? ca.places[0].racine.parent : null;
        RectTransform sol = null;
        if (solParent != null)
        {
            sol = solParent.Find("Sol_Lumieres_Ombres") as RectTransform;
            if (sol == null) { sol = Noeud("Sol_Lumieres_Ombres", solParent); MenuEcran.Pose(sol, 0, 0, _valeurs["Campement_taille"][0], _valeurs["Campement_taille"][1]); }
        }
        // (de même entre les places 2 et 1) : ordre de dessin 4, 3, 2, 1, la place 1 au-dessus
        if (ca.places.Length >= 4 && ca.places.All(p => p.racine != null))
        {
            int baseIdx = ca.places.Min(p => p.racine.GetSiblingIndex());
            for (int k = 3; k >= 0; k--) ca.places[k].racine.SetSiblingIndex(baseIdx + (3 - k));
            var solCalque = ca.places[0].racine.parent.Find("Sol_Lumieres_Ombres");
            if (solCalque != null) solCalque.SetSiblingIndex(ca.places.Min(p => p.racine.GetSiblingIndex()));
        }
        var mBase = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_Campement_Assis.mat");
        if (mBase != null) { ReglesEclairage(mBase); EditorUtility.SetDirty(mBase); }
        var ui = e.transform.Find("Interface");
        for (int i = 0; i < 4; i++)
        {
            var pl = ca.places[i];
            var racine = pl.racine;
            if (pl.corps != null && pl.corps.material != null) { ReglesEclairage(pl.corps.material); EditorUtility.SetDirty(pl.corps.material); }
            // ombre de contact : sous la lueur et le corps
            var om = racine.Find("Ombre");
            if (om == null)
            {
                var im = Img(racine, "Ombre", Spr("Commun", "Menus_Lueur"), R(0, 0, 10, 10));
                im.color = new Color(0, 0, 0, 0.62f);
                om = im.transform;
            }
            om.SetSiblingIndex(0);
            pl.ombre = om.GetComponent<Image>();
            om.gameObject.SetActive(false);
            // fondu (2026-11-22) : lumière de la lanterne sur le décor, ombres calculées depuis les silhouettes
            int ip = i;
            Image Couche(string nom, string mat, string shader, Action<Material> regle)
            {
                var tr = racine.Find(nom);
                if (tr == null && sol != null) tr = sol.Find(nom + "_" + (ip + 1));
                Image im = tr != null ? tr.GetComponent<Image>() : Img(racine, nom, null, R(0, 0, 10, 10));
                im.material = Mat(mat + "_" + (ip + 1), shader, regle);
                im.raycastTarget = false;
                return im;
            }
            pl.eclairage = Couche("Eclairage", "M_Campement_Eclairage", "ItHunts/UI/Eclaire", m =>
            {
                m.SetFloat("_Force", 0.32f); m.SetVector("_Centre", new Vector4(0.5f, 0.45f, 0.05f, 0)); m.SetFloat("_Durete", 1.8f);
                m.SetFloat("_Vacille", 0.25f); m.SetFloat("_Vitesse", 1.2f); m.SetFloat("_Graine", 11 + ip * 3.7f);
            });
            pl.ombrePortee = Couche("Ombre_Portee", "M_Campement_Ombre", "ItHunts/UI/OmbrePortee", m =>
            {
                m.SetFloat("_ContactForce", 2.0f); m.SetFloat("_ContactFlou", 9f); m.SetFloat("_ContactHaut", 0.6f); m.SetFloat("_Sol", 0.03f);
                m.SetFloat("_PorteeForce", 0.8f); m.SetFloat("_Ecrase", 0.3f); m.SetFloat("_PorteeFlou", 4f); m.SetFloat("_PorteeFlouHaut", 18f);
                m.SetFloat("_PorteeLongueur", 0.7f); m.SetFloat("_Max", 0.8f);
            });
            pl.ombreLanterne = Couche("Ombre_Lanterne", "M_Campement_OmbreLanterne", "ItHunts/UI/OmbrePortee", m =>
            {
                m.SetFloat("_ContactForce", 1.8f); m.SetFloat("_ContactFlou", 3f); m.SetFloat("_ContactHaut", 0.3f); m.SetFloat("_Sol", 0.04f);
                m.SetFloat("_PorteeForce", 0.6f); m.SetFloat("_Ecrase", 0.3f); m.SetFloat("_PorteeFlou", 2f); m.SetFloat("_PorteeFlouHaut", 8f);
                m.SetFloat("_PorteeLongueur", 0.8f); m.SetFloat("_Max", 0.75f);
            });
            if (pl.lanterne != null)
                pl.lanterne.material = Mat("M_Campement_Lanterne_" + (ip + 1), "ItHunts/UI/Deformation", m =>
                {
                    m.SetFloat("_UsePoids", 0); m.DisableKeyword("_POIDS_TEXTURE");
                    m.SetFloat("_Respiration", 0); m.SetFloat("_Cape", 0); m.SetFloat("_Flottement", 0); m.SetFloat("_Orbite", 0);
                    ReglesEclairage(m);
                    m.SetFloat("_LanterneForce", 0); m.SetFloat("_LisereForce", 0.45f); m.SetFloat("_GardeLumiere", 0.3f);
                });
            // l'ancienne lueur additive (brouillard coloré) devient un petit halo autour du verre
            if (pl.lueur != null && pl.lueur.material != null) { pl.lueur.material.SetFloat("_Intensite", 0.4f); EditorUtility.SetDirty(pl.lueur.material); }
            if (i != 1) continue;
            // place 2 (pierre) : lanterne éteinte (celle du banc, pied au pied de la lanterne de la place 2) et bouton +
            if (pl.lanterneEteinte == null)
            {
                float[] lb = _pieces["Campement_Lanterne_Banc"];
                float sc = 0.95f, w = lb[2] * sc, h = lb[3] * sc;
                var le = Img(e.transform, "Lanterne_Eteinte_2", Spr("Campement", "Campement_Lanterne_Banc"), R(918f - w / 2f, 596f - h, w, h));
                le.transform.SetSiblingIndex(racine.GetSiblingIndex());
                pl.lanterneEteinte = le.gameObject;
            }
            if (pl.plus == null && ui != null)
            {
                float[] p3 = _pieces["Campement_Plus_3"];
                float[] c2 = _valeurs["Campement_cartouche_2"], c3 = _valeurs["Campement_cartouche_3"];
                float dx = (c2[0] + c2[2] / 2f) - (c3[0] + c3[2] / 2f);
                var pi = Img(ui, "Plus_2", Spr("Campement", "Campement_Plus_3"), R(p3[0] + dx, p3[1], p3[2], p3[3]));
                Bouton(pi);
                var p3go = ui.Find("Plus_3");
                if (p3go != null) pi.transform.SetSiblingIndex(p3go.GetSiblingIndex());
                pl.plus = pi.gameObject;
            }
        }
        // calque commun : les calques de chaque place (nom unique par place), puis plus aucun orphelin
        if (sol != null)
        {
            var gardes = new HashSet<Transform>();
            for (int i = 0; i < ca.places.Length; i++)
            {
                var pl = ca.places[i];
                foreach (var im in new Image[] { pl.eclairage, pl.ombrePortee, pl.ombreLanterne })
                {
                    if (im == null) continue;
                    string nom = im.name.EndsWith("_" + (i + 1)) ? im.name : im.name + "_" + (i + 1);
                    im.name = nom;
                    if (im.transform.parent != sol) im.transform.SetParent(sol, false);
                    gardes.Add(im.transform);
                }
            }
            for (int c = sol.childCount - 1; c >= 0; c--)
                if (!gardes.Contains(sol.GetChild(c))) UnityEngine.Object.DestroyImmediate(sol.GetChild(c).gameObject);
            // ordre : lumières au sol de toutes les places, puis ombres (une ombre ne s'éclaire pas par la lueur voisine)
            int k = 0;
            foreach (var pl in ca.places) if (pl.eclairage != null) pl.eclairage.transform.SetSiblingIndex(k++);
            foreach (var pl in ca.places) if (pl.ombrePortee != null) pl.ombrePortee.transform.SetSiblingIndex(k++);
            foreach (var pl in ca.places) if (pl.ombreLanterne != null) pl.ombreLanterne.transform.SetSiblingIndex(k++);
            sol.SetSiblingIndex(ca.places.Min(p => p.racine.GetSiblingIndex()));
        }
    }

    /// <summary>Retouche le seul prefab Menu_Campement (aucun autre prefab ni la scène ne sont reconstruits).</summary>
    public static string RetoucheCampement()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Lit();
        string P = PRE + "/Menu_Campement.prefab";
        var root = PrefabUtility.LoadPrefabContents(P);
        try
        {
            var ca = root.GetComponent<CampementApercu>();
            CompleteCampement(root, ca);
            for (int i = 0; i < 4; i++)
            {
                int d = ca.departOccupants[i];
                if (d >= 0) ca.Occupe(i, d, ca.nomsApercu[i], false); else ca.Libere(i);
                ca.Pret(i, ca.departPrets[i]);
            }
            PrefabUtility.SaveAsPrefabAsset(root, P);
            AssetDatabase.SaveAssets();
            return "Menu_Campement retouché";
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static readonly List<(int, CampementApercu.Place)> places_ui = new List<(int, CampementApercu.Place)>();

    // ------------------------------------------------------------------ choix du personnage
    static GameObject ConstruitChoix(Transform canvas)
    {
        var taille = new Vector2(_valeurs["Choix_taille"][0], _valeurs["Choix_taille"][1]);
        var e = Ecran(canvas, "Menu_Choix", taille);
        Img(e, "Fond", Spr("Choix", "Choix_Fond"), _pieces["Choix_Fond"]);
        var cp = e.gameObject.AddComponent<ChoixPersoApercu>();
        cp.roster = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.CharacterRoster>("Assets/_Project/Settings/Personnages/Characters_Default.asset");
        var entrees = new List<ChoixPersoApercu.Entree>();
        string[] artefacts = { "Lanterne", null, "Barillet", null };
        string[] noms = { "Renard", "Koala", "Caméléon", "Hibou" };
        string[] nomsArt = { "Lanterne de feu", "", "Barillet", "" };
        // descriptions d'aperçu, résumées de docs/design/features/artefacts/design.md (en attendant un champ
        // « description » dans ArtifactDefinition, lu en priorité par ChoixPersoApercu)
        string[] desc = { "Un jet de flammes en cône : chaque ennemi touché brûle quelques secondes.", "",
                          "Six coups tirés sous le viseur ; le barillet se recharge une fois vide.", "" };
        string[] ids = { "renard", "koala", "cameleon", "hibou" };
        var grands = Noeud("Grands", e); MenuEcran.Pose(grands, 0, 0, taille.x, taille.y);
        var arts = Noeud("Artefacts", e); MenuEcran.Pose(arts, 0, 0, taille.x, taille.y);
        var vigs = Noeud("Vignettes", e); MenuEcran.Pose(vigs, 0, 0, taille.x, taille.y);
        for (int i = 0; i < 4; i++)
        {
            string p = PERSOS[i];
            float[] r = _pieces["Choix_" + p];
            float pieds = _valeurs["Choix_pieds_" + p][0];
            var g = Noeud("Grand_" + p, grands); MenuEcran.Pose(g, 0, 0, taille.x, taille.y);
            Img(g, "Corps", Spr("Choix", "Choix_" + p), r, Mat("M_Choix_" + p, "ItHunts/UI/Deformation", m =>
            {
                m.SetFloat("_UsePoids", 0); m.DisableKeyword("_POIDS_TEXTURE"); m.SetFloat("_Pivot", (r[1] + r[3] - pieds) / r[3]);
                m.SetFloat("_Respiration", 1.6f); m.SetFloat("_Rythme", 0.2f); m.SetFloat("_Cape", 1.1f); m.SetFloat("_CapeHaut", 0.3f); m.SetFloat("_Phase", 0.1f * i);
            }));
            Img(g, "Lanterne_Vacille", Spr("Choix", "Choix_" + p + "_Lueur"), r, MatModulation("M_Choix_Lueur_" + p, new Vector4(1, 0, 0, 0), 0, 0.35f, 1.1f, 0.01f, 20 + i, new Color(1, 0.9f, 0.85f)));
            GameObject art = null;
            if (artefacts[i] != null)
            {
                string a = artefacts[i];
                var ag = Noeud("Artefact_" + a, arts); MenuEcran.Pose(ag, 0, 0, taille.x, taille.y);
                float[] cu = _valeurs["Choix_centre_" + a];
                Img(ag, "Trainees", Spr("Choix", "Choix_Artefact_" + a + "_Trainees"), _pieces["Choix_Artefact_" + a + "_Trainees"], Mat("M_Choix_" + a + "_Trainees", "ItHunts/UI/Deformation", m =>
                {
                    m.SetFloat("_Respiration", 0); m.SetFloat("_Cape", 0); m.SetFloat("_Flottement", 5); m.SetFloat("_FlotteVitesse", 0.3f);
                    m.SetFloat("_Orbite", 0.05f); m.SetVector("_Centre", new Vector4(cu[0], cu[1], 0.45f, 0));
                }));
                Img(ag, "Corps", Spr("Choix", "Choix_Artefact_" + a), _pieces["Choix_Artefact_" + a], Mat("M_Choix_" + a, "ItHunts/UI/Deformation", m =>
                {
                    m.SetFloat("_Respiration", 0); m.SetFloat("_Cape", 0); m.SetFloat("_Flottement", 5); m.SetFloat("_FlotteVitesse", 0.3f); m.SetFloat("_Orbite", 0);
                }));
                art = ag.gameObject;
            }
            var vd = Img(vigs, "Vignette_" + p + "_Discrete", Spr("Choix", "Choix_Vignette_" + p + "_Discrete"), _pieces["Choix_Vignette_" + p + "_Discrete"], null, true);
            var vi = Img(vigs, "Vignette_" + p + "_Illuminee", Spr("Choix", "Choix_Vignette_" + p + "_Illuminee"), _pieces["Choix_Vignette_" + p + "_Illuminee"], null, true);
            foreach (var v in new[] { vd, vi })
            {
                var b = v.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
                int idx = i;
                UnityEventTools.AddIntPersistentListener(b.onClick, cp.ChoisitIndex, idx);
                v.gameObject.AddComponent<SelectionAuSurvol>();
            }
            entrees.Add(new ChoixPersoApercu.Entree
            {
                id = ids[i], vignetteDiscrete = vd, vignetteIlluminee = vi, grand = g.gameObject, artefact = art,
                nomApercu = noms[i], artefactApercu = nomsArt[i], descriptionApercu = desc[i]
            });
        }
        cp.entrees = entrees.ToArray();
        // textes : nom du personnage et nom de l'artefact au style des titres, description au style italique
        Font fs = Police("SemiBold"), fi = Police("Italic");
        float[] t1 = _textes["Choix/Titre1"], t2 = _textes["Choix/Titre2"], d1 = _textes["Choix/Desc2a"], d2 = _textes["Choix/Desc2b"];
        var c1 = Cale(fs, FontStyle.Normal, "Passive — Flair du traqueur", t1[0], t1[1], t1[2], t1[3], true, "H");
        var c2 = Cale(fs, FontStyle.Normal, "Artefact — Lanterne spectrale", t2[0], t2[1], t2[2], t2[3], true, "H");
        var c3 = Cale(fi, FontStyle.Normal, "Projette une lumière occulte qui affaiblit", d1[0], d1[1], d1[2], d1[3], true, "H");
        cp.nom = Txt(e, "Nom", fs, FontStyle.Normal, c1.taille, C3(t1, 4), TextAnchor.UpperLeft, R(c1.rx, c1.ry, 400, 60), "Renard");
        cp.nom.rectTransform.localScale = new Vector3(c1.etire, 1, 1);
        cp.nomArtefact = Txt(e, "NomArtefact", fs, FontStyle.Normal, c2.taille, C3(t2, 4), TextAnchor.UpperLeft, R(c2.rx, c2.ry, 400, 60), "Lanterne de feu");
        cp.nomArtefact.rectTransform.localScale = new Vector3(c2.etire, 1, 1);
        var de = Txt(e, "Description", fi, FontStyle.Normal, c3.taille, C3(d1, 4), TextAnchor.UpperLeft, R(c3.rx, c3.ry, (1216 - d1[0]) / c3.etire, 80), "");
        de.horizontalOverflow = HorizontalWrapMode.Wrap;
        de.rectTransform.localScale = new Vector3(c3.etire, 1, 1);
        // interligne : écart des lignes de base de la maquette
        var g2 = new TextGenerator();
        var set = de.GetGenerationSettings(new Vector2(4000, 400)); set.scaleFactor = 1;
        g2.Populate("A\nA", set);
        float ecart = g2.lines.Count > 1 ? g2.lines[0].topY - g2.lines[1].topY : c3.taille;
        de.lineSpacing = (d2[3] - d1[3]) / Mathf.Max(ecart, 1);
        cp.description = de;
        Bouton(Img(e, "Valider", Spr("Choix", "Choix_Valider"), _pieces["Choix_Valider"]));
        cp.depart = 0;
        cp.Choisit(0);
        return e.gameObject;
    }

    // ------------------------------------------------------------------ pause et paramètres
    static Image Cadre(Transform parent, string nom, float x, float y, float w, float h)
    {
        var rt = Noeud(nom, parent); MenuEcran.Pose(rt, x, y, w, h);
        var im = rt.gameObject.AddComponent<Image>();
        im.sprite = Spr("Commun", "Menus_Cadre"); im.type = Image.Type.Sliced; im.raycastTarget = true; im.pixelsPerUnitMultiplier = 1;
        return im;
    }

    static Button BoutonTexte(Transform parent, string nom, string libelle, float x, float y, float w, float h, Font f, int taille, Color c)
    {
        var rt = CartoucheLong(parent, nom, x, y, w, h, true, out Image hit);
        Txt(rt, "Libelle", f, FontStyle.Normal, taille, c, TextAnchor.MiddleCenter, R(0, -2, w, h), libelle);
        return rt.GetComponent<Button>();
    }

    static GameObject ConstruitPause(Transform canvas, out Selectable premier, out Button btParam, out Button btReprendre)
    {
        var taille = new Vector2(_valeurs["Titre_taille"][0], _valeurs["Titre_taille"][1]);
        var e = Ecran(canvas, "Menu_Pause", taille);
        var voile = Img(e, "Voile", null, R(0, 0, taille.x, taille.y), null, true); voile.color = new Color(0, 0, 0, 0.55f);
        float w = 560, h = 600, x = (taille.x - w) / 2, y = (taille.y - h) / 2;
        Cadre(e, "Cadre", x, y, w, h);
        float[] tn = _textes["Campement/RenardRouge"], tj = _textes["Titre/Jouer"];
        Font f = Police("Regular");
        var calNom = Cale(f, FontStyle.Normal, "RenardRouge", tn[0], tn[1], tn[2], tn[3], false, "H");
        Txt(e, "Titre", f, FontStyle.Normal, 44, C3(tn, 4), TextAnchor.MiddleCenter, R(x, y + 70, w, 70), "Pause");
        string[] lib = { "Reprendre", "Paramètres", "Retour au campement", "Quitter le jeu" };
        var bs = new List<Button>();
        for (int i = 0; i < lib.Length; i++)
            bs.Add(BoutonTexte(e, "Bouton_" + i, lib[i], x + 80, y + 180 + i * 88, w - 160, 68, f, calNom.taille + 4, C3(tn, 4)));
        Chaine(bs);
        premier = bs[0]; btReprendre = bs[0]; btParam = bs[1];
        return e.gameObject;
    }

    static GameObject ConstruitParametres(Transform canvas, out Selectable premier, out Button btRetour)
    {
        var taille = new Vector2(_valeurs["Titre_taille"][0], _valeurs["Titre_taille"][1]);
        var e = Ecran(canvas, "Menu_Parametres", taille);
        var voile = Img(e, "Voile", null, R(0, 0, taille.x, taille.y), null, true); voile.color = new Color(0, 0, 0, 0.55f);
        float w = 820, h = 760, x = (taille.x - w) / 2, y = (taille.y - h) / 2;
        Cadre(e, "Cadre", x, y, w, h);
        float[] tn = _textes["Campement/RenardRouge"];
        Font f = Police("Regular");
        var calNom = Cale(f, FontStyle.Normal, "RenardRouge", tn[0], tn[1], tn[2], tn[3], false, "H");
        Txt(e, "Titre", f, FontStyle.Normal, 44, C3(tn, 4), TextAnchor.MiddleCenter, R(x, y + 60, w, 70), "Paramètres");
        var rows = new (string lib, string[] vals, int idx)[]
        {
            ("Volume général", new[] { "0 %", "20 %", "40 %", "60 %", "80 %", "100 %" }, 4),
            ("Musique", new[] { "0 %", "20 %", "40 %", "60 %", "80 %", "100 %" }, 3),
            ("Effets", new[] { "0 %", "20 %", "40 %", "60 %", "80 %", "100 %" }, 4),
            ("Sensibilité", new[] { "0,5", "0,75", "1", "1,25", "1,5", "2" }, 2),
            ("Plein écran", new[] { "Oui", "Non" }, 0),
            ("Réduire les animations", new[] { "Non", "Oui" }, 0),
        };
        var sels = new List<Selectable>();
        for (int i = 0; i < rows.Length; i++)
        {
            var rt = CartoucheLong(e, "Option_" + i, x + 70, y + 150 + i * 80, w - 140, 64, true, out Image hit, typeof(OptionDefilante));
            Txt(rt, "Libelle", f, FontStyle.Normal, calNom.taille + 2, C3(tn, 4), TextAnchor.MiddleLeft, R(44, -2, 360, 64), rows[i].lib);
            var val = Txt(rt, "Valeur", f, FontStyle.Normal, calNom.taille + 2, C3(tn, 4), TextAnchor.MiddleRight, R(w - 140 - 44 - 260, -2, 260, 64), "");
            var oc = rt.GetComponent<OptionDefilante>(); oc.valeur = val; oc.valeurs = rows[i].vals; oc.index = rows[i].idx;
            oc.transition = Selectable.Transition.None;
            sels.Add(oc);
        }
        btRetour = BoutonTexte(e, "Retour", "Retour", x + (w - 300) / 2, y + h - 120, 300, 64, f, calNom.taille + 4, C3(tn, 4));
        sels.Add(btRetour);
        Chaine(sels);
        premier = sels[0];
        return e.gameObject;
    }

    static void Chaine<T>(List<T> l) where T : Selectable
    {
        for (int i = 0; i < l.Count; i++)
        {
            var n = l[i].navigation; n.mode = Navigation.Mode.Explicit;
            n.selectOnUp = i > 0 ? l[i - 1] : null; n.selectOnDown = i + 1 < l.Count ? l[i + 1] : null;
            l[i].navigation = n;
        }
    }

    // ------------------------------------------------------------------ captures gelées
    /// <summary>args : [ecran, etat] ; ecran = Titre | Campement | Choix | Pause | Parametres ; etat (campement) : liste
    /// d'occupants « 0,3,-1,-1 », (choix) : index ; sortie : chemin PNG (relatif au projet).</summary>
    public static string Capture(string ecran, string etat, string sortie, string fige)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        if (SceneManager.GetActiveScene().path != SCENE)
            EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var can = UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var ap = can.GetComponent<MenusApercu>();
        var cam = can.worldCamera;
        var ecrans = new Dictionary<string, GameObject> { { "Titre", ap.titre }, { "Campement", ap.campement }, { "Choix", ap.choix }, { "Pause", ap.pause }, { "Parametres", ap.parametres } };
        foreach (var kv in ecrans) kv.Value.SetActive(kv.Key == ecran || (ecran == "Pause" && kv.Key == "Titre") || (ecran == "Parametres" && kv.Key == "Titre"));
        var go = ecrans[ecran];
        Vector2 t = go.GetComponent<MenuEcran>().taille;
        if (ecran == "Pause" || ecran == "Parametres") t = ap.titre.GetComponent<MenuEcran>().taille;
        int W = Mathf.RoundToInt(t.x), H = Mathf.RoundToInt(t.y);
        if (ecran == "Campement" && !string.IsNullOrEmpty(etat))
        {
            var ca = go.GetComponent<CampementApercu>();
            int[] occ = etat.Split(',').Select(int.Parse).ToArray();
            for (int i = 0; i < 4; i++) { if (occ[i] >= 0) ca.Occupe(i, occ[i], ca.nomsApercu[i], false); else ca.Libere(i); }
            ca.Pret(1, occ[1] >= 0);
        }
        if (ecran == "Choix" && !string.IsNullOrEmpty(etat))
            go.GetComponent<ChoixPersoApercu>().Choisit(int.Parse(etat));
        if (ecran == "Titre" && !string.IsNullOrEmpty(etat))
            go.GetComponentInChildren<TitreApercu>().Applique(int.Parse(etat));
        MenuFige.Regle(fige == "1", fige == "1" ? 0 : 3.7f);
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        rt.antiAliasing = 1;
        var old = cam.targetTexture;
        cam.targetTexture = rt;
        // premier rendu : le canvas prend la taille de la cible ; puis les écrans se recalent et on rend pour de bon
        for (int passe = 0; passe < 2; passe++)
        {
            cam.Render();
            foreach (var e in UnityEngine.Object.FindObjectsByType<MenuEcran>(FindObjectsInactive.Include, FindObjectsSortMode.None)) e.Ajuste();
            Canvas.ForceUpdateCanvases();
        }
        cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = old;
        string p = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(p));
        File.WriteAllBytes(p, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex); rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        MenuFige.Regle(false);
        foreach (var kv in ecrans) kv.Value.SetActive(kv.Key == "Titre");
        return sortie + " " + W + "x" + H;
    }
}
