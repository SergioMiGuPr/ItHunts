using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// HUD d'un joueur (étape 5), en uGUI, sur un canvas « Screen Space - Camera » posé sur la caméra des mains de CE
    /// joueur : il reste dans sa zone d'écran (écran partagé) et passe devant les mains. Construit par le code (greybox).
    /// - Autour du viseur : point, anneaux (bleu traction, orange répulsion, puis violet / vert / rouge pour la
    ///   télékinésie), charges de mouvement, charges d'attraction et répulsion (temps restant), refus en rouge.
    /// - HUD discret (GaugeFade) : une jauge pleine et inutilisée s'estompe ; utilisée, refusée ou en recharge, elle
    ///   revient tout de suite à 100 % ; une charge qui revient clignote une fois.
    /// - Dans le monde : chiffres de dégâts, barres de PV, durabilité de l'objet tenu.
    /// - Coins réservés (vides) : âmes (haut gauche), timer (haut droite), traqueur enragé (sous le timer).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed partial class PlayerHud : MonoBehaviour
    {
        [SerializeField] private HudSettings settings;
        /// <summary>Habillage de la DA (écran de chargement, panneaux).</summary>
        public HudSkinSettings Skin => settings != null ? settings.skin : null;
        [Tooltip("Couleurs du magnétisme, anneaux, refus.")]
        [SerializeField] private MagnetismFeedbackSettings feedback;
        [Tooltip("Couleurs de la télékinésie (anneaux violet et vert).")]
        [SerializeField] private TelekinesisFeedbackSettings tkFeedback;
        [Tooltip("Couleurs des chiffres de dégâts, des barres de PV et de la durabilité.")]
        [SerializeField] private DamageFeedbackSettings damageFeedback;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.PlayerHud");
        private PlayerMotor _motor;
        private FirstPersonView _view;
        private Camera _worldCamera;
        private Canvas _canvas;
        private RectTransform _root;
        private Font _font;
        private Sprite _white;
        private readonly Dictionary<float, Sprite> _rings = new Dictionary<float, Sprite>();

        private Image _attractRing, _repelRing, _tkRing;
        private readonly List<(Image back, Image fill)> _moveSegments = new List<(Image, Image)>();
        private readonly List<(Image back, Image fill)> _attractSegments = new List<(Image, Image)>();
        private (Image back, Image fill) _repelSegment;
        private readonly List<(Image back, Image fill)> _repelList = new List<(Image, Image)>();
        private RectTransform _worldRect;
        private Text _repelTime;
        private Image _holdBack, _holdFill;
        private CanvasGroup _moveGroup, _attractGroup, _repelGroup, _ammoGroup;
        private readonly GaugeFade _moveFade = new GaugeFade(), _attractFade = new GaugeFade(), _repelFade = new GaugeFade(),
            _ammoFade = new GaugeFade();
        private readonly List<(Image back, Image fill)> _ammoSegments = new List<(Image, Image)>();
        private ItHunts.Artifacts.ArtifactHolder _artifact;
        private PlayerHealth _health;
        private Image _healthBack, _healthFill;
        private float _healthFlashUntil = float.NegativeInfinity;
        private int _ammoBuiltFor = -1;
        private float _ammoRowY;
        private RectTransform _center;
        private ItHunts.Enemies.ZoneState _zone;
        private ItHunts.Enemies.Stalker _stalker;
        private Text _timerText, _stalkerText, _zoneMessage, _waveText, _soulsText, _promptText;
        private float _refusedAt = float.NegativeInfinity, _soulsGainAt = float.NegativeInfinity;
        private AudioClip _refusalClip;
        private ItHunts.Economy.PlayerInteractor _interactor;
        private PlayerParry _parry;
        private CanvasGroup _parryGroup;
        private Image _parryBack, _parryFill;
        private readonly GaugeFade _parryFade = new GaugeFade();

        /// <summary>Remplissage de l'icône de parade affichée (1 = prête), -1 sans parade.</summary>
        public float ParryIconFill => _parryFill != null ? _parryFill.fillAmount : -1f;

        private struct Number
        {
            public Text Text;
            public Vector3 Point;
            public float Born;
            public Color Color;
            public int Size;
        }

        private readonly List<Number> _numbers = new List<Number>();
        private readonly Stack<Text> _freeTexts = new Stack<Text>();
        private readonly List<(Image back, Image fill)> _healthBars = new List<(Image, Image)>();
        private readonly List<Image> _pips = new List<Image>();

        /// <summary>Coins réservés (vides) : âmes, timer, traqueur enragé.</summary>
        public RectTransform SoulsCorner { get; private set; }
        public RectTransform TimerCorner { get; private set; }
        public RectTransform StalkerCorner { get; private set; }
        public Canvas Canvas => _canvas;

        /// <summary>Munitions affichées (segments) : 0 si pas de barillet ; opacité (HUD discret).</summary>
        public int AmmoSegments => _ammoSegments.Count;
        public float AmmoAlpha => _ammoFade.Alpha;

        /// <summary>Opacité actuelle des jauges (HUD discret) : mouvement, attraction, répulsion.</summary>
        public Vector3 GaugeAlphas => new Vector3(_moveFade.Alpha, _attractFade.Alpha, _repelFade.Alpha);

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _view = GetComponent<FirstPersonView>();
            _artifact = GetComponent<ItHunts.Artifacts.ArtifactHolder>();
            _health = GetComponent<PlayerHealth>();
            var playerInput = GetComponent<PlayerInput>();
            _worldCamera = playerInput != null ? playerInput.camera : null;
        }

        private void Start()
        {
            if (settings == null || _motor == null)
                return;
            Build();
            Built?.Invoke(this);
        }

        /// <summary>HUD construit : la vue de la DA (ItHunts.HudArt) y pose le réticule, les charges et la parade.</summary>
        public static event System.Action<PlayerHud> Built;

        /// <summary>Habillage du HUD (prefabs de la DA, polices).</summary>
        public HudSkinSettings HudSkin => settings != null ? settings.skin : null;

        /// <summary>Canevas du HUD (caméra, calque) : la vue de la DA y aligne le sien.</summary>
        public Canvas HudCanvas => _canvas;

        /// <summary>Réticule, charges et parade de la DA posés : le point, l'anneau de la télékinésie, les segments des charges et
        /// l'icône provisoire de la parade sont masqués.</summary>
        public bool ArtGauges { get; private set; }

        public void UseArtGauges()
        {
            ArtGauges = true;
            if (_dot != null)
                _dot.gameObject.SetActive(false);
            if (_tkRing != null)
                _tkRing.gameObject.SetActive(false);
            if (_moveGroup != null)
                _moveGroup.gameObject.SetActive(false);
            if (_parryGroup != null)
                _parryGroup.gameObject.SetActive(false);
        }

        private Image _dot;

        private void OnEnable()
        {
            DamageEvents.Hit += OnHit;
            PlayerHealth.Hurt += OnPlayerHurt;
        }

        private void OnDisable()
        {
            DamageEvents.Hit -= OnHit;
            PlayerHealth.Hurt -= OnPlayerHurt;
        }

        // ---------- Construction ----------

        private void Build()
        {
            _font = settings.skin != null && settings.skin.textFont != null ? settings.skin.textFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

            var go = new GameObject("HUD (joueur)");
            go.transform.SetParent(transform, false);
            if (_view != null && _view.Layer >= 0)
                go.layer = _view.Layer;
            // Superposé (Screen Space - Overlay) : dessiné après le rendu, donc jamais étalonné par le post-process (Enragé rouge sang).
            // Écran partagé : la racine épouse le rectangle de la caméra du joueur, l'échelle suit la hauteur de sa vue (LateUpdate).
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10 + (_motor != null ? Mathf.Max(0, PlayerHealth.IdOf(transform)) : 0);
            _scaler = go.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            var view = new GameObject("Vue du joueur", typeof(RectTransform));
            view.layer = go.layer;
            view.transform.SetParent(go.transform, false);
            _root = (RectTransform)view.transform;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            FitView();

            RectTransform center = Rect("Viseur", _root, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            _center = center;
            Image dot = NewImage("Point", center, Vector2.zero, Vector2.one * settings.crosshairSize, settings.crosshairColor);
            dot.raycastTarget = false;
            _dot = dot;
            float thickness = feedback != null ? feedback.ringThickness : 2f;
            // Traction et répulsion : plus d'anneaux ni de jauges, elles sont sur les tablettes de l'orbe (2026-11-21).
            _tkRing = Ring("Anneau télékinésie", center, tkFeedback != null ? tkFeedback.grabRingRadius : 18f, thickness);

            // Rangée 1 : charges de mouvement.
            float y = -settings.offsetBelowCrosshair;
            int moves = _motor.Settings != null ? _motor.Settings.movementCharges : 0;
            _moveGroup = Group("Charges de mouvement", center);
            Segments(_moveGroup.transform, moves, -Width(moves) * 0.5f, y, _moveSegments);

            // Parade (D-075) : icône provisoire à droite des charges (recharge qui se remplit), en attendant celle de la DA.
            _parry = GetComponent<PlayerParry>();
            if (_parry != null)
            {
                _parryGroup = Group("Parade (provisoire)", center);
                float px = Width(moves) * 0.5f + settings.groupGap + settings.segmentHeight;
                _parryBack = NewImage("Parade (fond)", _parryGroup.transform, new Vector2(px, y - settings.segmentHeight * 0.5f),
                    Vector2.one * settings.segmentHeight * 2f, settings.emptyColor);
                _parryBack.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                _parryFill = NewImage("Parade", _parryBack.rectTransform, Vector2.zero, Vector2.one * settings.segmentHeight * 2f, Color.white);
                _parryFill.type = Image.Type.Filled;
                _parryFill.fillMethod = Image.FillMethod.Vertical;
            }

            // Rangée 2 (charges d'attraction, répulsion) retirée le 2026-11-21 : sur les tablettes de l'orbe.
            y -= settings.segmentHeight + settings.rowGap;
            MagnetismOrbSettings mc = null;
            if (mc != null)
            {
                int attract = mc.attractCharges;
                float row = Width(attract) + settings.groupGap + settings.segmentWidth;
                float x = -row * 0.5f;
                _attractGroup = Group("Charges d'attraction", center);
                Segments(_attractGroup.transform, attract, x, y, _attractSegments);
                _repelGroup = Group("Répulsion", center);
                var list = new List<(Image, Image)>();
                float repelX = x + Width(attract) + settings.groupGap;
                Segments(_repelGroup.transform, 1, repelX, y, list);
                _repelSegment = list[0];
                _repelList.Add(_repelSegment);
                _repelTime = NewText("Temps de recharge", _repelGroup.transform,
                    new Vector2(repelX + settings.segmentWidth + 4f + 30f, y - settings.segmentHeight * 0.5f), new Vector2(60f, 18f),
                    settings.timeFontSize, TextAnchor.MiddleLeft);

                // Répulsion maintenue : durée restante, barre orange qui rétrécit sous les jauges.
                float holdY = y - settings.segmentHeight - settings.rowGap - 2f;
                _holdBack = NewImage("Répulsion maintenue (fond)", center, new Vector2(0f, holdY), new Vector2(row, 3f), settings.emptyColor);
                _holdFill = NewImage("Répulsion maintenue", _holdBack.rectTransform, Vector2.zero, new Vector2(row, 3f), Color.white);
                _holdFill.type = Image.Type.Filled;
                _holdFill.fillMethod = Image.FillMethod.Horizontal;
                _holdFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                _holdBack.gameObject.SetActive(false);
            }

            // Rangée 3 : munitions de l'artefact (construite quand l'artefact est connu, voir UpdateAmmo).
            _ammoRowY = y - 2f * (settings.segmentHeight + settings.rowGap) - 4f;

            // PV du joueur (tir ami) : coin bas gauche, cachés quand ils sont pleins.
            float hm = settings.safeMargin;
            RectTransform healthCorner = Rect("Coin : PV du joueur", _root, new Vector2(hm, hm), Vector2.zero, settings.playerHealthSize,
                new Vector2(0f, 0f));
            _healthBack = NewImage("PV du joueur (fond)", healthCorner, Vector2.zero, settings.playerHealthSize, new Color(0f, 0f, 0f, 0.6f));
            _healthFill = NewImage("PV du joueur", _healthBack.rectTransform, Vector2.zero, settings.playerHealthSize, settings.playerHealthColor);
            _healthFill.type = Image.Type.Filled;
            _healthFill.fillMethod = Image.FillMethod.Horizontal;
            _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            _healthBack.gameObject.SetActive(false);

            // Coins réservés (vides), dans la marge de sécurité.
            float m = settings.safeMargin;
            SoulsCorner = Rect("Coin : âmes", _root, new Vector2(m, 1f - m), Vector2.zero, new Vector2(240f, 60f), new Vector2(0f, 1f));
            TimerCorner = Rect("Coin : timer", _root, new Vector2(1f - m, 1f - m), Vector2.zero, new Vector2(260f, settings.timerFontSize + 16f), new Vector2(1f, 1f));
            StalkerCorner = Rect("Coin : traqueur enragé", _root, new Vector2(1f - m, 1f - m), new Vector2(0f, -70f), new Vector2(240f, 40f), new Vector2(1f, 1f));
            _timerText = NewText("Timer de la zone", TimerCorner, Vector2.zero, new Vector2(260f, settings.timerFontSize + 16f), settings.timerFontSize, TextAnchor.MiddleRight);
            _stalkerText = NewText("Traqueur enragé", StalkerCorner, Vector2.zero, new Vector2(240f, 40f), 22, TextAnchor.MiddleRight);
            _stalkerText.fontStyle = FontStyle.Bold;
            _zoneMessage = NewText("Message de zone", _root, new Vector2(0f, 120f), new Vector2(900f, 60f), 36, TextAnchor.MiddleCenter);
            _zoneMessage.rectTransform.anchorMin = _zoneMessage.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            // Pic de vague (D-041) : indication brève en haut, au centre.
            _waveText = NewText("Pic de vague", _root, new Vector2(0f, -90f), new Vector2(600f, 50f), 30, TextAnchor.MiddleCenter);
            _waveText.rectTransform.anchorMin = _waveText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            _waveText.fontStyle = FontStyle.Bold;
            // Âmes (D-043) : coin haut gauche ; aide d'interaction sous le viseur (E / Y).
            _soulsText = NewText("Âmes", SoulsCorner, Vector2.zero, new Vector2(240f, 60f), 34, TextAnchor.MiddleLeft);
            _soulsText.fontStyle = FontStyle.Bold;
            _promptText = NewText("Aide d'interaction", _root, new Vector2(0f, -150f), new Vector2(1100f, 44f), 24, TextAnchor.MiddleCenter);
            _promptText.rectTransform.anchorMin = _promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ItHunts.Artifacts.RunState.Refused += OnRefused;
            ItHunts.Artifacts.RunState.SoulsChanged += OnSoulsChanged;
            foreach (Text t in new[] { _timerText, _stalkerText, _zoneMessage, _waveText, _soulsText, _promptText })
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.8f);
                sh.effectDistance = new Vector2(2f, -2f);
                t.text = string.Empty;
            }

            BuildSkin();                                                // habillage de la DA (PR #10), s'il est réglé

            // Monde : barres de PV, chiffres de dégâts, durabilité.
            var world = Rect("Monde", _root, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            _worldRect = world;
            for (int i = 0; i < settings.healthBarPool; i++)
            {
                Image back = NewImage("Barre de PV", world, Vector2.zero, new Vector2(60f, 6f), new Color(0f, 0f, 0f, 0.7f));
                Image fill = NewImage("PV", back.rectTransform, Vector2.zero, new Vector2(60f, 6f), Color.green);
                fill.rectTransform.anchorMin = fill.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                fill.rectTransform.pivot = new Vector2(0f, 0.5f);
                fill.rectTransform.anchoredPosition = Vector2.zero;
                back.gameObject.SetActive(false);
                _healthBars.Add((back, fill));
            }

            for (int i = 0; i < settings.damageNumberPool; i++)
            {
                Text t = NewText("Dégâts", world, Vector2.zero, new Vector2(200f, 60f), settings.damageFontMin, TextAnchor.MiddleCenter);
                t.fontStyle = FontStyle.Bold;
                var shadow = t.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
                shadow.effectDistance = new Vector2(2f, -2f);
                t.gameObject.SetActive(false);
                _freeTexts.Push(t);
            }

            for (int i = 0; i < 8; i++)
            {
                Image pip = NewImage("Durabilité", world, Vector2.zero, Vector2.one * 7f, Color.white);
                var outline = pip.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
                pip.gameObject.SetActive(false);
                _pips.Add(pip);
            }
        }

        private float Width(int count) => count * settings.segmentWidth + Mathf.Max(0, count - 1) * settings.segmentGap;

        private void Segments(Transform parent, int count, float x, float y, List<(Image, Image)> into)
        {
            for (int i = 0; i < count; i++)
            {
                var pos = new Vector2(x + i * (settings.segmentWidth + settings.segmentGap) + settings.segmentWidth * 0.5f, y - settings.segmentHeight * 0.5f);
                Image back = NewImage("Segment", parent, pos, new Vector2(settings.segmentWidth, settings.segmentHeight), settings.emptyColor);
                Image fill = NewImage("Rempli", back.rectTransform, Vector2.zero, new Vector2(settings.segmentWidth, settings.segmentHeight), Color.white);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Horizontal;
                into.Add((back, fill));
            }
        }

        private CanvasGroup Group(string name, RectTransform parent)
        {
            RectTransform r = Rect(name, parent, Vector2.one * 0.5f, Vector2.zero, Vector2.zero);
            var g = r.gameObject.AddComponent<CanvasGroup>();
            g.interactable = false;
            g.blocksRaycasts = false;
            return g;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        private Image NewImage(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            RectTransform r = Rect(name, parent, Vector2.one * 0.5f, position, size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = _white;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Text NewText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAnchor anchor)
        {
            RectTransform r = Rect(name, parent, Vector2.one * 0.5f, position, size);
            var t = r.gameObject.AddComponent<Text>();
            t.font = _font;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Image Ring(string name, RectTransform parent, float radius, float thickness)
        {
            Sprite sprite = RingSprite(radius, thickness);
            Image img = NewImage(name, parent, Vector2.zero, sprite.rect.size, Color.white);
            img.sprite = sprite;
            img.enabled = false;
            return img;
        }

        /// <summary>Anneau anticrénelé, un par rayon.</summary>
        private Sprite RingSprite(float radius, float thickness)
        {
            if (_rings.TryGetValue(radius, out Sprite cached))
                return cached;
            int size = Mathf.CeilToInt((radius + thickness) * 2f) + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(thickness * 0.5f + 0.5f - Mathf.Abs(d - radius));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            _rings[radius] = sprite;
            return sprite;
        }

        // ---------- Mise à jour ----------

        private CanvasScaler _scaler;

        /// <summary>Racine du HUD sur le rectangle de la caméra du joueur ; échelle = hauteur de sa vue / hauteur de référence.</summary>
        private void FitView()
        {
            Camera cam = _worldCamera;
            Rect r = cam != null ? cam.rect : new Rect(0f, 0f, 1f, 1f);
            _root.anchorMin = r.min;
            _root.anchorMax = r.max;
            Rect px = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            _scaler.scaleFactor = ScaleFor(px.width, px.height, settings);
        }

        /// <summary>
        /// Échelle du HUD (pur, testable) : la hauteur de la vue rapportée à 1080, et la largeur quand la vue est plus étroite que
        /// minLayoutWidth (écran partagé côte à côte, 4:3) : les coins (âmes, mini-carte, timer) ne se chevauchent pas et restent dans
        /// la vue.
        /// </summary>
        public static float ScaleFor(float viewWidth, float viewHeight, HudSettings s)
        {
            float k = Mathf.Max(0.1f, s.scale);
            float byHeight = viewHeight / Mathf.Max(1f, s.referenceResolution.y / k);
            float byWidth = s.minLayoutWidth > 0f ? viewWidth / Mathf.Max(1f, s.minLayoutWidth / k) : float.MaxValue;
            return Mathf.Max(0.01f, Mathf.Min(byHeight, byWidth));
        }

        private void LateUpdate()
        {
            if (_canvas == null)
                return;
            FitView();
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float now = Time.time, dt = Time.deltaTime, fixedDt = Time.fixedDeltaTime;
            int tick = _motor.State.Tick;

            UpdateRings(tick, fixedDt);

            // Charges de mouvement.
            float moveCharges = Mathf.Max(0f, _motor.State.Charges);
            _moveGroup.alpha = _moveFade.Update(moveCharges, _moveSegments.Count, false, now, dt, settings.idleAlpha,
                settings.idleDelay, settings.fadeSpeed, settings.returnBlinkDuration);
            FillSegments(_moveSegments, moveCharges, settings.movementColor, settings.emptyColor, _moveFade.Blinking(now));

            if (_parryFill != null && _parry.Settings != null && !ArtGauges)
            {
                float fill = _parry.Fill;
                _parryFill.fillAmount = fill;
                Color pc = _parry.Settings.hudColor;
                _parryFill.color = fill >= 1f ? pc : new Color(pc.r * 0.6f, pc.g * 0.6f, pc.b * 0.6f, pc.a);
                _parryGroup.alpha = _parryFade.Update(fill >= 1f ? 1f : 0f, 1f, fill < 1f, now, dt, settings.idleAlpha,
                    settings.idleDelay, settings.fadeSpeed, settings.returnBlinkDuration);
            }

            MagnetismOrbSettings mc = _motor.MagnetismSettings;
            if (mc != null && _attractGroup != null)
            {
                MagnetismState m = _motor.Magnetism;
                bool attractRefused = feedback != null && MagnetismFeedback.RefusalBlinkOn(m, MagnetMode.Attraction, tick, fixedDt,
                    feedback.refusalFlashDuration, feedback.refusalBlinkPeriod);
                bool repelRefused = feedback != null && MagnetismFeedback.RefusalBlinkOn(m, MagnetMode.Repulsion, tick, fixedDt,
                    feedback.refusalFlashDuration, feedback.refusalBlinkPeriod);
                float attract = m.AttractCharges < 0f ? mc.attractCharges : m.AttractCharges;
                _attractGroup.alpha = _attractFade.Update(attract, mc.attractCharges, attractRefused || m.Tracting, now, dt,
                    settings.idleAlpha, settings.idleDelay, settings.fadeSpeed, settings.returnBlinkDuration);
                Color attractColor = Opaque(feedback != null ? feedback.attractColor : Color.cyan);
                FillSegments(_attractSegments, attract, attractColor, attractRefused ? feedback.refusalColor : settings.emptyColor,
                    _attractFade.Blinking(now), attractRefused ? feedback.refusalColor : (Color?)null);

                float remaining = m.RepelHolding ? 0f : MagnetismOrb.RepelCooldownRemaining(m, mc, tick, fixedDt);
                float holdLeft = MagnetismOrb.RepelHoldRemaining(m, mc, tick, fixedDt);
                _holdBack.gameObject.SetActive(m.RepelHolding);
                if (m.RepelHolding)
                {
                    _holdFill.fillAmount = mc.repelHoldMaxDuration > 0f ? holdLeft / mc.repelHoldMaxDuration : 0f;
                    _holdFill.color = Opaque(feedback != null ? feedback.repelColor : new Color(1f, 0.5f, 0.1f));
                }
                float repelValue = m.RepelHolding ? 0f : mc.repelCooldown > 0f && remaining > 0f ? 1f - remaining / mc.repelCooldown : 1f;
                _repelGroup.alpha = _repelFade.Update(repelValue >= 1f ? 1f : 0f, 1f, repelRefused || remaining > 0f || m.RepelHolding, now, dt,
                    settings.idleAlpha, settings.idleDelay, settings.fadeSpeed, settings.returnBlinkDuration);
                Color repelColor = Opaque(feedback != null ? feedback.repelColor : new Color(1f, 0.5f, 0.1f));
                FillSegments(_repelList, Mathf.Clamp01(repelValue), repelColor, repelRefused ? feedback.refusalColor : settings.emptyColor,
                    _repelFade.Blinking(now), repelRefused ? feedback.refusalColor : (Color?)null);
                _repelTime.text = m.RepelHolding ? $"{holdLeft:0.0} s" : remaining > 0f ? $"{remaining:0.0} s" : string.Empty;
                _repelTime.color = repelColor;
            }

            UpdateZone(now);
            UpdateSouls(now);
            UpdateAmmo(tick, now, dt, fixedDt);
            UpdatePlayerHealth(now);
            UpdateHealthBar(now, dt);
            UpdateFullMap();
            UpdateMinimap();
            UpdateWorld();
        }

        private static string Clock(float seconds)
        {
            int s = Mathf.CeilToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        /// <summary>Timer de la zone (coin haut droite), traqueur enragé (sous le timer), message central (mort, sortie).</summary>
        private void UpdateZone(float now)
        {
            if (_timerText == null)
                return;
            if (_zone == null)
                _zone = FindFirstObjectByType<ItHunts.Enemies.ZoneState>();
            if (_zone == null)
            {
                _timerText.text = _stalkerText.text = _zoneMessage.text = string.Empty;
                return;
            }

            int left = Mathf.CeilToInt(_zone.TimeLeft);
            // Le temps seul, en grand (retours du 2026-11-17 : plus de texte d'objectif dessous). Il attend la sortie de la salle de départ (D-048).
            _timerText.text = $"{left / 60}:{left % 60:00}";
            _timerText.color = _zone.Enraged ? new Color(1f, 0.25f, 0.2f) : left <= 30 ? new Color(1f, 0.7f, 0.3f) : Color.white;
            _stalkerText.text = _zone.Enraged ? "TRAQUEUR ENRAGÉ" : string.Empty;
            // D-038 : plus il accélère, plus l'indication pulse fort ; éclat d'une seconde à chaque palier.
            if (_stalker == null)
                _stalker = FindFirstObjectByType<ItHunts.Enemies.Stalker>();
            int tier = _stalker != null ? _stalker.EnragedTier : 0;
            float flash = _stalker != null ? Mathf.Clamp01(1f - (now - _stalker.LastTierTime)) : 0f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(now * (8f + 3f * tier));
            _stalkerText.color = new Color(1f, 0.2f + 0.6f * flash, 0.15f + 0.5f * flash, Mathf.Lerp(0.6f - 0.08f * tier, 1f, pulse));
            _stalkerText.rectTransform.localScale = Vector3.one * (1f + 0.04f * tier * pulse + 0.35f * flash);
            _zoneMessage.text = _zone.Message ?? string.Empty;
            if (_waveText != null)
            {
                if (_waves == null)
                    _waves = FindFirstObjectByType<ItHunts.Enemies.WaveDirector>();
                float show = _waves != null && _waves.Settings != null ? _waves.Settings.peakAnnounceTime : 1.5f;
                float since = now - ItHunts.Enemies.WaveDirector.LastPeakTime;
                bool on = _waves != null && since >= 0f && since < show;
                _waveText.text = on ? $"VAGUE {_waves.PeakIndex}" : string.Empty;
                if (on)
                {
                    float a = Mathf.Clamp01((show - since) / 0.4f);
                    _waveText.color = new Color(1f, 0.55f, 0.2f, a);
                    _waveText.rectTransform.localScale = Vector3.one * (1f + 0.25f * Mathf.Clamp01(1f - since / 0.25f));
                }
            }
        }

        private ItHunts.Enemies.WaveDirector _waves;

        private void OnDestroy()
        {
            ItHunts.Artifacts.RunState.Refused -= OnRefused;
            ItHunts.Artifacts.RunState.SoulsChanged -= OnSoulsChanged;
        }

        private void OnRefused(int playerId)
        {
            if (_health == null || playerId != _health.PlayerId)
                return;
            _refusedAt = Time.time;
            if (_refusalClip == null)
                _refusalClip = ItHunts.Audio.GameAudio.Or(s => s.refusal, ItHunts.Audio.ProceduralSfx.ToClip("Refus (âmes)", ItHunts.Audio.ProceduralSfx.Refusal()));
            AudioSource.PlayClipAtPoint(_refusalClip, _health.transform.position + Vector3.up, 0.7f);
        }

        private void OnSoulsChanged(int playerId, int total, int delta)
        {
            if (_health != null && playerId == _health.PlayerId && delta > 0)
                _soulsGainAt = Time.time;
        }

        /// <summary>Âmes (rouge un instant si un achat est refusé, plus clair à chaque gain) et aide d'interaction.</summary>
        private void UpdateSouls(float now)
        {
            if (_soulsText == null || _health == null)
                return;
            var run = ItHunts.Artifacts.RunState.Current;
            int souls = run != null ? run.Souls(_health.PlayerId) : 0;
            _soulsText.text = _skin != null ? souls.ToString() : $"Âmes  {souls}";
            float refused = Mathf.Clamp01(1f - (now - _refusedAt) / 0.8f);
            float gain = Mathf.Clamp01(1f - (now - _soulsGainAt) / 0.3f);
            Color normal = Color.Lerp(_skin != null ? _skin.soulsTextColor : new Color(0.55f, 0.95f, 1f), Color.white, gain);
            _soulsText.color = refused > 0f ? Color.Lerp(normal, new Color(1f, 0.2f, 0.15f), refused) : normal;
            _soulsText.rectTransform.localScale = Vector3.one * (1f + 0.25f * refused + 0.08f * gain);
            if (_interactor == null)
                _interactor = GetComponent<ItHunts.Economy.PlayerInteractor>();
            var reader = GetComponent<ItHunts.Input.PlayerInputReader>();
            string key = reader != null ? ItHunts.Input.RebindStore.Prompt(reader.InteractAction, reader.UsingGamepad) : "E";   // touche actuelle (A5)
            var focus = _interactor != null ? _interactor.Focus : null;
            _promptText.text = focus != null ? $"{key} : {focus.Prompt(_health)}" : string.Empty;
            Color promptColor = _skin != null ? new Color(0.95f, 0.88f, 0.74f) : Color.white;
            _promptText.color = refused > 0f ? Color.Lerp(promptColor, new Color(1f, 0.25f, 0.2f), refused) : promptColor;
            SyncPromptFrame();
        }

        /// <summary>Munitions du barillet : un segment par munition ; en rechargement, ils se remplissent ; grisés si inactif.</summary>
        private void UpdateAmmo(int tick, float now, float dt, float fixedDt)
        {
            if (!settings.showAmmoRow)
                return;                                                  // recharge de tir retirée du HUD (2026-11-21)
            ItHunts.Settings.BarrelSettings cfg = _artifact != null ? _artifact.BarrelSettings : null;
            ItHunts.Settings.FireLanternSettings lantern = _artifact != null ? _artifact.LanternSettings : null;
            int rounds = cfg != null ? cfg.rounds : lantern != null ? 1 : 0;   // lanterne (D-065) : un seul segment, sa recharge
            if (rounds != _ammoBuiltFor)
            {
                _ammoBuiltFor = rounds;
                if (_ammoGroup != null)
                    Destroy(_ammoGroup.gameObject);
                _ammoSegments.Clear();
                _ammoGroup = null;
                if (rounds > 0)
                {
                    _ammoGroup = Group("Munitions", _center);
                    float w = rounds * settings.ammoSegmentWidth + (rounds - 1) * settings.segmentGap;
                    float x = -w * 0.5f;
                    var size = new Vector2(settings.ammoSegmentWidth, settings.segmentHeight * 1.5f);
                    for (int i = 0; i < rounds; i++)
                    {
                        float sx = x + i * (settings.ammoSegmentWidth + settings.segmentGap) + settings.ammoSegmentWidth * 0.5f;
                        Image back = NewImage("Munition (fond)", _ammoGroup.transform, new Vector2(sx, _ammoRowY), size, settings.emptyColor);
                        Image fill = NewImage("Munition", back.rectTransform, Vector2.zero, size, Color.white);
                        fill.type = Image.Type.Filled;
                        fill.fillMethod = Image.FillMethod.Vertical;
                        fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                        _ammoSegments.Add((back, fill));
                    }
                }
            }

            if (_ammoGroup != null && lantern != null)
            {
                ItHunts.Artifacts.LanternState ls = _artifact.Lantern;
                float charge = ls.Jetting ? 1f - ItHunts.Artifacts.FireLantern.JetProgress(ls, tick, lantern, fixedDt)
                    : ItHunts.Artifacts.FireLantern.CooldownProgress(ls, tick, lantern, fixedDt);
                bool busy = charge < 1f;
                _ammoGroup.alpha = _ammoFade.Update(busy ? 0f : 1f, 1, busy, now, dt, settings.idleAlpha, settings.idleDelay,
                    settings.fadeSpeed, settings.returnBlinkDuration);
                FillSegments(_ammoSegments, charge, ls.Inactive ? settings.ammoInactiveColor : settings.ammoColor, settings.emptyColor,
                    _ammoFade.Blinking(now));
                return;
            }

            if (_ammoGroup == null || cfg == null)
                return;
            ItHunts.Artifacts.BarrelState b = _artifact.Barrel;
            float value = b.Reloading ? ItHunts.Artifacts.Barrel.ReloadProgress(b, tick, cfg, fixedDt) * rounds : b.Rounds;
            bool active = b.Reloading || b.Rounds < rounds;
            _ammoGroup.alpha = _ammoFade.Update(b.Reloading ? 0f : b.Rounds, rounds, active, now, dt, settings.idleAlpha, settings.idleDelay,
                settings.fadeSpeed, settings.returnBlinkDuration);
            FillSegments(_ammoSegments, value, b.Inactive ? settings.ammoInactiveColor : settings.ammoColor, settings.emptyColor,
                _ammoFade.Blinking(now));
        }

        private void UpdatePlayerHealth(float now)
        {
            if (_health == null || _healthBack == null || _skin != null)
                return;
            HealthState h = _health.Health;
            bool show = h.Dead || h.Current < h.Max;
            _healthBack.gameObject.SetActive(show);
            if (!show)
                return;
            _healthFill.fillAmount = h.Max > 0f ? h.Current / h.Max : 0f;
            _healthFill.color = now < _healthFlashUntil ? Color.white : settings.playerHealthColor;
        }

        private void OnPlayerHurt(PlayerHurtEvent e)
        {
            if (_canvas == null)
                return;
            if (e.Victim != null && e.Victim == _health)
            {
                _healthFlashUntil = Time.time + 0.15f;                   // c'est moi : la barre de PV clignote
                return;
            }

            if (e.AttackerId > 0)                                        // tir ami seulement (pas les coups des ennemis)
                AddNumber(Mathf.CeilToInt(e.Damage).ToString(), e.Victim != null ? e.Victim.Center + Vector3.up * 0.6f : e.Point,
                    settings.friendlyHitColor, settings.damageFontMin);
        }

        private void UpdateRings(int tick, float fixedDt)
        {
            if (feedback == null)
                return;
            MagnetismState m = _motor.Magnetism;
            bool magnet = _motor.MagnetismSettings != null;
            SetRing(_attractRing, magnet && MagnetismFeedback.CanAttract(m), Opaque(feedback.attractColor));
            SetRing(_repelRing, magnet && MagnetismFeedback.CanRepel(m), Opaque(feedback.repelColor));

            if (tkFeedback == null || _motor.TelekinesisSettings == null)
            {
                SetRing(_tkRing, false, Color.clear);
                return;
            }

            TelekinesisState t = _motor.Telekinesis;
            bool refused = MagnetismFeedback.BlinkOn(t.LastRefusalTick, tick, fixedDt, feedback.refusalFlashDuration, feedback.refusalBlinkPeriod);
            if (refused)
                SetRing(_tkRing, true, feedback.refusalColor);
            else if (TelekinesisOrb.CanThrow(t))
                SetRing(_tkRing, true, Opaque(tkFeedback.throwColor));
            else
                SetRing(_tkRing, TelekinesisOrb.CanGrab(t, _motor.TelekinesisSettings, tick, fixedDt), Opaque(tkFeedback.grabColor));
        }

        private static void SetRing(Image ring, bool on, Color color)
        {
            if (ring == null)
                return;
            ring.enabled = on;
            if (on)
                ring.color = color;
        }

        private void FillSegments(List<(Image back, Image fill)> segments, float value, Color full, Color empty, bool blink,
            Color? overrideFull = null)
        {
            Color partial = full;
            partial.a *= settings.rechargeAlpha;
            Color shown = overrideFull ?? full;
            if (blink)
                shown = new Color(Mathf.Min(1f, shown.r * settings.returnBlinkBrightness), Mathf.Min(1f, shown.g * settings.returnBlinkBrightness),
                    Mathf.Min(1f, shown.b * settings.returnBlinkBrightness), shown.a);
            for (int i = 0; i < segments.Count; i++)
            {
                float fill = Mathf.Clamp01(value - i);
                segments[i].back.color = empty;
                segments[i].fill.fillAmount = fill;
                segments[i].fill.color = fill >= 1f ? shown : partial;
            }
        }

        private static Color Opaque(Color c) => new Color(c.r, c.g, c.b, 0.95f);

        // ---------- Monde : chiffres, barres de PV, durabilité ----------

        private void OnHit(DamageEvent e)
        {
            if (damageFeedback == null || _canvas == null)
                return;
            Color color = e.Killed ? damageFeedback.killColor
                : e.Tier == HitTier.Gros ? damageFeedback.bigColor
                : e.Tier == HitTier.Moyen ? damageFeedback.mediumColor
                : damageFeedback.smallColor;
            int size = Mathf.RoundToInt(Mathf.Min(settings.damageFontMax, settings.damageFontMin + damageFeedback.numberSizePerDamage * e.Damage));
            Vector3 start = e.Target != null ? Vector3.Lerp(e.Point, e.Target.Top, 0.5f) : e.Point;
            AddNumber(Mathf.CeilToInt(e.Damage).ToString(), start, color, size);
            if (e.Killed)
                AddNumber("K.O.", start + Vector3.up * 0.5f, damageFeedback.killColor, settings.damageFontMax);
        }

        private void AddNumber(string text, Vector3 point, Color color, int size)
        {
            if (_freeTexts.Count == 0)
                return;
            Text t = _freeTexts.Pop();
            t.text = text;
            t.gameObject.SetActive(true);
            _numbers.Add(new Number { Text = t, Point = point, Born = Time.time, Color = color, Size = size });
        }

        private void UpdateWorld()
        {
            Camera cam = _worldCamera;
            if (cam == null || damageFeedback == null)
                return;

            for (int i = _numbers.Count - 1; i >= 0; i--)
            {
                Number n = _numbers[i];
                float age = Time.time - n.Born;
                if (age > damageFeedback.numberDuration)
                {
                    n.Text.gameObject.SetActive(false);
                    _freeTexts.Push(n.Text);
                    _numbers.RemoveAt(i);
                    continue;
                }

                float t = age / damageFeedback.numberDuration;
                Vector3 world = n.Point + Vector3.up * (damageFeedback.numberRise * (1f - (1f - t) * (1f - t)));
                bool visible = Place(n.Text.rectTransform, cam, world);
                n.Text.enabled = visible;
                Color c = n.Color;
                c.a = 1f - t * t;
                n.Text.color = c;
                n.Text.fontSize = Mathf.RoundToInt(n.Size * (t < 0.1f ? 1f + (0.1f - t) * 4f : 1f));
            }

            int bar = 0;
            foreach (Damageable d in Damageable.All)
            {
                if (bar >= _healthBars.Count)
                    break;
                if (d == null || d.Dead || d.ImmuneToOrbs)
                    continue;                                            // le traqueur, intuable, n'a pas de barre de PV
                // Nuit 3, point 1 : plus de barres vertes partout ; seulement un ennemi blessé, 2.5 s après le coup.
                if (Time.time - d.LastDamagedAt > 2.5f)
                    continue;
                Vector3 top = d.Top + Vector3.up * 0.35f;
                if (Vector3.Distance(cam.transform.position, top) > damageFeedback.healthBarDistance)
                    continue;
                (Image back, Image fill) = _healthBars[bar];
                if (!Place(back.rectTransform, cam, top))
                    continue;
                back.gameObject.SetActive(true);
                back.rectTransform.sizeDelta = new Vector2(damageFeedback.healthBarWidth, damageFeedback.healthBarHeight);
                HealthState h = d.Health;
                float k = h.Max > 0f ? h.Current / h.Max : 0f;
                fill.rectTransform.sizeDelta = new Vector2(damageFeedback.healthBarWidth * k, damageFeedback.healthBarHeight);
                fill.color = Color.Lerp(new Color(0.55f, 0.06f, 0.05f, 0.95f), new Color(0.85f, 0.2f, 0.12f, 0.95f), k);
                bar++;
            }

            for (int i = bar; i < _healthBars.Count; i++)
                _healthBars[i].back.gameObject.SetActive(false);

            UpdateDurability(cam);
        }

        private void UpdateDurability(Camera cam)
        {
            Grabbable held = _motor.Holder != null ? _motor.Holder.Held : null;
            int n = held != null && held.MaxDurability > 0 ? Mathf.Min(held.MaxDurability, _pips.Count) : 0;
            Vector2 local = Vector2.zero;
            bool visible = false;
            if (n > 0 && held.Colliders != null && held.Colliders.Length > 0)
            {
                Bounds b = held.Colliders[0].bounds;
                visible = ToCanvas(cam, new Vector3(b.center.x, b.max.y + 0.15f, b.center.z), out local);
            }

            float size = damageFeedback.durabilityPipSize, gap = 3f;
            float x = local.x - (n * size + (n - 1) * gap) * 0.5f + size * 0.5f;
            for (int i = 0; i < _pips.Count; i++)
            {
                bool on = visible && i < n;
                _pips[i].gameObject.SetActive(on);
                if (!on)
                    continue;
                _pips[i].rectTransform.anchoredPosition = new Vector2(x + i * (size + gap), local.y);
                _pips[i].rectTransform.sizeDelta = Vector2.one * size;
                _pips[i].color = i < held.ThrowsLeft ? damageFeedback.durabilityFull : damageFeedback.durabilityUsed;
            }
        }

        private bool Place(RectTransform r, Camera cam, Vector3 world)
        {
            if (!ToCanvas(cam, world, out Vector2 local))
                return false;
            r.anchoredPosition = local;
            return true;
        }

        /// <summary>
        /// Point du monde vu par la caméra du monde → position dans le canvas ; faux s'il est derrière ou hors de la zone.
        /// Calcul direct (écart au centre de la zone de la caméra, divisé par l'échelle du canvas), sans rayon vers le plan
        /// du canvas : en « Screen Space - Camera », Unity ne replace ce plan devant la caméra qu'au moment de dessiner l'UI,
        /// après LateUpdate ; le rayon partait donc de la caméra de cette image vers le plan de l'image précédente, à 5 cm
        /// de l'œil, et les barres s'envolaient dès que la caméra bougeait vite (mesuré : 168 px d'écart médian en saut,
        /// 556 px en dash).
        /// </summary>
        private bool ToCanvas(Camera cam, Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            Vector3 s = cam.WorldToScreenPoint(world);
            if (s.z < 0.1f || !cam.pixelRect.Contains(new Vector2(s.x, s.y)))
                return false;
            local = (new Vector2(s.x, s.y) - cam.pixelRect.center) / Mathf.Max(1e-4f, _canvas.scaleFactor);
            return true;
        }
    }
}
