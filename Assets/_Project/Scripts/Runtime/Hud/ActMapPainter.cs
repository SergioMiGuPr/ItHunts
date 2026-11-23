using System;
using System.Collections.Generic;
using ItHunts.Level.Acts;
using UnityEngine;

namespace ItHunts.Hud
{
    /// <summary>
    /// Peintre « encre » de la carte d'un acte (D-101, nuit 4, point 6) : à partir du plan seul (ActPlan, calcul pur testé en EditMode),
    /// peint un tableau de pixels, nord en haut (une case plus au nord est plus haute dans la texture), pixels isotropes (emprise
    /// carrée) :
    /// - sol des salles découvertes en teinte sombre (un peu plus claire par étage de sol) ;
    /// - contours de murs (ActContour, biseaux compris) en traits d'encre nets, épaisseur d'une demi-case (1 m) ;
    /// - portes = ouvertures dans le trait ; barre orange en travers : porte verrouillée ; grise : herse ; rouge : sortie ;
    /// - terrasses hachurées plus claires, bassins teinte eau, fosses très sombres, ponts et îlots plus clairs ;
    /// - rampes en dégradé (clair au sommet, côté porte ; sombre en bas).
    /// La même texture nourrit la bulle ronde du HUD et la carte ouverte ; son alpha (0 hors des salles découvertes) pilote aussi la
    /// brume de la carte ouverte (shader UICarteBrume). La brume par salle vient de MapDiscovery (roomKnown).
    /// </summary>
    public static class ActMapPainter
    {
        /// <summary>Encre de la DA (#CDBEA5) : murs et barres des portes.</summary>
        public static readonly Color32 Ink = new Color32(205, 190, 165, 255);
        public static readonly Color32 FloorLow = new Color32(40, 38, 48, 232);
        public static readonly Color32 FloorHigh = new Color32(74, 70, 86, 232);
        public static readonly Color32 Terrace = new Color32(104, 98, 114, 238);
        public static readonly Color32 TerraceStripe = new Color32(136, 128, 142, 238);
        public static readonly Color32 Water = new Color32(32, 70, 110, 238);
        public static readonly Color32 Void = new Color32(8, 6, 10, 246);
        public static readonly Color32 Bridge = new Color32(124, 104, 80, 238);
        public static readonly Color32 Island = new Color32(92, 84, 76, 238);
        public static readonly Color32 Threshold = new Color32(76, 72, 84, 236);
        public static readonly Color32 LockedBar = new Color32(255, 128, 38, 255);
        public static readonly Color32 GateBar = new Color32(150, 150, 160, 255);
        public static readonly Color32 ExitBar = new Color32(220, 50, 50, 255);

        /// <summary>Épaisseur du trait de mur (cases) : une demi-case, soit 1 m pour des cases de 2 m.</summary>
        public const float WallCells = 0.5f;
        /// <summary>Marge autour des salles (cases) dans l'emprise de la carte.</summary>
        public const float MarginCells = 3f;
        /// <summary>Résolution visée (px par mètre) et bornes de la texture (côté, puissance de 2).</summary>
        public const float PixelsPerMeter = 4f;
        public const int MinTexture = 512, MaxTexture = 2048;

        /// <summary>
        /// Emprise (m, axes x et z) des salles du plan plus la marge, rendue carrée autour de son centre : les pixels ont la même taille
        /// en x et en z (traits d'épaisseur égale dans les deux sens).
        /// </summary>
        public static Bounds BoundsOf(ActPlan plan)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (ActRoom r in plan.Rooms)
            foreach (Vector2Int c in r.Cells)
            {
                x0 = Mathf.Min(x0, c.x);
                y0 = Mathf.Min(y0, c.y);
                x1 = Mathf.Max(x1, c.x);
                y1 = Mathf.Max(y1, c.y);
            }

            if (x1 < x0)
                return new Bounds(plan.Origin + new Vector3(plan.Size * plan.Cell * 0.5f, 0f, plan.Size * plan.Cell * 0.5f), new Vector3(plan.Size * plan.Cell, 1f, plan.Size * plan.Cell));
            float cell = plan.Cell;
            Vector3 min = plan.Origin + new Vector3((x0 - MarginCells) * cell, 0f, (y0 - MarginCells) * cell);
            Vector3 max = plan.Origin + new Vector3((x1 + 1 + MarginCells) * cell, 1f, (y1 + 1 + MarginCells) * cell);
            var b = new Bounds((min + max) * 0.5f, max - min);
            float side = Mathf.Max(b.size.x, b.size.z);
            b.size = new Vector3(side, 1f, side);
            return b;
        }

        /// <summary>Côté de la texture (puissance de 2 entre MinTexture et MaxTexture) pour au moins pxPerMeter pixels par mètre.</summary>
        public static int TextureSize(Bounds all, float pxPerMeter = PixelsPerMeter)
        {
            float side = Mathf.Max(all.size.x, all.size.z, 1f) * pxPerMeter;
            int size = MinTexture;
            while (size < side && size < MaxTexture)
                size *= 2;
            return size;
        }

