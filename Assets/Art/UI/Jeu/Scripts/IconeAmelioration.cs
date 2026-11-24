using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Illustration d'une amélioration dans son cadre (nuit 4, lot 6 ; nuit 5, lot 2) : HUD, infobulle, coffre, marchand, écran de
    /// fin, codex. Amélioration : icône à 80 % sous un cadre **carré** ; passif : icône à 62 % découpée en losange sous un cadre
    /// **losange**. Couleur du cadre = personnage propriétaire (générique : fer) ; **rang = étoiles** (1 à 5, Bronze à Mythique),
    /// posées à cheval sur le bas du cadre. Sans étoiles ni cadres de personnage dans le catalogue : anciens cadres de rang.
    /// </summary>
    public sealed class IconeAmelioration : MonoBehaviour
    {
        public Image icone, cadre;
        [Tooltip("Masque losange des passifs (Image + Mask, parent de l'icône).")]
        public Mask masque;
        public RectTransform zoneIcone;
        public CatalogueAmeliorations catalogue;
        [Tooltip("Cinq étoiles de rang (enfants), de gauche à droite.")]
        public Image[] etoiles = new Image[0];
        [Tooltip("Montrer les emplacements éteints (codex, fiche) ; sinon seules les étoiles gagnées, centrées (HUD, cartes).")]
        public bool etoilesVides;
        [Tooltip("Taille d'une étoile et pas entre deux, en fraction de la case.")]
        public float tailleEtoile = 0.24f, pasEtoile = 0.17f;

        public string Id { get; private set; }
        public int Rang { get; private set; }
        private int _nEtoiles;

        /// <summary>Par identifiant de AMELIORATIONS.md (ex. « G12 », « R-P1 »), rang 0 (Bronze) à 4 (Mythique).</summary>
        public void Configure(string id, int rang)
        {
            Id = id;
            if (catalogue == null) { Vide(); return; }
            var sp = catalogue.Icone(id);
            if (sp == null) Debug.LogWarning("IconeAmelioration : identifiant inconnu (" + id + "), voir Catalogue_Ameliorations", this);
            Configure(sp, rang, catalogue.EstPassif(id), CatalogueAmeliorations.Proprietaire(id));
        }

        /// <summary>Ancienne signature : propriétaire générique.</summary>
        public void Configure(Sprite illustration, int rang, bool passif) => Configure(illustration, rang, passif, 0);

        /// <summary>proprietaire : 0 générique, 1 Renard, 2 Caméléon, 3 Koala, 4 Hibou (UpgradeCharacter de main).</summary>
        public void Configure(Sprite illustration, int rang, bool passif, int proprietaire)
        {
            rang = Mathf.Clamp(rang, 0, 4);
            Rang = rang;
            if (icone) { icone.sprite = illustration; icone.enabled = illustration != null; }
            bool couleurs = catalogue && catalogue.etoile != null && catalogue.CadrePersonnage(proprietaire, passif) != null;
            if (cadre && catalogue)
            {
                if (couleurs) cadre.sprite = catalogue.CadrePersonnage(proprietaire, passif);
                else
                {
                    var jeu = passif ? catalogue.cadresPassifs : catalogue.cadres;
                    cadre.sprite = jeu != null && jeu.Length > 0 ? jeu[Mathf.Min(rang, jeu.Length - 1)] : null;
                }
                cadre.enabled = cadre.sprite != null;
            }
            if (masque) masque.enabled = passif;
            var m = masque ? masque.GetComponent<Image>() : null;
            if (m) m.enabled = passif;
            if (zoneIcone)
            {
                float f = passif ? 0.62f : 0.80f;
                zoneIcone.anchorMin = new Vector2(0.5f - f / 2, 0.5f - f / 2);
                zoneIcone.anchorMax = new Vector2(0.5f + f / 2, 0.5f + f / 2);
                zoneIcone.offsetMin = zoneIcone.offsetMax = Vector2.zero;
            }
            PoseEtoiles(couleurs ? rang + 1 : 0);
            gameObject.SetActive(true);
        }

        /// <summary>Étoiles : n gagnées (0 = aucune) ; centrées sur le bas de la case.</summary>
        public void PoseEtoiles(int n)
        {
            _nEtoiles = n;
            if (etoiles == null) return;
            int vis = etoilesVides && n > 0 ? etoiles.Length : n;
            for (int i = 0; i < etoiles.Length; i++)
            {
                var e = etoiles[i];
                if (e == null) continue;
                bool on = i < vis;
                e.gameObject.SetActive(on);
                if (!on) continue;
                e.sprite = i < n || catalogue == null ? (catalogue ? catalogue.etoile : e.sprite) : catalogue.etoileVide;
                var rt = e.rectTransform;
                float x = 0.5f + (i - (vis - 1) / 2f) * pasEtoile;
                rt.anchorMin = rt.anchorMax = new Vector2(x, tailleEtoile * 0.45f - 0.04f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                var parent = (RectTransform)transform;
                float t = parent.rect.height > 0 ? parent.rect.height * tailleEtoile : 12f;
                rt.sizeDelta = new Vector2(t, t);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled && _nEtoiles > 0) PoseEtoiles(_nEtoiles);
        }

        public void Vide() => gameObject.SetActive(false);
    }
}
