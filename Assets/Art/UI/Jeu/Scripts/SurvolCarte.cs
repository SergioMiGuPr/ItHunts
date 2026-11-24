using UnityEngine;
using UnityEngine.EventSystems;

namespace ItHunts.Art.UIJeu
{
    /// <summary>Survol d'une carte d'offre (souris) : focus.</summary>
    public sealed class SurvolCarte : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public PanneauOffres panneau;
        public int index;
        public void OnPointerEnter(PointerEventData e) { if (panneau) panneau.Focus(index); }
        public void OnPointerExit(PointerEventData e) { if (panneau) panneau.Focus(-1); }
    }
}
