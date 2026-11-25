using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Carte faite à la main (jour du 2026-11-24, D-110) : un fichier texte lisible (plan en caractères, salles, portes, structures,
    /// ancres) converti en ActPlan par ActPlan.FromHand. La géométrie, les kits de la DA, le NavMesh, la minicarte et le bot
    /// restent ceux des actes ; seul le tracé est écrit à la main. Le contenu reste tiré au hasard (ennemis, coffres sur ancres,
    /// marchand parmi ses emplacements). Format : voir docs/design/level/CARTES.md.
    /// </summary>
    public sealed class HandMap
    {
        public sealed class Room
        {
            public char Id;
            public string Name = "", Function = "";
            public float Floor;
            /// <summary>Hauteur sous plafond (m) ; négatif : celle des réglages ; 0 : ciel ouvert.</summary>
            public float Ceiling = -1f;
            public RoomRole Role = RoomRole.Normale;
            public RoomFeature Feature = RoomFeature.Aucun;
            /// <summary>Galerie le long d'un ou plusieurs côtés (n, s, e, o) : bande surélevée de TerraceDepth cases.</summary>
            public string TerraceSides = "";
            public float TerraceHeight;
            public int TerraceDepth = 4;
            /// <summary>Faux : la galerie n'a pas d'escalier (on y monte par traction, wall jump, hissage ou par une porte haute).</summary>
            public bool TerraceStairs = true;
        }

        public sealed class Door
        {
            public char A, B;
            public bool High, Narrow, Locked, Exit, Gate, Ramp;
            /// <summary>Côté de A où percer la porte (n, s, e, o), ou '\0' : la plus longue limite commune.</summary>
            public char Side;
            /// <summary>Position le long de la limite (0 début, 1 fin), 0.5 par défaut.</summary>
            public float At = 0.5f;
            public int Line;
        }

        public sealed class Structure
        {
            public string Kind = "";
            public char Room;
            public string Side = "";
            public float Height;
            /// <summary>Points en caractères du plan (x vers la droite, y vers le bas) ; NaN si absents.</summary>
            public Vector2 P0 = new Vector2(float.NaN, float.NaN), P1 = new Vector2(float.NaN, float.NaN);
            public int Line;
        }

        public string Name = "";
        public string Source = "";
        public int Act = 1;
        public float Scale = 8f;
        /// <summary>Bords organiques (caves, jardin) : profondeur rongée au bord des salles, en m (0 : bords droits, château).</summary>
        public float Organic;
        public readonly List<string> Rows = new List<string>();
        public readonly List<Room> Rooms = new List<Room>();
        public readonly List<Door> Doors = new List<Door>();
        public readonly List<Structure> Structures = new List<Structure>();
        public readonly List<char> ChestRooms = new List<char>();
        public readonly List<char> MerchantRooms = new List<char>();
        public readonly List<string> Errors = new List<string>();

        public int Width { get; private set; }
        public int Height => Rows.Count;

        public Room Find(char id)
        {
            foreach (Room r in Rooms)
                if (r.Id == id)
                    return r;
            return null;
        }

        /// <summary>Caractère du plan (x de gauche à droite, y de haut en bas), '.' hors plan.</summary>
        public char At(int x, int y) => y >= 0 && y < Rows.Count && x >= 0 && x < Rows[y].Length ? Rows[y][x] : '.';

        private static readonly Dictionary<string, HandMap> Cache = new Dictionary<string, HandMap>();

        public static HandMap Load(TextAsset asset)
        {
            if (asset == null)
                return null;
            string key = asset.name + ":" + asset.text.GetHashCode();
            if (Cache.TryGetValue(key, out HandMap m))
                return m;
            m = Parse(asset.text, asset.name);
            Cache[key] = m;
            return m;
        }

        private static float F(string s) => float.Parse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);

        private static bool TryF(string s, out float v) =>
            float.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

        private static Vector2 Point(string s)
        {
            string[] xy = s.Split(',');
            return xy.Length == 2 && TryF(xy[0], out float x) && TryF(xy[1], out float y) ? new Vector2(x, y) : new Vector2(float.NaN, float.NaN);
        }

        public static HandMap Parse(string text, string source)
        {
            var m = new HandMap { Source = source };
            string section = "";
            string[] lines = text.Replace("\r", "").Split('\n');
            for (int li = 0; li < lines.Length; li++)
            {
                string raw = lines[li];
                int line = li + 1;
                if (section == "plan")
                {
                    // Le plan est lu tel quel jusqu'à la section suivante ; « | » encadre les lignes (espaces de fin gardés).
                    string t = raw.Trim();
                    if (t.StartsWith("["))
                        section = "";
                    else if (t.StartsWith("|"))
                    {
                        string row = t.Substring(1);
                        if (row.EndsWith("|"))
                            row = row.Substring(0, row.Length - 1);
                        m.Rows.Add(row.Replace(' ', '.'));
                        continue;
                    }
                    else if (t.Length == 0 || t.StartsWith("#"))
                        continue;
                }

                string l = raw;
                int hash = l.IndexOf('#');
                if (hash >= 0)
                    l = l.Substring(0, hash);
                l = l.Trim();
                if (l.Length == 0)
                    continue;
                if (l.StartsWith("[") && l.EndsWith("]"))
                {
                    section = l.Substring(1, l.Length - 2).Trim().ToLowerInvariant();
                    continue;
                }

                try
                {
                    switch (section)
                    {
                        case "":
                        {
                            int colon = l.IndexOf(':');
                            if (colon < 0)
                                break;
                            string k = l.Substring(0, colon).Trim().ToLowerInvariant(), v = l.Substring(colon + 1).Trim();
                            if (k == "nom")
                                m.Name = v;
                            else if (k == "acte")
                                m.Act = int.Parse(v, CultureInfo.InvariantCulture);
                            else if (k == "echelle" || k == "échelle")
                                m.Scale = F(v);
                            else if (k == "organique")
                                m.Organic = F(v);
                            break;
                        }
                        case "salles":
                            m.ParseRoom(l, line);
                            break;
                        case "portes":
                            m.ParseDoor(l, line);
                            break;
                        case "structures":
                            m.ParseStructure(l, line);
                            break;
                        case "ancres":
                        {
                            string[] t = l.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                            List<char> list = t[0] == "coffre" || t[0] == "coffres" ? m.ChestRooms : t[0] == "marchand" ? m.MerchantRooms : null;
                            if (list == null)
                                m.Errors.Add($"ligne {line} : ancre inconnue « {t[0]} »");
                            else
                                for (int i = 1; i < t.Length; i++)
                                    list.Add(t[i][0]);
                            break;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    m.Errors.Add($"ligne {line} : {e.Message} ({l})");
                }
            }

            foreach (string r in m.Rows)
                m.Width = Mathf.Max(m.Width, r.Length);
            m.Validate();
            return m;
        }

        private void ParseRoom(string l, int line)
        {
            // A : Nom ; sol 0 ; plafond 14 ; depart ; eau ; galerie n,e 6 [prof 3] [sans-escalier] ; fonction Cachot
            int colon = l.IndexOf(':');
            if (colon < 1)
            {
                Errors.Add($"ligne {line} : salle sans « : »");
                return;
            }

            var r = new Room { Id = l.Substring(0, colon).Trim()[0] };
            string[] parts = l.Substring(colon + 1).Split(';');
            r.Name = parts[0].Trim();
            r.Function = r.Name;
            for (int i = 1; i < parts.Length; i++)
            {
                string[] t = parts[i].Trim().Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (t.Length == 0)
                    continue;
                switch (t[0].ToLowerInvariant())
                {
                    case "sol": r.Floor = F(t[1]); break;
                    case "plafond": r.Ceiling = F(t[1]); break;
                    case "ciel": r.Ceiling = 0f; break;
                    case "fonction": r.Function = string.Join(" ", t, 1, t.Length - 1); break;
                    case "depart": case "départ": r.Role = RoomRole.Depart; break;
                    case "sortie": r.Role = RoomRole.Sortie; break;
                    case "hall": r.Role = RoomRole.HallDeSortie; break;
                    case "arrivee": case "arrivée": r.Role = RoomRole.Arrivee; break;
                    case "verrouillee": case "verrouillée": r.Role = RoomRole.Verrouillee; break;
                    case "eau": r.Feature = RoomFeature.Eau; break;
                    case "vide": case "fosse": r.Feature = RoomFeature.Vide; break;
                    case "galerie":
                        r.TerraceSides = t[1].Replace(",", "");
                        r.TerraceHeight = F(t[2]);
                        for (int k = 3; k < t.Length; k++)
                        {
                            if (t[k] == "prof" && k + 1 < t.Length)
                                r.TerraceDepth = Mathf.Max(2, Mathf.RoundToInt(F(t[++k])));
                            else if (t[k] == "sans-escalier")
                                r.TerraceStairs = false;
                        }

                        break;
                    default:
                        Errors.Add($"ligne {line} : mot inconnu « {t[0]} » pour la salle {r.Id}");
                        break;
                }
            }

            if (Find(r.Id) != null)
                Errors.Add($"ligne {line} : salle {r.Id} en double");
            Rooms.Add(r);
        }

        private void ParseDoor(string l, int line)
        {
            // A-B [haute] [etroite] [verrou] [sortie] [arrivee] [rampe] [@n|s|e|o] [à 0.3]
            string[] t = l.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            string[] ab = t[0].Split('-');
            if (ab.Length != 2 || ab[0].Length != 1 || ab[1].Length != 1)
            {
                Errors.Add($"ligne {line} : porte « {t[0]} » (attendu A-B)");
                return;
            }

            var d = new Door { A = ab[0][0], B = ab[1][0], Line = line };
            for (int i = 1; i < t.Length; i++)
            {
                string w = t[i].ToLowerInvariant();
                if (w.StartsWith("@") && w.Length == 2)
                    d.Side = w[1];
                else if (w == "haute")
                    d.High = true;
                else if (w == "etroite" || w == "étroite")
                    d.Narrow = true;
                else if (w == "verrou")
                    d.Locked = true;
                else if (w == "sortie")
                    d.Exit = true;
                else if (w == "arrivee" || w == "arrivée")
                    d.Gate = true;
                else if (w == "rampe")
                    d.Ramp = true;
                else if ((w == "a" || w == "à") && i + 1 < t.Length)
                    d.At = Mathf.Clamp01(F(t[++i]));
                else
                    Errors.Add($"ligne {line} : mot inconnu « {t[i]} » pour la porte {t[0]}");
            }

            Doors.Add(d);
        }

        private void ParseStructure(string l, int line)
        {
            // coursive A n 8 | passerelle A 3,4 9,4 8 | mezzanine A n | plateforme A 5,6 4 | murescalade A e | murrebords A o
            string[] t = l.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            var s = new Structure { Kind = t[0].ToLowerInvariant(), Room = t.Length > 1 ? t[1][0] : '?', Line = line };
            for (int i = 2; i < t.Length; i++)
            {
                if (t[i].Contains(","))
                {
                    if (float.IsNaN(s.P0.x))
                        s.P0 = Point(t[i]);
                    else
                        s.P1 = Point(t[i]);
                }
                else if (TryF(t[i], out float h))
                    s.Height = h;
                else
                    s.Side = t[i].ToLowerInvariant();
            }

            Structures.Add(s);
        }

        private void Validate()
        {
            if (Rows.Count == 0)
                Errors.Add("plan vide");
            var inPlan = new HashSet<char>();
            foreach (string row in Rows)
                foreach (char c in row)
                    if (c != '.')
                        inPlan.Add(c);
            foreach (char c in inPlan)
                if (Find(c) == null)
                    Errors.Add($"caractère « {c} » du plan sans salle");
            foreach (Room r in Rooms)
                if (!inPlan.Contains(r.Id))
                    Errors.Add($"salle {r.Id} ({r.Name}) absente du plan");
            foreach (Door d in Doors)
                if (Find(d.A) == null || Find(d.B) == null)
                    Errors.Add($"ligne {d.Line} : porte {d.A}-{d.B} vers une salle inconnue");
            foreach (Structure s in Structures)
                if (Find(s.Room) == null)
                    Errors.Add($"ligne {s.Line} : structure {s.Kind} dans une salle inconnue {s.Room}");
            int starts = 0, exits = 0, halls = 0;
            foreach (Room r in Rooms)
            {
                starts += r.Role == RoomRole.Depart ? 1 : 0;
                exits += r.Role == RoomRole.Sortie ? 1 : 0;
                halls += r.Role == RoomRole.HallDeSortie ? 1 : 0;
            }

            if (starts != 1 || exits != 1 || halls != 1)
                Errors.Add($"il faut un départ, une sortie et un hall (ici {starts}, {exits}, {halls})");
            if (Mathf.Abs(Scale / 2f - Mathf.Round(Scale / 2f)) > 0.01f)
                Errors.Add("l'échelle doit être un multiple de 2 m");
        }
    }
}
