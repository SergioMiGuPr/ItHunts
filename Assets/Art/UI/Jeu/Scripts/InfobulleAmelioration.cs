using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Infobulle d'une amélioration (nuit 5, lot 2) : survol d'une case du HUD (curseur libre : pause, carte), d'une carte de coffre
    /// ou de marchand, d'une case du codex. Contenu : illustration encadrée (couleur du personnage, étoiles du rang), nom, rang et
    /// personnage, effet général, effet précis au rang (ligne du tableau de AMELIORATIONS.md). Liseré à la couleur du personnage.
    /// Se place à droite de la case survolée (à gauche si l'écran manque), sans sortir du canvas. Fondu de 0,12 s.
    /// </summary>
    public sealed class InfobulleAmelioration : MonoBehaviour
    {
        public CatalogueAmeliorations catalogue;
        public CanvasGroup groupe;
        public RectTransform boite;
        public IconeAmelioration icone;
        public Text nom, rang, effet, effetRang;
        [Tooltip("Liseré et lueur à la couleur du personnage.")]
        public Graphic[] teintes = new Graphic[0];
        public float ecart = 18f, fondu = 0.12f;

        private float _cible;

        private void Awake()
        {
            if (groupe) { groupe.alpha = 0; groupe.blocksRaycasts = false; groupe.interactable = false; }
        }

        /// <summary>Avec la fiche du catalogue (aperçus, codex).</summary>
        public void Montre(string id, int rangIndex, RectTransform ancre)
        {
            var f = catalogue ? catalogue.FicheDe(id) : null;
            string txtRang = f != null && f.rangs != null && rangIndex >= 0 && rangIndex < f.rangs.Length ? f.rangs[rangIndex] : "";
            Montre(id, rangIndex, f != null ? f.nom : id, f != null ? f.effet : "", txtRang, ancre);
        }

        /// <summary>Avec les textes de main (UpgradeDefinition : displayName, description, RankText(rang)).</summary>
        public void Montre(string id, int rangIndex, string nomAmelioration, string effetGeneral, string effetAuRang, RectTransform ancre)
        {
            int prop = CatalogueAmeliorations.Proprietaire(id);
            bool passif = catalogue && catalogue.EstPassif(id);
            if (icone) icone.Configure(id, rangIndex);
            if (nom) nom.text = nomAmelioration;
            if (rang)
            {
                string r = CatalogueAmeliorations.Rangs[Mathf.Clamp(rangIndex, 0, 4)];
                string qui = prop == 0 ? "tous les personnages" : CatalogueAmeliorations.Personnages[prop];
                rang.text = "Rang " + r + "  ·  " + qui + (passif ? "  ·  passif" : "");
            }
            if (effet) effet.text = effetGeneral;
            if (effetRang) effetRang.text = string.IsNullOrEmpty(effetAuRang) ? "" :
                "<b>" + CatalogueAmeliorations.Rangs[Mathf.Clamp(rangIndex, 0, 4)] + "</b> : " + effetAuRang;
            Agence();
            Color c = catalogue ? catalogue.Couleur(prop) : Color.white;
            foreach (var g in teintes) if (g) g.color = new Color(c.r, c.g, c.b, g.color.a);
            Place(ancre);
            _cible = 1;
            if (!gameObject.activeSelf) gameObject.SetActive(true);
        }

        [Tooltip("Haut de l'effet, écart entre l'effet et l'effet au rang, marge du bas (pixels de la boîte).")]
        public float hautEffet = 152f, ecartTextes = 12f, margeBas = 30f, hauteurMin = 220f;

        /// <summary>La boîte prend la hauteur de ses textes (effets longs sur 3 ou 4 lignes, effets courts sans vide).</summary>
        private void Agence()
        {
            if (boite == null || effet == null || effetRang == null) return;
            Canvas.ForceUpdateCanvases();
            float he = string.IsNullOrEmpty(effet.text) ? 0f : effet.preferredHeight;
            float hr = string.IsNullOrEmpty(effetRang.text) ? 0f : effetRang.preferredHeight;
            var re = effet.rectTransform; re.sizeDelta = new Vector2(re.sizeDelta.x, he);
            re.anchoredPosition = new Vector2(re.anchoredPosition.x, -hautEffet);
            var rr = effetRang.rectTransform; rr.sizeDelta = new Vector2(rr.sizeDelta.x, hr);
            rr.anchoredPosition = new Vector2(rr.anchoredPosition.x, -(hautEffet + he + (he > 0 ? ecartTextes : 0f)));
            float h = Mathf.Max(hauteurMin, hautEffet + he + ecartTextes + hr + margeBas);
            boite.sizeDelta = new Vector2(boite.sizeDelta.x, h);
        }

        public void Cache() => _cible = 0;

        /// <summary>Fixe l'opacité tout de suite (captures).</summary>
        public void Immediat() { if (groupe) groupe.alpha = _cible; }

        private void Place(RectTransform ancre)
        {
            if (boite == null || ancre == null) return;
            var canvas = boite.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var cr = (RectTransform)canvas.rootCanvas.transform;
            Canvas.ForceUpdateCanvases();
            var coins = new Vector3[4];
            ancre.GetWorldCorners(coins);
            // tout se calcule dans l'espace du canvas racine : l'infobulle peut vivre sous n'importe quel parent
            Vector2 bg = cr.InverseTransformPoint(coins[0]), hd = cr.InverseTransformPoint(coins[2]);
            Rect r = cr.rect;
            float k = cr.lossyScale.x > 0 ? boite.lossyScale.x / cr.lossyScale.x : 1f;
            float w = boite.rect.width * k, h = boite.rect.height * k;
            float x = hd.x + ecart;
            if (x + w > r.xMax - 8) x = bg.x - ecart - w;                 // pas la place à droite : à gauche
            float y = Mathf.Clamp((bg.y + hd.y) / 2f - h / 2f, r.yMin + 8, r.yMax - 8 - h);
            boite.pivot = Vector2.zero;
            boite.position = cr.TransformPoint(new Vector3(x, y, 0f));
        }

        private void Update()
        {
            if (!groupe) return;
            float pas = fondu > 0 ? Time.unscaledDeltaTime / fondu : 1;
            groupe.alpha = Mathf.MoveTowards(groupe.alpha, _cible, pas);
        }
    }
}
