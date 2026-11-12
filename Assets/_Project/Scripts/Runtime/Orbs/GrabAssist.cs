using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Aide à la visée de la prise (F / X), calculs purs. Un objet attrapable est sélectionné s'il est vu dans un cône
    /// autour du viseur ; l'angle du cône diminue avec la distance (généreux de près, exigeant de loin). Parmi plusieurs
    /// objets dans le cône, le plus proche du viseur (le plus petit angle) l'emporte, pas le plus proche du joueur.
    /// Le côté physique (ligne de vue, points des colliders) est dans PhysicsGrabProbe.
    /// </summary>
    public static class GrabAssist
    {
        /// <summary>Demi-angle du cône (degrés) à cette distance : assistNearAngle jusqu'à assistNearDistance, puis
        /// linéaire jusqu'à assistFarAngle à la portée ; 0 si l'aide est coupée.</summary>
        public static float ConeAngle(TelekinesisOrbSettings cfg, float distance)
        {
            if (!cfg.aimAssist)
                return 0f;
            float t = Mathf.InverseLerp(cfg.assistNearDistance, cfg.range, distance);
            return Mathf.Lerp(cfg.assistNearAngle, cfg.assistFarAngle, t);
        }

        /// <summary>Angle (degrés) entre la visée et un point vu depuis les yeux ; 180 s'il est derrière.</summary>
        public static float AngleTo(Vector3 eye, Vector3 aimDir, Vector3 point)
        {
            Vector3 to = point - eye;
            return to.sqrMagnitude < 1e-8f ? 0f : Vector3.Angle(aimDir, to);
        }

        /// <summary>Le point est-il dans le cône (et dans la portée) ?</summary>
        public static bool InCone(TelekinesisOrbSettings cfg, float angle, float distance) =>
            distance <= cfg.range && angle <= ConeAngle(cfg, distance);

        /// <summary>Écart latéral toléré (m) à cette distance : utile pour régler et pour le debug.</summary>
        public static float LateralTolerance(TelekinesisOrbSettings cfg, float distance) =>
            distance * Mathf.Tan(ConeAngle(cfg, distance) * Mathf.Deg2Rad);
    }
}
