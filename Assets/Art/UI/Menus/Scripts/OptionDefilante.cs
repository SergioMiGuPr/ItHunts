using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Ligne de réglage des paramètres (aperçu DA) : cartouche découpé + libellé à gauche + valeur « ‹ 80 % › » à droite,
    /// tout en texte Unity (aucune glissière dessinée). Gauche / droite (ou clic) font défiler les valeurs. La logique de
    /// réglage réelle (main) lit Index ou s'abonne à Change.
    /// </summary>
    public sealed class OptionDefilante : Selectable, IPointerClickHandler, ISubmitHandler
    {
        public Text valeur;
        public string[] valeurs = { "0 %", "20 %", "40 %", "60 %", "80 %", "100 %" };
        public int index = 4;
        public event Action<int> Change;

        public int Index => index;

        protected override void OnEnable()
        {
            base.OnEnable();
            Affiche();
        }

        private void Affiche()
        {
            if (valeur != null && valeurs.Length > 0)
                valeur.text = "‹  " + valeurs[Mathf.Clamp(index, 0, valeurs.Length - 1)] + "  ›";
        }

        private void Pas(int d)
        {
            if (valeurs.Length == 0) return;
            index = (index + d + valeurs.Length) % valeurs.Length;
            Affiche();
            Change?.Invoke(index);
        }

        public override void OnMove(AxisEventData e)
        {
            if (e.moveDir == MoveDirection.Left) { Pas(-1); return; }
            if (e.moveDir == MoveDirection.Right) { Pas(1); return; }
            base.OnMove(e);
        }

        public void OnPointerClick(PointerEventData e) => Pas(e.button == PointerEventData.InputButton.Right ? -1 : 1);

        public void OnSubmit(BaseEventData e) => Pas(1);
    }
}

