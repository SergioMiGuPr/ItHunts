using System;
using System.Collections.Generic;
using ItHunts.Economy;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// État de la run (objet de la scène, pas du joueur) : seed, personnages et artefacts, âmes et améliorations. Chaque joueur
    /// demande son personnage et son artefact par son index (PlayerInput.playerIndex) : le personnage choisi dans le menu
    /// (RunSession.Characters), jamais deux fois le même (D-064). Sans liste de personnages (anciennes scènes), tirage par la
    /// seed dans le pool (D-032, ArtifactDraw). Améliorations à rangs (nuit 4, D-100) : 10 emplacements par joueur, 1 passif, un
    /// rang par amélioration possédée (UpgradeSlots) ; les 30 génériques pour tous, les 10 du personnage pour son porteur.
    /// </summary>
    public sealed class RunState : MonoBehaviour
    {
        [Tooltip("Personnages (D-064) : chaque joueur reçoit l'artefact de son personnage.")]
        [SerializeField] private CharacterRoster characters;
        [Tooltip("Ancien tirage (D-032), seulement sans liste de personnages.")]
        [SerializeField] private ArtifactPool pool;
        [Tooltip("Améliorations génériques (30, pour tous) que les coffres et le marchand proposent (D-043, D-100). Celles d'un personnage " +
                 "viennent de CharacterDefinition.upgrades (sinon de ArtifactDefinition.upgrades).")]
        [SerializeField] private UpgradePool upgrades;
        [Tooltip("Seed tirée au hasard au lancement ; décoché : la seed ci-dessous (rejouer une run).")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 1;

        public int Seed { get; private set; }
        public ArtifactPool Pool => pool;
        public UpgradePool Upgrades => upgrades;

        // ---------- Âmes et améliorations, par joueur (D-007, D-043, D-100) ----------

        private readonly Dictionary<int, int> _souls = new Dictionary<int, int>();
        private readonly Dictionary<int, UpgradeSlots> _slots = new Dictionary<int, UpgradeSlots>();
        private readonly Dictionary<(int, PriceKind), int> _purchases = new Dictionary<(int, PriceKind), int>();
        private readonly HashSet<(int, int)> _freeRankUps = new HashSet<(int, int)>();
        /// <summary>Coffres ouverts d'affilée sans prendre d'amélioration du personnage, par joueur (pitié, point 3).</summary>
        private readonly Dictionary<int, int> _dryChests = new Dictionary<int, int>();
        private int _draws;

        /// <summary>Âmes d'un joueur changées (identifiant, total, différence).</summary>
        public static event Action<int, int, int> SoulsChanged;
        /// <summary>Achat refusé, pas assez d'âmes (identifiant) : HUD en rouge et son de refus.</summary>
        public static event Action<int> Refused;
        /// <summary>Améliorations d'un joueur changées (identifiant) : HUD, écran de fin.</summary>
        public static event Action<int> UpgradesChanged;

        /// <summary>Refus sans achat (porte encore verrouillée) : même retour qu'un achat refusé (HUD en rouge, son de refus).</summary>
        public static void RaiseRefused(int playerId) => Refused?.Invoke(playerId);

        private static RunState _current;
        public static RunState Current => _current != null ? _current : _current = FindFirstObjectByType<RunState>();

        public int Souls(int playerId) => _souls.TryGetValue(playerId, out int n) ? n : 0;

        public void AddSouls(int playerId, int amount)
        {
            if (playerId == 0 || amount == 0)
                return;
            int n = Mathf.Max(0, Souls(playerId) + amount);
            _souls[playerId] = n;
            SoulsChanged?.Invoke(playerId, n, amount);
        }

        /// <summary>Paie price âmes ; sinon refus (événement Refused), rien n'est retiré.</summary>
        public bool TrySpend(int playerId, int price)
        {
            if (!SoulMath.CanAfford(Souls(playerId), price))
            {
                Refused?.Invoke(playerId);
                return false;
            }

            if (price > 0)
                AddSouls(playerId, -price);
            return true;
        }

        /// <summary>Achats payants de ce joueur dans cette catégorie (hausse du prix à chaque achat).</summary>
        public int Purchases(int playerId, PriceKind kind) => _purchases.TryGetValue((playerId, kind), out int n) ? n : 0;

        /// <summary>Prix actuel pour ce joueur : prix de base monté de increasePercent % par achat déjà fait dans la catégorie, plafonné (cap × base ; 0 : sans).</summary>
        public int PriceFor(int playerId, PriceKind kind, int basePrice, float increasePercent, float cap = 0f) =>
            SoulMath.RaisedPrice(basePrice, increasePercent, Purchases(playerId, kind), cap);

        /// <summary>Achat au prix actuel (refus sinon) ; un achat payant compte pour la hausse suivante.</summary>
        public bool TryBuy(int playerId, PriceKind kind, int basePrice, float increasePercent, float cap = 0f)
        {
            int price = PriceFor(playerId, kind, basePrice, increasePercent, cap);
            if (!TrySpend(playerId, price))
                return false;
            if (price > 0)
                _purchases[(playerId, kind)] = Purchases(playerId, kind) + 1;
            return true;
        }

        /// <summary>Marchandage Mythique : la première montée de rang de chaque acte est gratuite ; vrai si elle vient d'être utilisée.</summary>
        public bool TryUseFreeRankUp(int playerId, int act) => _freeRankUps.Add((playerId, act));
        public bool FreeRankUpUsed(int playerId, int act) => _freeRankUps.Contains((playerId, act));

        // ---------- Tirages ----------

        /// <summary>Tirage de count améliorations différentes (coffre, marchand) : seed de la run + numéro de tirage.</summary>
        public UpgradeDefinition[] DrawUpgrades(int count) => DrawUpgrades(count, upgrades);

        /// <summary>Tirage pour ce joueur : sans les améliorations réservées à un autre personnage que le sien.</summary>
        public UpgradeDefinition[] DrawUpgradesFor(int count, UpgradePool pool, ItHunts.Player.PlayerHealth player)
        {
            ArtifactDefinition art = null;
            if (player != null && player.TryGetComponent(out ArtifactHolder h))
                art = h.Current;
            return DrawUpgrades(count, pool, art);
        }

        /// <summary>Tirage dans une table donnée (coffre rare : sa table dédiée, D-071), même suite aléatoire que la run.</summary>
        public UpgradeDefinition[] DrawUpgrades(int count, UpgradePool pool, ArtifactDefinition forArtifact = null)
        {
            if (pool == null || pool.upgrades.Count == 0)
                pool = upgrades;
            if (pool == null || pool.upgrades.Count == 0)
                return new UpgradeDefinition[0];
            UpgradeCharacter c = UpgradeRules.CharacterOf(forArtifact);
            var usable = new List<UpgradeDefinition>();
            foreach (UpgradeDefinition u in pool.upgrades)
                if (u != null && (forArtifact == null || UpgradeRules.VisibleBy(u, c, forArtifact)))
                    usable.Add(u);
            return Pick(usable, count);
        }

        private UpgradeDefinition[] Pick(List<UpgradeDefinition> usable, int count)
        {
            if (usable == null || usable.Count == 0 || count <= 0)
                return new UpgradeDefinition[0];
            int[] idx = SoulMath.Draw(Seed, _draws++, usable.Count, Mathf.Min(count, usable.Count));
            var r = new UpgradeDefinition[idx.Length];
            for (int i = 0; i < idx.Length; i++)
                r[i] = usable[idx[i]];
            return r;
        }

        /// <summary>Personnage (enum des améliorations) d'un joueur : celui de sa fiche, sinon déduit de son artefact.</summary>
        public static UpgradeCharacter CharacterOfPlayer(ItHunts.Player.PlayerHealth p)
        {
            var holder = p != null ? p.GetComponent<ArtifactHolder>() : null;
            if (holder == null)
                return UpgradeCharacter.Aucun;
            holder.EnsureReady();
            UpgradeCharacter c = UpgradeRules.CharacterOf(holder.Character);
            return c != UpgradeCharacter.Aucun ? c : UpgradeRules.CharacterOf(holder.Current);
        }

        /// <summary>Toutes les améliorations qu'un joueur peut voir : les génériques et les 10 de son personnage.</summary>
        public List<UpgradeDefinition> VisibleUpgrades(ItHunts.Player.PlayerHealth p)
        {
            var r = new List<UpgradeDefinition>();
            UpgradeCharacter c = CharacterOfPlayer(p);
            var holder = p != null ? p.GetComponent<ArtifactHolder>() : null;
            ArtifactDefinition art = holder != null ? holder.Current : null;
            void Add(UpgradePool src)
            {
                if (src == null)
                    return;
                foreach (UpgradeDefinition u in src.upgrades)
                    if (u != null && !r.Contains(u) && UpgradeRules.VisibleBy(u, c, art))
                        r.Add(u);
            }

            Add(upgrades);
            if (holder != null && holder.Character != null)
                Add(holder.Character.upgrades);
            if (art != null)
                Add(art.upgrades);
            return r;
        }

        /// <summary>Tirage de count améliorations nouvelles pour ce joueur (pas possédées ; aucune si ses emplacements sont pleins).</summary>
        public UpgradeDefinition[] DrawNewFor(ItHunts.Player.PlayerHealth p, int count)
        {
            if (p == null)
                return new UpgradeDefinition[0];
            UpgradeSlots slots = SlotsOf(p.PlayerId);
            var candidates = UpgradeRules.NewCandidates(VisibleUpgrades(p), CharacterOfPlayer(p), slots);
            return Pick(candidates, count);
        }

        /// <summary>Tirage de count montées de rang parmi les améliorations possédées non Mythiques de ce joueur.</summary>
        public UpgradeDefinition[] DrawRankUpFor(int playerId, int count) => Pick(SlotsOf(playerId).RankUpCandidates(), count);

        /// <summary>Comme ci-dessus, sans celles de exclude (cartes déjà posées chez le marchand).</summary>
        public UpgradeDefinition[] DrawRankUpFor(int playerId, int count, ICollection<UpgradeDefinition> exclude)
        {
            List<UpgradeDefinition> candidates = SlotsOf(playerId).RankUpCandidates();
            if (exclude != null)
                candidates.RemoveAll(u => exclude.Contains(u));
            return Pick(candidates, count);
        }

        /// <summary>Tirage à pile ou face (chance de 0 à 1) dans la suite aléatoire de la run (seed + numéro de tirage).</summary>
        public bool Roll(float chance)
        {
            if (chance <= 0f)
                return false;
            if (chance >= 1f)
                return true;
            var rng = new System.Random(unchecked(Seed * 486187739 + _draws++ * 16777619 + 0x2C4A));
            return rng.NextDouble() < chance;
        }

        // ---------- Coffres : choix, garantie des améliorations du personnage (point 3) ----------

        /// <summary>Coffres ouverts d'affilée par ce joueur sans prendre d'amélioration de son personnage.</summary>
        public int DryChests(int playerId) => _dryChests.TryGetValue(playerId, out int n) ? n : 0;

        /// <summary>Un coffre vient d'être ouvert par ce joueur (compteur de pitié).</summary>
        public void NoteChestOpened(int playerId) => _dryChests[playerId] = DryChests(playerId) + 1;

        /// <summary>Améliorations du personnage possédées par ce joueur (passifs compris).</summary>
        public int CharacterUpgradesOwned(int playerId)
        {
            int n = 0;
            if (_slots.TryGetValue(playerId, out UpgradeSlots s))
                foreach (UpgradeDefinition u in s.Owned)
                    if (u != null && !u.Generic)
                        n++;
            return n;
        }

        /// <summary>
        /// Garantie (pur) : moins de guarantee améliorations du personnage, et soit pityChests coffres d'affilée sans en prendre une,
        /// soit plus assez d'emplacements libres pour atteindre la garantie autrement. Le prochain coffre n'offre alors que les siennes.
        /// </summary>
        public bool CharacterPityDue(int playerId, int guarantee, int pityChests)
        {
            int missing = guarantee - CharacterUpgradesOwned(playerId);
            if (missing <= 0)
                return false;
            UpgradeSlots s = SlotsOf(playerId);
            if (s.Full)
                return false;
            int free = UpgradeSlots.MaxSlots - s.Count;
            return DryChests(playerId) >= Mathf.Max(1, pityChests) || free <= missing;
        }

        public bool CharacterPityDue(int playerId, EconomySettings settings) =>
            settings != null && CharacterPityDue(playerId, settings.characterUpgradeGuarantee, settings.characterPityChests);

        /// <summary>
        /// Choix d'un coffre (point 3, pur) : count offres différentes ; chacune a characterChance d'être une amélioration du personnage
        /// (sinon une commune) ; forceCharacter (garantie) : seulement celles du personnage, sans second passif ; rankUp : la dernière est
        /// une montée de rang s'il y a de quoi monter ; emplacements pleins : rien que des montées. startRank : la première nouvelle
        /// arrive à ce rang (coffre rare). Renvoie (amélioration, rang de départ ou -1, rang visé).
        /// </summary>
        public List<(UpgradeDefinition u, int from, int to)> DrawChest(int playerId, IEnumerable<UpgradeDefinition> visible, UpgradeCharacter character,
            int count, float characterChance, bool forceCharacter, bool rankUp, int startRank = 0)
        {
            var r = new List<(UpgradeDefinition u, int from, int to)>();
            if (count <= 0)
                return r;
            UpgradeSlots slots = SlotsOf(playerId);
            var mine = new List<UpgradeDefinition>();
            var common = new List<UpgradeDefinition>();
            foreach (UpgradeDefinition u in UpgradeRules.NewCandidates(visible, character, slots))
            {
                if (u.Generic)
                    common.Add(u);
                else
                    mine.Add(u);
            }

            if (forceCharacter && slots.Passive != null)
            {
                // Garantie : un passif en remplacerait un autre sans rien ajouter ; on le garde si c'est tout ce qui reste.
                List<UpgradeDefinition> active = mine.FindAll(u => !u.isPassive);
                if (active.Count > 0)
                    mine = active;
            }

            // Garantie : toutes les cartes pour le personnage (une montée de rang offrirait une échappatoire).
            int ups = rankUp && !forceCharacter && slots.RankUpCandidates().Count > 0 ? 1 : 0;
            int newCount = Mathf.Max(0, count - ups);
            int nMine = 0;
            for (int i = 0; i < newCount; i++)
                if (forceCharacter || Roll(characterChance))
                    nMine++;
            nMine = Mathf.Min(nMine, mine.Count);
            int nCommon = Mathf.Min(newCount - nMine, common.Count);
            if (nMine + nCommon < newCount)
                nMine = Mathf.Min(mine.Count, newCount - nCommon);           // plus assez de communes : celles du personnage
            var picked = new List<UpgradeDefinition>(Pick(mine, nMine));
            picked.AddRange(Pick(common, nCommon));
            if (picked.Count > 1)
            {
                int[] order = SoulMath.Draw(Seed, _draws++, picked.Count, picked.Count);   // l'ordre des cartes ne dit rien du tirage
                var shuffled = new List<UpgradeDefinition>(picked.Count);
                foreach (int i in order)
                    shuffled.Add(picked[i]);
                picked = shuffled;
            }

            for (int i = 0; i < picked.Count; i++)
                r.Add((picked[i], -1, i == 0 ? Mathf.Clamp(startRank, 0, UpgradeDefinition.MaxRank) : 0));
            int missingOffers = count - r.Count;
            if (missingOffers > 0)
                foreach (UpgradeDefinition u in Pick(slots.RankUpCandidates(), missingOffers))
                {
                    int rank = slots.RankOf(u);
                    r.Add((u, rank, rank + 1));
                }

            return r;
        }

        // ---------- Possédées et rangs ----------

        private UpgradeSlots SlotsOf(int playerId)
        {
            if (!_slots.TryGetValue(playerId, out UpgradeSlots s))
                _slots[playerId] = s = new UpgradeSlots();
            return s;
        }

        /// <summary>Emplacements d'un joueur (lecture : rangs, passif, possédées).</summary>
        public UpgradeSlots Slots(int playerId) => SlotsOf(playerId);

        public IReadOnlyList<UpgradeDefinition> UpgradesOf(int playerId) =>
            _slots.TryGetValue(playerId, out UpgradeSlots s) ? s.Owned : (IReadOnlyList<UpgradeDefinition>)Array.Empty<UpgradeDefinition>();

        /// <summary>Rang (0 Bronze … 4 Mythique) d'une amélioration possédée ; −1 sinon.</summary>
        public int RankOf(int playerId, UpgradeDefinition u) => _slots.TryGetValue(playerId, out UpgradeSlots s) ? s.RankOf(u) : -1;

        /// <summary>Prend une amélioration (nouvelle en Bronze, ou montée d'un rang si possédée ; règles de UpgradeSlots).</summary>
        public UpgradeGrant Grant(int playerId, UpgradeDefinition upgrade) => Grant(playerId, upgrade, 0);

        public UpgradeGrant Grant(int playerId, UpgradeDefinition upgrade, int startRank)
        {
            UpgradeGrant g = SlotsOf(playerId).Grant(upgrade, startRank);
            if (g.Ok)
            {
                if (!upgrade.Generic && g.Kind != UpgradeGrantKind.Montee)
                    _dryChests[playerId] = 0;                                    // pitié : une amélioration du personnage prise
                NoteStats(upgrade, g.Rank);
                UpgradesChanged?.Invoke(playerId);
            }

            return g;
        }

        /// <summary>Monte d'un rang une amélioration possédée ; faux si elle ne l'est pas ou si elle est Mythique.</summary>
        public bool RankUp(int playerId, UpgradeDefinition upgrade)
        {
            if (RankOf(playerId, upgrade) < 0)
                return false;
            return Grant(playerId, upgrade).Ok;
        }

        private static void NoteStats(UpgradeDefinition u, int rank)
        {
            if (u == null)
                return;
            var list = ItHunts.Level.RunStats.Upgrades;
            string prefix = u.displayName + " (";
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i].StartsWith(prefix, StringComparison.Ordinal))
                    list.RemoveAt(i);
            list.Add($"{u.displayName} ({UpgradeDefinition.RankName(rank)})");
        }

        private bool _started;

        private void Awake() => EnsureStarted();

        /// <summary>
        /// La run a sa seed (appelé aussi par le générateur de la carte aléatoire, qui s'exécute avant tout le monde : la carte et
        /// la run partagent la même seed). Une seule fois.
        /// </summary>
        public void EnsureStarted()
        {
            if (_started)
                return;
            _started = true;
            RunCarry carry = ItHunts.Level.RunSession.Active ? ItHunts.Level.RunSession.Carry : null;
            if (carry != null)
            {
                Restore(carry);                                                   // acte suivant : même run, même stuff
                return;
            }

            NewRun(randomSeed ? (int?)null : seed);
            if (ItHunts.Level.RunSession.Active)
                ItHunts.Level.RunSession.Carry = Snapshot();                      // la seed de la run reste celle du premier acte
        }

        /// <summary>Stuff de la run à cet instant (passage d'un acte à l'autre).</summary>
        public RunCarry Snapshot()
        {
            var c = new RunCarry { Seed = Seed, Draws = _draws };
            // L'identifiant d'un joueur est celui de son objet : il change à chaque scène. Le stuff suit l'emplacement (joueur 1, 2…).
            foreach (ItHunts.Player.PlayerHealth p in ItHunts.Player.PlayerHealth.All)
                if (p != null)
                    c.Slots[p.PlayerId] = SlotOf(p);
            foreach (KeyValuePair<int, int> kv in _souls)
                c.Souls[kv.Key] = kv.Value;
            foreach (KeyValuePair<int, UpgradeSlots> kv in _slots)
            {
                c.Owned[kv.Key] = new List<UpgradeDefinition>(kv.Value.Owned);
                c.Ranks[kv.Key] = kv.Value.RanksCopy();
            }

            foreach (KeyValuePair<(int, PriceKind), int> kv in _purchases)
                c.Purchases[kv.Key] = kv.Value;
            foreach (KeyValuePair<int, int> kv in _dryChests)
                c.DryChests[kv.Key] = kv.Value;
            return c;
        }

        public void Restore(RunCarry c)
        {
            Seed = c.Seed;
            _draws = c.Draws;
            _souls.Clear();
            _slots.Clear();
            _purchases.Clear();
            _freeRankUps.Clear();
            _dryChests.Clear();
            foreach (KeyValuePair<int, int> kv in c.DryChests)
                _dryChests[kv.Key] = kv.Value;
            foreach (KeyValuePair<int, int> kv in c.Souls)
                _souls[kv.Key] = kv.Value;
            foreach (KeyValuePair<int, List<UpgradeDefinition>> kv in c.Owned)
            {
                var s = new UpgradeSlots();
                s.Load(kv.Value, c.Ranks.TryGetValue(kv.Key, out Dictionary<UpgradeDefinition, int> ranks) ? ranks : null);
                _slots[kv.Key] = s;
            }

            foreach (KeyValuePair<(int, PriceKind), int> kv in c.Purchases)
                _purchases[kv.Key] = kv.Value;
            _carrySlots = c.Slots.Count > 0 ? new Dictionary<int, int>(c.Slots) : null;
            RemapCarried();
            Debug.Log($"Run : seed {Seed}, stuff repris ({_souls.Count} joueurs)");
            _reapply = true;
        }

        private Dictionary<int, int> _carrySlots;

        /// <summary>Emplacement d'un joueur : index de son PlayerInput (écran partagé), sinon son rang dans la liste des joueurs.</summary>
        public static int SlotOf(ItHunts.Player.PlayerHealth p)
        {
            var input = p.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (input != null && input.playerIndex >= 0)
                return input.playerIndex;
            for (int i = 0; i < ItHunts.Player.PlayerHealth.All.Count; i++)
                if (ItHunts.Player.PlayerHealth.All[i] == p)
                    return i;
            return 0;
        }

        /// <summary>Rattache le stuff repris (rangé sous les identifiants de l'acte précédent) aux joueurs de cet acte, par emplacement.</summary>
        private void RemapCarried()
        {
            if (_carrySlots == null || ItHunts.Player.PlayerHealth.All.Count == 0)
                return;
            var newId = new Dictionary<int, int>();
            foreach (ItHunts.Player.PlayerHealth p in ItHunts.Player.PlayerHealth.All)
                if (p != null)
                    newId[SlotOf(p)] = p.PlayerId;
            RunCarry.Remap(_carrySlots, newId, _souls, _slots, _purchases);
            // Compteur de pitié : mêmes renommages (ancien identifiant -> emplacement -> nouvel identifiant).
            var dry = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> kv in _dryChests)
                dry[_carrySlots.TryGetValue(kv.Key, out int slot) && newId.TryGetValue(slot, out int id) ? id : kv.Key] = kv.Value;
            _dryChests.Clear();
            foreach (KeyValuePair<int, int> kv in dry)
                _dryChests[kv.Key] = kv.Value;
            _carrySlots = null;
        }

        private bool _reapply;

        /// <summary>Acte suivant : les effets des améliorations reprises (avec leurs rangs) sont réappliqués aux nouveaux joueurs dès qu'ils existent.</summary>
        private void Update()
        {
            if (!_reapply || ItHunts.Player.PlayerHealth.All.Count == 0)
                return;
            RemapCarried();
            _reapply = false;
            foreach (ItHunts.Player.PlayerHealth p in ItHunts.Player.PlayerHealth.All)
            {
                if (p == null || !_slots.TryGetValue(p.PlayerId, out UpgradeSlots s) || s.Count == 0)
                    continue;
                PlayerUpgrades.Of(p).SyncFrom(this);
                SoulsChanged?.Invoke(p.PlayerId, Souls(p.PlayerId), 0);
                UpgradesChanged?.Invoke(p.PlayerId);
            }
        }

        /// <summary>Nouvelle run : seed donnée, ou tirée au hasard.</summary>
        public void NewRun(int? withSeed)
        {
            Seed = withSeed ?? unchecked((int)System.DateTime.Now.Ticks);
            _souls.Clear();
            _slots.Clear();
            _purchases.Clear();
            _freeRankUps.Clear();
            _dryChests.Clear();
            _draws = 0;
            Debug.Log($"Run : seed {Seed}");
        }

        public CharacterRoster Characters => characters;

        /// <summary>Indices des personnages des joueurs 0 à count − 1 (choix du menu, sans doublon ; −1 : aucun libre).</summary>
        public int[] CharacterIndices(int count)
        {
            if (characters == null || characters.Count == 0)
                return new int[count];
            return CharacterPick.Assign(characters.Selectable(), ItHunts.Level.RunSession.Characters, count, characters.defaultIndex);
        }

        /// <summary>Personnage du joueur d'index donné (null sans liste ou si aucun n'est libre).</summary>
        public CharacterDefinition CharacterFor(int playerIndex)
        {
            if (characters == null || characters.Count == 0 || playerIndex < 0)
                return null;
            int[] all = CharacterIndices(playerIndex + 1);
            return characters[all[playerIndex]];
        }

        /// <summary>Artefact du joueur d'index donné : celui de son personnage ; sans liste, tirage dans le pool (null si vide).</summary>
        public ArtifactDefinition ArtifactFor(int playerIndex)
        {
            if (characters != null && characters.Count > 0)
                return CharacterFor(playerIndex)?.artifact;
            if (pool == null || pool.artifacts.Count == 0)
                return null;
            int i = ArtifactDraw.ForPlayer(Seed, pool.artifacts.Count, playerIndex);
            return i >= 0 ? pool.artifacts[i] : null;
        }

        /// <summary>Réglages (tests).</summary>
        public void Configure(ArtifactPool artifacts, int runSeed, UpgradePool upgradePool = null, CharacterRoster roster = null)
        {
            pool = artifacts;
            characters = roster;
            Seed = runSeed;
            if (upgradePool != null)
                upgrades = upgradePool;
            _souls.Clear();
            _slots.Clear();
            _purchases.Clear();
            _freeRankUps.Clear();
            _dryChests.Clear();
            _draws = 0;
        }
    }

    /// <summary>Stuff de la run gardé d'un acte à l'autre (nuit 3, point 3 ; rangs : nuit 4) : âmes, améliorations et rangs, achats, tirages.</summary>
    public sealed class RunCarry
    {
        public int Seed, Draws;
        public readonly Dictionary<int, int> Souls = new Dictionary<int, int>();
        public readonly Dictionary<int, List<UpgradeDefinition>> Owned = new Dictionary<int, List<UpgradeDefinition>>();
        /// <summary>Rang de chaque amélioration possédée, par joueur (une amélioration sans rang : Bronze).</summary>
        public readonly Dictionary<int, Dictionary<UpgradeDefinition, int>> Ranks = new Dictionary<int, Dictionary<UpgradeDefinition, int>>();
        public readonly Dictionary<(int, PriceKind), int> Purchases = new Dictionary<(int, PriceKind), int>();
        /// <summary>Compteur de pitié des améliorations du personnage (coffres d'affilée sans en prendre une), par joueur.</summary>
        public readonly Dictionary<int, int> DryChests = new Dictionary<int, int>();
        /// <summary>Identifiant du joueur à la sortie → son emplacement (0, 1…) : les identifiants changent d'une scène à l'autre.</summary>
        public readonly Dictionary<int, int> Slots = new Dictionary<int, int>();

        /// <summary>
        /// Renomme les clés : ancien identifiant → emplacement → identifiant du joueur de la nouvelle scène. Les entrées sans joueur
        /// correspondant restent sous leur ancien identifiant.
        /// </summary>
        public static void Remap(Dictionary<int, int> slots, Dictionary<int, int> newIdBySlot, Dictionary<int, int> souls,
            Dictionary<int, List<UpgradeDefinition>> owned, Dictionary<(int, PriceKind), int> purchases)
        {
            var slotsByPlayer = new Dictionary<int, UpgradeSlots>();
            foreach (KeyValuePair<int, List<UpgradeDefinition>> kv in owned)
            {
                var s = new UpgradeSlots();
                s.Load(kv.Value, null);
                slotsByPlayer[kv.Key] = s;
            }

            Remap(slots, newIdBySlot, souls, slotsByPlayer, purchases);
            owned.Clear();
            foreach (KeyValuePair<int, UpgradeSlots> kv in slotsByPlayer)
                owned[kv.Key] = new List<UpgradeDefinition>(kv.Value.Owned);
        }

        /// <summary>Même chose avec les emplacements (rangs compris).</summary>
        public static void Remap(Dictionary<int, int> slots, Dictionary<int, int> newIdBySlot, Dictionary<int, int> souls,
            Dictionary<int, UpgradeSlots> owned, Dictionary<(int, PriceKind), int> purchases)
        {
            int To(int old) => slots.TryGetValue(old, out int s) && newIdBySlot.TryGetValue(s, out int id) ? id : old;
            var s2 = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> kv in souls)
                s2[To(kv.Key)] = kv.Value;
            var o2 = new Dictionary<int, UpgradeSlots>();
            foreach (KeyValuePair<int, UpgradeSlots> kv in owned)
                o2[To(kv.Key)] = kv.Value;
            var p2 = new Dictionary<(int, PriceKind), int>();
            foreach (KeyValuePair<(int, PriceKind), int> kv in purchases)
                p2[(To(kv.Key.Item1), kv.Key.Item2)] = kv.Value;
            souls.Clear();
            owned.Clear();
            purchases.Clear();
            foreach (KeyValuePair<int, int> kv in s2)
                souls[kv.Key] = kv.Value;
            foreach (KeyValuePair<int, UpgradeSlots> kv in o2)
                owned[kv.Key] = kv.Value;
            foreach (KeyValuePair<(int, PriceKind), int> kv in p2)
                purchases[kv.Key] = kv.Value;
        }
    }
}
