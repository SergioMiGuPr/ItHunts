using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Étanchéité des salles fermées (nuit du 27/11, playtest : à l'acte III, on entrait par les murs dans la salle au coffre sans payer).
    /// Remplissage de voxels depuis le sol de la salle, porte fermée : chaque voxel libre (une boîte de la taille d'un joueur accroupi qui
    /// ne touche aucun collider) est relié à ses six voisins. La salle est étanche si le remplissage n'atteint jamais une case hors de la
    /// salle (à plus d'une case de son bord : l'épaisseur des murs) ni, sous le ciel ouvert, le haut du volume contrôlé.
    /// </summary>
    public sealed partial class ActLevel
    {
        /// <summary>Fuite trouvée : salle, point atteint hors de la salle, et la manière.</summary>
        public struct SealLeak
        {
            public string Room;
            public Vector3 Point;
            public string How;
            public override string ToString() => $"{Room} : {How} à ({Point.x:0.0}, {Point.y:0.0}, {Point.z:0.0})";
        }

        /// <summary>Côté d'un voxel (m) : un joueur accroupi (glissade) passe dans un trou de cette taille.</summary>
        public const float SealVoxel = 0.7f;

        /// <summary>Hauteur contrôlée au-dessus du sol d'une salle sans plafond (m) : au-delà, aucun joueur ne monte.</summary>
        public const float SealOpenHeight = 30f;

        /// <summary>Salles fermées (coffre derrière une porte payée) : fuites trouvées (vide si toutes sont étanches).</summary>
        public List<SealLeak> LockedRoomLeaks() => SealLeaks(r => r.Role == RoomRole.Verrouillee);

        /// <summary>Nombre de salles fermées contrôlées (mesure).</summary>
        public int LockedRoomCount()
        {
            int n = 0;
            if (Plan != null)
                foreach (ActRoom r in Plan.Rooms)
                    if (r.Role == RoomRole.Verrouillee)
                        n++;
            return n;
        }

        private List<SealLeak> SealLeaks(System.Predicate<ActRoom> which)
        {
            var leaks = new List<SealLeak>();
            if (Plan == null)
                return leaks;
            Physics.SyncTransforms();
            foreach (ActRoom r in Plan.Rooms)
                if (which(r))
                {
                    // Deux grilles décalées d'un demi-voxel : une fente plus large qu'un voxel est vue quel que soit son alignement.
                    SealLeak? l = SealOf(r, 0f) ?? SealOf(r, SealVoxel * 0.5f);
                    if (l.HasValue)
                        leaks.Add(l.Value);
                }

            return leaks;
        }

        private SealLeak? SealOf(ActRoom r, float shift)
        {
            var cells = new HashSet<Vector2Int>(r.Cells);
            if (cells.Count == 0)
                return null;
            int cx0 = int.MaxValue, cy0 = int.MaxValue, cx1 = int.MinValue, cy1 = int.MinValue;
            foreach (Vector2Int c in cells)
            {
                cx0 = Mathf.Min(cx0, c.x);
                cy0 = Mathf.Min(cy0, c.y);
                cx1 = Mathf.Max(cx1, c.x);
                cy1 = Mathf.Max(cy1, c.y);
            }

            float margin = Plan.Cell * 2f + 1f;
            Vector3 min = World(new Vector2(cx0, cy0), 0f) - new Vector3(margin - shift, 0f, margin - shift);
            Vector3 max = World(new Vector2(cx1 + 1, cy1 + 1), 0f) + new Vector3(margin, 0f, margin);
            bool open = r.Ceiling <= 0f;
            float y0 = r.Floor + 0.12f + shift * 0.5f;
            float y1 = open ? r.Floor + SealOpenHeight : r.Floor + r.Ceiling + 1.5f;
            const float v = SealVoxel;
            int nx = Mathf.CeilToInt((max.x - min.x) / v), ny = Mathf.CeilToInt((y1 - y0) / v), nz = Mathf.CeilToInt((max.z - min.z) / v);
            var state = new byte[nx * ny * nz];                    // 0 inconnu, 1 libre, 2 plein, 3 vu
            Vector3 half = Vector3.one * (v * 0.5f - 0.02f);
            Vector3 Center(int x, int y, int z) => new Vector3(min.x + (x + 0.5f) * v, y0 + (y + 0.5f) * v, min.z + (z + 0.5f) * v);
            int Index(int x, int y, int z) => (y * nz + z) * nx + x;
            bool Free(int x, int y, int z)
            {
                int i = Index(x, y, z);
                if (state[i] == 0)
                    state[i] = Physics.CheckBox(Center(x, y, z), half, Quaternion.identity, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                        ? (byte)2 : (byte)1;
                return state[i] == 1;
            }

            Vector2Int CellAt(Vector3 p)
            {
                Vector3 u = (p - Plan.Origin) / Plan.Cell;
                return new Vector2Int(Mathf.FloorToInt(u.x), Mathf.FloorToInt(u.z));
            }

            bool FarOutside(Vector3 p)
            {
                Vector2Int c = CellAt(p);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        if (cells.Contains(c + new Vector2Int(dx, dy)))
                            return false;
                return true;
            }

            var queue = new Queue<int>();
            var parent = new Dictionary<int, int>();
            string Trail(int i)
            {
                var pts = new List<string>();
                int n = 0;
                while (parent.TryGetValue(i, out int pi) && pi >= 0 && n++ < 4000)
                {
                    {
                        int tx = i % nx, tz = i / nx % nz, ty = i / (nx * nz);
                        Vector3 q = Center(tx, ty, tz);
                        pts.Add($"({q.x:0},{q.y:0},{q.z:0})");
                    }

                    i = pi;
                }

                pts.Reverse();
                if (pts.Count > 16)
                {
                    var some = new List<string>();
                    for (int k = 0; k < 16; k++)
                        some.Add(pts[k * (pts.Count - 1) / 15]);
                    pts = some;
                }

                return string.Join(" ", pts);
            }

            // Graines : au-dessus de chaque case de sol de la salle, le premier voxel libre en partant du sol.
            foreach (Vector2Int c in cells)
            {
                Vector3 w = World(new Vector2(c.x + 0.5f, c.y + 0.5f), r.Floor + 1f);
                int x = Mathf.FloorToInt((w.x - min.x) / v), z = Mathf.FloorToInt((w.z - min.z) / v);
                for (int y = 0; y < Mathf.Min(ny, 6); y++)
                    if (Free(x, y, z))
                    {
                        int i = Index(x, y, z);
                        if (state[i] != 3)
                        {
                            state[i] = 3;
                            parent[i] = -1;
                            queue.Enqueue(i);
                        }

                        break;
                    }
            }

            int visited = 0;
            while (queue.Count > 0 && visited < 4_000_000)
            {
                int i = queue.Dequeue();
                visited++;
                int x = i % nx, z = i / nx % nz, y = i / (nx * nz);
                Vector3 p = Center(x, y, z);
                if (FarOutside(p))
                    return new SealLeak { Room = r.Name, Point = p, How = "sortie hors de la salle (trou dans le mur ou passage) ; chemin " + Trail(i) };
                if (open && y == ny - 1)
                    return new SealLeak { Room = r.Name, Point = p, How = $"par le haut (ciel ouvert, rien au-dessus jusqu'à {SealOpenHeight:0} m)" };
                for (int k = 0; k < 6; k++)
                {
                    int ax = x + (k == 0 ? 1 : k == 1 ? -1 : 0), ay = y + (k == 2 ? 1 : k == 3 ? -1 : 0), az = z + (k == 4 ? 1 : k == 5 ? -1 : 0);
                    if (ax < 0 || ay < 0 || az < 0 || ax >= nx || ay >= ny || az >= nz)
                    {
                        if (ay >= 0 && ay < ny)
                            return new SealLeak { Room = r.Name, Point = p, How = "bord du volume contrôlé atteint" };
                        continue;
                    }

                    int j = Index(ax, ay, az);
                    if (state[j] == 3 || !Free(ax, ay, az))
                        continue;
                    state[j] = 3;
                    parent[j] = i;
                    queue.Enqueue(j);
                }
            }

            if (visited >= 4_000_000)
                return new SealLeak { Room = r.Name, Point = World(new Vector2(cx0, cy0), r.Floor), How = "non concluant (limite de visites atteinte)" };
            return null;
        }
    }
}
