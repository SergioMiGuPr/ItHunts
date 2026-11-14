using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Orbs;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Passages en hauteur et galeries de parcours (étape 8, après playtest, points 3 et 4).
    /// <para><b>Passage</b> : à côté de l'entrée d'un couloir dans une salle, une ouverture de 3 × 3 m à 4.5 m du sol dans le mur de
    /// la salle mène à un conduit de 8 m qui débouche, à la même hauteur, dans le mur du couloir. On y monte en saut mural (mur
    /// non magnétique autour) ; trop petit pour le traqueur, qui fait le tour ; sa main l'atteint.</para>
    /// <para><b>Galerie de parcours</b> : parallèle à un couloir, derrière son mur (20 m de large), sans sol (vide : retour au
    /// palier, dégâts) ; portes de 3 m aux deux bouts, paliers et poutres à portée de sa main, puis des plateformes qui ne
    /// tiennent qu'un instant et des panneaux de course hors de sa portée. Toutes les surfaces sont non magnétiques. C'est le seul
    /// endroit où l'on peut le dépasser.</para>
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        private struct PassageDef
        {
            public int Edge, Node, Side;
            public bool AtStart;
        }

        private struct GalleryDef
        {
            public int Edge, Side;
        }

        private static readonly List<PassageDef> Passages = new List<PassageDef>();
        private static readonly List<GalleryDef> Galleries = new List<GalleryDef>();
        private const float DuctY = 4.5f, DuctH = 3f, DuctW = 3.5f, DuctLen = 8f, GalleryWidth = 20f;

        /// <summary>Repère d'un bout de couloir : point de la face intérieure du mur de la salle, direction vers le couloir.</summary>
        private static void CorridorFrame(int e, bool atStart, out Vector3 face, out Vector3 axis, out Vector3 lat, out bool alongX, out float floorY, out float len)
        {
            CorridorEnds(e, out Vector3 s, out Vector3 en, out float ya, out float yb, out Vector3 dir, out alongX);
            face = atStart ? s : en;
            axis = atStart ? dir : -dir;
            lat = alongX ? Vector3.forward : Vector3.right;
            floorY = atStart ? ya : yb;
            len = Vector3.Distance(s, en);
        }

        private static float AxisCoord(Vector3 p, bool alongX) => alongX ? p.x : p.z;

        /// <summary>Plan : galeries (couloirs plats de 55 m et plus) puis passages (côtés libres).</summary>
        private static void PlanParkour(int galleries, int passages)
        {
            Passages.Clear();
            Galleries.Clear();
            var flat = new List<int>();
            for (int e = 0; e < Edges.Count; e++)
                if (!IsLairEdge(e) && !IsSlope(e) && Nodes[Edges[e].x].Hall && Nodes[Edges[e].y].Hall)
                    flat.Add(e);
            Shuffle(flat);
            foreach (int e in flat)
            {
                if (Galleries.Count >= galleries)
                    break;
                CorridorFrame(e, true, out Vector3 face, out _, out _, out bool ax, out float y, out float len);
                if (len < 55f)
                    continue;
                int side = R01() < 0.5f ? 1 : -1;
                if (CorridorSideUsed.ContainsKey((e, side)))
                    side = -side;
                if (CorridorSideUsed.ContainsKey((e, side)))
                    continue;
                CorridorSideUsed[(e, side)] = true;
                Galleries.Add(new GalleryDef { Edge = e, Side = side });
                float w = Width(e);
                foreach (float d in new[] { 6f, len - 9f })
                {
                    CorridorFrame(e, true, out Vector3 f2, out Vector3 axis2, out _, out _, out _, out _);
                    float p0 = AxisCoord(f2 + axis2 * d, ax), p1 = AxisCoord(f2 + axis2 * (d + 3f), ax);
                    AddCorridorOpening(e, side, new Opening(Mathf.Min(p0, p1), Mathf.Max(p0, p1), y, y + 3.5f));
                }
            }

            Shuffle(flat);
            foreach (int e in flat)
            {
                if (Passages.Count >= passages)
                    break;
                bool atStart = R01() < 0.5f;
                int side = R01() < 0.5f ? 1 : -1;
                if (CorridorSideUsed.ContainsKey((e, side)))
                    side = -side;
                if (CorridorSideUsed.ContainsKey((e, side)))
                    continue;
                CorridorSideUsed[(e, side)] = true;
                int node = atStart ? Edges[e].x : Edges[e].y;
                Passages.Add(new PassageDef { Edge = e, Node = node, Side = side, AtStart = atStart });
                CorridorFrame(e, atStart, out Vector3 face, out Vector3 axis, out Vector3 lat, out bool ax, out float y, out _);
                float w = Width(e), c0 = Vector3.Dot(face, lat);
                float lo = c0 + side * (w / 2f + T), hi = c0 + side * (w / 2f + T + DuctW);
                // Mur de la salle (côté qui fait face au couloir) : ouverture au-dessus du sol, devant le conduit.
                Node n = Nodes[node];
                int wall = SideOf(axis);
                AddOpening(node, wall, new Opening(Mathf.Min(lo, hi) + 0.25f, Mathf.Max(lo, hi) - 0.25f, y + DuctY, y + DuctY + DuctH));
                // Mur du couloir : ouverture vers le conduit, près de son bout.
                float p0 = AxisCoord(face + axis * (T + 4f), ax), p1 = AxisCoord(face + axis * (T + 7f), ax);
                AddCorridorOpening(e, side, new Opening(Mathf.Min(p0, p1), Mathf.Max(p0, p1), y + DuctY, y + DuctY + DuctH));
            }
        }

        private static void AddCorridorOpening(int e, int side, Opening o)
        {
            if (!CorridorOpenings.TryGetValue((e, side), out List<Opening> list))
                CorridorOpenings[(e, side)] = list = new List<Opening>();
            list.Add(o);
        }

        private static GameObject NMPiece(string name, Transform parent, Vector3 c, Vector3 size, bool walkable = true)
        {
            GameObject go = Box(name, parent, c, size, NonMagneticMat());
            go.AddComponent<NonMagneticSurface>();
            if (!walkable)
                go.AddComponent<NotWalkable>();
            return go;
        }

        /// <summary>Conduits des passages en hauteur (après les murs).</summary>
        private static void BuildPassages()
        {
            var root = new GameObject("Passages en hauteur (saut mural)").transform;
            root.SetParent(_level, false);
            foreach (PassageDef p in Passages)
            {
                CorridorFrame(p.Edge, p.AtStart, out Vector3 face, out Vector3 axis, out Vector3 lat, out _, out float y, out _);
                float w = Width(p.Edge);
                Vector3 latS = lat * p.Side;
                Vector3 Pt(float ax, float l, float up) => new Vector3(face.x, y, face.z) + axis * ax + latS * l + Vector3.up * up;
                Vector3 Size(float axLen, float latLen, float hgt) => Mathf.Abs(axis.x) > 0.5f ? new Vector3(axLen, hgt, latLen) : new Vector3(latLen, hgt, axLen);
                float l0 = w / 2f + T, l1 = l0 + DuctW;
                string nm = $"Passage {CorridorName(p.Edge)}";
                Piece($"{nm} : sol", Pt(T + DuctLen / 2f, (l0 + l1) / 2f, DuctY - 0.25f), Size(DuctLen, DuctW, 0.5f), _platform);
                Piece($"{nm} : plafond", Pt(T + DuctLen / 2f, (l0 + l1) / 2f, DuctY + DuctH + 0.25f), Size(DuctLen + 1f, DuctW + 1f, 0.5f), _ceiling, walkable: false);
                Piece($"{nm} : mur extérieur", Pt(T + (DuctLen + 1f) / 2f, l1 + 0.5f, DuctY + DuctH / 2f), Size(DuctLen + 1f, 1f, DuctH + 1f), _wall, walkable: false);
                Piece($"{nm} : mur du fond", Pt(T + DuctLen + 0.5f, (l0 + l1) / 2f, DuctY + DuctH / 2f), Size(1f, DuctW, DuctH + 1f), _wall, walkable: false);
                // Murs non magnétiques autour des deux ouvertures (on y monte en saut mural, pas à la traction).
                NMPiece($"{nm} : plaque (salle)", root, Pt(-0.08f, (l0 + l1) / 2f, (DuctY + DuctH + 1.5f) / 2f), Size(0.12f, DuctW + 5f, DuctY + DuctH + 1.5f), walkable: false);
                NMPiece($"{nm} : plaque (couloir)", root, Pt(T + 5.5f, w / 2f - 0.08f, (DuctY + DuctH + 1.5f) / 2f), Size(7f, 0.12f, DuctY + DuctH + 1.5f), walkable: false);
                PropLibrary.Place("Lanterne", root, Pt(T + DuctLen / 2f, (l0 + l1) / 2f, DuctY + 1.6f), 0f);
                PropLibrary.Place("Torche", root, Pt(T + 2f, w / 2f - 0.35f, DuctY - 1f), Mathf.Atan2(-latS.x, -latS.z) * Mathf.Rad2Deg);
            }
        }

        /// <summary>Galeries de parcours (après les murs).</summary>
        private static void BuildGalleries(float damage)
        {
            foreach (GalleryDef g in Galleries)
            {
                CorridorFrame(g.Edge, true, out Vector3 face, out Vector3 axis, out Vector3 lat, out bool ax, out float y, out float len);
                float w = Width(g.Edge);
                Vector3 latS = lat * g.Side;
                string nm = $"Galerie de parcours {CorridorName(g.Edge)}";
                var root = new GameObject(nm).transform;
                root.SetParent(_level, false);
                Vector3 Pt(float a, float l, float up) => new Vector3(face.x, y, face.z) + axis * a + latS * l + Vector3.up * up;
                Vector3 Size(float axLen, float latLen, float hgt) => ax ? new Vector3(axLen, hgt, latLen) : new Vector3(latLen, hgt, axLen);
                float l0 = w / 2f + T, l1 = l0 + GalleryWidth, a0 = 3f, a1 = len - 3f, H = L.corridorHeight, depth = 10f;
                // Enveloppe : mur extérieur, bouts, plafond, fond du vide ; tout non magnétique.
                NMPiece($"{nm} : mur extérieur", root, Pt((a0 + a1) / 2f, l1 + 0.5f, (H - depth) / 2f), Size(a1 - a0 + 2f, 1f, H + depth), walkable: false);
                NMPiece($"{nm} : bout 1", root, Pt(a0 - 0.5f, (l0 + l1) / 2f, (H - depth) / 2f), Size(1f, GalleryWidth, H + depth), walkable: false);
                NMPiece($"{nm} : bout 2", root, Pt(a1 + 0.5f, (l0 + l1) / 2f, (H - depth) / 2f), Size(1f, GalleryWidth, H + depth), walkable: false);
                NMPiece($"{nm} : plafond", root, Pt((a0 + a1) / 2f, (l0 + l1) / 2f, H + 0.5f), Size(a1 - a0 + 2f, GalleryWidth + 2f, 1f), walkable: false);
                NMPiece($"{nm} : plaque du mur du couloir", root, Pt((a0 + a1) / 2f, l0 + 0.06f, (H - depth) / 2f), Size(a1 - a0, 0.12f, H + depth), walkable: false);
                Piece($"{nm} : fond du vide", Pt((a0 + a1) / 2f, (l0 + l1) / 2f, -depth - 0.5f), Size(a1 - a0, GalleryWidth, 1f), _ceiling, walkable: false);

                // Paliers aux portes (à portée de sa main) et poutres.
                var respawns = new List<Transform>();
                foreach ((float a, string tag) in new[] { (7.5f, "entrée"), (len - 7.5f, "sortie") })
                {
                    NMPiece($"{nm} : palier {tag}", root, Pt(a, l0 + 2.5f, -0.5f), Size(6f, 5f, 1f));
                    var t = new GameObject($"Reprise {tag}").transform;
                    t.SetParent(root, false);
                    t.position = Pt(a, l0 + 2.5f, 0.2f);
                    respawns.Add(t);
                    PropLibrary.Place("Lanterne", root, Pt(a, l0 + 2.5f, 3.5f), 0f);
                }

                float beamL = l0 + 7f;                                           // à 15 m de l'axe : encore à portée
                NMPiece($"{nm} : poutre d'entrée", root, Pt(13f, beamL, -0.25f), Size(11f, 0.6f, 0.5f));
                NMPiece($"{nm} : poutre de sortie", root, Pt(len - 13f, beamL, -0.25f), Size(11f, 0.6f, 0.5f));
                NMPiece($"{nm} : passerelle d'entrée", root, Pt(10f, (l0 + 5f + beamL) / 2f, -0.25f), Size(0.6f, beamL - l0 - 5f + 0.6f, 0.5f));
                NMPiece($"{nm} : passerelle de sortie", root, Pt(len - 10f, (l0 + 5f + beamL) / 2f, -0.25f), Size(0.6f, beamL - l0 - 5f + 0.6f, 0.5f));
                // Plateformes qui tombent, en zigzag à 21–25 m de l'axe (hors de sa portée), et panneaux de course.
                int k = 0;
                for (float a = 21f; a <= len - 21f; a += 4.2f, k++)
                {
                    float l = l0 + (k % 2 == 0 ? 13f : 16.5f);
                    GameObject plat = NMPiece($"{nm} : plateforme {k + 1}", root, Pt(a, l, -0.25f), Size(2.4f, 2.4f, 0.5f));
                    GameObjectUtility.SetStaticEditorFlags(plat, 0);
                    plat.AddComponent<CrumblePlatform>();
                    if (k % 4 == 2)
                        NMPiece($"{nm} : panneau de course {k}", root, Pt(a, l1 - 0.4f, 3.5f), Size(8f, 0.6f, 7f), walkable: false);
                }

                var trig = new GameObject($"{nm} : vide");
                trig.transform.SetParent(root, false);
                trig.transform.position = Pt((a0 + a1) / 2f, (l0 + l1) / 2f, -depth / 2f - 0.75f);
                var box = trig.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = Size(a1 - a0, GalleryWidth, depth - 1.5f);
                trig.AddComponent<FallVoid>().Configure(damage, respawns.ToArray());
                trig.AddComponent<NotWalkable>();
                RoomVolume v = RoomVolumeAt(nm, Pt((a0 + a1) / 2f, (l0 + l1) / 2f, H / 2f), Quaternion.identity, Size(a1 - a0, GalleryWidth, H), RoomKind.Parcours);
                v.transform.SetParent(root, true);
                for (float a = 20f; a < len - 15f; a += 18f)
                    PropLibrary.Place("Torche", root, Pt(a, l1 - 0.35f, 5f), Mathf.Atan2(-latS.x, -latS.z) * Mathf.Rad2Deg);
                _parkourCount++;
            }
        }
    }
}
