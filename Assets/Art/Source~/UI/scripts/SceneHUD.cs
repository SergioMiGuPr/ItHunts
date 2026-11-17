using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Scène d'essai du HUD habillé (Assets/Art/Scenes/Test_UI_HUD.unity) et captures 1920x1080 / 960x1080.
public static class SceneHUD
{
    const string Scene = "Assets/Art/Scenes/Test_UI_HUD.unity";
    const string Out = "C:/Users/PC/AppData/Local/Temp/claude/fa13b7ae4f76688a/5c2bc682-360e-47d5-a52c-698998cd4727/scratchpad/ui/";
    static Font _font;

    static Sprite S(string p) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/" + p + ".png");

    static RectTransform Rt(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static Image Img(string name, Transform parent, Sprite sp, Image.Type type = Image.Type.Simple, float ppuMul = 1f)
    {
        var r = Rt(name, parent); var im = r.gameObject.AddComponent<Image>();
        im.sprite = sp; im.type = type; im.pixelsPerUnitMultiplier = ppuMul; im.raycastTarget = false; return im;
    }

    static void Place(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
    }

    static void Stretch(RectTransform r, float l = 0, float b = 0, float rr = 0, float t = 0)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rr, -t);
    }

    static Text Txt(string name, Transform parent, string s, int size, Color c, TextAnchor a)
    {
        var r = Rt(name, parent); var t = r.gameObject.AddComponent<Text>();
        t.font = _font; t.text = s; t.fontSize = size; t.color = c; t.alignment = a; t.fontStyle = FontStyle.Normal; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        var o = r.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.04f, 0.02f, 0.04f, 0.9f); o.effectDistance = new Vector2(1.5f, -1.5f);
        return t;
    }

    public static string Run()
    {
        if (!Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets")) return "MAUVAIS PROJET";
        _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Polices/EBGaramond/EBGaramond-SemiBold.ttf");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Camera_UI"); var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f); cam.orthographic = true;
        var cv = new GameObject("HUD", typeof(RectTransform)).AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceCamera; cv.worldCamera = cam; cv.planeDistance = 1;
        var sc = cv.gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 1f;
        sc.referencePixelsPerUnit = 100;
        var root = cv.transform;

        // ---- Âmes (haut gauche), échelle k = 0,75 de la taille native
        float ka = 0.75f; var ames = Rt("Ames", root);
        Place(ames, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -32), new Vector2(250, 204 / 2f * ka));
        Stretch(Img("Fond", ames, S("HUD/Ames_Fond"), Image.Type.Sliced, 1 / ka).rectTransform);
        Stretch(Img("Cadre", ames, S("HUD/Ames_Cadre"), Image.Type.Sliced, 1 / ka).rectTransform);
        var fl = Img("Flamme", ames, S("HUD/Ames_Flamme")); fl.preserveAspect = true;
        Place(fl.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(116 / 2f * ka, -102 / 2f * ka), new Vector2(34, 40));
        var tn = Txt("Nombre", ames, "1 284", 30, new Color(0.84f, 0.88f, 0.92f), TextAnchor.MiddleLeft);
        Place(tn.rectTransform, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(173 / 2f * ka + 10, -(56 + 46.5f) / 2f * ka), new Vector2(150, 40));

        // ---- Barre de vie (bas gauche), k = 0,9
        float kv = 0.9f; var vie = Rt("BarreVie", root);
        Place(vie, Vector2.zero, Vector2.zero, new Vector2(32, 32), new Vector2(440, 262 / 2f * kv));
        Stretch(Img("Fond", vie, S("HUD/BarreVie_Fond"), Image.Type.Sliced, 1 / kv).rectTransform);
        var canal = Rt("Canal", vie);
        canal.anchorMin = new Vector2(0, 1); canal.anchorMax = new Vector2(1, 1); canal.pivot = new Vector2(0, 1);
        canal.offsetMin = new Vector2(100 * kv, -(54 + 23) * kv); canal.offsetMax = new Vector2(-26 * kv, -54 * kv);
        foreach (var (n, f) in new[] { ("Remplissage_Retard", 0.6f), ("Remplissage", 0.45f) })
        {
            var im = Img(n, canal, S("HUD/BarreVie_" + n), Image.Type.Filled);
            im.fillMethod = Image.FillMethod.Horizontal; im.fillOrigin = (int)Image.OriginHorizontal.Left; im.fillAmount = f;
            Stretch(im.rectTransform);
        }
        Stretch(Img("Cadre", vie, S("HUD/BarreVie_Cadre"), Image.Type.Sliced, 1 / kv).rectTransform);
        var tv = Txt("Texte", canal, "45/100", 17, new Color(0.95f, 0.9f, 0.86f), TextAnchor.MiddleLeft);
        Stretch(tv.rectTransform, 14);

        // ---- Mini-carte (haut droite), k = 0,85
        float km = 0.85f; var mc = Rt("MiniCarte", root);
        Place(mc, Vector2.one, Vector2.one, new Vector2(-32, -32), new Vector2(562, 590) / 2f * km);
        Stretch(Img("Fond", mc, S("MiniCarte/MiniCarte_Fond")).rectTransform);
        var masque = Img("Masque", mc, S("MiniCarte/MiniCarte_Masque")); Stretch(masque.rectTransform);
        masque.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var rnd = new System.Random(3); var blanc = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        for (int i = 0; i < 16; i++)
        {
            var c = Img("Couloir", masque.transform, null); c.color = new Color(0.28f, 0.26f, 0.33f);
            bool h = i % 2 == 0; float len = 40 + rnd.Next(110);
            Place(c.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(rnd.Next(-130, 130), rnd.Next(-130, 130)), h ? new Vector2(len, 9) : new Vector2(9, len));
        }
        var cen = new Vector2(280.3f - 281, -(286.2f - 295)) / 2f * km;  // centre du trou par rapport au centre du cadre
        void Ico(string n, Vector2 p, float s) { var ic = Img(n, masque.transform, S("MiniCarte/MiniCarte_" + n)); Place(ic.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), cen + p, Vector2.one * s); }
        Ico("Fleche", Vector2.zero, 24); Ico("Joueur_Vert", new Vector2(-45, 25), 14); Ico("Joueur_Orange", new Vector2(40, -40), 14); Ico("Joueur_Bleu", new Vector2(55, 35), 14);
        Ico("Coffre_Normal", new Vector2(-60, -35), 24); Ico("Coffre_Rare", new Vector2(20, 62), 24); Ico("Coffre_Ouvert", new Vector2(-20, -70), 24);
        Ico("Porte_Verrouillee", new Vector2(72, -5), 24); Ico("Allie_Mort", new Vector2(-75, 10), 24); Ico("Bougie", new Vector2(-35, 65), 24); Ico("Flamme", new Vector2(42, 15), 24);
        Stretch(Img("Cadre", mc, S("MiniCarte/MiniCarte_Cadre")).rectTransform);

        // ---- Planche des icônes à 48 et 24 px (bas droite)
        var pl = Img("Test_Icones_48_24", root, null); pl.color = new Color(0.06f, 0.05f, 0.08f, 0.92f);
        string[] icons = { "Fleche", "Joueur_Rouge", "Joueur_Vert", "Joueur_Orange", "Joueur_Bleu", "Coffre_Normal", "Coffre_Rare", "Coffre_Ouvert", "Porte_Verrouillee", "Porte_Ouverte", "Allie_Mort", "Bougie", "Flamme" };
        Place(pl.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-32, 32), new Vector2(icons.Length * 56 + 16, 104));
        for (int i = 0; i < icons.Length; i++)
        {
            var a = Img(icons[i] + "_48", pl.transform, S("MiniCarte/MiniCarte_" + icons[i])); Place(a.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(36 + i * 56, -34), Vector2.one * 48);
            var b = Img(icons[i] + "_24", pl.transform, S("MiniCarte/MiniCarte_" + icons[i])); Place(b.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 0.5f), new Vector2(36 + i * 56, -82), Vector2.one * 24);
        }
        EditorSceneManager.SaveScene(scene, Scene);

        // ---- Captures (fond de jeu provisoire : décor du menu, assombri ; retiré avant la sauvegarde)
        var bgTex = new Texture2D(2, 2); bgTex.LoadImage(File.ReadAllBytes(Application.dataPath + "/../docs/art/ui/maquettes/ui_menu_principal_fond_v01.png"));
        var bg = Rt("FondProvisoire", root).gameObject.AddComponent<RawImage>(); bg.texture = bgTex; bg.color = new Color(0.75f, 0.75f, 0.8f);
        bg.transform.SetAsFirstSibling(); Stretch(bg.rectTransform);
        var sb = new StringBuilder();
        foreach (var (w, h) in new[] { (1920, 1080), (960, 1080) })
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            cam.targetTexture = rt;
            typeof(CanvasScaler).GetMethod("Handle", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(sc, null);
            Canvas.ForceUpdateCanvases(); cam.Render();
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); var prev = RenderTexture.active; RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply(); RenderTexture.active = prev;
            File.WriteAllBytes(Out + $"unity_hud_{w}x{h}.png", tex.EncodeToPNG());
            sb.AppendFormat("capture {0}x{1}, scaleFactor {2}, canvas {3}\n", w, h, cv.scaleFactor, ((RectTransform)cv.transform).rect.size);
            cam.targetTexture = null; Object.DestroyImmediate(tex); rt.Release();
        }
        Object.DestroyImmediate(bg.gameObject); Object.DestroyImmediate(bgTex);
        EditorSceneManager.SaveScene(scene, Scene);
        return sb.ToString();
    }
}