        /// <summary>Pixel (colonne, ligne depuis le bas) d'un point du monde sur une texture carrée size² qui couvre all.</summary>
        public static Vector2Int PixelOf(Bounds all, int size, Vector3 world) =>
            new Vector2Int(Mathf.FloorToInt((world.x - all.min.x) / Mathf.Max(1e-3f, all.size.x) * size),
                Mathf.FloorToInt((world.z - all.min.z) / Mathf.Max(1e-3f, all.size.z) * size));

        /// <summary>
        /// Peint la carte : les salles dont roomKnown(id) est vrai, leurs murs, et les portes dont au moins une salle est connue.
        /// Hors des salles découvertes : transparent (alpha 0).
        /// </summary>
        public static Color32[] Paint(ActPlan plan, Bounds all, int size, Func<int, bool> roomKnown)
        {
            var px = new Color32[size * size];
            Paint(px, plan, all, size, roomKnown);
            return px;
        }

        /// <summary>Même chose dans un tableau fourni (size² pixels), remis à zéro d'abord.</summary>
        public static void Paint(Color32[] px, ActPlan plan, Bounds all, int size, Func<int, bool> roomKnown)
        {
            Array.Clear(px, 0, px.Length);
            if (plan == null || plan.Rooms.Count == 0)
                return;
            float sx = size / Mathf.Max(1e-3f, all.size.x), sz = size / Mathf.Max(1e-3f, all.size.z);
            float cell = plan.Cell;
            float minFloor = float.MaxValue, maxFloor = float.MinValue;
            foreach (ActRoom r in plan.Rooms)
            {
                minFloor = Mathf.Min(minFloor, r.Floor);
                maxFloor = Mathf.Max(maxFloor, r.Floor);
            }

            Color32 FloorTint(ActRoom r) => Color32.Lerp(FloorLow, FloorHigh, maxFloor - minFloor < 0.05f ? 0.35f : Mathf.InverseLerp(minFloor, maxFloor, r.Floor));

            // Pixels d'une case (x, y) : [x, x+1] × [y, y+1] en cases.
            void CellRect(Vector2Int c, out int x0, out int y0, out int x1, out int y1)
            {
                float wx0 = plan.Origin.x + c.x * cell - all.min.x, wz0 = plan.Origin.z + c.y * cell - all.min.z;
                x0 = Mathf.Clamp(Mathf.FloorToInt(wx0 * sx), 0, size - 1);
                y0 = Mathf.Clamp(Mathf.FloorToInt(wz0 * sz), 0, size - 1);
                x1 = Mathf.Clamp(Mathf.CeilToInt((wx0 + cell) * sx) - 1, 0, size - 1);
                y1 = Mathf.Clamp(Mathf.CeilToInt((wz0 + cell) * sz) - 1, 0, size - 1);
            }

            int stripe = Mathf.Max(2, Mathf.RoundToInt(sx * cell * 0.25f));          // hachure des terrasses : bandes diagonales d'un quart de case

            // 1. Sols.
            foreach (ActRoom r in plan.Rooms)
            {
                if (!roomKnown(r.Id))
                    continue;
                Color32 floor = FloorTint(r);
                foreach (Vector2Int c in r.Cells)
                {
                    if (!r.Use.TryGetValue(c, out CellUse use))
                        use = CellUse.Sol;
                    Color32 col = use switch
                    {
                        CellUse.Creux => r.Feature == RoomFeature.Eau ? Water : Void,
                        CellUse.Pont => Bridge,
                        CellUse.Ilot => Island,
                        CellUse.Terrasse => Terrace,
                        _ => floor,
                    };
                    CellRect(c, out int x0, out int y0, out int x1, out int y1);
                    bool hatch = use == CellUse.Terrasse;
                    for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        px[y * size + x] = hatch && (x + y) / stripe % 2 == 0 ? TerraceStripe : col;
                }

                // Rampes : dégradé du sommet (côté porte, sol de la salle haute) vers le bas (sol de cette salle).
                foreach (int di in r.Doors)
                {
                    if (di < 0 || di >= plan.Doors.Count)
                        continue;
                    ActDoor d = plan.Doors[di];
                    if (d.RampRoom != r.Id || d.RampCells.Count == 0)
                        continue;
                    Vector2Int inward = r.Id == d.A ? -d.Dir : d.Dir;
                    Vector2Int edge0 = r.Id == d.A ? d.First : d.First + d.Dir;
                    int deep = Mathf.Max(1, d.RampCellsDeep);
                    foreach (Vector2Int c in d.RampCells)
                    {
                        int k = (c.x - edge0.x) * inward.x + (c.y - edge0.y) * inward.y;
                        CellRect(c, out int x0, out int y0, out int x1, out int y1);
                        for (int y = y0; y <= y1; y++)
                        for (int x = x0; x <= x1; x++)
                        {
                            // Position continue dans la rampe (0 au sommet, 1 en bas), par pixel : dégradé lisse.
                            float f = inward.x != 0 ? (x + 0.5f - x0) / Mathf.Max(1f, x1 - x0 + 1) : (y + 0.5f - y0) / Mathf.Max(1f, y1 - y0 + 1);
                            if (inward.x < 0 || inward.y < 0)
                                f = 1f - f;
                            float t = Mathf.Clamp01((k + f) / deep);
                            px[y * size + x] = Color32.Lerp(Terrace, floor, t);
                        }
                    }
                }
            }

            // 2. Murs : contour de chaque salle connue (un mur entre deux salles connues est tracé deux fois, sans effet).
            float half = WallCells * cell * 0.5f * sx;
            foreach (ActRoom r in plan.Rooms)
            {
                if (!roomKnown(r.Id) || r.Cells.Count == 0)
                    continue;
                foreach (ActContour.Segment s in ActContour.Of(plan, r))
                    Line(px, size, ToPixel(s.A), ToPixel(s.B), half, Ink);
            }

            Vector2 ToPixel(Vector2 u) => new Vector2((plan.Origin.x + u.x * cell - all.min.x) * sx, (plan.Origin.z + u.y * cell - all.min.z) * sz);

            // 3. Portes : l'ouverture efface le trait (seuil), une barre en travers signale verrou, herse ou sortie.
            foreach (ActDoor d in plan.Doors)
            {
                if (d.A < 0 || d.B < 0 || d.A >= plan.Rooms.Count || d.B >= plan.Rooms.Count)
                    continue;
                if (!roomKnown(d.A) && !roomKnown(d.B))
                    continue;
                DoorRect(d, WallCells * 0.5f + 1.5f / (sx * cell), out Vector2 a, out Vector2 b);
                FillRect(px, size, ToPixel(a), ToPixel(b), Threshold);
                Color32? bar = d.Locked ? LockedBar : d.Gate ? GateBar : d.Exit ? ExitBar : (Color32?)null;
                if (bar.HasValue)
                {
                    DoorRect(d, WallCells * 0.2f, out a, out b);
                    FillRect(px, size, ToPixel(a), ToPixel(b), bar.Value);
                }
            }
        }

