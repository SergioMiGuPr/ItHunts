using System.Collections.Generic;
using ItHunts.Art.UIJeu;
using ItHunts.Economy;
using ItHunts.Hud;
using ItHunts.Menus;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.HudArt
{
    /// <summary>
    /// Vue de la DA du coffre et du marchand (nuit 5, lot 2 : Panneau_Offres_Coffre, Panneau_Offres_Marchand ; point 3) : trois cartes
    /// (illustration du catalogue par code, cadre du personnage, rang actuel → suivant, effet précis, prix), bouton Relancer au coffre,
    /// carte de soin au marchand. La logique reste à <see cref="ShopMenu"/> : focus (clavier, manette ◄ ►, survol), achat, relance,
    /// fermeture. Comme <see cref="ShopPanelArt"/>, aucun évènement de l'EventSystem : survol et clic sont testés ici.
    /// </summary>
    public sealed class OffersPanelArt : MonoBehaviour
    {
        private static OffersPanelArt _instance;
        private Canvas _canvas;

        private sealed class View
        {
            public ShopMenu Menu;
            public RectTransform Root;
            public PanneauOffres Panel;
            public int Version = -1, Souls = -1, Reroll = -2, Focus = -2;
            public Graphic[] CloseTargets, RerollTargets;
        }

        private readonly List<View> _views = new List<View>();

        public static int OpenViews => _instance != null ? _instance._views.Count : 0;

        /// <summary>Pose la vue si le skin a le panneau d'offres et s'il a assez de cartes ; faux : repli (ancien panneau ou greybox).</summary>
        public static bool Attach(ShopMenu menu)
        {
            HudSkinSettings skin = SkinOf(menu);
            GameObject prefab = skin == null ? null : menu.Type == ShopMenu.Kind.Coffre ? skin.chestOffersPrefab : skin.merchantOffersPrefab;
            if (prefab == null)
                return false;
            if (_instance == null)
                _instance = new GameObject("Panneaux d'offres (DA)").AddComponent<OffersPanelArt>();
            return _instance.Build(menu, prefab);
        }

        private static HudSkinSettings SkinOf(ShopMenu m)
        {
            var hud = m.Player != null ? m.Player.GetComponent<PlayerHud>() : null;
            return hud != null ? hud.HudSkin : null;
        }

        private bool Build(ShopMenu menu, GameObject prefab)
        {
            if (_canvas == null)
                _canvas = UiKit.NewCanvas("Panneaux d'offres", 70, transform);
            var v = new View { Menu = menu };
            v.Root = new GameObject("Offres : " + menu.Player.name, typeof(RectTransform)).GetComponent<RectTransform>();
            v.Root.SetParent(_canvas.transform, false);
            UiKit.FitTo(v.Root, menu.Input != null ? menu.Input.camera : null);
            GameObject go = MenuArtKit.Spawn(prefab, v.Root, prefab.name);
            foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            v.Panel = go.GetComponent<PanneauOffres>();
            if (v.Panel == null)
                v.Panel = go.GetComponentInChildren<PanneauOffres>(true);
            // Plus d'offres que de cartes (Œil du collectionneur, coffre rare : 4 choix et plus) : nuit du 27/11, le panneau illustré
            // reçoit des cartes en plus (copies de la dernière) au lieu de l'ancien panneau en liste, sans illustrations (playtest).
            if (v.Panel != null && v.Panel.cartes != null && v.Panel.cartes.Length > 0 && menu.Offers.Count > v.Panel.cartes.Length)
            {
                float before = ((RectTransform)v.Panel.transform).rect.width;
                float grown = before + GrowCards(v.Panel, menu.Offers.Count);
                // 5 cartes et plus (Œil du collectionneur), écran partagé : tout le panneau réduit pour tenir dans la vue du joueur.
                float room = v.Root.rect.width > 1f ? v.Root.rect.width * 0.96f : grown;
                if (grown > room)
                    go.transform.localScale = Vector3.one * (room / grown);
            }
            if (v.Panel == null || v.Panel.cartes == null || menu.Offers.Count > v.Panel.cartes.Length)
            {
                Destroy(v.Root.gameObject);
                return false;
            }

            Transform veil = go.transform.Find("Voile");
            if (veil != null)
            {
                veil.SetParent(v.Root, false);
                veil.SetAsFirstSibling();
                UiKit.Stretch((RectTransform)veil);
            }

            v.CloseTargets = v.Panel.fermer != null ? MenuArtKit.Targets(v.Panel.fermer) : new Graphic[0];
            v.RerollTargets = v.Panel.relancer != null ? MenuArtKit.Targets(v.Panel.relancer) : new Graphic[0];
            menu.Skinned = true;
            menu.Closed += () => Remove(v);
            _views.Add(v);
            Sync(v);
            return true;
        }

        /// <summary>Cartes copiées sur la dernière jusqu'à count, sur la même ligne ; le panneau s'élargit d'autant.</summary>
        public static float GrowCards(PanneauOffres panel, int count)
        {
            PanneauOffres.Carte[] cards = panel.cartes;
            int n0 = cards.Length;
            PanneauOffres.Carte last = cards[n0 - 1];
            if (last == null || last.racine == null || count <= n0)
                return 0f;
            RectTransform r0 = cards[0]?.racine, r1 = n0 > 1 ? cards[1]?.racine : null;
            Vector2 step = r0 != null && r1 != null ? r1.anchoredPosition - r0.anchoredPosition : new Vector2(last.racine.rect.width * 1.05f, 0f);
            var all = new System.Collections.Generic.List<PanneauOffres.Carte>(cards);
            for (int i = n0; i < count; i++)
            {
                GameObject copy = Instantiate(last.racine.gameObject, last.racine.parent, false);
                copy.name = last.racine.name + " (" + (i + 1) + ")";
                all.Add(Rewire(last, (RectTransform)copy.transform));
            }

            // Les cartes gardent leur taille (le panneau anime leur échelle au survol) et leur espacement ; la ligne est recentrée sur le
            // centre des cartes d'origine, et le cadre et le fond (enfants à la largeur du panneau) s'élargissent d'autant.
            float extra = Mathf.Abs(step.x) * (count - n0);
            var panelRt = (RectTransform)panel.transform;
            float panelW = panelRt.rect.width;
            foreach (Transform child in panelRt)
            {
                if (!(child is RectTransform crt) || all.Exists(c => c?.racine == crt))
                    continue;
                bool stretched = !Mathf.Approximately(crt.anchorMin.x, crt.anchorMax.x);
                if (!stretched && crt.rect.width >= panelW * 0.9f)
                {
                    crt.sizeDelta += new Vector2(extra, 0f);             // cadre et fond à taille fixe, élargis autour de leur centre
                    crt.anchoredPosition -= new Vector2(extra * (0.5f - crt.pivot.x), 0f);
                }
                else if (stretched && crt.rect.width >= panelW * 0.9f)
                    crt.offsetMin -= new Vector2(extra * 0.5f, 0f);       // étirés : on écarte leurs bords
                if (stretched && crt.rect.width >= panelW * 0.9f)
                    crt.offsetMax += new Vector2(extra * 0.5f, 0f);
            }

            Vector2 center = r0 != null ? r0.anchoredPosition + step * (n0 - 1) * 0.5f : Vector2.zero;
            for (int i = 0; i < all.Count; i++)
                if (all[i]?.racine != null)
                    all[i].racine.anchoredPosition = center + step * (i - (count - 1) * 0.5f);

            panel.cartes = all.ToArray();
            return extra;
        }

        /// <summary>Copie d'une carte : chaque référence vers un enfant de la carte d'origine pointe vers le même enfant de la copie.</summary>
        private static PanneauOffres.Carte Rewire(PanneauOffres.Carte src, RectTransform copyRoot)
        {
            var dst = new PanneauOffres.Carte();
            foreach (System.Reflection.FieldInfo f in typeof(PanneauOffres.Carte).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
            {
                object value = f.GetValue(src);
                if (f.FieldType == typeof(RectTransform) && ReferenceEquals(value, src.racine))
                    f.SetValue(dst, copyRoot);
                else if (value is Component c)
                    f.SetValue(dst, Same(c, src.racine, copyRoot));
                else if (value is Component[] arr)
                {
                    var res = (Component[])System.Array.CreateInstance(f.FieldType.GetElementType(), arr.Length);
                    for (int i = 0; i < arr.Length; i++)
                        res[i] = arr[i] != null ? Same(arr[i], src.racine, copyRoot) : null;
                    f.SetValue(dst, res);
                }
                else if (!f.IsNotSerialized || f.FieldType.IsValueType)
                    f.SetValue(dst, value);
            }

            dst.abordable = true;
            dst.proprietaire = 0;
            return dst;
        }

        private static Component Same(Component c, Transform srcRoot, Transform dstRoot)
        {
            if (c == null)
                return null;
            if (!c.transform.IsChildOf(srcRoot))
                return c;                                                // hors de la carte : partagé
            var path = new System.Collections.Generic.List<int>();
            for (Transform t = c.transform; t != srcRoot; t = t.parent)
                path.Add(t.GetSiblingIndex());
            Transform d = dstRoot;
            for (int i = path.Count - 1; i >= 0 && d != null; i--)
                d = path[i] < d.childCount ? d.GetChild(path[i]) : null;
            return d != null ? d.GetComponent(c.GetType()) : null;
        }

        private void Remove(View v)
        {
            _views.Remove(v);
            if (v.Root != null)
                Destroy(v.Root.gameObject);
        }

        private void LateUpdate()
        {
            foreach (View v in _views.ToArray())
            {
                if (v.Menu == null || !v.Menu.IsOpen)
                {
                    Remove(v);
                    continue;
                }

                Sync(v);
            }
        }

        /// <summary>Offres du menu pour les cartes de la DA (code de AMELIORATIONS.md, rang possédé ou -1, effet au rang visé, prix).</summary>
        public static PanneauOffres.Offre[] OffersOf(ShopMenu m)
        {
            var r = new PanneauOffres.Offre[m.Offers.Count];
            for (int i = 0; i < r.Length; i++)
            {
                OfferOrb o = m.Offers[i];
                if (o == null)
                    continue;
                if (o.Upgrade == null)
                    r[i] = new PanneauOffres.Offre
                    {
                        soin = true, soinPV = Mathf.RoundToInt(o.Heal), nom = "Soin", description = m.Description(i), prix = m.Price(i),
                    };
                else
                    r[i] = new PanneauOffres.Offre
                    {
                        id = o.Upgrade.code, nom = o.Upgrade.displayName, rangActuel = o.IsRankUp ? o.FromRank : -1,
                        description = m.Description(i), prix = m.Price(i),
                    };
            }

            return r;
        }

        private void Sync(View v)
        {
            ShopMenu m = v.Menu;
            var run = ItHunts.Artifacts.RunState.Current;
            int souls = run != null && m.Player != null ? run.Souls(m.Player.PlayerId) : 0;
            int reroll = m.Type == ShopMenu.Kind.Coffre ? m.RerollPrice : -1;
            if (v.Version != m.Version || v.Souls != souls || v.Reroll != reroll)
            {
                v.Panel.Remplit(m.Title, OffersOf(m), souls, reroll);
                v.Version = m.Version;
                v.Souls = souls;
                v.Reroll = reroll;
            }

            if (m.Input != null)
                UiKit.FitTo(v.Root, m.Input.camera);

            // Souris (joueur au clavier) : survol = focus, clic = achat, relance ou fermer.
            bool mouse = m.Menu.Owner is Keyboard && Mouse.current != null;
            Camera none = null;
            for (int i = 0; i < m.Offers.Count && i < v.Panel.cartes.Length; i++)
            {
                RectTransform card = v.Panel.cartes[i] != null ? v.Panel.cartes[i].racine : null;
                if (card == null)
                    continue;
                if (mouse && MenuArtKit.MouseMoved && MenuArtKit.Over(card, none))
                    m.Menu.SetFocus(i);
                if (mouse && MenuArtKit.Clicked && MenuArtKit.Over(card, none))
                {
                    m.Menu.SetFocus(i);
                    m.Buy(i);
                    return;                                               // le menu a pu se fermer ou changer d'offres
                }
            }

            int rerollIndex = m.RerollIndex;
            if (v.Panel.relancer != null && rerollIndex >= 0)
            {
                var rect = (RectTransform)v.Panel.relancer.transform;
                if (mouse && MenuArtKit.MouseMoved && MenuArtKit.Over(rect, none))
                    m.Menu.SetFocus(rerollIndex);
                if (mouse && MenuArtKit.Clicked && MenuArtKit.Over(rect, none))
                {
                    m.Menu.SetFocus(rerollIndex);
                    m.Reroll();
                    return;
                }

                bool pressed = mouse && MenuArtKit.MouseDown && MenuArtKit.Over(rect, none);
                MenuArtKit.Tint(v.RerollTargets, !m.RerollAffordable ? MenuArtKit.State.Disabled : pressed ? MenuArtKit.State.Pressed
                    : m.Menu.Focus == rerollIndex ? MenuArtKit.State.Hover : MenuArtKit.State.Normal);
            }

            int closeIndex = m.Menu.Items.Count - 1;
            if (v.Panel.fermer != null)
            {
                var closeRect = (RectTransform)v.Panel.fermer.transform;
                if (mouse && MenuArtKit.MouseMoved && MenuArtKit.Over(closeRect, none))
                    m.Menu.SetFocus(closeIndex);
                if (mouse && MenuArtKit.Clicked && MenuArtKit.Over(closeRect, none))
                {
                    m.Close();
                    return;
                }

                bool pressed = mouse && MenuArtKit.MouseDown && MenuArtKit.Over(closeRect, none);
                MenuArtKit.Tint(v.CloseTargets, pressed ? MenuArtKit.State.Pressed
                    : m.Menu.Focus == closeIndex ? MenuArtKit.State.Hover : MenuArtKit.State.Normal);
            }

            int focus = m.FocusedOffer;
            if (focus != v.Focus)
            {
                v.Panel.Focus(focus);
                v.Focus = focus;
            }
        }
    }
}
