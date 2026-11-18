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
    /// Carte ouverte (Tab / croix haut, maintenu), dans la vue de chaque joueur (écran partagé compris), habillée par le lot B de la
    /// DA (UI-HANDOFF § 2 bis) : cadre plein écran en 9 tranches, parchemin sombre répété, salles et couloirs découverts tracés à
    /// l'encre claire, icônes de la mini-carte, brume sur la zone non découverte (dévoilée par les salles découvertes et par le
    /// pinceau peint au passage des joueurs ; découverte par joueur, partagée en coop), vignette, titre « Carte » en gothique, rose
    /// des vents, légende dans son panneau. Ordre : fond → carte → brume → vignette → cadre → légende → rose.
    /// </summary>
    public sealed partial class PlayerHud
    {
        private RectTransform _fullMap, _fullMapArea, _fullMapIcons, _legend;
        private RawImage _fullMapImage, _fullFog;
        private Image _fullArrow;
        private readonly List<Image> _fullMarks = new List<Image>(), _fullAllies = new List<Image>();
        private ItHunts.Input.PlayerInputReader _reader;

        /// <summary>Carte ouverte affichée pour ce joueur (tests, captures).</summary>
        public bool FullMapOpen => _fullMap != null && _fullMap.gameObject.activeSelf;
        public RectTransform FullMapRoot => _fullMap;
        /// <summary>Forcer la carte ouverte (captures, tests) ; sinon la touche du joueur.</summary>
        public bool ForceFullMap { get; set; }

        /// <summary>Bordure du cadre (px 1080p) : du bord de l'écran au rail intérieur.</summary>
        public const float MapFrameBorder = 130f;

        private static readonly Color MapBand = new Color(0.035f, 0.035f, 0.05f, 0.95f), LegendText = new Color(0.86f, 0.83f, 0.78f);

        private void BuildFullMap()
        {
            if (_skin == null)
                return;
            MinimapHud.BrushSource = _skin.mapFogBrush != null ? _skin.mapFogBrush.texture : null;
            _fullMap = Rect("Carte ouverte", _root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(_fullMap);

            // Voile opaque : rien du HUD ni du monde ne transparaît sous le cadre (bande sombre de la DA à 86 %).
            Layer2("Voile", _fullMap, null, Image.Type.Simple, Color.black).color = Color.black;
            // Fond : parchemin répété sous le rail intérieur (une bande sombre si le sprite manque).
            Image back = Layer2("Fond (parchemin)", _fullMap, _skin.mapParchment, Image.Type.Tiled, MapBand);
            Inset(back.rectTransform, MapFrameBorder * 0.75f);

            _fullMapArea = Rect("Zone de la carte", _fullMap, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(_fullMapArea);
            Inset(_fullMapArea, MapFrameBorder + 20f);
            RectTransform img = Rect("Salles découvertes (encre)", _fullMapArea, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _fullMapImage = img.gameObject.AddComponent<RawImage>();
            _fullMapImage.raycastTarget = false;
            if (_skin.mapInkMaterial != null)
                _fullMapImage.material = _skin.mapInkMaterial;
            _fullMapIcons = Rect("Icônes", img, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(_fullMapIcons);
            _fullArrow = MapIcon(_fullMapIcons, "Joueur (flèche)", _skin.iconArrow, MapIconSize * 1.15f);

            // Brume de la zone non découverte : sur toute la zone, masquée par les salles et le pinceau (matériau à soi par joueur).
            RectTransform fog = Rect("Brume (zone non découverte)", _fullMap, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(fog);
            Inset(fog, MapFrameBorder * 0.75f);
            _fullFog = fog.gameObject.AddComponent<RawImage>();
            _fullFog.raycastTarget = false;
            _fullFog.texture = _skin.mapFog != null ? _skin.mapFog.texture : null;
            if (_skin.mapFogMaterial != null)
                _fullFog.material = new Material(_skin.mapFogMaterial) { name = _skin.mapFogMaterial.name + " (joueur)" };
            _fullFog.enabled = _skin.mapFog != null && _skin.mapFogMaterial != null;

            if (_skin.mapVignette != null)
                Inset(Layer2("Vignette", _fullMap, _skin.mapVignette, Image.Type.Simple, Color.white).rectTransform, MapFrameBorder * 0.75f);
            if (_skin.mapFrame != null)
            {
                Image frame = Layer2("Cadre", _fullMap, _skin.mapFrame, Image.Type.Sliced, Color.white);
                // Bordure de 260 px à 200 px par unité : 130 px en 1080p (le canvas compte 100 px par unité).
                frame.pixelsPerUnitMultiplier = 1f;
            }

            Text title = NewText("Titre", _fullMap, new Vector2(0f, -MapFrameBorder * 0.5f), new Vector2(700f, 70f), 46, TextAnchor.MiddleCenter);
            title.rectTransform.anchorMin = title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            title.font = _skin.titleFont != null ? _skin.titleFont : _font;
            title.text = "Carte";
            title.color = new Color(0.9f, 0.87f, 0.92f);

            BuildLegend();
            if (_skin.mapCompass != null)
            {
                Image rose = MapIcon(_fullMap, "Rose des vents", _skin.mapCompass, 128f);
                rose.rectTransform.anchorMin = rose.rectTransform.anchorMax = new Vector2(0f, 1f);
                rose.rectTransform.anchoredPosition = new Vector2(MapFrameBorder + 100f, -MapFrameBorder - 90f);
            }

            _fullMap.gameObject.SetActive(false);
        }

        private float MapIconSize => _skin != null ? _skin.mapIconSize : 30f;

        private Image Layer2(string name, RectTransform parent, Sprite sprite, Image.Type type, Color color)
        {
            RectTransform r = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(r);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null ? type : Image.Type.Simple;
            img.color = sprite != null ? color : MapBand;
            img.raycastTarget = false;
            return img;
        }

        private static void Inset(RectTransform r, float d)
        {
            r.offsetMin = new Vector2(d, d);
            r.offsetMax = new Vector2(-d, -d);
        }

        /// <summary>Légende (panneau en 9 tranches, bas droite) : icônes de la mini-carte en 40 px et leur nom (EB Garamond SemiBold 26).</summary>
        private void BuildLegend()
        {
            var items = new List<(Sprite, string)>
            {
                (_skin.iconArrow, "Vous"), (_skin.iconChest, "Coffre"), (_skin.iconChestRare, "Coffre rare"), (_skin.iconMerchant, "Marchand"),
                (_skin.iconSouls, "Âmes"), (_skin.iconExitLocked, "Sortie"), (_skin.allyFox, "Allié"), (_skin.iconAllyDead, "Allié mort"),
            };
            items.RemoveAll(i => i.Item1 == null);
            const float row = 50f, pad = 46f;
            var size = new Vector2(330f, pad * 2f + row * items.Count);
            _legend = Rect("Légende", _fullMap, new Vector2(1f, 0f), new Vector2(-MapFrameBorder - 40f, MapFrameBorder + 40f), size, new Vector2(1f, 0f));
            if (_skin.mapLegendFrame != null)
            {
                Image panel = Layer2("Cadre de la légende", _legend, _skin.mapLegendFrame, Image.Type.Sliced, Color.white);
                panel.pixelsPerUnitMultiplier = 1f;
            }
            else
                NewImage("Fond", _legend, Vector2.zero, size, MapBand);

            for (int i = 0; i < items.Count; i++)
            {
                (Sprite s, string label) = items[i];
                RectTransform cell = Rect("Légende : " + label, _legend, new Vector2(0f, 1f), new Vector2(pad, -pad - row * (i + 0.5f)), new Vector2(size.x - 2f * pad, row), new Vector2(0f, 0.5f));
                Image icon = MapIcon(cell, "Icône", s, 40f);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(20f, 0f);
                Text t = NewText("Texte", cell, new Vector2(56f, 0f), new Vector2(size.x - 2f * pad - 56f, row), 26, TextAnchor.MiddleLeft);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                t.rectTransform.pivot = new Vector2(0f, 0.5f);
                if (_skin.mapLegendFont != null)
                    t.font = _skin.mapLegendFont;
                t.text = label;
                t.color = LegendText;
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

        /// <summary>
        /// UV de la brume tendue sur une zone plus grande que la carte (pur, testable) : la carte (taille map, centrée) couvre [0, 1] ;
        /// la brume (taille area) déborde de chaque côté.
        /// </summary>
        public static Rect FogUv(Vector2 area, Vector2 map) =>
            new Rect(-(area.x - map.x) * 0.5f / Mathf.Max(1f, map.x), -(area.y - map.y) * 0.5f / Mathf.Max(1f, map.y),
                area.x / Mathf.Max(1f, map.x), area.y / Mathf.Max(1f, map.y));

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

        private static readonly int RoomsId = Shader.PropertyToID("_Rooms"), BrushId = Shader.PropertyToID("_Brush"), TilingId = Shader.PropertyToID("_Tiling");

        private void UpdateFullMap()
        {
            if (_fullMap == null || _health == null)
                return;
            MinimapHud mm = MinimapHud.Current;
            Texture tex = mm != null ? mm.TextureFor(_health.PlayerId) : null;
            bool on = tex != null && WantsFullMap();
            if (_fullMap.gameObject.activeSelf != on)
            {
                _fullMap.gameObject.SetActive(on);
                if (on)
                    _fullMap.SetAsLastSibling();                        // au-dessus du HUD (âmes, timer, barre de vie)
            }
            if (!on)
                return;

            // Vue étroite (écran partagé côte à côte) : légende réduite.
            Rect view = _root.rect;
            _legend.localScale = Vector3.one * Mathf.Clamp(view.width / 1500f, 0.6f, 1f);
            Bounds all = mm.AllBounds;
            Vector2 area = _fullMapArea.rect.size;
            Vector2 size = FitMap(area, new Vector2(all.size.x, all.size.z));
            _fullMapImage.rectTransform.sizeDelta = size;
            _fullMapImage.texture = tex;
            _fullMapImage.uvRect = new Rect(0f, 0f, 1f, 1f);
            if (_fullFog.enabled && _fullFog.material != null)
            {
                Vector2 fogArea = ((RectTransform)_fullFog.transform).rect.size;
                _fullFog.uvRect = FogUv(fogArea, size);
                Material m = _fullFog.material;
                m.SetTexture(RoomsId, tex);
                m.SetTexture(BrushId, mm.BrushFor(_health.PlayerId));
                float tile = _skin.mapFogTiling;
                m.SetVector(TilingId, new Vector4(tile, tile * size.y / Mathf.Max(1f, size.x), 0f, 0f));
            }

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
