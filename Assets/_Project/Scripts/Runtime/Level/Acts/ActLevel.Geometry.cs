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

        /// <summary>Matériau des murs de la salle en cours de construction (fonction de la salle, sinon celui de l'acte).</summary>
        private Material RoomWall => _roomMaterials != null && _roomMaterials.wall != null ? _roomMaterials.wall : settings.wallMaterial;

        private float WallTop(ActRoom r) => r.Ceiling > 0f ? r.Floor + r.Ceiling : r.Floor + (r.Special ? 12f : settings.openInnerWall);
        private float Bottom(ActRoom r) => r.Floor - (r.Feature == RoomFeature.Vide ? VoidDepth : FloorDepth);

        private void BuildRoom(ActRoom r)
        {
            Transform root = Group($"Salle : {r.Name}");
            _roomRoots[r.Id] = root;
            _roomMaterials = Art != null ? Art.MaterialsFor(r.Function) : null;
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
                Solid(root, "Sol", RectCenter(rc, r.Floor - depth * 0.5f), RectSize(rc, depth), 0f, KitBuild.Kind.Bloc, null);
            // Sous les rampes : sol au niveau bas (la rampe le recouvre).
            foreach (ActDoor d in Plan.Doors.Where(d => d.RampRoom == r.Id))
                foreach (RectInt rc in Rects(new HashSet<Vector2Int>(d.RampCells)))
                    Solid(root, "Sol (rampe)", RectCenter(rc, r.Floor - FloorDepth * 0.5f), RectSize(rc, FloorDepth), 0f, KitBuild.Kind.Bloc, null);
            foreach (RectInt rc in Rects(terrace))
                Solid(root, "Terrasse", RectCenter(rc, r.Floor + (r.TerraceHeight - FloorDepth) * 0.5f), RectSize(rc, r.TerraceHeight + FloorDepth), 0f,
                    KitBuild.Kind.Bloc, null);

            if (r.Feature == RoomFeature.Eau && hollow.Count > 0)
                BuildBasin(r, root, hollow, island);
            else if (r.Feature == RoomFeature.Vide && hollow.Count > 0)
                BuildVoid(r, root, hollow, bridge, island);
            else
                foreach (RectInt rc in Rects(new HashSet<Vector2Int>(hollow.Concat(island))))
                    Solid(root, "Sol", RectCenter(rc, r.Floor - depth * 0.5f), RectSize(rc, depth), 0f, KitBuild.Kind.Bloc, null);
            if (terrace.Count > 0)
                BuildTerraceRoutes(r, root, terrace);
        }

        /// <summary>Bassin : eau sans danger (on a pied), fond à −waterDepth, deux rampes pour en sortir, îlots à fleur d'eau.</summary>
        private void BuildBasin(ActRoom r, Transform root, HashSet<Vector2Int> hollow, HashSet<Vector2Int> island)
        {
            float depth = settings.waterDepth;
            foreach (RectInt rc in Rects(hollow))
                Solid(root, "Fond du bassin", RectCenter(rc, r.Floor - depth - FloorDepth * 0.5f), RectSize(rc, FloorDepth), 0f, KitBuild.Kind.Bloc, null);
            foreach (RectInt rc in Rects(island))
                Solid(root, "Îlot", RectCenter(rc, r.Floor - (depth + FloorDepth) * 0.5f + 0.4f), RectSize(rc, depth + FloorDepth), 0f, KitBuild.Kind.Bloc, null);
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
            var exits = new HashSet<(Vector2Int, Vector2Int)>();
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
                    KitBuild.Ramp(root, "Marches du bassin", top - dir * len - Vector3.up * depth, dir, len, depth, Plan.Cell * 2f, settings.kit, under: RoomWall);
                    exits.Add((c, d));
                    made++;
                    break;
                }
            }

            // Revue de la DA (jour 5, point 12) : une margelle de pierre le long de chaque bord entre l'eau et le sol (sauf aux marches) :
            // dalle à fleur du sol (+5 cm, sans gêne pour les sauts ni le NavMesh) qui déborde de 0,45 m au-dessus de l'eau.
            if (settings.trimMaterial == null)
                return;
            foreach (Vector2Int c in all)
                foreach (Vector2Int d in ActPlan.Dirs)
                {
                    Vector2Int n = c + d;
                    if (all.Contains(n) || exits.Contains((c, d)) || !r.Use.TryGetValue(n, out CellUse u)
                        || (u != CellUse.Sol && u != CellUse.Passage && u != CellUse.Pont))
                        continue;
                    Vector3 dir = new Vector3(d.x, 0f, d.y);
                    Vector3 mid = Plan.CellCenter(c, r.Floor - 0.1f) + dir * (Plan.Cell * 0.5f);
                    Vector3 size = d.x != 0 ? new Vector3(0.9f, 0.3f, Plan.Cell + 0.9f) : new Vector3(Plan.Cell + 0.9f, 0.3f, 0.9f);
                    Solid(root, "Margelle du bassin", mid, size, 0f, KitBuild.Kind.Pont, settings.trimMaterial);
                }
        }

        /// <summary>Fosse : vide (−voidDamage PV, retour au dernier sol sûr), ponts étroits et îlots (piliers) pour la traverser.</summary>
        private void BuildVoid(ActRoom r, Transform root, HashSet<Vector2Int> hollow, HashSet<Vector2Int> bridge, HashSet<Vector2Int> island)
        {
            foreach (RectInt rc in Rects(bridge))
                Solid(root, "Pont", RectCenter(rc, r.Floor - 0.35f), RectSize(rc, 0.7f), 0f, KitBuild.Kind.Pont, settings.trimMaterial != null ? settings.trimMaterial : null);
            foreach (RectInt rc in Rects(island))
                Solid(root, "Pilier", RectCenter(rc, r.Floor + 0.3f - VoidDepth * 0.5f), RectSize(rc, VoidDepth + 0.6f), 0f, KitBuild.Kind.Bloc, null);
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
            // Paliers (nuit 4) : leur escalier est réservé par le plan (cases Passage devant le palier) ; wall jump en plus si la place.
            foreach (Landing l in r.Landings)
            {
                if (Plan.Hand != null)
                {
                    // Carte faite à la main : le traqueur (7 m de large) ne passe pas l'escalier de 6 m ; il escalade le bord du palier.
                    Vector3 inward = new Vector3(l.StairsDir.x, 0f, l.StairsDir.y);
                    float front = l.Cells.Max(c => Vector2.Dot(c, (Vector2)l.StairsDir));
                    var edgeRow = l.Cells.Where(c => Mathf.Abs(Vector2.Dot(c, (Vector2)l.StairsDir) - front) < 0.1f).OrderBy(c => c.x + c.y).ToList();
                    Vector2Int mid = edgeRow[edgeRow.Count / 4];               // à côté de l'escalier, pas dessus
                    Vector3 lip = Plan.CellCenter(mid, r.Floor + l.Height);
                    // Haut : dans la salle haute, 4 m derrière la porte (le palier de 6 m est trop étroit pour lui).
                    ActDoor door = l.Door >= 0 && l.Door < Plan.Doors.Count ? Plan.Doors[l.Door] : null;
                    Vector3 climbTop = door != null ? Plan.DoorFront(door, door.Other(r.Id), 4f) : lip - inward * Plan.Cell;
                    StalkerClimb(root, lip + inward * (Plan.Cell * 2.5f) - Vector3.up * l.Height, climbTop);
                }

                if (l.Stairs.Count == 0)
                    continue;                                           // porte haute : traction sur le mur au-dessus
                Vector3 dir = new Vector3(l.StairsDir.x, 0f, l.StairsDir.y);      // du palier vers le fond : l'escalier descend
                Vector2Int top = l.Stairs.OrderBy(c => Vector2.Dot(c, (Vector2)l.StairsDir)).First();
                Vector3 topEdge = Plan.CellCenter(top, r.Floor) - dir * (Plan.Cell * 0.5f);
                float len = Mathf.Max(1, l.Stairs.Count / 3) * Plan.Cell;
                KitBuild.Ramp(root, "Escalier du palier", topEdge + dir * len, -dir, len, l.Height, Plan.Cell * 3f, settings.kit, under: RoomWall);
                foreach (Vector2Int c in l.Stairs)
                    r.Use[c] = CellUse.Rampe;
                // Garde-corps bas du palier (lisible) : une margelle le long du bord libre, côté escalier exclu.
                TractionAnchor(root, Plan.CellCenter(top, r.Floor + l.Height + 3.5f) - dir * 0.2f, -dir);
            }

            var done = new HashSet<Vector2Int>();
            foreach (Landing l in r.Landings)
                foreach (Vector2Int c in l.Cells)
                    done.Add(c);
            // Autres terrasses : par composante connexe, un escalier lisse (le traqueur et les goules le prennent) et un mur de wall jump.
            foreach (HashSet<Vector2Int> component in Components(terrace.Where(c => !done.Contains(c))))
                TerraceComponentRoutes(r, root, component);
        }

        /// <summary>Escalade du traqueur (rebord) du pied d'une paroi au bord du haut : lien de son NavMesh (D-110).</summary>
        private void StalkerClimb(Transform root, Vector3 bottom, Vector3 top)
        {
            var go = new GameObject("Escalade du traqueur");
            go.transform.SetParent(root, false);
            go.transform.position = bottom;
            var end = new GameObject("Haut").transform;
            end.SetParent(go.transform, false);
            end.position = top;
            var p = go.AddComponent<ItHunts.Enemies.StalkerPassage>();
            p.kind = top.y - bottom.y > 7.5f ? ItHunts.Enemies.StalkerLinkKind.MurGrimpe : ItHunts.Enemies.StalkerLinkKind.EscaladeRebord;
            p.end = end;
            p.width = 6f;
            StalkerClimbs++;
        }

        /// <summary>
        /// Îlots des fosses et des bassins (D-110) : le traqueur y bondit depuis le sol ferme le plus proche (lien Bond, 22 m au plus) ;
        /// sans cela, un joueur sur un îlot au milieu d'une fosse était hors de sa portée.
        /// </summary>
        private void IslandLeaps()
        {
            foreach (ActRoom r in Plan.Rooms)
            {
                if (r.Feature == RoomFeature.Aucun)
                    continue;
                var islands = Components(r.Cells.Where(c => r.Use[c] == CellUse.Ilot));
                var ground = r.Cells.Where(c => r.Use[c] == CellUse.Sol || r.Use[c] == CellUse.Passage || r.Use[c] == CellUse.Pont).ToList();
                if (ground.Count == 0)
                    continue;
                float islandY = r.Floor + (r.Feature == RoomFeature.Vide ? 0.6f : 0.4f);
                foreach (HashSet<Vector2Int> isl in islands)
                {
                    Vector2 mid = Vector2.zero;
                    foreach (Vector2Int c in isl)
                        mid += c;
                    mid /= isl.Count;
                    Vector2Int best = ground.OrderBy(c => (c - mid).sqrMagnitude).First();
                    if ((best - mid).magnitude * Plan.Cell > 20f)
                        continue;
                    var go = new GameObject("Bond du traqueur vers l'îlot");
                    go.transform.SetParent(_roomRoots[r.Id], false);
                    go.transform.position = Plan.CellCenter(best, r.Floor);
                    var end = new GameObject("Îlot").transform;
                    end.SetParent(go.transform, false);
                    end.position = Plan.CellCenter(mid, islandY);
                    var pass = go.AddComponent<ItHunts.Enemies.StalkerPassage>();
                    pass.kind = ItHunts.Enemies.StalkerLinkKind.Bond;
                    pass.end = end;
                    pass.width = 4f;
                    StalkerClimbs++;
                }
            }
        }

        /// <summary>Escalades du traqueur posées par le niveau (mesure).</summary>
        public int StalkerClimbs { get; private set; }

        /// <summary>Composantes connexes (4-voisinage) d'un ensemble de cases.</summary>
        public static List<HashSet<Vector2Int>> Components(IEnumerable<Vector2Int> cells)
        {
            var left = new HashSet<Vector2Int>(cells);
            var result = new List<HashSet<Vector2Int>>();
            while (left.Count > 0)
            {
                var comp = new HashSet<Vector2Int>();
                var q = new Queue<Vector2Int>();
                Vector2Int seed = left.First();
                left.Remove(seed);
                q.Enqueue(seed);
                comp.Add(seed);
                while (q.Count > 0)
                {
                    Vector2Int c = q.Dequeue();
                    foreach (Vector2Int d in ActPlan.Dirs)
                    {
                        Vector2Int n = c + d;
                        if (left.Remove(n))
                        {
                            comp.Add(n);
                            q.Enqueue(n);
                        }
                    }
                }

                result.Add(comp);
            }

            return result;
        }

        private void TerraceComponentRoutes(ActRoom r, Transform root, HashSet<Vector2Int> terrace)
        {
            float h = r.TerraceHeight;
            float run = h / Mathf.Tan(28f * Mathf.Deg2Rad);
            int cells = Mathf.CeilToInt(run / Plan.Cell);
            bool stairs = r.NoTerraceStairs, wallJump = false;              // galerie sans escalier : wall jump et traction seulement
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
                        KitBuild.Ramp(root, "Escalier de la terrasse", edge + dir * (cells * Plan.Cell), -dir, cells * Plan.Cell, h, Plan.Cell * 2.5f, settings.kit, under: RoomWall);
                        for (int k = 1; k <= cells; k++)
                        for (int s = -1; s <= 1; s++)
                            r.Use[t + d * k + side * s] = CellUse.Rampe;
                        stairs = true;
                    }
                    else if (!wallJump)
                    {
                        if (r.NoTerraceStairs || Plan.Hand != null)
                            StalkerClimb(root, edge + dir * (Plan.Cell * 2.5f), edge - dir * Plan.Cell + Vector3.up * h);
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

                // Prolongement d'un demi-mur aux bouts qui tournent (coin fermé) ; jamais à un bout coupé par une porte ni à une jonction
                // colinéaire (deux murs dans le même plan se chevaucheraient : faces confondues qui scintillent, nuit 4 point 1).
                List<(Vector2, Vector2)> pieces = Split(s, cuts).ToList();
                for (int i = 0; i < pieces.Count; i++)
                {
                    (Vector2 a, Vector2 b) = pieces[i];
                    bool extA = (a - s.A).sqrMagnitude < 1e-4f && Turns(segs, s, s.A);
                    bool extB = (b - s.B).sqrMagnitude < 1e-4f && Turns(segs, s, s.B);
                    WallPiece(root, a, b, s.Out, s.Diagonal, bottom, top, nb == null, r, extA, extB);
                }
            }
        }

        /// <summary>Un autre segment du contour touche ce bout avec une autre direction (coin ou biseau) : le mur se prolonge pour fermer.</summary>
        private static bool Turns(List<ActContour.Segment> segs, ActContour.Segment self, Vector2 end)
        {
            Vector2 dir = (self.B - self.A).normalized;
            foreach (ActContour.Segment o in segs)
            {
                if (o.A == self.A && o.B == self.B)
                    continue;
                bool touches = (o.A - end).sqrMagnitude < 1e-4f || (o.B - end).sqrMagnitude < 1e-4f;
                if (!touches)
                    continue;
                Vector2 od = (o.B - o.A).normalized;
                if (Mathf.Abs(Vector2.Dot(dir, od)) < 0.99f)
                    return true;                                          // perpendiculaire ou à 45° : coin
            }

            return false;
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

        private void WallPiece(Transform root, Vector2 a, Vector2 b, Vector2 outN, bool diagonal, float bottom, float top, bool outer, ActRoom r,
            bool extendA = true, bool extendB = true)
        {
            Vector3 pa = World(a, 0f), pb = World(b, 0f);
            float len0 = Vector3.Distance(pa, pb);
            if (len0 < 0.05f)
                return;
            float t = diagonal ? TDiag : T;
            Vector3 dir = (pb - pa) / len0;
            // Bouts prolongés d'une demi-épaisseur seulement là où le mur tourne (coin fermé) : plus de chevauchement avec le linteau, le
            // seuil ou le mur colinéaire voisin.
            Vector3 qa = pa - dir * (extendA ? t * 0.5f : 0f), qb = pb + dir * (extendB ? t * 0.5f : 0f);
            float len = Vector3.Distance(qa, qb) - t;                     // Solid ajoute t à la longueur : on compense
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            Vector3 mid = (qa + qb) * 0.5f + new Vector3(outN.x, 0f, outN.y).normalized * (outer ? t * 0.5f : 0f);
            float h = top - bottom;
            // Ciel ouvert : la falaise extérieure n'est magnétique qu'en bas (on ne l'escalade pas en traction).
            if (outer && r.Ceiling <= 0f && top - r.Floor > settings.outerMagneticTop + 1f)
            {
                float split = r.Floor + settings.outerMagneticTop;
                Solid(root, "Mur", new Vector3(mid.x, (bottom + split) * 0.5f, mid.z), new Vector3(t, split - bottom, len + t), yaw, KitBuild.Kind.Mur, null, walkable: false);
                if (!diagonal)
                    DressOuterWall(root, pa, pb, -new Vector3(outN.x, 0f, outN.y).normalized, r.Floor, r.Floor + 22f);
                GameObject high = Solid(root, "Falaise", new Vector3(mid.x, (split + top) * 0.5f, mid.z), new Vector3(t, top - split, len + t), yaw, KitBuild.Kind.Mur,
                    null, walkable: false);
                high.AddComponent<NonMagneticSurface>();
                return;
            }

            GameObject wall = Solid(root, outer ? "Mur" : "Mur mitoyen", new Vector3(mid.x, (bottom + top) * 0.5f, mid.z), new Vector3(t, h, len + t), yaw,
                KitBuild.Kind.Mur, null, walkable: false);
            Vector3 inward = -new Vector3(outN.x, 0f, outN.y).normalized;
            if (outer && !diagonal)
                DressOuterWall(root, pa + inward * 0f, pb, inward, r.Floor, top);
            else if (!outer && r.Ceiling <= 0f && DressInnerWall(root, pa, pb, Mathf.Max(r.Floor, bottom + 4f), top))
                foreach (MeshRenderer mr in wall.GetComponentsInChildren<MeshRenderer>())
                    mr.enabled = false;                                   // haie du kit à la place du volume
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
            float openTop = sill + (d.Gate ? Mathf.Max(settings.doorHeight, 11f)
                : d.Exit && Art != null && Art.exitDoor != null ? Mathf.Max(settings.doorHeight, Art.exitDoorOpening) : settings.doorHeight);
            float bottom = Mathf.Min(Bottom(a), Bottom(b));
            float top = Mathf.Max(WallTop(a), WallTop(b));
            float yaw = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            // Linteau et seuil à la largeur exacte de l'ouverture (les murs s'arrêtent net à la coupe) ; le seuil affleure 2 cm sous le sol
            // haut : plus de faces confondues qui scintillent sur les rebords de portes (nuit 4, point 1).
            if (openTop < top - 0.05f)
                Solid(root, "Linteau", new Vector3(c.x, (openTop + top) * 0.5f, c.z), new Vector3(w + 0.04f, top - openTop, T), yaw, KitBuild.Kind.Mur, null, walkable: false);
            if (sill > bottom + 0.05f)
                Solid(root, "Seuil", new Vector3(c.x, (bottom + sill - 0.02f) * 0.5f, c.z), new Vector3(w + 0.04f, sill - 0.02f - bottom, T), yaw, KitBuild.Kind.Bloc, null);
            // Encadrement (lisibilité : une porte se voit de loin) : piédroits et linteau en pierre de taille.
            Vector3 lat = new Vector3(-n.z, 0f, n.x);
            Material trim = settings.trimMaterial;
            // Piédroits hors de l'ouverture (elle garde toute sa largeur pour le NavMesh du traqueur).
            if (!(d.Exit && Art != null && Art.exitDoor != null))
            foreach (float side in new[] { -1f, 1f })
                Solid(root, "Piédroit", c + lat * side * (w * 0.5f + 0.55f) + Vector3.up * ((openTop - sill) * 0.5f), new Vector3(0.9f, openTop - sill, T + 0.5f), yaw,
                    KitBuild.Kind.Mur, trim, walkable: false);
            if (!(d.Exit && Art != null && Art.exitDoor != null))
                Solid(root, "Arc de la porte", c + Vector3.up * (openTop - sill + 0.4f), new Vector3(w + 1.6f, 0.8f, T + 0.5f), yaw, KitBuild.Kind.Mur, trim, walkable: false);
            Encapsulate(c + Vector3.up * top);

            // Rampe entre deux étages.
            if (d.RampRoom >= 0)
            {
                ActRoom low = Plan.Rooms[d.RampRoom], high = Plan.Rooms[d.Other(d.RampRoom)];
                Vector3 inward = n * (d.RampRoom == d.A ? -1f : 1f);
                float run = d.RampCellsDeep * Plan.Cell;
                Vector3 doorFoot = new Vector3(c.x, low.Floor, c.z) + inward * (T * 0.5f);
                KitBuild.Ramp(_roomRoots[low.Id], "Rampe entre salles", doorFoot + inward * (run - T * 0.5f), -inward, run, high.Floor - low.Floor, w, settings.kit, under: settings.wallMaterial);
            }

            // Nuit 4, point 1 (« ouvertures de portes en noir total ») : une lueur chaude de chaque côté de l'ouverture, qui dessine le seuil
            // et laisse deviner la salle voisine (sans ombre, portée 12 m). Pas sur la porte d'arrivée du traqueur (il attend dans le noir).
            if (!d.Gate)
                foreach (float side in new[] { -1f, 1f })
                {
                    int room = side < 0 ? d.A : d.B;
                    if (Plan.Rooms[room].Role == RoomRole.Arrivee || Plan.Rooms[room].Role == RoomRole.Repaire)
                        continue;
                    Vector3 at = c + n * side * 2.5f;
                    at.y = Plan.Rooms[room].Floor + Mathf.Min(6f, openTop - sill - 1f);
                    PointLight(_roomRoots[room], at, settings.torchColor, settings.torchIntensity * 0.55f, 12f).name = "Lueur de la porte";
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
