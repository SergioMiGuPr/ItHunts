using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Coffre et marchand (nuit 5, lot 2) : cartes d'offre. Chaque carte : illustration dans son cadre (couleur du personnage,
    /// étoiles du rang), nom, **rang actuel → rang suivant** (« Nouvelle → Bronze », « Argent → Or »), effet précis au rang
    /// proposé, prix en âmes (masqué au coffre, gratuit). Une montée de rang montre l'amélioration au rang actuel, une flèche,
    /// puis au rang suivant. Soin : cœur sans cadre, « +N PV ». Coffre : bouton **Relancer** (prix en âmes, 0 = gratuit,
    /// -1 = masqué) qui retire toutes les offres. Carte trop chère : illustration assombrie, prix rouge, bouton inactif.
    /// Survol ou sélection : lueur à la couleur du personnage, la carte grandit de 3 %.
    /// La logique (tirage, achat, focus manette) reste à main : Remplit, puis écouter Achat, Relance, Ferme.
    /// </summary>
    public sealed class PanneauOffres : MonoBehaviour
    {
        [Serializable]
        public sealed class Offre
        {
            public string id;
            public string nom;
            [Tooltip("-1 : amélioration nouvelle (arrive en Bronze) ; sinon rang possédé (0 Bronze … 3 Diamant), la carte propose rang + 1.")]
            public int rangActuel = -1;
            [TextArea] public string description;
            public int prix;
            [Tooltip("Carte de soin (illustration soin, pas de cadre ni d'étoiles) : PV rendus.")]
            public bool soin;
            public int soinPV;
        }

        [Serializable]
        public sealed class Carte
        {
            public Button bouton;
            public RectTransform racine;
            public IconeAmelioration avant, apres;
            public Text fleche, nom, rang, description, prix, etiquette;
            public Image flamme, lueur, liseré;
            public Graphic[] corps = new Graphic[0];
            [NonSerialized] public bool abordable = true;
            [NonSerialized] public int proprietaire;
        }

        public CatalogueAmeliorations catalogue;
        public Text titre, solde;
        public Image flammeSolde;
        public Carte[] cartes = new Carte[0];
        public Button relancer, fermer;
        public Text prixRelance;
        public Image flammeRelance;
        public Sprite illustrationSoin;
        public bool coffre;
        [Header("Couleurs")]
        public Color prixOk = new Color(0.86f, 0.9f, 0.96f), prixTrop = new Color(0.86f, 0.24f, 0.25f), eteint = new Color(0.42f, 0.4f, 0.44f);
        public Color nomOk = new Color(0.969f, 0.882f, 0.702f);
        public Color rangAvant = new Color(0.78f, 0.74f, 0.70f), rangApres = new Color(0.97f, 0.86f, 0.55f);

        [Header("Mise en page de l'illustration (pixels de la carte, depuis le haut gauche)")]
        public float tailleSeule = 176f, ySeule = 58f, tailleMontee = 132f, yMontee = 78f, margeMontee = 30f;

        public event Action<int> Achat;
        public event Action Relance, Ferme;

        private int _focus = -1;

        [Header("Aperçu (scène de la DA)")]
        public Offre[] apercu = new Offre[0];
        public int apercuAmes = 120, apercuRelance = 25;

        private void Awake()
        {
            for (int i = 0; i < cartes.Length; i++)
            {
                int k = i;
                if (cartes[i]?.bouton != null) cartes[i].bouton.onClick.AddListener(() => { if (cartes[k].abordable) Achat?.Invoke(k); });
                var s = cartes[i]?.racine != null ? cartes[i].racine.GetComponent<SurvolCarte>() : null;
                if (s == null && cartes[i]?.racine != null) s = cartes[i].racine.gameObject.AddComponent<SurvolCarte>();
                if (s != null) { s.panneau = this; s.index = k; }
            }
            if (relancer) relancer.onClick.AddListener(() => Relance?.Invoke());
            if (fermer) fermer.onClick.AddListener(() => Ferme?.Invoke());
        }

        public void RemplitApercu() => Remplit(coffre ? "Coffre" : "Marchand", apercu, apercuAmes, coffre ? apercuRelance : -1);

        /// <summary>offres : une par carte (null ou plus court : cartes masquées) ; prixRelance : -1 masque le bouton, 0 = gratuit.</summary>
        public void Remplit(string nomPanneau, Offre[] offres, int ames, int prixRelanceAmes = -1)
        {
            if (titre && !string.IsNullOrEmpty(nomPanneau)) titre.text = nomPanneau;
            if (solde) solde.text = ames.ToString();
            for (int i = 0; i < cartes.Length; i++)
            {
                var c = cartes[i];
                if (c == null || c.racine == null) continue;
                var o = offres != null && i < offres.Length ? offres[i] : null;
                c.racine.gameObject.SetActive(o != null);
                if (o != null) RemplitCarte(c, o, ames);
            }
            if (relancer)
            {
                relancer.gameObject.SetActive(prixRelanceAmes >= 0);
                bool ok = prixRelanceAmes <= ames;
                relancer.interactable = ok;
                if (prixRelance) { prixRelance.text = prixRelanceAmes > 0 ? prixRelanceAmes.ToString() : "gratuit"; prixRelance.color = ok ? prixOk : prixTrop; }
                if (flammeRelance) flammeRelance.gameObject.SetActive(prixRelanceAmes > 0);
            }
            Focus(_focus);
        }

        private void RemplitCarte(Carte c, Offre o, int ames)
        {
            bool montee = !o.soin && o.rangActuel >= 0;
            int suivant = Mathf.Clamp(o.rangActuel + 1, 0, 4);
            c.proprietaire = o.soin ? 0 : CatalogueAmeliorations.Proprietaire(o.id);
            if (c.avant)
            {
                var a = (RectTransform)c.avant.transform;
                float w = c.racine.rect.width;
                a.anchorMin = a.anchorMax = a.pivot = new Vector2(0f, 1f);
                if (montee) { a.sizeDelta = Vector2.one * tailleMontee; a.anchoredPosition = new Vector2(margeMontee, -yMontee); }
                else { a.sizeDelta = Vector2.one * tailleSeule; a.anchoredPosition = new Vector2((w - tailleSeule) / 2f, -ySeule); }
            }
            if (c.nom) c.nom.text = o.nom;
            if (c.description) c.description.text = o.description;
            if (o.soin)
            {
                if (c.avant) { c.avant.Configure(illustrationSoin, 0, false, 0); if (c.avant.cadre) c.avant.cadre.enabled = false; c.avant.PoseEtoiles(0); }
                if (c.apres) c.apres.Vide();
                if (c.fleche) c.fleche.gameObject.SetActive(false);
                if (c.rang) c.rang.text = "+" + o.soinPV + " PV";
                if (c.etiquette) c.etiquette.text = "Soin";
            }
            else
            {
                if (c.avant) c.avant.Configure(o.id, montee ? o.rangActuel : 0);
                if (c.apres) { if (montee) c.apres.Configure(o.id, suivant); else c.apres.Vide(); }
                if (c.fleche) c.fleche.gameObject.SetActive(montee);
                if (c.rang)
                {
                    string a = montee ? CatalogueAmeliorations.Rangs[o.rangActuel] : "Nouvelle";
                    c.rang.text = "<color=#" + ColorUtility.ToHtmlStringRGB(rangAvant) + ">" + a + "</color>  →  <color=#" +
                                  ColorUtility.ToHtmlStringRGB(rangApres) + ">" + CatalogueAmeliorations.Rangs[suivant] + "</color>";
                }
                if (c.etiquette)
                {
                    bool passif = catalogue && catalogue.EstPassif(o.id);
                    string qui = c.proprietaire == 0 ? "Commune" : CatalogueAmeliorations.Personnages[c.proprietaire];
                    c.etiquette.text = (montee ? "Montée de rang" : passif ? "Nouveau passif" : "Nouvelle amélioration") + "  ·  " + qui;
                }
            }
            bool gratuit = coffre || o.prix <= 0;
            c.abordable = gratuit || o.prix <= ames;
            if (c.prix) { c.prix.gameObject.SetActive(!gratuit); c.prix.text = o.prix.ToString(); c.prix.color = c.abordable ? prixOk : prixTrop; }
            if (c.flamme) c.flamme.gameObject.SetActive(!gratuit);
            if (c.bouton) c.bouton.interactable = c.abordable;
            Color teinte = c.abordable ? Color.white : eteint;
            foreach (var ia in new[] { c.avant, c.apres })
            {
                if (!ia) continue;
                if (ia.icone) ia.icone.color = teinte;
                if (ia.cadre) ia.cadre.color = teinte;
                foreach (var e in ia.etoiles) if (e) e.color = teinte;
            }
            if (c.nom) c.nom.color = c.abordable ? nomOk : eteint;
            if (c.rang) c.rang.color = c.abordable ? Color.white : eteint;
            Color col = catalogue && !o.soin ? catalogue.Couleur(c.proprietaire) : new Color(0.86f, 0.36f, 0.36f);
            if (c.liseré) c.liseré.color = new Color(col.r, col.g, col.b, c.abordable ? 0.85f : 0.35f);
            if (c.lueur) c.lueur.color = new Color(col.r, col.g, col.b, 0);
        }

        /// <summary>Carte qui a le focus (souris ou manette) : -1 aucune.</summary>
        public void Focus(int i)
        {
            _focus = i;
            for (int k = 0; k < cartes.Length; k++)
            {
                var c = cartes[k];
                if (c?.racine == null) continue;
                bool on = k == i;
                c.racine.localScale = Vector3.one * (on ? 1.03f : 1f);
                if (c.lueur) { var l = c.lueur.color; l.a = on ? (c.abordable ? 0.5f : 0.2f) : 0f; c.lueur.color = l; }
            }
        }
    }
}
