using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Type de passage du traqueur : un clip d'animation par type plus tard (point 9 de la nuit du 2026-11-21).</summary>
    public enum StalkerLinkKind : byte
    {
        Bond,
        EscaladeRebord,
        MurGrimpe,
        ChaineGrimpe,
        ChaineSaut,
        PorteEtroite,
    }

    /// <summary>
    /// Passage du traqueur posé par un module (point 9) : de ce point à <see cref="end"/>, d'un type donné. ZoneNavMesh en fait un
    /// lien de NavMesh pour le traqueur seulement (coût = temps de traversée), Stalker le parcourt (arc, montée, ralentissement).
    /// Porte étroite : au moins aussi grande que sa plus petite porte (DA : 4 × 6 m, D-080), un lien la traverse (il se faufile,
    /// ralenti, même si son NavMesh baissé n'y passe pas) ; plus petite, seul son bras passe.
    /// </summary>
    public sealed class StalkerPassage : MonoBehaviour
    {
        public StalkerLinkKind kind;
        public Transform end;
        [Min(1f)] public float width = 6f;
        [Tooltip("Porte étroite : demi-taille de la zone où il ralentit (m).")]
        public Vector3 doorHalfSize = new Vector3(5f, 6f, 3f);

        public static readonly List<StalkerPassage> All = new List<StalkerPassage>();

        /// <summary>Bouts posés par le module ; recalés sur le NavMesh du traqueur à la construction du lien (LinkA, LinkB).</summary>
        public Vector3 A => Linked ? LinkA : transform.position;
        public Vector3 B => Linked ? LinkB : end != null ? end.position : transform.position;
        public Vector3 RawA => transform.position;
        public Vector3 RawB => end != null ? end.position : transform.position;

        /// <summary>Lien du NavMesh créé (bouts recalés au plus près, ZoneNavMesh).</summary>
        [System.NonSerialized] public bool Linked;
        [System.NonSerialized] public Vector3 LinkA, LinkB;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        /// <summary>Dans la zone d'une porte étroite (repère local du passage) ?</summary>
        public bool InDoor(Vector3 p)
        {
            if (kind != StalkerLinkKind.PorteEtroite)
                return false;
            Vector3 l = transform.InverseTransformPoint(p);
            return Mathf.Abs(l.x) <= doorHalfSize.x && l.y >= -1f && l.y <= doorHalfSize.y * 2f && Mathf.Abs(l.z) <= doorHalfSize.z;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = kind switch
            {
                StalkerLinkKind.Bond => new Color(1f, 0.5f, 0.1f),
                StalkerLinkKind.ChaineSaut => new Color(1f, 0.8f, 0.1f),
                StalkerLinkKind.PorteEtroite => new Color(0.4f, 0.8f, 1f),
                _ => new Color(0.5f, 1f, 0.4f),
            };
            Gizmos.DrawLine(A, B);
            Gizmos.DrawWireSphere(A, 0.6f);
            Gizmos.DrawWireSphere(B, 0.6f);
        }
    }

    /// <summary>Traversée en cours d'un passage par le traqueur (données seulement ; réseau : calculée par l'hôte).</summary>
    public struct StalkerTraversal
    {
        public bool Active;
        public StalkerLinkKind Kind;
        public Vector3 From, To;
        public float Start, Telegraph, Duration;
        public float RecoverUntil;
        public bool Caught;
        /// <summary>Clip de la DA joué pendant la traversée (null : mouvement provisoire) et récupération après la réception (s).</summary>
        public string Clip;
        public float ClipRecovery;
    }

    /// <summary>Calculs des passages du traqueur (purs, testables) : permis, durée, trajectoire, coût pour le choix du chemin.</summary>
    public static class StalkerPassageMath
    {
        public static bool IsLeap(StalkerLinkKind k) => k == StalkerLinkKind.Bond || k == StalkerLinkKind.ChaineSaut;

        /// <summary>Porte qu'il franchit en se faufilant : au moins sa plus petite porte (narrowDoorWidth × narrowDoorHeight).</summary>
        public static bool DoorPassable(Vector3 doorHalfSize, StalkerSettings s) =>
            doorHalfSize.x * 2f >= s.narrowDoorWidth - 1e-3f && doorHalfSize.y * 2f >= s.narrowDoorHeight - 1e-3f;

        /// <summary>Bouts du lien d'une porte étroite : de part et d'autre de la zone de la porte (repère du passage).</summary>
        public static void DoorEnds(Transform door, Vector3 doorHalfSize, float radius, out Vector3 a, out Vector3 b)
        {
            Vector3 f = door.forward;
            f.y = 0f;
            f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
            float d = doorHalfSize.z + radius + 1f;
            a = door.position - f * d;
            b = door.position + f * d;
        }

        public static bool IsClimb(StalkerLinkKind k) =>
            k == StalkerLinkKind.EscaladeRebord || k == StalkerLinkKind.MurGrimpe || k == StalkerLinkKind.ChaineGrimpe;

        /// <summary>Distance horizontale et dénivelé permis pour ce type (Stalker_Default).</summary>
        public const float ClimbMaxFlat = 22f;

        public static bool Allowed(StalkerLinkKind k, Vector3 a, Vector3 b, StalkerSettings cfg)
        {
            float flat = new Vector2(b.x - a.x, b.z - a.z).magnitude, rise = Mathf.Abs(b.y - a.y);
            return k switch
            {
                StalkerLinkKind.Bond => flat <= cfg.leapMaxDistance && rise <= cfg.leapMaxRise,
                // Saut depuis une chaîne : il se laisse tomber de la hauteur où il s'accroche (jusqu'à la hauteur de chaîne).
                StalkerLinkKind.ChaineSaut => flat <= cfg.chainJumpMaxDistance && rise <= Mathf.Max(cfg.leapMaxRise + 4f, cfg.chainMaxHeight),
                // Escalades : 16 m à plat au plus (pied du mur et bord d'arrivée recalés sur son NavMesh, 4 m chacun).
                StalkerLinkKind.EscaladeRebord => rise <= cfg.ledgeMaxHeight && flat <= ClimbMaxFlat,
                StalkerLinkKind.MurGrimpe => rise <= cfg.wallMaxHeight && flat <= ClimbMaxFlat,
                StalkerLinkKind.ChaineGrimpe => rise <= cfg.chainMaxHeight && flat <= ClimbMaxFlat,
                _ => true,
            };
        }

        /// <summary>Durée de la traversée (s), sans le télégraphe ni la récupération.</summary>
        public static float Duration(StalkerLinkKind k, Vector3 a, Vector3 b, StalkerSettings cfg)
        {
            float flat = new Vector2(b.x - a.x, b.z - a.z).magnitude, rise = Mathf.Abs(b.y - a.y);
            if (IsLeap(k))
                return cfg.leapDuration * Mathf.Lerp(0.7f, 1.2f, Mathf.InverseLerp(0f, Mathf.Max(1f, cfg.leapMaxDistance), flat));
            if (IsClimb(k))
                return rise / Mathf.Max(0.1f, cfg.climbSpeed) + flat / Mathf.Max(0.1f, cfg.chaseSpeed);
            return flat / Mathf.Max(0.1f, cfg.chaseSpeed * cfg.narrowDoorSlowFactor);
        }

        /// <summary>Temps total d'un passage (télégraphe, traversée, récupération) : ce que compare le choix du chemin.</summary>
        public static float TotalTime(StalkerLinkKind k, Vector3 a, Vector3 b, StalkerSettings cfg)
        {
            // Clips de la DA (C6) : le passage dure le clip (bond : appel, vol, réception ; chaîne : montée en boucle selon la hauteur).
            float clip = ClipTime(k, Mathf.Abs(b.y - a.y), cfg);
            if (clip > 0f)
                return clip;
            return Duration(k, a, b, cfg) + (IsLeap(k) ? cfg.leapTelegraph + cfg.leapRecovery : cfg.climbStartPause);
        }

        /// <summary>Durée d'un passage d'après les clips de la DA (s), 0 sans clip réglé.</summary>
        public static float ClipTime(StalkerLinkKind k, float rise, StalkerSettings cfg) => k switch
        {
            StalkerLinkKind.Bond => cfg.clipBondLength > 0f ? Mathf.Max(cfg.clipBondLength, cfg.clipBondLand + cfg.leapRecovery) : 0f,
            StalkerLinkKind.ChaineSaut => cfg.clipChainJumpLength > 0f ? Mathf.Max(cfg.clipChainJumpLength, cfg.clipChainJumpLand + cfg.leapRecovery) : 0f,
            StalkerLinkKind.EscaladeRebord or StalkerLinkKind.MurGrimpe => cfg.clipClimbLength,
            StalkerLinkKind.ChaineGrimpe => cfg.clipChainClimbLength > 0f ? cfg.clipChainClimbLength + EnemyMath.ChainExtraCycles(rise, cfg) * cfg.clipChainCycle : 0f,
            StalkerLinkKind.PorteEtroite => cfg.clipDoorLength,
            _ => 0f,
        };

        /// <summary>
        /// Coût du lien pour le NavMesh (multiplicateur de longueur) : le temps du passage ramené à la marche en Chasse ; le chemin
        /// le plus court en temps l'emporte (contourner, grimper ou sauter).
        /// </summary>
        public static float CostModifier(StalkerLinkKind k, Vector3 a, Vector3 b, StalkerSettings cfg)
        {
            float length = Mathf.Max(0.5f, Vector3.Distance(a, b));
            return Mathf.Max(1f, TotalTime(k, a, b, cfg) * cfg.chaseSpeed / length);
        }

        /// <summary>Position au cours de la traversée (u de 0 à 1) : arc pour un bond, montée puis avancée pour une escalade.</summary>
        public static Vector3 Position(StalkerLinkKind k, Vector3 a, Vector3 b, float u, StalkerSettings cfg)
        {
            u = Mathf.Clamp01(u);
            if (IsLeap(k))
            {
                float apex = cfg.leapArcHeight + Mathf.Max(0f, b.y - a.y);
                Vector3 line = Vector3.Lerp(a, b, u);
                return line + Vector3.up * (4f * apex * u * (1f - u));
            }

            if (IsClimb(k))
            {
                const float rise = 0.8f;
                if (b.y >= a.y)
                    return u <= rise
                        ? new Vector3(a.x, Mathf.Lerp(a.y, b.y, u / rise), a.z)
                        : Vector3.Lerp(new Vector3(a.x, b.y, a.z), b, (u - rise) / (1f - rise));
                // Descente : avancer au bord puis se laisser descendre.
                return u <= 1f - rise
                    ? Vector3.Lerp(a, new Vector3(b.x, a.y, b.z), u / (1f - rise))
                    : new Vector3(b.x, Mathf.Lerp(a.y, b.y, (u - (1f - rise)) / rise), b.z);
            }

            return Vector3.Lerp(a, b, u);
        }

        /// <summary>
        /// Fin de la traversée : posé à l'arrivée ; après un bond, récupération à quatre pattes (leapRecovery). Vrai si c'était un
        /// bond raté (personne attrapé).
        /// </summary>
        public static bool Land(ref StalkerTraversal t, float now, StalkerSettings cfg)
        {
            t.Active = false;
            if (!IsLeap(t.Kind))
                return false;
            // Réception : la fin du clip si elle est plus longue que la récupération réglée (clip livré par la DA).
            t.RecoverUntil = now + Mathf.Max(cfg.leapRecovery, t.ClipRecovery);
            return !t.Caught;
        }

        /// <summary>
        /// Un bond touche-t-il ce joueur ? (corps du traqueur à cette position : sphère de leapHitRadius au milieu de sa hauteur).
        /// </summary>
        public static bool LeapHits(Vector3 stalkerFeet, Vector3 playerCenter, StalkerSettings cfg)
        {
            Vector3 body = stalkerFeet + Vector3.up * (cfg.height * 0.45f);
            return Vector3.Distance(body, playerCenter) <= cfg.leapHitRadius;
        }

        /// <summary>
        /// Le passage qui commence ici : le coin actuel du chemin près d'un bout, le suivant près de l'autre bout (le lien du NavMesh
        /// a été pris). Renvoie le sens (de → vers).
        /// </summary>
        public static StalkerPassage Find(Vector3 corner, Vector3 nextCorner, IList<StalkerPassage> all, float tolerance, out Vector3 from, out Vector3 to) =>
            Find(corner, nextCorner, all, tolerance, tolerance, out from, out to);

        /// <summary>Comme ci-dessus, avec une tolérance par bout (le départ peut être la position du traqueur, large).</summary>
        public static StalkerPassage Find(Vector3 corner, Vector3 nextCorner, IList<StalkerPassage> all, float fromTolerance, float tolerance,
            out Vector3 from, out Vector3 to)
        {
            from = to = Vector3.zero;
            foreach (StalkerPassage p in all)
            {
                if (p == null || p.kind == StalkerLinkKind.PorteEtroite)
                    continue;
                if (Vector3.Distance(corner, p.A) <= fromTolerance && Vector3.Distance(nextCorner, p.B) <= tolerance)
                {
                    from = p.A;
                    to = p.B;
                    return p;
                }

                if (Vector3.Distance(corner, p.B) <= fromTolerance && Vector3.Distance(nextCorner, p.A) <= tolerance)
                {
                    from = p.B;
                    to = p.A;
                    return p;
                }
            }

            return null;
        }
    }
}
