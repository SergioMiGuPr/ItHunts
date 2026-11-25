using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Ensembles de décor d'un acte (D-118) : compositions posées d'un bloc selon la fonction de la salle (banquet, cellule, bancs de
    /// chapelle, tombes…). Le texte se règle sans code ; la liste des pièces est remplie par ItHunts > Ensembles de décor.
    /// Format, une section par ensemble : « [Nom] centre|mur LxP fonctions A, B ; max 3 » puis une pièce par ligne :
    /// « Prefab x z [lacet] [y] [échelle] » (x à droite, z vers l'avant ; mur : z depuis le mur). « @Prefab x z » reprend l'intérieur d'une
    /// salle d'exemple de la DA (sans ses murs, sols et plafonds). Fonction « * » : toutes les salles, pour combler.
    /// </summary>
    [CreateAssetMenu(menuName = "ItHunts/Ensembles de décor", fileName = "Ensembles")]
    public sealed class EnsembleLibrary : ScriptableObject
    {
        public TextAsset text;
        [Tooltip("Allée libre autour d'un ensemble (m).")]
        [Range(2f, 12f)] public float aisle = 4f;
        [Tooltip("Pas de décor à moins de … m d'une porte.")]
        [Range(4f, 20f)] public float doorClear = 9f;
        [Tooltip("Espace entre deux ensembles d'un même mur (m).")]
        public Vector2 wallGap = new Vector2(2f, 6f);
        [Tooltip("Au-delà de cette hauteur (m), le traqueur contourne la pièce ; en dessous il l'enjambe.")]
        [Range(1f, 10f)] public float stepOver = 6f;
        [Tooltip("Place libre autour d'un ensemble plus haut que cela (m) : le traqueur (7 m de large) en fait le tour.")]
        [Range(4f, 20f)] public float tallClear = 10f;
        public List<GameObject> pieces = new List<GameObject>();

        public sealed class Piece
        {
            public string Name;
            public bool Example;
            public float X, Z, Yaw, Y, Scale = 1f;
        }

        public sealed class Set
        {
            public string Name;
            public bool Wall;
            public float Width, Depth;
            public int Max = 99;
            public bool Any;
            public readonly List<string> Functions = new List<string>();
            public readonly List<Piece> Pieces = new List<Piece>();
            public bool Fits(string function) => Any || Functions.Contains(function ?? "");
        }

        /// <summary>Vrai si une pièce dépasse stepOver (le traqueur la contourne).</summary>
        public bool Tall(Set s)
        {
            if (_tall.TryGetValue(s, out bool t))
                return t;
            foreach (Piece p in s.Pieces)
                t |= !p.Example && !p.Name.Contains("Arbre") && Find(p.Name) is GameObject go && go.GetComponent<ItHunts.Enemies.StalkerPassesThrough>() == null && p.Y + Height(go) * p.Scale > stepOver;
            _tall[s] = t;
            return t;
        }

        /// <summary>Hauteur d'un prefab au-dessus de son pivot (m).</summary>
        public static float Height(GameObject prefab)
        {
            float top = 0f;
            foreach (MeshFilter mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null)
                    continue;
                Bounds b = mf.sharedMesh.bounds;
                Matrix4x4 m = prefab.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                    top = Mathf.Max(top, m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) * 2 - 1, (i & 2) - 1, (i & 4) / 2 - 1))).y);
            }

            return top;
        }

        private readonly Dictionary<Set, bool> _tall = new Dictionary<Set, bool>();
        private Dictionary<string, GameObject> _byName;
        private List<Set> _sets;
        private string _parsed;

        public GameObject Find(string name)
        {
            if (_byName == null || _byName.Count != pieces.Count)
            {
                _byName = new Dictionary<string, GameObject>();
                foreach (GameObject p in pieces)
                    if (p != null)
                        _byName[p.name] = p;
            }

            return _byName.TryGetValue(name, out GameObject go) ? go : null;
        }

        public List<Set> Sets
        {
            get
            {
                string t = text != null ? text.text : "";
                if (_sets == null || _parsed != t)
                {
                    _sets = Parse(t);
                    _parsed = t;
                }

                return _sets;
            }
        }

        public static List<Set> Parse(string text)
        {
            var list = new List<Set>();
            Set s = null;
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                if (line[0] == '[')
                {
                    int e = line.IndexOf(']');
                    s = new Set { Name = line.Substring(1, e - 1) };
                    string rest = line.Substring(e + 1).Trim();
                    int f = rest.IndexOf("fonctions", System.StringComparison.Ordinal);
                    string[] head = rest.Substring(0, f).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                    s.Wall = head[0] == "mur";
                    string[] size = head[1].Split('x');
                    s.Width = F(size[0]);
                    s.Depth = F(size[1]);
                    string[] tail = rest.Substring(f + 9).Split(';');
                    foreach (string fn in tail[0].Split(','))
                        if (fn.Trim() == "*")
                            s.Any = true;
                        else
                            s.Functions.Add(fn.Trim());
                    for (int i = 1; i < tail.Length; i++)
                        if (tail[i].Trim().StartsWith("max "))
                            s.Max = int.Parse(tail[i].Trim().Substring(4));
                    list.Add(s);
                    continue;
                }

                string[] w = line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (s == null || w.Length < 3)
                    continue;
                s.Pieces.Add(new Piece
                {
                    Example = w[0][0] == '@',
                    Name = w[0].TrimStart('@'),
                    X = F(w[1]),
                    Z = F(w[2]),
                    Yaw = w.Length > 3 ? F(w[3]) : 0f,
                    Y = w.Length > 4 ? F(w[4]) : 0f,
                    Scale = w.Length > 5 ? F(w[5]) : 1f,
                });
            }

            return list;
        }

        private static float F(string s) => float.Parse(s, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