        /// <summary>Rectangle (cases) de l'ouverture d'une porte : le long de la limite commune, sur toute sa longueur, ±acrossHalf en travers.</summary>
        public static void DoorRect(ActDoor d, float acrossHalf, out Vector2 min, out Vector2 max)
        {
            if (d.Dir.x != 0)
            {
                float bx = d.First.x + (d.Dir.x > 0 ? 1f : 0f);
                min = new Vector2(bx - acrossHalf, d.First.y);
                max = new Vector2(bx + acrossHalf, d.First.y + d.Length);
            }
            else
            {
                float by = d.First.y + (d.Dir.y > 0 ? 1f : 0f);
                min = new Vector2(d.First.x, by - acrossHalf);
                max = new Vector2(d.First.x + d.Length, by + acrossHalf);
            }
        }

        /// <summary>Centre de l'ouverture d'une porte (cases) : sur la limite commune, au milieu de la longueur.</summary>
        public static Vector2 DoorMiddle(ActDoor d)
        {
            DoorRect(d, 0f, out Vector2 a, out Vector2 b);
            return (a + b) * 0.5f;
        }

        private static void FillRect(Color32[] px, int size, Vector2 a, Vector2 b, Color32 col)
        {
            int x0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(a.x, b.x)), 0, size - 1), x1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(a.x, b.x)) - 1, 0, size - 1);
            int y0 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(a.y, b.y)), 0, size - 1), y1 = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(a.y, b.y)) - 1, 0, size - 1);
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                px[y * size + x] = col;
        }

        /// <summary>Trait épais anticrénelé (pixels) de a à b, demi-épaisseur half.</summary>
        public static void Line(Color32[] px, int size, Vector2 a, Vector2 b, float half, Color32 col)
        {
            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, b.x) - half - 1f), 0, size - 1), x1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, b.x) + half + 1f), 0, size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, b.y) - half - 1f), 0, size - 1), y1 = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, b.y) + half + 1f), 0, size - 1);
            Vector2 ab = b - a;
            float len2 = ab.sqrMagnitude;
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float t = len2 > 1e-6f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2) : 0f;
                float d = Vector2.Distance(p, a + ab * t);
                float cov = Mathf.Clamp01(half + 0.5f - d);
                if (cov <= 0f)
                    continue;
                Blend(ref px[y * size + x], col, cov);
            }
        }

        private static void Blend(ref Color32 dst, Color32 src, float cov)
        {
            if (cov >= 0.999f)
            {
                dst = src;
                return;
            }

            float da = dst.a / 255f * (1f - cov);
            float outA = cov + da;
            if (outA <= 1e-4f)
                return;
            dst.r = (byte)Mathf.RoundToInt((src.r * cov + dst.r * da) / outA);
            dst.g = (byte)Mathf.RoundToInt((src.g * cov + dst.g * da) / outA);
            dst.b = (byte)Mathf.RoundToInt((src.b * cov + dst.b * da) / outA);
            dst.a = (byte)Mathf.RoundToInt(Mathf.Clamp01(outA) * 255f);
        }
    }
}
