using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Charges de mouvement (dash et saut en l'air), en bas de l'écran près du centre : une orbe par charge. Pleine : orbe
    /// bleue et sa lueur ; vide : orbe fêlée grise ; en recharge : l'orbe fêlée, et l'orbe bleue qui monte du bas
    /// (remplissage vertical) à demi opaque. Une charge qui revient brille une fois. Le jeu appelle Regle(i, 0..1).
    /// </summary>
    public sealed class ChargesJeu : MonoBehaviour
    {
        [System.Serializable]
        public sealed class Charge
        {
            public Image vide;
            public Image pleine;     // Filled, Vertical, origine Bottom
            public Image lueur;
            [System.NonSerialized] public float valeur = 1f, eclat;
        }

        public Charge[] charges = new Charge[3];
        [Tooltip("Opacité de l'orbe bleue pendant la recharge.")]
        [Range(0f, 1f)] public float alphaRecharge = 0.55f;
        [Tooltip("Lueur d'une charge pleine (alpha) et de l'éclat quand elle revient.")]
        public Vector2 lueurPleineEclat = new Vector2(0.35f, 1f);
        [Tooltip("Durée de l'éclat (s).")]
        public float dureeEclat = 0.35f;
        public Color couleurLueur = new Color(0.36f, 0.72f, 1f, 1f);

        /// <summary>valeur : 1 = pleine, 0 = vide, entre les deux = en recharge.</summary>
        public void Regle(int i, float valeur)
        {
            if (i < 0 || i >= charges.Length || charges[i] == null)
                return;
            Charge c = charges[i];
            valeur = Mathf.Clamp01(valeur);
            if (valeur >= 1f && c.valeur < 1f)
                c.eclat = 1f;
            c.valeur = valeur;
        }

        /// <summary>Les premières charges pleines, la suivante en recharge, les autres vides.</summary>
        public void RegleTout(int pleines, float recharge01)
        {
            for (int i = 0; i < charges.Length; i++)
                Regle(i, i < pleines ? 1f : i == pleines ? recharge01 : 0f);
        }

        /// <summary>Applique l'état tout de suite (captures).</summary>
        public void Rafraichit() => Applique(0f);

        private void Update() => Applique(Time.unscaledDeltaTime);

        private void Applique(float dt)
        {
            foreach (Charge c in charges)
            {
                if (c == null) continue;
                c.eclat = Mathf.Max(0f, c.eclat - dt / Mathf.Max(dureeEclat, 0.01f));
                bool pleine = c.valeur >= 1f;
                if (c.pleine != null)
                {
                    c.pleine.fillAmount = c.valeur;
                    c.pleine.color = new Color(1, 1, 1, pleine ? 1f : alphaRecharge);
                }
                if (c.vide != null)
                    c.vide.enabled = !pleine;
                if (c.lueur != null)
                {
                    float a = (pleine ? lueurPleineEclat.x : 0f) + c.eclat * lueurPleineEclat.y;
                    Color l = couleurLueur; l.a = a;
                    c.lueur.color = l;
                    c.lueur.rectTransform.localScale = Vector3.one * (1f + 0.35f * c.eclat);
                }
            }
        }
    }
}
