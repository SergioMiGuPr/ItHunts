using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// États d'un bouton en plusieurs morceaux (cartouche long : bouts, tranches, fleuron) : la même teinte sur tous les
    /// morceaux, lue x2 par le shader ItHunts/UI/Bouton (0,5 = pixels d'origine). Normal, survol / sélection, pressé,
    /// désactivé : teinte et luminosité seulement.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public sealed class TeinteGroupe : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public Graphic[] cibles = new Graphic[0];
        public Color normal = new Color(0.5f, 0.5f, 0.5f, 1);
        public Color survol = new Color(0.60f, 0.575f, 0.55f, 1);
        public Color presse = new Color(0.40f, 0.38f, 0.36f, 1);
        public Color desactive = new Color(0.30f, 0.29f, 0.31f, 0.85f);
        public float fondu = 0.08f;

        private Selectable _s;
        private bool _survol, _presse, _selection;

        private void Awake() => _s = GetComponent<Selectable>();

        private void OnEnable() => Applique(true);

        public void OnPointerEnter(PointerEventData e) { _survol = true; Applique(); }
        public void OnPointerExit(PointerEventData e) { _survol = false; _presse = false; Applique(); }
        public void OnPointerDown(PointerEventData e) { _presse = true; Applique(); }
        public void OnPointerUp(PointerEventData e) { _presse = false; Applique(); }
        public void OnSelect(BaseEventData e) { _selection = true; Applique(); }
        public void OnDeselect(BaseEventData e) { _selection = false; Applique(); }

        private void Update()
        {
            if (_s != null && !_s.IsInteractable())
                Applique();
        }

        private void Applique(bool instant = false)
        {
            Color c = _s != null && !_s.IsInteractable() ? desactive : _presse ? presse : (_survol || _selection) ? survol : normal;
            foreach (Graphic g in cibles)
                if (g != null)
                    g.CrossFadeColor(c, instant || !Application.isPlaying ? 0f : fondu, true, true);
        }
    }
}
