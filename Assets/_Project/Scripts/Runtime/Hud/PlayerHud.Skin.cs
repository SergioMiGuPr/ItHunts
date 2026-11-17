using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Level;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// HUD habillé par la DA (PR #10, docs/art/ui/UI-HANDOFF.md, réglages HudSkinSettings) : barre de vie (remplissage rouge sang,
    /// traînée des dégâts récents, « PV/max »), cartouche des âmes (flamme gris-bleu et nombre), mini-carte ronde (cadre, masque
    /// circulaire, fond, icônes des points clés et des alliés, flèche du joueur), polices EB Garamond et Grenze Gotisch. Unités :
    /// pixels en 1080p (l'échelle du canevas suit la hauteur de la vue du joueur, écran partagé compris).
    /// </summary>
    public sealed partial class PlayerHud
    {
        private HudSkinSettings _skin;
        private Image _hpFill, _hpTrail;
        private Text _hpText;
        private float _hpTrailValue = 1f, _hpTrailHoldUntil, _hpLast = -1f;
        private RectTransform _mapCircle, _mapIcons;
        private RawImage _map;
        private Image _arrow;
        private readonly List<Image> _markIcons = new List<Image>();
        private readonly List<Image> _allyIcons = new List<Image>();

        /// <summary>Racine de la mini-carte ronde (tests, captures) ; null sans habillage.</summary>
        public RectTransform MinimapRoot { get; private set; }
        public RectTransform HealthBarRoot { get; private set; }
        public bool Skinned => _skin != null;

        private void BuildSkin()
        {
            _skin = settings != null ? settings.skin : null;
            if (_skin == null)
                return;
            MinimapHud.RoundMinimap = true;
            if (_skin.titleFont != null)
                foreach (Text t in new[] { _zoneMessage, _waveText, _stalkerText })
                    if (t != null)
                        t.font = _skin.titleFont;
            if (_healthBack != null)
                _healthBack.transform.parent.gameObject.SetActive(false);    // barre greybox remplacée
            BuildHealthBar();
            BuildSoulsBadge();
            BuildMinimap();
            // Le timer et le traqueur enragé passent sous la mini-carte (même coin).
            float down = _skin.minimapNativeSize.y * _skin.minimapScale + 12f;
            if (TimerCorner != null)
                TimerCorner.anchoredPosition += new Vector2(0f, -down);
            if (StalkerCorner != null)
                StalkerCorner.anchoredPosition += new Vector2(0f, -down);
        }

        private Image Layer(string name, RectTransform parent, Sprite sprite, float k, Image.Type type = Image.Type.Sliced)
        {
            RectTransform r = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(r);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null && sprite.border.sqrMagnitude > 0f ? type : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, k);     // bordures (emblème, pointe) à l'échelle k
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private void Outline(Text t)
        {
            var o = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
            o.effectColor = _skin.numberOutline;
            o.effectDistance = new Vector2(_skin.numberOutlineWidth, -_skin.numberOutlineWidth);
        }

        // ---------- Barre de vie ----------

        private void BuildHealthBar()
        {
            float k = _skin.healthScale;
            RectTransform bar = Rect("Barre de vie", _root, Vector2.zero, _skin.healthCorner,
                new Vector2(_skin.healthLength, _skin.healthNativeHeight * k), Vector2.zero);
            HealthBarRoot = bar;
            Layer("Fond", bar, _skin.healthBack, k);
            Vector4 ch = _skin.healthChannel * k;
            RectTransform channel = Rect("Canal", bar, Vector2.zero, Vector2.zero, Vector2.zero);
            channel.anchorMin = new Vector2(0f, 1f);
            channel.anchorMax = new Vector2(1f, 1f);
            channel.offsetMin = new Vector2(ch.x, -(ch.z + ch.w));
            channel.offsetMax = new Vector2(-ch.y, -ch.z);
            _hpTrail = Layer("Traînée des dégâts récents", channel, _skin.healthTrail, k, Image.Type.Filled);
            _hpFill = Layer("Remplissage", channel, _skin.healthFill, k, Image.Type.Filled);
            foreach (Image img in new[] { _hpTrail, _hpFill })
            {
                img.type = Image.Type.Filled;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.fillOrigin = (int)Image.OriginHorizontal.Left;
            }

            Layer("Cadre", bar, _skin.healthFrame, k);
            _hpText = NewText("PV", channel, Vector2.zero, Vector2.zero, _skin.healthFontSize, TextAnchor.MiddleLeft);
            Stretch(_hpText.rectTransform);
            _hpText.rectTransform.offsetMin = new Vector2(14f * k, -6f);
            _hpText.rectTransform.offsetMax = new Vector2(0f, 6f);
            _hpText.color = _skin.healthTextColor;
            Outline(_hpText);
        }

        private void UpdateHealthBar(float now, float dt)
        {
            if (_hpFill == null || _health == null)
                return;
            HealthState h = _health.Health;
            float v = h.Max > 0f ? Mathf.Clamp01(h.Current / h.Max) : 0f;
            if (_hpLast < 0f || v > _hpLast)
                _hpTrailValue = Mathf.Max(_hpTrailValue, v);
            if (_hpLast >= 0f && v < _hpLast - 1e-4f)
                _hpTrailHoldUntil = now + _skin.trailDelay;              // dégâts : la traînée attend, puis rattrape
            _hpLast = v;
            if (now >= _hpTrailHoldUntil)
                _hpTrailValue = Mathf.MoveTowards(_hpTrailValue, v, dt / Mathf.Max(0.01f, _skin.trailCatchUp));
            if (_hpTrailValue < v)
                _hpTrailValue = v;
            _hpFill.fillAmount = v;
            _hpTrail.fillAmount = _hpTrailValue;
            _hpFill.color = now < _healthFlashUntil ? new Color(1f, 0.85f, 0.85f) : Color.white;
            _hpText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, h.Current))}/{Mathf.RoundToInt(h.Max)}";
        }

        // ---------- Cartouche des âmes ----------

        private void BuildSoulsBadge()
        {
            float k = _skin.soulsScale;
            RectTransform badge = Rect("Cartouche des âmes", _root, new Vector2(0f, 1f), new Vector2(_skin.soulsCorner.x, -_skin.soulsCorner.y),
                new Vector2(_skin.soulsLength, _skin.soulsNativeHeight * k), new Vector2(0f, 1f));
            Layer("Fond", badge, _skin.soulsBack, k);
            Vector4 e = _skin.soulsEmblem * k;
            if (_skin.soulsFlame != null)
            {
                float fh = 0.8f * e.z;
                Vector2 size = new Vector2(fh * _skin.soulsFlame.rect.width / Mathf.Max(1f, _skin.soulsFlame.rect.height), fh);
                RectTransform fr = Rect("Flamme des âmes", badge, new Vector2(0f, 1f), new Vector2(e.x, -e.y), size);
                var flame = fr.gameObject.AddComponent<Image>();
                flame.sprite = _skin.soulsFlame;
                flame.preserveAspect = true;
                flame.raycastTarget = false;
            }

            Layer("Cadre", badge, _skin.soulsFrame, k);
            if (_soulsText != null)
            {
                _soulsText.transform.SetParent(badge, false);
                _soulsText.rectTransform.pivot = new Vector2(0f, 0.5f);       // avant les marges : elles restent où on les met
                _soulsText.rectTransform.localScale = Vector3.one;
                Stretch(_soulsText.rectTransform);
                _soulsText.rectTransform.offsetMin = new Vector2(e.w, 0f);
                _soulsText.rectTransform.offsetMax = new Vector2(-8f, 0f);
                _soulsText.fontSize = _skin.soulsFontSize;
                _soulsText.fontStyle = FontStyle.Normal;
                Outline(_soulsText);
            }

            SoulsCorner = badge;
        }

        // ---------- Mini-carte ronde ----------

        private void BuildMinimap()
        {
            float k = _skin.minimapScale;
            Vector2 size = _skin.minimapNativeSize * k;
            RectTransform root = Rect("Mini-carte", _root, new Vector2(1f, 1f), new Vector2(-_skin.minimapCorner.x, -_skin.minimapCorner.y), size,
                new Vector2(1f, 1f));
            MinimapRoot = root;
            Layer("Fond", root, _skin.minimapBack, k, Image.Type.Simple);
            Image mask = Layer("Masque", root, _skin.minimapMask, k, Image.Type.Simple);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            float r = _skin.minimapCircle.z * k;
            _mapCircle = Rect("Zone visible", mask.rectTransform, new Vector2(0f, 1f),
                new Vector2(_skin.minimapCircle.x * k, -_skin.minimapCircle.y * k), new Vector2(2f * r, 2f * r));
            RectTransform mapRect = Rect("Carte", _mapCircle, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(mapRect);
            _map = mapRect.gameObject.AddComponent<RawImage>();
            _map.raycastTarget = false;
            _mapIcons = Rect("Icônes", _mapCircle, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(_mapIcons);
            Layer("Cadre", root, _skin.minimapFrame, k, Image.Type.Simple);
            _arrow = Icon("Joueur (flèche)", _skin.iconArrow, _skin.arrowSize);
            _arrow.rectTransform.SetParent(_mapCircle, false);                // au centre, au-dessus des autres icônes
        }

        private Image Icon(string name, Sprite sprite, float size)
        {
            RectTransform r = Rect(name, _mapIcons, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private void UpdateMinimap()
        {
            if (MinimapRoot == null || _health == null)
                return;
            MinimapHud mm = MinimapHud.Current;
            bool on = mm != null && mm.MapTexture != null && !mm.FullMap;
            if (MinimapRoot.gameObject.activeSelf != on)
                MinimapRoot.gameObject.SetActive(on);
            if (!on)
                return;
            Vector3 c = _health.transform.position;
            float metres = _skin.minimapMetres;
            Bounds all = mm.AllBounds;
            _map.texture = mm.MapTexture;
            _map.uvRect = new Rect((c.x - metres / 2f - all.min.x) / Mathf.Max(1f, all.size.x), (c.z - metres / 2f - all.min.z) / Mathf.Max(1f, all.size.z),
                metres / Mathf.Max(1f, all.size.x), metres / Mathf.Max(1f, all.size.z));
            float radius = _mapCircle.rect.width * 0.5f, scale = 2f * radius / metres;
            Vector2 P(Vector3 w) => new Vector2((w.x - c.x) * scale, (w.z - c.z) * scale);

            int used = 0;
            for (int i = 0; i < mm.MarkCount; i++)
            {
                if (!mm.Mark(i, out Vector3 at, out Object owner))
                    continue;
                Sprite s = owner switch
                {
                    Chest ch => ch.OpenedFor(_health.PlayerId) ? _skin.iconChestOpen : _skin.iconChest,
                    ArtifactMerchant => _skin.iconMerchant,
                    LockedDoor => _skin.iconSouls,
                    ExitDoor ed => ed.Open || ed.Unlocked ? _skin.iconExitOpen : _skin.iconExitLocked,
                    _ => null,
                };
                Vector2 pos = P(at);
                if (s == null || pos.magnitude > radius + _skin.iconSize)
                    continue;
                Image img = Pooled(_markIcons, used++, _skin.iconSize, "Point clé");
                img.sprite = s;
                img.rectTransform.anchoredPosition = pos;
            }

            for (int i = used; i < _markIcons.Count; i++)
                _markIcons[i].enabled = false;

            used = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p == _health)
                    continue;
                CharacterDefinition cd = p.TryGetComponent(out ItHunts.Artifacts.ArtifactHolder holder) ? holder.Character : null;
                Sprite s = p.Dead ? _skin.iconAllyDead : cd == null ? _skin.allyOwl : cd.id switch
                {
                    "renard" => _skin.allyFox,
                    "koala" => _skin.allyKoala,
                    "cameleon" => _skin.allyChameleon,
                    _ => _skin.allyOwl,
                };
                Image img = Pooled(_allyIcons, used++, p.Dead ? _skin.iconSize : _skin.allySize, "Allié");
                img.sprite = s;
                img.rectTransform.anchoredPosition = P(p.transform.position);
            }

            for (int i = used; i < _allyIcons.Count; i++)
                _allyIcons[i].enabled = false;

            Vector3 fwd = _worldCamera != null ? _worldCamera.transform.forward : _health.transform.forward;
            _arrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, -Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg);
        }

        private Image Pooled(List<Image> pool, int i, float size, string name)
        {
            while (pool.Count <= i)
                pool.Add(Icon($"{name} {pool.Count + 1}", null, size));
            Image img = pool[i];
            img.enabled = true;
            img.rectTransform.sizeDelta = Vector2.one * size;
            return img;
        }
    }
}
