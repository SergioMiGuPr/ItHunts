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
    /// Règle unique de visibilité (corrections des étapes 8 et 9), pour tous les textes : affiché seulement si aucun décor ne se
    /// trouve entre la caméra et lui (ligne de vue, PlayerSight), en plus du test de profondeur du shader. Toujours à l'échelle
    /// du monde : posé sur un objet mis à l'échelle (une porte), il n'en hérite pas (la cause des textes géants et déformés qui
    /// dépassaient des murs) ; il suit cet objet (ancre) et disparaît avec lui.
    /// </summary>
    public sealed class WorldLabel : MonoBehaviour
    {
        private TextMesh _text;
        private MeshRenderer _renderer;
        private ViewerOnly _only;
        private string _base = string.Empty;
        private Transform _anchor;
        private Vector3 _offset;
        private bool _follow;
        private Transform _ignore;
        private Color _color = Color.white;
        private float _alpha = -1f;

        /// <summary>Mesure : dernière opacité appliquée (fondu avec la distance, correctifs-rendu point 5).</summary>
        public float LastAlpha => _alpha;

        /// <summary>Texte selon le joueur qui regarde (null : le texte de base).</summary>
        public Func<PlayerHealth, string> TextFor;
        /// <summary>Visible pour ce joueur ? (null : oui).</summary>
        public Func<PlayerHealth, bool> VisibleFor;

        /// <summary>Mesure : dernier verdict de la ligne de vue (caméra → texte) et caméra concernée.</summary>
        public bool LastLineOfSight { get; private set; } = true;
        public Camera LastCamera { get; private set; }
        public MeshRenderer Renderer => _renderer;
        /// <summary>Colliders qui ne cachent pas ce texte (l'objet qui le porte), null pour une porte.</summary>
        public Transform IgnoreRoot => _ignore;
        /// <summary>Hauteur d'une ligne (m).</summary>
        public float LineHeight => _text != null ? _text.characterSize * 6.4f : 0.28f;
        /// <summary>Ce joueur peut-il voir ce texte (ViewerOnly, VisibleFor), ligne de vue mise à part ?</summary>
        public bool AllowedFor(PlayerHealth viewer) =>
            (_only == null || _only.Allows(viewer)) && (viewer == null || VisibleFor == null || VisibleFor(viewer));

        /// <summary>
        /// Texte posé à localPosition (en mètres, dans le repère tourné du parent, sans son échelle). Les colliders sous
        /// ignoreRoot ne cachent pas le texte (l'objet qui le porte ; par défaut le parent). ownerBlocks : le parent cache le
        /// texte comme un décor (une porte cache le texte posé sur son autre face).
        /// </summary>
        public static WorldLabel Create(Transform parent, Vector3 localPosition, string text, Color color, float height = 0.28f,
            Transform ignoreRoot = null, bool ownerBlocks = false)
        {
            var go = new GameObject("Texte");
            // Parent : l'ancêtre le plus proche à l'échelle uniforme (une porte, boîte étirée, n'en est pas un). Posé avant le
            // composant : son OnEnable cherche un ViewerOnly parmi les parents.
            Transform host = parent;
            while (host != null && !Uniform(host.lossyScale))
                host = host.parent;
            go.transform.SetParent(host, false);
            float s = host != null ? host.lossyScale.x : 1f;
            go.transform.localScale = Vector3.one / Mathf.Max(0.0001f, s);
            var label = go.AddComponent<WorldLabel>();
            label._anchor = parent;
            label._offset = localPosition;
            label._ignore = ownerBlocks ? null : ignoreRoot != null ? ignoreRoot : parent;
            label._follow = host != parent;
            label.Place();
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = height / 6.4f;                            // hauteur d'une ligne ≈ height (m)
            tm.color = color;
            label._color = color;
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = TextMaterial(font);
            }

            label._text = tm;
            label._renderer = go.GetComponent<MeshRenderer>();
            label._renderer.shadowCastingMode = ShadowCastingMode.Off;
            label._base = text;
            return label;
        }

        /// <summary>
        /// Deux textes, un devant chaque grande face d'un bloc (porte), à 0.12 m : le bloc cache celui de l'autre face. Le bloc
        /// est un cube unité mis à l'échelle (greybox) ou porte un BoxCollider.
        /// </summary>
        public static WorldLabel[] CreateOnFaces(Transform block, string text, Color color, float height = 0.28f)
        {
            Vector3 size = block.lossyScale;
            Vector3 center = Vector3.zero;
            var box = block.GetComponent<BoxCollider>();
            if (box != null)
            {
                size = Vector3.Scale(box.size, block.lossyScale);
                center = Vector3.Scale(box.center, block.lossyScale);
            }

            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            int axis = size.x <= size.z ? (size.x <= size.y ? 0 : 1) : (size.z <= size.y ? 2 : 1);
            Vector3 n = Vector3.zero;
            n[axis] = size[axis] * 0.5f + 0.12f;
            return new[]
            {
                Create(block, center + n, text, color, height, null, true),
                Create(block, center - n, text, color, height, null, true),
            };
        }

        private static bool Uniform(Vector3 s) => Mathf.Abs(s.x - s.y) < 0.001f && Mathf.Abs(s.x - s.z) < 0.001f && s.x > 0.0001f;

        /// <summary>Position : l'ancre, plus le décalage en mètres dans son repère tourné.</summary>
        private void Place()
        {
            if (_anchor != null)
                transform.position = _anchor.position + _anchor.rotation * _offset;
        }

        private static Material _textMaterial;

        /// <summary>
        /// Matériau du texte : la police avec test de profondeur (shader ItHunts/WorldText, Resources/WorldText) : le décor
        /// cache le texte (playtest des étapes 8 et 9). Sans lui, le matériau de la police (visible à travers les murs).
        /// </summary>
        public static Material TextMaterial(Font font)
        {
            if (_textMaterial == null)
            {
                Material template = Resources.Load<Material>("WorldText");
                if (template == null || template.shader == null || !template.shader.isSupported)
                    return font.material;
                _textMaterial = new Material(template) { name = "WorldText (police)", mainTexture = font.material.mainTexture };
                Font.textureRebuilt += f =>
                {
                    if (f == font && _textMaterial != null)
                        _textMaterial.mainTexture = font.material.mainTexture;
                };
            }

            return _textMaterial;
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
                _color = value;
                _alpha = -1f;                                            // réappliquée avec l'opacité de la distance
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

        /// <summary>
        /// Avant chaque caméra : face à elle, texte et visibilité pour son joueur. Visible seulement si aucun décor n'est entre la
        /// caméra et le texte (en plus du test de profondeur) ; loin hors de l'écran, il n'est pas dessiné.
        /// </summary>
        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null || _text == null)
                return;
            if (_follow)
            {
                if (_anchor == null)
                {
                    Destroy(gameObject);                                     // l'objet qui le portait a disparu
                    return;
                }

                Place();
            }

            transform.rotation = cam.transform.rotation;                  // dans le plan de l'écran : jamais penché
            PlayerHealth viewer = Interactables.PlayerOfCamera(cam);
            bool visible = AllowedFor(viewer) && (!_follow || _anchor.gameObject.activeInHierarchy);
            // Fondu avec la distance (correctifs-rendu point 5) : lisible jusqu'à 20 m, invisible à 35 m, jamais à travers le
            // brouillard (réglages de l'ambiance de la scène).
            float alpha = 1f;
            var amb = ItHunts.Level.SceneAmbiance.Active;
            if (amb != null)
            {
                float d = Vector3.Distance(cam.transform.position, transform.position);
                float fogEnd = RenderSettings.fog ? RenderSettings.fogEndDistance : float.PositiveInfinity;
                alpha = ItHunts.Settings.AmbianceSettings.LabelAlpha(d, amb.labelFullDistance, amb.labelHiddenDistance, fogEnd, amb.labelFogShare);
            }

            if (alpha <= 0.01f)
                visible = false;
            if (visible)
                visible = InSight(cam);
            if (visible && Mathf.Abs(alpha - _alpha) > 0.01f)
            {
                _alpha = alpha;
                _text.color = new Color(_color.r, _color.g, _color.b, _color.a * alpha);
            }
            if (_renderer != null)
                _renderer.enabled = visible;
            string t = viewer != null && TextFor != null ? TextFor(viewer) : _base;
            if (visible && _text.text != t)
                _text.text = t;
        }

        /// <summary>Dans le champ (large marge) et aucun décor entre la caméra et le texte ?</summary>
        public bool InSight(Camera cam)
        {
            Vector3 pos = transform.position;
            Vector3 vp = cam.WorldToViewportPoint(pos);
            bool los = vp.z > 0f && vp.x > -0.5f && vp.x < 1.5f && vp.y > -0.5f && vp.y < 1.5f
                       && !PlayerSight.DecorBetween(cam.transform.position, pos, _ignore);
            LastLineOfSight = los;
            LastCamera = cam;
            return los;
        }
    }
}
