using System.Collections.Generic;
using ItHunts.Economy;
using ItHunts.Level;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Hud
{
    /// <summary>
    /// Minimap greybox (D-053), dessinée dans la vue de chaque joueur (écran partagé compris), en haut à gauche, nord en haut :
    /// salles et tronçons découverts (MapDiscovery), le reste dans le noir ; le joueur (flèche blanche), ses alliés (points
    /// bleus) ; points clés une fois découverts : coffres (or, gris une fois ouverts par ce joueur), marchand (violet), salle
    /// verrouillée (orange), sortie (rouge verrouillée, verte ouverte), départ (jaune). Tab (maintenu) : toute la carte.
    /// Refaite à l'étape 10 (DA).
    /// </summary>
    public sealed class MinimapHud : MonoBehaviour
    {
        [Tooltip("Côté de la minimap (fraction de la hauteur de la vue du joueur).")]
        [Range(0.1f, 0.5f)] [SerializeField] private float size = 0.28f;
        [Tooltip("Mètres montrés autour du joueur (côté du carré).")]
        [Min(50f)] [SerializeField] private float span = 320f;

        private readonly List<RoomVolume> _volumes = new List<RoomVolume>();
        private readonly List<(Vector3 at, Color color, string label, Object owner)> _marks = new List<(Vector3, Color, string, Object)>();
        private Bounds _all;
        private float _refreshAt;
        private static GUIStyle _letter;

        public bool FullMap { get; private set; }

        /// <summary>La mini-carte de la scène (lue par le HUD habillé de chaque joueur).</summary>
        public static MinimapHud Current { get; private set; }
        /// <summary>
        /// Vrai quand le HUD habillé (PR #10) dessine la mini-carte ronde de chaque joueur : ici, il ne reste que la carte entière
        /// (Tab).
        /// </summary>
        public static bool RoundMinimap { get; set; }
        /// <summary>Salles découvertes peintes (nord en haut) et l'emprise de la carte qu'elles couvrent.</summary>
        public Texture MapTexture => _mapTex;
        public Bounds AllBounds => _all;
        public int MarkCount => _marks.Count;

        /// <summary>Point clé i s'il est connu (sinon faux) : position et objet (coffre, marchand, salle verrouillée, sortie, départ).</summary>
        public bool Mark(int i, out Vector3 at, out Object owner)
        {
            at = default;
            owner = null;
            if (i < 0 || i >= _marks.Count || i >= _markKnown.Count || !_markKnown[i] || _marks[i].owner == null)
                return false;
            at = _marks[i].at;
            owner = _marks[i].owner;
            return true;
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        /// <summary>Carte à jour (salles découvertes, points clés) : à chaque image, même quand rien n'est dessiné ici.</summary>
        private void LateUpdate()
        {
            if (Time.time >= _refreshAt || _volumes.Count == 0)
            {
                Refresh();
                _paintedKnown = -1;
            }

            MapDiscovery fog = MapDiscovery.Current;
            if (_mapTex == null || (fog != null ? fog.KnownCount : 0) != _paintedKnown)
                Paint(fog);
        }

        // Performance (étape 8, après playtest : 7 ms d'IMGUI au pic) : les salles découvertes sont peintes dans une texture,
        // refaite seulement quand une salle est découverte ; chaque image n'affiche que cette texture et quelques repères.
        private Texture2D _mapTex;
        private int _paintedKnown = -1;
        private readonly List<bool> _markKnown = new List<bool>();
        private const int TexSize = 512;

        private void Paint(MapDiscovery fog)
        {
            if (_mapTex == null)
            {
                _mapTex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            }

            var px = new Color32[TexSize * TexSize];
            float sx = (TexSize - 2) / Mathf.Max(1f, _all.size.x), sz = (TexSize - 2) / Mathf.Max(1f, _all.size.z);
            foreach (RoomVolume r in _volumes)
            {
                if (r == null || (fog != null && !fog.IsKnown(r)))
                    continue;
                Bounds b = r.WorldBounds();
                bool high = b.min.y > 8f;
                Color32 col = r.IsCorridor ? new Color(0.35f, 0.38f, 0.45f, 0.9f) : high ? new Color(0.55f, 0.6f, 0.75f, 0.9f) : new Color(0.45f, 0.48f, 0.58f, 0.9f);
                if (r.Kind == RoomKind.Depart) col = new Color(0.6f, 0.6f, 0.35f, 0.9f);
                int x0 = 1 + Mathf.Clamp(Mathf.FloorToInt((b.min.x - _all.min.x) * sx), 0, TexSize - 3);
                int x1 = 1 + Mathf.Clamp(Mathf.CeilToInt((b.max.x - _all.min.x) * sx), 0, TexSize - 3);
                int z0 = 1 + Mathf.Clamp(Mathf.FloorToInt((b.min.z - _all.min.z) * sz), 0, TexSize - 3);
                int z1 = 1 + Mathf.Clamp(Mathf.CeilToInt((b.max.z - _all.min.z) * sz), 0, TexSize - 3);
                for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    px[z * TexSize + x] = col;
            }

            _mapTex.SetPixels32(px);
            _mapTex.Apply(false);
            _markKnown.Clear();
            foreach ((Vector3 at, Color _, string _, Object owner) in _marks)
                _markKnown.Add(MarkVisible(owner, at, fog));
            _paintedKnown = fog != null ? fog.KnownCount : 0;
        }

        private void Refresh()
        {
            _volumes.Clear();
            _volumes.AddRange(FindObjectsByType<RoomVolume>(FindObjectsSortMode.None));
            _all = new Bounds(Vector3.zero, Vector3.one);
            bool first = true;
            foreach (RoomVolume r in _volumes)
            {
                if (first) _all = r.WorldBounds(); else _all.Encapsulate(r.WorldBounds());
                first = false;
            }

            _marks.Clear();
            foreach (Chest c in FindObjectsByType<Chest>(FindObjectsSortMode.None))
                _marks.Add((c.transform.position, new Color(1f, 0.8f, 0.2f), "C", c));
            foreach (ArtifactMerchant m in FindObjectsByType<ArtifactMerchant>(FindObjectsSortMode.None))
                _marks.Add((m.transform.position, new Color(0.8f, 0.4f, 1f), "M", m));
            foreach (LockedDoor d in FindObjectsByType<LockedDoor>(FindObjectsSortMode.None))
                _marks.Add((d.transform.position, new Color(1f, 0.5f, 0.15f), "V", d));
            foreach (ExitDoor d in FindObjectsByType<ExitDoor>(FindObjectsSortMode.None))
                _marks.Add((d.transform.position, Color.red, "S", d));
            foreach (RoomVolume r in _volumes)
                if (r.Kind == RoomKind.Depart)
                    _marks.Add((r.transform.position, new Color(1f, 1f, 0.3f), "D", r));
            _refreshAt = Time.time + 5f;
        }

        /// <summary>
        /// Une marque est-elle affichée ? La sortie l'est dès le début de la zone (playtest des étapes 8 et 9) ; les autres
        /// points clés une fois leur salle découverte.
        /// </summary>
        public static bool MarkVisible(Object owner, Vector3 at, MapDiscovery fog) =>
            owner is ExitDoor || fog == null || fog.IsKnownPoint(at + Vector3.up);

        private void OnDestroy()
        {
            if (_mapTex != null)
                Destroy(_mapTex);
        }

        private void Update() => FullMap = Keyboard.current != null && Keyboard.current.tabKey.isPressed;

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || PlayerLook.MenuOpen || _mapTex == null)
                return;
            if (RoundMinimap && !FullMap)
                return;                                                 // mini-carte ronde : dessinée par le HUD de chaque joueur
            MapDiscovery fog = MapDiscovery.Current;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null)
                    continue;
                Camera cam = Interactables.CameraOf(p);
                Rect view = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
                Rect gui = new Rect(view.x, Screen.height - view.yMax, view.width, view.height);
                Draw(p, cam, gui, fog);
            }
        }

        private void Draw(PlayerHealth viewer, Camera cam, Rect view, MapDiscovery fog)
        {
            float side = FullMap ? view.height * 0.85f : view.height * size;
            Rect box = FullMap
                ? new Rect(view.x + (view.width - side) / 2f, view.y + (view.height - side) / 2f, side, side)
                : new Rect(view.x + 16f, view.y + view.height * 0.3f, side, side);
            Vector3 c = FullMap ? _all.center : viewer.transform.position;
            float metres = FullMap ? Mathf.Max(_all.size.x, _all.size.z) + 20f : span;
            float scale = side / metres;

            Vector2 Map(Vector3 w) => new Vector2(box.center.x + (w.x - c.x) * scale, box.center.y - (w.z - c.z) * scale);

            Fill(box, new Color(0f, 0f, 0f, 0.6f));
            GUI.BeginClip(box);
            Vector2 o = box.position;
            // Salles découvertes : la fenêtre de la texture autour du centre (une seule image dessinée).
            var uv = new Rect((c.x - metres / 2f - _all.min.x) / _all.size.x, (c.z - metres / 2f - _all.min.z) / _all.size.z,
                metres / _all.size.x, metres / _all.size.z);
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, box.width, box.height), _mapTex, uv);

            for (int i = 0; i < _marks.Count; i++)
            {
                (Vector3 at, Color color, string label, Object owner) = _marks[i];
                if (owner == null || i >= _markKnown.Count || !_markKnown[i])
                    continue;
                Color col = color;
                if (owner is Chest ch && ch.OpenedFor(viewer.PlayerId)) col = new Color(0.5f, 0.5f, 0.5f);
                if (owner is LockedDoor ld && ld.Open) col = new Color(0.5f, 0.5f, 0.5f);
                if (owner is ExitDoor ed) col = ed.Open || ed.Unlocked ? new Color(0.3f, 1f, 0.4f) : Color.red;
                Vector2 m = Map(at) - o;
                Fill(new Rect(m.x - 6f, m.y - 6f, 12f, 12f), col);
                Letter(new Rect(m.x - 6f, m.y - 7f, 12f, 12f), label);
            }

            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p == viewer || p.Dead)
                    continue;
                Vector2 m = Map(p.transform.position) - o;
                Fill(new Rect(m.x - 4f, m.y - 4f, 8f, 8f), new Color(0.3f, 0.7f, 1f));
            }

            Vector2 me = Map(viewer.transform.position) - o;
            Vector3 fwd = cam != null ? cam.transform.forward : viewer.transform.forward;
            float yaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            Matrix4x4 keep = GUI.matrix;
            GUIUtility.RotateAroundPivot(yaw, me);
            Fill(new Rect(me.x - 4f, me.y - 4f, 8f, 8f), Color.white);
            Fill(new Rect(me.x - 1.5f, me.y - 13f, 3f, 10f), Color.white);
            GUI.matrix = keep;
            GUI.EndClip();
            Letter(new Rect(box.x, box.yMax + 2f, box.width, 18f), FullMap ? "carte (Tab)" : "N ↑   Tab : carte", Color.white);
        }

        private static void Fill(Rect r, Color c)
        {
            Color keep = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = keep;
        }

        private static void Letter(Rect r, string s, Color? color = null)
        {
            if (_letter == null)
            {
                _letter = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _letter.normal.textColor = Color.black;
            }

            _letter.normal.textColor = color ?? Color.black;
            GUI.Label(r, s, _letter);
        }
    }
}
