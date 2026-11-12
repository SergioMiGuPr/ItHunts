using ItHunts.Settings;

namespace ItHunts.Combat
{
    /// <summary>
    /// Règle du tir ami (D-032), pure : un joueur ne se blesse jamais lui-même ; les autres joueurs seulement si le tir
    /// ami est actif. Une source sans joueur (0 : objet du monde, lustre tombé seul) ne blesse aucun joueur pour l'instant.
    /// </summary>
    public static class FriendlyFire
    {
        public static bool Allows(int attackerPlayerId, int victimPlayerId, CoopSettings coop) =>
            attackerPlayerId != 0 && attackerPlayerId != victimPlayerId && coop != null && coop.friendlyFire;

        public static float Scale(float damage, CoopSettings coop) => coop != null ? damage * coop.friendlyFireMultiplier : 0f;
    }
}
