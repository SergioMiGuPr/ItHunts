using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Campement (aperçu DA) : 4 places. Places 1 (bûche) et 2 (pierre) : personnage assis et sa lanterne allumée, aux
    /// rects mesurés sur la maquette ; places 3 (banc) et 4 (rocher) : lanterne éteinte et bouton + tant qu'elles sont
    /// vides. Le nom du joueur est du texte Unity dans le cartouche vidé de la place. Petite apparition quand un joueur
    /// rejoint. Touches d'aperçu (éditeur) : 1 à 4 font tourner l'occupant de la place, R bascule « prêt ».
    /// La logique de salon (main) appelle Occupe / Libere / Pret.
    /// Éclairage des personnages (shader ItHunts/UI/Deformation, _Eclairage = 1) : valeur et saturation par personnage,
    /// feu (foyer) et lanterne posés en uv du sprite à chaque Occupe ; ombre de contact sous le siège et les pieds.
    /// </summary>
    public sealed class CampementApercu : MonoBehaviour
    {
        [Serializable]
        public sealed class Perso
        {
            public string id;
            public Sprite corps;
            public Sprite lanterne;
            public Color couleur = Color.white;
            [Tooltip("Gain de valeur (ramène le personnage aux valeurs du renard et du hibou de la maquette).")]
            public float luminosite = 1f;
            [Tooltip("Saturation (1 = celle du sprite).")]
            public float saturation = 1f;
            [Tooltip("Contraste (exposant sur la luminance) : noirs profonds comme sur la maquette.")]
            public float contraste = 1f;
        }

        [Serializable]
        public sealed class Placement
        {
            public string perso;
            public int place;
            public Rect corps;      // pixels de la maquette (x, y depuis le haut gauche, w, h)
            public Rect lanterne;
            [Tooltip("Pieds : x gauche, x droit (pixels de la maquette), pour l'ombre de contact.")]
            public Vector2 pieds;
            [Header("Fondu dans le décor (mesuré par place : fondu.py)")]
            [Tooltip("Luminosité du corps à cette place (0 = celle du personnage).")]
            public float luminosite;
            [Tooltip("Saturation du corps à cette place (0 = celle du personnage).")]
            public float saturation;
            [Tooltip("Douceur (rayon du flou en pixels du sprite) : égale la netteté du décor peint autour.")]
            public float douceur = 1f;
            [Tooltip("Contraste des détails fins (1 = celui du sprite) : égale le contraste des détails du décor peint.")]
            public float detail = 1f;
            [Tooltip("Luminosité et saturation de la lanterne (hors verre allumé).")]
            public Vector2 reglageLanterne = new Vector2(1f, 1f);
            [Tooltip("Contraste du corps à cette place (exposant de la luminance ; 0 = celui du personnage).")]
            public float contraste;
            [Tooltip("Personnage retourné (places où le feu est à sa gauche : il regarde le feu, sa lumière chaude tombe du bon côté).")]
            public bool miroir;
        }

        [Serializable]
        public sealed class Place
        {
            public RectTransform racine;
            public Image corps;
            public Image lanterne;
            public Image lueur;                 // lueur additive au sol, à la couleur du personnage
            public GameObject cartouche;
            public Text nom;
            public GameObject plus;             // places 3 et 4 : bouton +
            public GameObject lanterneEteinte;  // places 3 et 4
            public GameObject coche;
            public Image ombre;                 // ancienne ombre de contact (tache), cachée quand ombrePortee existe
            public Image ombrePortee;           // ombre du corps (shader ItHunts/UI/OmbrePortee) : contact et portée au sol
            public Image ombreLanterne;         // ombre de la lanterne
            public Image eclairage;             // lumière de la lanterne sur le sol, la bûche et les jambes (ItHunts/UI/Eclaire)
            [NonSerialized] public Material matLanterneJeu;
            [NonSerialized] public Material matOmbreJeu;
            [NonSerialized] public Material matOmbreLanterneJeu;
            [NonSerialized] public int occupant = -1;
            [NonSerialized] public Material matJeu;   // copie du matériau en jeu (les assets restent intacts)
        }

        public Perso[] persos = Array.Empty<Perso>();
        public Placement[] placements = Array.Empty<Placement>();
        public Place[] places = new Place[4];
        [Tooltip("Noms d'aperçu des joueurs.")]
        public string[] nomsApercu = { "RenardRouge", "HibouLune", "Koalabis", "Caméléonne" };
        [Tooltip("Occupants au démarrage (index dans persos, -1 = vide) : l'état de la maquette.")]
        public int[] departOccupants = { 0, 3, -1, -1 };
        public bool[] departPrets = { false, true, false, false };
        [Tooltip("Rayon de la lueur au sol, en hauteurs de lanterne.")]
        public float rayonLueur = 0.95f;
        [Tooltip("Durée de l'apparition (s).")]
        public float dureeApparition = 0.6f;
        [Tooltip("Centre des flammes du feu de camp (pixels de la maquette) : côté chaud et liseré des personnages.")]
        public Vector2 foyer = new Vector2(990, 700);
        [Tooltip("Rayon de la lueur de lanterne sur le personnage, en hauteurs de lanterne.")]
        public float rayonLanterneCorps = 0.9f;
        [Tooltip("Mélange de la couleur du personnage dans la lueur de sa lanterne (0 = lumière chaude neutre).")]
        public float teinteLanterne = 0.55f;
        [Tooltip("Ombre de contact : opacité, hauteur (fraction de la largeur), débord de chaque côté (px).")]
        public Vector3 ombre = new Vector3(0.62f, 0.2f, 18f);
        [Header("Fondu (2026-11-22)")]
        [Tooltip("Teinte des ombres du décor peint autour de chaque place (linéaire, mesurée sur Campement_Fond).")]
        public Color[] ombresPlaces = { new Color(0.89f, 0.58f, 1f), new Color(0.46f, 0.46f, 1f), new Color(0.52f, 0.44f, 1f), new Color(0.31f, 0.39f, 1f) };
        [Tooltip("Ombre portée : marges de l'image autour du sprite (gauche, droite, bas, en fraction du sprite).")]
        public Vector3 margeOmbre = new Vector3(0.7f, 0.7f, 0.3f);
        [Tooltip("Ombre portée : luminance de la couleur multipliée (0 = noir, 1 = aucune ombre).")]
        public float valeurOmbre = 0.34f;
        [Tooltip("Ombre portée : cisaillement à l'opposé du feu (uv x par uv de hauteur).")]
        public float cisaillement = 0.55f;
        [Tooltip("Lumière de la lanterne au sol : rayon (hauteurs de lanterne), aplatissement, mélange de la couleur du personnage.")]
        public Vector3 eclairageLanterne = new Vector3(2.1f, 0.5f, 0.7f);

        private void Start()
        {
            for (int i = 0; i < places.Length; i++)
            {
                if (places[i] == null)
                    continue;
                if (i < departOccupants.Length && departOccupants[i] >= 0)
                    Occupe(i, departOccupants[i], i < nomsApercu.Length ? nomsApercu[i] : "Joueur", false);
                else
                    Libere(i);
                Pret(i, i < departPrets.Length && departPrets[i]);
            }
        }

        private void Update()
        {
            var k = Keyboard.current;
            if (k == null || !Application.isEditor)
                return;
            Key[] touches = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };
            for (int i = 0; i < touches.Length && i < places.Length; i++)
            {
                if (!k[touches[i]].wasPressedThisFrame)
                    continue;
                int n = places[i].occupant + 1;
                if (n >= persos.Length)
                    Libere(i);
                else
                    Occupe(i, n, i < nomsApercu.Length ? nomsApercu[i] : "Joueur", true);
            }
            if (k.rKey.wasPressedThisFrame && places.Length > 0 && places[0].coche != null)
                Pret(0, !places[0].coche.activeSelf);
        }

        private Placement Trouve(string id, int place)
        {
            foreach (Placement p in placements)
                if (p.perso == id && p.place == place)
                    return p;
            return null;
        }

        /// <summary>Un joueur prend la place (index 0 à 3) avec le personnage d'index perso.</summary>
        public void Occupe(int place, int perso, string nom, bool apparition = true)
        {
            Place pl = places[place];
            if (pl == null || perso < 0 || perso >= persos.Length)
                return;
            Perso p = persos[perso];
            Placement pc = Trouve(p.id, place + 1);
            if (pc == null)
            {
                // nuit 5 : sans placement mesuré, la place reste vide (avant : l'ancien corps et ses ombres restaient affichés)
                Debug.LogWarning("CampementApercu : pas de placement pour " + p.id + " à la place " + (place + 1), this);
                Libere(place);
                return;
            }
            pl.occupant = perso;
            if (pc != null && pl.corps != null)
            {
                pl.corps.sprite = p.corps;
                MenuEcran.Pose(pl.corps.rectTransform, pc.corps.x, pc.corps.y, pc.corps.width, pc.corps.height);
                if (pc.miroir) Retourne(pl.corps.rectTransform);
                PivotBas(pl.corps.rectTransform);
                pl.corps.gameObject.SetActive(true);
                Eclaire(pl, p, pc);
            }
            if (pl.ombre != null && pl.ombrePortee != null)
                pl.ombre.gameObject.SetActive(false);
            if (pc != null && pl.ombrePortee != null)
                PoseOmbre(ref pl.matOmbreJeu, pl.ombrePortee, p.corps, pc.corps, place, false, pc.miroir);
            if (pc != null && pl.ombreLanterne != null)
                PoseOmbre(ref pl.matOmbreLanterneJeu, pl.ombreLanterne, p.lanterne, pc.lanterne, place, true);
            if (pc != null && pl.eclairage != null)
            {
                Vector2 c = new Vector2(pc.lanterne.center.x, pc.lanterne.yMax - pc.lanterne.height * 0.12f);
                float r = pc.lanterne.height * eclairageLanterne.x;
                MenuEcran.Pose(pl.eclairage.rectTransform, c.x - r, c.y - r * eclairageLanterne.y, r * 2, r * eclairageLanterne.y * 2);
                Color lc = Color.Lerp(new Color(1f, 0.78f, 0.5f), CouleurLueur(p.couleur), eclairageLanterne.z);
                lc.a = 1f;
                pl.eclairage.color = lc;
                pl.eclairage.gameObject.SetActive(true);
            }
            if (pc != null && pl.ombre != null && pl.ombrePortee == null)
            {
                Rect r = pc.corps;
                float x0 = Mathf.Min(pc.pieds.x, r.center.x - r.width * 0.22f) - ombre.z;
                float x1 = Mathf.Max(pc.pieds.y, r.center.x + r.width * 0.22f) + ombre.z;
                float h = (x1 - x0) * ombre.y;
                MenuEcran.Pose(pl.ombre.rectTransform, x0, r.yMax - h * 0.62f, x1 - x0, h);
                pl.ombre.color = new Color(0, 0, 0, ombre.x);
                pl.ombre.gameObject.SetActive(true);
            }
            if (pc != null && pl.lanterne != null)
            {
                pl.lanterne.sprite = p.lanterne;
                MenuEcran.Pose(pl.lanterne.rectTransform, pc.lanterne.x, pc.lanterne.y, pc.lanterne.width, pc.lanterne.height);
                pl.lanterne.gameObject.SetActive(true);
            }
            if (pc != null && pl.lueur != null)
            {
                Vector2 c = pc.lanterne.center;
                float r = Mathf.Max(pc.lanterne.width, pc.lanterne.height) * rayonLueur;
                if (pl.eclairage != null)
                {
                    // halo autour du verre (la lumière reçue par le sol et la bûche est la couche Eclairage)
                    float rv = pc.lanterne.height * 0.75f;
                    float cy = pc.lanterne.y + pc.lanterne.height * 0.6f;
                    MenuEcran.Pose(pl.lueur.rectTransform, c.x - rv, cy - rv * 0.9f, rv * 2, rv * 1.8f);
                }
                else // lueur au sol autour du pied de la lanterne (ellipse aplatie)
                    MenuEcran.Pose(pl.lueur.rectTransform, c.x - r, pc.lanterne.yMax - r * 0.55f, r * 2, r * 0.9f);
                pl.lueur.color = CouleurLueur(p.couleur);
                pl.lueur.gameObject.SetActive(true);
            }
            if (pl.cartouche != null)
                pl.cartouche.SetActive(true);
            if (pl.nom != null)
                pl.nom.text = nom;
            if (pl.plus != null)
                pl.plus.SetActive(false);
            if (pl.lanterneEteinte != null)
                pl.lanterneEteinte.SetActive(false);
            if (apparition && isActiveAndEnabled)
                StartCoroutine(Apparition(pl));
        }

        /// <summary>La place se libère : lanterne éteinte et bouton + (places 3 et 4).</summary>
        public void Libere(int place)
        {
            Place pl = places[place];
            if (pl == null)
                return;
            pl.occupant = -1;
            if (pl.corps != null) pl.corps.gameObject.SetActive(false);
            if (pl.ombre != null) pl.ombre.gameObject.SetActive(false);
            if (pl.ombrePortee != null) pl.ombrePortee.gameObject.SetActive(false);
            if (pl.ombreLanterne != null) pl.ombreLanterne.gameObject.SetActive(false);
            if (pl.eclairage != null) pl.eclairage.gameObject.SetActive(false);
            if (pl.lanterne != null) pl.lanterne.gameObject.SetActive(false);
            if (pl.lueur != null) pl.lueur.gameObject.SetActive(false);
            if (pl.cartouche != null) pl.cartouche.SetActive(false);
            if (pl.plus != null) pl.plus.SetActive(true);
            if (pl.lanterneEteinte != null) pl.lanterneEteinte.SetActive(true);
            if (pl.coche != null) pl.coche.SetActive(false);
        }

        /// <summary>Lumière de la scène sur le personnage : feu et lanterne en uv du sprite, valeur et saturation.</summary>
        private void Eclaire(Place pl, Perso p, Placement pc)
        {
            Material m = pl.corps.material;
            if (m == null)
                return;
            if (Application.isPlaying)
            {
                // en jeu : une copie par place (les matériaux de l'asset restent ceux de l'aperçu)
                if (pl.matJeu == null)
                {
                    pl.matJeu = new Material(m) { name = m.name + " (jeu)" };
                    pl.corps.material = pl.matJeu;
                }
                m = pl.matJeu;
            }
            Rect r = pc.corps;
            Vector2 Uv(Vector2 px) => new Vector2((px.x - r.x) / r.width, 1f - (px.y - r.y) / r.height);
            Vector2 f = Uv(foyer);
            if (pc.miroir) f.x = 1f - f.x;
            m.SetVector("_FeuUV", new Vector4(f.x, f.y, 0, 0));
            Vector2 l = Uv(new Vector2(pc.lanterne.center.x, pc.lanterne.y + pc.lanterne.height * 0.45f));
            if (pc.miroir) l.x = 1f - l.x;
            m.SetVector("_LanterneUV", new Vector4(l.x, l.y, pc.lanterne.height * rayonLanterneCorps / r.height, 0));
            Color chaud = new Color(1f, 0.78f, 0.5f);
            m.SetColor("_LanterneCouleur", Color.Lerp(chaud, p.couleur, teinteLanterne));
            m.SetFloat("_Luminosite", pc.luminosite > 0f ? pc.luminosite : p.luminosite);
            m.SetFloat("_Saturation", pc.saturation > 0f ? pc.saturation : p.saturation);
            m.SetFloat("_Contraste", pc.contraste > 0f ? pc.contraste : p.contraste);
            m.SetFloat("_Douceur", pc.douceur);
            m.SetFloat("_Detail", pc.detail);
            int i = Array.IndexOf(places, pl);
            Color oc = i >= 0 && i < ombresPlaces.Length ? ombresPlaces[i] : new Color(0.5f, 0.5f, 1f);
            m.SetColor("_OmbreCouleur", oc * 0.6f);
            // lanterne : même lumière de scène (feu, ombres du décor), sans sa propre lueur ; le verre allumé reste tel quel
            if (pl.lanterne != null && pl.lanterne.material != null && pl.lanterne.material.shader == m.shader)
            {
                Material ml = pl.lanterne.material;
                if (Application.isPlaying)
                {
                    if (pl.matLanterneJeu == null)
                    {
                        pl.matLanterneJeu = new Material(ml) { name = ml.name + " (jeu)" };
                        pl.lanterne.material = pl.matLanterneJeu;
                    }
                    ml = pl.matLanterneJeu;
                }
                Rect rl = pc.lanterne;
                Vector2 fl = new Vector2((foyer.x - rl.x) / rl.width, 1f - (foyer.y - rl.y) / rl.height);
                ml.SetVector("_FeuUV", new Vector4(fl.x, fl.y, 0, 0));
                ml.SetFloat("_Luminosite", pc.reglageLanterne.x);
                ml.SetFloat("_Saturation", pc.reglageLanterne.y);
                ml.SetFloat("_Contraste", 1f);
                ml.SetFloat("_Douceur", pc.douceur);
                ml.SetFloat("_Detail", 1f);
                ml.SetColor("_OmbreCouleur", oc * 0.6f);
            }
        }

        [Tooltip("Luminance visée des lueurs de lanterne (nuit 5) : le vert, le jaune et le bleu, perçus bien plus clairs que le rouge, sont ramenés à la même force.")]
        public float luminanceLueur = 0.4f;

        [Tooltip("Part de lumière chaude neutre mêlée à la couleur du personnage dans la lueur de sa lanterne (nuit 5).")]
        public float chaleurLueur = 0.45f;

        /// <summary>Couleur de lanterne mêlée de lumière chaude puis ramenée à une luminance commune (le vert du koala faisait
        /// un aplat saturé sur la bûche et le rocher).</summary>
        private Color CouleurLueur(Color c)
        {
            c = Color.Lerp(c, new Color(1f, 0.8f, 0.58f, c.a), chaleurLueur);
            float l = c.r * 0.2126f + c.g * 0.7152f + c.b * 0.0722f;
            float k = l > luminanceLueur ? luminanceLueur / l : 1f;
            return new Color(c.r * k, c.g * k, c.b * k, c.a);
        }

        /// <summary>Retourne horizontalement un rect posé par MenuEcran.Pose (même emprise à l'écran).</summary>
        private static void Retourne(RectTransform rt)
        {
            Vector2 pos = rt.anchoredPosition, size = rt.sizeDelta;
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(pos.x + size.x * 0.5f, pos.y);
            // nuit 5 : rotation de 180° au lieu d'une échelle -1 : les apparitions (ici et MainMenuArt.Appear de main) règlent
            // localScale et écrasaient le retournement pendant 0,6 s
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        /// <summary>Pivot au bas du rect (même emprise) : un fondu qui écrase la hauteur garde les pieds sur le siège.</summary>
        private static void PivotBas(RectTransform rt)
        {
            Vector2 piv = rt.pivot, size = rt.sizeDelta;
            rt.pivot = new Vector2(piv.x, 0f);
            rt.anchoredPosition += new Vector2(0f, -size.y * piv.y);
        }

        /// <summary>Ombre (contact et portée) d'un sprite posé dans le rect r : image élargie, uv du sprite aux coins.</summary>
        private void PoseOmbre(ref Material matJeu, Image im, Sprite spr, Rect r, int place, bool estLanterne, bool miroir = false)
        {
            if (spr == null)
            {
                im.gameObject.SetActive(false);   // nuit 5 : plus d'ombre du personnage précédent
                return;
            }
            im.sprite = spr;
            float gx = margeOmbre.x * r.width, dx = margeOmbre.y * r.width, by = margeOmbre.z * r.height;
            if (estLanterne) { gx *= 0.8f; dx *= 0.8f; by *= 0.6f; }
            // retourné : la marge gauche des uv (gx) tombe à droite à l'écran
            if (miroir) { MenuEcran.Pose(im.rectTransform, r.x - dx, r.y, r.width + gx + dx, r.height + by); Retourne(im.rectTransform); }
            else MenuEcran.Pose(im.rectTransform, r.x - gx, r.y, r.width + gx + dx, r.height + by);
            Material m = im.material;
            if (m == null)
                return;
            if (Application.isPlaying)
            {
                if (matJeu == null)
                {
                    matJeu = new Material(m) { name = m.name + " (jeu)" };
                    im.material = matJeu;
                }
                m = matJeu;
            }
            m.SetVector("_Cadre", new Vector4(-gx / r.width, -by / r.height, 1f + dx / r.width, 1f));
            float cote = Mathf.Sign(foyer.x - r.center.x) * (miroir ? -1f : 1f);   // +1 : feu à droite (dans les uv du sprite)
            Color oc = place < ombresPlaces.Length ? ombresPlaces[place] : new Color(0.5f, 0.5f, 1f);
            float lum = oc.r * 0.2126f + oc.g * 0.7152f + oc.b * 0.0722f;
            Color mul = oc * (valeurOmbre / Mathf.Max(lum, 1e-3f));
            mul.r = Mathf.Min(mul.r, 1f); mul.g = Mathf.Min(mul.g, 1f); mul.b = Mathf.Min(mul.b, 1f); mul.a = 1f;
            m.SetColor("_Couleur", mul);
            m.SetFloat("_Cisaille", -cote * cisaillement);
            m.SetVector("_ContactDecal", new Vector4(-cote * 0.012f, -0.018f, 0, 0));
            im.gameObject.SetActive(true);
        }

        public void Pret(int place, bool pret)
        {
            Place pl = places[place];
            if (pl?.coche == null)
                return;
            bool on = pret && pl.occupant >= 0;
            pl.coche.SetActive(on);
            // comme sur la maquette, le nom se décale à gauche pour laisser la place à la coche
            if (pl.nom != null && pl.cartouche != null)
            {
                var cart = (RectTransform)pl.cartouche.transform;
                var co = (RectTransform)pl.coche.transform;
                var rt = pl.nom.rectTransform;
                float w = on ? co.anchoredPosition.x - 4f : cart.sizeDelta.x;
                rt.sizeDelta = new Vector2(w, rt.sizeDelta.y);
            }
        }

        private IEnumerator Apparition(Place pl)
        {
            // fondu et léger grandissement depuis le siège, la lueur de la lanterne s'allume un peu plus fort
            var cg = pl.racine != null ? pl.racine.GetComponent<CanvasGroup>() : null;
            Vector3 s0 = pl.corps != null ? pl.corps.rectTransform.localScale : Vector3.one;
            Color lc = pl.lueur != null ? pl.lueur.color : Color.white;
            for (float t = 0; t < dureeApparition; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0, 1, t / dureeApparition);
                if (cg != null) cg.alpha = k;
                if (pl.corps != null) pl.corps.rectTransform.localScale = new Vector3(1, Mathf.Lerp(0.96f, 1f, k), 1);
                if (pl.lueur != null) pl.lueur.color = lc * (1f + 1.5f * (1f - k)) * k;
                yield return null;
            }
            if (cg != null) cg.alpha = 1;
            if (pl.corps != null) pl.corps.rectTransform.localScale = s0;
            if (pl.lueur != null) pl.lueur.color = lc;
        }
    }
}
