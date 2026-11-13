using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Marchand de la salle de départ (D-050) : améliorations de l'artefact (barillet pour l'instant). Chaque joueur voit ses
    /// propres offres (3 tirées au hasard dans le pool, avec la seed de la run) ; il en achète une, ses offres sont tirées de
    /// nouveau. Prix de base de chaque amélioration, plus la hausse par achat du marchand (par joueur, EconomySettings).
    /// Les coffres donnent les améliorations de stats ; le marchand placé au hasard (D-043) n'est plus utilisé.
    /// </summary>
    public sealed class ArtifactMerchant : MonoBehaviour
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Améliorations de l'artefact proposées (pool du barillet).")]
        [SerializeField] private UpgradePool pool;
        [SerializeField] private Material orbMaterial;
        [Tooltip("Nombre d'offres par joueur.")]
        [Min(1)] [SerializeField] private int offers = 3;

        private readonly Dictionary<int, List<OfferOrb>> _offers = new Dictionary<int, List<OfferOrb>>();
        private int _draws;

        public UpgradePool Pool => pool;
        public IReadOnlyList<OfferOrb> OffersFor(int playerId) => _offers.TryGetValue(playerId, out List<OfferOrb> l) ? l : new List<OfferOrb>();
        public int Purchases { get; private set; }

        private void Update()
        {
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !_offers.ContainsKey(p.PlayerId))
                    Stock(p.PlayerId);
        }

        /// <summary>Remise à zéro de la zone : offres de tout le monde retirées (de nouveau tirées à la prochaine image).</summary>
        public void ResetMerchant()
        {
            foreach (List<OfferOrb> l in _offers.Values)
                foreach (OfferOrb o in l)
                    if (o != null)
                        Interactables.Discard(o.gameObject);
            _offers.Clear();
            _draws = 0;
        }

        /// <summary>Tire les offres d'un joueur (orbes vues par lui seul).</summary>
        public void Stock(int playerId)
        {
            if (_offers.TryGetValue(playerId, out List<OfferOrb> old))
                foreach (OfferOrb o in old)
                    if (o != null && !o.Taken)
                        Interactables.Discard(o.gameObject);
            var list = new List<OfferOrb>();
            _offers[playerId] = list;
            if (pool == null || pool.upgrades.Count == 0)
                return;
            RunState run = RunState.Current;
            int seed = (run != null ? run.Seed : 1) * 31 + playerId;
            int[] idx = SoulMath.Draw(seed, _draws++, pool.upgrades.Count, Mathf.Min(offers, pool.upgrades.Count));
            float range = settings != null ? settings.orbInteractRange : 8f;
            for (int i = 0; i < idx.Length; i++)
            {
                UpgradeDefinition u = pool.upgrades[idx[i]];
                float x = (i - (idx.Length - 1) * 0.5f) * 2.4f;
                Vector3 at = transform.position + transform.forward * 1.6f + transform.right * x + Vector3.up * (1.6f + (i % 2 == 1 ? 0.6f : 0f));
                OfferOrb orb = OfferOrb.Create(transform, at, u, u.merchantPrice, 0f, playerId, range, orbMaterial);
                orb.Pricing = settings;
                orb.TakenBy += (o, p) =>
                {
                    Purchases++;
                    Stock(p.PlayerId);                                    // nouveau choix parmi 3, prix plus haut
                };
                list.Add(orb);
            }
        }
    }
}
