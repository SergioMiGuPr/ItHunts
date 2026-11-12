using ItHunts.Movement;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Règles du retour visuel du magnétisme, sans dépendance à l'affichage (testables) : quand allumer les anneaux
    /// du viseur, quand faire clignoter le HUD après un refus.
    /// </summary>
    public static class MagnetismFeedback
    {
        /// <summary>Anneau bleu : une traction peut démarrer (surface à portée de l'attraction et une charge).</summary>
        public static bool CanAttract(in MagnetismState m) => m.AimHit && !m.Tracting && m.AttractCharges >= 1f;

        /// <summary>Anneau orange : la répulsion peut s'appliquer (surface à portée de la répulsion, charge prête).</summary>
        public static bool CanRepel(in MagnetismState m) => m.RepelInRange && m.RepelStatus == OrbStatus.Prete;

        /// <summary>
        /// Clignotement rouge du groupe du HUD d'un mode refusé : allumé pendant la première moitié de chaque période,
        /// pendant <paramref name="duration"/> secondes après le refus.
        /// </summary>
        public static bool RefusalBlinkOn(in MagnetismState m, MagnetMode mode, int tick, float dt, float duration, float period)
        {
            if (m.LastRefusalTick == MovementState.NeverTick || m.LastRefusalMode != mode || period <= 0f)
                return false;
            float t = (tick - m.LastRefusalTick) * dt;
            if (t < 0f || t >= duration)
                return false;
            return Mathf.Repeat(t, period) < period * 0.5f;
        }
    }
}
