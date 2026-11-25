using System.Collections.Generic;
using System.Linq;
using ItHunts.Enemies;
using ItHunts.Level.Acts;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Le traqueur atteint-il chaque zone accessible au joueur (jour du 2026-11-24, D-110) ? Échantillonne le sol praticable
    /// (NavMesh des goules, taille humaine) tous les step m sur chaque étage de chaque salle (sol, galerie), puis cherche un chemin
    /// complet du traqueur (son NavMesh et ses liens d'escalade et de bond) jusqu'à moins de reach m du point. Les points sans chemin
    /// sont regroupés par salle : ce sont les blocages à corriger. « Atteint » : sa main (portée GrabReach depuis l'épaule) y arrive.
    /// </summary>
    public static class StalkerReachAudit
    {
        public sealed class Result
        {
            public int Samples, Reached;
            public readonly List<(Vector3 point, string room, string why)> Misses = new List<(Vector3, string, string)>();
            public float Share => Samples == 0 ? 1f : Reached / (float)Samples;

            /// <summary>Tous les points manqués, avec la cause.</summary>
            public string All() => string.Join("\n", Misses.Select(m => $"- {m.room} {m.point:F0} : {m.why}"));
            /// <summary>Un point par salle (le premier), pour aller voir.</summary>
            public string Points() => string.Join(" ; ", Misses.GroupBy(m => m.room).Select(g => $"{g.Key} {g.First().point:F0}"));

            public string Summary()
            {
                var byRoom = Misses.GroupBy(m => m.room).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} ({g.Count()}, {g.First().why})");
                return $"{Reached}/{Samples} points atteints ({Share:P1}) ; non atteints : " + (Misses.Count == 0 ? "aucun" : string.Join(", ", byRoom));
            }
        }

        private static readonly Collider[] Overlap = new Collider[16];

        /// <summary>Le joueur (rayon 0,3 m, même accroupi) ne peut pas tenir ici : décor fixe partout à hauteur de torse.</summary>
        private static bool InsideSolid(Vector3 feet)
        {
            int n = Physics.OverlapSphereNonAlloc(feet + Vector3.up * 0.7f, 0.3f, Overlap, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (Overlap[i] is MeshCollider mc && mc.convex && mc.name.StartsWith("Kit_Massif"))
                    return true;
                else if (Overlap[i].name.StartsWith("Massif de l'escalier"))
                    return true;
            return Enclosed(feet);
        }

        /// <summary>Cœur creux d'une pièce (pilier de roche en maillage non convexe) : de tous côtés, la même pièce barre l'accès.</summary>
        private static bool Enclosed(Vector3 feet)
        {
            Vector3 at = feet + Vector3.up * 0.9f;
            Collider same = null;
            for (int i = 0; i < 8; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                Vector3 from = at + d * 8f;
                Collider last = null;
                foreach (RaycastHit h in Physics.RaycastAll(from, -d, 7.7f, ~0, QueryTriggerInteraction.Ignore))
                    if (same == null ? h.collider is MeshCollider : h.collider == same)
                        last = h.collider;
                if (last == null)
                    return false;
                same = last;
            }

            return same != null;
        }

        public static Result Run(ActLevel level, float step = 6f)
        {
            var res = new Result();
            var nav = Object.FindFirstObjectByType<ZoneNavMesh>();
            var stalker = Object.FindFirstObjectByType<Stalker>(FindObjectsInactive.Include);
            if (level == null || level.Plan == null || nav == null || stalker == null)
                return res;
            ActPlan p = level.Plan;
            if (stalker.Settings == null)
            {
                res.Misses.Add((stalker.transform.position, "traqueur", "réglages absents"));
                return res;                                             // 10 : CanReach lèverait une exception
            }

            var small = new NavMeshQueryFilter { agentTypeID = nav.SmallAgent, areaMask = NavMesh.AllAreas };
            var big = new NavMeshQueryFilter { agentTypeID = nav.StalkerAgent, areaMask = NavMesh.AllAreas };
            // Point de départ : la salle de départ (le traqueur peut encore attendre derrière sa porte d'arrivée fermée).
            ActPlan plan = level.Plan;
            Vector3 origin = plan.Start >= 0 ? plan.CellCenter(plan.Rooms[plan.Start].CenterCell, plan.Rooms[plan.Start].Floor) : stalker.transform.position;
            if (!NavMesh.SamplePosition(origin, out NavMeshHit from, 30f, big))
            {
                res.Misses.Add((origin, "départ", "pas de NavMesh du traqueur à 30 m"));
                return res;
            }

            // Portée de la main (D-034) : bras + fente depuis l'épaule ; un point est atteint si sa main y arrive depuis son NavMesh.
            float reach = stalker.Settings != null ? stalker.Settings.GrabReach : 12f;
            float shoulder = stalker.Settings != null ? stalker.Settings.shoulderHeight : 10.5f;
            // Même calcul que Stalker.CanReach (jour 5) : poitrine du joueur (pieds + 1 m), épaule avancée vers lui, portée + edgeLean
            // quand il ne peut pas se placer dessous (bord d'une corniche, d'une fosse).
            // (InReach : ancien calcul, gardé pour comparaison ; la vérification passe par Stalker.ReachFrom.)
            float forward = stalker.Settings != null ? stalker.Settings.shoulderForward : 0.9f;
            float lean = stalker.Settings != null ? stalker.Settings.edgeLean : 0f;
            float radius = stalker.Settings != null ? stalker.Settings.width * 0.5f : 1.25f;
            bool InReach(Vector3 stand, Vector3 feet)
            {
                Vector3 chest = feet + Vector3.up;
                Vector3 fd = feet - stand;
                fd.y = 0f;
                float flat = fd.magnitude;
                fd = flat > 1e-3f ? fd / flat : Vector3.forward;
                Vector3 sh = stand + Vector3.up * shoulder + fd * forward;
                return (chest - sh).magnitude <= reach + (flat > radius + 1f ? lean : 0f);
            }

            var path = new NavMeshPath();
            int k = Mathf.Max(1, Mathf.RoundToInt(step / p.Cell));
            foreach (ActRoom r in p.Rooms)
            {
                if (r.Role == RoomRole.Arrivee || r.Role == RoomRole.HallDeSortie || r.Role == RoomRole.Verrouillee)
                    continue;
                var levels = new List<float> { r.Floor };
                if (r.TerraceHeight > 0.01f)
                    levels.Add(r.Floor + r.TerraceHeight);
                foreach (Vector2Int c in r.Cells)
                {
                    if (c.x % k != 0 || c.y % k != 0)
                        continue;
                    foreach (float y in levels)
                    {
                        Vector3 q = p.CellCenter(c, y + 0.5f);
                        if (!NavMesh.SamplePosition(q, out NavMeshHit h, 1.2f, small) || Mathf.Abs(h.position.y - y) > 1.2f)
                            continue;                                   // pas un sol du joueur ici (creux, mur, autre étage)
                        // Une capsule de joueur ne tient pas ici (intérieur d'un massif plein : le NavMesh y garde un îlot fictif) : pas un sol.
                        if (InsideSolid(h.position))
                            continue;
                        res.Samples++;
                        // D'abord la règle du jeu (Stalker.CanReach : 34 essais autour, ou le bord d'un surplomb, D-116).
                        if (stalker.CanReach(h.position, out Vector3 stand, out _)
                            && NavMesh.SamplePosition(stand, out NavMeshHit sh, 3f, big)
                            && NavMesh.CalculatePath(from.position, sh.position, big, path) && path.status == NavMeshPathStatus.PathComplete)
                        {
                            res.Reached++;
                            continue;
                        }

                        if (!stalker.OverheadClear(h.position))
                        {
                            res.Misses.Add((h.position, r.Name, "sous un surplomb, pas de bord où se tenir"));
                            continue;
                        }

                        // Candidats : le point du NavMesh du traqueur le plus proche, puis tout autour (un fond de fosse isolé ne compte pas).
                        bool any = false, reached = false;
                        string why = null;
                        float whyD = float.PositiveInfinity;
                        for (int a = -1; a < 26 && !reached; a++)
                        {
                            // 13 directions à la hauteur du point, puis les mêmes au sol de la salle (il attrape une galerie d'en bas).
                            int dirIdx = a < 0 ? -1 : a % 13;
                            Vector3 basePos = a < 13 ? h.position : new Vector3(h.position.x, r.Floor, h.position.z);
                            Vector3 probe = dirIdx < 0 || dirIdx == 12 ? basePos : basePos + Quaternion.Euler(0f, dirIdx * 30f, 0f) * Vector3.forward * (dirIdx % 2 == 0 ? 6f : 11f);
                            if (!NavMesh.SamplePosition(probe, out NavMeshHit t, 6f, big))
                                continue;
                            if (!stalker.ReachFrom(t.position, h.position))
                            {
                                float dd = (t.position - h.position).sqrMagnitude;
                                if (dd < whyD)
                                {
                                    whyD = dd;
                                    why = $"depuis {t.position:F0} : " + stalker.ReachWhyNot(t.position, h.position);
                                }

                                continue;
                            }

                            why = $"bout sans chemin {t.position:F0}";
                            any = true;
                            reached = NavMesh.CalculatePath(from.position, t.position, big, path) && path.status == NavMeshPathStatus.PathComplete;
                        }

                        if (reached)
                            res.Reached++;
                        else
                            res.Misses.Add((h.position, r.Name, (any ? "pas de chemin" : "pas de sol du traqueur à portée")
                                + (why != null ? " ; " + why : "")));
                    }
                }
            }

            return res;
        }
    }
}
