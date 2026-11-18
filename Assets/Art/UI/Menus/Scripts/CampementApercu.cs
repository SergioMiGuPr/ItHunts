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
        }

        [Serializable]
        public sealed class Placement
        {
            public string perso;
            public int place;
            public Rect corps;      // pixels de la maquette (x, y depuis le haut gauche, w, h)
            public Rect lanterne;
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
            [NonSerialized] public int occupant = -1;
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
            if (pl.lanterne != null) pl.lanterne.gameObject.SetActive(false);
            if (pl.lueur != null) pl.lueur.gameObject.SetActive(false);
            if (pl.cartouche != null) pl.cartouche.SetActive(false);
            if (pl.plus != null) pl.plus.SetActive(true);
            if (pl.lanterneEteinte != null) pl.lanterneEteinte.SetActive(true);
            if (pl.coche != null) pl.coche.SetActive(false);
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
