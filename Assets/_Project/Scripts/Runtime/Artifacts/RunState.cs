using System;
using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// État de la run (objet de la scène, pas du joueur) : seed, personnages et artefacts, âmes et améliorations. Chaque joueur
    /// demande son personnage et son artefact par son index (PlayerInput.playerIndex) : le personnage choisi dans le menu
    /// (RunSession.Characters), jamais deux fois le même (D-064). Sans liste de personnages (anciennes scènes), tirage par la
    /// seed dans le pool (D-032, ArtifactDraw).
    /// </summary>
    public sealed class RunState : MonoBehaviour
    {
        [Tooltip("Personnages (D-064) : chaque joueur reçoit l'artefact de son personnage.")]
        [SerializeField] private CharacterRoster characters;
        [Tooltip("Ancien tirage (D-032), seulement sans liste de personnages.")]
        [SerializeField] private ArtifactPool pool;
        [Tooltip("Améliorations que les coffres et le marchand proposent (étape 7, D-043).")]
        [SerializeField] private UpgradePool upgrades;
        [Tooltip("Seed tirée au hasard au lancement ; décoché : la seed ci-dessous (rejouer une run).")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 1;

        public int Seed { get; private set; }
        public ArtifactPool Pool => pool;
        public UpgradePool Upgrades => upgrades;

        // ---------- Âmes et améliorations, par joueur (D-007, D-043) ----------

        private readonly Dictionary<int, int> _souls = new Dictionary<int, int>();
        private readonly Dictionary<int, List<UpgradeDefinition>> _owned = new Dictionary<int, List<UpgradeDefinition>>();
        private readonly Dictionary<(int, ItHunts.Economy.PriceKind), int> _purchases = new Dictionary<(int, ItHunts.Economy.PriceKind), int>();
        private int _draws;

        /// <summary>Âmes d'un joueur changées (identifiant, total, différence).</summary>
        public static event Action<int, int, int> SoulsChanged;
        /// <summary>Achat refusé, pas assez d'âmes (identifiant) : HUD en rouge et son de refus.</summary>
        public static event Action<int> Refused;

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
            if (!ItHunts.Economy.SoulMath.CanAfford(Souls(playerId), price))
            {
                Refused?.Invoke(playerId);
                return false;
            }

            if (price > 0)
                AddSouls(playerId, -price);
            return true;
        }

        /// <summary>Achats payants de ce joueur dans cette catégorie (hausse du prix à chaque achat).</summary>
        public int Purchases(int playerId, ItHunts.Economy.PriceKind kind) => _purchases.TryGetValue((playerId, kind), out int n) ? n : 0;

        /// <summary>Prix actuel pour ce joueur : prix de base monté de increasePercent % par achat déjà fait dans la catégorie.</summary>
        public int PriceFor(int playerId, ItHunts.Economy.PriceKind kind, int basePrice, float increasePercent) =>
            ItHunts.Economy.SoulMath.RaisedPrice(basePrice, increasePercent, Purchases(playerId, kind));

        /// <summary>Achat au prix actuel (refus sinon) ; un achat payant compte pour la hausse suivante.</summary>
        public bool TryBuy(int playerId, ItHunts.Economy.PriceKind kind, int basePrice, float increasePercent)
        {
            int price = PriceFor(playerId, kind, basePrice, increasePercent);
            if (!TrySpend(playerId, price))
                return false;
            if (price > 0)
                _purchases[(playerId, kind)] = Purchases(playerId, kind) + 1;
            return true;
        }

        /// <summary>Tirage de count améliorations différentes (coffre, marchand) : seed de la run + numéro de tirage.</summary>
        public UpgradeDefinition[] DrawUpgrades(int count) => DrawUpgrades(count, upgrades);

        /// <summary>Tirage dans une table donnée (coffre rare : sa table dédiée, D-071), même suite aléatoire que la run.</summary>
        public UpgradeDefinition[] DrawUpgrades(int count, UpgradePool pool)
        {
            if (pool == null || pool.upgrades.Count == 0)
                pool = upgrades;
            if (pool == null || pool.upgrades.Count == 0)
                return new UpgradeDefinition[0];
            int[] idx = ItHunts.Economy.SoulMath.Draw(Seed, _draws++, pool.upgrades.Count, Mathf.Min(count, pool.upgrades.Count));
            var r = new UpgradeDefinition[idx.Length];
            for (int i = 0; i < idx.Length; i++)
                r[i] = pool.upgrades[idx[i]];
            return r;
        }

        public IReadOnlyList<UpgradeDefinition> UpgradesOf(int playerId) =>
            _owned.TryGetValue(playerId, out List<UpgradeDefinition> l) ? l : (IReadOnlyList<UpgradeDefinition>)Array.Empty<UpgradeDefinition>();

        public void Grant(int playerId, UpgradeDefinition upgrade)
        {
            if (!_owned.TryGetValue(playerId, out List<UpgradeDefinition> l))
                _owned[playerId] = l = new List<UpgradeDefinition>();
            l.Add(upgrade);
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
            NewRun(randomSeed ? (int?)null : seed);
        }

        /// <summary>Nouvelle run : seed donnée, ou tirée au hasard.</summary>
        public void NewRun(int? withSeed)
        {
            Seed = withSeed ?? unchecked((int)System.DateTime.Now.Ticks);
            _souls.Clear();
            _owned.Clear();
            _purchases.Clear();
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
            _owned.Clear();
            _purchases.Clear();
            _draws = 0;
        }
    }
}
