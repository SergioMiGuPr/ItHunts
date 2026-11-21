using ItHunts.Art.Menus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Outils communs aux vues des menus de la DA : composants d'aperçu coupés (la logique du jeu pilote), teintes des boutons
    /// (UI-HANDOFF § 2.1 : lues × 2 par le shader ItHunts/UI/Bouton), survol et clic à la souris.
    /// </summary>
    public static class MenuArtKit
    {
        public enum State : byte { Normal, Hover, Pressed, Disabled }

        public static readonly Color Normal = new Color(0.5f, 0.5f, 0.5f, 1f);
        public static readonly Color Hover = new Color(0.60f, 0.575f, 0.55f, 1f);
        public static readonly Color Pressed = new Color(0.40f, 0.38f, 0.36f, 1f);
        public static readonly Color Disabled = new Color(0.30f, 0.29f, 0.31f, 0.85f);

        public static Color ColorOf(State s) => s switch { State.Hover => Hover, State.Pressed => Pressed, State.Disabled => Disabled, _ => Normal };

        /// <summary>
        /// Écran de la DA instancié pour le jeu : les composants d'aperçu qui lisent eux-mêmes le clavier, la manette ou
        /// l'EventSystem sont coupés (leurs méthodes restent appelables) ; les boutons ne changent plus de teinte seuls.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Transform parent, string name)
        {
            if (prefab == null)
                return null;
            GameObject go = Object.Instantiate(prefab, parent, false);
            go.name = name;
            foreach (Selectable s in go.GetComponentsInChildren<Selectable>(true))
            {
                s.transition = Selectable.Transition.None;
                s.navigation = new Navigation { mode = Navigation.Mode.None };
            }
            foreach (OptionDefilante o in go.GetComponentsInChildren<OptionDefilante>(true))
                o.enabled = false;
            foreach (TeinteGroupe t in go.GetComponentsInChildren<TeinteGroupe>(true))
                t.enabled = false;
            foreach (SelectionAuSurvol s in go.GetComponentsInChildren<SelectionAuSurvol>(true))
                s.enabled = false;
            foreach (TitreApercu t in go.GetComponentsInChildren<TitreApercu>(true))
                t.enabled = false;
            foreach (CampementApercu c in go.GetComponentsInChildren<CampementApercu>(true))
                c.enabled = false;
            foreach (ChoixPersoApercu c in go.GetComponentsInChildren<ChoixPersoApercu>(true))
                c.enabled = false;
            foreach (MenuFige f in go.GetComponentsInChildren<MenuFige>(true))
                f.enabled = false;
            return go;
        }

        /// <summary>Morceaux teintés d'un bouton : le groupe (cartouche long en 5 morceaux) ou le graphisme du bouton.</summary>
        public static Graphic[] Targets(Selectable s)
        {
            if (s == null)
                return new Graphic[0];
            var group = s.GetComponent<TeinteGroupe>();
            if (group != null && group.cibles != null && group.cibles.Length > 0)
                return group.cibles;
            return s.targetGraphic != null ? new[] { s.targetGraphic } : new Graphic[0];
        }

        public static void Tint(Graphic[] targets, State state, bool instant = false)
        {
            Color c = ColorOf(state);
            foreach (Graphic g in targets)
                if (g != null)
                    g.CrossFadeColor(c, instant || !Application.isPlaying ? 0f : 0.08f, true, true);
        }

        /// <summary>La souris est-elle sur ce rect (canvas en Screen Space - Camera ou Overlay) ?</summary>
        /// <summary>Souris ignorée (captures comparées aux maquettes : le pointeur posé sur la vue Game ne déplace pas le focus).</summary>
        public static bool IgnoreMouse { get; set; }

        /// <summary>Effets gelés pour les captures (rendu = maquette), en plus de l'option « Réduire les animations ».</summary>
        public static bool FreezeForCapture { get; set; }

        /// <summary>Gel des effets de la DA (shaders ItHunts/UI/*) : option du joueur ou capture.</summary>
        public static void ApplyFreeze() => MenuFige.Regle(ItHunts.Level.GameSettings.ReduceMotion || FreezeForCapture);

        public static bool Over(RectTransform r, Camera cam)
        {
            if (IgnoreMouse || Mouse.current == null || r == null || !r.gameObject.activeInHierarchy)
                return false;
            Canvas c = r.GetComponentInParent<Canvas>();
            Camera use = c != null && c.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
            return RectTransformUtility.RectangleContainsScreenPoint(r, Mouse.current.position.ReadValue(), use);
        }

        /// <summary>Position de la souris dans le rect (0 à 1 en x), pour le sélecteur ‹ ACTE ›.</summary>
        public static float LocalX(RectTransform r, Camera cam)
        {
            Canvas c = r.GetComponentInParent<Canvas>();
            Camera use = c != null && c.renderMode == RenderMode.ScreenSpaceOverlay ? null : cam;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(r, Mouse.current.position.ReadValue(), use, out Vector2 p);
            return Mathf.InverseLerp(r.rect.xMin, r.rect.xMax, p.x);
        }

        public static bool MouseMoved => Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f;
        public static bool Clicked => !IgnoreMouse && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool MouseDown => Mouse.current != null && Mouse.current.leftButton.isPressed;

        /// <summary>
        /// Lignes de l'écran Paramètres de la DA (Option_0…) pour ces libellés : au-delà des lignes de la maquette, la dernière est
        /// reproduite ; l'ensemble se resserre (pas et hauteur) pour tenir avec « Retour » dans le cadre, agrandi d'au plus maxGrow px.
        /// </summary>
        public static System.Collections.Generic.List<(RectTransform rect, Text value, Graphic[] tint)> SettingsRows(GameObject screen,
            System.Collections.Generic.IReadOnlyList<string> labels, float maxGrow = 40f)
        {
            var rows = new System.Collections.Generic.List<(RectTransform, Text, Graphic[])>();
            Transform t = screen.transform;
            int have = 0;
            while (t.Find("Option_" + have) != null)
                have++;
            if (have == 0)
                return rows;
            var first = (RectTransform)t.Find("Option_0");
            var lastOriginal = (RectTransform)t.Find("Option_" + (have - 1));
            float step = have > 1 ? ((RectTransform)t.Find("Option_1")).anchoredPosition.y - first.anchoredPosition.y : -80f;
            var back = t.Find("Retour") as RectTransform;
            float backGap = back != null ? back.anchoredPosition.y - lastOriginal.anchoredPosition.y : step;
            for (int i = have; i < labels.Count; i++)
            {
                var prev = (RectTransform)t.Find("Option_" + (i - 1));
                RectTransform copy = Object.Instantiate(prev, prev.parent, false);
                copy.name = "Option_" + i;
            }

            int n = Mathf.Max(have, labels.Count);
            if (n > have)
            {
                // Même hauteur totale (+ maxGrow) répartie sur n lignes et « Retour ».
                float span = (have - 1) * step + backGap - Mathf.Sign(step) * maxGrow;
                float newStep = span / (n - 1 + backGap / step);
                float rowHeight = first.rect.height;
                float scale = Mathf.Min(1f, (Mathf.Abs(newStep) - 4f) / Mathf.Max(1f, rowHeight));
                Vector2 p0 = first.anchoredPosition;                       // avant de déplacer la première ligne
                float width = first.rect.width;
                for (int i = 0; i < n; i++)
                {
                    var r = (RectTransform)t.Find("Option_" + i);
                    r.anchoredPosition = new Vector2(p0.x + width * (1f - scale) * 0.5f, p0.y + i * newStep);
                    r.localScale = new Vector3(scale, scale, 1f);
                }

                if (back != null)
                    back.anchoredPosition = new Vector2(back.anchoredPosition.x, p0.y + (n - 1) * newStep + backGap * (newStep / step));
                var frame = t.Find("Cadre") as RectTransform;
                if (frame != null)
                    frame.sizeDelta += new Vector2(0f, maxGrow);
            }

            for (int i = 0; i < labels.Count; i++)
            {
                Transform row = t.Find("Option_" + i);
                var label = row.Find("Libelle")?.GetComponent<Text>();
                if (label != null)
                    label.text = labels[i];
                rows.Add(((RectTransform)row, row.Find("Valeur")?.GetComponent<Text>(), Targets(row.GetComponent<Selectable>())));
            }

            return rows;
        }

        /// <summary>Enfant par chemin (« Interface/Pret »), composant demandé.</summary>
        public static T Find<T>(GameObject root, string path) where T : Component
        {
            if (root == null)
                return null;
            Transform t = root.transform.Find(path);
            return t != null ? t.GetComponent<T>() : null;
        }

        /// <summary>Texte simple posé par le code (messages, liens de développement), pas un dessin.</summary>
        public static Text Label(Transform parent, string name, Font font, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return t;
        }
    }
}
