using UnityEngine;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Racine d'un écran de menu : un rect de la taille de la maquette (en pixels de la maquette, coin haut gauche à
    /// l'origine), mis à l'échelle pour couvrir le canvas (comme un fond « envelope »). Tous les éléments enfants sont posés
    /// en pixels de la maquette, aux positions mesurées : l'écran reste identique à la maquette quelle que soit la
    /// définition. Aperçu de la DA : la logique de jeu (main) peut garder ce composant et brancher ses propres scripts.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class MenuEcran : MonoBehaviour
    {
        [Tooltip("Taille de la maquette en pixels (base de l'écran).")]
        public Vector2 taille = new Vector2(1672, 941);
        [Tooltip("Couvrir (true : rogne les bords si le rapport diffère) ou contenir (false : bandes).")]
        public bool couvrir = true;

        private RectTransform _rt;

        private void OnEnable() => Ajuste();

        private void LateUpdate() => Ajuste();

        public void Ajuste()
        {
            if (_rt == null)
                _rt = (RectTransform)transform;
            var parent = _rt.parent as RectTransform;
            if (parent == null)
                return;
            Vector2 p = parent.rect.size;
            if (p.x <= 0 || p.y <= 0)
                return;
            float sx = p.x / taille.x, sy = p.y / taille.y;
            float s = couvrir ? Mathf.Max(sx, sy) : Mathf.Min(sx, sy);
            _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
            _rt.pivot = new Vector2(0.5f, 0.5f);
            _rt.sizeDelta = taille;
            _rt.anchoredPosition = Vector2.zero;
            _rt.localScale = new Vector3(s, s, 1);
        }

        /// <summary>Pose un enfant en pixels de la maquette (x, y depuis le coin haut gauche ; w, h).</summary>
        public static void Pose(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }
}
