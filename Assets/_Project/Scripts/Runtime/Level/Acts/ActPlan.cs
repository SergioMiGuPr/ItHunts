using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>Usage d'une case dans une salle (plan d'un acte).</summary>
    public enum CellUse : byte
    {
        Sol,
        /// <summary>Bassin (eau sans danger) ou fosse (vide) selon la salle.</summary>
        Creux,
        /// <summary>Pont au-dessus du creux.</summary>
        Pont,
        Terrasse,
        /// <summary>Rampe entre deux salles de hauteurs différentes (dans la plus basse).</summary>
        Rampe,
        /// <summary>Abords d'une porte et chemins entre les portes : rien n'y est posé.</summary>
        Passage,
        /// <summary>Îlot (plateforme) dans un creux : route rapide.</summary>
        Ilot,
    }

    public enum RoomFeature : byte
    {
        Aucun,
        Eau,
        Vide,
    }

    /// <summary>Rôle particulier d'une salle.</summary>
    public enum RoomRole : byte
    {
        Normale,
        Depart,
        Sortie,
        /// <summary>Vestibule derrière la porte de sortie (déclencheur de fin d'acte).</summary>
        HallDeSortie,
        /// <summary>Repaire du traqueur (acte I).</summary>
        Repaire,
        /// <summary>Vestibule derrière la porte d'arrivée, où le traqueur attend (actes II et III).</summary>
        Arrivee,
        Verrouillee,
    }

    /// <summary>Une salle organique (cases de la grille de tracé).</summary>
    public sealed class ActRoom
    {
        public int Id;
        public string Name;
        public string Function;
        public RoomRole Role;
        public readonly List<Vector2Int> Cells = new List<Vector2Int>();
        public readonly Dictionary<Vector2Int, CellUse> Use = new Dictionary<Vector2Int, CellUse>();
        public RectInt Box;
        public float Floor;
        /// <summary>Hauteur sous plafond (m au-dessus du sol) ; 0 : ciel ouvert.</summary>
        public float Ceiling;
        public int Parent = -1, Depth;
        public RoomFeature Feature;
        public float TerraceHeight;
        public readonly List<int> Doors = new List<int>();
        public Vector2 CenterCell;

        public bool Special => Role == RoomRole.HallDeSortie || Role == RoomRole.Repaire || Role == RoomRole.Arrivee || Role == RoomRole.Verrouillee;
        public float Area(float cell) => Cells.Count * cell * cell;
    }

    /// <summary>Porte entre deux salles : une ouverture droite sur la limite commune.</summary>
    public sealed class ActDoor
    {
        public int A, B;
        /// <summary>Direction de A vers B (axe de la grille).</summary>
        public Vector2Int Dir;
        /// <summary>Case de A au bord de l'ouverture (la première), et longueur (cases) le long de la limite.</summary>
        public Vector2Int First;
        public int Length;
        public bool Locked, Gate, Exit, Loop;
        /// <summary>Salle qui porte la rampe (la plus basse ; −1 : même hauteur) et ses cases.</summary>
        public int RampRoom = -1;
        public readonly List<Vector2Int> RampCells = new List<Vector2Int>();
        public int RampCellsDeep;

        public Vector2Int Along => new Vector2Int(Mathf.Abs(Dir.y), Mathf.Abs(Dir.x));
        public int Other(int room) => room == A ? B : A;
    }

    /// <summary>
    /// Plan d'un acte (D-092) : salles aux formes organiques posées salle à salle sur une grille de tracé invisible (cases de 2 m),
    /// sans couloirs ; une porte droite sur la limite commune de deux salles voisines, des boucles (portes en plus) pour tourner
    /// autour du traqueur ; étages de sol différents reliés par des rampes ; fonction de chaque salle (cachot, chapelle, salle de
    /// bal…) ; creux (bassin ou fosse) traversé par des ponts, terrasses. Calcul pur à partir de la seed (testé en EditMode, jeu en
    /// ligne : même plan partout).
    /// </summary>
    public sealed class ActPlan
    {
        public ActLayoutSettings Settings;
        public int Seed;
        public int Size;
        public float Cell;
        public Vector3 Origin;
        public int[,] Owner;
        public readonly List<ActRoom> Rooms = new List<ActRoom>();
        public readonly List<ActDoor> Doors = new List<ActDoor>();
        public readonly List<string> Failures = new List<string>();
        public int Start = -1, Exit = -1, ExitHall = -1, Lair = -1, Arrival = -1;
        public int StartExitDepth;

        private System.Random _rng;
        private int DoorCells => Mathf.Max(2, Mathf.RoundToInt(Settings.doorWidth / Cell));

        public Vector3 CellCenter(Vector2Int c, float y = 0f) => Origin + new Vector3((c.x + 0.5f) * Cell, y, (c.y + 0.5f) * Cell);
        public Vector3 CellCenter(Vector2 c, float y = 0f) => Origin + new Vector3((c.x + 0.5f) * Cell, y, (c.y + 0.5f) * Cell);

        public int OwnerAt(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < Size && c.y < Size ? Owner[c.x, c.y] : -1;

        public Vector2Int CellOf(Vector3 p) => new Vector2Int(Mathf.FloorToInt((p.x - Origin.x) / Cell), Mathf.FloorToInt((p.z - Origin.z) / Cell));

        /// <summary>Salle sous ce point (−1 : hors des salles).</summary>
        public int RoomAt(Vector3 p) => OwnerAt(CellOf(p));

        // ---------- Portes : géométrie ----------

        /// <summary>Centre de l'ouverture (au sol de la plus haute des deux salles), normale de A vers B, largeur (m).</summary>
        public Vector3 DoorCenter(ActDoor d)
        {
            Vector2 first = d.First, along = d.Along;
            Vector2 mid = first + along * ((d.Length - 1) * 0.5f) + (Vector2)d.Dir * 0.5f;
            float y = Mathf.Max(Rooms[d.A].Floor, Rooms[d.B].Floor);
            return CellCenter(mid, y);
        }

        public Vector3 DoorNormal(ActDoor d) => new Vector3(d.Dir.x, 0f, d.Dir.y);
        public float DoorWidth(ActDoor d) => d.Length * Cell;

        /// <summary>Point devant la porte, côté de la salle donnée, à cette distance (m), au sol de cette salle.</summary>
        public Vector3 DoorFront(ActDoor d, int room, float distance)
        {
            Vector3 c = DoorCenter(d);
            Vector3 n = DoorNormal(d) * (room == d.A ? -1f : 1f);
            Vector3 p = c + n * distance;
            p.y = Rooms[room].Floor;
            if (d.RampRoom == room)
                p += n * (d.RampCellsDeep * Cell);
            return p;
        }

        // ---------- Tirage ----------

        public static ActPlan Make(ActLayoutSettings settings, int seed)
        {
            var p = new ActPlan { Settings = settings, Seed = seed, Size = settings.gridSize, Cell = settings.cell };
            p.Origin = new Vector3(-p.Size * p.Cell * 0.5f, 0f, -p.Size * p.Cell * 0.5f);
            for (int attempt = 0; attempt < 6; attempt++)
            {
                p.Reset(seed + attempt * 7919);
                if (p.Build())
                    return p;
            }

            p.Failures.Add("aucun essai complet : plan gardé tel quel");
            return p;
        }

        private void Reset(int seed)
        {
            _rng = new System.Random(seed);
            Owner = new int[Size, Size];
            for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                Owner[x, y] = -1;
            Rooms.Clear();
            Doors.Clear();
            Start = Exit = ExitHall = Lair = Arrival = -1;
        }

        private bool Build()
        {
            ActLayoutSettings s = Settings;
            // Départ au centre.
            bool[,] first = Shape(s.theme, Mathf.Lerp(s.roomArea.x, s.roomArea.y, 0.35f), RoomRole.Depart);
            Vector2Int at = new Vector2Int(Size / 2 - first.GetLength(0) / 2, Size / 2 - first.GetLength(1) / 2);
            Start = Commit(first, at, -1, RoomRole.Depart, 0f);
            int mainTip = Start;
            // Porte d'arrivée (actes II, III) : posée tout de suite contre la salle de départ, avant que les autres salles l'entourent.
            if (s.stalkerFromArrival)
            {
                Arrival = TryAttach(Rect(Cells(18f), Cells(24f)), Start, RoomRole.Arrivee, null, gate: true);
                if (Arrival < 0)
                    Failures.Add("pas de place pour la porte d'arrivée du traqueur");
            }

            for (int k = 1; k < s.rooms; k++)
            {
                int placed = -1;
                for (int attempt = 0; attempt < 90 && placed < 0; attempt++)
                {
                    bool main = _rng.NextDouble() < s.mainPathShare;
                    int parent = main ? mainTip : PickParent();
                    float area = Mathf.Lerp(s.roomArea.x, s.roomArea.y, Mathf.Pow((float)_rng.NextDouble(), 1.6f));
                    bool[,] shape = Shape(s.theme, area, RoomRole.Normale);
                    placed = TryAttach(shape, parent, RoomRole.Normale, main ? AwayFromStart(parent) : (float?)null);
                    if (placed >= 0 && main)
                        mainTip = placed;
                }

                if (placed < 0)
                {
                    Failures.Add($"salle {k} : pas de place");
                    break;
                }
            }

            if (Rooms.Count < Mathf.Max(4, s.rooms * 2 / 3))
                return false;

            FillGaps(Settings.theme == ActTheme.Jardin ? 14 : 12);

            // Sortie : la salle la plus profonde qui accepte un hall de sortie.
            int[] depth = Depths(Start);
            foreach (int r in Enumerable.Range(0, Rooms.Count).Where(i => i != Start).OrderByDescending(i => depth[i]))
            {
                bool[,] hall = Rect(Cells(18f), Cells(16f));
                int h = TryAttach(hall, r, RoomRole.HallDeSortie, null, exitDoor: true);
                if (h < 0)
                    continue;
                Exit = r;
                ExitHall = h;
                Rooms[r].Role = RoomRole.Sortie;
                StartExitDepth = depth[r];
                break;
            }

            if (Exit < 0)
                return false;

            // Traqueur : repaire à mi-chemin (acte I, ou si la porte d'arrivée n'a pas eu de place).
            if (Arrival < 0 && s.theme != ActTheme.Boss)
            {
                int half = Mathf.Max(1, StartExitDepth / 2);
                foreach (int r in Enumerable.Range(0, Rooms.Count).Where(i => !Rooms[i].Special && i != Start && i != Exit)
                             .OrderBy(i => Mathf.Abs(depth[i] - half)))
                {
                    Lair = TryAttach(Rect(Cells(20f), Cells(26f)), r, RoomRole.Repaire, null);
                    if (Lair >= 0)
                        break;
                }
            }

            // Salles verrouillées (coffre gratuit derrière une porte payante).
            int locked = 0;
            foreach (int r in Enumerable.Range(0, Rooms.Count).Where(i => !Rooms[i].Special && i != Start).OrderBy(_ => _rng.Next()).ToList())
            {
                if (locked >= s.lockedRooms)
                    break;
                if (TryAttach(Rect(Cells(18f), Cells(16f)), r, RoomRole.Verrouillee, null, locked: true) >= 0)
                    locked++;
            }

            AddLoops();
            AssignFunctions(depth);
            foreach (ActRoom r in Rooms)
                PlanInterior(r);
            return true;
        }

        private int Cells(float meters) => Mathf.Max(2, Mathf.RoundToInt(meters / Cell));

        private int PickParent()
        {
            var options = Rooms.Where(r => !r.Special && r.Doors.Count < 4).ToList();
            return options.Count > 0 ? options[_rng.Next(options.Count)].Id : Start;
        }

        private float AwayFromStart(int parent)
        {
            Vector2 d = Rooms[parent].CenterCell - Rooms[Start].CenterCell;
            float baseAngle = d.sqrMagnitude < 1f ? (float)_rng.NextDouble() * 360f : Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            return baseAngle + ((float)_rng.NextDouble() * 2f - 1f) * 75f;
        }

        // ---------- Formes ----------

        private static bool[,] Rect(int w, int h)
        {
            var m = new bool[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                m[x, y] = true;
            return m;
        }

        /// <summary>
        /// Forme d'une salle (masque de cases) : caves et jardin, union de 2 à 4 ellipses bruitée puis lissée ; château, union de 1 à 3
        /// rectangles (L, T, croix), coins coupés ou abside ; boss, une grande ellipse. Les parties plus étroites que minWidth sont
        /// retirées (ouverture morphologique), seule la plus grande partie est gardée.
        /// </summary>
        private bool[,] Shape(ActTheme theme, float area, RoomRole role)
        {
            float target = area / (Cell * Cell);
            bool[,] m = null;
            float scale = 1f;
            for (int pass = 0; pass < 3; pass++)
            {
                m = RawShape(theme, target * scale);
                int n = Count(m);
                if (n == 0)
                {
                    scale *= 1.5f;
                    continue;
                }

                float ratio = target / n;
                if (ratio > 0.75f && ratio < 1.35f)
                    break;
                scale *= ratio;
            }

            return m;
        }

        private bool[,] RawShape(ActTheme theme, float target)
        {
            int side = Mathf.CeilToInt(Mathf.Sqrt(target) * 2.6f) + 6;
            var m = new bool[side, side];
            float cx = side * 0.5f, cy = side * 0.5f;
            int minW = Mathf.Max(3, Mathf.RoundToInt(Settings.minWidth / Cell));
            if (theme == ActTheme.Chateau)
            {
                int parts = 1 + _rng.Next(3);
                float partArea = target / (parts * 0.85f);
                for (int i = 0; i < parts; i++)
                {
                    float aspect = Mathf.Lerp(1f, 2.6f, (float)_rng.NextDouble());
                    int w = Mathf.Max(minW, Mathf.RoundToInt(Mathf.Sqrt(partArea * aspect)));
                    int h = Mathf.Max(minW, Mathf.RoundToInt(partArea / w));
                    if (_rng.Next(2) == 0)
                        (w, h) = (h, w);
                    float ox = i == 0 ? 0f : ((float)_rng.NextDouble() - 0.5f) * w * 0.9f;
                    float oy = i == 0 ? 0f : ((float)_rng.NextDouble() - 0.5f) * h * 0.9f;
                    FillRect(m, Mathf.RoundToInt(cx + ox - w * 0.5f), Mathf.RoundToInt(cy + oy - h * 0.5f), w, h);
                }

                int style = _rng.Next(3);
                if (style == 0)
                    CutCorners(m, Mathf.Max(2, minW / 2));                      // salle octogonale
                else if (style == 1)
                    AddApse(m);                                                // abside (chapelle, salle du trône)
            }
            else
            {
                float r0 = Mathf.Sqrt(target / Mathf.PI) * (theme == ActTheme.Boss ? 1f : 0.82f);
                int lobes = theme == ActTheme.Boss ? 1 : 2 + _rng.Next(3);
                for (int i = 0; i < lobes; i++)
                {
                    float r = i == 0 ? r0 : r0 * Mathf.Lerp(0.45f, 0.8f, (float)_rng.NextDouble());
                    float ang = (float)_rng.NextDouble() * Mathf.PI * 2f;
                    float dist = i == 0 ? 0f : r0 * Mathf.Lerp(0.5f, 0.95f, (float)_rng.NextDouble());
                    float ex = r * Mathf.Lerp(0.8f, 1.3f, (float)_rng.NextDouble()), ey = r * r * 1f / ex;
                    FillEllipse(m, cx + Mathf.Cos(ang) * dist, cy + Mathf.Sin(ang) * dist, ex, ey, (float)_rng.NextDouble() * Mathf.PI);
                }

                if (theme != ActTheme.Boss)
                    Roughen(m, theme == ActTheme.Caves ? 0.35f : 0.2f);
            }

            Open(m, minW / 2);
            KeepLargest(m);
            return Trim(m);
        }

        private static void FillRect(bool[,] m, int x0, int y0, int w, int h)
        {
            for (int x = Mathf.Max(0, x0); x < Mathf.Min(m.GetLength(0), x0 + w); x++)
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(m.GetLength(1), y0 + h); y++)
                m[x, y] = true;
        }

        private static void FillEllipse(bool[,] m, float cx, float cy, float rx, float ry, float rot)
        {
            float c = Mathf.Cos(rot), s = Mathf.Sin(rot);
            for (int x = 0; x < m.GetLength(0); x++)
            for (int y = 0; y < m.GetLength(1); y++)
            {
                float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                float u = (dx * c + dy * s) / rx, v = (-dx * s + dy * c) / ry;
                if (u * u + v * v <= 1f)
                    m[x, y] = true;
            }
        }

        private static void CutCorners(bool[,] m, int k)
        {
            RectInt b = Bounds(m);
            for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
            {
                int dx = Mathf.Min(x - b.xMin, b.xMax - 1 - x), dy = Mathf.Min(y - b.yMin, b.yMax - 1 - y);
                if (dx + dy < k)
                    m[x, y] = false;
            }
        }

        private void AddApse(bool[,] m)
        {
            RectInt b = Bounds(m);
            int side = _rng.Next(4);
            float r = Mathf.Min(b.width, b.height) * 0.35f;
            Vector2 c = side switch
            {
                0 => new Vector2(b.center.x, b.yMax),
                1 => new Vector2(b.center.x, b.yMin),
                2 => new Vector2(b.xMax, b.center.y),
                _ => new Vector2(b.xMin, b.center.y),
            };
            FillEllipse(m, c.x, c.y, r, r, 0f);
        }

        /// <summary>Bord irrégulier (caves) : bruit sur les cases du bord puis lissage par automate (5 voisins sur 8).</summary>
        private void Roughen(bool[,] m, float noise)
        {
            int w = m.GetLength(0), h = m.GetLength(1);
            var copy = (bool[,])m.Clone();
            for (int x = 1; x < w - 1; x++)
            for (int y = 1; y < h - 1; y++)
            {
                int n = Neighbors8(copy, x, y);
                if (n > 0 && n < 8 && _rng.NextDouble() < noise)
                    m[x, y] = !m[x, y];
            }

            for (int pass = 0; pass < 2; pass++)
            {
                copy = (bool[,])m.Clone();
                for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                {
                    int n = Neighbors8(copy, x, y);
                    m[x, y] = n >= 5 || (copy[x, y] && n >= 4);
                }
            }
        }

        private static int Neighbors8(bool[,] m, int x, int y)
        {
            int n = 0;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if ((dx != 0 || dy != 0) && m[x + dx, y + dy])
                    n++;
            return n;
        }

        /// <summary>Ouverture (érosion puis dilatation d'un disque) : retire les goulets plus étroits que 2r + 1 cases.</summary>
        public static void Open(bool[,] m, int r)
        {
            if (r <= 0)
                return;
            bool[,] e = Morph(m, r, true);
            bool[,] d = Morph(e, r, false);
            for (int x = 0; x < m.GetLength(0); x++)
            for (int y = 0; y < m.GetLength(1); y++)
                m[x, y] = d[x, y];
        }

        private static bool[,] Morph(bool[,] m, int r, bool erode)
        {
            int w = m.GetLength(0), h = m.GetLength(1);
            var o = new bool[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                bool any = false, all = true;
                for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (dx * dx + dy * dy > r * r + r)
                        continue;
                    int px = x + dx, py = y + dy;
                    bool v = px >= 0 && py >= 0 && px < w && py < h && m[px, py];
                    any |= v;
                    all &= v;
                }

                o[x, y] = erode ? all : any;
            }

            return o;
        }

        private static void KeepLargest(bool[,] m)
        {
            int w = m.GetLength(0), h = m.GetLength(1);
            var label = new int[w, h];
            int best = 0, bestSize = 0, next = 0;
            var stack = new Stack<Vector2Int>();
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (!m[x, y] || label[x, y] != 0)
                    continue;
                next++;
                int size = 0;
                stack.Push(new Vector2Int(x, y));
                label[x, y] = next;
                while (stack.Count > 0)
                {
                    Vector2Int c = stack.Pop();
                    size++;
                    foreach (Vector2Int d in Dirs)
                    {
                        Vector2Int q = c + d;
                        if (q.x < 0 || q.y < 0 || q.x >= w || q.y >= h || !m[q.x, q.y] || label[q.x, q.y] != 0)
                            continue;
                        label[q.x, q.y] = next;
                        stack.Push(q);
                    }
                }

                if (size > bestSize)
                {
                    bestSize = size;
                    best = next;
                }
            }

            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                m[x, y] = m[x, y] && label[x, y] == best;
        }

        private static RectInt Bounds(bool[,] m)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
            for (int x = 0; x < m.GetLength(0); x++)
            for (int y = 0; y < m.GetLength(1); y++)
                if (m[x, y])
                {
                    x0 = Mathf.Min(x0, x);
                    y0 = Mathf.Min(y0, y);
                    x1 = Mathf.Max(x1, x);
                    y1 = Mathf.Max(y1, y);
                }

            return x1 < 0 ? new RectInt(0, 0, 0, 0) : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        private static bool[,] Trim(bool[,] m)
        {
            RectInt b = Bounds(m);
            var o = new bool[Mathf.Max(1, b.width), Mathf.Max(1, b.height)];
            for (int x = 0; x < b.width; x++)
            for (int y = 0; y < b.height; y++)
                o[x, y] = m[b.x + x, b.y + y];
            return o;
        }

        private static int Count(bool[,] m)
        {
            int n = 0;
            foreach (bool b in m)
                if (b)
                    n++;
            return n;
        }

        public static readonly Vector2Int[] Dirs = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };

        // ---------- Placement ----------

        private bool Overlaps(bool[,] shape, Vector2Int at)
        {
            int w = shape.GetLength(0), h = shape.GetLength(1);
            if (at.x < 3 || at.y < 3 || at.x + w > Size - 3 || at.y + h > Size - 3)
                return true;
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (shape[x, y] && Owner[at.x + x, at.y + y] != -1)
                    return true;
            return false;
        }

        /// <summary>
        /// Pose la forme contre la salle parent : partie de son centre, elle glisse dans une direction (donnée ou au hasard) jusqu'à
        /// ne plus rien chevaucher, puis il faut une limite commune droite assez longue pour une porte. Renvoie la salle, ou −1.
        /// </summary>
        private int TryAttach(bool[,] shape, int parent, RoomRole role, float? angleDeg, bool exitDoor = false, bool gate = false, bool locked = false)
        {
            ActRoom pr = Rooms[parent];
            int w = shape.GetLength(0), h = shape.GetLength(1);
            // Candidats : plusieurs directions ; on garde celui qui touche le plus d'autres salles (plan compact, boucles possibles).
            int candidates = role == RoomRole.Normale ? 6 : 14;
            List<Vector2Int> bestCells = null, bestExtra = null;
            Vector2Int first = default, d = default, bestAt = default;
            int len = 0;
            float bestScore = float.NegativeInfinity;
            for (int tries = 0; tries < candidates; tries++)
            {
                float ang = (tries == 0 && angleDeg.HasValue ? angleDeg.Value : (float)_rng.NextDouble() * 360f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                // Petites salles annexes : dans l'axe d'une grille (portes droites, contact franc).
                if (role != RoomRole.Normale)
                    dir = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? new Vector2(Mathf.Sign(dir.x), 0f) : new Vector2(0f, Mathf.Sign(dir.y));
                Vector2 pos = pr.CenterCell - new Vector2(w, h) * 0.5f;
                Vector2Int at = Vector2Int.zero;
                bool free = false;
                for (int step = 0; step < 440; step++)
                {
                    at = new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
                    if (at.x < 3 || at.y < 3 || at.x + w > Size - 3 || at.y + h > Size - 3)
                        break;
                    if (!Overlaps(shape, at))
                    {
                        free = true;
                        break;
                    }

                    pos += dir * 0.5f;
                }

                if (!free)
                    continue;
                var tmp = new List<Vector2Int>();
                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    if (shape[x, y])
                        tmp.Add(new Vector2Int(at.x + x, at.y + y));
                var extra = new List<Vector2Int>();
                if (!BestRun(tmp, parent, DoorCells + 2, out Vector2Int f0, out Vector2Int d0, out int l0)
                    && !SquareContact(tmp, parent, out f0, out d0, out l0, extra))
                    continue;
                // Score : voisins à moins de 4 cases (autres que le parent), un peu de hasard ; chemin principal : s'éloigner du départ.
                var near = new HashSet<int>();
                foreach (Vector2Int c in tmp)
                    for (int k = 1; k <= 4; k += 3)
                        foreach (Vector2Int dd in Dirs)
                        {
                            int o = OwnerAt(c + dd * k);
                            if (o >= 0 && o != parent && !Rooms[o].Special)
                                near.Add(o);
                        }

                float score = near.Count * 2f + (float)_rng.NextDouble();
                if (angleDeg.HasValue && Start >= 0)
                    score += (new Vector2(at.x + w * 0.5f, at.y + h * 0.5f) - Rooms[Start].CenterCell).magnitude * 0.04f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestCells = tmp;
                    bestExtra = extra;
                    bestAt = at;
                    first = f0;
                    d = d0;
                    len = l0;
                }

                if (role != RoomRole.Normale)
                    break;                                                   // annexes : le premier qui va
            }

            if (bestCells != null)
            {
                Vector2Int at = bestAt;
                List<Vector2Int> tmp = bestCells;
                List<Vector2Int> extraParent = bestExtra;
                float floor = pr.Floor;
                if (role == RoomRole.Normale && Settings.floorStep > 0f && _rng.NextDouble() < Settings.floorChangeChance)
                {
                    float f = pr.Floor + (_rng.Next(2) == 0 ? -1f : 1f) * Settings.floorStep;
                    if (f >= Settings.minFloor - 0.01f && f <= Settings.maxFloor + 0.01f)
                        floor = f;
                }

                foreach (Vector2Int c in extraParent)
                {
                    Owner[c.x, c.y] = parent;
                    pr.Cells.Add(c);
                    pr.Use[c] = CellUse.Sol;
                }

                if (extraParent.Count > 0)
                    RecomputeBox(pr);
                int id = Commit(shape, at, parent, role, floor, tmp);
                ActDoor door = MakeDoor(parent, id, first, d, len);
                door.Exit = exitDoor;
                door.Gate = gate;
                door.Locked = locked;
                if (!PlanRamp(door))
                {
                    Rooms[id].Floor = pr.Floor;                                  // pas de place pour la rampe : même hauteur
                    door.RampRoom = -1;
                    door.RampCells.Clear();
                }

                AddDoor(door);
                return id;
            }

            return -1;
        }

        private int Commit(bool[,] shape, Vector2Int at, int parent, RoomRole role, float floor, List<Vector2Int> cells = null)
        {
            var r = new ActRoom { Id = Rooms.Count, Role = role, Parent = parent, Floor = floor };
            r.Depth = parent >= 0 ? Rooms[parent].Depth + 1 : 0;
            if (cells == null)
            {
                cells = new List<Vector2Int>();
                for (int x = 0; x < shape.GetLength(0); x++)
                for (int y = 0; y < shape.GetLength(1); y++)
                    if (shape[x, y])
                        cells.Add(new Vector2Int(at.x + x, at.y + y));
            }

            foreach (Vector2Int c in cells)
            {
                if (Owner[c.x, c.y] != -1)
                    continue;
                Owner[c.x, c.y] = r.Id;
                r.Cells.Add(c);
                r.Use[c] = CellUse.Sol;
            }

            RecomputeBox(r);
            float t = Mathf.InverseLerp(Settings.roomArea.x, Settings.roomArea.y, r.Area(Cell));
            r.Ceiling = Settings.Open ? 0f : Mathf.Lerp(Settings.ceiling.x, Settings.ceiling.y, t);
            if (role == RoomRole.Repaire || role == RoomRole.Arrivee)
                r.Ceiling = Settings.Open ? 0f : Mathf.Max(r.Ceiling, 18f);
            Rooms.Add(r);
            return r.Id;
        }

        /// <summary>
        /// Plus longue limite droite entre ces cases (nouvelle salle) et la salle other : l'ouverture d'une porte au milieu.
        /// first : case de other au bord de l'ouverture ; dir : de other vers la nouvelle salle.
        /// </summary>
        private bool BestRun(List<Vector2Int> cells, int other, int minLen, out Vector2Int first, out Vector2Int dir, out int len)
        {
            var set = new HashSet<Vector2Int>(cells);
            var edges = new Dictionary<(Vector2Int d, int line), List<int>>();
            foreach (Vector2Int c in cells)
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int o = c - d;                                        // case de other, d : de other vers la nouvelle
                    if (OwnerAt(o) != other || set.Contains(o))
                        continue;
                    int line = d.x != 0 ? o.x : o.y, pos = d.x != 0 ? o.y : o.x;
                    if (!edges.TryGetValue((d, line), out List<int> l))
                        edges[(d, line)] = l = new List<int>();
                    l.Add(pos);
                }

            return PickRun(edges, minLen, out first, out dir, out len);
        }

        private bool PickRun(Dictionary<(Vector2Int d, int line), List<int>> edges, int minLen, out Vector2Int first, out Vector2Int dir, out int len)
        {
            first = default;
            dir = default;
            len = 0;
            var runs = new List<(Vector2Int d, int line, int from, int n)>();
            foreach (KeyValuePair<(Vector2Int d, int line), List<int>> kv in edges)
            {
                List<int> l = kv.Value;
                l.Sort();
                int start = 0;
                for (int i = 1; i <= l.Count; i++)
                    if (i == l.Count || l[i] != l[i - 1] + 1)
                    {
                        int n = i - start;
                        if (n >= minLen)
                            runs.Add((kv.Key.d, kv.Key.line, l[start], n));
                        start = i;
                    }
            }

            if (runs.Count == 0)
                return false;
            // Une des plus longues, au hasard parmi celles d'au moins 70 % de la meilleure.
            int best = runs.Max(r => r.n);
            var good = runs.Where(r => r.n >= best * 0.7f).ToList();
            (Vector2Int d, int line, int from, int n) pick = good[_rng.Next(good.Count)];
            int doorLen = DoorCells;
            int offset = (pick.n - doorLen) / 2 + (pick.n - doorLen > 2 ? _rng.Next(-(pick.n - doorLen) / 4, (pick.n - doorLen) / 4 + 1) : 0);
            offset = Mathf.Clamp(offset, 1, pick.n - doorLen - 1);
            int at = pick.from + offset;
            first = pick.d.x != 0 ? new Vector2Int(pick.line, at) : new Vector2Int(at, pick.line);
            dir = pick.d;
            len = doorLen;
            return true;
        }

        /// <summary>
        /// Interstices (D-092) : une case libre entre deux salles proches (somme des distances ≤ maxGap cases) rejoint la plus proche. Les
        /// salles voisines se touchent alors le long d'une limite organique (médiatrice) : on peut y ouvrir des boucles.
        /// </summary>
        private void FillGaps(int maxGap)
        {
            int n = Rooms.Count;
            var best = new Dictionary<Vector2Int, (int room, int d, int room2, int d2)>();
            for (int r = 0; r < n; r++)
            {
                if (Rooms[r].Special)
                    continue;
                var dist = new Dictionary<Vector2Int, int>();
                var q = new Queue<Vector2Int>();
                foreach (Vector2Int c in Rooms[r].Cells)
                    foreach (Vector2Int d in Dirs)
                    {
                        Vector2Int f = c + d;
                        if (OwnerAt(f) == -1 && f.x > 3 && f.y > 3 && f.x < Size - 4 && f.y < Size - 4 && !dist.ContainsKey(f))
                        {
                            dist[f] = 1;
                            q.Enqueue(f);
                        }
                    }

                while (q.Count > 0)
                {
                    Vector2Int c = q.Dequeue();
                    int dc = dist[c];
                    if (!best.TryGetValue(c, out (int room, int d, int room2, int d2) b))
                        b = (-1, int.MaxValue, -1, int.MaxValue);
                    if (dc < b.d)
                        b = (r, dc, b.room, b.d);
                    else if (dc < b.d2 && r != b.room)
                        b = (b.room, b.d, r, dc);
                    best[c] = b;
                    if (dc >= maxGap)
                        continue;
                    foreach (Vector2Int d in Dirs)
                    {
                        Vector2Int f = c + d;
                        if (OwnerAt(f) == -1 && f.x > 3 && f.y > 3 && f.x < Size - 4 && f.y < Size - 4 && !dist.ContainsKey(f))
                        {
                            dist[f] = dc + 1;
                            q.Enqueue(f);
                        }
                    }
                }
            }

            var touched = new HashSet<int>();
            foreach (KeyValuePair<Vector2Int, (int room, int d, int room2, int d2)> kv in best)
            {
                (int room, int d, int room2, int d2) b = kv.Value;
                if (b.room < 0 || b.room2 < 0 || b.d + b.d2 > maxGap + 1)
                    continue;
                Owner[kv.Key.x, kv.Key.y] = b.room;
                Rooms[b.room].Cells.Add(kv.Key);
                Rooms[b.room].Use[kv.Key] = CellUse.Sol;
                touched.Add(b.room);
            }

            foreach (int r in touched)
                RecomputeBox(Rooms[r]);
        }

        public static void RecomputeBox(ActRoom r)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            Vector2 sum = Vector2.zero;
            foreach (Vector2Int c in r.Cells)
            {
                x0 = Mathf.Min(x0, c.x);
                y0 = Mathf.Min(y0, c.y);
                x1 = Mathf.Max(x1, c.x);
                y1 = Mathf.Max(y1, c.y);
                sum += c;
            }

            r.Box = r.Cells.Count == 0 ? new RectInt() : new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            r.CenterCell = sum / Mathf.Max(1, r.Cells.Count);
        }

        /// <summary>
        /// Seuil équarri (D-092) : les formes organiques n'ont presque jamais 12 m de limite droite commune. On prend le contact le plus
        /// central entre la nouvelle salle (cells) et la salle other, et on taille de part et d'autre un seuil droit (largeur de la porte
        /// + 1 case de chaque côté, 3 cases de profondeur) dans les cases libres ; les cases ajoutées côté other vont dans extraOther.
        /// </summary>
        private bool SquareContact(List<Vector2Int> cells, int other, out Vector2Int first, out Vector2Int dir, out int len, List<Vector2Int> extraOther)
        {
            first = default;
            dir = default;
            len = 0;
            var set = new HashSet<Vector2Int>(cells);
            Vector2 mine = Vector2.zero;
            foreach (Vector2Int c in cells)
                mine += c;
            mine /= Mathf.Max(1, cells.Count);
            Vector2 theirs = Rooms[other].CenterCell;
            var contacts = new List<(Vector2Int o, Vector2Int d)>();
            foreach (Vector2Int c in cells)
                foreach (Vector2Int d in Dirs)
                    if (OwnerAt(c - d) == other)
                        contacts.Add((c - d, d));
            if (contacts.Count == 0)
                return false;
            Vector2 mid = (mine + theirs) * 0.5f;
            int half = (DoorCells + 2) / 2 + 1;
            foreach ((Vector2Int o, Vector2Int d) in contacts.OrderBy(x => ((Vector2)x.o - mid).sqrMagnitude).Take(12))
            {
                Vector2Int along = new Vector2Int(Mathf.Abs(d.y), Mathf.Abs(d.x));
                var addMine = new List<Vector2Int>();
                var addOther = new List<Vector2Int>();
                bool ok = true;
                for (int k = -half; k <= half && ok; k++)
                for (int j = 0; j < 3 && ok; j++)
                {
                    Vector2Int po = o + along * k - d * j, pm = o + d + along * k + d * j;
                    int ow = OwnerAt(po);
                    if (set.Contains(po) || (ow != other && ow != -1) || po.x < 3 || po.y < 3 || po.x >= Size - 3 || po.y >= Size - 3)
                        ok = false;
                    else if (ow == -1)
                        addOther.Add(po);
                    else if (Rooms[other].Use.TryGetValue(po, out CellUse u) && u != CellUse.Sol)
                        ok = false;
                    int om = OwnerAt(pm);
                    if (om != -1 || pm.x < 3 || pm.y < 3 || pm.x >= Size - 3 || pm.y >= Size - 3)
                        ok = false;
                    else if (!set.Contains(pm))
                        addMine.Add(pm);
                }

                if (!ok)
                    continue;
                cells.AddRange(addMine.Distinct());
                extraOther.AddRange(addOther.Distinct());
                first = o + along * (-(DoorCells / 2));
                dir = d;
                len = DoorCells;
                return true;
            }

            return false;
        }

        /// <summary>Boucle entre deux salles déjà posées : seuil équarri en échangeant des cases entre elles (sol libre seulement).</summary>
        private bool SquarePair(int a, int b, Dictionary<(Vector2Int d, int line), List<int>> edges, out Vector2Int first, out Vector2Int dir, out int len)
        {
            first = default;
            dir = default;
            len = 0;
            var contacts = new List<(Vector2Int o, Vector2Int d)>();
            foreach (KeyValuePair<(Vector2Int d, int line), List<int>> kv in edges)
                foreach (int pos in kv.Value)
                    contacts.Add((kv.Key.d.x != 0 ? new Vector2Int(kv.Key.line, pos) : new Vector2Int(pos, kv.Key.line), kv.Key.d));
            Vector2 mid = (Rooms[a].CenterCell + Rooms[b].CenterCell) * 0.5f;
            int half = (DoorCells + 2) / 2 + 1;
            foreach ((Vector2Int o, Vector2Int d) in contacts.OrderBy(x => ((Vector2)x.o - mid).sqrMagnitude).Take(10))
            {
                Vector2Int along = new Vector2Int(Mathf.Abs(d.y), Mathf.Abs(d.x));
                var toA = new List<Vector2Int>();
                var toB = new List<Vector2Int>();
                bool ok = true;
                for (int k = -half; k <= half && ok; k++)
                for (int j = 0; j < 3 && ok; j++)
                {
                    Vector2Int pa = o + along * k - d * j, pb = o + d + along * k + d * j;
                    ok = Swappable(pa, a, b) && Swappable(pb, a, b);
                    if (ok && OwnerAt(pa) != a)
                        toA.Add(pa);
                    if (ok && OwnerAt(pb) != b)
                        toB.Add(pb);
                }

                if (!ok || toA.Count + toB.Count > 48)
                    continue;
                foreach (Vector2Int c in toA)
                    Move(c, b, a);
                foreach (Vector2Int c in toB)
                    Move(c, a, b);
                RecomputeBox(Rooms[a]);
                RecomputeBox(Rooms[b]);
                first = o + along * (-(DoorCells / 2));
                dir = d;
                len = DoorCells;
                return true;
            }

            return false;
        }

        private bool Swappable(Vector2Int c, int a, int b)
        {
            int o = OwnerAt(c);
            if (o != a && o != b)
                return false;
            return Rooms[o].Use.TryGetValue(c, out CellUse u) && u == CellUse.Sol;
        }

        private void Move(Vector2Int c, int from, int to)
        {
            Owner[c.x, c.y] = to;
            Rooms[from].Cells.Remove(c);
            Rooms[from].Use.Remove(c);
            Rooms[to].Cells.Add(c);
            Rooms[to].Use[c] = CellUse.Sol;
        }

        private ActDoor MakeDoor(int a, int b, Vector2Int first, Vector2Int dir, int len) =>
            new ActDoor { A = a, B = b, First = first, Dir = dir, Length = len };

        private void AddDoor(ActDoor d)
        {
            Doors.Add(d);
            Rooms[d.A].Doors.Add(Doors.Count - 1);
            Rooms[d.B].Doors.Add(Doors.Count - 1);
            // Abords : 2 cases de chaque côté, réservées (rien n'y est posé).
            for (int i = 0; i < d.Length; i++)
            for (int k = 0; k < 3; k++)
            {
                Vector2Int ca = d.First + d.Along * i - d.Dir * k, cb = d.First + d.Along * i + d.Dir * (k + 1);
                Reserve(d.A, ca, CellUse.Passage);
                Reserve(d.B, cb, CellUse.Passage);
            }
        }

        private void Reserve(int room, Vector2Int c, CellUse use)
        {
            ActRoom r = Rooms[room];
            if (r.Use.TryGetValue(c, out CellUse u) && u == CellUse.Sol)
                r.Use[c] = use;
        }

        /// <summary>Rampe dans la plus basse des deux salles, devant la porte (largeur de la porte). Faux s'il n'y a pas la place.</summary>
        private bool PlanRamp(ActDoor d)
        {
            float fa = Rooms[d.A].Floor, fb = Rooms[d.B].Floor;
            d.RampCells.Clear();
            if (Mathf.Abs(fa - fb) < 0.05f)
            {
                d.RampRoom = -1;
                return true;
            }

            int low = fa < fb ? d.A : d.B;
            Vector2Int inward = low == d.A ? -d.Dir : d.Dir;
            Vector2Int edge0 = low == d.A ? d.First : d.First + d.Dir;
            float run = Mathf.Abs(fa - fb) / Mathf.Tan(Settings.rampSlope * Mathf.Deg2Rad);
            int deep = Mathf.CeilToInt(run / Cell);
            for (int i = -1; i <= d.Length; i++)
            for (int k = 0; k < deep + 1; k++)
            {
                Vector2Int c = edge0 + d.Along * i + inward * k;
                if (OwnerAt(c) != low)
                    return false;
                if (i >= 0 && i < d.Length && k < deep)
                    d.RampCells.Add(c);
            }

            foreach (Vector2Int c in d.RampCells)
                if (Rooms[low].Use.TryGetValue(c, out CellUse u) && u == CellUse.Rampe)
                    return false;
            d.RampRoom = low;
            d.RampCellsDeep = deep;
            foreach (Vector2Int c in d.RampCells)
                Rooms[low].Use[c] = CellUse.Rampe;
            return true;
        }

        /// <summary>Boucles : portes en plus entre salles voisines non reliées (chance loopChance), si la rampe a la place.</summary>
        private void AddLoops()
        {
            var linked = new HashSet<(int, int)>();
            foreach (ActDoor d in Doors)
                linked.Add((Mathf.Min(d.A, d.B), Mathf.Max(d.A, d.B)));
            var contacts = new Dictionary<(int a, int b), Dictionary<(Vector2Int d, int line), List<int>>>();
            for (int x = 0; x < Size - 1; x++)
            for (int y = 0; y < Size - 1; y++)
            {
                int a = Owner[x, y];
                if (a < 0 || Rooms[a].Special)
                    continue;
                foreach (Vector2Int d in new[] { Vector2Int.right, Vector2Int.up })
                {
                    int b = Owner[x + d.x, y + d.y];
                    if (b < 0 || b == a || Rooms[b].Special)
                        continue;
                    int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
                    if (linked.Contains((lo, hi)))
                        continue;
                    // Orientée de lo vers hi.
                    Vector2Int dd = a == lo ? d : -d;
                    Vector2Int o = a == lo ? new Vector2Int(x, y) : new Vector2Int(x, y) + d;
                    int line = dd.x != 0 ? o.x : o.y, pos = dd.x != 0 ? o.y : o.x;
                    if (!contacts.TryGetValue((lo, hi), out var map))
                        contacts[(lo, hi)] = map = new Dictionary<(Vector2Int, int), List<int>>();
                    if (!map.TryGetValue((dd, line), out List<int> l))
                        map[(dd, line)] = l = new List<int>();
                    l.Add(pos);
                }
            }

            foreach (KeyValuePair<(int a, int b), Dictionary<(Vector2Int d, int line), List<int>>> kv in contacts.OrderBy(k => k.Key.a).ThenBy(k => k.Key.b))
            {
                if (_rng.NextDouble() >= Settings.loopChance)
                    continue;
                if (!PickRun(kv.Value, DoorCells + 2, out Vector2Int first, out Vector2Int dir, out int len)
                    && !SquarePair(kv.Key.a, kv.Key.b, kv.Value, out first, out dir, out len))
                    continue;
                ActDoor door = MakeDoor(kv.Key.a, kv.Key.b, first, dir, len);
                door.Loop = true;
                if (!PlanRamp(door))
                    continue;
                AddDoor(door);
            }
        }

        public int[] Depths(int from)
        {
            var depth = Enumerable.Repeat(int.MaxValue, Rooms.Count).ToArray();
            var q = new Queue<int>();
            depth[from] = 0;
            q.Enqueue(from);
            while (q.Count > 0)
            {
                int r = q.Dequeue();
                foreach (int di in Rooms[r].Doors)
                {
                    int o = Doors[di].Other(r);
                    if (depth[o] != int.MaxValue)
                        continue;
                    depth[o] = depth[r] + 1;
                    q.Enqueue(o);
                }
            }

            for (int i = 0; i < depth.Length; i++)
                if (depth[i] == int.MaxValue)
                    depth[i] = -1;
            return depth;
        }

        // ---------- Fonctions des salles ----------

        /// <summary>Fonctions par thème : nom, creux préféré (eau, vide), terrasse préférée, surface minimale (m²).</summary>
        public static readonly Dictionary<ActTheme, (string name, RoomFeature feature, bool terrace, float minArea)[]> Functions =
            new Dictionary<ActTheme, (string, RoomFeature, bool, float)[]>
            {
                [ActTheme.Caves] = new[]
                {
                    ("Cachot", RoomFeature.Aucun, true, 0f), ("Salle des gardes", RoomFeature.Aucun, true, 0f),
                    ("Réserve", RoomFeature.Aucun, true, 0f), ("Ossuaire", RoomFeature.Vide, false, 900f),
                    ("Citerne", RoomFeature.Eau, true, 900f), ("Gouffre", RoomFeature.Vide, true, 1100f),
                    ("Chapelle souterraine", RoomFeature.Aucun, false, 0f), ("Galerie de mine", RoomFeature.Aucun, true, 0f),
                    ("Grotte inondée", RoomFeature.Eau, false, 1000f), ("Salle de torture", RoomFeature.Aucun, true, 0f),
                    ("Cave à vin", RoomFeature.Aucun, true, 0f), ("Crypte", RoomFeature.Aucun, false, 0f),
                },
                [ActTheme.Chateau] = new[]
                {
                    ("Salle de bal", RoomFeature.Aucun, true, 1400f), ("Bibliothèque", RoomFeature.Aucun, true, 0f),
                    ("Chapelle", RoomFeature.Aucun, false, 0f), ("Salle à manger", RoomFeature.Aucun, true, 0f),
                    ("Galerie des portraits", RoomFeature.Aucun, true, 0f), ("Chambre", RoomFeature.Aucun, false, 0f),
                    ("Armurerie", RoomFeature.Aucun, true, 0f), ("Salle du trône", RoomFeature.Aucun, true, 1200f),
                    ("Bains", RoomFeature.Eau, false, 900f), ("Tour effondrée", RoomFeature.Vide, true, 1000f),
                    ("Grand escalier", RoomFeature.Aucun, true, 0f), ("Salon de musique", RoomFeature.Aucun, false, 0f),
                },
                [ActTheme.Jardin] = new[]
                {
                    ("Roseraie", RoomFeature.Aucun, true, 0f), ("Grand bassin", RoomFeature.Eau, false, 1000f),
                    ("Labyrinthe de haies", RoomFeature.Aucun, true, 0f), ("Verger", RoomFeature.Aucun, false, 0f),
                    ("Cimetière", RoomFeature.Aucun, true, 0f), ("Serre en ruine", RoomFeature.Aucun, true, 0f),
                    ("Allée des statues", RoomFeature.Aucun, false, 0f), ("Kiosque", RoomFeature.Eau, false, 800f),
                    ("Ravin", RoomFeature.Vide, true, 1100f), ("Potager", RoomFeature.Aucun, false, 0f),
                    ("Bosquet", RoomFeature.Aucun, true, 0f), ("Fontaine", RoomFeature.Eau, true, 0f),
                },
                [ActTheme.Boss] = new[] { ("Arène", RoomFeature.Aucun, false, 0f) },
            };

        private void AssignFunctions(int[] depth)
        {
            (string name, RoomFeature feature, bool terrace, float minArea)[] list = Functions[Settings.theme];
            var bag = new List<int>();
            foreach (ActRoom r in Rooms)
            {
                switch (r.Role)
                {
                    case RoomRole.Depart:
                        r.Function = Settings.theme switch { ActTheme.Chateau => "Palier", ActTheme.Jardin => "Terrasse du château", ActTheme.Boss => "Arène", _ => "Salle du puits" };
                        r.Name = r.Function;
                        continue;
                    case RoomRole.HallDeSortie:
                        r.Name = r.Function = "Hall de sortie";
                        continue;
                    case RoomRole.Repaire:
                        r.Name = r.Function = "Repaire du traqueur";
                        continue;
                    case RoomRole.Arrivee:
                        r.Name = r.Function = "Porte d'arrivée";
                        continue;
                    case RoomRole.Verrouillee:
                        r.Name = r.Function = "Salle verrouillée";
                        continue;
                }

                float area = r.Area(Cell);
                if (bag.Count == 0)
                    bag.AddRange(Enumerable.Range(0, list.Length).OrderBy(_ => _rng.Next()));
                var neighbors = new HashSet<string>(r.Doors.Select(di => Rooms[Doors[di].Other(r.Id)].Function).Where(f => f != null));
                int pick = bag.FindIndex(i => list[i].minArea <= area && !neighbors.Contains(list[i].name));
                if (pick < 0)
                {
                    // Sac vide de candidats : on le remplit à nouveau (sans la fonction des voisines).
                    bag.AddRange(Enumerable.Range(0, list.Length).OrderBy(_ => _rng.Next()));
                    pick = bag.FindIndex(i => list[i].minArea <= area && !neighbors.Contains(list[i].name));
                }

                if (pick < 0)
                    pick = bag.FindIndex(i => !neighbors.Contains(list[i].name));
                if (pick < 0)
                    pick = 0;
                int fi = bag[pick];
                bag.RemoveAt(pick);
                (string name, RoomFeature feature, bool terrace, float minArea) f = list[fi];
                r.Function = f.name;
                r.Name = f.name;
                bool featureOk = r.Role != RoomRole.Sortie;
                if (featureOk && f.feature == RoomFeature.Eau && _rng.NextDouble() < Mathf.Max(0.7f, Settings.waterShare))
                    r.Feature = RoomFeature.Eau;
                else if (featureOk && f.feature == RoomFeature.Vide && _rng.NextDouble() < Mathf.Max(0.7f, Settings.voidShare))
                    r.Feature = RoomFeature.Vide;
                else if (featureOk && f.feature == RoomFeature.Aucun && area > 1100f)
                {
                    double roll = _rng.NextDouble();
                    if (roll < Settings.waterShare * 0.4f)
                        r.Feature = RoomFeature.Eau;
                    else if (roll < (Settings.waterShare + Settings.voidShare) * 0.4f)
                        r.Feature = RoomFeature.Vide;
                }

                if ((f.terrace || _rng.NextDouble() < 0.3f) && _rng.NextDouble() < Settings.terraceShare + (f.terrace ? 0.25f : 0f))
                    r.TerraceHeight = Mathf.Lerp(Settings.terraceHeight.x, Settings.terraceHeight.y, (float)_rng.NextDouble());
            }

            // Noms uniques (« Cachot », « Cachot II »…).
            var seen = new Dictionary<string, int>();
            foreach (ActRoom r in Rooms)
            {
                seen.TryGetValue(r.Name, out int n);
                seen[r.Name] = n + 1;
                if (n > 0)
                    r.Name = $"{r.Name} {Roman(n + 1)}";
            }
        }

        private static string Roman(int n) => n switch { 2 => "II", 3 => "III", 4 => "IV", 5 => "V", 6 => "VI", _ => n.ToString() };

        // ---------- Intérieur : chemins, creux, ponts, îlots, terrasse ----------

        /// <summary>Distance (cases, 4-voisinage) de chaque case de la salle à son bord.</summary>
        public Dictionary<Vector2Int, int> EdgeDistance(ActRoom r)
        {
            var dist = new Dictionary<Vector2Int, int>();
            var q = new Queue<Vector2Int>();
            foreach (Vector2Int c in r.Cells)
            {
                bool edge = false;
                foreach (Vector2Int d in Dirs)
                    edge |= OwnerAt(c + d) != r.Id;
                if (edge)
                {
                    dist[c] = 0;
                    q.Enqueue(c);
                }
            }

            while (q.Count > 0)
            {
                Vector2Int c = q.Dequeue();
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int n = c + d;
                    if (OwnerAt(n) != r.Id || dist.ContainsKey(n))
                        continue;
                    dist[n] = dist[c] + 1;
                    q.Enqueue(n);
                }
            }

            return dist;
        }

        private void PlanInterior(ActRoom r)
        {
            if (r.Special)
                return;
            Dictionary<Vector2Int, int> edge = EdgeDistance(r);
            // Chemins entre les portes (largeur 2 cases) : réservés.
            var doorPoints = r.Doors.Select(di => DoorCellInside(Doors[di], r.Id)).ToList();
            for (int i = 0; i < doorPoints.Count; i++)
            for (int j = i + 1; j < doorPoints.Count; j++)
                MarkLine(r, doorPoints[i], doorPoints[j], r.Feature != RoomFeature.Aucun ? CellUse.Pont : CellUse.Passage, 1);
            if (r.Role == RoomRole.Depart || r.Role == RoomRole.Sortie)
                MarkDisk(r, Vector2Int.RoundToInt(r.CenterCell), 4, CellUse.Passage);

            // Creux : cases loin du bord (≥ 3 cases) dans une ellipse autour du centre, sauf chemins et rampes.
            if (r.Feature != RoomFeature.Aucun)
            {
                Vector2 c = r.CenterCell;
                float rx = r.Box.width * 0.36f, ry = r.Box.height * 0.36f;
                int hollow = 0;
                foreach (Vector2Int cell in r.Cells)
                {
                    if (!edge.TryGetValue(cell, out int e) || e < 5 || r.Use[cell] != CellUse.Sol)
                        continue;
                    float u = (cell.x - c.x) / Mathf.Max(1f, rx), v = (cell.y - c.y) / Mathf.Max(1f, ry);
                    if (u * u + v * v <= 1f)
                    {
                        r.Use[cell] = CellUse.Creux;
                        hollow++;
                    }
                }

                if (hollow < 12)
                {
                    foreach (Vector2Int cell in r.Cells.Where(x => r.Use[x] == CellUse.Creux).ToList())
                        r.Use[cell] = CellUse.Sol;
                    r.Feature = RoomFeature.Aucun;
                }
                else
                {
                    // Ponts (chemins posés avant) : là où un chemin traverse le creux. Îlots : route rapide (2 × 2, écart ≤ 3 cases).
                    foreach (Vector2Int cell in r.Cells.Where(x => r.Use[x] == CellUse.Pont).ToList())
                        if (!Touches(r, cell, CellUse.Creux))
                            r.Use[cell] = CellUse.Passage;
                    var hollowCells = r.Cells.Where(x => r.Use[x] == CellUse.Creux).ToList();
                    int islands = Mathf.Clamp(hollowCells.Count / 30, 1, 5);
                    for (int k = 0; k < islands * 4 && islands > 0; k++)
                    {
                        Vector2Int at = hollowCells[_rng.Next(hollowCells.Count)];
                        bool fits = true;
                        for (int dx = -1; dx <= 2 && fits; dx++)
                        for (int dy = -1; dy <= 2 && fits; dy++)
                        {
                            Vector2Int q = at + new Vector2Int(dx, dy);
                            fits = r.Use.TryGetValue(q, out CellUse u) && u == CellUse.Creux;
                        }

                        if (!fits)
                            continue;
                        for (int dx = 0; dx <= 1; dx++)
                        for (int dy = 0; dy <= 1; dy++)
                            r.Use[at + new Vector2Int(dx, dy)] = CellUse.Ilot;
                        islands--;
                    }
                }
            }

            // Terrasse : bande de 3 à 4 cases le long du bord, loin des portes, d'un seul tenant.
            if (r.TerraceHeight > 0f)
            {
                int depth = 3 + _rng.Next(2);
                var candidates = r.Cells.Where(x => edge.TryGetValue(x, out int e) && e < depth && r.Use[x] == CellUse.Sol).ToList();
                if (candidates.Count < 20)
                    r.TerraceHeight = 0f;
                else
                {
                    // Graine au hasard, croissance jusqu'à ~35 cases dans la bande.
                    Vector2Int seed = candidates[_rng.Next(candidates.Count)];
                    var set = new HashSet<Vector2Int>(candidates);
                    var grown = new HashSet<Vector2Int> { seed };
                    var q = new Queue<Vector2Int>();
                    q.Enqueue(seed);
                    int want = Mathf.Clamp(r.Cells.Count / 6, 24, 60);
                    while (q.Count > 0 && grown.Count < want)
                    {
                        Vector2Int c = q.Dequeue();
                        foreach (Vector2Int d in Dirs)
                        {
                            Vector2Int n = c + d;
                            if (set.Contains(n) && grown.Add(n))
                                q.Enqueue(n);
                        }
                    }

                    if (grown.Count < 16)
                        r.TerraceHeight = 0f;
                    else
                        foreach (Vector2Int c in grown)
                            r.Use[c] = CellUse.Terrasse;
                }
            }
        }

        private bool Touches(ActRoom r, Vector2Int c, CellUse use)
        {
            foreach (Vector2Int d in Dirs)
                if (r.Use.TryGetValue(c + d, out CellUse u) && u == use)
                    return true;
            return false;
        }

        public Vector2Int DoorCellInside(ActDoor d, int room)
        {
            Vector2Int mid = d.First + d.Along * (d.Length / 2);
            Vector2Int inward = room == d.A ? -d.Dir : d.Dir;
            Vector2Int c = room == d.A ? mid : mid + d.Dir;
            if (d.RampRoom == room)
                c += inward * d.RampCellsDeep;
            return c;
        }

        private void MarkLine(ActRoom r, Vector2Int a, Vector2Int b, CellUse use, int half)
        {
            int n = Mathf.Max(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)) * 2 + 1;
            for (int i = 0; i <= n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                var c = Vector2Int.RoundToInt(p);
                for (int dx = -half; dx <= half; dx++)
                for (int dy = -half; dy <= half; dy++)
                {
                    Vector2Int q = c + new Vector2Int(dx, dy);
                    if (r.Use.TryGetValue(q, out CellUse u) && (u == CellUse.Sol || u == CellUse.Passage))
                        r.Use[q] = use;
                }
            }
        }

        private void MarkDisk(ActRoom r, Vector2Int c, int rad, CellUse use)
        {
            for (int dx = -rad; dx <= rad; dx++)
            for (int dy = -rad; dy <= rad; dy++)
            {
                Vector2Int q = c + new Vector2Int(dx, dy);
                if (dx * dx + dy * dy <= rad * rad && r.Use.TryGetValue(q, out CellUse u) && u == CellUse.Sol)
                    r.Use[q] = use;
            }
        }
    }
}
