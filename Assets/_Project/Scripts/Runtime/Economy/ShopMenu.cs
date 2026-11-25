using System;
using System.Collections.Generic;
using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Economy
{
    /// <summary>
    /// Menu du marchand et du coffre (point 4 de la nuit du 2026-11-21) : interagir avec le marchand ouvre la liste de ses offres,
    /// ouvrir un coffre ouvre le choix de ses améliorations. Clavier, souris (survol et clic, par la vue de la DA) et manette ;
    /// Échap ou B ferme. Un menu par joueur, dans sa vue (écran partagé). Le joueur ne bouge plus tant qu'il est ouvert ;
    /// le jeu continue. Les offres restent des <see cref="OfferOrb"/> (achat, prix, refus en rouge) : le menu ne fait que les
    /// présenter. La vue (ItHunts.HudArt) s'abonne à <see cref="Opened"/> ; sans elle, un menu greybox (IMGUI).
    /// </summary>
    public sealed class ShopMenu
    {
        public enum Kind : byte { Marchand, Coffre }

        private static readonly List<ShopMenu> OpenList = new List<ShopMenu>();
        public static IReadOnlyList<ShopMenu> All => OpenList;
        public static event Action<ShopMenu> Opened;

        public PlayerHealth Player { get; private set; }
        public PlayerInput Input { get; private set; }
        public Kind Type { get; private set; }
        public string Title { get; private set; }
        public readonly List<OfferOrb> Offers = new List<OfferOrb>();
        public GreyboxMenu Menu { get; private set; }
        public bool IsOpen { get; private set; }
        /// <summary>Vue de la DA posée (sinon menu greybox).</summary>
        public bool Skinned { get; set; }
        public int OpenedFrame { get; private set; }
        public event Action Closed;

        /// <summary>Index de l'offre qui a le focus (-1 : « Fermer »).</summary>
        public int FocusedOffer => Menu != null && Menu.Focus < Offers.Count ? Menu.Focus : -1;

        /// <summary>Ferme tous les menus (changement de scène, tests).</summary>
        public static void CloseAll()
        {
            foreach (ShopMenu m in new List<ShopMenu>(OpenList))
                m.Close();
        }

        public static ShopMenu Of(PlayerHealth p)
        {
            foreach (ShopMenu m in OpenList)
                if (m.Player == p)
                    return m;
            return null;
        }

        /// <summary>Ouvre le menu pour ce joueur (referme celui qu'il avait). Vrai si ouvert.</summary>
        public static ShopMenu Show(PlayerHealth p, Kind kind, string title, IEnumerable<OfferOrb> offers) =>
            Show(p, kind, title, () => offers);

        /// <summary>
        /// Comme ci-dessus, avec une source relue après chaque achat (le marchand de l'artefact retire de nouvelles offres après un
        /// achat).
        /// </summary>
        public static ShopMenu Show(PlayerHealth p, Kind kind, string title, Func<IEnumerable<OfferOrb>> source) =>
            Show(p, kind, title, source, null, null);

        /// <summary>
        /// Comme ci-dessus, avec une relance (coffre, point 3) : reroll tire de nouvelles offres (vrai si payé et fait), rerollPrice
        /// donne son prix actuel (-1 : pas de relance).
        /// </summary>
        public static ShopMenu Show(PlayerHealth p, Kind kind, string title, Func<IEnumerable<OfferOrb>> source, Func<bool> reroll, Func<int> rerollPrice)
        {
            if (p == null || p.Dead)
                return null;
            Of(p)?.Close();
            var m = new ShopMenu { Player = p, Type = kind, Title = title, Input = p.GetComponent<PlayerInput>(), OpenedFrame = Time.frameCount };
            m._source = source;
            m._reroll = reroll;
            m._rerollPrice = rerollPrice;
            m.Menu = new GreyboxMenu { Owner = m.Input != null ? PauseMenu.OwnerOf(m.Input) : Keyboard.current };
            m.Refresh();
            m.Menu.FocusFirst();
            m.IsOpen = true;
            OpenList.Add(m);
            if (m.Input != null && Application.isPlaying)
                m.Input.DeactivateInput();                       // il ne bouge plus ; le jeu continue
            if (m.Menu.Owner is Keyboard && Application.isPlaying)
            {
                PlayerLook.MenuOpen = true;                      // la souris sert au menu
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            ShopMenuDriver.Ensure();
            Opened?.Invoke(m);
            return m;
        }

        private Func<IEnumerable<OfferOrb>> _source;
        private Func<bool> _reroll;
        private Func<int> _rerollPrice;

        /// <summary>Prix actuel de la relance (-1 : pas de relance dans ce menu).</summary>
        public int RerollPrice => _reroll != null && _rerollPrice != null ? _rerollPrice() : -1;
        public bool CanReroll => RerollPrice >= 0;
        /// <summary>Ligne « Relancer » du menu (-1 : aucune) : juste après les offres, avant « Fermer ».</summary>
        public int RerollIndex => _rerollItem != null ? Menu.Items.IndexOf(_rerollItem) : -1;
        public int Rerolls { get; private set; }
        private GreyboxMenu.Item _rerollItem;

        /// <summary>Relance : paie et tire de nouvelles offres (coffre). Vrai si fait ; refus en rouge s'il manque des âmes.</summary>
        public bool Reroll()
        {
            if (!IsOpen || !CanReroll || !_reroll())
                return false;
            Rerolls++;
            Refresh();
            return true;
        }

        public bool RerollAffordable
        {
            get
            {
                int price = RerollPrice;
                var run = ItHunts.Artifacts.RunState.Current;
                return price >= 0 && (price == 0 || run == null || run.Souls(Player.PlayerId) >= price);
            }
        }

        /// <summary>Change à chaque nouvelle liste d'offres (la vue refait ses cartes).</summary>
        public int Version { get; private set; }

        /// <summary>Relit les offres (source) et refait les lignes du menu, focus gardé.</summary>
        public void Refresh()
        {
            int focus = Menu.Focus;
            Offers.Clear();
            if (_source != null)
                foreach (OfferOrb o in _source())
                    if (o != null && !o.Taken)
                        Offers.Add(o);
            Menu.Items.Clear();
            for (int i = 0; i < Offers.Count; i++)
            {
                int index = i;
                GreyboxMenu.Item it = Menu.Add(Label(i), () => Buy(index));
                it.Dim = () => !Affordable(index);
                it.Change = d => Menu.SetFocus(Mathf.Clamp(index + d, 0, Offers.Count - 1));   // cartes côte à côte : ◄ ►
            }

            _rerollItem = null;
            if (CanReroll)
            {
                _rerollItem = Menu.Add(RerollLabel, () => Reroll());
                _rerollItem.Dim = () => !RerollAffordable;
            }

            Menu.Add("Fermer", Close);
            Menu.SetFocus(Mathf.Clamp(focus, 0, Menu.Items.Count - 1));
            Version++;
        }

        public string RerollLabel
        {
            get
            {
                int price = RerollPrice;
                return price > 0 ? $"Relancer  ({price} âmes)" : "Relancer  (gratuit)";
            }
        }

        public string Label(int i)
        {
            OfferOrb o = i >= 0 && i < Offers.Count ? Offers[i] : null;
            if (o == null)
                return "—";
            string name = o.Upgrade != null ? o.Title : $"Soin (+{o.Heal:0} PV)";
            int price = o.PriceFor(Player);
            return o.Price > 0 ? (price > 0 ? $"{name}  ({price} âmes)" : $"{name}  (gratuit)") : name;
        }

        /// <summary>Nom de l'offre avec son rang : « Nom · Bronze » (nouvelle), « Nom · Bronze → Argent » (montée), « Soin ».</summary>
        public string Name(int i)
        {
            OfferOrb o = i >= 0 && i < Offers.Count ? Offers[i] : null;
            return o == null ? string.Empty : o.Title;
        }

        /// <summary>Effet du rang visé (texte du tableau) ; un passif le dit, et prévient s'il remplace le passif porté.</summary>
        public string Description(int i)
        {
            OfferOrb o = i >= 0 && i < Offers.Count ? Offers[i] : null;
            if (o == null)
                return string.Empty;
            if (o.Upgrade == null)
                return $"Rend {o.Heal:0} PV.";
            string text = o.IsRankUp ? o.Effect : $"{o.Upgrade.description} {o.Effect}".Trim();
            if (o.Upgrade.isPassive && !o.IsRankUp)
            {
                var run = ItHunts.Artifacts.RunState.Current;
                UpgradeDefinition worn = run != null && Player != null ? run.Slots(Player.PlayerId).Passive : null;
                text = worn != null && worn != o.Upgrade ? $"Passif : remplace {worn.displayName} (son rang est perdu). {text}" : $"Passif (un seul porté). {text}";
            }

            return text;
        }

        /// <summary>Prix affiché (0 : choix gratuit d'un coffre).</summary>
        public int Price(int i) => i >= 0 && i < Offers.Count && Offers[i] != null ? Offers[i].PriceFor(Player) : 0;

        public bool Taken(int i) => i < 0 || i >= Offers.Count || Offers[i] == null || Offers[i].Taken;

        public bool Affordable(int i)
        {
            if (Taken(i))
                return false;
            int price = Price(i);
            var run = ItHunts.Artifacts.RunState.Current;
            return price <= 0 || run == null || run.Souls(Player.PlayerId) >= price;
        }

        /// <summary>
        /// Achète (ou choisit) l'offre i : mêmes règles que l'orbe (paiement, refus en rouge). Coffre : un seul choix, le menu se
        /// ferme. Vrai si pris.
        /// </summary>
        public bool Buy(int i)
        {
            if (!IsOpen || Taken(i))
                return false;
            OfferOrb o = Offers[i];
            if (!o.TakeFromMenu(Player))
                return false;
            if (Type == Kind.Coffre)
                Close();
            else
            {
                Refresh();                                       // marchand : offres retirées de nouveau après un achat
                if (Offers.Count == 0)
                    Close();
            }

            return true;
        }

        private bool AllTaken()
        {
            for (int i = 0; i < Offers.Count; i++)
                if (!Taken(i))
                    return false;
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
                return;
            IsOpen = false;
            OpenList.Remove(this);
            ShopMenuDriver.ReactivateNextFrame(this);
            Closed?.Invoke();
        }

        /// <summary>Remise de l'entrée et du curseur (l'image d'après : Échap ne rouvre pas la pause).</summary>
        internal void Restore()
        {
            if (Input != null)
                Input.ActivateInput();
            bool keyboardStillOpen = false;
            foreach (ShopMenu m in OpenList)
                keyboardStillOpen |= m.Menu.Owner is Keyboard;
            if (Menu.Owner is Keyboard && !keyboardStillOpen && (PauseMenu.Current == null || PauseMenu.Current.Opened.Count == 0))
            {
                PlayerLook.MenuOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>Une image : navigation, achat, retour (Échap / B / Start).</summary>
        internal void Tick()
        {
            if (Player == null || Player.Dead)
            {
                Close();
                return;
            }

            if (Time.frameCount > OpenedFrame && PauseMenu.BackPressed(Menu.Owner))
            {
                Close();
                return;
            }

            if (Time.frameCount > OpenedFrame)
                Menu.Update();
        }
    }

    /// <summary>Fait vivre les menus de boutique (une image à la fois) et dessine le menu greybox quand la DA n'est pas posée.</summary>
    public sealed class ShopMenuDriver : MonoBehaviour
    {
        private static ShopMenuDriver _instance;
        private readonly List<(ShopMenu menu, int frame)> _restore = new List<(ShopMenu, int)>();

        public static void Ensure()
        {
            if (_instance != null || !Application.isPlaying)
                return;
            _instance = new GameObject("Menus de boutique").AddComponent<ShopMenuDriver>();
        }

        internal static void ReactivateNextFrame(ShopMenu m)
        {
            if (_instance == null || !Application.isPlaying)
            {
                m.Restore();
                return;
            }

            _instance._restore.Add((m, Time.frameCount));
        }

        private void Update()
        {
            for (int i = _restore.Count - 1; i >= 0; i--)
                if (Time.frameCount > _restore[i].frame)
                {
                    _restore[i].menu.Restore();
                    _restore.RemoveAt(i);
                }

            var open = new List<ShopMenu>(ShopMenu.All);
            foreach (ShopMenu m in open)
                m.Tick();
        }

        private void OnGUI()
        {
            foreach (ShopMenu m in ShopMenu.All)
            {
                if (m.Skinned || m.Input == null || m.Input.camera == null)
                    continue;
                int f = m.FocusedOffer;
                string desc = f >= 0 ? m.Description(f) : "Échap / B : fermer";
                Rect r = m.Input.camera.pixelRect;
                m.Menu.Draw(m.Title, desc, 560f, new Rect(r.x, Screen.height - r.yMax, r.width, r.height));
            }
        }
    }
}
