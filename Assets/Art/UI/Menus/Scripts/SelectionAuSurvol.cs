using UnityEngine;
using UnityEngine.EventSystems;

namespace ItHunts.Art.Menus
{
    /// <summary>La souris qui survole un élément le sélectionne (même état que la manette ou le clavier).</summary>
    public sealed class SelectionAuSurvol : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(gameObject);
        }
    }
}
