using System.Collections.Generic;
using ItHunts.Economy;
using ItHunts.Level;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// Carte ouverte (Tab / croix haut, maintenu), dans la vue de chaque joueur (écran partagé compris) : toute l'emprise du niveau,
    /// seules les salles et les couloirs découverts sont peints (brouillard de guerre, MapDiscovery : par joueur, partagé en coop),
    /// icônes de la DA (coffres, coffre rare, marchand, salle d'âmes, sortie, alliés, allié mort) à la bonne taille, flèche du joueur,
    /// légende. Habillage provisoire (cadre sombre, filets) en attendant le lot B de la DA (cadre, parchemin, brume).
    /// </summary>
    public sealed partial class PlayerHud
    {
        private RectTransform _fullMap, _fullMapArea, _fullMapIcons;
        private RawImage _fullMapImage;
        private Image _fullArrow;
        private readonly List<Image> _fullMarks = new List<Image>(), _fullAllies = new List<Image>();
        private ItHunts.Input.PlayerInputReader _reader;

        /// <summary>Carte ouverte affichée pour ce joueur (tests, captures).</summary>
        public bool FullMapOpen => _fullMap != null && _fullMap.gameObject.activeSelf;
        public RectTransform FullMapRoot => _fullMap;
        /// <summary>Forcer la carte ouverte (captures, tests) ; sinon la touche du joueur.</summary>
        public bool ForceFullMap { get; set; }

        private static readonly Color MapPanel = new Color(0.045f, 0.05f, 0.075f, 0.94f), MapLine = new Color(0.62f, 0.6f, 0.68f, 0.9f),
            MapFog = new Color(0.02f, 0.022f, 0.035f, 1f);

        private void BuildFullMap()
        {
            if (_skin == null)
                return;
            _fullMap = Rect("Carte ouverte", _root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(_fullMap);
            NewImage("Voile", _fullMap, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.55f)).rectTransform.anchorMin = Vector2.zero;
            Stretch(_fullMap.GetChild(0) as RectTransform);
            RectTransform panel = Rect("Cadre (provisoire)", _fullMap, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 900f));
            Frame(panel, MapPanel, 3f, 14f);
            Text title = NewText("Titre", panel, new Vector2(0f, -28f), new Vector2(600f, 44f), 34, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.font = _skin.titleFont != null ? _skin.titleFont : _font;
            title.text = "Carte";
            title.color = new Color(0.86f, 0.84f, 0.9f);
            _fullMapArea = Rect("Zone de la carte", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(760f, 700f));
            NewImage("Brouillard", _fullMapArea, Vector2.zero, Vector2.zero, MapFog);
            Stretch(_fullMapArea.GetChild(0) as RectTransform);
            RectTransform img = Rect("Salles découvertes", _fullMapArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _fullMapImage = img.gameObject.AddComponent<RawImage>();
            _fullMapImage.raycastTarget = false;
            _fullMapIcons = Rect("Icônes", img, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(_fullMapIcons);
            _fullArrow = MapIcon(_fullMapIcons, "Joueur (flèche)", _skin.iconArrow, MapIconSize * 1.15f);
            Legend(panel);
            _fullMap.gameObject.SetActive(false);
        }

        private float MapIconSize => _skin != null ? _skin.mapIconSize : 30f;

        /// <summary>Panneau sombre et deux filets (provisoire, propre ; le lot B de la DA le remplacera).</summary>
        private void Frame(RectTransform panel, Color fill, float line, float inset)
        {
            Image back = NewImage("Fond", panel, Vector2.zero, Vector2.zero, fill);
            Stretch(back.rectTransform);
            foreach (float d in new[] { 0f, inset })
            {
                foreach ((Vector2 a, Vector2 b, bool horizontal) in new[]
                         {
                             (new Vector2(0f, 1f), new Vector2(1f, 1f), true), (new Vector2(0f, 0f), new Vector2(1f, 0f), true),
                             (new Vector2(0f, 0f), new Vector2(0f, 1f), false), (new Vector2(1f, 0f), new Vector2(1f, 1f), false),
                         })
                {
                    Image l = NewImage("Filet", panel, Vector2.zero, Vector2.zero, d > 0f ? new Color(MapLine.r, MapLine.g, MapLine.b, 0.45f) : MapLine);
                    RectTransform r = l.rectTransform;
                    r.anchorMin = a;
                    r.anchorMax = b;
                    r.pivot = new Vector2(a.x, a.y);
                    float w = d > 0f ? 1.5f : line;
                    Vector2 inward = new Vector2(a.x == b.x ? (a.x < 0.5f ? d : -d) : 0f, a.y == b.y ? (a.y < 0.5f ? d : -d) : 0f);
                    r.anchoredPosition = inward;
                    r.sizeDelta = horizontal ? new Vector2(-2f * d, w) : new Vector2(w, -2f * d);
                }
            }
        }

        private void Legend(RectTransform panel)
        {
            var items = new List<(Sprite, string)>
            {
                (_skin.iconChest, "coffre"), (_skin.iconChestRare, "coffre rare"), (_skin.iconMerchant, "marchand"),
                (_skin.iconSouls, "salle d'âmes"), (_skin.iconExitLocked, "sortie"), (_skin.allyFox, "allié"), (_skin.iconAllyDead, "allié mort"),
            };
            items.RemoveAll(i => i.Item1 == null);
            // Deux rangées de quatre (lisible aussi dans une demi-vue d'écran partagé).
            const int perRow = 4;
            float step = 190f;
            for (int i = 0; i < items.Count; i++)
            {
                (Sprite s, string label) = items[i];
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, items.Count - row * perRow);
                float x = (col - (inRow - 1) * 0.5f) * step;
                RectTransform cell = Rect("Légende : " + label, panel, new Vector2(0.5f, 0f), new Vector2(x, 62f - row * 30f), new Vector2(step, 28f));
                Image icon = MapIcon(cell, "Icône", s, 24f);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(18f, 0f);
                Text t = NewText("Texte", cell, new Vector2(18f, 0f), new Vector2(step - 40f, 28f), 18, TextAnchor.MiddleLeft);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                t.text = label;
                t.color = new Color(0.8f, 0.8f, 0.86f);
            }
        }

        private static Image MapIcon(RectTransform parent, string name, Sprite sprite, float size)
        {
            RectTransform r = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>
        /// Taille de la carte dans la zone (pur, testable) : l'emprise du niveau (x, z) tient dans la zone sans déformation.
        /// </summary>
        public static Vector2 FitMap(Vector2 area, Vector2 worldSize)
        {
            float wx = Mathf.Max(1f, worldSize.x), wz = Mathf.Max(1f, worldSize.y);
            float k = Mathf.Min(area.x / wx, area.y / wz);
            return new Vector2(wx * k, wz * k);
        }

        /// <summary>Icône d'un point clé (pur vis-à-vis de l'affichage, testable) ; null : rien à montrer.</summary>
        public static Sprite MarkIcon(HudSkinSettings skin, Object owner, int playerId) => skin == null ? null : owner switch
        {
            Chest ch => ch.OpenedFor(playerId) ? skin.iconChestOpen : ch.Rare && skin.iconChestRare != null ? skin.iconChestRare : skin.iconChest,
            ArtifactMerchant => skin.iconMerchant,
            LockedDoor => skin.iconSouls,
            ExitDoor ed => ed.Open || ed.Unlocked ? skin.iconExitOpen : skin.iconExitLocked,
            _ => null,
        };

        private Sprite AllyIcon(PlayerHealth p)
        {
            CharacterDefinition cd = p.TryGetComponent(out ItHunts.Artifacts.ArtifactHolder holder) ? holder.Character : null;
            return p.Dead ? _skin.iconAllyDead : cd == null ? _skin.allyOwl : cd.id switch
            {
                "renard" => _skin.allyFox,
                "koala" => _skin.allyKoala,
                "cameleon" => _skin.allyChameleon,
                _ => _skin.allyOwl,
            };
        }

        /// <summary>Touche de la carte maintenue par ce joueur (Tab / croix haut).</summary>
        private bool WantsFullMap()
        {
            if (ForceFullMap)
                return true;
            if (_reader == null)
                _reader = GetComponent<ItHunts.Input.PlayerInputReader>();
            return _reader != null && _reader.MapHeld && !PlayerLook.MenuOpen;
        }

        private void UpdateFullMap()
        {
            if (_fullMap == null || _health == null)
                return;
            MinimapHud mm = MinimapHud.Current;
            Texture tex = mm != null ? mm.TextureFor(_health.PlayerId) : null;
            bool on = tex != null && WantsFullMap();
            if (_fullMap.gameObject.activeSelf != on)
                _fullMap.gameObject.SetActive(on);
            if (!on)
                return;

            // La zone de la carte suit la vue du joueur (écran partagé : plus étroite).
            Rect view = _root.rect;
            float side = Mathf.Min(view.width, view.height) * 0.86f;
            var panel = (RectTransform)_fullMap.Find("Cadre (provisoire)");
            panel.sizeDelta = new Vector2(Mathf.Min(view.width * 0.94f, side * 1.05f), side);
            _fullMapArea.sizeDelta = new Vector2(panel.sizeDelta.x - 70f, side - 190f);
            _fullMapArea.anchoredPosition = new Vector2(0f, 26f);
            Bounds all = mm.AllBounds;
            Vector2 size = FitMap(_fullMapArea.sizeDelta, new Vector2(all.size.x, all.size.z));
            _fullMapImage.rectTransform.sizeDelta = size;
            _fullMapImage.texture = tex;
            _fullMapImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            Vector2 P(Vector3 w) => new Vector2(((w.x - all.min.x) / Mathf.Max(1f, all.size.x) - 0.5f) * size.x,
                ((w.z - all.min.z) / Mathf.Max(1f, all.size.z) - 0.5f) * size.y);

            int used = 0;
            for (int i = 0; i < mm.MarkCount; i++)
            {
                if (!mm.MarkFor(i, _health.PlayerId, out Vector3 at, out Object owner))
                    continue;
                Sprite s = MarkIcon(_skin, owner, _health.PlayerId);
                if (s == null)
                    continue;
                Image img = PooledIn(_fullMarks, used++, MapIconSize, "Point clé");
                img.sprite = s;
                img.rectTransform.anchoredPosition = P(at);
            }

            for (int i = used; i < _fullMarks.Count; i++)
                _fullMarks[i].enabled = false;

            used = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p == _health)
                    continue;
                Image img = PooledIn(_fullAllies, used++, p.Dead ? MapIconSize : MapIconSize * 0.8f, "Allié");
                img.sprite = AllyIcon(p);
                img.rectTransform.anchoredPosition = P(p.transform.position);
            }

            for (int i = used; i < _fullAllies.Count; i++)
                _fullAllies[i].enabled = false;

            _fullArrow.rectTransform.SetAsLastSibling();
            _fullArrow.rectTransform.anchoredPosition = P(_health.transform.position);
            Vector3 fwd = _worldCamera != null ? _worldCamera.transform.forward : _health.transform.forward;
            _fullArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg);
        }

        private Image PooledIn(List<Image> pool, int i, float size, string name)
        {
            while (pool.Count <= i)
                pool.Add(MapIcon(_fullMapIcons, $"{name} {pool.Count + 1}", null, size));
            Image img = pool[i];
            img.enabled = true;
            img.rectTransform.sizeDelta = Vector2.one * size;
            return img;
        }
    }
}
