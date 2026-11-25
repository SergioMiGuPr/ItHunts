using System;
using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Offre d'une amélioration (D-043, rangs D-100) : choix d'un coffre (gratuit, pour celui qui l'a ouvert) ou offre du marchand
    /// (avec son prix). Une offre mène à un rang : nouvelle (Bronze, ou Argent direct) ou montée d'un rang d'une amélioration
    /// possédée (« Nom · Bronze → Argent »). Un soin du marchand est aussi une offre (sans amélioration). Présentée par le menu
    /// (ShopMenu) ; l'ancienne orbe flottante en monde ne sert plus qu'aux scènes de test (WorldVisible).
    /// </summary>
    public sealed class OfferOrb : MonoBehaviour, IInteractable
    {
        public UpgradeDefinition Upgrade { get; private set; }
        /// <summary>Prix de base (0 : gratuit, choix d'un coffre) ; le prix payé monte à chaque achat au marchand (PriceFor).</summary>
        public int Price { get; private set; }
        public EconomySettings Pricing { get; set; }
        /// <summary>Catégorie de prix (hausse par achat) : marchand, ou soin (point 3 : sa propre hausse).</summary>
        public PriceKind Kind { get; set; } = PriceKind.Marchand;
        public int PriceFor(PlayerHealth p) => FreeFor(p) ? 0 : Prices.For(Pricing, p, Kind, Price);
        public float Heal { get; private set; }
        /// <summary>Seul ce joueur peut la prendre (0 : tout joueur).</summary>
        public int RestrictedTo { get; private set; }
        public bool Taken { get; private set; }
        public float Range { get; private set; } = 8f;
        public bool AcceptsGrab => true;
        public Vector3 Point => transform.position;

        /// <summary>Rang visé (0 Bronze … 4 Mythique) ; rang de départ (−1 : nouvelle).</summary>
        public int TargetRank { get; set; }
        public int FromRank { get; set; } = -1;
        public bool IsRankUp => Upgrade != null && FromRank >= 0;
        /// <summary>Titre de l'offre : « Nom · Bronze » ou « Nom · Bronze → Argent » ; « Soin ».</summary>
        public string Title => Upgrade != null ? Upgrade.OfferTitle(FromRank, TargetRank) : "Soin";
        /// <summary>Effet du rang visé.</summary>
        public string Effect => Upgrade != null ? Upgrade.RankText(TargetRank) : $"Rend {Heal:0} PV.";

        /// <summary>Marchandage Mythique : la première montée de rang de chaque acte est gratuite pour ce joueur.</summary>
        public bool FreeFor(PlayerHealth p)
        {
            if (!IsRankUp || p == null || Price <= 0)
                return false;
            var up = PlayerUpgrades.OfId(p.PlayerId);
            RunState run = RunState.Current;
            return up != null && up.Mythic(UpgradeKind.Marchandage) && run != null && !run.FreeRankUpUsed(p.PlayerId, ItHunts.Level.RunSession.Act);
        }

        /// <summary>Prise (par qui) : le coffre fait disparaître les autres, le marchand retire l'offre.</summary>
        public event Action<OfferOrb, PlayerHealth> TakenBy;

        private Vector3 _base;
        private float _phase;

        /// <summary>
        /// Nuit 4, point 1 : les offres du marchand et des coffres ne flottent plus dans le monde (orbes lumineuses et textes hors DA) ;
        /// tout passe par le menu du marchand ou du coffre. Vrai : anciennes orbes visibles (mesures, scènes de test).
        /// </summary>
        public static bool WorldVisible;

        public static OfferOrb Create(Transform parent, Vector3 at, UpgradeDefinition upgrade, int price, float heal, int restrictedTo,
            float range, Material material, int fromRank = -1, int targetRank = 0)
        {
            GameObject go;
            if (WorldVisible)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Interactables.Discard(go.GetComponent<Collider>());
                var r = go.GetComponent<MeshRenderer>();
                if (material != null)
                {
                    r.material = material;
                    Color c = upgrade != null ? upgrade.color : new Color(0.4f, 3f, 0.6f);
                    r.material.SetColor("_BaseColor", c);
                    r.material.SetColor("_EmissionColor", c);
                }

                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
                go = new GameObject();                                    // porteur de données seulement (menu de la DA)
            go.name = upgrade != null ? $"Orbe : {upgrade.displayName}" : "Orbe : soin";
            go.transform.SetParent(parent, true);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.55f;
            if (restrictedTo != 0)
                go.AddComponent<ViewerOnly>().PlayerId = restrictedTo;  // choix d'un coffre : vu par ce joueur seul
            var orb = go.AddComponent<OfferOrb>();
            orb.Upgrade = upgrade;
            orb.Price = price;
            orb.Heal = heal;
            orb.RestrictedTo = restrictedTo;
            orb.Range = range;
            orb.FromRank = fromRank;
            orb.TargetRank = Mathf.Clamp(targetRank, 0, UpgradeDefinition.MaxRank);
            orb._base = at;
            orb._phase = at.x * 0.37f + at.z * 0.21f;
            string title = upgrade != null ? $"{upgrade.iconSymbol}\n{orb.Title}" : $"+{heal:0} PV\nSoin";
            string priceText = price > 0 ? $"\n{price} âmes" : string.Empty;
            if (!WorldVisible)
                return orb;
            // Texte au-dessus de l'orbe, à l'échelle du monde (l'orbe est réduite).
            var holder = new GameObject("Étiquette").transform;
            holder.SetParent(go.transform, false);
            holder.localScale = Vector3.one / 0.55f;
            WorldLabel label = WorldLabel.Create(holder, new Vector3(0f, 0.95f, 0f), title + priceText, Color.white, 0.28f, go.transform);
            if (price > 0)
                label.TextFor = viewer => viewer != null ? $"{title}\n{orb.PriceFor(viewer)} âmes" : title + priceText;   // prix de ce joueur (hausse par achat)
            return orb;
        }

        private void OnEnable() => Interactables.All.Add(this);
        private void OnDisable() => Interactables.All.Remove(this);

        private void Update()
        {
            float t = Time.time + _phase;
            transform.position = _base + Vector3.up * (Mathf.Sin(t * 1.6f) * 0.12f);
            transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }

        /// <summary>Offre présentée par un menu (marchand, coffre) : on ne la prend plus directement avec E / F.</summary>
        public bool MenuOnly { get; set; }

        public bool Available(PlayerHealth p) => !MenuOnly && CanTake(p);

        private bool CanTake(PlayerHealth p) => !Taken && p != null && (RestrictedTo == 0 || RestrictedTo == p.PlayerId);

        /// <summary>Prise depuis le menu du marchand ou du coffre : mêmes règles que l'orbe. Vrai si prise.</summary>
        public bool TakeFromMenu(PlayerHealth p)
        {
            if (!CanTake(p))
                return false;
            Take(p);
            return Taken;
        }

        public string Prompt(PlayerHealth p)
        {
            string what = Upgrade != null ? $"{Title} : {Effect}" : $"soin, +{Heal:0} PV";
            return Price > 0 ? $"acheter {what} ({PriceFor(p)} âmes)" : $"prendre {what}";
        }

        public void Interact(PlayerHealth p)
        {
            if (Available(p))
                Take(p);
        }

        private void Take(PlayerHealth p)
        {
            RunState run = RunState.Current;
            bool free = FreeFor(p);
            if (Price > 0 && !free && !Prices.TryBuy(Pricing, p, Kind, Price))
                return;                                                  // refus en rouge (RunState.Refused)
            if (free && run != null)
                run.TryUseFreeRankUp(p.PlayerId, ItHunts.Level.RunSession.Act);
            Taken = true;
            if (Upgrade != null)
            {
                PlayerUpgrades up = PlayerUpgrades.Of(p);
                if (run != null)
                {
                    // La run fait foi (emplacements, passif, rangs) ; le joueur recopie puis applique ses effets.
                    UpgradeGrant g = IsRankUp ? run.Grant(p.PlayerId, Upgrade) : run.Grant(p.PlayerId, Upgrade, TargetRank);
                    if (!g.Ok)
                    {
                        Taken = false;
                        RunState.RaiseRefused(p.PlayerId);
                        return;
                    }

                    up?.SyncFrom(run);
                }
                else
                    up?.Apply(Upgrade);
            }

            if (Heal > 0f)
                p.Heal(Heal);
            TakenBy?.Invoke(this, p);
            Interactables.Discard(gameObject);
        }
    }
}
