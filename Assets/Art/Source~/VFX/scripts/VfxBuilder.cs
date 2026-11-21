// Effets peints (lot D, 2026-11-21) : import des textures, matériaux, prefabs, mesure du coût, captures.
// `unity cmd ... script run --file VfxBuilder.cs --entry VfxBuilder.<Construit|Mesure|Capture>` (éditeur du projet art).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using ItHunts.Art.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public static class VfxBuilder
{
    const string V = "Assets/Art/VFX";
    const string TEX = V + "/Textures";
    const string MAT = V + "/Materiaux";
    const string PRE = V + "/Prefabs";

    static bool BonProjet() => Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets");

    static Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{TEX}/{n}.png");

    static void Importe()
    {
        foreach (var f in Directory.GetFiles(TEX, "*.png"))
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(f.Replace("\\", "/"));
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = false;                 // R et G sont des données (ton, dissolution)
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.mipmapEnabled = true;
            ti.wrapMode = f.Contains("Lien") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;   // BC7 : R, G et A restent propres
            ti.SaveAndReimport();
        }
    }

    static Material Mat(string nom, string tex, Color encre, Color t1, Color t2, Color t3, float intensite, bool additif = false, float encreForce = 1f)
    {
        Directory.CreateDirectory(MAT);
        string p = $"{MAT}/{nom}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        var sh = Shader.Find("ItHunts/VFX/Peint");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, p); }
        m.shader = sh;
        m.SetTexture("_MainTex", T(tex));
        m.SetColor("_Encre", encre); m.SetColor("_Ton1", t1); m.SetColor("_Ton2", t2); m.SetColor("_Ton3", t3);
        m.SetFloat("_Intensite", intensite); m.SetFloat("_EncreForce", encreForce);
        m.SetFloat("_Additif", additif ? 1 : 0);
        m.SetFloat("_SrcBlend", additif ? 1 : 5); m.SetFloat("_DstBlend", additif ? 1 : 10);
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    static readonly Color ENCRE = new Color(0.03f, 0.02f, 0.03f);
    static Color C(float r, float g, float b) => new Color(r, g, b, 1);

    // ------------------------------------------------------------------ systèmes de particules
    sealed class P
    {
        public string nom; public Material mat; public int tuiles = 1; public bool boucle = true; public float duree = 1f;
        public Vector2 vie = new Vector2(0.5f, 0.8f), vitesse = new Vector2(0.5f, 1f), taille = new Vector2(0.3f, 0.5f);
        public float debit; public int rafale; public float gravite; public bool monde = true;
        public ParticleSystemShapeType forme = ParticleSystemShapeType.Sphere; public float rayon = 0.05f, angle = 10f;
        public AnimationCurve tailleVie; public bool etire; public float etirement = 0.05f;
        public float cyclesFlip = 1f; public bool flipAleatoire = true; public Vector3 rotationForme;
        public bool rotation = true; public float dissout = 1f; public int max = 64;
        public Vector3 orbitalXYZ;
    }

    static ParticleSystem PS(Transform parent, P p)
    {
        var go = new GameObject(p.nom);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var m = ps.main;
        m.duration = p.duree; m.loop = p.boucle; m.playOnAwake = true; m.maxParticles = p.max;
        m.startLifetime = new ParticleSystem.MinMaxCurve(p.vie.x, p.vie.y);
        m.startSpeed = new ParticleSystem.MinMaxCurve(p.vitesse.x, p.vitesse.y);
        m.startSize = new ParticleSystem.MinMaxCurve(p.taille.x, p.taille.y);
        m.startRotation = p.rotation ? new ParticleSystem.MinMaxCurve(-0.35f, 0.35f) : new ParticleSystem.MinMaxCurve(0);
        m.gravityModifier = p.gravite;
        m.simulationSpace = p.monde ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.startColor = Color.white;
        var em = ps.emission;
        em.rateOverTime = p.debit;
        if (p.rafale > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)p.rafale) });
        var sh = ps.shape;
        sh.enabled = true; sh.shapeType = p.forme; sh.radius = p.rayon; sh.angle = p.angle; sh.rotation = p.rotationForme;
        if (p.tailleVie != null) { var s = ps.sizeOverLifetime; s.enabled = true; s.size = new ParticleSystem.MinMaxCurve(1f, p.tailleVie); }
        // dissolution : alpha de la couleur au fil de la vie (le shader dissout par morceaux, pas de fondu)
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                  new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, Mathf.Clamp01(1f - p.dissout * 0.45f)), new GradientAlphaKey(0, 1) });
        col.color = g;
        if (p.tuiles > 1)
        {
            var ts = ps.textureSheetAnimation; ts.enabled = true;
            ts.numTilesX = p.tuiles; ts.numTilesY = p.tuiles;
            ts.animation = ParticleSystemAnimationType.WholeSheet;
            ts.cycleCount = Mathf.Max(1, Mathf.RoundToInt(p.cyclesFlip));
            ts.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 1));
            if (p.flipAleatoire) ts.startFrame = new ParticleSystem.MinMaxCurve(0f, p.tuiles * p.tuiles - 1);
            // pas d'interpolation entre images : animation dessinée image par image
        }
        if (p.orbitalXYZ != Vector3.zero)
        {
            var vo = ps.velocityOverLifetime; vo.enabled = true;
            vo.orbitalX = p.orbitalXYZ.x; vo.orbitalY = p.orbitalXYZ.y; vo.orbitalZ = p.orbitalXYZ.z;
            vo.x = 0; vo.y = 0; vo.z = 0;
        }
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = p.mat;
        r.renderMode = p.etire ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        if (p.etire) { r.velocityScale = p.etirement; r.lengthScale = 1.2f; }
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        r.sortMode = ParticleSystemSortMode.YoungestInFront;
        r.minParticleSize = 0f; r.maxParticleSize = 5f;
        return ps;
    }

    static AnimationCurve K(params float[] tv) { var c = new AnimationCurve(); for (int i = 0; i < tv.Length; i += 2) c.AddKey(tv[i], tv[i + 1]); return c; }

    static GameObject Racine(string nom, float intensite = 1f)
    {
        var go = new GameObject(nom);
        var v = go.AddComponent<VfxPeint>(); v.intensite = intensite;
        return go;
    }

    static void Sauve(GameObject go)
    {
        Directory.CreateDirectory(PRE);
        PrefabUtility.SaveAsPrefabAsset(go, $"{PRE}/{go.name}.prefab");
        Object.DestroyImmediate(go);
    }

    static LineRenderer Ligne(GameObject go, Material m)
    {
        var l = go.AddComponent<LineRenderer>();
        l.sharedMaterial = m; l.alignment = LineAlignment.View; l.numCapVertices = 0; l.numCornerVertices = 2;
        l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; l.receiveShadows = false;
        return l;
    }

    public static string Construit()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Importe();
        var feu = Mat("M_VFX_Feu", "T_VFX_Flamme_4x4", ENCRE, C(0.62f, 0.13f, 0.04f), C(1f, 0.42f, 0.08f), C(1f, 0.86f, 0.5f), 2.2f);
        var feuL = Mat("M_VFX_FeuLanterne", "T_VFX_Flamme_4x4", ENCRE, C(0.55f, 0.03f, 0.05f), C(1f, 0.22f, 0.1f), C(1f, 0.75f, 0.45f), 2.6f);
        var etin = Mat("M_VFX_Etincelle", "T_VFX_Etincelle", ENCRE, C(0.9f, 0.35f, 0.05f), C(1f, 0.7f, 0.2f), C(1f, 0.95f, 0.75f), 3f);
        var tir = Mat("M_VFX_Barillet_Eclat", "T_VFX_Eclat_2x2", ENCRE, C(0.85f, 0.45f, 0.15f), C(1f, 0.78f, 0.5f), C(1f, 0.95f, 0.85f), 3f);
        var trace = Mat("M_VFX_Barillet_Trace", "T_VFX_Trace", ENCRE, C(0.8f, 0.5f, 0.2f), C(1f, 0.82f, 0.55f), C(1f, 0.96f, 0.86f), 2.5f, false, 0.6f);
        var ame = Mat("M_VFX_Ame", "T_VFX_Flamme_4x4", C(0.05f, 0.07f, 0.1f), C(0.32f, 0.42f, 0.55f), C(0.58f, 0.74f, 0.9f), C(0.88f, 0.96f, 1f), 2f);
        var fumG = Mat("M_VFX_Fumee_Goule", "T_VFX_Fumee_4x4", ENCRE, C(0.16f, 0.1f, 0.14f), C(0.32f, 0.22f, 0.27f), C(0.55f, 0.18f, 0.16f), 1.4f);
        var fumV = Mat("M_VFX_Fumee_Volant", "T_VFX_Fumee_4x4", ENCRE, C(0.14f, 0.13f, 0.2f), C(0.3f, 0.3f, 0.42f), C(0.55f, 0.6f, 0.78f), 1.4f);
        var debris = Mat("M_VFX_Debris", "T_VFX_Etincelle", ENCRE, C(0.3f, 0.04f, 0.05f), C(0.55f, 0.08f, 0.08f), C(0.8f, 0.2f, 0.15f), 1.2f);
        var etoile = Mat("M_VFX_Etoile", "T_VFX_Etoile", ENCRE, C(0.85f, 0.5f, 0.05f), C(1f, 0.82f, 0.2f), C(1f, 0.97f, 0.7f), 2f);
        var parE = Mat("M_VFX_Parade_Eclat", "T_VFX_Eclat_2x2", ENCRE, C(0.35f, 0.55f, 0.85f), C(0.7f, 0.85f, 1f), C(1f, 1f, 1f), 3.5f);
        var parA = Mat("M_VFX_Parade_Anneau", "T_VFX_Anneau", ENCRE, C(0.3f, 0.5f, 0.85f), C(0.65f, 0.82f, 1f), C(0.95f, 0.98f, 1f), 3f);
        var drain = Mat("M_VFX_Lien_Drain", "T_VFX_Lien", ENCRE, C(0.32f, 0.01f, 0.03f), C(0.62f, 0.03f, 0.06f), C(0.95f, 0.25f, 0.2f), 2f);
        var soin = Mat("M_VFX_Lien_Soin", "T_VFX_Lien", ENCRE, C(0.03f, 0.3f, 0.3f), C(0.15f, 0.68f, 0.62f), C(0.7f, 1f, 0.9f), 2.2f);
        var pous = Mat("M_VFX_Poussiere", "T_VFX_Poussiere_4x4", ENCRE, C(0.16f, 0.13f, 0.12f), C(0.3f, 0.26f, 0.22f), C(0.46f, 0.41f, 0.35f), 1f);

        // torches et feux : flammes dessinées en boucle + quelques braises
        var go = Racine("VFX_Torche_Feu");
        PS(go.transform, new P { nom = "Flammes", mat = feu, tuiles = 4, debit = 7, vie = new Vector2(0.55f, 0.8f), vitesse = new Vector2(0.15f, 0.35f), taille = new Vector2(0.32f, 0.46f), forme = ParticleSystemShapeType.Sphere, rayon = 0.04f, tailleVie = K(0, 0.6f, 0.25f, 1f, 1, 0.5f), dissout = 1.2f, max = 16, gravite = -0.05f });
        PS(go.transform, new P { nom = "Braises", mat = etin, debit = 3, vie = new Vector2(0.6f, 1.1f), vitesse = new Vector2(0.4f, 0.9f), taille = new Vector2(0.03f, 0.05f), rayon = 0.06f, gravite = -0.1f, etire = true, etirement = 0.08f, max = 8 });
        Sauve(go);

        // jet de feu de la lanterne : cône de flammes qui gonflent (le long de +Z)
        go = Racine("VFX_Lanterne_Jet", 1.1f);
        var jet = PS(go.transform, new P { nom = "Jet", mat = feuL, tuiles = 4, debit = 55, vie = new Vector2(0.28f, 0.38f), vitesse = new Vector2(9f, 12f), taille = new Vector2(0.14f, 0.22f), forme = ParticleSystemShapeType.Cone, angle = 7f, rayon = 0.04f, tailleVie = K(0, 0.6f, 0.4f, 2.6f, 1, 4.2f), dissout = 1.4f, max = 40, cyclesFlip = 1 });
        jet.transform.localRotation = Quaternion.identity;
        PS(go.transform, new P { nom = "Etincelles", mat = etin, debit = 18, vie = new Vector2(0.2f, 0.4f), vitesse = new Vector2(8f, 13f), taille = new Vector2(0.03f, 0.05f), forme = ParticleSystemShapeType.Cone, angle = 12f, rayon = 0.03f, etire = true, etirement = 0.03f, max = 16 });
        Sauve(go);

        // étincelles d'impact (ponctuel)
        go = Racine("VFX_Etincelles");
        PS(go.transform, new P { nom = "Etincelles", mat = etin, boucle = false, duree = 0.6f, rafale = 12, vie = new Vector2(0.25f, 0.55f), vitesse = new Vector2(3f, 7f), taille = new Vector2(0.04f, 0.07f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.05f, gravite = 1f, etire = true, etirement = 0.05f, max = 16 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // tir du barillet : éclat de bouche (2 x 2 images, 0,08 s) + étincelles
        go = Racine("VFX_Barillet_Tir");
        PS(go.transform, new P { nom = "Eclat", mat = tir, tuiles = 2, boucle = false, duree = 0.1f, rafale = 1, vie = new Vector2(0.08f, 0.08f), vitesse = Vector2.zero, taille = new Vector2(0.22f, 0.28f), rayon = 0.001f, flipAleatoire = false, monde = false, max = 2, dissout = 0.3f });
        PS(go.transform, new P { nom = "Etincelles", mat = etin, boucle = false, duree = 0.3f, rafale = 5, vie = new Vector2(0.08f, 0.16f), vitesse = new Vector2(4f, 8f), taille = new Vector2(0.015f, 0.025f), forme = ParticleSystemShapeType.Cone, angle = 18f, rayon = 0.01f, etire = true, etirement = 0.02f, max = 8 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // trace de balle
        go = Racine("VFX_Barillet_Trace");
        Ligne(go, trace); go.AddComponent<TraceVfx>();
        Sauve(go);

        // âme : petite flamme gris-bleu en boucle + volutes qui montent
        go = Racine("VFX_Ame");
        PS(go.transform, new P { nom = "Flamme", mat = ame, tuiles = 4, debit = 6, vie = new Vector2(0.5f, 0.7f), vitesse = new Vector2(0.05f, 0.15f), taille = new Vector2(0.22f, 0.3f), rayon = 0.02f, tailleVie = K(0, 0.7f, 0.3f, 1f, 1, 0.6f), dissout = 1.2f, max = 10, monde = false });
        PS(go.transform, new P { nom = "Volutes", mat = ame, tuiles = 4, debit = 2.5f, vie = new Vector2(0.8f, 1.2f), vitesse = new Vector2(0.2f, 0.4f), taille = new Vector2(0.07f, 0.1f), rayon = 0.08f, gravite = -0.05f, max = 6, dissout = 1.5f });
        Sauve(go);

        // mort de goule : bouffées d'encre violacées + débris rouge sombre
        go = Racine("VFX_Mort_Goule");
        PS(go.transform, new P { nom = "Bouffees", mat = fumG, tuiles = 4, boucle = false, duree = 1f, rafale = 4, vie = new Vector2(0.7f, 0.9f), vitesse = new Vector2(0.4f, 1.1f), taille = new Vector2(1.1f, 1.6f), rayon = 0.4f, flipAleatoire = false, max = 6, dissout = 0.6f });
        PS(go.transform, new P { nom = "Debris", mat = debris, boucle = false, duree = 1f, rafale = 10, vie = new Vector2(0.4f, 0.8f), vitesse = new Vector2(3f, 6f), taille = new Vector2(0.06f, 0.12f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.3f, gravite = 1.5f, etire = true, etirement = 0.04f, max = 12 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // mort de volant : bouffées bleutées plus petites + plumes d'encre
        go = Racine("VFX_Mort_Volant");
        PS(go.transform, new P { nom = "Bouffees", mat = fumV, tuiles = 4, boucle = false, duree = 1f, rafale = 3, vie = new Vector2(0.55f, 0.75f), vitesse = new Vector2(0.3f, 0.8f), taille = new Vector2(0.7f, 1f), rayon = 0.2f, flipAleatoire = false, max = 4, dissout = 0.6f });
        PS(go.transform, new P { nom = "Plumes", mat = fumV, boucle = false, duree = 1f, rafale = 8, vie = new Vector2(0.6f, 1.1f), vitesse = new Vector2(1.5f, 3.5f), taille = new Vector2(0.05f, 0.09f), forme = ParticleSystemShapeType.Sphere, rayon = 0.2f, gravite = 0.6f, etire = true, etirement = 0.06f, max = 10 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // étourdissement : 4 étoiles qui tournent au-dessus de la tête (repère local)
        go = Racine("VFX_Etourdissement");
        PS(go.transform, new P { nom = "Etoiles", mat = etoile, debit = 0, rafale = 4, duree = 1f, boucle = true, vie = new Vector2(1.0f, 1.0f), vitesse = Vector2.zero, taille = new Vector2(0.16f, 0.2f), forme = ParticleSystemShapeType.Circle, rotationForme = new Vector3(-90, 0, 0), rayon = 0.35f, monde = false, orbitalXYZ = new Vector3(0, 2.6f, 0), max = 8, dissout = 0.15f });
        Sauve(go);

        // éclair de parade (contre réussi) : éclat blanc-bleu + anneau qui s'ouvre
        go = Racine("VFX_Parade_Eclair", 1.2f);
        PS(go.transform, new P { nom = "Eclat", mat = parE, tuiles = 2, boucle = false, duree = 0.3f, rafale = 1, vie = new Vector2(0.16f, 0.16f), vitesse = Vector2.zero, taille = new Vector2(0.9f, 0.9f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.4f });
        PS(go.transform, new P { nom = "Anneau", mat = parA, boucle = false, duree = 0.4f, rafale = 1, vie = new Vector2(0.28f, 0.28f), vitesse = Vector2.zero, taille = new Vector2(0.4f, 0.4f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.3f, 1, 4f), max = 2, dissout = 0.8f });
        PS(go.transform, new P { nom = "Etincelles", mat = parE, boucle = false, duree = 0.4f, rafale = 10, vie = new Vector2(0.15f, 0.3f), vitesse = new Vector2(4f, 8f), taille = new Vector2(0.04f, 0.07f), forme = ParticleSystemShapeType.Sphere, rayon = 0.05f, etire = true, etirement = 0.04f, max = 12 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // liens du livre du hibou
        go = Racine("VFX_Lien_Drain");
        Ligne(go, drain); var ld = go.AddComponent<LienVfx>(); ld.sens = -1f; ld.largeur = 0.2f;
        Sauve(go);
        go = Racine("VFX_Lien_Soin");
        Ligne(go, soin); var ls = go.AddComponent<LienVfx>(); ls.sens = 1f; ls.largeur = 0.16f; ls.ondulation = new Vector2(0.05f, 0.9f);
        Sauve(go);

        // poussière d'atterrissage du traqueur (bond) : anneau de bouffées qui s'écartent au sol
        go = Racine("VFX_Poussiere_Atterrissage");
        var po = PS(go.transform, new P { nom = "Bouffees", mat = pous, tuiles = 4, boucle = false, duree = 1.4f, rafale = 12, vie = new Vector2(0.9f, 1.3f), vitesse = new Vector2(3f, 5.5f), taille = new Vector2(1.8f, 3f), forme = ParticleSystemShapeType.Circle, rotationForme = new Vector3(-90, 0, 0), rayon = 2.4f, flipAleatoire = false, max = 16, dissout = 0.6f, gravite = -0.05f });
        var lim = po.limitVelocityOverLifetime; lim.enabled = true; lim.limit = 1.2f; lim.dampen = 0.12f;
        PS(go.transform, new P { nom = "Gravats", mat = debris, boucle = false, duree = 1.2f, rafale = 10, vie = new Vector2(0.5f, 0.9f), vitesse = new Vector2(4f, 7f), taille = new Vector2(0.12f, 0.22f), forme = ParticleSystemShapeType.Hemisphere, rayon = 1.5f, gravite = 2f, etire = true, etirement = 0.03f, max = 12 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        AssetDatabase.SaveAssets();
        return string.Join(", ", Directory.GetFiles(PRE, "*.prefab").Select(Path.GetFileNameWithoutExtension));
    }

    // ------------------------------------------------------------------ coût
    public static string Mesure()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        var sb = new StringBuilder();
        sb.AppendLine("| Effet | Systèmes / lignes | Matériaux | Particules max | Durée simulée (s) | Simulation (µs par image, moyenne) | Textures (Kio) |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var f in Directory.GetFiles(PRE, "*.prefab").OrderBy(x => x))
        {
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(f.Replace("\\", "/"));
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            var ps = go.GetComponentsInChildren<ParticleSystem>();
            var lines = go.GetComponentsInChildren<LineRenderer>();
            var mats = go.GetComponentsInChildren<Renderer>().Select(r => r.sharedMaterial).Where(m => m != null).Distinct().ToArray();
            long texMem = mats.Select(m => m.mainTexture).Where(t => t != null).Distinct().Sum(t => Profiler.GetRuntimeMemorySizeLong(t)) / 1024;
            float dur = ps.Length > 0 ? Mathf.Max(2f, ps.Max(p => p.main.duration + p.main.startLifetime.constantMax)) : 0f;
            int maxP = 0; long ticks = 0; int frames = 0;
            foreach (var p in ps) { p.Clear(true); }
            if (ps.Length > 0)
            {
                var roots = ps.Where(p => p.transform.parent == go.transform).ToArray();
                foreach (var p in roots) p.Play(false);
                var sw = new Stopwatch();
                for (float t = 0; t < dur; t += 1f / 60f)
                {
                    sw.Restart();
                    foreach (var p in roots) p.Simulate(1f / 60f, false, false, false);
                    sw.Stop(); ticks += sw.ElapsedTicks; frames++;
                    maxP = Mathf.Max(maxP, roots.Sum(p => p.particleCount));
                }
            }
            double us = frames > 0 ? ticks * 1e6 / Stopwatch.Frequency / frames : 0;
            sb.AppendLine($"| `{pf.name}` | {ps.Length} / {lines.Length} | {mats.Length} | {(lines.Length > 0 && ps.Length == 0 ? "ligne (≤ 64 points)" : maxP.ToString())} | {dur:0.0} | {us:0.0} | {texMem} |");
            Object.DestroyImmediate(go);
        }
        File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Art/Source~/VFX/scripts/mesures.md"), sb.ToString());
        return sb.ToString();
    }

    // ------------------------------------------------------------------ captures : chaque effet à un instant donné, fond sombre
    public static string Capture(string effet, float temps, float distance, float hauteur, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{PRE}/{effet}.prefab");
        var root = new GameObject("CaptureVfx");
        root.transform.position = new Vector3(0, -500, 0);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, root.transform);
        go.transform.localPosition = Vector3.zero;
        var camGo = new GameObject("Cam"); camGo.transform.SetParent(root.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
        cam.fieldOfView = 40; cam.nearClipPlane = 0.01f; cam.farClipPlane = 200;
        bool jet = effet.Contains("Jet") || effet.Contains("Trace") || effet.Contains("Tir");
        camGo.transform.localPosition = jet ? new Vector3(distance, hauteur, distance * 0.35f) : new Vector3(0, hauteur, -distance);
        camGo.transform.LookAt(root.transform.position + (jet ? new Vector3(0, 0, distance * 0.45f) : new Vector3(0, hauteur * 0.6f, 0)));
        var tr = go.GetComponent<TraceVfx>(); if (tr != null) { tr.depart = root.transform.position; tr.arrivee = root.transform.position + new Vector3(0, 0, 6); tr.Pose(temps); }
        var li = go.GetComponent<LienVfx>(); if (li != null) li.Regle(new Vector3(-1.6f, 0, 0), new Vector3(1.6f, 0, 0));
        foreach (var p in go.GetComponentsInChildren<ParticleSystem>()) { p.useAutoRandomSeed = false; p.randomSeed = 1234; }
        var roots = go.GetComponentsInChildren<ParticleSystem>().Where(p => p.transform.parent == go.transform).ToArray();
        foreach (var p in roots) p.Simulate(temps, true, true, true);
        go.GetComponent<VfxPeint>().Applique();
        var rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGBHalf);
        cam.targetTexture = rt;
        cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(512, 512, TextureFormat.RGBAFloat, false, true);
        tex.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        // linéaire -> sRGB, avec un écrêtage doux des valeurs > 1 (le bloom n'est pas rendu ici)
        var px = tex.GetPixels();
        var o = new Texture2D(512, 512, TextureFormat.RGB24, false);
        o.SetPixels(px.Select(c => new Color(S(c.r), S(c.g), S(c.b))).ToArray()); o.Apply();
        string ps2 = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(ps2));
        File.WriteAllBytes(ps2, o.EncodeToPNG());
        Object.DestroyImmediate(tex); Object.DestroyImmediate(o); rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(root);
        return sortie;
    }

    static float S(float v) { v = v / (1f + 0.25f * Mathf.Max(0, v - 1f)); return Mathf.LinearToGammaSpace(Mathf.Clamp01(v)); }
}
