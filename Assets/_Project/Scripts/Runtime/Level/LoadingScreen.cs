using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Écran de chargement simple (greybox) pendant la construction de la carte aléatoire : fond noir, titre, étape en cours et barre
    /// de progression, au lieu d'une image figée. Sa propre caméra (les caméras des joueurs ne sont pas encore actives) ; détruit
    /// quand la carte est prête. À habiller à l'étape 10 (DA).
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        public RandomLevel Level;

        private GUIStyle _title, _text;
        private Texture2D _white;

        private void Awake()
        {
            var cam = gameObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            cam.cullingMask = 0;
            cam.depth = 100f;
            _white = Texture2D.whiteTexture;
        }

        private void OnGUI()
        {
            if (Level == null)
                return;
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _title.fontSize = Mathf.RoundToInt(48 * s);
            _text.fontSize = Mathf.RoundToInt(24 * s);
            _title.normal.textColor = new Color(0.85f, 0.8f, 0.7f);
            _text.normal.textColor = new Color(0.65f, 0.62f, 0.58f);
            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0f, h * 0.36f, w, 70f * s), "Les caves du château", _title);
            GUI.Label(new Rect(0f, h * 0.45f, w, 40f * s), Level.Stage, _text);
            var bar = new Rect(w * 0.3f, h * 0.53f, w * 0.4f, 14f * s);
            GUI.color = new Color(0.15f, 0.14f, 0.16f);
            GUI.DrawTexture(bar, _white);
            GUI.color = new Color(0.9f, 0.55f, 0.25f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(Level.Progress), bar.height), _white);
            GUI.color = Color.white;
            GUI.Label(new Rect(0f, h * 0.56f, w, 40f * s), $"{Mathf.RoundToInt(Level.Progress * 100f)} %", _text);
        }
    }
}
