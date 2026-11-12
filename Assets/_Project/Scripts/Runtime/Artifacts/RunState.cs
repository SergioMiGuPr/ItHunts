using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// État de la run (objet de la scène, pas du joueur) : seed et tirage des artefacts (D-032). Le reste de la run (âmes,
    /// améliorations, salle en cours) viendra aux étapes 7 et 8. Chaque joueur demande son artefact par son index
    /// (PlayerInput.playerIndex) ; le tirage ne dépend que de la seed (voir ArtifactDraw).
    /// </summary>
    public sealed class RunState : MonoBehaviour
    {
        [SerializeField] private ArtifactPool pool;
        [Tooltip("Seed tirée au hasard au lancement ; décoché : la seed ci-dessous (rejouer une run).")]
        [SerializeField] private bool randomSeed = true;
        [SerializeField] private int seed = 1;

        public int Seed { get; private set; }
        public ArtifactPool Pool => pool;

        private void Awake() => NewRun(randomSeed ? (int?)null : seed);

        /// <summary>Nouvelle run : seed donnée, ou tirée au hasard.</summary>
        public void NewRun(int? withSeed)
        {
            Seed = withSeed ?? unchecked((int)System.DateTime.Now.Ticks);
            Debug.Log($"Run : seed {Seed}");
        }

        /// <summary>Artefact du joueur d'index donné (null si le pool est vide).</summary>
        public ArtifactDefinition ArtifactFor(int playerIndex)
        {
            if (pool == null || pool.artifacts.Count == 0)
                return null;
            int i = ArtifactDraw.ForPlayer(Seed, pool.artifacts.Count, playerIndex);
            return i >= 0 ? pool.artifacts[i] : null;
        }

        /// <summary>Réglages (tests).</summary>
        public void Configure(ArtifactPool artifacts, int runSeed)
        {
            pool = artifacts;
            Seed = runSeed;
        }
    }
}
