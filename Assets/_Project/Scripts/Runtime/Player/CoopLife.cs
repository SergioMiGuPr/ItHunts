using System;

namespace ItHunts.Player
{
    /// <summary>État de vie d'un joueur en coop (D-048) : prévu pour la réanimation et le spectateur, fini à l'étape 9.</summary>
    public enum LifeState
    {
        Vivant,
        /// <summary>À terre : un allié peut le réanimer (D-007).</summary>
        ATerre,
        /// <summary>Non réanimé à temps : regarde un allié vivant, revient à la zone suivante (D-007).</summary>
        Spectateur,
    }

    /// <summary>
    /// Événements d'état de la réanimation et du spectateur (D-048), écrits comme des messages pour que l'hôte les décide à
    /// l'étape 11. Rien ne les déclenche encore en solo : la mort termine la run (ZoneState). L'étape 9 (écran partagé) les
    /// branche : mort d'un joueur avec un allié debout → ATerre ; réanimé → Vivant ; délai dépassé → Spectateur.
    /// </summary>
    public static class CoopLife
    {
        /// <summary>(joueur, ancien état, nouvel état).</summary>
        public static event Action<int, LifeState, LifeState> Changed;
        /// <summary>(réanimateur, joueur réanimé).</summary>
        public static event Action<int, int> ReviveRequested;
        /// <summary>(spectateur, joueur regardé).</summary>
        public static event Action<int, int> SpectateRequested;

        public static void RaiseChanged(int playerId, LifeState from, LifeState to) => Changed?.Invoke(playerId, from, to);
        public static void RequestRevive(int reviverId, int targetId) => ReviveRequested?.Invoke(reviverId, targetId);
        public static void RequestSpectate(int spectatorId, int targetId) => SpectateRequested?.Invoke(spectatorId, targetId);
    }
}
