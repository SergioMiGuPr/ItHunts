using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Marchand (D-043) : placé au hasard parmi ses emplacements avec la seed de la run ; ses offres sont des orbes flottantes
    /// (3 améliorations tirées avec la seed et un soin), avec leur prix, achetées avec E / Y (ou F) ; une offre achetée disparaît.
    /// Étape 8 : un marchand en plus dans la zone de départ sûre des zones 2 et 3.
    /// </summary>
    public sealed class Merchant : MonoBehaviour, IInteractable
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Emplacements possibles (un est tiré avec la seed de la run).")]
        [SerializeField] private Transform[] spots;
        [SerializeField] private Material orbMaterial;

        private readonly List<OfferOrb> _offers = new List<OfferOrb>();
        private int _placedForSeed = int.MinValue;

        public int SpotIndex { get; private set; } = -1;
        public IReadOnlyList<OfferOrb> Offers => _offers;

        private void Start() => Place();

        private void OnEnable() => Interactables.All.Add(this);
        private void OnDisable() => Interactables.All.Remove(this);

        // Interagir ouvre le menu de ses offres (point 4, 2026-11-21) ; les orbes restent la vitrine, prises par le menu seulement.
        public Vector3 Point => transform.position + Vector3.up * 1.4f;
        public float Range => settings != null ? settings.orbInteractRange : 8f;
        public bool AcceptsGrab => false;

        public bool Available(PlayerHealth p)
        {
            if (p == null || p.Dead || ShopMenu.Of(p) != null)
                return false;
            foreach (OfferOrb o in _offers)
                if (o != null && !o.Taken)
                    return true;
            return false;
        }

        public string Prompt(PlayerHealth p) => "voir les offres du marchand";

        public void Interact(PlayerHealth p) => ShopMenu.Show(p, ShopMenu.Kind.Marchand, "Marchand", _offers);

        /// <summary>Place le marchand (emplacement tiré avec la seed) et pose ses offres ; remise à zéro de la zone : de nouveau.</summary>
        public void Place()
        {
            RunState run = RunState.Current;
            int seed = run != null ? run.Seed : 1;
            foreach (OfferOrb o in _offers)
                if (o != null)
                    Destroy(o.gameObject);
            _offers.Clear();
            if (spots != null && spots.Length > 0)
            {
                SpotIndex = SoulMath.PickSpot(seed, spots.Length);
                transform.SetPositionAndRotation(spots[SpotIndex].position, spots[SpotIndex].rotation);
            }

            _placedForSeed = seed;
            // Offres à rangs (point 3) : des montées de rang seulement, puis le soin ; tirées pour le premier joueur (ancien marchand).
            PlayerHealth first = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0] : null;
            var drawn = ArtifactMerchant.OffersFor(run, first, settings != null ? settings.merchantRankUpOffers : 2);
            float range = settings != null ? settings.orbInteractRange : 8f;
            int n = drawn.Count + 1;
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * 2.4f;
                Vector3 at = transform.position + transform.forward * 1.6f + transform.right * x + Vector3.up * (1.6f + (i % 2 == 1 ? 0.6f : 0f));
                OfferOrb orb = i < drawn.Count
                    ? OfferOrb.Create(transform, at, drawn[i].u, settings != null ? settings.RankPrice(drawn[i].to) : drawn[i].u.merchantPrice, 0f, 0, range, orbMaterial, drawn[i].from, drawn[i].to)
                    : OfferOrb.Create(transform, at, null, settings != null ? settings.healPrice : 35, settings != null ? settings.healAmount : 40f, 0, range, orbMaterial);
                orb.Pricing = settings;
                if (orb.Upgrade == null)
                    orb.Kind = PriceKind.Soin;
                orb.MenuOnly = true;
                orb.TakenBy += (o, p) => _offers.Remove(o);
                _offers.Add(orb);
            }
        }
    }
}
