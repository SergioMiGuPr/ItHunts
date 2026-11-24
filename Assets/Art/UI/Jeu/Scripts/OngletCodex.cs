using UnityEngine;
using UnityEngine.EventSystems;

namespace ItHunts.Art.UIJeu
{
    /// <summary>Onglet du codex sélectionné à la manette (EventSystem) : l'onglet s'ouvre, comme au clic.</summary>
    public sealed class OngletCodex : MonoBehaviour, ISelectHandler
    {
        public CodexAmeliorations codex;
        public int index;
        public void OnSelect(BaseEventData e) { if (codex) codex.ChoisitOnglet(index); }
    }
}
