using UnityEngine;
using UnityEngine.EventSystems;

namespace ItHunts.Art.UIJeu
{
    /// <summary>Survol ou clic d'une case du codex.</summary>
    public sealed class SurvolCodex : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        public CodexAmeliorations codex;
        public int index;
        public void OnPointerEnter(PointerEventData e) { if (codex) codex.Choisit(index); }
        public void OnPointerClick(PointerEventData e) { if (codex) codex.Choisit(index); }
    }
}
