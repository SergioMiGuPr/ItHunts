using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Menu de l'écran titre (aperçu DA) : Jouer / Paramètres / Quitter en texte Unity, aux emplacements de la maquette.
    /// L'élément sélectionné prend le style de « Jouer » sur la maquette (blanc, plus grand, décalé à droite) avec le
    /// curseur flamme et le soulignement découpés de la maquette ; les autres le style de « Settings ».
    /// </summary>
    public sealed class TitreApercu : MonoBehaviour
    {
        [Serializable]
        public sealed class Element
        {
            public Selectable bouton;
            public Text texte;
            [Tooltip("Ligne de base (y, pixels de la maquette).")]
            public float ligneDeBase;
        }

        [Serializable]
        public sealed class Style
        {
            public int taille = 52;
            public Color couleur = Color.white;
            [Tooltip("Bord gauche du texte (x, pixels de la maquette).")]
            public float x;
            [Tooltip("Étirement horizontal (le texte peint est un peu plus large que l'EB Garamond).")]
            public float etirement = 1f;
            [Tooltip("Décalage de la ligne de base dans le rect du texte (calibré par capture).")]
            public float baseDansRect;
        }

        public Element[] elements = Array.Empty<Element>();
        public Style selectionne = new Style();
        public Style normal = new Style();
        public RectTransform curseur;
        public RectTransform soulignement;
        [Tooltip("Curseur : décalage (x, y) de son coin haut gauche par rapport au début du texte et à la ligne de base.")]
        public Vector2 decalageCurseur = new Vector2(-61, -66);
        [Tooltip("Soulignement : décalage (x, y) de son coin haut gauche par rapport au début du texte et à la ligne de base.")]
        public Vector2 decalageSoulignement = new Vector2(-43, 14);

        private int _courant = -1;

        private void OnEnable()
        {
            if (elements.Length > 0 && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null)
                EventSystem.current.SetSelectedGameObject(elements[0].bouton.gameObject);
            Applique(Courant());
        }

        private int Courant()
        {
            GameObject sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < elements.Length; i++)
                if (elements[i].bouton != null && elements[i].bouton.gameObject == sel)
                    return i;
            return _courant >= 0 ? _courant : 0;
        }

        private void Update()
        {
            int c = Courant();
            if (c != _courant)
                Applique(c);
        }

        /// <summary>Met en forme les éléments (sélectionné = index).</summary>
        public void Applique(int index)
        {
            _courant = index;
            for (int i = 0; i < elements.Length; i++)
            {
                Element e = elements[i];
                if (e.texte == null)
                    continue;
                Style s = i == index ? selectionne : normal;
                e.texte.fontSize = s.taille;
                e.texte.color = s.couleur;
                var rt = e.texte.rectTransform;
                float h = s.taille * 2f;
                MenuEcran.Pose(rt, s.x, e.ligneDeBase - s.baseDansRect, 600, h);
                rt.localScale = new Vector3(s.etirement, 1, 1);
                if (i == index)
                {
                    if (curseur != null)
                        curseur.anchoredPosition = new Vector2(s.x + decalageCurseur.x, -(e.ligneDeBase + decalageCurseur.y));
                    if (soulignement != null)
                        soulignement.anchoredPosition = new Vector2(s.x + decalageSoulignement.x, -(e.ligneDeBase + decalageSoulignement.y));
                }
            }
        }
    }
}
