using System;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItHunts.Economy
{
    /// <summary>
    /// Texte en monde (nom, icône, prix) qui fait face à la caméra (D-043) ; lisible à 10 m. Greybox : TextMesh et police
    /// intégrée. Par caméra (écran partagé, coffres par joueur) : tourné vers chaque caméra, et son texte comme sa visibilité
    /// peuvent dépendre du joueur qui regarde (TextFor, VisibleFor ; caméra sans joueur : le texte de base). Un parent
    /// ViewerOnly le réserve à un joueur.
    /// </summary>
    public sealed class WorldLabel : MonoBehaviour
    {
        private TextMesh _text;
        private MeshRenderer _renderer;
        private ViewerOnly _only;
        private string _base = string.Empty;

        /// <summary>Texte selon le joueur qui regarde (null : le texte de base).</summary>
        public Func<PlayerHealth, string> TextFor;
        /// <summary>Visible pour ce joueur ? (null : oui).</summary>
        public Func<PlayerHealth, bool> VisibleFor;

        public static WorldLabel Create(Transform parent, Vector3 localPosition, string text, Color color, float height = 0.28f)
        {
            var go = new GameObject("Texte");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var label = go.AddComponent<WorldLabel>();
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = height / 6.4f;                            // hauteur d'une ligne ≈ height (m)
            tm.color = color;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }

            label._text = tm;
            label._renderer = go.GetComponent<MeshRenderer>();
            label._base = text;
            return label;
        }

        /// <summary>Texte de base (vu par une caméra sans joueur, ou quand TextFor est absent).</summary>
        public string Text
        {
            get => _base;
            set
            {
                _base = value ?? string.Empty;
                if (_text != null)
                    _text.text = _base;
            }
        }

        public Color Color
        {
            set
            {
                if (_text != null)
                    _text.color = value;
            }
        }

        private void OnEnable()
        {
            _only = GetComponentInParent<ViewerOnly>();
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
        }

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= BeforeCamera;

        /// <summary>Avant chaque caméra : face à elle, texte et visibilité pour son joueur.</summary>
        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null || _text == null)
                return;
            transform.rotation = cam.transform.rotation;                  // dans le plan de l'écran : jamais penché
            PlayerHealth viewer = Interactables.PlayerOfCamera(cam);
            bool visible = (_only == null || _only.Allows(viewer)) && (viewer == null || VisibleFor == null || VisibleFor(viewer));
            if (_renderer != null)
                _renderer.enabled = visible;
            string t = viewer != null && TextFor != null ? TextFor(viewer) : _base;
            if (visible && _text.text != t)
                _text.text = t;
        }
    }
}
