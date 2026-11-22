using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Contour d'une salle (D-092) : carrés marchants sur les centres des cases, donc des murs droits le long des cases et des biseaux
    /// à 45° dans les coins (plus d'escaliers de cases). Chaque segment connaît la salle voisine (−1 : roche, falaise) ; les segments
    /// alignés et contigus de même voisin sont fusionnés en un seul mur. Coordonnées en cases (u) : la case (x, y) couvre
    /// [x, x+1] × [y, y+1].
    /// </summary>
    public static class ActContour
    {
        public struct Segment
        {
            public Vector2 A, B;
            /// <summary>Normale vers l'extérieur de la salle.</summary>
            public Vector2 Out;
            public int Neighbor;
            /// <summary>Porte (indice dans le plan) que ce segment traverse ; −1 : mur plein.</summary>
            public int Door;
            public bool Diagonal => Mathf.Abs(A.x - B.x) > 0.01f && Mathf.Abs(A.y - B.y) > 0.01f;
            public float Length => Vector2.Distance(A, B);
        }

        /// <summary>Segments du contour de la salle room, fusionnés.</summary>
        public static List<Segment> Of(ActPlan plan, ActRoom room)
        {
            var raw = new List<Segment>();
            RectInt b = room.Box;
            for (int x = b.xMin - 1; x < b.xMax; x++)
            for (int y = b.yMin - 1; y < b.yMax; y++)
            {
                bool bl = plan.OwnerAt(new Vector2Int(x, y)) == room.Id, br = plan.OwnerAt(new Vector2Int(x + 1, y)) == room.Id;
                bool tr = plan.OwnerAt(new Vector2Int(x + 1, y + 1)) == room.Id, tl = plan.OwnerAt(new Vector2Int(x, y + 1)) == room.Id;
                int c = (bl ? 1 : 0) | (br ? 2 : 0) | (tr ? 4 : 0) | (tl ? 8 : 0);
                if (c == 0 || c == 15)
                    continue;
                var B = new Vector2(x + 1f, y + 0.5f);
                var R = new Vector2(x + 1.5f, y + 1f);
                var T = new Vector2(x + 1f, y + 1.5f);
                var L = new Vector2(x + 0.5f, y + 1f);
                var center = new Vector2(x + 1f, y + 1f);
                void Add(Vector2 p, Vector2 q)
                {
                    // Normale vers l'extérieur : du côté des coins hors de la salle.
                    Vector2 d = (q - p).normalized, n = new Vector2(-d.y, d.x);
                    Vector2 mid = (p + q) * 0.5f;
                    Vector2 probe = mid + n * 0.4f;
                    int at = plan.OwnerAt(new Vector2Int(Mathf.FloorToInt(probe.x), Mathf.FloorToInt(probe.y)));
                    if (at == room.Id)
                        n = -n;
                    Vector2 outP = mid + n * 0.45f;
                    int nb = plan.OwnerAt(new Vector2Int(Mathf.FloorToInt(outP.x), Mathf.FloorToInt(outP.y)));
                    raw.Add(new Segment { A = p, B = q, Out = n, Neighbor = nb == room.Id ? -1 : nb, Door = -1 });
                }

                switch (c)
                {
                    case 1: Add(L, B); break;
                    case 2: Add(B, R); break;
                    case 3: Add(L, R); break;
                    case 4: Add(R, T); break;
                    case 5: Add(L, B); Add(R, T); break;
                    case 6: Add(B, T); break;
                    case 7: Add(L, T); break;
                    case 8: Add(T, L); break;
                    case 9: Add(B, T); break;
                    case 10: Add(B, R); Add(T, L); break;
                    case 11: Add(R, T); break;
                    case 12: Add(L, R); break;
                    case 13: Add(B, R); break;
                    case 14: Add(L, B); break;
                }
            }

            return Merge(raw);
        }

        private static List<Segment> Merge(List<Segment> raw)
        {
            // Clé : direction (normalisée, sens unique), ligne, voisin, porte ; puis tri le long et fusion des contigus.
            var groups = new Dictionary<(int dir, int line, int nb, int door, int ox, int oy), List<Segment>>();
            foreach (Segment s0 in raw)
            {
                Segment s = s0;
                Vector2 d = s.B - s.A;
                int dir;
                if (Mathf.Abs(d.y) < 0.01f) dir = 0;
                else if (Mathf.Abs(d.x) < 0.01f) dir = 1;
                else dir = d.x * d.y > 0 ? 2 : 3;
                // Sens unique : A avant B selon x (ou y pour les verticaux).
                bool swap = dir == 1 ? s.A.y > s.B.y : s.A.x > s.B.x;
                if (swap)
                    (s.A, s.B) = (s.B, s.A);
                float lineV = dir switch { 0 => s.A.y, 1 => s.A.x, 2 => s.A.y - s.A.x, _ => s.A.y + s.A.x };
                var key = (dir, Mathf.RoundToInt(lineV * 4f), s.Neighbor, s.Door, Mathf.RoundToInt(s.Out.x * 2f), Mathf.RoundToInt(s.Out.y * 2f));
                if (!groups.TryGetValue(key, out List<Segment> l))
                    groups[key] = l = new List<Segment>();
                l.Add(s);
            }

            var result = new List<Segment>();
            foreach (KeyValuePair<(int dir, int line, int nb, int door, int ox, int oy), List<Segment>> kv in groups)
            {
                bool vertical = kv.Key.dir == 1;
                kv.Value.Sort((p, q) => vertical ? p.A.y.CompareTo(q.A.y) : p.A.x.CompareTo(q.A.x));
                Segment cur = kv.Value[0];
                for (int i = 1; i < kv.Value.Count; i++)
                {
                    Segment n = kv.Value[i];
                    if ((n.A - cur.B).sqrMagnitude < 0.0025f)
                        cur.B = n.B;
                    else
                    {
                        result.Add(cur);
                        cur = n;
                    }
                }

                result.Add(cur);
            }

            return result;
        }
    }
}
