using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Level.Acts;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Contrôle d'un acte (nuit 3, point 4) sur une seed : cachettes (points où un joueur tient debout hors de portée du traqueur),
    /// salles atteignables par le traqueur (blocages) et par les goules, coffres atteignables depuis le départ, trous (sol manquant
    /// hors des fosses voulues, murs ouverts vers l'extérieur), liens du traqueur, goules par salle, temps de génération et du NavMesh.
    /// Outil de mesure (Play Mode, CLI) : jamais en partie.
    /// </summary>
    public static class ActAudit
    {
        public static string Run(int seed)
        {
            ActLevel level = ActLevel.Current;
            ZoneNavMesh nav = Object.FindFirstObjectByType<ZoneNavMesh>();
            if (level == null || nav == null)
                return "pas d'acte chargé";
            var sw = Stopwatch.StartNew();
            level.Generate(seed);
            long gen = sw.ElapsedMilliseconds;
            sw.Restart();
            nav.Build();
            long navMs = sw.ElapsedMilliseconds;
            Physics.SyncTransforms();
            ActPlan p = level.Plan;
            var sb = new StringBuilder();
            sb.Append($"seed {seed} : {p.Rooms.Count} salles, {p.Doors.Count} portes ({p.Doors.Count(d => d.Loop)} boucles), départ–sortie {p.StartExitDepth}, ");
            sb.Append($"génération {gen} ms, NavMesh {navMs} ms, liens du traqueur {nav.StalkerLinkCount}, escalades des goules {nav.ClimbLinkCount}");

            // Salles atteignables (traqueur depuis son repaire ou sa porte d'arrivée ; goules depuis le départ).
            var stalkerFilter = new NavMeshQueryFilter { agentTypeID = nav.StalkerAgent, areaMask = NavMesh.AllAreas };
            var ghoulFilter = new NavMeshQueryFilter { agentTypeID = nav.SmallAgent, areaMask = NavMesh.AllAreas };
            int stalkerRoom = p.Arrival >= 0 ? p.Arrival : p.Lair >= 0 ? p.Lair : p.Start;
            Vector3 stalkerFrom = RoomPoint(p, p.Rooms[stalkerRoom]);
            Vector3 startAt = RoomPoint(p, p.Rooms[p.Start]);
            int stalkerBlocked = 0, ghoulBlocked = 0;
            var blockedNames = new List<string>();
            var path = new NavMeshPath();
            foreach (ActRoom r in p.Rooms)
            {
                if (r.Role == RoomRole.Verrouillee)
                    continue;                                           // fermée tant qu'on ne l'a pas payée
                // Plusieurs points de sol répartis dans la salle : atteinte si l'un d'eux l'est (un point peut tomber sur un îlot local).
                var samples = r.Cells.Where(c => r.Use[c] == CellUse.Sol || r.Use[c] == CellUse.Passage).Where((c, i) => i % Mathf.Max(1, r.Cells.Count / 14) == 0)
                    .Select(c => p.CellCenter(c, r.Floor)).ToList();
                samples.Insert(0, RoomPoint(p, r));
                if (!samples.Any(at => PathOk(stalkerFrom, at, stalkerFilter, path)))
                {
                    stalkerBlocked++;
                    blockedNames.Add(r.Name);
                }

                if (!samples.Any(at => PathOk(startAt, at, ghoulFilter, path)))
                    ghoulBlocked++;
            }

            sb.Append($"\n  traqueur : {stalkerBlocked} salle(s) hors d'atteinte{(blockedNames.Count > 0 ? " (" + string.Join(", ", blockedNames) + ")" : "")} ; goules : {ghoulBlocked}");

            // Cachettes : points où un joueur tient debout, hors de portée de la main du traqueur.
            Bounds area = LevelBounds(p);
            ReachCheck.Result reach = ReachCheck.Run(Physics.defaultPhysicsScene, area, 3f, stalkerFilter);
            // Seuls comptent les points dans une salle, sous son plafond (le dessus des toits n'est pas accessible aux joueurs).
            reach.Hidden.RemoveAll(h =>
            {
                int ri = p.RoomAt(h);
                return ri < 0 || (p.Rooms[ri].Ceiling > 0f && h.y > p.Rooms[ri].Floor + p.Rooms[ri].Ceiling - 1f);
            });
            sb.Append($"\n  cachettes : {reach.Hidden.Count} sur {reach.Points} points");
            if (reach.Hidden.Count > 0)
                sb.Append(" (" + string.Join(" ; ", reach.Hidden.Take(6).Select(h => $"{RoomName(p, h)} {h.y:0.0} m")) + ")");

            // Coffres atteignables (chemin des goules depuis le départ jusqu'au pied du coffre ; terrasses par leurs escaliers).
            int chests = 0, chestsOk = 0;
            foreach (Chest c in Object.FindObjectsByType<Chest>(FindObjectsSortMode.None))
            {
                chests++;
                if (PathOk(startAt, c.transform.position, ghoulFilter, path, 3f))
                    chestsOk++;
            }

            sb.Append($"\n  coffres atteignables : {chestsOk} / {chests}");

            // Trous : sol manquant (hors creux et rampes) ; mur ouvert vers l'extérieur (rayon horizontal à 2 m qui ne touche rien).
            int floorHoles = 0, wallHoles = 0;
            foreach (ActRoom r in p.Rooms)
            {
                foreach (KeyValuePair<Vector2Int, CellUse> kv in r.Use)
                {
                    if (kv.Value == CellUse.Creux || kv.Value == CellUse.Rampe)
                        continue;
                    Vector3 c = p.CellCenter(kv.Key, r.Floor + 12f);
                    if (!Physics.Raycast(c, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore))
                        floorHoles++;
                }

                foreach (Vector2Int cell in r.Cells)
                    foreach (Vector2Int d in ActPlan.Dirs)
                    {
                        if (p.OwnerAt(cell + d) != -1)
                            continue;
                        Vector3 from = p.CellCenter(cell, r.Floor + 2f);
                        if (!Physics.Raycast(from, new Vector3(d.x, 0f, d.y), p.Cell * 2.5f, ~0, QueryTriggerInteraction.Ignore))
                            wallHoles++;
                    }
            }

            sb.Append($"\n  trous : sol {floorHoles}, murs {wallHoles}");

            // Goules par salle (aucune salle vide).
            int empty = 0, residents = 0;
            var counts = new Dictionary<int, int>();
            foreach (Ghoul g in Object.FindObjectsByType<Ghoul>(FindObjectsSortMode.None))
            {
                if (!g.name.StartsWith("Goule habitante"))
                    continue;
                residents++;
                int room = p.RoomAt(g.transform.position);
                if (room >= 0)
                    counts[room] = counts.TryGetValue(room, out int n) ? n + 1 : 1;
            }

            foreach (ActRoom r in p.Rooms)
                if (!r.Special && r.Role != RoomRole.Depart && !counts.ContainsKey(r.Id))
                    empty++;
            sb.Append($"\n  goules habitantes : {residents} ({residents / Mathf.Max(1f, p.Rooms.Count(r => !r.Special && r.Role != RoomRole.Depart)):0.0} par salle), salles vides : {empty}");
            return sb.ToString();
        }

        private static string RoomName(ActPlan p, Vector3 at)
        {
            int r = p.RoomAt(at);
            return r >= 0 ? p.Rooms[r].Name : "hors salle";
        }

        private static Vector3 RoomPoint(ActPlan p, ActRoom r)
        {
            Vector2Int best = r.Cells[0];
            float bd = float.MaxValue;
            foreach (Vector2Int c in r.Cells)
            {
                if (r.Use[c] != CellUse.Sol && r.Use[c] != CellUse.Passage)
                    continue;
                float d = (c - r.CenterCell).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = c;
                }
            }

            return p.CellCenter(best, r.Floor);
        }

        private static bool PathOk(Vector3 from, Vector3 to, NavMeshQueryFilter f, NavMeshPath path, float snap = 4f)
        {
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 8f, f) || !NavMesh.SamplePosition(to, out NavMeshHit b, Mathf.Max(snap, 8f), f))
                return false;
            return NavMesh.CalculatePath(a.position, b.position, f, path) && path.status == NavMeshPathStatus.PathComplete;
        }

        private static Bounds LevelBounds(ActPlan p)
        {
            var b = new Bounds(p.CellCenter(p.Rooms[0].CenterCell, p.Rooms[0].Floor), Vector3.zero);
            foreach (ActRoom r in p.Rooms)
            {
                b.Encapsulate(p.CellCenter(new Vector2Int(r.Box.xMin, r.Box.yMin), r.Floor - 6f));
                b.Encapsulate(p.CellCenter(new Vector2Int(r.Box.xMax, r.Box.yMax), r.Floor + (r.Ceiling > 0f ? r.Ceiling : 30f) - 1f));
            }

            return b;
        }
    }
}
