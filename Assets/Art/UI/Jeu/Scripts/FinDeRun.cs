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

        public event Action Recommencer, Campement;

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
            for (int i = 0; i < libelles.Length; i++)
            {
                bool on = stats != null && i < stats.Length;
                libelles[i].gameObject.SetActive(on);
                valeurs[i].gameObject.SetActive(on);
                if (!on) continue;
                libelles[i].text = stats[i].libelle;
                valeurs[i].text = stats[i].valeur;
            }
        }

        public void RemplitApercu() => Remplit(victoireApercu, sousTitreApercu, apercu);
    }
}
