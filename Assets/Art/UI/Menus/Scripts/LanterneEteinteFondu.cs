using UnityEngine;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Campement (nuit 5) : quand un joueur arrive, la lanterne éteinte de sa place s'efface pendant que le personnage et sa
    /// lanterne allumée apparaissent (fondu de la place, ici ou dans MainMenuArt de main), au lieu de disparaître d'un coup.
    /// Posé sur la racine de la place (toujours active) ; pilote un CanvasGroup sur la lanterne éteinte.
    /// </summary>
    public sealed class LanterneEteinteFondu : MonoBehaviour
    {
        public CampementApercu campement;
        public int place;
        public CanvasGroup groupePlace;
        public GameObject eteinte;
        private CanvasGroup _g;

        private void LateUpdate()
        {
            if (campement == null || eteinte == null || groupePlace == null || place < 0 || place >= campement.places.Length)
                return;
            if (_g == null && (_g = eteinte.GetComponent<CanvasGroup>()) == null)
                _g = eteinte.AddComponent<CanvasGroup>();
            bool occupee = campement.places[place].occupant >= 0;
            if (!occupee)
            {
                _g.alpha = 1f;                                  // place libre : Libere l'a déjà rendue visible
                return;
            }
            bool fondu = groupePlace.alpha < 0.999f;
            if (eteinte.activeSelf != fondu)
                eteinte.SetActive(fondu);
            if (fondu)
                _g.alpha = 1f - groupePlace.alpha;
        }
    }
}
