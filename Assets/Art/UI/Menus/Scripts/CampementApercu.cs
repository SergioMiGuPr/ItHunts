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
            public Image ombre;                 // ombre de contact (multiplication douce) sous le siège et les pieds
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
            pl.occupant = perso;
            if (pc != null && pl.corps != null)
            {
                pl.corps.sprite = p.corps;
                MenuEcran.Pose(pl.corps.rectTransform, pc.corps.x, pc.corps.y, pc.corps.width, pc.corps.height);
                pl.corps.gameObject.SetActive(true);
                Eclaire(pl, p, pc);
            }
            if (pc != null && pl.ombre != null)
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
                // lueur au sol autour du pied de la lanterne (ellipse aplatie)
                float r = Mathf.Max(pc.lanterne.width, pc.lanterne.height) * rayonLueur;
                MenuEcran.Pose(pl.lueur.rectTransform, c.x - r, pc.lanterne.yMax - r * 0.55f, r * 2, r * 0.9f);
                pl.lueur.color = p.couleur;
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
            m.SetVector("_FeuUV", new Vector4(f.x, f.y, 0, 0));
            Vector2 l = Uv(new Vector2(pc.lanterne.center.x, pc.lanterne.y + pc.lanterne.height * 0.45f));
            m.SetVector("_LanterneUV", new Vector4(l.x, l.y, pc.lanterne.height * rayonLanterneCorps / r.height, 0));
            Color chaud = new Color(1f, 0.78f, 0.5f);
            m.SetColor("_LanterneCouleur", Color.Lerp(chaud, p.couleur, teinteLanterne));
            m.SetFloat("_Luminosite", p.luminosite);
            m.SetFloat("_Saturation", p.saturation);
            m.SetFloat("_Contraste", p.contraste);
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
