using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Marqueur de critique (headshot, nuit du 2026-11-22) : autour du réticule, quatre éclats en croix (l'étoile du réticule,
    /// étirée) qui jaillissent et s'écartent, un anneau or qui s'élargit, une lueur, et « Critique » en petites capitales
    /// sous le viseur ; dure 0.45 s, sans masquer le centre. Le jeu appelle Affiche() à chaque tir critique.
    /// </summary>
    public sealed class MarqueurCritique : MonoBehaviour
    {
        public RectTransform[] eclats;
        public Image anneau, lueur;
        public Text texte;
        public float duree = 0.45f;
        public Color couleur = new Color(1f, 0.82f, 0.35f);

        private float _t = 1f;

        public void Affiche() => _t = 0f;

        /// <summary>Pose l'animation à un instant donné (0..1), pour les captures.</summary>
        public void Pose(float t01) { _t = t01; Applique(); }

        private void Update()
        {
            if (_t >= 1f) return;
            _t = Mathf.Min(1f, _t + Time.unscaledDeltaTime / duree);
            Applique();
        }

        private void Applique()
        {
            float e = 1f - Mathf.Pow(1f - _t, 3f);              // sortie rapide
            float a = _t < 0.15f ? 1f : 1f - (_t - 0.15f) / 0.85f;
            for (int i = 0; i < eclats.Length; i++)
            {
                var r = eclats[i];
                float ang = i * 90f + 45f;
                var d = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                r.anchoredPosition = d * Mathf.Lerp(18f, 42f, e);
                r.localScale = new Vector3(Mathf.Lerp(1.4f, 0.8f, e), Mathf.Lerp(0.5f, 0.35f, e), 1f);
                r.localRotation = Quaternion.Euler(0, 0, ang);
                r.GetComponent<Image>().color = new Color(couleur.r, couleur.g, couleur.b, a);
            }
            anneau.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.3f, e);
            anneau.color = new Color(couleur.r, couleur.g, couleur.b, a * 0.8f);
            lueur.color = new Color(1f, 0.6f, 0.25f, a * 0.5f);
            texte.color = new Color(couleur.r, couleur.g, couleur.b, a);
            texte.rectTransform.anchoredPosition = new Vector2(0, -46f - 6f * e);
        }
    }
}
