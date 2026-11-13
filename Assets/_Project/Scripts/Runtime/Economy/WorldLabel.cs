using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Texte en monde (nom, icône, prix) qui fait face à la caméra (D-043) ; lisible à 10 m. Greybox : TextMesh et police
    /// intégrée. Écran partagé (étape 9) : à tourner vers chaque caméra.
    /// </summary>
    public sealed class WorldLabel : MonoBehaviour
    {
        private TextMesh _text;

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
            return label;
        }

        public string Text
        {
            get => _text != null ? _text.text : string.Empty;
            set
            {
                if (_text != null)
                    _text.text = value;
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

        private static Camera _cam;

        /// <summary>Caméra d'un joueur (elles ne sont pas marquées MainCamera, pour l'écran partagé) ; à défaut, la principale.</summary>
        private static Camera ViewCamera()
        {
            if (_cam != null && _cam.isActiveAndEnabled)
                return _cam;
            _cam = null;
            foreach (ItHunts.Player.PlayerHealth p in ItHunts.Player.PlayerHealth.All)
            {
                _cam = Interactables.CameraOf(p);
                if (_cam != null)
                    break;
            }

            return _cam != null ? _cam : _cam = Camera.main;
        }

        private void LateUpdate()
        {
            Camera cam = ViewCamera();
            if (cam == null)
                return;
            transform.rotation = cam.transform.rotation;                  // dans le plan de l'écran : jamais penché
        }
    }
}
