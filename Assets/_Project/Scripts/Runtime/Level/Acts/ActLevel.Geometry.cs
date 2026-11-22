using System.Collections.Generic;
using System.Linq;
using ItHunts.Enemies;
using ItHunts.Orbs;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    public sealed partial class ActLevel
    {
        /// <summary>Épaisseur du sol (m) sous les salles ; au-dessus d'une fosse, les blocs descendent plus bas (on voit le vide).</summary>
        private const float FloorDepth = 4f, VoidDepth = 34f;

        private readonly Dictionary<int, Transform> _roomRoots = new Dictionary<int, Transform>();
        private readonly Dictionary<int, List<ActContour.Segment>> _contours = new Dictionary<int, List<ActContour.Segment>>();

        private float WallTop(ActRoom r) => r.Ceiling > 0f ? r.Floor + r.Ceiling : r.Floor + (r.Special ? 12f : settings.openInnerWall);
        private float Bottom(ActRoom r) => r.Floor - (r.Feature == RoomFeature.Vide ? VoidDepth : FloorDepth);

        private void BuildRoom(ActRoom r)
        {
            Transform root = Group($"Salle : {r.Name}");
            _roomRoots[r.Id] = root;
            BuildFloors(r, root);
            BuildWalls(r, root);
            if (r.Ceiling > 0f)
            {
                var cells = new HashSet<Vector2Int>(r.Cells);
                foreach (RectInt rc in Rects(cells))
                    Solid(root, "Plafond", RectCenter(rc, r.Floor + r.Ceiling + 0.5f), RectSize(rc, 1f), 0f, KitBuild.Kind.Sol, settings.ceilingMaterial, walkable: false);
            }
        }

        // ---------- Sols, creux, terrasses ----------

        private void BuildFloors(ActRoom r, Transform root)
        {
            var floor = new HashSet<Vector2Int>();
            var hollow = new HashSet<Vector2Int>();
            var bridge = new HashSet<Vector2Int>();
            var island = new HashSet<Vector2Int>();
            var terrace = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, CellUse> kv in r.Use)
                switch (kv.Value)
                {
                    case CellUse.Creux: hollow.Add(kv.Key); break;
                    case CellUse.Pont: (r.Feature == RoomFeature.Vide ? bridge : floor).Add(kv.Key); break;
                    case CellUse.Ilot: island.Add(kv.Key); break;
                    case CellUse.Terrasse: terrace.Add(kv.Key); break;
                    case CellUse.Rampe: break;
                    default: floor.Add(kv.Key); break;
                }

            float depth = r.Feature == RoomFeature.Vide ? VoidDepth : FloorDepth;
            foreach (RectInt rc in Rects(floor))
                Solid(root, "Sol", RectCenter(rc, r.Floor - depth * 0.5f), RectSize(rc, depth), 0f, KitBuild.Kind.Bloc, settings.floorMaterial);
            // Sous les rampes : sol au niveau bas (la rampe le recouvre).
            foreach (ActDoor d in Plan.Doors.Where(d => d.RampRoom == r.Id))
                foreach (RectInt rc in Rects(new HashSet<Vector2Int>(d.RampCells)))
                    Solid(root, "Sol (rampe)", RectCenter(rc, r.Floor - FloorDepth * 0.5f), RectSize(rc, FloorDepth), 0f, KitBuild.Kind.Bloc, settings.floorMaterial);
            foreach (RectInt rc in Rects(terrace))
                Solid(root, "Terrasse", RectCenter(rc, r.Floor + (r.TerraceHeight - FloorDepth) * 0.5f), RectSize(rc, r.TerraceHeight + FloorDepth), 0f,
                    KitBuild.Kind.Bloc, settings.floorMaterial);

            if (r.Feature == RoomFeature.Eau && hollow.Count > 0)
                BuildBasin(r, root, hollow, island);
            else if (r.Feature == RoomFeature.Vide && hollow.Count > 0)
                BuildVoid(r, root, hollow, bridge, island);
            else
                foreach (RectInt rc in Rects(new HashSet<Vector2Int>(hollow.Concat(island))))
                    Solid(root, "Sol", RectCenter(rc, r.Floor - depth * 0.5f), RectSize(rc, depth), 0f, KitBuild.Kind.Bloc, settings.floorMaterial);
            if (terrace.Count > 0)
                BuildTerraceRoutes(r, root, terrace);
        }

        /// <summary>Bassin : eau sans danger (on a pied), fond à −waterDepth, deux rampes pour en sortir, îlots à fleur d'eau.</summary>
        private void BuildBasin(ActRoom r, Transform root, HashSet<Vector2Int> hollow, HashSet<Vector2Int> island)
        {
            float depth = settings.waterDepth;
            foreach (RectInt rc in Rects(hollow))
                Solid(root, "Fond du bassin", RectCenter(rc, r.Floor - depth - FloorDepth * 0.5f), RectSize(rc, FloorDepth), 0f, KitBuild.Kind.Bloc, settings.floorMaterial);
            foreach (RectInt rc in Rects(island))
                Solid(root, "Îlot", RectCenter(rc, r.Floor - (depth + FloorDepth) * 0.5f + 0.4f), RectSize(rc, depth + FloorDepth), 0f, KitBuild.Kind.Bloc, settings.floorMaterial);
            // Surface : un plan par rectangle du creux (îlots compris : ils dépassent).
            float surface = r.Floor - depth + Mathf.Min(1.1f, depth * 0.45f);
            var all = new HashSet<Vector2Int>(hollow.Concat(island));
            foreach (RectInt rc in Rects(all))
            {
                Vector3 c = RectCenter(rc, surface);
                var w = new GameObject("Eau");
                w.transform.SetParent(root, false);
                w.transform.position = c;
                w.transform.localScale = new Vector3(rc.width * Plan.Cell, 1f, rc.height * Plan.Cell);
                w.AddComponent<MeshFilter>().sharedMesh = PlaneMesh;
                w.AddComponent<MeshRenderer>().sharedMaterial = settings.waterMaterial;
                if (waterSplash != null)
                {
                    var trig = w.AddComponent<BoxCollider>();
                    trig.isTrigger = true;
                    trig.size = new Vector3(1f, 2f, 1f);
                    trig.center = new Vector3(0f, -0.9f, 0f);
                    WaterSplash.Add(w, waterSplash, surface);
                }
            }

            // Sorties : rampes douces depuis le fond vers deux bords opposés (escaliers lisses).
            int made = 0;
            foreach (Vector2Int c in hollow.OrderBy(_ => _rng.Next()))
            {
                if (made >= 2)
                    break;
                foreach (Vector2Int d in ActPlan.Dirs)
                {
                    Vector2Int edge = c + d, back = c - d, side1 = c + new Vector2Int(d.y, d.x), side2 = c - new Vector2Int(d.y, d.x);
                    if (!r.Use.TryGetValue(edge, out CellUse u) || (u != CellUse.Sol && u != CellUse.Passage && u != CellUse.Pont))
                        continue;
                    if (!hollow.Contains(back) || !hollow.Contains(side1) || !hollow.Contains(side2))
                        continue;
                    Vector3 dir = new Vector3(d.x, 0f, d.y);
                    Vector3 top = Plan.CellCenter(c, r.Floor) + dir * (Plan.Cell * 0.5f);
                    float len = Plan.Cell * 2f;
                    KitBuild.Ramp(root, "Marches du bassin", top - dir * len - Vector3.up * depth, dir, len, depth, Plan.Cell * 2f, settings.kit);
                    made++;
                    break;
                }
            }
        }

        /// <summary>Fosse : vide (−voidDamage PV, retour au dernier sol sûr), ponts étroits et îlots (piliers) pour la traverser.</summary>
        private void BuildVoid(ActRoom r, Transform root, HashSet<Vector2Int> hollow, HashSet<Vector2Int> bridge, HashSet<Vector2Int> island)
        {
            foreach (RectInt rc in Rects(bridge))
                Solid(root, "Pont", RectCenter(rc, r.Floor - 0.35f), RectSize(rc, 0.7f), 0f, KitBuild.Kind.Pont, settings.trimMaterial != null ? settings.trimMaterial : null);
            foreach (RectInt rc in Rects(island))
                Solid(root, "Pilier", RectCenter(rc, r.Floor + 0.3f - VoidDepth * 0.5f), RectSize(rc, VoidDepth + 0.6f), 0f, KitBuild.Kind.Bloc, settings.floorMaterial);
            // Déclencheur sous tout le creux : chute = dégâts et retour au dernier sol sûr.
            var all = new HashSet<Vector2Int>(hollow.Concat(bridge).Concat(island));
            int x0 = all.Min(c => c.x), x1 = all.Max(c => c.x), y0 = all.Min(c => c.y), y1 = all.Max(c => c.y);
            var rect = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            var go = new GameObject("Vide (chute)");
            go.transform.SetParent(root, false);
            go.transform.position = RectCenter(rect, r.Floor - 16f);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = RectSize(rect, 8f) + new Vector3(4f, 0f, 4f);
            go.AddComponent<FallVoid>().Configure(settings.voidDamage, null);
            // Fond noir loin dessous (on ne voit pas le dessous du monde).
            Visual(root, "Fond du vide", CubeMesh, settings.ceilingMaterial != null ? settings.ceilingMaterial : settings.wallMaterial,
                RectCenter(rect, r.Floor - VoidDepth - 1f), Quaternion.identity, RectSize(rect, 1f) + new Vector3(8f, 0f, 8f));
        }

        private static Mesh _plane;
        private static Mesh PlaneMesh
        {
            get
            {
                if (_plane != null)
                    return _plane;
                _plane = new Mesh { name = "Plan d'eau" };
                _plane.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
                _plane.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
                _plane.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                _plane.RecalculateNormals();
                _plane.RecalculateBounds();
                return _plane;
            }
        }

        /// <summary>
        /// Routes de la terrasse : lente (escalier lisse, le traqueur et les goules la prennent aussi) et rapide (mur de wall jump à
        /// 3.6 m de la façade, anneaux de traction lumineux au-dessus).
        /// </summary>
        private void BuildTerraceRoutes(ActRoom r, Transform root, HashSet<Vector2Int> terrace)
        {
            float h = r.TerraceHeight;
            float run = h / Mathf.Tan(28f * Mathf.Deg2Rad);
            int cells = Mathf.CeilToInt(run / Plan.Cell);
            bool stairs = false, wallJump = false;
            foreach (Vector2Int t in terrace.OrderBy(_ => _rng.Next()))
            {
                foreach (Vector2Int d in ActPlan.Dirs)
                {
                    Vector2Int side = new Vector2Int(d.y, d.x);
                    if (terrace.Contains(t + d) || !terrace.Contains(t + side) || !terrace.Contains(t - side))
                        continue;
                    bool free = true;
                    for (int k = 1; k <= cells + 1 && free; k++)
                    for (int s = -1; s <= 1 && free; s++)
                    {
                        Vector2Int c = t + d * k + side * s;
                        free = r.Use.TryGetValue(c, out CellUse u) && (u == CellUse.Sol || (u == CellUse.Passage && k > cells));
                    }

                    if (!free)
                        continue;
                    Vector3 dir = new Vector3(d.x, 0f, d.y);
                    Vector3 edge = Plan.CellCenter(t, r.Floor) + dir * (Plan.Cell * 0.5f);
                    if (!stairs)
                    {
                        KitBuild.Ramp(root, "Escalier de la terrasse", edge + dir * (cells * Plan.Cell), -dir, cells * Plan.Cell, h, Plan.Cell * 2.5f, settings.kit);
                        for (int k = 1; k <= cells; k++)
                        for (int s = -1; s <= 1; s++)
                            r.Use[t + d * k + side * s] = CellUse.Rampe;
                        stairs = true;
                    }
                    else if (!wallJump)
                    {
                        // Mur parallèle à la façade : on rebondit entre les deux jusqu'en haut.
                        float gap = settings.wallJumpGap;
                        Vector3 c = edge + dir * (gap + 0.5f) + Vector3.up * ((h + 4f) * 0.5f);
                        float yaw = Mathf.Atan2(side.x, side.y) * Mathf.Rad2Deg;
                        Solid(root, "Mur de wall jump", c, new Vector3(1f, h + 4f, Plan.Cell * 3f), Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg + 90f,
                            KitBuild.Kind.Mur, null, walkable: false);
                        for (int k = 1; k <= 2; k++)
                        for (int s = -1; s <= 1; s++)
                            if (r.Use.TryGetValue(t + d * k + side * s, out CellUse u) && u == CellUse.Sol)
                                r.Use[t + d * k + side * s] = CellUse.Passage;
                        TractionAnchor(root, edge + Vector3.up * (h + 3.5f) - dir * 0.2f, -dir);
                        wallJump = true;
                    }

                    break;
                }

                if (stairs && wallJump)
                    break;
            }
        }

        /// <summary>Anneau de traction : repère lumineux (bleu, couleur de la traction) au-dessus d'une route rapide.</summary>
        private void TractionAnchor(Transform root, Vector3 at, Vector3 facing)
        {
            var go = new GameObject("Point de traction");
            go.transform.SetParent(root, false);
            go.transform.position = at;
            go.transform.rotation = Quaternion.LookRotation(facing, Vector3.up);
            Visual(go.transform, "Anneau", CylinderMesh, settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial,
                at, go.transform.rotation * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.9f, 0.08f, 0.9f));
            // Pas de petite lumière bleue (nuit 3, point 1) : l'anneau de fer se voit à la lumière des appliques.
        }

        // ---------- Murs ----------

        private void BuildWalls(ActRoom r, Transform root)
        {
            List<ActContour.Segment> segs = ActContour.Of(Plan, r);
            _contours[r.Id] = segs;
            foreach (ActContour.Segment s in segs)
            {
                if (s.Neighbor >= 0 && s.Neighbor < r.Id)
                    continue;                                              // mur commun : construit par la salle de plus petit indice
                ActRoom nb = s.Neighbor >= 0 ? Plan.Rooms[s.Neighbor] : null;
                float bottom = nb != null ? Mathf.Min(Bottom(r), Bottom(nb)) : Bottom(r);
                float top;
                if (nb == null)
                    top = r.Ceiling > 0f ? WallTop(r) : r.Floor + settings.openOuterWall;
                else
                    top = Mathf.Max(WallTop(r), WallTop(nb));
                // Ouvertures de porte sur ce segment (droit, même voisin) : le mur est coupé, linteau et seuil construits par BuildDoor.
                var cuts = new List<(float a, float b)>();
                if (!s.Diagonal && nb != null)
                    foreach (int di in r.Doors)
                    {
                        ActDoor d = Plan.Doors[di];
                        if (d.Other(r.Id) != nb.Id)
                            continue;
                        Vector2 line = (Vector2)d.First + new Vector2(0.5f, 0.5f) + (Vector2)d.Dir * 0.5f;
                        bool vertical = d.Dir.x != 0;
                        if (vertical != (Mathf.Abs(s.A.x - s.B.x) < 0.01f))
                            continue;
                        float across = vertical ? s.A.x - line.x : s.A.y - line.y;
                        if (Mathf.Abs(across) > 0.05f)
                            continue;
                        float from = vertical ? d.First.y : d.First.x;
                        cuts.Add((from, from + d.Length));
                    }

                foreach ((Vector2 a, Vector2 b) in Split(s, cuts))
                    WallPiece(root, a, b, s.Out, s.Diagonal, bottom, top, nb == null, r);
            }
        }

        private static IEnumerable<(Vector2, Vector2)> Split(ActContour.Segment s, List<(float a, float b)> cuts)
        {
            if (cuts.Count == 0)
            {
                yield return (s.A, s.B);
                yield break;
            }

            bool vertical = Mathf.Abs(s.A.x - s.B.x) < 0.01f;
            float lo = vertical ? s.A.y : s.A.x, hi = vertical ? s.B.y : s.B.x;
            Vector2 At(float v) => vertical ? new Vector2(s.A.x, v) : new Vector2(v, s.A.y);
            float cur = lo;
            foreach ((float a, float b) in cuts.OrderBy(c => c.a))
            {
                if (a > cur + 0.01f)
                    yield return (At(cur), At(Mathf.Min(a, hi)));
                cur = Mathf.Max(cur, b);
            }

            if (hi > cur + 0.01f)
                yield return (At(cur), At(hi));
        }

        private void WallPiece(Transform root, Vector2 a, Vector2 b, Vector2 outN, bool diagonal, float bottom, float top, bool outer, ActRoom r)
        {
            Vector3 pa = World(a, 0f), pb = World(b, 0f);
            float len = Vector3.Distance(pa, pb);
            if (len < 0.05f)
                return;
            float t = diagonal ? TDiag : T;
            Vector3 dir = (pb - pa) / len;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Vector3 mid = (pa + pb) * 0.5f + new Vector3(outN.x, 0f, outN.y).normalized * (outer ? t * 0.5f : 0f);
            float h = top - bottom;
            // Ciel ouvert : la falaise extérieure n'est magnétique qu'en bas (on ne l'escalade pas en traction).
            if (outer && r.Ceiling <= 0f && top - r.Floor > settings.outerMagneticTop + 1f)
            {
                float split = r.Floor + settings.outerMagneticTop;
                Solid(root, "Mur", new Vector3(mid.x, (bottom + split) * 0.5f, mid.z), new Vector3(t, split - bottom, len + t), yaw, KitBuild.Kind.Mur, null, walkable: false);
                GameObject high = Solid(root, "Falaise", new Vector3(mid.x, (split + top) * 0.5f, mid.z), new Vector3(t, top - split, len + t), yaw, KitBuild.Kind.Mur,
                    null, walkable: false);
                high.AddComponent<NonMagneticSurface>();
                return;
            }

            Solid(root, outer ? "Mur" : "Mur mitoyen", new Vector3(mid.x, (bottom + top) * 0.5f, mid.z), new Vector3(t, h, len + t), yaw, KitBuild.Kind.Mur, null,
                walkable: false);
        }

        // ---------- Portes ----------

        private void BuildDoor(int di)
        {
            ActDoor d = Plan.Doors[di];
            ActRoom a = Plan.Rooms[d.A], b = Plan.Rooms[d.B];
            Transform root = _roomRoots[Mathf.Min(d.A, d.B)];
            Vector3 c = Plan.DoorCenter(d);
            Vector3 n = Plan.DoorNormal(d);
            float w = Plan.DoorWidth(d);
            float sill = Mathf.Max(a.Floor, b.Floor);
            float openTop = sill + (d.Gate ? Mathf.Max(settings.doorHeight, 11f) : settings.doorHeight);
            float bottom = Mathf.Min(Bottom(a), Bottom(b));
            float top = Mathf.Max(WallTop(a), WallTop(b));
            float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            if (openTop < top - 0.05f)
                Solid(root, "Linteau", new Vector3(c.x, (openTop + top) * 0.5f, c.z), new Vector3(w + T, top - openTop, T), yaw, KitBuild.Kind.Mur, null, walkable: false);
            if (sill > bottom + 0.05f)
                Solid(root, "Seuil", new Vector3(c.x, (bottom + sill) * 0.5f, c.z), new Vector3(w + T, sill - bottom, T), yaw, KitBuild.Kind.Bloc, null);
            // Encadrement (lisibilité : une porte se voit de loin) : piédroits et linteau en pierre de taille.
            Vector3 lat = new Vector3(-n.z, 0f, n.x);
            Material trim = settings.trimMaterial;
            foreach (float side in new[] { -1f, 1f })
                Solid(root, "Piédroit", c + lat * side * (w * 0.5f + 0.35f) + Vector3.up * ((openTop - sill) * 0.5f), new Vector3(0.9f, openTop - sill, T + 0.5f), yaw,
                    KitBuild.Kind.Mur, trim, walkable: false);
            Solid(root, "Arc de la porte", c + Vector3.up * (openTop - sill + 0.4f), new Vector3(w + 1.6f, 0.8f, T + 0.5f), yaw, KitBuild.Kind.Mur, trim, walkable: false);
            Encapsulate(c + Vector3.up * top);

            // Rampe entre deux étages.
            if (d.RampRoom >= 0)
            {
                ActRoom low = Plan.Rooms[d.RampRoom], high = Plan.Rooms[d.Other(d.RampRoom)];
                Vector3 inward = n * (d.RampRoom == d.A ? -1f : 1f);
                float run = d.RampCellsDeep * Plan.Cell;
                Vector3 doorFoot = new Vector3(c.x, low.Floor, c.z) + inward * (T * 0.5f);
                KitBuild.Ramp(_roomRoots[low.Id], "Rampe entre salles", doorFoot + inward * (run - T * 0.5f), -inward, run, high.Floor - low.Floor, w, settings.kit);
            }

            if (d.Exit)
                BuildExitDoor(root, d, c, n, w, sill, openTop);
            else if (d.Locked)
                BuildLockedDoor(root, d, c, n, w, sill, openTop);
            else if (d.Gate)
                BuildArrivalGate(root, d, c, n, w, sill, openTop);
        }
    }
}
