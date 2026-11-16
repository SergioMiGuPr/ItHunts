using System;
using System.Collections.Generic;
using ItHunts.Enemies;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ItHunts.Level
{
    /// <summary>
    /// Construction avec le kit (carte aléatoire, modules) : un volume = une boîte de collision fusionnée (BoxCollider, sans
    /// rendu) + des instances visuelles des pièces du kit qui le pavent. Les pièces sont posées entières (4 m) ; le reste d'un
    /// côté qui n'est pas un multiple de 4 m est comblé par une seule pièce raccourcie (au bord). Sert à l'exécution (générateur)
    /// et dans l'éditeur (modules : Spawner garde alors le lien au prefab, pour qu'une retouche du kit se répercute).
    /// </summary>
    public static class KitBuild
    {
        /// <summary>Instancie une pièce (éditeur : PrefabUtility.InstantiatePrefab ; jeu : Object.Instantiate).</summary>
        public static Func<GameObject, Transform, GameObject> Spawner = (prefab, parent) => Object.Instantiate(prefab, parent, false);

        /// <summary>Pièces posées depuis le dernier RAZ (mesures).</summary>
        public static int PieceCount;

        public enum Kind
        {
            /// <summary>Mur vertical (le côté le plus mince est l'épaisseur).</summary>
            Mur,
            /// <summary>Dalle horizontale (sol, plafond, palier).</summary>
            Sol,
            /// <summary>Volume plein (étage rempli jusqu'au sol, tour, muret) : côtés en murs, dessus en dalles.</summary>
            Bloc,
            /// <summary>Tablier de pont (passerelle).</summary>
            Pont,
            /// <summary>Poutre (long côté pavé de poutres de 6 m).</summary>
            Poutre,
        }

        /// <summary>
        /// Volume du kit : collision (si collider) et pièces visuelles. walkable faux : NotWalkable (NavMesh). Renvoie l'objet de
        /// collision (parent des pièces).
        /// </summary>
        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Kind kind, KitSet kit,
            bool walkable = true, bool collider = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            if (collider)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = size;
            }

            if (!walkable)
                go.AddComponent<NotWalkable>();
            if (kit == null)
                return go;
            Transform t = go.transform;
            switch (kind)
            {
                case Kind.Mur:
                    WallTiles(t, Vector3.zero, size, kit);
                    break;
                case Kind.Sol:
                    SlabTiles(t, Vector3.zero, size, kit.sol4);
                    break;
                case Kind.Bloc:
                    BlockTiles(t, size, kit);
                    break;
                case Kind.Pont:
                    BridgeTiles(t, size, kit);
                    break;
                case Kind.Poutre:
                    BeamTiles(t, size, kit);
                    break;
            }

            return go;
        }

        /// <summary>
        /// Pente de liaison (couloir entre deux niveaux, rampe d'un module) : collision lisse (même comportement pour la course,
        /// la glissade et le bunny hop), visuel en marches (Kit_Escalier). De bottom (milieu du bas, au sol) dans la direction
        /// dir (horizontale), sur length (m) à plat, montée rise (m), largeur width.
        /// </summary>
        public static GameObject Ramp(Transform parent, string name, Vector3 bottom, Vector3 dir, float length, float rise, float width,
            KitSet kit, float thickness = 1f)
        {
            dir.y = 0f;
            dir.Normalize();
            float slopeLen = Mathf.Sqrt(length * length + rise * rise);
            float angle = Mathf.Atan2(rise, length) * Mathf.Rad2Deg;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-angle, 0f, 0f);
            Vector3 mid = bottom + dir * (length * 0.5f) + Vector3.up * (rise * 0.5f);
            go.transform.localPosition = mid - rot * Vector3.up * (thickness * 0.5f);
            go.transform.localRotation = rot;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(width, thickness, slopeLen);
            if (kit == null || kit.escalier == null || rise < 0.05f)
                return go;
            // Marches : visuel seulement. La surface lisse de la rampe passe par le nez de chaque marche : les marches restent
            // dessous (les pieds ne s'y enfoncent jamais). Marche k : nez à k × giron, à la hauteur k × hauteur de marche, pleine
            // jusqu'au sol, profonde d'un giron.
            var steps = new GameObject("Marches (visuel)").transform;
            steps.SetParent(parent, false);
            steps.localPosition = bottom;
            steps.localRotation = Quaternion.LookRotation(dir, Vector3.up);
            const float stepRise = 0.25f;
            int n = Mathf.Max(1, Mathf.RoundToInt(rise / stepRise));
            float r = rise / n, run = length / n;
            List<(float c, float s)> across = Segments(width, 4f);
            for (int k = 1; k < n; k++)
                foreach ((float c, float s) in across)
                {
                    GameObject st = Spawn(kit.escalier, steps);
                    st.transform.localPosition = new Vector3(c - width * 0.5f, 0f, k * run);
                    st.transform.localRotation = Quaternion.identity;
                    st.transform.localScale = new Vector3(s, k * r / stepRise, run);
                }

            return go;
        }

        /// <summary>Hauteur de l'arc du kit (m), lue sur son modèle ; 11 m (SM_Arc_Couloir) par défaut.</summary>
        public static float ArchHeight(KitSet kit)
        {
            if (kit == null || kit.arc == null)
                return 11f;
            Renderer[] rs = kit.arc.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0)
                return 11f;
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (Renderer r in rs)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                    continue;
                Bounds b = mf.sharedMesh.bounds;
                Matrix4x4 m = kit.arc.transform.worldToLocalMatrix * r.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = m.MultiplyPoint3x4(b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    lo = Mathf.Min(lo, c.y);
                    hi = Mathf.Max(hi, c.y);
                }
            }

            return hi > lo ? hi - lo : 11f;
        }

        /// <summary>
        /// Arc d'un couloir (ou d'une entrée) : son sommet touche le plafond (height au-dessus de floor), porté par deux piédroits
        /// du kit contre les murs, du sol à la naissance de l'arc ; élargi à la largeur du couloir (modèle de 14 m). Visuel seulement.
        /// </summary>
        public static void Arch(Transform parent, KitSet kit, Vector3 floor, float yaw, float width, float height)
        {
            if (kit == null || kit.arc == null)
                return;
            float ah = ArchHeight(kit), rise = Mathf.Max(0f, height - ah), sx = width / 14f;
            var root = new GameObject("Arc (porte la voûte)").transform;
            root.SetParent(parent, false);
            root.localPosition = floor;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Piece(kit.arc, root, new Vector3(0f, rise, 0f), 0f, new Vector3(sx, 1f, 1f));
            ArchCollision(root, sx, rise);
            Spandrels(root, kit, width, height, sx, rise);
            if (rise > 0.05f)
                foreach (float s in new[] { -1f, 1f })
                    Pier(root, kit, s, width, rise);
        }

        /// <summary>
        /// Ligne médiane de l'anneau de SM_Arc_Couloir (moitié gauche, repère du modèle de 14 × 11 m), relevée sur son maillage :
        /// pied de x = −7 à −5.9 m, intrados à 9.6 m au sommet, clé à 11 m. Épaisseur de l'anneau 1.2 m, profondeur 1.6 m.
        /// </summary>
        public static readonly Vector2[] ArchMidline =
        {
            new Vector2(-6.45f, 0f), new Vector2(-6.3f, 2.5f), new Vector2(-5.85f, 4.2f), new Vector2(-5.2f, 5.6f), new Vector2(-4.4f, 6.75f),
            new Vector2(-3.4f, 7.75f), new Vector2(-2.3f, 8.65f), new Vector2(-1.2f, 9.45f), new Vector2(0f, 10.3f),
        };

        public const float ArchRingThickness = 1.2f, ArchDepth = 1.6f;

        /// <summary>
        /// Collision de l'anneau (correctifs-rendu point 2) : une boîte par segment de la ligne médiane, des deux côtés, élargie avec
        /// l'arc. Pour les joueurs et les objets ; le traqueur passe au travers (StalkerPassesThrough), comme avant.
        /// </summary>
        public static Transform ArchCollision(Transform root, float sx, float rise)
        {
            var col = new GameObject("Collision de l'arc").transform;
            col.SetParent(root, false);
            col.gameObject.AddComponent<Enemies.StalkerPassesThrough>();
            col.gameObject.AddComponent<Enemies.NotWalkable>();
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i + 1 < ArchMidline.Length; i++)
                {
                    Vector2 a = new Vector2(side * ArchMidline[i].x * sx, ArchMidline[i].y + rise);
                    Vector2 b = new Vector2(side * ArchMidline[i + 1].x * sx, ArchMidline[i + 1].y + rise);
                    Vector2 d = b - a;
                    var seg = new GameObject($"Anneau {(side < 0 ? "gauche" : "droite")} {i + 1}");
                    seg.transform.SetParent(col, false);
                    seg.transform.localPosition = new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
                    seg.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                    var box = seg.AddComponent<BoxCollider>();
                    box.size = new Vector3(d.magnitude + 0.15f, ArchRingThickness, ArchDepth);       // segments qui se chevauchent : pas de fente
                }

            return col;
        }

        /// <summary>
        /// Extrados de l'anneau (moitié gauche, repère du modèle) : ligne médiane décalée d'une demi-épaisseur vers l'extérieur ;
        /// clé à 10.9 m.
        /// </summary>
        public static Vector2[] ArchExtrados()
        {
            int n = ArchMidline.Length;
            var e = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                Vector2 t = (ArchMidline[Mathf.Min(n - 1, i + 1)] - ArchMidline[Mathf.Max(0, i - 1)]).normalized;
                e[i] = ArchMidline[i] + new Vector2(-t.y, t.x) * (ArchRingThickness * 0.5f);   // vers le haut et l'extérieur
            }

            e[n - 1] = new Vector2(0f, ArchMidline[n - 1].y + ArchRingThickness * 0.5f);
            return e;
        }

        /// <summary>Profondeur des écoinçons (un peu moins que l'anneau : l'arc reste en saillie) et enfoncement dans l'anneau.</summary>
        public const float SpandrelDepth = 1.4f, SpandrelTuck = 0.3f, SpandrelIntoCeiling = 0.8f;

        private static readonly Dictionary<(int, int, int), Mesh> SpandrelMeshes = new Dictionary<(int, int, int), Mesh>();

        /// <summary>
        /// Écoinçons de l'arc (retours du lot, point 2) : le modèle n'est que l'anneau en ogive ; sous un plafond plat, il restait
        /// un vide de 3 à 6 m entre son extrados et le plafond (seule la clé le touchait). Mur de pierre plein (matériau des murs du
        /// kit, UV monde) de l'extrados au plafond et d'un mur à l'autre, 1.4 m d'épaisseur, enfoncé dans l'anneau et dans le
        /// plafond : l'arc épouse le plafond sur toute sa largeur. Fusionné avec le décor (Kit_) ; collision à part (boîtes),
        /// traversée par le traqueur comme l'anneau. À remplacer par un arc à écoinçons pleins de la DA (ART-HANDOFF).
        /// </summary>
        public static Transform Spandrels(Transform root, KitSet kit, float width, float height, float sx, float rise)
        {
            List<Vector2> left = SpandrelOutline(width, sx, rise, height);
            if (left.Count < 2)
                return null;
            float top = height + SpandrelIntoCeiling;
            Material mat = null;
            if (kit != null && kit.mur4x4 != null)
            {
                Renderer r = kit.mur4x4.GetComponentInChildren<Renderer>();
                mat = r != null ? r.sharedMaterial : null;
            }

            var key = (Mathf.RoundToInt(width * 100f), Mathf.RoundToInt(height * 100f), Mathf.RoundToInt(rise * 100f));
            if (!SpandrelMeshes.TryGetValue(key, out Mesh mesh) || mesh == null)
                SpandrelMeshes[key] = mesh = SpandrelMesh(left, top);
            if (mat != null)
            {
                var vis = new GameObject("Kit_Ecoincons");
                vis.transform.SetParent(root, false);
                vis.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = vis.AddComponent<MeshRenderer>();
                mr.sharedMaterial = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var col = new GameObject("Collision des écoinçons").transform;
            col.SetParent(root, false);
            col.gameObject.AddComponent<StalkerPassesThrough>();
            col.gameObject.AddComponent<NotWalkable>();
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i + 1 < left.Count; i++)
                {
                    Vector2 a = left[i], b = left[i + 1];
                    float bottom = Mathf.Min(a.y, b.y);
                    if (top - bottom < 0.05f || b.x - a.x < 0.01f)
                        continue;
                    var box = new GameObject($"Écoinçon {(side < 0 ? "gauche" : "droite")} {i + 1}").AddComponent<BoxCollider>();
                    box.transform.SetParent(col, false);
                    box.transform.localPosition = new Vector3(side * (a.x + b.x) * 0.5f, (bottom + top) * 0.5f, 0f);
                    box.size = new Vector3(b.x - a.x, top - bottom, SpandrelDepth);
                }

            return col;
        }

        /// <summary>
        /// Bas de l'écoinçon gauche (repère de la racine de l'arc), du mur (x = −largeur/2 − 0.1) à la clé (x = 0) : extrados
        /// élargi (sx), monté de rise et enfoncé de SpandrelTuck dans l'anneau.
        /// </summary>
        public static List<Vector2> SpandrelOutline(float width, float sx, float rise, float height)
        {
            Vector2[] e = ArchExtrados();
            float wall = -(width * 0.5f + 0.1f);
            var pts = new List<Vector2>();
            for (int i = 0; i < e.Length; i++)
            {
                var p = new Vector2(e[i].x * sx, Mathf.Min(height, e[i].y + rise - SpandrelTuck));
                if (p.x <= wall)
                    continue;
                if (pts.Count == 0)
                {
                    float y = p.y;
                    if (i > 0)
                    {
                        var q = new Vector2(e[i - 1].x * sx, e[i - 1].y + rise - SpandrelTuck);
                        y = Mathf.Lerp(q.y, p.y, Mathf.InverseLerp(q.x, p.x, wall));
                    }
                    else
                        y = Mathf.Min(y, rise);
                    pts.Add(new Vector2(wall, y));
                }

                pts.Add(p);
            }

            return pts;
        }

        private static Mesh SpandrelMesh(List<Vector2> left, float top)
        {
            var v = new List<Vector3>();
            var nrm = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float hz = SpandrelDepth * 0.5f;
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n)
            {
                int k = v.Count;
                v.AddRange(new[] { a, b, c, d });
                foreach (Vector3 p in new[] { a, b, c, d })
                {
                    nrm.Add(n);
                    uv.Add(new Vector2(p.x + p.z, p.y));
                }

                // Face avant : sens horaire vu du côté de la normale (règle de Unity).
                bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), n) < 0f;
                if (flip)
                    tri.AddRange(new[] { k, k + 1, k + 2, k, k + 2, k + 3 });
                else
                    tri.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 });
            }

            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i + 1 < left.Count; i++)
                {
                    Vector2 p0 = left[i], p1 = left[i + 1];
                    foreach (float z in new[] { -hz, hz })
                    {
                        var n = new Vector3(0f, 0f, Mathf.Sign(z));
                        Quad(new Vector3(side * p0.x, p0.y, z), new Vector3(side * p0.x, top, z), new Vector3(side * p1.x, top, z),
                            new Vector3(side * p1.x, p1.y, z), n);
                    }

                    // Dessous (dans l'anneau, invisible d'ordinaire ; ferme le volume vu d'en dessous en biais).
                    Vector3 down = Vector3.Cross(new Vector3(side * (p1.x - p0.x), p1.y - p0.y, 0f), Vector3.forward).normalized;
                    if (down.y > 0f)
                        down = -down;
                    Quad(new Vector3(side * p0.x, p0.y, -hz), new Vector3(side * p0.x, p0.y, hz), new Vector3(side * p1.x, p1.y, hz),
                        new Vector3(side * p1.x, p1.y, -hz), down);
                }

            var mesh = new Mesh { name = "Écoinçons de l'arc" };
            mesh.SetVertices(v);
            mesh.SetNormals(nrm);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Piédroit sous un pied de l'arc : la colonne de la DA (SM_Colonne, 1.4 × 8 × 1.4 m) réduite à la hauteur du piédroit, de la
        /// largeur du pied de l'arc (1.05 m) et de sa profondeur (1.6 m), contre le mur ; collision comprise. Greybox sans le modèle.
        /// </summary>
        private static void Pier(Transform root, KitSet kit, float side, float width, float rise)
        {
            const float footWidth = 1.05f;
            Vector3 at = new Vector3(side * (width * 0.5f - footWidth * 0.5f), 0f, 0f);
            if (kit.pilier == null)
            {
                Box(root, "Piédroit", at + Vector3.up * (rise * 0.5f), new Vector3(1f, rise, ArchDepth), Kind.Mur, kit, collider: false);
                return;
            }

            GameObject pier = Piece(kit.pilier, root, at, 0f, new Vector3(footWidth / 1.4f, rise / 8f, ArchDepth / 1.4f));
            pier.name = "Piédroit (colonne)";
            var box = pier.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 4f, 0f);                           // repère du modèle : 1.4 × 8 × 1.4 m, pied à 0
            box.size = new Vector3(1.4f, 8f, 1.4f);
            pier.AddComponent<Enemies.NotWalkable>();
        }

        /// <summary>Une pièce du kit à une position et une orientation locales.</summary>
        public static GameObject Piece(GameObject prefab, Transform parent, Vector3 localPosition, float yaw, Vector3? scale = null)
        {
            if (prefab == null)
                return null;
            GameObject go = Spawn(prefab, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (scale.HasValue)
                go.transform.localScale = scale.Value;
            return go;
        }

        private static GameObject Spawn(GameObject prefab, Transform parent)
        {
            PieceCount++;
            return Spawner(prefab, parent);
        }

        /// <summary>Découpe une longueur en pièces de unit (centre, échelle) ; le reste en une pièce raccourcie au bout.</summary>
        public static List<(float center, float scale)> Segments(float length, float unit)
        {
            var list = new List<(float, float)>();
            int whole = Mathf.FloorToInt(length / unit + 1e-3f);
            float rest = length - whole * unit;
            for (int i = 0; i < whole; i++)
                list.Add((unit * (i + 0.5f), 1f));
            if (rest > 0.05f)
                list.Add((whole * unit + rest * 0.5f, rest / unit));
            return list;
        }

        private static void WallTiles(Transform t, Vector3 c, Vector3 size, KitSet kit)
        {
            if (kit.mur4x4 == null)
                return;
            bool alongX = size.x >= size.z;
            float len = alongX ? size.x : size.z, th = alongX ? size.z : size.x;
            foreach ((float u, float su) in Segments(len, 4f))
            foreach ((float v, float sv) in Segments(size.y, 4f))
            {
                float a = u - len * 0.5f, y = v - size.y * 0.5f - 2f * sv;
                Vector3 p = alongX ? new Vector3(c.x + a, c.y + y, c.z) : new Vector3(c.x, c.y + y, c.z + a);
                // Demi-pièces de la DA pour les bouts de 2 × 4 m et de 4 × 3 m ; sinon la pièce de 4 × 4 m mise à l'échelle (UV monde :
                // la texture ne s'étire pas).
                GameObject prefab = kit.mur4x4;
                Vector3 scale = new Vector3(su, sv, th);
                if (kit.mur2x4 != null && Mathf.Abs(su - 0.5f) < 1e-3f && Mathf.Abs(sv - 1f) < 1e-3f)
                {
                    prefab = kit.mur2x4;
                    scale = new Vector3(1f, 1f, th);
                }
                else if (kit.mur4x3 != null && Mathf.Abs(su - 1f) < 1e-3f && Mathf.Abs(sv - 0.75f) < 1e-3f)
                {
                    prefab = kit.mur4x3;
                    scale = new Vector3(1f, 1f, th);
                }

                Piece(prefab, t, p, alongX ? 0f : 90f, scale);
            }
        }

        private static void SlabTiles(Transform t, Vector3 c, Vector3 size, GameObject prefab)
        {
            if (prefab == null)
                return;
            foreach ((float u, float su) in Segments(size.x, 4f))
            foreach ((float v, float sv) in Segments(size.z, 4f))
                Piece(prefab, t, new Vector3(c.x + u - size.x * 0.5f, c.y + size.y * 0.5f, c.z + v - size.z * 0.5f), 0f, new Vector3(su, size.y, sv));
        }

        private static void BlockTiles(Transform t, Vector3 size, KitSet kit)
        {
            Vector3 h = size * 0.5f;
            if (size.y <= 1.01f || size.x < 1.01f || size.z < 1.01f)
            {
                SlabTiles(t, Vector3.zero, size, kit.sol4);
                return;
            }

            // Côtés : murs de 1 m d'épaisseur posés à l'intérieur du volume ; dessus : dalle d'1 m.
            float wallH = size.y - 1f, yc = -h.y + wallH * 0.5f;
            float th = Mathf.Min(1f, Mathf.Min(size.x, size.z) * 0.5f);
            WallTiles(t, new Vector3(0f, yc, h.z - th * 0.5f), new Vector3(size.x, wallH, th), kit);
            WallTiles(t, new Vector3(0f, yc, -h.z + th * 0.5f), new Vector3(size.x, wallH, th), kit);
            if (size.z > 2f * th + 0.05f)
            {
                WallTiles(t, new Vector3(h.x - th * 0.5f, yc, 0f), new Vector3(th, wallH, size.z - 2f * th), kit);
                WallTiles(t, new Vector3(-h.x + th * 0.5f, yc, 0f), new Vector3(th, wallH, size.z - 2f * th), kit);
            }

            SlabTiles(t, new Vector3(0f, h.y - 0.5f, 0f), new Vector3(size.x, 1f, size.z), kit.sol4);
        }

        private static void BridgeTiles(Transform t, Vector3 size, KitSet kit)
        {
            if (kit.pont == null)
            {
                SlabTiles(t, Vector3.zero, size, kit.sol4);
                return;
            }

            bool alongX = size.x >= size.z;
            float len = alongX ? size.x : size.z, w = alongX ? size.z : size.x;
            foreach ((float u, float su) in Segments(len, 4f))
            {
                float a = u - len * 0.5f;
                Piece(kit.pont, t, alongX ? new Vector3(a, size.y * 0.5f, 0f) : new Vector3(0f, size.y * 0.5f, a), alongX ? 90f : 0f,
                    new Vector3(w / 3f, size.y, su));
            }
        }

        private static void BeamTiles(Transform t, Vector3 size, KitSet kit)
        {
            if (kit.poutre == null)
            {
                SlabTiles(t, Vector3.zero, size, kit.sol4);
                return;
            }

            bool alongX = size.x >= size.z;
            float len = alongX ? size.x : size.z;
            foreach ((float u, float su) in Segments(len, 6f))
            {
                float a = u - len * 0.5f;
                Piece(kit.poutre, t, alongX ? new Vector3(a, -size.y * 0.5f, 0f) : new Vector3(0f, -size.y * 0.5f, a), alongX ? 0f : 90f,
                    new Vector3(su, size.y / 0.5f, (alongX ? size.z : size.x) / 0.5f));
            }
        }
    }
}
