using System;
using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Orbe flottante d'une amélioration (D-043) : choix au-dessus d'un coffre ouvert (gratuit, pour celui qui l'a ouvert) ou offre
    /// du marchand (avec son prix, pour tout joueur). Nom et icône en monde ; on la prend avec E / Y ou F en la regardant. Un soin
    /// du marchand est aussi une orbe (sans amélioration).
    /// </summary>
    public sealed class OfferOrb : MonoBehaviour, IInteractable
    {
        public UpgradeDefinition Upgrade { get; private set; }
        /// <summary>Prix de base (0 : gratuit, choix d'un coffre) ; le prix payé monte à chaque achat au marchand (PriceFor).</summary>
        public int Price { get; private set; }
        public EconomySettings Pricing { get; set; }
        public int PriceFor(PlayerHealth p) => Prices.For(Pricing, p, PriceKind.Marchand, Price);
        public float Heal { get; private set; }
        /// <summary>Seul ce joueur peut la prendre (0 : tout joueur).</summary>
        public int RestrictedTo { get; private set; }
        public bool Taken { get; private set; }
        public float Range { get; private set; } = 8f;
        public bool AcceptsGrab => true;
        public Vector3 Point => transform.position;

        /// <summary>Prise (par qui) : le coffre fait disparaître les autres, le marchand retire l'offre.</summary>
        public event Action<OfferOrb, PlayerHealth> TakenBy;

        private Vector3 _base;
        private float _phase;

        public static OfferOrb Create(Transform parent, Vector3 at, UpgradeDefinition upgrade, int price, float heal, int restrictedTo,
            float range, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = upgrade != null ? $"Orbe : {upgrade.displayName}" : "Orbe : soin";
            Interactables.Discard(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.55f;
            var r = go.GetComponent<MeshRenderer>();
            if (material != null)
            {
                r.material = material;
                Color c = upgrade != null ? upgrade.color : new Color(0.4f, 3f, 0.6f);
                r.material.SetColor("_BaseColor", c);
                r.material.SetColor("_EmissionColor", c);
            }

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (restrictedTo != 0)
                go.AddComponent<ViewerOnly>().PlayerId = restrictedTo;  // choix d'un coffre : vu par ce joueur seul
            var orb = go.AddComponent<OfferOrb>();
            orb.Upgrade = upgrade;
            orb.Price = price;
            orb.Heal = heal;
            orb.RestrictedTo = restrictedTo;
            orb.Range = range;
            orb._base = at;
            orb._phase = at.x * 0.37f + at.z * 0.21f;
            string title = upgrade != null ? $"{upgrade.iconSymbol}\n{upgrade.displayName}" : $"+{heal:0} PV\nSoin";
            string priceText = price > 0 ? $"\n{price} âmes" : string.Empty;
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
            string what = Upgrade != null ? $"{Upgrade.displayName} : {Upgrade.description}" : $"soin, +{Heal:0} PV";
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
            if (Price > 0 && !Prices.TryBuy(Pricing, p, PriceKind.Marchand, Price))
                return;                                                  // refus en rouge (RunState.Refused)
            Taken = true;
            if (Upgrade != null)
            {
                if (run != null)
                    run.Grant(p.PlayerId, Upgrade);
                PlayerUpgrades.Of(p).Apply(Upgrade);
            }

            if (Heal > 0f)
                p.Heal(Heal);
            TakenBy?.Invoke(this, p);
            Interactables.Discard(gameObject);
        }
    }
}
