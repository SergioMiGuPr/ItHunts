using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Graphe du labyrinthe (D-035) : les intersections (salles et carrefours, chacune une boîte) et les couloirs qui les
    /// relient. Le traqueur s'en sert pour la règle de Pac-Man : arrivé à une intersection, il prend la branche qui le
    /// rapproche le plus du joueur (par le chemin), jamais celle d'où il vient, sauf dans un cul-de-sac (ou enragé).
    /// Construit par le générateur de la scène. Calculs purs (testés en EditMode).
    /// </summary>
    public sealed class MazeGraph : MonoBehaviour
    {
        [SerializeField] private string[] names = new string[0];
        [Tooltip("Centre de chaque intersection (au sol) et demi-taille de sa boîte (la hauteur part du sol).")]
        [SerializeField] private Vector3[] centers = new Vector3[0];
        [SerializeField] private Vector3[] halfExtents = new Vector3[0];
        [Tooltip("Couloirs : paires d'indices d'intersections.")]
        [SerializeField] private Vector2Int[] edges = new Vector2Int[0];

        private float[,] _dist;
        private List<int>[] _neighbors;

        public int NodeCount => centers.Length;
        public int EdgeCount => edges.Length;
        public Vector3 Center(int i) => centers[i];
        public Vector3 HalfExtents(int i) => halfExtents[i];
        public string NodeName(int i) => i >= 0 && i < names.Length ? names[i] : $"#{i}";
        public Vector2Int Edge(int i) => edges[i];
        public IReadOnlyList<int> Neighbors(int i)
        {
            Prepare();
            return _neighbors[i];
        }

        public void Configure(string[] nodeNames, Vector3[] nodeCenters, Vector3[] nodeHalfExtents, Vector2Int[] links)
        {
            names = nodeNames;
            centers = nodeCenters;
            halfExtents = nodeHalfExtents;
            edges = links;
            _dist = null;
            _avoiding.Clear();
        }

        /// <summary>Distances par les couloirs entre intersections (Floyd-Warshall, une fois).</summary>
        private void Prepare()
        {
            if (_dist != null && _dist.GetLength(0) == centers.Length)
                return;
            _avoiding.Clear();
            int n = centers.Length;
            _dist = new float[n, n];
            _neighbors = new List<int>[n];
            for (int i = 0; i < n; i++)
            {
                _neighbors[i] = new List<int>();
                for (int j = 0; j < n; j++)
                    _dist[i, j] = i == j ? 0f : float.PositiveInfinity;
            }

            foreach (Vector2Int e in edges)
            {
                float d = Vector3.Distance(centers[e.x], centers[e.y]);
                _dist[e.x, e.y] = _dist[e.y, e.x] = d;
                _neighbors[e.x].Add(e.y);
                _neighbors[e.y].Add(e.x);
            }

            for (int k = 0; k < n; k++)
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (_dist[i, k] + _dist[k, j] < _dist[i, j])
                    _dist[i, j] = _dist[i, k] + _dist[k, j];
        }

        public float Distance(int a, int b)
        {
            Prepare();
            return _dist[a, b];
        }

        /// <summary>Intersection qui contient le point (−1 : dans un couloir).</summary>
        public int NodeAt(Vector3 p)
        {
            for (int i = 0; i < centers.Length; i++)
            {
                Vector3 c = centers[i], h = halfExtents[i];
                if (Mathf.Abs(p.x - c.x) <= h.x && Mathf.Abs(p.z - c.z) <= h.z && p.y >= c.y - 3f && p.y <= c.y + 2f * h.y)
                    return i;
            }

            return -1;
        }

        /// <summary>Couloir le plus proche du point (segment entre deux centres) ; t : position le long de a → b.</summary>
        public int NearestEdge(Vector3 p, out float t)
        {
            int best = -1;
            float bestD = float.PositiveInfinity;
            t = 0f;
            for (int i = 0; i < edges.Length; i++)
            {
                Vector3 a = centers[edges[i].x], b = centers[edges[i].y];
                Vector3 ab = b - a;
                float u = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-4f, ab.sqrMagnitude));
                float d = (a + ab * u - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                    t = u;
                }
            }

            return best;
        }

        private readonly Dictionary<int, float[,]> _avoiding = new Dictionary<int, float[,]>();

        /// <summary>Distances par les couloirs sans passer par l'intersection <paramref name="avoid"/> (y repasser = demi-tour).</summary>
        private float[,] Avoiding(int avoid)
        {
            Prepare();
            if (_avoiding.TryGetValue(avoid, out float[,] d))
                return d;
            int n = centers.Length;
            d = new float[n, n];
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                d[i, j] = i == j ? 0f : float.PositiveInfinity;
            foreach (Vector2Int e in edges)
                if (e.x != avoid && e.y != avoid)
                    d[e.x, e.y] = d[e.y, e.x] = Vector3.Distance(centers[e.x], centers[e.y]);
            for (int k = 0; k < n; k++)
            for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (d[i, k] + d[k, j] < d[i, j])
                    d[i, j] = d[i, k] + d[k, j];
            _avoiding[avoid] = d;
            return d;
        }

        /// <summary>Distance (par les couloirs) de l'intersection n jusqu'au point p, sans repasser par <paramref name="avoid"/> (−1 : libre).</summary>
        public float CostFrom(int n, Vector3 p, int avoid = -1)
        {
            Prepare();
            float[,] dist = avoid >= 0 ? Avoiding(avoid) : _dist;
            int at = NodeAt(p);
            if (at >= 0)
                return at == avoid ? float.PositiveInfinity : dist[n, at] + Flat(p - centers[at]);
            int e = NearestEdge(p, out _);
            if (e < 0)
                return float.PositiveInfinity;
            int a = edges[e].x, b = edges[e].y;
            float ca = a == avoid ? float.PositiveInfinity : dist[n, a] + Flat(p - centers[a]);
            float cb = b == avoid ? float.PositiveInfinity : dist[n, b] + Flat(p - centers[b]);
            return Mathf.Min(ca, cb);
        }

        /// <summary>
        /// Règle de Pac-Man : depuis l'intersection <paramref name="at"/>, où il est arrivé par <paramref name="cameFrom"/>,
        /// la branche qui le rapproche le plus de <paramref name="target"/>. Jamais cameFrom, sauf cul-de-sac (une seule
        /// branche) ou demi-tour permis. −1 : le joueur est dans cette intersection (il fonce droit dessus).
        /// </summary>
        public int ChooseNext(int at, int cameFrom, Vector3 target, bool allowUTurn)
        {
            Prepare();
            if (at < 0 || at >= centers.Length)
                return -1;
            if (NodeAt(target) == at)
                return -1;
            List<int> next = _neighbors[at];
            bool deadEnd = next.Count <= 1;
            int targetEdge = NodeAt(target) < 0 ? NearestEdge(target, out _) : -1;
            int best = -1;
            float bestCost = float.PositiveInfinity;
            foreach (int n in next)
            {
                if (n == cameFrom && !deadEnd && !allowUTurn)
                    continue;
                float cost;
                if (targetEdge >= 0 && IsEdge(targetEdge, at, n))
                    cost = Flat(target - centers[at]);                 // le joueur est dans ce couloir-là
                else if (allowUTurn)
                    cost = _dist[at, n] + CostFrom(n, target);
                else
                    cost = _dist[at, n] + CostFrom(n, target, at);       // sans revenir par ici (ce serait un demi-tour)
                if (cost < bestCost || best < 0)
                {
                    bestCost = cost;
                    best = n;
                }
            }

            return best;
        }

        /// <summary>
        /// Étape d'un long trajet (goules, playtest des étapes 8 et 9) : l'entrée de l'intersection suivante vers la cible, par
        /// les couloirs, depuis la dernière intersection traversée (<paramref name="lastNode"/>, mise à jour ici) : jamais de
        /// retour en arrière au seuil d'une salle. Faux si la cible est dans cette intersection (aller droit dessus).
        /// </summary>
        public bool Waypoint(Vector3 from, Vector3 target, ref int lastNode, out Vector3 point)
        {
            Prepare();
            point = target;
            if (centers == null || centers.Length == 0)
                return false;
            int at = NodeAt(from);
            if (at >= 0)
                lastNode = at;
            int a = lastNode;
            if (a < 0 || a >= centers.Length)
            {
                // Inconnue (apparue dans un couloir) : le bout de son couloir qui rapproche le plus.
                int e0 = NearestEdge(from, out _);
                if (e0 < 0)
                    return false;
                int x = edges[e0].x, y = edges[e0].y;
                a = Flat(centers[x] - from) + CostFrom(x, target) <= Flat(centers[y] - from) + CostFrom(y, target) ? x : y;
                point = Entry(a, e0);
                return true;
            }

            if (NodeAt(target) == a)
                return false;
            int n = ChooseNext(a, -1, target, true);
            if (n < 0)
                return false;
            int e = EdgeBetween(a, n);
            point = e >= 0 ? Entry(n, e) : centers[n];
            return true;
        }

        /// <summary>Couloir entre deux intersections (−1 : aucun).</summary>
        public int EdgeBetween(int a, int b)
        {
            for (int i = 0; i < edges.Length; i++)
                if (IsEdge(i, a, b))
                    return i;
            return -1;
        }

        /// <summary>
        /// Entrée de l'intersection <paramref name="node"/> côté couloir <paramref name="edge"/> : son sol près du bord, pas son
        /// centre (une fosse ou une plateforme de module peut l'occuper).
        /// </summary>
        public Vector3 Entry(int node, int edge)
        {
            Prepare();
            Vector3 mid = (centers[edges[edge].x] + centers[edges[edge].y]) * 0.5f;
            Vector3 dir = mid - centers[node];
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f)
                return centers[node];
            dir.Normalize();
            Vector3 h = halfExtents[node];
            float reach = Mathf.Abs(dir.x) > Mathf.Abs(dir.z) ? h.x : h.z;
            return centers[node] + dir * (reach * 0.75f);
        }

        private bool IsEdge(int e, int a, int b) =>
            (edges[e].x == a && edges[e].y == b) || (edges[e].x == b && edges[e].y == a);

        public bool HasEdge(int a, int b)
        {
            for (int i = 0; i < edges.Length; i++)
                if (IsEdge(i, a, b))
                    return true;
            return false;
        }

        /// <summary>Le couloir e fait partie d'au moins une boucle (sans lui, ses deux bouts restent reliés).</summary>
        public bool EdgeInLoop(int e)
        {
            int a = edges[e].x, b = edges[e].y;
            var seen = new bool[centers.Length];
            var stack = new Stack<int>();
            stack.Push(a);
            seen[a] = true;
            while (stack.Count > 0)
            {
                int x = stack.Pop();
                if (x == b)
                    return true;
                for (int i = 0; i < edges.Length; i++)
                {
                    if (i == e)
                        continue;
                    int y = edges[i].x == x ? edges[i].y : edges[i].y == x ? edges[i].x : -1;
                    if (y >= 0 && !seen[y])
                    {
                        seen[y] = true;
                        stack.Push(y);
                    }
                }
            }

            return false;
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude + Mathf.Abs(v.y) * 0.5f;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            foreach (Vector2Int e in edges)
                if (e.x < centers.Length && e.y < centers.Length)
                    Gizmos.DrawLine(centers[e.x] + Vector3.up, centers[e.y] + Vector3.up);
            for (int i = 0; i < centers.Length; i++)
                Gizmos.DrawWireCube(centers[i] + Vector3.up * halfExtents[i].y, halfExtents[i] * 2f);
        }
    }
}
