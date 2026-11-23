using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// Couche des ennemis sur la carte (D-101, nuit 4, point 6) : goules en point rouge, traqueur en symbole, dans la bulle ronde et
    /// dans la carte ouverte, seulement quand CE joueur les a dans son champ de vision (MinimapVision : cône de sa caméra + 5°, 60 m,
    /// ligne de vue libre). Hors du champ, la marque s'efface en douceur (0.5 s) à la dernière position vue. En écran partagé, chaque
    /// vue montre ce que son joueur voit. Sprites de la DA (HudSkinSettings.iconEnemy / iconStalker) ou formes générées.
    /// </summary>
    public sealed partial class PlayerHud
    {
        private sealed class Sighting
        {
            public Transform Root;
            public Collider Collider;
            public Damageable Damageable;
            public Vector3 At;
            public float Alpha;
            public bool Stalker, Seen;
        }

        private readonly Dictionary<Transform, Sighting> _sightings = new Dictionary<Transform, Sighting>();
        private readonly List<Transform> _gone = new List<Transform>();
        private readonly List<Image> _enemyIconsMini = new List<Image>(), _enemyIconsFull = new List<Image>();
        private int _sightFrame = -1;
        private float _stalkerSearchAt;
        private static Sprite _dotSprite, _stalkerSprite;

        /// <summary>Ennemis actuellement marqués sur la carte de ce joueur (vus, ou en train de s'effacer) : tests, debug.</summary>
        public int EnemyMarks => _sightings.Count;

        /// <summary>Met à jour, une fois par image, la liste des ennemis vus par ce joueur et le fondu de ceux qui ne le sont plus.</summary>
        private void UpdateEnemySight()
        {
            if (_sightFrame == Time.frameCount || _skin == null)
                return;
            _sightFrame = Time.frameCount;
            foreach (Sighting s in _sightings.Values)
                s.Seen = false;

            float margin = _skin.enemySightMargin, range = _skin.enemySightRange;
            if (_health != null && !_health.Dead)
            {
                foreach (Ghoul g in Ghoul.All)
                {
                    if (g == null || !g.isActiveAndEnabled)
                        continue;
                    Consider(g.transform, false, margin, range);
                }

                if (_stalker == null && Time.time >= _stalkerSearchAt)
                {
                    _stalkerSearchAt = Time.time + 2f;
                    _stalker = FindFirstObjectByType<Stalker>();
                }

                if (_stalker != null && _stalker.isActiveAndEnabled)
                    Consider(_stalker.transform, true, margin, range);
            }

            float fade = Mathf.Max(0.01f, _skin.enemyFade);
            _gone.Clear();
            foreach (KeyValuePair<Transform, Sighting> kv in _sightings)
            {
                Sighting s = kv.Value;
                if (s.Root == null || (s.Damageable != null && s.Damageable.Dead && !s.Seen))
                {
                    _gone.Add(kv.Key);
                    continue;
                }

                if (!s.Seen)
                {
                    s.Alpha -= Time.deltaTime / fade;
                    if (s.Alpha <= 0f)
                        _gone.Add(kv.Key);
                }
            }

            foreach (Transform t in _gone)
                _sightings.Remove(t);
            if (_parts.Count > 256)
                _parts.Clear();                                           // goules détruites et recréées : on ne garde pas tout
        }

        private readonly Dictionary<Transform, (Collider col, Damageable dmg)> _parts = new Dictionary<Transform, (Collider, Damageable)>();

        private void Consider(Transform root, bool stalker, float margin, float range)
        {
            _sightings.TryGetValue(root, out Sighting s);
            if (!_parts.TryGetValue(root, out (Collider col, Damageable dmg) parts))
                _parts[root] = parts = (root.GetComponentInChildren<Collider>(), root.GetComponentInParent<Damageable>());   // cherchés une fois par ennemi
            Collider col = parts.col;
            Damageable dmg = parts.dmg;
            if (dmg != null && dmg.Dead)
                return;
            Vector3 center = col != null ? col.bounds.center : root.position + Vector3.up;
            if (!MinimapVision.Sees(_worldCamera, _health, center, root, margin, range))
                return;
            if (s == null)
            {
                s = new Sighting { Root = root, Collider = col, Damageable = dmg, Stalker = stalker };
                _sightings[root] = s;
            }

            s.Seen = true;
            s.Alpha = 1f;
            s.At = center;
        }

        /// <summary>
        /// Place les marques d'ennemis dans un parent (bulle ou carte ouverte) : P convertit une position du monde en position dans ce
        /// parent ; clipRadius &gt; 0 cache les marques hors du rond.
        /// </summary>
        private void DrawEnemyMarks(List<Image> pool, RectTransform parent, System.Func<Vector3, Vector2> P, float clipRadius, float scale)
        {
            if (parent == null)
                return;
            int used = 0;
            foreach (Sighting s in _sightings.Values)
            {
                Vector2 pos = P(s.At);
                float size = (s.Stalker ? _skin.stalkerIconSize : _skin.enemyIconSize) * scale;
                if (clipRadius > 0f && pos.magnitude > clipRadius + size * 0.5f)
                    continue;
                Image img = PooledEnemy(pool, parent, used++);
                Sprite custom = s.Stalker ? _skin.iconStalker : _skin.iconEnemy;
                img.sprite = custom != null ? custom : s.Stalker ? StalkerSprite() : DotSprite();
                Color tint = custom != null ? Color.white : s.Stalker ? _skin.stalkerColor : _skin.enemyColor;
                tint.a *= Mathf.Clamp01(s.Alpha);
                img.color = tint;
                img.rectTransform.sizeDelta = Vector2.one * size;
                img.rectTransform.anchoredPosition = pos;
            }

            for (int i = used; i < pool.Count; i++)
                pool[i].enabled = false;
        }

        private static Image PooledEnemy(List<Image> pool, RectTransform parent, int i)
        {
            while (pool.Count <= i)
            {
                Image img = MapIcon(parent, $"Ennemi vu {pool.Count + 1}", null, 10f);
                pool.Add(img);
            }

            Image e = pool[i];
            if (e.transform.parent != parent)
                e.transform.SetParent(parent, false);
            e.enabled = true;
            return e;
        }

        /// <summary>Disque anticrénelé (goule) : blanc, teinté par la couleur de l'habillage.</summary>
        private static Sprite DotSprite()
        {
            if (_dotSprite != null)
                return _dotSprite;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave, name = "Carte : point ennemi" };
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f, r = n * 0.5f - 1.5f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r + 0.5f - d) * 255f));
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            _dotSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _dotSprite;
        }

        /// <summary>Symbole du traqueur : triangle pointe en haut percé d'un œil (blanc, teinté rouge sombre).</summary>
        private static Sprite StalkerSprite()
        {
            if (_stalkerSprite != null)
                return _stalkerSprite;
            const int n = 48;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave, name = "Carte : traqueur" };
            var px = new Color32[n * n];
            var a = new Vector2(n * 0.5f, n - 3f);
            var b = new Vector2(3f, 5f);
            var cc = new Vector2(n - 3f, 5f);
            var eye = new Vector2(n * 0.5f, n * 0.38f);
            float eyeR = n * 0.11f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                // Couverture par sur-échantillonnage 3 × 3 : triangle plein moins l'œil.
                int inside = 0;
                for (int sy = 0; sy < 3; sy++)
                for (int sx = 0; sx < 3; sx++)
                {
                    var p = new Vector2(x + (sx + 0.5f) / 3f, y + (sy + 0.5f) / 3f);
                    if (InTriangle(p, a, b, cc) && (p - eye).magnitude > eyeR)
                        inside++;
                }

                px[y * n + x] = new Color32(255, 255, 255, (byte)(inside * 255 / 9));
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            _stalkerSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
            return _stalkerSprite;
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float s1 = Cross(b - a, p - a), s2 = Cross(c - b, p - b), s3 = Cross(a - c, p - c);
            return (s1 >= 0f && s2 >= 0f && s3 >= 0f) || (s1 <= 0f && s2 <= 0f && s3 <= 0f);
        }

        private static float Cross(Vector2 u, Vector2 v) => u.x * v.y - u.y * v.x;
    }
}
