using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Portes en haut ou en bas d'un escalier (nuit du 27/11, playtest : le traqueur restait bloqué par l'encadrement d'une porte à l'acte I,
    /// juste après un escalier). Une porte est « d'escalier » quand, d'un côté, le sol du NavMesh du traqueur monte ou descend de plus de
    /// 1.2 m dans les 14 m devant elle (rampe, escalier lisse d'une terrasse ou d'un palier). Pour chacune : un point au pied (ou en haut)
    /// de l'escalier et un point de l'autre côté, sur le NavMesh du traqueur (StairDoorTests le fait marcher de l'un à l'autre).
    /// </summary>
    public sealed partial class ActLevel
    {
        public struct StairDoor
        {
            public int Door;
            public string Rooms;
            /// <summary>Point sur l'escalier (côté qui monte ou descend), à environ 16 m de la porte.</summary>
            public Vector3 StairSide;
            /// <summary>Point de l'autre côté de la porte.</summary>
            public Vector3 OtherSide;
            public Vector3 Center;
            /// <summary>Dénivelé mesuré du côté de l'escalier (m).</summary>
            public float Drop;
            public override string ToString() => $"porte {Door} ({Rooms}) au ({Center.x:0}, {Center.y:0}, {Center.z:0}), dénivelé {Drop:0.0} m";
        }

        /// <summary>Portes d'escalier franchissables par le traqueur (portes payées exclues : il n'entre pas dans les salles fermées).</summary>
        public List<StairDoor> StairDoors(int stalkerAgent)
        {
            var r = new List<StairDoor>();
            if (Plan == null)
                return r;
            var filter = new NavMeshQueryFilter { agentTypeID = stalkerAgent, areaMask = NavMesh.AllAreas };
            for (int di = 0; di < Plan.Doors.Count; di++)
            {
                ActDoor d = Plan.Doors[di];
                if (d.Locked)
                    continue;
                Vector3 c = Plan.DoorCenter(d);
                Vector3 n = Plan.DoorNormal(d);
                n.y = 0f;
                n.Normalize();
                float sill = Mathf.Max(Plan.Rooms[d.A].Floor, Plan.Rooms[d.B].Floor);
                c.y = sill;
                foreach (float side in new[] { 1f, -1f })
                {
                    float lo = float.PositiveInfinity, hi = float.NegativeInfinity;
                    int found = 0;
                    for (float s = 2f; s <= 14f; s += 1f)
                        if (Point(c + n * (side * s), filter, out Vector3 g) && Flat(g - (c + n * (side * s))) < 1.6f)
                        {
                            lo = Mathf.Min(lo, g.y);
                            hi = Mathf.Max(hi, g.y);
                            found++;
                        }

                    if (found < 4 || hi - lo < 1.2f)
                        continue;
                    if (!Point(c + n * (side * 16f), filter, out Vector3 a) && !Point(c + n * (side * 12f), filter, out a))
                        continue;
                    if (!Point(c - n * (side * 12f), filter, out Vector3 b) && !Point(c - n * (side * 8f), filter, out b))
                        continue;
                    r.Add(new StairDoor
                    {
                        Door = di, Rooms = $"{Plan.Rooms[d.A].Name} / {Plan.Rooms[d.B].Name}", StairSide = a, OtherSide = b, Center = c, Drop = hi - lo,
                    });
                    break;
                }
            }

            return r;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        private static bool Point(Vector3 at, NavMeshQueryFilter filter, out Vector3 p)
        {
            p = at;
            // Sol sous le point (escalier qui descend : jusqu'à 20 m plus bas), puis le NavMesh du traqueur à cet endroit.
            Vector3 probe = at + Vector3.up * 3f;
            if (Physics.Raycast(probe, Vector3.down, out RaycastHit ground, 25f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                probe = ground.point;
            if (!NavMesh.SamplePosition(probe, out NavMeshHit h, 1.5f, filter))
                return false;
            p = h.position;
            return true;
        }
    }
}
