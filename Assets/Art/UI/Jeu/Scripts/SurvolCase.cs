using UnityEngine;
using UnityEngine.EventSystems;

namespace ItHunts.Art.UIJeu
{
    /// <summary>Survol d'une case de la colonne (souris) : relaie à <see cref="ColonneAmeliorations.Survole"/>.</summary>
    public sealed class SurvolCase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public ColonneAmeliorations colonne;
        public int index;
        public void OnPointerEnter(PointerEventData e) { if (colonne) colonne.Survole(index); }
        public void OnPointerExit(PointerEventData e) { if (colonne) colonne.Survole(-1); }
    }
}
