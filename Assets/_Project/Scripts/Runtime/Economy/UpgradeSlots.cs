using System.Collections.Generic;
using ItHunts.Settings;

namespace ItHunts.Economy
{
    /// <summary>Résultat d'une prise d'amélioration (nouvelle, montée de rang, passif remplacé, refusée).</summary>
    public enum UpgradeGrantKind : byte { Refuse, Nouvelle, Montee, PassifRemplace }

    public struct UpgradeGrant
    {
        public UpgradeGrantKind Kind;
        /// <summary>Rang obtenu (0 Bronze … 4 Mythique) ; −1 si refusée.</summary>
        public int Rank;
        /// <summary>Passif retiré (rang perdu) quand un second passif est pris.</summary>
        public UpgradeDefinition Replaced;
        public bool Ok => Kind != UpgradeGrantKind.Refuse;
    }

    /// <summary>
    /// Emplacements d'améliorations d'un joueur (nuit 4, D-100), données pures et testables : au plus <see cref="MaxSlots"/>
    /// améliorations différentes, chacune à un rang (Bronze → Mythique) ; au plus un passif, le second remplace le premier (rang
    /// perdu). Une amélioration déjà possédée monte d'un rang au lieu de prendre un emplacement. Le même objet sert à l'état de la
    /// run (RunState, qui fait foi et voyage d'un acte à l'autre) et aux effets du joueur (PlayerUpgrades).
    /// </summary>
    public sealed class UpgradeSlots
    {
        public const int MaxSlots = 10;
        public const int MaxRank = UpgradeDefinition.MaxRank;

        private readonly List<UpgradeDefinition> _owned = new List<UpgradeDefinition>();
        private readonly Dictionary<UpgradeDefinition, int> _ranks = new Dictionary<UpgradeDefinition, int>();

        /// <summary>Améliorations possédées, dans l'ordre d'acquisition (le passif en premier à l'affichage, voir Sorted).</summary>
        public IReadOnlyList<UpgradeDefinition> Owned => _owned;
        public int Count => _owned.Count;
        public bool Full => _owned.Count >= MaxSlots;

        /// <summary>Rang d'une amélioration possédée (0 Bronze … 4 Mythique) ; −1 si elle ne l'est pas.</summary>
        public int RankOf(UpgradeDefinition u) => u != null && _ranks.TryGetValue(u, out int r) ? r : -1;
        public bool Has(UpgradeDefinition u) => RankOf(u) >= 0;

        /// <summary>Le passif porté (null sans passif).</summary>
        public UpgradeDefinition Passive
        {
            get
            {
                foreach (UpgradeDefinition u in _owned)
                    if (u != null && u.isPassive)
                        return u;
                return null;
            }
        }

        /// <summary>Possédées, le passif en premier (HUD, écran de fin).</summary>
        public List<UpgradeDefinition> Sorted()
        {
            var r = new List<UpgradeDefinition>(_owned.Count);
            UpgradeDefinition p = Passive;
            if (p != null)
                r.Add(p);
            foreach (UpgradeDefinition u in _owned)
                if (u != null && u != p)
                    r.Add(u);
            return r;
        }

        /// <summary>Possédées qui peuvent encore monter (pas Mythiques).</summary>
        public List<UpgradeDefinition> RankUpCandidates()
        {
            var r = new List<UpgradeDefinition>();
            foreach (UpgradeDefinition u in _owned)
                if (u != null && RankOf(u) < MaxRank)
                    r.Add(u);
            return r;
        }

        /// <summary>
        /// Prend une amélioration : possédée → monte d'un rang (jamais au-delà de Mythique) ; nouvelle → Bronze si un emplacement
        /// est libre ; passif alors qu'un autre est porté → l'autre est retiré (rang perdu) ; emplacements pleins → refusée.
        /// </summary>
        public UpgradeGrant Grant(UpgradeDefinition u) => Grant(u, 0);

        /// <summary>Comme Grant, une nouvelle arrivant directement au rang startRank (coffre « Argent direct », Œil du collectionneur).</summary>
        public UpgradeGrant Grant(UpgradeDefinition u, int startRank)
        {
            if (u == null)
                return new UpgradeGrant { Kind = UpgradeGrantKind.Refuse, Rank = -1 };
            if (_ranks.TryGetValue(u, out int rank))
            {
                if (rank >= MaxRank)
                    return new UpgradeGrant { Kind = UpgradeGrantKind.Refuse, Rank = rank };
                _ranks[u] = rank + 1;
                return new UpgradeGrant { Kind = UpgradeGrantKind.Montee, Rank = rank + 1 };
            }

            UpgradeDefinition replaced = null;
            if (u.isPassive)
            {
                replaced = Passive;
                if (replaced != null)
                    Remove(replaced);
            }

            if (Full)
            {
                if (replaced != null)
                    Set(replaced, 0);                                     // on rend le passif retiré : rien n'a changé
                return new UpgradeGrant { Kind = UpgradeGrantKind.Refuse, Rank = -1 };
            }

            int start = UnityEngine.Mathf.Clamp(startRank, 0, MaxRank);
            _owned.Add(u);
            _ranks[u] = start;
            return new UpgradeGrant { Kind = replaced != null ? UpgradeGrantKind.PassifRemplace : UpgradeGrantKind.Nouvelle, Rank = start, Replaced = replaced };
        }

