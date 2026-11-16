using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Level
{
    /// <summary>
    /// Contrôle des cachettes (validation des modules, mesures de la carte aléatoire) : tout point où un joueur tient debout doit
    /// être à portée de la main du traqueur. Portée : 15 m depuis l'épaule (D-034), épaule à 7 m (baissé, 9 m de haut) ou 12 m
    /// (debout), au-dessus d'un point de son NavMesh, et rien de solide entre l'épaule et le joueur. Points testés : grille au sol
    /// (tous les étages), sur les surfaces praticables (ni pente raide, ni NotWalkable, ni objet physique) avec 1.8 m de place.
    /// </summary>
    public static class ReachCheck
    {
        public sealed class Result
        {
            public int Points;
            public readonly List<Vector3> Hidden = new List<Vector3>();
        }

        private static readonly RaycastHit[] Hits = new RaycastHit[16];
        /// <summary>Rayon du corps du traqueur (m) : l'épaule est à son bord.</summary>
        public const float BodyRadius = 3.5f;
        /// <summary>Épaule du traqueur (StalkerSettings.shoulderHeight, shoulderSide) : hauteur debout (m) et écart au centre (m).</summary>
        public static float ShoulderHeight = 11.1f, ShoulderSide = 1.8f;

        /// <summary>Points où un joueur tient debout dans la zone (grille de step m, tous les étages).</summary>
        public static List<Vector3> StandPoints(PhysicsScene physics, Bounds area, float step)
        {
            var list = new List<Vector3>();
            for (float x = area.min.x + step * 0.5f; x < area.max.x; x += step)
            for (float z = area.min.z + step * 0.5f; z < area.max.z; z += step)
            {
                float y = area.max.y;
                for (int k = 0; k < 8; k++)
                {
                    if (!physics.Raycast(new Vector3(x, y, z), Vector3.down, out RaycastHit h, y - area.min.y + 2f, ~0, QueryTriggerInteraction.Ignore))
                        break;
                    y = h.point.y - 0.05f;
                    Collider c = h.collider;
                    if (h.normal.y < 0.7f || c.attachedRigidbody != null || c.GetComponentInParent<NotWalkable>() != null
                        || c.GetComponentInParent<PlayerHealth>() != null)
                        continue;
                    Vector3 p = h.point;
                    if (physics.OverlapCapsule(p + Vector3.up * 0.45f, p + Vector3.up * 1.75f, 0.35f, new Collider[1], ~0, QueryTriggerInteraction.Ignore) > 0)
                        continue;
                    list.Add(p);
                }
            }

            return list;
        }

        /// <summary>
        /// Points hors de portée : aucun point du NavMesh du traqueur (filter) à moins de reach + 12 m dont l'épaule (7 ou 12 m) voit
        /// le joueur (centre du corps à 0.9 m) à moins de reach.
        /// </summary>
        public static Result Run(PhysicsScene physics, Bounds area, float step, NavMeshQueryFilter stalker, float reach = 15f)
        {
            var result = new Result();
            List<Vector3> points = StandPoints(physics, area, step);
            result.Points = points.Count;
            foreach (Vector3 p in points)
            {
                // Arène du boss : ses poches sont voulues (on y attire le traqueur, D-049), hors contrôle.
                if (RoomVolume.InKind(p, RoomKind.Arene))
                {
                    result.Points--;
                    continue;
                }

                if (!Reachable(physics, p, stalker, reach) && !InReachRoom(p, reach))
                    result.Hidden.Add(p);
            }

            return result;
        }

        /// <summary>
        /// Pièce fermée (ReachRoom) : le traqueur y passe le bras par une ouverture (règle du jeu : 15 m au plus depuis
        /// l'ouverture la plus proche).
        /// </summary>
        public static bool InReachRoom(Vector3 p, float reach = 15f)
        {
            foreach (ReachRoom r in ReachRoom.All)
            {
                if (r == null || !r.Contains(p + Vector3.up * 0.9f))
                    continue;
                Transform o = r.NearestOpening(p);
                return o != null && Vector3.Distance(o.position, p + Vector3.up * 0.9f) <= reach;
            }

            return false;
        }

        /// <summary>Diagnostic d'un point : candidats du NavMesh du traqueur et premier obstacle vu de l'épaule.</summary>
        public static string Explain(PhysicsScene physics, Vector3 p, NavMeshQueryFilter stalker, Vector3 origin)
        {
            var sb = new System.Text.StringBuilder();
            Vector3 body = p + Vector3.up * 0.9f;
            if (NavMesh.SamplePosition(p, out NavMeshHit near, 27f, stalker))
            {
                Vector3 s = near.position;
                sb.Append($"NavMesh du traqueur le plus proche : {(s - origin).ToString("F1")} ; ");
                Vector3 toward = new Vector3(body.x - s.x, 0f, body.z - s.z);
                Vector3 sh = s + Vector3.up * 12f + toward.normalized * Mathf.Min(BodyRadius, toward.magnitude);
                Vector3 d = body - sh;
                int n = physics.Raycast(sh, d.normalized, Hits, d.magnitude, ~0, QueryTriggerInteraction.Ignore);
                sb.Append($"épaule → joueur {d.magnitude:0.0} m, obstacles : ");
                for (int i = 0; i < n; i++)
                    sb.Append(Hits[i].collider.name).Append(PlayerSight.IsDecor(Hits[i].collider) ? "" : " (ignoré)").Append(", ");
            }
            else
                sb.Append("aucun NavMesh du traqueur à moins de 27 m");
            return sb.ToString();
        }

        public static bool Reachable(PhysicsScene physics, Vector3 p, NavMeshQueryFilter stalker, float reach = 15f)
        {
            Vector3 body = p + Vector3.up * 0.9f;
            // Points candidats du NavMesh du traqueur : le plus proche, puis autour (8 directions, 4 à 14 m).
            var candidates = new List<Vector3>(17);
            if (NavMesh.SamplePosition(p, out NavMeshHit near, reach + 12f, stalker))
                candidates.Add(near.position);
            for (int k = 0; k < 8; k++)
            {
                float a = k * Mathf.PI * 0.25f;
                foreach (float r in new[] { 4f, 9f, 14f })
                {
                    Vector3 q = p + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    if (NavMesh.SamplePosition(q, out NavMeshHit h, 6f + Mathf.Max(0f, p.y - FloorBelow(physics, q)), stalker))
                        candidates.Add(h.position);
                }
            }

            // Main par-dessus (mains levées) : rien au-dessus du joueur jusqu'à la hauteur de l'épaule (ou 3 m au moins).
            float top = 0f;
            foreach (Vector3 s in candidates)
                top = Mathf.Max(top, s.y + 12f);
            bool openAbove = !physics.Raycast(body, Vector3.up, out RaycastHit above, Mathf.Max(3f, top - body.y), ~0, QueryTriggerInteraction.Ignore)
                             || !PlayerSight.IsDecor(above.collider);
            foreach (Vector3 s in candidates)
            foreach (float shoulder in new[] { ShoulderHeight, ShoulderHeight * 0.6f })
            {
                // L'épaule du modèle de la DA : 11.1 m de haut (6.7 m baissé), à 1.8 m du centre, du côté du joueur.
                Vector3 toward = new Vector3(body.x - s.x, 0f, body.z - s.z);
                float hd = toward.magnitude;
                Vector3 sh = s + Vector3.up * shoulder + (hd > 0.01f ? toward / hd * Mathf.Min(ShoulderSide, hd) : Vector3.zero);
                Vector3 d = body - sh;
                float len = d.magnitude;
                if (len > reach)
                    continue;
                if (!Blocked(physics, sh, body))
                    return true;
                if (openAbove && body.y <= sh.y + 1f && new Vector2(d.x, d.z).magnitude <= reach)
                {
                    Vector3 over = new Vector3(body.x, sh.y, body.z);
                    if (!Blocked(physics, sh, over))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Hauteur du sol le plus bas sous ce point (premier sol après tous les étages), ou le point lui-même.</summary>
        private static float FloorBelow(PhysicsScene physics, Vector3 q)
        {
            float y = q.y;
            Vector3 o = q + Vector3.up * 0.1f;
            for (int k = 0; k < 6; k++)
            {
                if (!physics.Raycast(o, Vector3.down, out RaycastHit h, 60f, ~0, QueryTriggerInteraction.Ignore))
                    break;
                y = h.point.y;
                o = h.point - Vector3.up * 0.05f;
            }

            return y;
        }

        private static bool Blocked(PhysicsScene physics, Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f)
                return false;
            int n = physics.Raycast(from, d / len, Hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (PlayerSight.IsDecor(Hits[i].collider))
                    return true;
            return false;
        }

        /// <summary>NavMesh temporaire du traqueur (rayon 3.5 m, baissé 9 m) sur des colliders : validation dans l'éditeur.</summary>
        public static NavMeshDataInstance BuildStalkerNavMesh(Transform root, Bounds bounds, out int agentType, float radius = 3.5f, float height = 9f, float voxel = 0.3f)
        {
            NavMeshBuildSettings s = NavMesh.CreateSettings();
            s.agentRadius = radius;
            s.agentHeight = height;
            s.agentClimb = 0.45f;
            s.agentSlope = 45f;
            s.overrideVoxelSize = true;
            s.voxelSize = voxel;
            agentType = s.agentTypeID;
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(root, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            sources.RemoveAll(src => src.component is Collider c && (c.isTrigger || c.attachedRigidbody != null));
            for (int i = 0; i < sources.Count; i++)
                if (sources[i].component != null && sources[i].component.GetComponentInParent<NotWalkable>() != null)
                {
                    NavMeshBuildSource src = sources[i];
                    src.area = 1;                                             // obstacle, pas un sol (comme ZoneNavMesh)
                    sources[i] = src;
                }
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(s, sources, bounds, Vector3.zero, Quaternion.identity);
            return NavMesh.AddNavMeshData(data);
        }
    }
}
