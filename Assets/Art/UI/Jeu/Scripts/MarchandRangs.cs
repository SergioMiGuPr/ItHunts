using System;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Marchand à rangs (nuit 4, lot 6 ; règles de docs/design/AMELIORATIONS.md) : trois cartes côte à côte, une amélioration
    /// nouvelle (toujours en Bronze), une montée de rang d'une amélioration possédée (cadre du rang actuel → cadre du rang
    /// suivant), le soin. Chaque carte : illustration dans son cadre de rang, nom, rang, description, prix en âmes. Le jeu appelle
    /// Remplit puis écoute Achat (0 nouvelle, 1 montée, 2 soin) et Ferme. Une carte sans offre (null) est masquée.
    /// </summary>
    public sealed class MarchandRangs : MonoBehaviour
    {
        [Serializable]
        public sealed class Offre
        {
            public string id;            // identifiant de AMELIORATIONS.md (illustration et passif via le catalogue)
            public string nom;
            public int rang;             // rang actuel (nouvelle : 0, Bronze)
            [TextArea] public string description;
            public int prix;
        }

        [Serializable]
        public sealed class Carte
        {
            public Button bouton;
            public IconeAmelioration illustration;
            public Text nom, rang, description, prix;
            public Image flamme, lueur;
            public IconeAmelioration rangApres;   // montée : petite icône encadrée au rang suivant (null ailleurs)
            public Graphic[] corps = new Graphic[0];
        }

        public CatalogueAmeliorations catalogue;
        public Text titre, solde;
        public Carte nouvelle, montee, soin;
        public Button fermer;
        public Sprite iconeSoin;
        public Color prixOk = new Color(0.86f, 0.9f, 0.96f), prixTrop = new Color(0.86f, 0.24f, 0.2f), teinteIndispo = new Color(0.42f, 0.4f, 0.44f);

        public event Action<int> Achat;
        public event Action Ferme;

        [Header("Aperçu")]
        public Offre apercuNouvelle, apercuMontee;
        public int apercuSoinPV = 40, apercuSoinPrix = 35, apercuAmes = 120;

        private void Awake()
        {
            if (nouvelle?.bouton) nouvelle.bouton.onClick.AddListener(() => Achat?.Invoke(0));
            if (montee?.bouton) montee.bouton.onClick.AddListener(() => Achat?.Invoke(1));
            if (soin?.bouton) soin.bouton.onClick.AddListener(() => Achat?.Invoke(2));
            if (fermer) fermer.onClick.AddListener(() => Ferme?.Invoke());
        }

        public void Remplit(Offre offreNouvelle, Offre offreMontee, int soinPV, int soinPrix, int ames)
        {
            if (solde) solde.text = ames.ToString();
            Pose(nouvelle, offreNouvelle, ames, false);
            Pose(montee, offreMontee, ames, true);
            if (soin != null && soin.bouton)
            {
                bool on = soinPrix > 0;
                soin.bouton.gameObject.SetActive(on);
                if (on)
                {
                    if (soin.illustration && soin.illustration.icone) soin.illustration.icone.sprite = iconeSoin;
                    if (soin.nom) soin.nom.text = "Soin";
                    if (soin.rang) { soin.rang.text = "+" + soinPV + " PV"; soin.rang.color = prixOk; }
                    if (soin.description) soin.description.text = "Referme vos plaies : " + soinPV + " points de vie tout de suite.";
                    Prix(soin, soinPrix, ames);
                }
            }
        }

        /// <summary>Couleur du texte de chaque rang (accord avec les cadres).</summary>
        public Color[] couleursRangs =
        {
            new Color(0.80f, 0.56f, 0.38f), new Color(0.80f, 0.81f, 0.84f), new Color(0.95f, 0.79f, 0.40f),
            new Color(0.62f, 0.84f, 0.97f), new Color(0.84f, 0.52f, 0.96f)
        };

        string Rang(int r)
        {
            r = Mathf.Clamp(r, 0, 4);
            return "<color=#" + ColorUtility.ToHtmlStringRGB(couleursRangs[r]) + ">" + CatalogueAmeliorations.Rangs[r] + "</color>";
        }

        private void Pose(Carte c, Offre o, int ames, bool estMontee)
        {
            if (c == null || c.bouton == null) return;
            if (o != null && estMontee && o.rang >= 4) o = null;      // pas de montée au-delà de Mythique : carte masquée
            c.bouton.gameObject.SetActive(o != null);
            if (o == null) return;
            int r = Mathf.Clamp(o.rang, 0, 4);
            if (c.illustration) { c.illustration.catalogue = catalogue; c.illustration.Configure(o.id, r); }
            if (c.nom) c.nom.text = o.nom;
            if (estMontee)
            {
                if (c.rang) { c.rang.supportRichText = true; c.rang.text = Rang(r) + "  \u2192  " + Rang(r + 1); }
                if (c.rangApres) { c.rangApres.catalogue = catalogue; c.rangApres.Configure(o.id, r + 1); }
            }
            else
            {
                if (c.rang) { c.rang.supportRichText = true; c.rang.text = (catalogue && catalogue.EstPassif(o.id) ? "Passif, " : "") + Rang(r); }
                if (c.rangApres) c.rangApres.Vide();
            }
            if (c.description) c.description.text = o.description;
            Prix(c, o.prix, ames);
        }

        /// <summary>Indisponible (âmes insuffisantes) : le cadre reste, l'illustration s'assombrit, prix en rouge, bouton inactif.</summary>
        private void Prix(Carte c, int p, int ames)
        {
            bool ok = ames >= p;
            if (c.prix) { c.prix.text = p.ToString(); c.prix.color = ok ? prixOk : prixTrop; }
            if (c.illustration && c.illustration.icone) c.illustration.icone.color = ok ? Color.white : teinteIndispo;
            if (c.rangApres && c.rangApres.icone) c.rangApres.icone.color = ok ? Color.white : teinteIndispo;
            if (c.flamme) c.flamme.color = ok ? Color.white : teinteIndispo;
            if (c.bouton) c.bouton.interactable = ok;
        }

        public void RemplitApercu() => Remplit(apercuNouvelle, apercuMontee, apercuSoinPV, apercuSoinPrix, apercuAmes);
    }
}
