using System;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Écran de fin de run (nuit du 2026-11-22) : titre gothique « Tu es mort » (rouge) ou « Victoire » (or), sous-titre
    /// (cause de la mort ou acte vaincu), lignes de statistiques (libellé à gauche, valeur à droite), deux boutons
    /// (Recommencer, Campement). Le jeu appelle Remplit puis écoute Recommencer et Campement.
    /// </summary>
    public sealed class FinDeRun : MonoBehaviour
    {
        [Serializable] public struct Stat { public string libelle; public string valeur; }

        public Text titre, sousTitre;
        public Text[] libelles, valeurs;
        public Image lueurTitre;
        public Button recommencer, campement;
        public Color couleurMort = new Color(0.86f, 0.22f, 0.25f), couleurVictoire = new Color(0.97f, 0.86f, 0.55f);
        [Header("Aperçu")] public bool victoireApercu;
        public string sousTitreApercu = "Attrapé par le traqueur, acte II";
        public Stat[] apercu;

        [Header("Améliorations (nuit 4) : grille d'icônes encadrées, 10 au plus")]
        public Text titreAmeliorations;
        [Tooltip("Plus d'améliorations que de cases : affiche +N.")] public Text surplus;
        public IconeAmelioration[] ameliorations = new IconeAmelioration[0];
        public string[] apercuAmeliorations = { "R-P1", "G12", "G1", "R5", "G20", "G26", "R8", "G7" };
        public int[] apercuRangs = { 2, 4, 1, 3, 0, 2, 1, 0 };

        public event Action Recommencer, Campement;
        [SerializeField, HideInInspector] private float _x0Serialise = float.NaN;
        private float? _x0 { get => float.IsNaN(_x0Serialise) ? (float?)null : _x0Serialise; set => _x0Serialise = value ?? float.NaN; }

        private void Awake()
        {
            if (recommencer) recommencer.onClick.AddListener(() => Recommencer?.Invoke());
            if (campement) campement.onClick.AddListener(() => Campement?.Invoke());
        }

        public void Remplit(bool victoire, string sousTitreTexte, Stat[] stats)
        {
            titre.text = victoire ? "Victoire" : "Tu es mort";
            titre.color = victoire ? couleurVictoire : couleurMort;
            if (lueurTitre) lueurTitre.color = new Color(titre.color.r, titre.color.g, titre.color.b, 0.35f);
            sousTitre.text = sousTitreTexte;
            for (int i = 0; i < libelles.Length && i < valeurs.Length; i++)
            {
                bool on = stats != null && i < stats.Length;
                libelles[i].gameObject.SetActive(on);
                valeurs[i].gameObject.SetActive(on);
                if (!on) continue;
                libelles[i].text = stats[i].libelle;
                valeurs[i].text = stats[i].valeur;
            }
        }

        /// <summary>Améliorations de la run (identifiants de AMELIORATIONS.md et rangs 0 à 4), le passif en premier.</summary>
        public void RemplitAmeliorations(string[] ids, int[] rangs)
        {
            int n = ids != null ? Mathf.Min(ids.Length, ameliorations.Length) : 0;
            for (int i = 0; i < ameliorations.Length; i++)
            {
                if (ameliorations[i] == null) continue;
                if (i < n) ameliorations[i].Configure(ids[i], rangs != null && i < rangs.Length ? rangs[i] : 0);
                else ameliorations[i].Vide();
            }
            if (titreAmeliorations) titreAmeliorations.text = n > 0 ? "Améliorations" : "Aucune amélioration";
            int reste = ids != null ? ids.Length - n : 0;
            if (surplus) { surplus.gameObject.SetActive(reste > 0); surplus.text = "+" + reste; }
            // la rangée reste centrée dans le cadre quel que soit le nombre d'améliorations
            if (n > 0 && ameliorations.Length > 1 && ameliorations[0] && ameliorations[1])
            {
                var r0 = (RectTransform)ameliorations[0].transform; var r1 = (RectTransform)ameliorations[1].transform;
                if (_x0 == null) _x0 = r0.anchoredPosition.x;
                float pas = r1.anchoredPosition.x - r0.anchoredPosition.x;
                float decal = (ameliorations.Length - n) * pas / 2f;
                for (int i = 0; i < ameliorations.Length; i++)
                {
                    if (!ameliorations[i]) continue;
                    var rt = (RectTransform)ameliorations[i].transform;
                    rt.anchoredPosition = new Vector2(_x0.Value + decal + i * pas, rt.anchoredPosition.y);
                }
            }
        }

        public void RemplitApercu()
        {
            Remplit(victoireApercu, sousTitreApercu, apercu);
            RemplitAmeliorations(apercuAmeliorations, apercuRangs);
        }
    }
}
