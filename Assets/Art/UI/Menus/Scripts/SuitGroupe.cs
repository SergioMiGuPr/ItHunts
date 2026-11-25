using UnityEngine;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Campement (nuit 5) : les ombres et la lumière de lanterne d'une place vivent dans le calque commun Sol_Lumieres_Ombres,
    /// hors du CanvasGroup de leur place. Ce composant recopie l'opacité du groupe de la place : pendant une apparition (fondu de
    /// la place, ici ou dans MainMenuArt de main), l'ombre apparaît avec le personnage au lieu d'être pleine d'emblée.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SuitGroupe : MonoBehaviour
    {
        public CanvasGroup source;
        private CanvasGroup _moi;

        private void LateUpdate()
        {
            if (source == null)
                return;
            if (_moi == null)
                _moi = GetComponent<CanvasGroup>();
            if (!Mathf.Approximately(_moi.alpha, source.alpha))
                _moi.alpha = source.alpha;
        }
    }
}
