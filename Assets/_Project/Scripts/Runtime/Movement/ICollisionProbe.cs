using System;
using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>Résultat d'un déplacement avec collisions.</summary>
    public struct MoveResult
    {
        public bool HitBelow;
        public bool HitSides;
        public bool HitAbove;

        /// <summary>Déplacement réellement effectué (m).</summary>
        public Vector3 ActualDelta;

        /// <summary>Normale du dernier obstacle touché sur le côté (si HasSideNormal).</summary>
        public Vector3 SideNormal;
        public bool HasSideNormal;
    }

    /// <summary>
    /// Ce dont la simulation a besoin du monde physique. Implémenté par un CharacterController en
    /// jeu, et par un sol factice dans les tests. Permet de changer de méthode de collision plus tard.
    /// </summary>
    public interface ICollisionProbe
    {
        Vector3 Position { get; }

        /// <summary>Hauteur actuelle de la capsule (m), pieds à la position.</summary>
        float Height { get; }

        /// <summary>Change la hauteur de la capsule en gardant les pieds en place.</summary>
        void SetHeight(float height);

        /// <summary>Vrai s'il y a la place de grandir jusqu'à targetHeight (pas de plafond au-dessus).</summary>
        bool HasHeadroom(float targetHeight);

        /// <summary>
        /// Cherche un mur quasi vertical au contact, dans l'une des directions horizontales données
        /// (les directions nulles sont ignorées). normal : normale horizontale du mur, vers le joueur ;
        /// wallId : identifiant stable du mur (pour interdire deux wall jumps de suite sur le même).
        /// </summary>
        bool ProbeWall(ReadOnlySpan<Vector3> directions, out Vector3 normal, out int wallId);

        /// <summary>Vrai si un sol marchable est juste sous les pieds ; normal = normale de ce sol.</summary>
        bool ProbeGround(out Vector3 normal);

        /// <summary>
        /// Rebord à escalader (hissage, D-074) devant soi dans la direction horizontale dir : un mur au contact dont le dessus,
        /// marchable, est entre minRise et maxRise au-dessus des pieds, profond d'au moins minDepth, avec la place de se tenir debout
        /// dessus et de monter le long du mur. standPoint : pieds sur le dessus, un rayon de capsule après le bord.
        /// </summary>
        bool ProbeLedge(Vector3 dir, float minRise, float maxRise, float minDepth, out Vector3 standPoint);

        /// <summary>Déplace la capsule. snapToGround : la recoller au sol en descente (pentes, marches).</summary>
        MoveResult Move(Vector3 delta, bool snapToGround);
    }
}
