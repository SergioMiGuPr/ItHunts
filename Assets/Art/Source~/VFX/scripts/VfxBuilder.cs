// Effets peints (lot D, 2026-11-21 ; lot 2 des retours, 2026-11-22 : ConstruitLot2) : import des textures, matériaux,
// prefabs, mesure du coût, captures.
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
        public bool local;                 // quad dans le plan XY de l'objet (anneau sur un mur, arc de lame) au lieu de faire face
        public Vector3 vitesseVie;         // vitesse ajoutée au fil de la vie (repère de simulation)
        public bool feuilleLineaire;       // planche lue dans l'ordre, sans image de départ au hasard
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
        if (p.vitesseVie != Vector3.zero)
        {
            var vv = ps.velocityOverLifetime; vv.enabled = true;
            vv.space = p.monde ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            vv.x = p.vitesseVie.x; vv.y = p.vitesseVie.y; vv.z = p.vitesseVie.z;
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
        if (p.local) r.alignment = ParticleSystemRenderSpace.Local;
        if (p.feuilleLineaire && p.tuiles > 1) { var ts = ps.textureSheetAnimation; ts.startFrame = 0f; ts.cycleCount = 1; }
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


    // ------------------------------------------------------------------ lot 2 des retours (2026-11-22)
    // traction (flux bleu de l'orbe au point visé), répulsion (onde courte orange), épée du koala (3 coups + impact vert),
    // sensations (traînées de vitesse, impacts génériques, poussière d'atterrissage du joueur). Les 13 prefabs du lot D ne
    // sont pas reconstruits.
    // ------------------------------------------------------------------ lot 8 de la nuit du 2026-11-22
    static Light Flash(Transform parent, Color c, float intensite, float portee, float duree, float pulsation = 0f)
    {
        var go = new GameObject("Lumiere"); go.transform.SetParent(parent, false);
        var l = go.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = portee; l.intensity = intensite; l.shadows = LightShadows.None;
        var f = go.AddComponent<LumiereFlash>(); f.intensite = intensite; f.duree = duree; f.pulsation = pulsation;
        return l;
    }

    public static string ConstruitLot3()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Importe();
        var debris = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_VFX_Debris.mat");
        var pous = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_VFX_Poussiere.mat");
        var fumG = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_VFX_Fumee_Goule.mat");
        // critique : or et rouge sang
        var crE = Mat("M_VFX_Critique_Eclat", "T_VFX_Eclat_2x2", ENCRE, C(0.55f, 0.12f, 0.05f), C(1f, 0.62f, 0.18f), C(1f, 0.95f, 0.7f), 3.4f);
        var crS = Mat("M_VFX_Critique_Etincelle", "T_VFX_Etincelle", ENCRE, C(0.7f, 0.3f, 0.05f), C(1f, 0.75f, 0.3f), C(1f, 0.97f, 0.8f), 3.2f, false, 0.5f);
        var crA = Mat("M_VFX_Critique_Anneau", "T_VFX_Anneau", ENCRE, C(0.55f, 0.15f, 0.05f), C(1f, 0.6f, 0.2f), C(1f, 0.92f, 0.7f), 2.8f, false, 0.7f);
        // paralysie : cyan électrique
        var elE = Mat("M_VFX_Eclair", "T_VFX_Eclair_2x2", C(0.01f, 0.03f, 0.08f), C(0.1f, 0.35f, 0.7f), C(0.45f, 0.85f, 1f), C(0.92f, 1f, 1f), 3.6f);
        var elA = Mat("M_VFX_Paralysie_Anneau", "T_VFX_Anneau", C(0.01f, 0.03f, 0.08f), C(0.15f, 0.45f, 0.8f), C(0.5f, 0.88f, 1f), C(0.9f, 1f, 1f), 2.6f, false, 0.6f);
        var elS = Mat("M_VFX_Paralysie_Etincelle", "T_VFX_Etincelle", C(0.01f, 0.03f, 0.08f), C(0.2f, 0.55f, 0.9f), C(0.6f, 0.92f, 1f), C(0.95f, 1f, 1f), 3f, false, 0.4f);
        // explosion de zone : cramoisi et os
        var exE = Mat("M_VFX_Explosion_Eclat", "T_VFX_Eclat_2x2", ENCRE, C(0.35f, 0.04f, 0.08f), C(0.85f, 0.25f, 0.2f), C(1f, 0.85f, 0.65f), 3f);
        var exF = Mat("M_VFX_Explosion_Fumee", "T_VFX_Fumee_4x4", ENCRE, C(0.12f, 0.04f, 0.07f), C(0.32f, 0.1f, 0.14f), C(0.7f, 0.3f, 0.25f), 1.4f, false, 0.8f);
        var exA = Mat("M_VFX_Explosion_Anneau", "T_VFX_Anneau", ENCRE, C(0.4f, 0.06f, 0.08f), C(0.95f, 0.35f, 0.2f), C(1f, 0.85f, 0.6f), 2.6f, false, 0.8f);
        var fis = Mat("M_VFX_Fissure", "T_VFX_Fissure", ENCRE, C(0.2f, 0.05f, 0.05f), C(0.5f, 0.12f, 0.08f), C(1f, 0.5f, 0.2f), 1f, false, 1f);
        // parade lisible : blanc bleuté
        var paE = Mat("M_VFX_ParadeL_Etoile", "T_VFX_Etoile", C(0.02f, 0.03f, 0.08f), C(0.45f, 0.6f, 0.95f), C(0.82f, 0.9f, 1f), C(1f, 1f, 1f), 3.8f);
        var paA = Mat("M_VFX_ParadeL_Anneau", "T_VFX_Anneau", C(0.02f, 0.03f, 0.08f), C(0.4f, 0.55f, 0.95f), C(0.8f, 0.9f, 1f), C(1f, 1f, 1f), 3f, false, 0.6f);
        // boss : rouge sombre, sang noir
        var bG = Mat("M_VFX_Boss_Griffe", "T_VFX_Arc_2x2", ENCRE, C(0.3f, 0.02f, 0.05f), C(0.75f, 0.1f, 0.12f), C(1f, 0.5f, 0.4f), 2.6f);
        var bS = Mat("M_VFX_Boss_Sang", "T_VFX_Eclat_2x2", ENCRE, C(0.12f, 0.0f, 0.02f), C(0.35f, 0.02f, 0.05f), C(0.6f, 0.08f, 0.1f), 1.2f);
        var bSG = Mat("M_VFX_Boss_Gouttes", "T_VFX_Etincelle", ENCRE, C(0.15f, 0.0f, 0.03f), C(0.4f, 0.03f, 0.06f), C(0.7f, 0.12f, 0.12f), 1.2f, false, 0.8f);
        var pfC = Mat("M_VFX_PointFaible", "T_VFX_Etoile", ENCRE, C(0.6f, 0.15f, 0.02f), C(1f, 0.55f, 0.1f), C(1f, 0.95f, 0.7f), 4.2f);
        var pfA = Mat("M_VFX_PointFaible_Anneau", "T_VFX_Anneau", ENCRE, C(0.6f, 0.15f, 0.02f), C(1f, 0.5f, 0.12f), C(1f, 0.9f, 0.6f), 3f, false, 0.6f);

        // 1. critique (headshot) au point touché
        var go = Racine("VFX_Critique", 1.2f);
        PS(go.transform, new P { nom = "Eclat", mat = crE, tuiles = 2, boucle = false, duree = 0.35f, rafale = 1, vie = new Vector2(0.2f, 0.2f), vitesse = Vector2.zero, taille = new Vector2(1.1f, 1.1f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.4f });
        PS(go.transform, new P { nom = "Anneau", mat = crA, boucle = false, duree = 0.4f, rafale = 1, vie = new Vector2(0.3f, 0.3f), vitesse = Vector2.zero, taille = new Vector2(0.5f, 0.5f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.3f, 1, 3.2f), max = 2, dissout = 0.8f });
        PS(go.transform, new P { nom = "Etincelles", mat = crS, boucle = false, duree = 0.5f, rafale = 16, vie = new Vector2(0.18f, 0.38f), vitesse = new Vector2(5f, 11f), taille = new Vector2(0.05f, 0.09f), rayon = 0.05f, etire = true, etirement = 0.05f, gravite = 0.6f, max = 18 });
        Flash(go.transform, new Color(1f, 0.7f, 0.3f), 4f, 6f, 0.18f);
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        // 2. balle paralysante : impact (éclairs qui jaillissent) et paralysie (boucle autour de la cible figée)
        go = Racine("VFX_Balle_Paralysante", 1.2f);
        PS(go.transform, new P { nom = "Eclairs", mat = elE, tuiles = 2, boucle = false, duree = 0.4f, rafale = 6, vie = new Vector2(0.1f, 0.18f), vitesse = new Vector2(0f, 0.5f), taille = new Vector2(0.7f, 1.2f), rayon = 0.25f, max = 8, dissout = 0.3f });
        PS(go.transform, new P { nom = "Anneau", mat = elA, boucle = false, duree = 0.4f, rafale = 1, vie = new Vector2(0.25f, 0.25f), vitesse = Vector2.zero, taille = new Vector2(0.4f, 0.4f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.3f, 1, 3.5f), max = 2, dissout = 0.7f });
        PS(go.transform, new P { nom = "Etincelles", mat = elS, boucle = false, duree = 0.4f, rafale = 12, vie = new Vector2(0.12f, 0.3f), vitesse = new Vector2(4f, 9f), taille = new Vector2(0.04f, 0.07f), rayon = 0.05f, etire = true, etirement = 0.05f, max = 14 });
        Flash(go.transform, new Color(0.5f, 0.85f, 1f), 3.5f, 6f, 0.2f);
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        go = Racine("VFX_Paralysie", 1.1f);
        var arcs = PS(go.transform, new P { nom = "Arcs", mat = elE, tuiles = 2, debit = 34, vie = new Vector2(0.1f, 0.2f), vitesse = Vector2.zero, taille = new Vector2(0.8f, 1.4f), forme = ParticleSystemShapeType.Box, max = 14, dissout = 0.3f, monde = false });
        var bx = arcs.shape; bx.scale = new Vector3(0.9f, 1.8f, 0.9f); bx.position = new Vector3(0, 0.9f, 0);
        var gr = PS(go.transform, new P { nom = "Grains", mat = elS, debit = 30, vie = new Vector2(0.2f, 0.4f), vitesse = new Vector2(0.2f, 0.6f), taille = new Vector2(0.03f, 0.05f), forme = ParticleSystemShapeType.Box, max = 20, monde = false });
        var bg = gr.shape; bg.scale = new Vector3(1f, 1.9f, 1f); bg.position = new Vector3(0, 0.95f, 0);
        var lp = Flash(go.transform, new Color(0.5f, 0.85f, 1f), 1.6f, 4f, 0f, 3.5f); lp.transform.localPosition = new Vector3(0, 1f, 0);
        Sauve(go);

        // 3. explosion de zone sur un kill (rayon ≈ 4 m) : éclat, onde au sol, fissures, fumée, débris, flash
        go = Racine("VFX_Explosion_Kill", 1.1f);
        PS(go.transform, new P { nom = "Eclat", mat = exE, tuiles = 2, boucle = false, duree = 0.4f, rafale = 1, vie = new Vector2(0.26f, 0.26f), vitesse = Vector2.zero, taille = new Vector2(3.2f, 3.2f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.5f });
        PS(go.transform, new P { nom = "Onde", mat = exA, boucle = false, duree = 0.6f, rafale = 1, vie = new Vector2(0.45f, 0.45f), vitesse = Vector2.zero, taille = new Vector2(1f, 1f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.5f, 1, 8.5f), max = 2, dissout = 0.8f, local = true });
        PS(go.transform, new P { nom = "Fissures", mat = fis, boucle = false, duree = 1.6f, rafale = 1, vie = new Vector2(1.4f, 1.4f), vitesse = Vector2.zero, taille = new Vector2(6f, 6f), rayon = 0.001f, rotation = false, max = 1, dissout = 0.6f, local = true });
        var fu = PS(go.transform, new P { nom = "Fumee", mat = exF, tuiles = 4, boucle = false, duree = 1.4f, rafale = 10, vie = new Vector2(0.8f, 1.2f), vitesse = new Vector2(2.5f, 4.5f), taille = new Vector2(1.2f, 2.2f), forme = ParticleSystemShapeType.Circle, rotationForme = new Vector3(-90, 0, 0), rayon = 1.2f, flipAleatoire = false, max = 12, dissout = 0.6f, gravite = -0.1f });
        var lf = fu.limitVelocityOverLifetime; lf.enabled = true; lf.limit = 1f; lf.dampen = 0.15f;
        PS(go.transform, new P { nom = "Debris", mat = debris, boucle = false, duree = 1f, rafale = 14, vie = new Vector2(0.5f, 0.9f), vitesse = new Vector2(5f, 9f), taille = new Vector2(0.12f, 0.24f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.5f, gravite = 2.2f, etire = true, etirement = 0.03f, max = 16 });
        Flash(go.transform, new Color(1f, 0.4f, 0.25f), 6f, 12f, 0.35f);
        // ondes et fissures à plat sur le sol
        foreach (var n in new[] { "Onde", "Fissures" }) go.transform.Find(n).localRotation = Quaternion.Euler(90, 0, 0);
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        // 4. éclair de parade lisible : étoile blanche, éclairs rayonnants, double anneau, flash
        go = Racine("VFX_Parade_Eclair_Lisible", 1.3f);
        PS(go.transform, new P { nom = "Etoile", mat = paE, boucle = false, duree = 0.35f, rafale = 1, vie = new Vector2(0.22f, 0.22f), vitesse = Vector2.zero, taille = new Vector2(1.5f, 1.5f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.6f, 0.25f, 1.15f, 1, 0.7f), max = 2, dissout = 0.5f });
        PS(go.transform, new P { nom = "Eclairs", mat = elE, tuiles = 2, boucle = false, duree = 0.3f, rafale = 5, vie = new Vector2(0.1f, 0.16f), vitesse = new Vector2(0f, 0.3f), taille = new Vector2(0.8f, 1.2f), rayon = 0.3f, max = 6, dissout = 0.3f });
        PS(go.transform, new P { nom = "Anneau", mat = paA, boucle = false, duree = 0.5f, rafale = 2, vie = new Vector2(0.28f, 0.4f), vitesse = Vector2.zero, taille = new Vector2(0.5f, 0.6f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.3f, 1, 4.5f), max = 3, dissout = 0.7f });
        PS(go.transform, new P { nom = "Etincelles", mat = elS, boucle = false, duree = 0.4f, rafale = 14, vie = new Vector2(0.15f, 0.3f), vitesse = new Vector2(5f, 10f), taille = new Vector2(0.05f, 0.08f), rayon = 0.05f, etire = true, etirement = 0.05f, max = 16 });
        Flash(go.transform, new Color(0.85f, 0.92f, 1f), 5f, 8f, 0.22f);
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        // 5. poussière blanc-gris dans les rayons de lune (volume 4 × 12 × 4 m), 6. brume au sol du jardin (30 × 30 m)
        go = Racine("VFX_Poussiere_Lune", 1f);
        var pl = PS(go.transform, new P { nom = "Poussiere", mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Kits/M_Kit_Poussiere.mat"), debit = 30, vie = new Vector2(6f, 10f), vitesse = Vector2.zero, taille = new Vector2(0.025f, 0.06f), forme = ParticleSystemShapeType.Box, max = 260, vitesseVie = new Vector3(0.01f, -0.02f, 0.01f) });
        var plm = pl.main; plm.prewarm = true; var pls = pl.shape; pls.scale = new Vector3(4f, 12f, 4f); pls.position = new Vector3(0, 6f, 0);
        var pln = pl.noise; pln.enabled = true; pln.strength = 0.08f; pln.frequency = 0.25f;
        Sauve(go);
        go = Racine("VFX_Brume_Sol", 1f);
        var br = PS(go.transform, new P { nom = "Brume", mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Kits/M_Kit_Brume.mat"), debit = 6, vie = new Vector2(18f, 26f), vitesse = Vector2.zero, taille = new Vector2(6f, 12f), forme = ParticleSystemShapeType.Box, max = 160, vitesseVie = new Vector3(0.25f, 0f, 0f), dissout = 0.2f });
        var brm = br.main; brm.prewarm = true; var brs = br.shape; brs.scale = new Vector3(30f, 1.2f, 30f); brs.position = new Vector3(0, 0.6f, 0);
        br.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        Sauve(go);

        // 7. attaques du boss : griffe (trois lacérations), morsure (mâchoires qui se ferment), impact au sol
        go = Racine("VFX_Boss_Griffe", 1.2f);
        for (int i = 0; i < 3; i++)
        {
            var a = PS(go.transform, new P { nom = "Lacerations_" + i, mat = bG, tuiles = 2, boucle = false, duree = 0.4f, rafale = 1, vie = new Vector2(0.28f, 0.28f), vitesse = Vector2.zero, taille = new Vector2(5.5f, 5.5f), rayon = 0.001f, rotation = false, flipAleatoire = false, feuilleLineaire = true, max = 1, local = true, dissout = 0.5f });
            a.transform.localPosition = new Vector3(0, (i - 1) * 0.7f, 0); a.transform.localRotation = Quaternion.Euler(0, 0, -25 + i * 4);
        }
        PS(go.transform, new P { nom = "Debris", mat = debris, boucle = false, duree = 0.8f, rafale = 10, vie = new Vector2(0.4f, 0.8f), vitesse = new Vector2(4f, 8f), taille = new Vector2(0.15f, 0.3f), forme = ParticleSystemShapeType.Hemisphere, rayon = 1.2f, gravite = 2f, etire = true, etirement = 0.03f, max = 12 });
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        go = Racine("VFX_Boss_Morsure", 1.2f);
        foreach (var (n, z, ry) in new[] { ("Machoire_Haut", 0.8f, 0f), ("Machoire_Bas", -0.8f, 180f) })
        {
            var a = PS(go.transform, new P { nom = n, mat = bG, tuiles = 2, boucle = false, duree = 0.35f, rafale = 1, vie = new Vector2(0.22f, 0.22f), vitesse = Vector2.zero, taille = new Vector2(4f, 4f), rayon = 0.001f, rotation = false, flipAleatoire = false, feuilleLineaire = true, max = 1, local = true, dissout = 0.4f });
            a.transform.localPosition = new Vector3(0, z, 0); a.transform.localRotation = Quaternion.Euler(0, 0, ry);
        }
        PS(go.transform, new P { nom = "Eclat", mat = bS, tuiles = 2, boucle = false, duree = 0.3f, rafale = 1, vie = new Vector2(0.2f, 0.2f), vitesse = Vector2.zero, taille = new Vector2(2.2f, 2.2f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.5f });
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        go = Racine("VFX_Boss_Impact_Sol", 1.1f);
        PS(go.transform, new P { nom = "Fissures", mat = fis, boucle = false, duree = 2f, rafale = 1, vie = new Vector2(1.8f, 1.8f), vitesse = Vector2.zero, taille = new Vector2(9f, 9f), rayon = 0.001f, rotation = false, max = 1, dissout = 0.6f, local = true });
        PS(go.transform, new P { nom = "Onde", mat = exA, boucle = false, duree = 0.7f, rafale = 1, vie = new Vector2(0.55f, 0.55f), vitesse = Vector2.zero, taille = new Vector2(2f, 2f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.4f, 1, 9f), max = 1, dissout = 0.8f, local = true });
        var po = PS(go.transform, new P { nom = "Poussiere", mat = pous, tuiles = 4, boucle = false, duree = 1.6f, rafale = 16, vie = new Vector2(1f, 1.5f), vitesse = new Vector2(4f, 7f), taille = new Vector2(2.4f, 4f), forme = ParticleSystemShapeType.Circle, rotationForme = new Vector3(-90, 0, 0), rayon = 3f, flipAleatoire = false, max = 18, dissout = 0.6f, gravite = -0.05f });
        var lpo = po.limitVelocityOverLifetime; lpo.enabled = true; lpo.limit = 1.2f; lpo.dampen = 0.12f;
        PS(go.transform, new P { nom = "Gravats", mat = debris, boucle = false, duree = 1.2f, rafale = 18, vie = new Vector2(0.6f, 1.1f), vitesse = new Vector2(6f, 11f), taille = new Vector2(0.2f, 0.45f), forme = ParticleSystemShapeType.Hemisphere, rayon = 2f, gravite = 2.2f, etire = true, etirement = 0.03f, max = 20 });
        foreach (var n in new[] { "Onde", "Fissures" }) go.transform.Find(n).localRotation = Quaternion.Euler(90, 0, 0);
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        // 8. blessure du boss : sang noir qui gicle, gouttes qui retombent
        go = Racine("VFX_Boss_Blessure", 1f);
        PS(go.transform, new P { nom = "Gerbe", mat = bS, tuiles = 2, boucle = false, duree = 0.5f, rafale = 3, vie = new Vector2(0.3f, 0.45f), vitesse = new Vector2(1f, 3f), taille = new Vector2(1.2f, 2.2f), forme = ParticleSystemShapeType.Cone, angle = 25, rayon = 0.2f, max = 4, dissout = 0.5f });
        PS(go.transform, new P { nom = "Gouttes", mat = bSG, boucle = false, duree = 1f, rafale = 22, vie = new Vector2(0.5f, 0.9f), vitesse = new Vector2(4f, 9f), taille = new Vector2(0.1f, 0.22f), forme = ParticleSystemShapeType.Cone, angle = 35, rayon = 0.2f, gravite = 2.5f, etire = true, etirement = 0.04f, max = 24 });
        PS(go.transform, new P { nom = "Fumee", mat = fumG, tuiles = 4, boucle = false, duree = 1f, rafale = 4, vie = new Vector2(0.6f, 0.9f), vitesse = new Vector2(0.5f, 1.2f), taille = new Vector2(0.8f, 1.4f), rayon = 0.3f, flipAleatoire = false, max = 6, dissout = 0.6f });
        go.GetComponent<VfxPeint>().detruireALaFin = true; Sauve(go);

        // 9. point faible du boss qui brille (boucle) : cœur ambré qui pulse, anneau qui s'en échappe, braises, lumière
        go = Racine("VFX_Boss_PointFaible", 1.2f);
        PS(go.transform, new P { nom = "Coeur", mat = pfC, debit = 2.5f, vie = new Vector2(0.8f, 0.8f), vitesse = Vector2.zero, taille = new Vector2(1.7f, 1.7f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.7f, 0.3f, 1.15f, 1, 0.75f), max = 4, dissout = 0.3f, monde = false });
        PS(go.transform, new P { nom = "Anneau", mat = pfA, debit = 1.25f, vie = new Vector2(0.7f, 0.7f), vitesse = Vector2.zero, taille = new Vector2(1.0f, 1.0f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.6f, 1, 3f), max = 3, dissout = 0.8f, monde = false });
        PS(go.transform, new P { nom = "Braises", mat = crS, debit = 24, vie = new Vector2(0.5f, 0.9f), vitesse = new Vector2(0.6f, 1.5f), taille = new Vector2(0.06f, 0.1f), rayon = 0.35f, gravite = -0.2f, max = 20 });
        Flash(go.transform, new Color(1f, 0.5f, 0.15f), 2.6f, 7f, 0f, 1.25f);
        Sauve(go);

        AssetDatabase.SaveAssets();
        return "ok VFX_Critique VFX_Balle_Paralysante VFX_Paralysie VFX_Explosion_Kill VFX_Parade_Eclair_Lisible VFX_Poussiere_Lune VFX_Brume_Sol VFX_Boss_Griffe VFX_Boss_Morsure VFX_Boss_Impact_Sol VFX_Boss_Blessure VFX_Boss_PointFaible";
    }

    public static string ConstruitLot2()
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        Importe();
        var etin = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_VFX_Etincelle.mat");
        var pous = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_VFX_Poussiere.mat");
        var debris = AssetDatabase.LoadAssetAtPath<Material>($"{MAT}/M_VFX_Debris.mat");
        var trFum = Mat("M_VFX_Traction_Fumee", "T_VFX_Fumee_4x4", C(0.02f, 0.04f, 0.12f), C(0.08f, 0.2f, 0.55f), C(0.22f, 0.52f, 1f), C(0.72f, 0.9f, 1f), 2.2f, false, 0.45f);
        var trEcl = Mat("M_VFX_Traction_Eclat", "T_VFX_Etincelle", C(0.02f, 0.04f, 0.12f), C(0.2f, 0.45f, 1f), C(0.5f, 0.78f, 1f), C(0.9f, 0.97f, 1f), 3.2f, false, 0.5f);
        var rep = Mat("M_VFX_Repulsion_Onde", "T_VFX_Anneau", ENCRE, C(0.6f, 0.2f, 0.03f), C(1f, 0.5f, 0.1f), C(1f, 0.85f, 0.5f), 2.4f, false, 0.6f);
        var repE = Mat("M_VFX_Repulsion_Eclat", "T_VFX_Etincelle", ENCRE, C(0.7f, 0.25f, 0.03f), C(1f, 0.55f, 0.15f), C(1f, 0.88f, 0.6f), 2.6f, false, 0.6f);
        var arc = Mat("M_VFX_Epee_Arc", "T_VFX_Arc_2x2", ENCRE, C(0.22f, 0.42f, 0.28f), C(0.62f, 0.9f, 0.66f), C(0.95f, 1f, 0.9f), 2.4f);
        var estoc = Mat("M_VFX_Epee_Estoc", "T_VFX_Etincelle", ENCRE, C(0.22f, 0.42f, 0.28f), C(0.62f, 0.9f, 0.66f), C(0.95f, 1f, 0.9f), 2.6f, false, 0.7f);
        var impV = Mat("M_VFX_Epee_Impact", "T_VFX_Eclat_2x2", ENCRE, C(0.08f, 0.42f, 0.14f), C(0.35f, 0.95f, 0.4f), C(0.86f, 1f, 0.8f), 3f);
        var impVA = Mat("M_VFX_Epee_Anneau", "T_VFX_Anneau", ENCRE, C(0.08f, 0.42f, 0.14f), C(0.35f, 0.95f, 0.4f), C(0.86f, 1f, 0.8f), 2.6f, false, 0.7f);
        var vit = Mat("M_VFX_Vitesse", "T_VFX_Etincelle", ENCRE, C(0.35f, 0.42f, 0.55f), C(0.65f, 0.72f, 0.85f), C(0.9f, 0.94f, 1f), 0.9f, true);
        var imp = Mat("M_VFX_Impact", "T_VFX_Eclat_2x2", ENCRE, C(0.55f, 0.45f, 0.35f), C(0.95f, 0.85f, 0.7f), C(1f, 1f, 0.95f), 2.6f);

        // traction : fumée et éclats bleus qui filent de l'orbe au point visé (FluxVfx pose les particules sur le trajet)
        var go = Racine("VFX_Traction", 1.1f);
        var f1 = PS(go.transform, new P { nom = "Fumee", mat = trFum, tuiles = 4, debit = 80, vie = new Vector2(0.5f, 0.5f), vitesse = Vector2.zero, taille = new Vector2(0.22f, 0.38f), rayon = 0.01f, etire = true, etirement = 0.018f, max = 280, dissout = 0.7f });
        var f2 = PS(go.transform, new P { nom = "Eclats", mat = trEcl, debit = 55, vie = new Vector2(0.5f, 0.5f), vitesse = Vector2.zero, taille = new Vector2(0.03f, 0.05f), rayon = 0.01f, etire = true, etirement = 0.014f, max = 220, dissout = 0.4f });
        var fx = go.AddComponent<FluxVfx>();
        fx.systemes = new[] { f1, f2 }; fx.poids = new[] { 1f, 0.7f }; fx.densite = 9f; fx.debitMax = 300f; fx.spirale = new Vector2(0.09f, 2f);
        Sauve(go);

        // répulsion : onde courte orange au point visé (dans le plan de la surface : +Z = normale) + quelques éclats
        go = Racine("VFX_Repulsion");
        PS(go.transform, new P { nom = "Onde", mat = rep, boucle = false, duree = 0.3f, rafale = 1, vie = new Vector2(0.2f, 0.2f), vitesse = Vector2.zero, taille = new Vector2(0.5f, 0.5f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.35f, 1, 3.2f), max = 2, dissout = 0.9f, local = true, monde = false });
        PS(go.transform, new P { nom = "Eclats", mat = repE, boucle = false, duree = 0.3f, rafale = 6, vie = new Vector2(0.12f, 0.2f), vitesse = new Vector2(3f, 5f), taille = new Vector2(0.025f, 0.04f), forme = ParticleSystemShapeType.Circle, rayon = 0.1f, etire = true, etirement = 0.03f, max = 8, dissout = 0.5f });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // épée du koala : arcs de lame (planche 2 x 2 lue dans l'ordre en 0,17 s), dans un plan devant le porteur
        GameObject Taille(string nom, Quaternion rot)
        {
            var g = Racine(nom, 1.1f);
            var a = PS(g.transform, new P { nom = "Arc", mat = arc, tuiles = 2, boucle = false, duree = 0.2f, rafale = 1, vie = new Vector2(0.17f, 0.17f), vitesse = Vector2.zero, taille = new Vector2(1.35f, 1.35f), rayon = 0.001f, rotation = false, flipAleatoire = false, feuilleLineaire = true, max = 2, dissout = 0.35f, local = true, monde = false });
            a.transform.localPosition = new Vector3(0, -0.28f, 0.9f);
            a.transform.localRotation = rot;
            g.GetComponent<VfxPeint>().detruireALaFin = true;
            return g;
        }
        Sauve(Taille("VFX_Epee_Taille_H", Quaternion.Euler(55f, 0f, 0f)));
        Sauve(Taille("VFX_Epee_Taille_D", Quaternion.AngleAxis(-35f, Vector3.forward) * Quaternion.Euler(55f, 0f, 0f)));
        go = Racine("VFX_Epee_Estoc", 1.1f);
        var traits = PS(go.transform, new P { nom = "Traits", mat = estoc, boucle = false, duree = 0.2f, rafale = 12, vie = new Vector2(0.09f, 0.13f), vitesse = new Vector2(16f, 22f), taille = new Vector2(0.025f, 0.04f), forme = ParticleSystemShapeType.Cone, angle = 2f, rayon = 0.12f, etire = true, etirement = 0.09f, max = 14, dissout = 0.5f });
        traits.transform.localPosition = new Vector3(0, -0.25f, 0.5f);
        var pointe = PS(go.transform, new P { nom = "Pointe", mat = impVA, boucle = false, duree = 0.25f, rafale = 1, vie = new Vector2(0.16f, 0.16f), vitesse = Vector2.zero, taille = new Vector2(0.6f, 0.6f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.2f, 1, 2.2f), max = 2, dissout = 0.9f, local = true, monde = false });
        pointe.transform.localPosition = new Vector3(0, -0.2f, 1.6f);
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);
        go = Racine("VFX_Epee_Impact", 1.2f);
        PS(go.transform, new P { nom = "Eclat", mat = impV, tuiles = 2, boucle = false, duree = 0.15f, rafale = 1, vie = new Vector2(0.12f, 0.12f), vitesse = Vector2.zero, taille = new Vector2(0.7f, 0.7f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.4f });
        PS(go.transform, new P { nom = "Anneau", mat = impVA, boucle = false, duree = 0.25f, rafale = 1, vie = new Vector2(0.18f, 0.18f), vitesse = Vector2.zero, taille = new Vector2(0.35f, 0.35f), rayon = 0.001f, rotation = false, tailleVie = K(0, 0.3f, 1, 3f), max = 2, dissout = 0.8f });
        PS(go.transform, new P { nom = "Etincelles", mat = estoc, boucle = false, duree = 0.3f, rafale = 8, vie = new Vector2(0.15f, 0.28f), vitesse = new Vector2(3f, 7f), taille = new Vector2(0.03f, 0.05f), forme = ParticleSystemShapeType.Sphere, rayon = 0.05f, gravite = 0.6f, etire = true, etirement = 0.04f, max = 10, dissout = 0.6f });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // traînées de vitesse : sous la caméra, anneau devant elle, les traits filent vers la caméra (vers les bords)
        go = Racine("VFX_Vitesse_Bords");
        var tv = PS(go.transform, new P { nom = "Traits", mat = vit, debit = 0, vie = new Vector2(0.14f, 0.2f), vitesse = Vector2.zero, taille = new Vector2(0.03f, 0.05f), forme = ParticleSystemShapeType.Circle, rayon = 3.1f, etire = true, etirement = 0.08f, max = 60, dissout = 0.3f, monde = false, rotation = false, vitesseVie = new Vector3(0, 0, -30f) });
        tv.transform.localPosition = new Vector3(0, 0, 4.5f);
        var shv = tv.shape; shv.radiusThickness = 0.25f; shv.arc = 360f;
        var vv2 = go.AddComponent<VitesseVfx>(); vv2.traits = tv; vv2.debitMax = 160f; vv2.etirementMax = 0.08f;
        Sauve(go);

        // impacts génériques : léger (balle, petit objet) et lourd (caisse lancée, chute d'un gros objet)
        go = Racine("VFX_Impact_Leger");
        PS(go.transform, new P { nom = "Eclat", mat = imp, tuiles = 2, boucle = false, duree = 0.12f, rafale = 1, vie = new Vector2(0.09f, 0.09f), vitesse = Vector2.zero, taille = new Vector2(0.3f, 0.36f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.4f });
        PS(go.transform, new P { nom = "Etincelles", mat = etin, boucle = false, duree = 0.4f, rafale = 7, vie = new Vector2(0.15f, 0.3f), vitesse = new Vector2(2.5f, 5f), taille = new Vector2(0.025f, 0.04f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.03f, gravite = 1f, etire = true, etirement = 0.04f, max = 10 });
        PS(go.transform, new P { nom = "Poussiere", mat = pous, tuiles = 4, boucle = false, duree = 0.6f, rafale = 2, vie = new Vector2(0.4f, 0.55f), vitesse = new Vector2(0.3f, 0.6f), taille = new Vector2(0.3f, 0.42f), rayon = 0.05f, flipAleatoire = false, max = 3, dissout = 0.6f });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);
        go = Racine("VFX_Impact_Lourd");
        PS(go.transform, new P { nom = "Eclat", mat = imp, tuiles = 2, boucle = false, duree = 0.15f, rafale = 1, vie = new Vector2(0.12f, 0.12f), vitesse = Vector2.zero, taille = new Vector2(0.75f, 0.85f), rayon = 0.001f, flipAleatoire = false, max = 2, dissout = 0.4f });
        PS(go.transform, new P { nom = "Etincelles", mat = etin, boucle = false, duree = 0.5f, rafale = 12, vie = new Vector2(0.2f, 0.4f), vitesse = new Vector2(3.5f, 7f), taille = new Vector2(0.03f, 0.055f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.08f, gravite = 1.2f, etire = true, etirement = 0.04f, max = 16 });
        var pl = PS(go.transform, new P { nom = "Poussiere", mat = pous, tuiles = 4, boucle = false, duree = 0.9f, rafale = 5, vie = new Vector2(0.55f, 0.8f), vitesse = new Vector2(1.2f, 2.4f), taille = new Vector2(0.6f, 0.95f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.2f, flipAleatoire = false, max = 6, dissout = 0.6f });
        var lpl = pl.limitVelocityOverLifetime; lpl.enabled = true; lpl.limit = 0.5f; lpl.dampen = 0.15f;
        PS(go.transform, new P { nom = "Gravats", mat = debris, boucle = false, duree = 0.8f, rafale = 6, vie = new Vector2(0.35f, 0.6f), vitesse = new Vector2(2.5f, 4.5f), taille = new Vector2(0.05f, 0.09f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.15f, gravite = 2f, etire = true, etirement = 0.03f, max = 8 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        // poussière d'atterrissage du joueur : petit anneau de bouffées au sol (échelle humaine ; VfxPeint.echelle selon la chute)
        go = Racine("VFX_Poussiere_Joueur");
        var pj = PS(go.transform, new P { nom = "Bouffees", mat = pous, tuiles = 4, boucle = false, duree = 0.8f, rafale = 9, vie = new Vector2(0.45f, 0.7f), vitesse = new Vector2(1.2f, 2.2f), taille = new Vector2(0.32f, 0.55f), forme = ParticleSystemShapeType.Circle, rotationForme = new Vector3(-90, 0, 0), rayon = 0.3f, flipAleatoire = false, max = 12, dissout = 0.6f, gravite = -0.03f });
        var lpj = pj.limitVelocityOverLifetime; lpj.enabled = true; lpj.limit = 0.4f; lpj.dampen = 0.15f;
        PS(go.transform, new P { nom = "Graviers", mat = debris, boucle = false, duree = 0.6f, rafale = 5, vie = new Vector2(0.25f, 0.45f), vitesse = new Vector2(1.5f, 3f), taille = new Vector2(0.025f, 0.045f), forme = ParticleSystemShapeType.Hemisphere, rayon = 0.2f, gravite = 2f, etire = true, etirement = 0.03f, max = 6 });
        go.GetComponent<VfxPeint>().detruireALaFin = true;
        Sauve(go);

        AssetDatabase.SaveAssets();
        return "lot 2 : VFX_Traction, VFX_Repulsion, VFX_Epee_Taille_H, VFX_Epee_Taille_D, VFX_Epee_Estoc, VFX_Epee_Impact, VFX_Vitesse_Bords, VFX_Impact_Leger, VFX_Impact_Lourd, VFX_Poussiere_Joueur";
    }

    // ------------------------------------------------------------------ captures vue à la première personne (lot 2)
    // L'effet est posé à l'origine (main ou caméra), la caméra regarde +Z depuis (0, yeux, -recul). Traction : source en bas
    // à droite de la vue (l'orbe), cible à `distance` m devant. Vitesse : intensité donnée par `distance` (m/s).
    public static string CaptureFps(string effet, float temps, float distance, string sortie)
    {
        if (!BonProjet()) return "MAUVAIS PROJET";
        var pf = AssetDatabase.LoadAssetAtPath<GameObject>($"{PRE}/{effet}.prefab");
        var root = new GameObject("CaptureVfx");
        root.transform.position = new Vector3(0, -500, 0);
        var camGo = new GameObject("Cam"); camGo.transform.SetParent(root.transform, false);
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
        cam.fieldOfView = 70; cam.nearClipPlane = 0.01f; cam.farClipPlane = 200;
        bool epee = effet.Contains("Epee_Taille") || effet.Contains("Estoc");
        camGo.transform.localPosition = Vector3.zero;
        camGo.transform.localRotation = Quaternion.identity;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, effet.Contains("Vitesse") ? camGo.transform : root.transform);
        go.transform.localPosition = Vector3.zero; go.transform.localRotation = Quaternion.identity;
        if (effet.Contains("Repulsion") || effet.Contains("Impact")) { go.transform.localPosition = new Vector3(0, 0, 3f); go.transform.localRotation = Quaternion.Euler(0, 180, 0); }
        if (effet.Contains("Poussiere_Joueur")) { go.transform.localPosition = new Vector3(0, -1.1f, 2.4f); }
        foreach (var p in go.GetComponentsInChildren<ParticleSystem>()) { p.useAutoRandomSeed = false; p.randomSeed = 1234; }
        var fx = go.GetComponent<FluxVfx>();
        if (fx != null) fx.Regle(root.transform.TransformPoint(new Vector3(0.28f, -0.22f, 0.55f)), root.transform.TransformPoint(new Vector3(-0.4f, 0.6f, distance)));
        var vx = go.GetComponent<VitesseVfx>();
        var roots = go.GetComponentsInChildren<ParticleSystem>().Where(p => p.transform.parent == go.transform).ToArray();
        if (fx != null || vx != null)
        {
            // effets continus : quelques pas pour régler le débit (selon le trajet ou la vitesse), puis la simulation
            if (vx != null) { vx.vitesse = distance; vx.lissage = 0f; }
            foreach (var p in roots) { p.Clear(true); p.Play(true); }
            for (float t = 0; t < temps; t += 1f / 60f)
            {
                if (fx != null) fx.Pose();
                if (vx != null) vx.Met(1f / 60f);
                foreach (var p in roots) p.Simulate(1f / 60f, true, false, false);
                if (fx != null) fx.Pose();
            }
        }
        else
            foreach (var p in roots) p.Simulate(temps, true, true, true);
        go.GetComponent<VfxPeint>().Applique();
        var rt = new RenderTexture(640, 400, 24, RenderTextureFormat.ARGBHalf);
        cam.targetTexture = rt;
        cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(640, 400, TextureFormat.RGBAFloat, false, true);
        tex.ReadPixels(new Rect(0, 0, 640, 400), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        var px = tex.GetPixels();
        var o = new Texture2D(640, 400, TextureFormat.RGB24, false);
        o.SetPixels(px.Select(c => new Color(S(c.r), S(c.g), S(c.b))).ToArray()); o.Apply();
        string ps2 = Path.Combine(Directory.GetCurrentDirectory(), sortie);
        Directory.CreateDirectory(Path.GetDirectoryName(ps2));
        File.WriteAllBytes(ps2, o.EncodeToPNG());
        Object.DestroyImmediate(tex); Object.DestroyImmediate(o); rt.Release(); Object.DestroyImmediate(rt); Object.DestroyImmediate(root);
        return sortie;
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
