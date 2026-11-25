using UnityEngine;
using UnityEngine.EventSystems;

namespace ItHunts.Art.UIJeu
{
    /// <summary>Survol (souris) ou sélection (manette, EventSystem) d'une carte d'offre : focus.</summary>
    public sealed class SurvolCarte : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public PanneauOffres panneau;
        public int index;
        public void OnPointerEnter(PointerEventData e) { if (panneau) panneau.Focus(index); }
        public void OnPointerExit(PointerEventData e) { if (panneau) panneau.Focus(-1); }
        public void OnSelect(BaseEventData e) { if (panneau) panneau.Focus(index); }
        public void OnDeselect(BaseEventData e) { if (panneau) panneau.Focus(-1); }
    }
}
