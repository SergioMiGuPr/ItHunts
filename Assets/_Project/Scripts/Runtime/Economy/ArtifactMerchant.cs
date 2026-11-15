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
    /// Les coffres donnent les améliorations de stats ; le marchand placé au hasard (D-043) n'est plus utilisé. 4e offre fixe :
    /// le soin (+40 PV, 35 âmes).
    /// </summary>
    public sealed class ArtifactMerchant : MonoBehaviour
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Améliorations de l'artefact proposées (pool du barillet).")]
        [SerializeField] private UpgradePool pool;
        [SerializeField] private Material orbMaterial;
        [Tooltip("Nombre d'offres par joueur.")]
        [Min(1)] [SerializeField] private int offers = 3;
        [Tooltip("Offre de soin en plus (prix et PV dans EconomySettings).")]
        [SerializeField] private bool withHeal = true;

        private readonly Dictionary<int, List<OfferOrb>> _offers = new Dictionary<int, List<OfferOrb>>();
        private int _draws;

        public UpgradePool Pool => pool;
        public IReadOnlyList<OfferOrb> OffersFor(int playerId) => _offers.TryGetValue(playerId, out List<OfferOrb> l) ? l : new List<OfferOrb>();
        public int Purchases { get; private set; }

        /// <summary>Décalage d'une offre (repère du marchand, m) : rangée devant lui, une sur deux plus haute ; le soin au bout.</summary>
        public static Vector3 OfferOffset(int i, int count) =>
            new Vector3((i - (count - 1) * 0.5f) * 2.4f, 1.6f + (i % 2 == 1 ? 0.6f : 0f), 1.6f);

        /// <summary>Décalage du soin (après les count offres, à la hauteur basse).</summary>
        public static Vector3 HealOffset(int count) => new Vector3((count - (count - 1) * 0.5f) * 2.4f, 1.6f, 1.6f);

        /// <summary>
        /// Enseigne (enfant « Enseigne ») : au-dessus de toute la rangée d'offres, dans le même plan que leurs textes, avec 1.6 m de
        /// marge sur le plus haut (orbe la plus haute + texte de 3 lignes + flottement) : elle ne cache aucune étiquette d'offre,
        /// sous aucun angle de face (corrections des étapes 8 et 9 ; avant, elle était derrière les textes, à 2.9 m).
        /// </summary>
        public static Vector3 SignOffset(int count, bool heal, float signHeight)
        {
            float minX = OfferOffset(0, count).x, maxX = heal ? HealOffset(count).x : OfferOffset(count - 1, count).x, top = 0f;
            for (int i = 0; i < count; i++)
                top = Mathf.Max(top, OfferOffset(i, count).y);
            top += 0.95f + 3f * 0.28f * 0.5f + 0.12f;                   // texte au-dessus de l'orbe (3 lignes de 0.28 m) + flottement
            return new Vector3((minX + maxX) * 0.5f, top + 1.6f + signHeight * 0.5f, 1.6f);
        }

        private void Start()
        {
            Transform sign = transform.Find("Enseigne");
            if (sign != null)
                sign.position = transform.TransformPoint(SignOffset(offers, withHeal, sign.lossyScale.y));
        }

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

        private static bool HasBarrel(int playerId)
        {
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && p.PlayerId == playerId)
                {
                    var holder = p.GetComponent<ItHunts.Artifacts.ArtifactHolder>();
                    holder?.EnsureReady();
                    return holder == null || holder.Current == null || holder.Current is ItHunts.Settings.BarrelSettings;
                }

            return true;
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
            // Améliorations du barillet seulement pour un joueur qui a le barillet ; la lanterne de feu (D-065) n'en a pas encore :
            // il ne voit que le soin.
            int[] idx = HasBarrel(playerId)
                ? SoulMath.Draw(seed, _draws++, pool.upgrades.Count, Mathf.Min(offers, pool.upgrades.Count))
                : new int[0];
            float range = settings != null ? settings.orbInteractRange : 8f;
            for (int i = 0; i < idx.Length; i++)
            {
                UpgradeDefinition u = pool.upgrades[idx[i]];
                Vector3 at = transform.TransformPoint(OfferOffset(i, idx.Length));
                OfferOrb orb = OfferOrb.Create(transform, at, u, u.merchantPrice, 0f, playerId, range, orbMaterial);
                orb.Pricing = settings;
                orb.TakenBy += (o, p) =>
                {
                    Purchases++;
                    Stock(p.PlayerId);                                    // nouveau choix parmi 3, prix plus haut
                };
                list.Add(orb);
            }

            // 4e offre fixe : le soin (+40 PV pour 35 âmes, EconomySettings) ; seul moyen de se soigner pendant une zone.
            if (withHeal)
            {
                Vector3 hat = transform.TransformPoint(HealOffset(idx.Length));
                OfferOrb heal = OfferOrb.Create(transform, hat, null, settings != null ? settings.healPrice : 35,
                    settings != null ? settings.healAmount : 40f, playerId, range, orbMaterial);
                heal.Pricing = settings;
                heal.TakenBy += (o, p) =>
                {
                    Purchases++;
                    Stock(p.PlayerId);
                };
                list.Add(heal);
            }
        }
    }
}
