using System.Collections.Generic;
using ItHunts.Economy;
using ItHunts.Level;
using ItHunts.Level.Acts;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Hud
{
    /// <summary>
    /// Minimap (D-053, D-101), une texture par joueur (écran partagé compris), nord en haut : salles découvertes (MapDiscovery), le
    /// reste transparent. Dans un acte (ActLevel.Plan), la texture est peinte à l'encre depuis le plan (ActMapPainter : sols, murs
    /// nets, portes, terrasses, bassins, fosses, rampes) ; sans plan (Level_01, Test_Arena), rectangles des RoomVolume comme avant.
    /// La même texture nourrit la bulle ronde du HUD habillé et la carte ouverte (PlayerHud) ; points clés une fois découverts :
    /// coffres (gris une fois ouverts par ce joueur), marchand, salle verrouillée, sortie, départ. Sans habillage : dessin IMGUI
    /// greybox (Tab maintenu : toute la carte).
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
        private ItHunts.Enemies.Stalker _stalker;
        private float _stalkerSearchAt;

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

        /// <summary>Plan de l'acte en cours (null : scène sans plan, dessin par RoomVolume).</summary>
        private ActPlan _plan;
        /// <summary>Salle du plan sous chaque volume (la découverte reste par volume, donc par salle : un volume connu = salle connue).</summary>
        private readonly Dictionary<RoomVolume, int> _volumeRoom = new Dictionary<RoomVolume, int>();
        private readonly HashSet<int> _knownRooms = new HashSet<int>();
        private int _texSize = FallbackTexSize;
        private Color32[] _buffer;

        /// <summary>Vrai quand la carte est peinte à l'encre depuis le plan de l'acte : la carte ouverte l'affiche telle quelle (D-101).</summary>
        public bool InkPainted => _plan != null;
        /// <summary>Côté (px) des textures de la carte.</summary>
        public int TextureSize => _texSize;

        private readonly Dictionary<int, (Texture2D tex, int painted, List<bool> known)> _perPlayer =
            new Dictionary<int, (Texture2D, int, List<bool>)>();

        /// <summary>Texture au bon côté : la même si elle convient, sinon une neuve (l'ancienne est détruite).</summary>
        private Texture2D Sized(Texture2D tex, string name)
        {
            if (tex != null && tex.width == _texSize)
                return tex;
            if (tex != null)
                Destroy(tex);
            return new Texture2D(_texSize, _texSize, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
        }

        /// <summary>
        /// Carte de ce joueur : coop partagée, la carte commune ; sinon la sienne (peinte quand il découvre un volume).
        /// </summary>
        public Texture TextureFor(int playerId)
        {
            MapDiscovery fog = MapDiscovery.Current;
            if (fog == null || fog.SharedInCoop || _all.size.sqrMagnitude < 1f)
                return _mapTex;
            if (!_perPlayer.TryGetValue(playerId, out var e) || e.tex == null || e.tex.width != _texSize || e.painted != fog.KnownCountFor(playerId))
            {
                Texture2D tex = Sized(e.tex, "Carte du joueur " + playerId);
                var known = e.known ?? new List<bool>();
                PaintInto(tex, r => fog.IsKnownFor(r, playerId));
                known.Clear();
                foreach ((Vector3 at, Color _, string _, Object owner) in _marks)
                    known.Add(owner is ExitDoor || fog.IsKnownPointFor(at + Vector3.up, playerId));
                _perPlayer[playerId] = (tex, fog.KnownCountFor(playerId), known);
                e = _perPlayer[playerId];
            }

            return e.tex;
        }

        // ---------- Pinceau de dévoilement (carte ouverte, UI-HANDOFF § 2 bis) ----------

        /// <summary>Pinceau de la DA (Carte_Brume_Pinceau), posé par le HUD habillé ; null : disque doux.</summary>
        public static Texture BrushSource { get; set; }
        /// <summary>Rayon du pinceau (m) et intervalle entre deux coups (s).</summary>
        public static float BrushMeters = 16f, BrushInterval = 0.15f;
        private const int MaskSize = 256, StampSize = 32;
        private readonly Dictionary<int, Texture2D> _masks = new Dictionary<int, Texture2D>();
        private readonly Dictionary<int, Color32[]> _maskPx = new Dictionary<int, Color32[]>();
        private float[] _stamp;
        private float _nextStamp;

        /// <summary>
        /// Masque de dévoilement de ce joueur (r : 0 dans la brume, 1 dévoilé), sur l'emprise du niveau : coop partagée, le masque commun
        /// (tous les joueurs y peignent) ; sinon le sien.
        /// </summary>
        public Texture BrushFor(int playerId)
        {
            MapDiscovery fog = MapDiscovery.Current;
            int key = fog == null || fog.SharedInCoop ? int.MinValue : playerId;
            return _masks.TryGetValue(key, out Texture2D t) ? t : Mask(key);
        }

        private Texture2D Mask(int key)
        {
            if (_masks.TryGetValue(key, out Texture2D t))
                return t;
            t = new Texture2D(MaskSize, MaskSize, TextureFormat.R8, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Dévoilement " + key };
            var px = new Color32[MaskSize * MaskSize];
            t.SetPixels32(px);
            t.Apply(false);
            _masks[key] = t;
            _maskPx[key] = px;
            return t;
        }

        /// <summary>Le pinceau en petit (StampSize²), lu une fois sur la texture de la DA (rendu dans une RenderTexture).</summary>
        private float[] Stamp()
        {
            if (_stamp != null)
                return _stamp;
            _stamp = new float[StampSize * StampSize];
            if (BrushSource != null)
            {
                RenderTexture rt = RenderTexture.GetTemporary(StampSize, StampSize, 0);
                Graphics.Blit(BrushSource, rt);
                RenderTexture keep = RenderTexture.active;
                RenderTexture.active = rt;
                var read = new Texture2D(StampSize, StampSize, TextureFormat.RGBA32, false);
                read.ReadPixels(new Rect(0, 0, StampSize, StampSize), 0, 0);
                read.Apply(false);
                RenderTexture.active = keep;
                RenderTexture.ReleaseTemporary(rt);
                Color32[] c = read.GetPixels32();
                for (int i = 0; i < c.Length; i++)
                    _stamp[i] = Mathf.Max(c[i].r, c[i].a) / 255f * (c[i].a / 255f);
                Destroy(read);
            }
            else
                for (int y = 0; y < StampSize; y++)
                for (int x = 0; x < StampSize; x++)
                {
                    float d = new Vector2(x - StampSize / 2f + 0.5f, y - StampSize / 2f + 0.5f).magnitude / (StampSize / 2f);
                    _stamp[y * StampSize + x] = Mathf.Clamp01(1f - d * d);
                }

            return _stamp;
        }

        /// <summary>Un coup de pinceau au point (UV de la carte) dans ce masque (pur sur les pixels, testable).</summary>
        public static void StampAt(Color32[] px, int size, float[] stamp, int stampSize, Vector2 uv, float radiusUv)
        {
            int r = Mathf.Max(1, Mathf.RoundToInt(radiusUv * size));
            int cx = Mathf.RoundToInt(uv.x * size), cy = Mathf.RoundToInt(uv.y * size);
            for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
            {
                int px0 = cx + x, py0 = cy + y;
                if (px0 < 0 || py0 < 0 || px0 >= size || py0 >= size)
                    continue;
                int sx = Mathf.Clamp(Mathf.FloorToInt((x + r) / (2f * r + 1f) * stampSize), 0, stampSize - 1);
                int sy = Mathf.Clamp(Mathf.FloorToInt((y + r) / (2f * r + 1f) * stampSize), 0, stampSize - 1);
                byte v = (byte)Mathf.RoundToInt(stamp[sy * stampSize + sx] * 255f);
                int i = py0 * size + px0;
                if (v > px[i].r)
                    px[i].r = v;
            }
        }

        private void PaintBrush()
        {
            if (Time.time < _nextStamp || _all.size.x < 1f || _all.size.z < 1f)
                return;
            _nextStamp = Time.time + BrushInterval;
            float[] stamp = Stamp();
            float radius = BrushMeters / Mathf.Max(_all.size.x, _all.size.z);
            var dirty = new HashSet<int>();
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                Vector3 w = p.transform.position;
                var uv = new Vector2((w.x - _all.min.x) / _all.size.x, (w.z - _all.min.z) / _all.size.z);
                foreach (int key in new[] { int.MinValue, p.PlayerId })
                {
                    Mask(key);
                    StampAt(_maskPx[key], MaskSize, stamp, StampSize, uv, radius);
                    dirty.Add(key);
                }
            }

            foreach (int key in dirty)
            {
                _masks[key].SetPixels32(_maskPx[key]);
                _masks[key].Apply(false);
            }
        }

        /// <summary>Point clé i sur la carte de ce joueur.</summary>
        public bool MarkFor(int i, int playerId, out Vector3 at, out Object owner)
        {
            MapDiscovery fog = MapDiscovery.Current;
            if (fog == null || fog.SharedInCoop || !_perPlayer.TryGetValue(playerId, out var e) || e.known == null)
                return Mark(i, out at, out owner);
            at = default;
            owner = null;
            if (i < 0 || i >= _marks.Count || i >= e.known.Count || !e.known[i] || _marks[i].owner == null)
                return false;
            at = _marks[i].at;
            owner = _marks[i].owner;
            return true;
        }
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
            if ((Time.time >= _refreshAt || _volumes.Count == 0) && Refresh())
            {
                // Quelque chose a changé (volumes, plan, points clés) : tout repeindre (sinon on garde les textures : la peinture à
                // l'encre d'un acte coûte quelques ms).
                _paintedKnown = -1;
                foreach (int k in new List<int>(_perPlayer.Keys))
                    _perPlayer[k] = (_perPlayer[k].tex, -1, _perPlayer[k].known);
            }

            MapDiscovery fog = MapDiscovery.Current;
            if (_mapTex == null || (fog != null ? fog.KnownCount : 0) != _paintedKnown)
                Paint(fog);
            PaintBrush();
        }

        // Performance (étape 8, après playtest : 7 ms d'IMGUI au pic) : les salles découvertes sont peintes dans une texture,
        // refaite seulement quand une salle est découverte ; chaque image n'affiche que cette texture et quelques repères.
        private Texture2D _mapTex;
        private int _paintedKnown = -1;
        private readonly List<bool> _markKnown = new List<bool>();
        /// <summary>Côté de la texture sans plan (rectangles des RoomVolume) ; avec un plan : ActMapPainter.TextureSize (≥ 4 px/m).</summary>
        private const int FallbackTexSize = 512;

        private void Paint(MapDiscovery fog)
        {
            _mapTex = Sized(_mapTex, "Carte commune");
            PaintInto(_mapTex, r => fog == null || fog.IsKnown(r));
            _markKnown.Clear();
            foreach ((Vector3 at, Color _, string _, Object owner) in _marks)
                _markKnown.Add(MarkVisible(owner, at, fog));
            _paintedKnown = fog != null ? fog.KnownCount : 0;
        }

        private void PaintInto(Texture2D tex, System.Func<RoomVolume, bool> known)
        {
            int size = tex.width;
            if (_buffer == null || _buffer.Length != size * size)
                _buffer = new Color32[size * size];
            Color32[] px = _buffer;
            if (_plan != null)
            {
                // Acte : encre déduite du plan ; une salle est connue dès qu'un de ses volumes l'est (brume par salle).
                _knownRooms.Clear();
                foreach (KeyValuePair<RoomVolume, int> kv in _volumeRoom)
                    if (kv.Key != null && kv.Value >= 0 && known(kv.Key))
                        _knownRooms.Add(kv.Value);
                ActMapPainter.Paint(px, _plan, _all, size, _knownRooms.Contains);
            }
            else
                PaintVolumes(px, size, known);

            tex.SetPixels32(px);
            tex.Apply(false);
        }

        /// <summary>Repli sans plan (Level_01, Test_Arena) : un rectangle teinté par volume découvert.</summary>
        private void PaintVolumes(Color32[] px, int size, System.Func<RoomVolume, bool> known)
        {
            System.Array.Clear(px, 0, px.Length);
            float sx = (size - 2) / Mathf.Max(1f, _all.size.x), sz = (size - 2) / Mathf.Max(1f, _all.size.z);
            foreach (RoomVolume r in _volumes)
            {
                if (r == null || !known(r))
                    continue;
                Bounds b = r.WorldBounds();
                bool high = b.min.y > 8f;
                Color32 col = r.IsCorridor ? new Color(0.35f, 0.38f, 0.45f, 0.9f) : high ? new Color(0.55f, 0.6f, 0.75f, 0.9f) : new Color(0.45f, 0.48f, 0.58f, 0.9f);
                if (r.Kind == RoomKind.Depart) col = new Color(0.6f, 0.6f, 0.35f, 0.9f);
                int x0 = 1 + Mathf.Clamp(Mathf.FloorToInt((b.min.x - _all.min.x) * sx), 0, size - 3);
                int x1 = 1 + Mathf.Clamp(Mathf.CeilToInt((b.max.x - _all.min.x) * sx), 0, size - 3);
                int z0 = 1 + Mathf.Clamp(Mathf.FloorToInt((b.min.z - _all.min.z) * sz), 0, size - 3);
                int z1 = 1 + Mathf.Clamp(Mathf.CeilToInt((b.max.z - _all.min.z) * sz), 0, size - 3);
                for (int z = z0; z <= z1; z++)
                for (int x = x0; x <= x1; x++)
                    px[z * size + x] = col;
            }
        }

        /// <summary>Relit volumes, plan et points clés ; vrai si quelque chose a changé depuis la dernière fois (repeindre).</summary>
        private bool Refresh()
        {
            int volumesBefore = _volumes.Count, marksBefore = _marks.Count;
            ActPlan planBefore = _plan;
            _volumes.Clear();
            _volumes.AddRange(FindObjectsByType<RoomVolume>(FindObjectsSortMode.None));
            ActLevel act = ActLevel.Current;
            _plan = act != null && act.Plan != null && act.Plan.Rooms.Count > 0 ? act.Plan : null;
            _volumeRoom.Clear();
            if (_plan != null)
            {
                // Emprise carrée du plan (pixels isotropes : traits d'égale épaisseur) et résolution suffisante (≥ 4 px/m).
                _all = ActMapPainter.BoundsOf(_plan);
                _texSize = ActMapPainter.TextureSize(_all);
                foreach (RoomVolume r in _volumes)
                    if (r != null)
                        _volumeRoom[r] = _plan.RoomAt(r.transform.position);
            }
            else
            {
                _texSize = FallbackTexSize;
                _all = new Bounds(Vector3.zero, Vector3.one);
                bool first = true;
                foreach (RoomVolume r in _volumes)
                {
                    if (first) _all = r.WorldBounds(); else _all.Encapsulate(r.WorldBounds());
                    first = false;
                }
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
            return _mapTex == null || volumesBefore != _volumes.Count || marksBefore != _marks.Count || planBefore != _plan;
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
            foreach (var e in _perPlayer.Values)
                if (e.tex != null)
                    Destroy(e.tex);
            foreach (Texture2D m in _masks.Values)
                if (m != null)
                    Destroy(m);
        }

        /// <summary>Carte entière IMGUI (sans habillage seulement) : un joueur maintient sa touche de carte.</summary>
        private void Update()
        {
            bool any = false;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && p.TryGetComponent(out ItHunts.Input.PlayerInputReader r) && r.MapHeld)
                    any = true;
            FullMap = any;
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || PlayerLook.MenuOpen || _mapTex == null)
                return;
            if (RoundMinimap)
                return;                                                 // HUD habillé : mini-carte ronde et carte ouverte par joueur
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

            // Ennemis dans le champ de vision de ce joueur (D-101, greybox sans fondu) : goules en rouge, traqueur en rouge sombre.
            foreach (ItHunts.Enemies.Ghoul g in ItHunts.Enemies.Ghoul.All)
            {
                if (g == null || !g.isActiveAndEnabled || !MinimapVision.Sees(cam, viewer, g.Position + Vector3.up, g.transform))
                    continue;
                Vector2 m = Map(g.Position) - o;
                Fill(new Rect(m.x - 3f, m.y - 3f, 6f, 6f), new Color(0.85f, 0.12f, 0.1f));
            }

            if (_stalker == null && Time.time >= _stalkerSearchAt)
            {
                _stalkerSearchAt = Time.time + 2f;
                _stalker = FindFirstObjectByType<ItHunts.Enemies.Stalker>();
            }

            if (_stalker != null && _stalker.isActiveAndEnabled && MinimapVision.Sees(cam, viewer, _stalker.transform.position + Vector3.up * 4f, _stalker.transform))
            {
                Vector2 m = Map(_stalker.transform.position) - o;
                Fill(new Rect(m.x - 6f, m.y - 6f, 12f, 12f), new Color(0.55f, 0.05f, 0.08f));
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
