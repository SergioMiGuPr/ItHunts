using System.Collections.Generic;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Tirage des artefacts au début d'une run (D-032), à partir de la seed de la run : même seed, même tirage (relance,
    /// partie en ligne). Le joueur i reçoit l'élément i d'une suite fixée par la seed : la suite enchaîne des tours
    /// complets du pool, chacun mélangé ; tant que le pool suffit, aucun doublon entre les joueurs. Quand il y a plus de
    /// joueurs que d'artefacts, un nouveau tour commence : les doublons n'apparaissent qu'à ce moment, répartis au mieux
    /// (chaque artefact au plus « joueurs / taille du pool, arrondi au-dessus » fois). L'artefact d'un joueur ne dépend pas
    /// du nombre de joueurs (rejoindre en cours de route ne change rien pour les autres).
    /// Générateur maison (SplitMix32) : le même résultat sur toutes les plateformes, contrairement à System.Random.
    /// </summary>
    public static class ArtifactDraw
    {
        /// <summary>Indices dans le pool pour les joueurs 0 à count − 1 ; vide si le pool est vide.</summary>
        public static int[] Sequence(int seed, int poolSize, int count)
        {
            if (poolSize <= 0 || count <= 0)
                return new int[0];
            var result = new List<int>(count);
            uint state = unchecked((uint)seed);
            var round = new int[poolSize];
            while (result.Count < count)
            {
                for (int i = 0; i < poolSize; i++)
                    round[i] = i;
                for (int i = poolSize - 1; i > 0; i--)            // Fisher-Yates
                {
                    int j = (int)(Next(ref state) % (uint)(i + 1));
                    (round[i], round[j]) = (round[j], round[i]);
                }

                for (int i = 0; i < poolSize && result.Count < count; i++)
                    result.Add(round[i]);
            }

            return result.ToArray();
        }

        /// <summary>Indice dans le pool pour un joueur (−1 si le pool est vide).</summary>
        public static int ForPlayer(int seed, int poolSize, int playerIndex)
        {
            if (poolSize <= 0 || playerIndex < 0)
                return -1;
            return Sequence(seed, poolSize, playerIndex + 1)[playerIndex];
        }

        private static uint Next(ref uint state)
        {
            unchecked
            {
                uint z = state += 0x9E3779B9u;
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }
    }
}
