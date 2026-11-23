using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Illustration d'une amélioration dans son cadre de rang (nuit 4, lot 6) : HUD (rangée au-dessus de la barre de vie), écran
    /// de fin, marchand. Amélioration : icône à 80 % sous un cadre carré ; passif : icône à 62 % découpée en losange (masque)
    /// sous un cadre losange. Rangs 0 à 4 : Bronze, Argent, Or, Diamant, Mythique (gemmes 1 à 5 sur le cadre).
    /// </summary>
    public sealed class IconeAmelioration : MonoBehaviour
    {
        public Image icone, cadre;
        [Tooltip("Masque losange des passifs (Image + Mask, parent de l'icône).")]
        public Mask masque;
        public RectTransform zoneIcone;
        public CatalogueAmeliorations catalogue;

        /// <summary>Par identifiant de AMELIORATIONS.md (ex. « G12 », « R-P1 »), rang 0 à 4.</summary>
        public void Configure(string id, int rang)
        {
            if (catalogue == null) return;
            Configure(catalogue.Icone(id), rang, catalogue.EstPassif(id));
        }

        public void Configure(Sprite illustration, int rang, bool passif)
        {
            rang = Mathf.Clamp(rang, 0, 4);
            if (icone) { icone.sprite = illustration; icone.enabled = illustration != null; }
            if (cadre && catalogue) cadre.sprite = passif ? catalogue.cadresPassifs[rang] : catalogue.cadres[rang];
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
            gameObject.SetActive(true);
        }

        public void Vide() => gameObject.SetActive(false);
    }
}
