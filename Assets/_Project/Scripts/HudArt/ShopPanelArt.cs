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
    /// Vue de la DA du menu du marchand et du coffre (<see cref="ShopMenu"/>) : panneau livré (Panneau_Marchand, Panneau_Coffre),
    /// dans la vue du joueur (écran partagé). La logique reste à ShopMenu : focus (clavier, manette, survol de la souris), achat
    /// (clic ou validation), description de l'offre qui a le focus, fermeture (bouton Fermer, Échap, B). Les composants de la DA
    /// ne reçoivent aucun évènement de l'EventSystem (pas de lanceur de rayons) : ils ne servent qu'à l'affichage.
    /// </summary>
    public sealed class ShopPanelArt : MonoBehaviour
    {
        private static ShopPanelArt _instance;
        private Canvas _canvas;

        private sealed class View
        {
            public ShopMenu Menu;
            public RectTransform Root;
            public PanneauBoutique Panel;
            public int Version = -1;
            public int Souls = -1;
            public Graphic[] CloseTargets;
        }

        private readonly List<View> _views = new List<View>();

        public static int OpenViews => _instance != null ? _instance._views.Count : 0;

        public static void Attach(ShopMenu menu)
        {
            HudSkinSettings skin = SkinOf(menu);
            GameObject prefab = skin == null ? null : menu.Type == ShopMenu.Kind.Coffre ? skin.chestPanelPrefab : skin.merchantPanelPrefab;
            if (prefab == null)
                return;                                                  // repli : menu greybox
            if (_instance == null)
                _instance = new GameObject("Panneaux de boutique (DA)").AddComponent<ShopPanelArt>();
            _instance.Build(menu, prefab);
        }

        private static HudSkinSettings SkinOf(ShopMenu m)
        {
            var hud = m.Player != null ? m.Player.GetComponent<PlayerHud>() : null;
            return hud != null ? hud.HudSkin : null;
        }

        private void Build(ShopMenu menu, GameObject prefab)
        {
            if (_canvas == null)
                _canvas = UiKit.NewCanvas("Panneaux de boutique", 70, transform);
            var v = new View { Menu = menu };
            v.Root = new GameObject("Boutique : " + menu.Player.name, typeof(RectTransform)).GetComponent<RectTransform>();
            v.Root.SetParent(_canvas.transform, false);
            UiKit.FitTo(v.Root, menu.Input != null ? menu.Input.camera : null);
            GameObject go = MenuArtKit.Spawn(prefab, v.Root, prefab.name);
            foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            Transform veil = go.transform.Find("Voile");
            if (veil != null)
            {
                veil.SetParent(v.Root, false);
                veil.SetAsFirstSibling();
                UiKit.Stretch((RectTransform)veil);
            }

            v.Panel = go.GetComponent<PanneauBoutique>();
            if (v.Panel == null)
            {
                Destroy(v.Root.gameObject);
                return;
            }

            if (v.Panel.titre != null)
                v.Panel.titre.text = menu.Title;
            v.CloseTargets = v.Panel.fermer != null ? MenuArtKit.Targets(v.Panel.fermer) : new Graphic[0];
            menu.Skinned = true;
            menu.Closed += () => Remove(v);
            _views.Add(v);
            Sync(v);
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

        private void Sync(View v)
        {
            ShopMenu m = v.Menu;
            var run = ItHunts.Artifacts.RunState.Current;
            int souls = run != null && m.Player != null ? run.Souls(m.Player.PlayerId) : 0;
            if (v.Version != m.Version || v.Souls != souls)
            {
                var offers = new List<PanneauBoutique.Offre>();
                for (int i = 0; i < m.Offers.Count && i < v.Panel.cartes.Length; i++)
                    offers.Add(new PanneauBoutique.Offre
                    {
                        icone = null, nom = m.Name(i), prix = m.Price(i), description = m.Description(i),
                    });
                v.Panel.Remplit(offers, souls);
                for (int i = 0; i < offers.Count; i++)
                    if (v.Panel.cartes[i] != null && v.Panel.cartes[i].icone != null)
                        v.Panel.cartes[i].icone.enabled = false;          // icônes des améliorations : à fournir par la DA
                v.Version = m.Version;
                v.Souls = souls;
            }

            if (m.Input != null)
                UiKit.FitTo(v.Root, m.Input.camera);

            // Souris (joueur au clavier) : survol = focus, clic = achat ou fermer.
            bool mouse = m.Menu.Owner is Keyboard && Mouse.current != null;
            Camera none = null;
            for (int i = 0; i < m.Offers.Count && i < v.Panel.cartes.Length; i++)
            {
                CarteAmelioration card = v.Panel.cartes[i];
                if (card == null)
                    continue;
                var rect = (RectTransform)card.transform;
                if (mouse && MenuArtKit.MouseMoved && MenuArtKit.Over(rect, none))
                    m.Menu.SetFocus(i);
                if (mouse && MenuArtKit.Clicked && MenuArtKit.Over(rect, none))
                {
                    m.Menu.SetFocus(i);
                    m.Buy(i);
                    return;                                               // le menu a pu se fermer ou changer d'offres
                }
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
            for (int i = 0; i < v.Panel.cartes.Length; i++)
                if (v.Panel.cartes[i] != null && v.Panel.cartes[i].gameObject.activeSelf)
                    v.Panel.cartes[i].ForceSurvol(i == focus);
            if (v.Panel.description != null)
                v.Panel.description.text = focus >= 0 ? m.Description(focus) : v.Panel.invite;
        }
    }
}