        /// <summary>Pose un rang (reprise d'un acte à l'autre, panneau admin, tests) ; ajoute l'amélioration si besoin.</summary>
        public void Set(UpgradeDefinition u, int rank)
        {
            if (u == null)
                return;
            if (!_ranks.ContainsKey(u))
                _owned.Add(u);
            _ranks[u] = UnityEngine.Mathf.Clamp(rank, 0, MaxRank);
        }

        public bool Remove(UpgradeDefinition u)
        {
            if (u == null || !_ranks.Remove(u))
                return false;
            _owned.Remove(u);
            return true;
        }

        public void Clear()
        {
            _owned.Clear();
            _ranks.Clear();
        }

        public void CopyFrom(UpgradeSlots other)
        {
            Clear();
            if (other == null)
                return;
            foreach (UpgradeDefinition u in other._owned)
                Set(u, other.RankOf(u));
        }

        /// <summary>Copie des rangs (sauvegarde d'un acte à l'autre).</summary>
        public Dictionary<UpgradeDefinition, int> RanksCopy() => new Dictionary<UpgradeDefinition, int>(_ranks);

        /// <summary>Recharge depuis une liste et ses rangs (une amélioration sans rang : Bronze).</summary>
        public void Load(IEnumerable<UpgradeDefinition> owned, Dictionary<UpgradeDefinition, int> ranks)
        {
            Clear();
            if (owned == null)
                return;
            foreach (UpgradeDefinition u in owned)
                if (u != null && !_ranks.ContainsKey(u))
                    Set(u, ranks != null && ranks.TryGetValue(u, out int r) ? r : 0);
        }
    }

    /// <summary>Règles communes des offres (marchand, coffres) : qui voit quoi, candidates, personnage d'un joueur. Pur, testable.</summary>
    public static class UpgradeRules
    {
        /// <summary>Personnage (enum des améliorations) d'une définition de personnage, par son identifiant stable.</summary>
        public static UpgradeCharacter CharacterOf(CharacterDefinition c) => c == null ? UpgradeCharacter.Aucun : c.id switch
        {
            "renard" => UpgradeCharacter.Renard,
            "cameleon" => UpgradeCharacter.Cameleon,
            "koala" => UpgradeCharacter.Koala,
            "hibou" => UpgradeCharacter.Hibou,
            _ => UpgradeCharacter.Aucun,
        };

        /// <summary>Personnage déduit de l'artefact porté (scènes de test sans liste de personnages).</summary>
        public static UpgradeCharacter CharacterOf(ArtifactDefinition a) => a switch
        {
            BarrelSettings => UpgradeCharacter.Cameleon,
            FireLanternSettings => UpgradeCharacter.Renard,
            BoneSwordSettings => UpgradeCharacter.Koala,
            SpiritBookSettings => UpgradeCharacter.Hibou,
            _ => UpgradeCharacter.Aucun,
        };

        /// <summary>Visible par ce personnage : générique, ou l'une des siennes (l'ancien champ forArtifact est encore respecté).</summary>
        public static bool VisibleBy(UpgradeDefinition u, UpgradeCharacter c, ArtifactDefinition artifact = null)
        {
            if (u == null)
                return false;
            if (u.forArtifact != null && artifact != null && !SameArtifact(u.forArtifact, artifact))
                return false;
            if (u.needsFire && !SetsFire(c))
                return false;                                            // D-125 : Allumette sans lanterne ne sert à rien
            return u.Generic || u.character == c;
        }

        /// <summary>Personnage qui met le feu aux ennemis par son artefact (lanterne du renard).</summary>
        public static bool SetsFire(UpgradeCharacter c) => c == UpgradeCharacter.Renard;

        /// <summary>Même artefact (l'asset ou sa copie par joueur faite par les améliorations).</summary>
        public static bool SameArtifact(ArtifactDefinition a, ArtifactDefinition b) =>
            a == b || (a != null && b != null && (b.name == a.name || b.name.StartsWith(a.name + " ")) && a.GetType() == b.GetType());

        /// <summary>Candidates à une amélioration nouvelle : visibles et pas possédées ; aucune si les emplacements sont pleins.</summary>
        public static List<UpgradeDefinition> NewCandidates(IEnumerable<UpgradeDefinition> pool, UpgradeCharacter c, UpgradeSlots slots,
            ArtifactDefinition artifact = null)
        {
            var r = new List<UpgradeDefinition>();
            if (pool == null || (slots != null && slots.Full))
                return r;
            foreach (UpgradeDefinition u in pool)
                if (VisibleBy(u, c, artifact) && (slots == null || !slots.Has(u)) && !r.Contains(u))
                    r.Add(u);
            return r;
        }

        /// <summary>Texte d'une offre : « Nom · Bronze » (nouvelle) ou « Nom · Bronze → Argent » (montée).</summary>
        public static string OfferTitle(UpgradeDefinition u, int fromRank, int toRank) => u == null ? "—" : u.OfferTitle(fromRank, toRank);
    }
}
