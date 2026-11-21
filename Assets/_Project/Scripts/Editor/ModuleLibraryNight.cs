using ItHunts.Enemies;
using ItHunts.Level;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Quatre modules d'après les références de l'utilisateur (docs/design/level/references-modules/, point 9 de la nuit du
    /// 2026-11-21) : prison à étages, caserne, citerne inondée, puits circulaire. Mêmes règles que les autres (voies dégagées,
    /// étages pleins jusqu'au sol, ponts à 10 m, la main du traqueur atteint tout point où l'on tient debout, aucune cachette).
    /// Verticalité et routes pour bien bouger ; passages du traqueur typés (StalkerPassage : bond, escalade, mur, chaîne, porte
    /// étroite) ; ancres de coffre avec leur difficulté. Pièces du kit de la DA (PR #25) posées quand elles existent, avec
    /// des collisions simples (A2) ; les blocs portent toujours les sols et le NavMesh.
    /// </summary>
    public static partial class ModuleLibrary
    {
        private const string KitDir = "Assets/Art/Models/Modules/";

        /// <summary>Pièce du kit de la DA posée par ses bornes : centre en x et z, base à baseY ; collisions simples (A2).</summary>
        private static GameObject ArtPiece(string fbx, float x, float z, float baseY, float yaw = 0f, float scale = 1f) =>
            ArtPiece(fbx, x, z, baseY, yaw, Vector3.one * scale);

        private static GameObject ArtPiece(string fbx, float x, float z, float baseY, float yaw, Vector3 scale)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(KitDir + fbx + ".fbx");
            if (model == null)
                return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, _m);
            go.name = "Kit (DA) : " + fbx;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = scale;
            go.transform.localPosition = Vector3.zero;
            Bounds b = default;
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) b = r.bounds;
                else b.Encapsulate(r.bounds);
                any = true;
            }

            Vector3 world = _m.TransformPoint(new Vector3(x, baseY, z));
            if (any)
                go.transform.position += new Vector3(world.x - b.center.x, world.y - b.min.y, world.z - b.center.z);
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            KitColliders(go, fbx.Replace("SM_Mod_", ""));
            return go;
        }

        // ---------- Collisions du kit (A2, matin du 2026-11-21) ----------

        /// <summary>
        /// Solide : bloque tout. Garde : garde-corps, cordes, échelles, chaînes : bloque les joueurs et les objets, pas la main ni le
        /// regard du traqueur (une collision cachait le joueur), hors NavMesh. Traverse : solide pour les joueurs, le traqueur passe
        /// au travers (comme les arcs des couloirs), hors NavMesh du traqueur. Décor : solide, jamais praticable (pas de NavMesh dessus).
        /// </summary>
        private enum KitCol { Solide, Garde, Traverse, Decor }

        private static GameObject KitCollider(GameObject piece, string name, Vector3 center, Vector3 size, Quaternion rot, KitCol kind, bool capsule = false)
        {
            var go = new GameObject("Collision : " + name);
            go.transform.SetParent(piece.transform, false);
            go.transform.localPosition = center;
            go.transform.localRotation = rot;
            GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(piece));
            if (capsule)
            {
                var c = go.AddComponent<CapsuleCollider>();
                c.direction = size.y >= size.x && size.y >= size.z ? 1 : size.z >= size.x ? 2 : 0;
                c.radius = Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.5f;
                c.height = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            }
            else
                go.AddComponent<BoxCollider>().size = size;
            if (kind == KitCol.Garde || kind == KitCol.Traverse)
                go.AddComponent<StalkerPassesThrough>();
            if (kind != KitCol.Solide)
                go.AddComponent<NotWalkable>();
            return go;
        }

        private static GameObject KitBox(GameObject piece, string name, Vector3 center, Vector3 size, KitCol kind = KitCol.Solide) =>
            KitCollider(piece, name, center, size, Quaternion.identity, kind);

        /// <summary>Segments d'un arc autour du centre du puits (repère du FBX) : dalle ou rampe, garde-corps côté intérieur.</summary>
        private static void KitArc(GameObject piece, string name, float centerDeg, float spanDeg, float r0, float r1, float y0, float y1, float railHeight,
            KitCol kind = KitCol.Solide)
        {
            const int n = 3;
            float r = (r0 + r1) * 0.5f, step = spanDeg / n, len = r * step * Mathf.Deg2Rad * 1.06f;
            float slope = Mathf.Atan2(y1 - y0, r * spanDeg * Mathf.Deg2Rad) * Mathf.Rad2Deg;
            for (int i = 0; i < n; i++)
            {
                float a = centerDeg - spanDeg * 0.5f + step * (i + 0.5f), y = Mathf.Lerp(y0, y1, (i + 0.5f) / n);
                Quaternion rot = Quaternion.Euler(-slope, -a, 0f);
                var radial = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad));
                Vector3 down = rot * Vector3.down;
                KitCollider(piece, $"{name} {i + 1}", radial * r + Vector3.up * y + down * 0.15f, new Vector3(r1 - r0, 0.3f, len), rot, kind);
                KitCollider(piece, $"{name} : garde-corps {i + 1}", radial * (r0 + 0.05f) + Vector3.up * (y + railHeight * 0.5f), new Vector3(0.12f, railHeight, len), rot, KitCol.Garde);
            }
        }

        /// <summary>Collisions simples de chaque pièce du kit des modules (cotes de docs/art/INTEGRATION-PROPS.md, repère du FBX).</summary>
        private static void KitColliders(GameObject go, string piece)
        {
            switch (piece)
            {
                case "GrandePorte_4x10":
                case "GrandePorte_6x10":
                {
                    bool small = piece.EndsWith("4x10");
                    float outer = small ? 10.4f : 12.4f, open = small ? 4f : 6f, jamb = (outer - open) * 0.5f;
                    foreach (float sx in new[] { 1f, -1f })
                        KitBox(go, "jambage", new Vector3(sx * (open + jamb) * 0.5f, 6f, 0.2f), new Vector3(jamb, 12f, 1.9f), KitCol.Decor);
                    KitBox(go, "linteau", new Vector3(0f, 11f, 0.2f), new Vector3(open, 2f, 1.9f), KitCol.Decor);
                    break;
                }
                case "Cellule_4m":
                    foreach (float sx in new[] { 1f, -1f })
                        KitBox(go, "mur de côté", new Vector3(sx * 2.35f, 2.05f, -2.35f), new Vector3(0.3f, 4.1f, 4.7f), KitCol.Decor);
                    KitBox(go, "mur du fond", new Vector3(0f, 2.05f, -4.55f), new Vector3(5f, 4.1f, 0.3f), KitCol.Decor);
                    KitBox(go, "toit", new Vector3(0f, 3.95f, -2.35f), new Vector3(5f, 0.3f, 4.7f));
                    break;
                case "Echelle_6m":
                    KitBox(go, "échelle", new Vector3(0f, 3f, 0f), new Vector3(0.7f, 6f, 0.12f), KitCol.Garde);
                    break;
                case "MurGrimpe_8x10":
                    KitBox(go, "mur grimpable", new Vector3(0f, 5f, -0.1f), new Vector3(8f, 10f, 1.05f), KitCol.Decor);
                    break;
                case "ChaineGrimpe_12m":
                    KitCollider(go, "chaîne", new Vector3(0f, -6.1f, 0f), new Vector3(0.6f, 12.6f, 0.6f), Quaternion.identity, KitCol.Garde, capsule: true);
                    break;
                case "ChaineTendue_16m":
                    KitCollider(go, "chaîne tendue", new Vector3(0f, -0.6f, 0f), new Vector3(0.5f, 0.5f, 16.2f), Quaternion.identity, KitCol.Garde, capsule: true);
                    break;
                case "PontCordes_12m":
                    // Le tablier est celui du pont du module (dessus à 11 m) ; cordes et poteaux de chaque côté.
                    foreach (float sx in new[] { 1f, -1f })
                        KitBox(go, "cordes", new Vector3(sx * 0.85f, 0.7f, 0f), new Vector3(0.12f, 1.4f, 12.2f), KitCol.Garde);
                    break;
                case "Rebord_6m":
                    KitBox(go, "rebord", new Vector3(0f, -0.7f, 0.6f), new Vector3(6f, 1.4f, 1.2f));
                    break;
                case "LitsSuperposes":
                    KitBox(go, "lits", new Vector3(0f, 0.95f, 0f), new Vector3(2f, 1.9f, 0.9f), KitCol.Traverse);   // le traqueur les enjambe
                    break;
                case "Balcon_6m":
                    KitBox(go, "dalle", new Vector3(0f, -0.2f, 1.25f), new Vector3(6f, 0.4f, 2.5f));
                    KitBox(go, "garde-corps avant", new Vector3(0f, 0.55f, 2.45f), new Vector3(6f, 1.1f, 0.12f), KitCol.Garde);
                    foreach (float sx in new[] { 1f, -1f })
                        KitBox(go, "garde-corps de côté", new Vector3(sx * 2.95f, 0.55f, 1.25f), new Vector3(0.12f, 1.1f, 2.5f), KitCol.Garde);
                    break;
                case "Ratelier":
                    KitBox(go, "râtelier", new Vector3(0f, 1.2f, 0f), new Vector3(2.1f, 2.4f, 0.4f), KitCol.Decor);
                    break;
                case "PasserellePilotis_8m":
                    KitBox(go, "plancher", new Vector3(0f, -0.15f, 0f), new Vector3(2f, 0.3f, 8f));
                    break;
                case "ArcheBrisee_8m":
                    foreach (float sx in new[] { 1f, -1f })
                        KitBox(go, "pilier", new Vector3(sx * 4f, 4.5f, 0f), new Vector3(1.2f, 9f, 1.2f), KitCol.Traverse);
                    break;
                case "TuyauGros":
                    KitBox(go, "tuyau", new Vector3(0f, -0.4f, 1.2f), new Vector3(1.4f, 2.1f, 3f), KitCol.Decor);
                    break;
                case "RoueEau_8m":
                    foreach (float a in new[] { 0f, 45f, 90f, 135f })
                        KitCollider(go, $"roue {a:0}", Vector3.zero, new Vector3(3.6f, 8.2f, 3.4f), Quaternion.Euler(a, 0f, 0f), KitCol.Decor);
                    break;
                case "EscalierSpirale_45":
                    // 46° de tour (rayons 9.5 à 12 m) ; la rampe monte de 0 à 2.5 m (collision lisse sous les marches).
                    KitArc(go, "rampe", -157.4f, 45.9f, 9.5f, 12f, 0f, 2.5f, 1.1f, KitCol.Traverse);   // au pied des passages du traqueur : il l'enjambe
                    break;
                case "Coursive_45":
                    KitArc(go, "dalle", -157.4f, 45.9f, 9.5f, 12f, 0f, 0f, 1.1f);
                    break;
            }
        }

        /// <summary>Passage du traqueur (point 9) de a à b (local).</summary>
        private static StalkerPassage Passage(StalkerLinkKind kind, Vector3 a, Vector3 b, float width = 6f)
        {
            var go = new GameObject($"Passage du traqueur : {kind}");
            go.transform.SetParent(_m, false);
            go.transform.localPosition = a;
            var end = new GameObject("Arrivée").transform;
            end.SetParent(go.transform, false);
            end.position = _m.TransformPoint(b);
            var p = go.AddComponent<StalkerPassage>();
            p.kind = kind;
            p.end = end;
            p.width = width;
            return p;
        }

        /// <summary>Porte étroite (8 × 10 m, sous la hauteur du traqueur) : deux jambages et un linteau, zone où il se faufile.</summary>
        private static void NarrowDoor(string name, float x, float z, float yaw, float width = 8f, float height = 10f)
        {
            Quaternion r = Quaternion.Euler(0f, yaw, 0f);
            Vector3 c = new Vector3(x, 0f, z), right = r * Vector3.right;
            foreach (float s in new[] { 1f, -1f })
            {
                Vector3 j = c + right * s * (width * 0.5f + 0.75f);
                KitBuild.Box(_m, $"{name} : jambage", new Vector3(j.x, height * 0.5f, j.z), new Vector3(1.5f, height, 1.5f), KitBuild.Kind.Mur, _kit, walkable: false);
            }

            GameObject lintel = KitBuild.Box(_m, $"{name} : linteau", new Vector3(c.x, height + 1f, c.z), new Vector3(width + 3f, 2f, 1.5f), KitBuild.Kind.Poutre, _kit);
            lintel.transform.localRotation = r;
            StalkerPassage p = Passage(StalkerLinkKind.PorteEtroite, c, c + r * Vector3.forward * 0.1f, width);
            p.transform.localRotation = r;
            p.doorHalfSize = new Vector3(width * 0.5f, height * 0.5f, 2.5f);
            ArtPiece(width <= 4.5f ? "SM_Mod_GrandePorte_4x10" : "SM_Mod_GrandePorte_6x10", x, z, 0f, yaw,
                new Vector3(Mathf.Max(1f, width / (width <= 4.5f ? 4f : 6f)), height / 10f, 1f));          // ouverture = celle de la porte (A2)
        }

        private static void ChestAnchor(float x, float z, float yaw, float y, int difficulty, string route)
        {
            ChestAt(x, z, yaw, y);
            var spot = _m.GetChild(_m.childCount - 1).GetComponent<ChestSpot>();
            spot.difficulty = difficulty;
            spot.route = route;
        }

        // ---------- Prison à étages ----------

        /// <summary>
        /// Prison à étages : cellules sur trois niveaux le long des murs est et ouest (sol, coursive à 5.5 m, coursive à 11 m), garde-
        /// corps, escaliers et échelles, pont de planches entre les deux côtés à 11 m, cages suspendues, chaînes entre les étages.
        /// Traqueur : mur grimpable vers la 1re coursive, chaîne vers la 2e, bond depuis le sol.
        /// </summary>
        private static void PrisonEtages(RoomModule m)
        {
            Setup(m, "Prison à étages", 64f, shape: ModuleShape.Etages);
            m.minPerMap = 1;                                              // au moins une par carte (point 9 : à tester)
            m.ceilingOverride = 24f;
            Ceil = 24f;
            float h = _h, lane = 5.5f;
            foreach (float sx in new[] { 1f, -1f })
            {
                foreach (float sz in new[] { 1f, -1f })
                {
                    float z0 = sz * (lane + 1f), z1 = sz * (h - 1f);
                    // 1re coursive profonde de 14 m (le traqueur, 7 m de large, y tient), 2e étroite (joueurs seulement).
                    Block($"Cellules, rez et 1re coursive {sx} {sz}", sx * (h - 14f), sx * h, z0, z1, 5.5f);
                    Block($"Cellules, 2e coursive {sx} {sz}", sx * (h - 4f), sx * h, z0, z1, 11f);
                    for (float z = lane + 3f; z < h - 2f; z += 4f)
                    {
                        P("Grille", sx * (h - 14.1f), sz * z, 90f);                   // portes de cellule au rez
                        P("Grille", sx * (h - 4.1f), sz * z, 90f, 5.5f);              // et à la 1re coursive
                        if (((int)z & 4) == 0)
                            P("Paille", sx * (h - 3f), sz * z, 0f, 11f);
                    }

                    // Garde-corps (poutres basses) au bord des coursives.
                    // Garde-corps : visuels seulement (sans collision) : ils ne cachent pas le joueur à la main du traqueur.
                    foreach ((string g, float gx, float gy) in new[] { ("1", h - 13.8f, 6.1f), ("2", h - 3.8f, 11.6f) })
                    {
                        GameObject rail = KitBuild.Box(_m, $"Garde-corps {g} {sx} {sz}", new Vector3(sx * gx, gy, (z0 + z1) * 0.5f), new Vector3(0.2f, 1.1f, Mathf.Abs(z1 - z0)), KitBuild.Kind.Poutre, _kit);
                        foreach (Collider c in rail.GetComponentsInChildren<Collider>())
                            Object.DestroyImmediate(c);
                    }
                    P("Chaine", sx * (h - 4.3f), sz * 20f, 0f, 9f);                    // chaînes entre les étages
                    ArtPiece("SM_Mod_Cellule_4m", sx * (h - 2f), sz * 24f, 0f, sx > 0 ? -90f : 90f);
                }

                // Escalier du sol à la 1re coursive (contre la coursive, côté sud), échelle vers la 2e (côté nord).
                Stairs($"Escalier {sx}", new Vector3(sx * (h - 15.6f), 0f, -(h - 2f)), new Vector3(0f, 0f, 1f), 5.5f);
                P("Echafaudage", sx * (h - 6f), h - 4f, 90f, 5.5f);
                ArtPiece("SM_Mod_Echelle_6m", sx * (h - 4.3f), h - 8f, 5.5f, sx > 0 ? -90f : 90f);

                // Passages du traqueur : mur grimpable (rez → 1re), chaîne (1re → 2e), bond depuis le sol.
                // Passages du traqueur : mur grimpable (rez vers 1re coursive), saut depuis la chaîne (1re vers sol), bond depuis le sol.
                // Côté +z, le pont de planches (11 m) passe au-dessus de la 1re coursive : le traqueur n'y tient pas debout. Mur grimpable
                // côté −z des deux côtés (A2 : son arrivée était sans NavMesh pour sx = 1).
                float wz = sx > 0f ? -10f : -14f;
                Passage(StalkerLinkKind.MurGrimpe, new Vector3(sx * (h - 18f), 0f, wz), new Vector3(sx * (h - 8f), 5.5f, wz));
                ArtPiece("SM_Mod_MurGrimpe_8x10", sx * (h - 14.2f), wz, 0f, sx > 0 ? -90f : 90f, 0.6f);
                Passage(StalkerLinkKind.ChaineSaut, new Vector3(sx * (h - 8f), 5.5f, -sz0(sx) * 18f), new Vector3(sx * 9f, 0f, -sz0(sx) * 18f));
                ArtPiece("SM_Mod_ChaineGrimpe_12m", sx * (h - 13f), -sz0(sx) * 18f, 5.5f);
                Passage(StalkerLinkKind.Bond, new Vector3(sx * 6f, 0f, sz0(sx) * 24f), new Vector3(sx * (h - 8f), 5.5f, sz0(sx) * 24f));
            }

            // Pont de planches à 11 m entre les deux côtés, cages suspendues au-dessus du vide.
            Bridge("planches", new Vector3(-(h - 4f), 0f, 16f), new Vector3(h - 4f, 0f, 16f));
            ArtPiece("SM_Mod_PontCordes_12m", 0f, 16f, 11f - 2.2f, 90f, 2.2f);           // bouts du tablier au dessus du pont (11 m)
            foreach (float x in new[] { -12f, 12f })
            {
                P("Chaine", x, -14f, 0f, 19f);
                P("Cage", x, -14f, 0f, 15f);
            }

            foreach ((float x, float z) in new[] { (-12f, -20f), (-10.6f, -20f), (12f, 20f), (13.4f, 20f), (10f, -24f) })
                P((int)x % 2 == 0 ? "Caisse_Attrapable" : "Tonneau_Attrapable", x, z);                  // objets à lancer

            ChestAnchor(h - 2f, -(h - 4f), 270f, 11f, 2, "2e coursive : échelle, chaîne ou traction depuis la 1re");
            ChestAnchor(-(h - 9f), h - 10f, 90f, 5.5f, 1, "1re coursive par l'escalier");
            ChestAnchor(14f, -(h - 3f), 0f, 0f, 0, "au sol");
            WallTorches(14f, 3.5f);
            Common(8, 3, 5);
        }

        private static float sz0(float sx) => sx > 0f ? 1f : -1f;

        // ---------- Caserne ----------

        /// <summary>
        /// Caserne : grande salle commune, mezzanine des dortoirs au nord (lits superposés) desservie par deux escaliers, armurerie
        /// au sud-est derrière une porte étroite (le traqueur se faufile), cheminée à l'ouest, tables. Traqueur : rebord vers la
        /// mezzanine, bond d'une moitié de mezzanine à l'autre.
        /// </summary>
        private static void Caserne(RoomModule m)
        {
            Setup(m, "Caserne", 64f, shape: ModuleShape.Etages);
            m.minPerMap = 1;                                              // au moins une par carte (point 9 : à tester)
            float h = _h, lane = 5.5f;
            foreach (float sx in new[] { 1f, -1f })
            {
                Block($"Mezzanine des dortoirs {sx}", sx * lane, sx * (h - 1f), h - 11f, h - 1f, 6f);
                Stairs($"Escalier de la mezzanine {sx}", new Vector3(sx * (h - 2.6f), 0f, h - 23f), new Vector3(0f, 0f, 1f), 6f);
                for (float x = lane + 3f; x < h - 3f; x += 5f)
                {
                    // Le traqueur enjambe les lits (A2) : sinon la mezzanine (10 m) n'a plus de NavMesh pour lui, ni rebord ni bond.
                    GameObject bed = P("Lit", sx * x, h - 4f, 180f, 6f);
                    if (bed != null)
                        bed.AddComponent<StalkerPassesThrough>();
                }
                Passage(StalkerLinkKind.EscaladeRebord, new Vector3(sx * 16f, 0f, h - 16f), new Vector3(sx * 16f, 6f, h - 6f));
                ArtPiece("SM_Mod_Rebord_6m", sx * 14f, h - 11.2f, 0f, 180f);
                ArtPiece("SM_Mod_LitsSuperposes", sx * (h - 6f), h - 3f, 6f, 180f);
            }

            Passage(StalkerLinkKind.Bond, new Vector3(11f, 6f, h - 6f), new Vector3(-11f, 6f, h - 6f));
            ArtPiece("SM_Mod_Balcon_6m", 10f, h - 11.6f, 6f - 1.65f, 180f);                // dalle au niveau de la mezzanine (6 m), hors de la voie

            // Armurerie (sud-est) : murs de 12 m, porte étroite de 8 × 10 m vers la salle.
            float ax0 = 16f, az1 = -16f;
            Block("Armurerie : mur ouest", ax0, ax0 + 1f, -(h - 1f), az1 - 5f, 12f, 0f, _stone, false);
            Block("Armurerie : mur nord (est)", ax0 + 9.5f, h - 1f, az1, az1 + 1f, 12f, 0f, _stone, false);
            NarrowDoor("Porte de l'armurerie", ax0 + 5f, az1 + 0.5f, 0f);
            Block("Armurerie : mur nord (ouest)", ax0, ax0 + 0.5f, az1, az1 + 1f, 12f, 0f, _stone, false);
            P("Ratelier", h - 2f, -(h - 8f), -90f);
            P("Ratelier", h - 2f, -(h - 13f), -90f);
            ArtPiece("SM_Mod_Ratelier", h - 2f, -(h - 18f), 0f, -90f);
            ChestAnchor(h - 4f, -(h - 4f), 225f, 0f, 1, "armurerie, derrière la porte étroite");

            // Cheminée (ouest) : foyer plein de 8 m de haut, feu.
            Block("Cheminée", -(h - 1f), -(h - 4f), -22f, -12f, 8f, 0f, _stone);
            P("Torche", -(h - 4.4f), -17f, 90f, 1.2f);
            P("Chaudron", -(h - 6f), -17f);
            ChestAnchor(-(h - 2.5f), -14f, 90f, 8f, 2, "dessus de la cheminée : saut depuis la mezzanine ou traction");

            // Tables de la salle commune.
            foreach (float sx in new[] { 1f, -1f })
                foreach (float sz in new[] { -10f, -22f })
                {
                    P("Table", sx * 14f, sz, 90f);
                    P("Banc", sx * 14f, sz + 1.6f, 90f);
                    P("Chaise_Attrapable", sx * 16f, sz - 1.4f);
                }

            ChestAnchor(-(h - 10f), h - 4f, 180f, 6f, 1, "mezzanine des dortoirs");
            WallTorches(14f, 4f);
            Common(8, 2, 5);
        }

        // ---------- Citerne inondée ----------

        /// <summary>
        /// Citerne inondée : eau profonde le long de x (canal de 16 m, creusé par le générateur), passerelles sur pilotis et
        /// plateformes à rejoindre en sautant ou en se tractant, piliers et arches brisés, tuyaux qui déversent de l'eau, roue à
        /// eau. Traqueur : il n'entre pas dans l'eau, il la franchit d'un bond (plateformes espacées) ou passe par les ponts.
        /// </summary>
        private static void Citerne(RoomModule m)
        {
            Setup(m, "Citerne inondée", 64f, shape: ModuleShape.Egout);
            m.minPerMap = 1;                                              // au moins une par carte (point 9 : à tester)
            m.hole = ModuleHole.Canal;
            m.holeRect = new Rect(-_h, 8f, 2f * _h, 14f);              // au nord : la voie est-ouest reste au sec
            m.holeDepth = 7f;
            m.rotate = false;
            float h = _h;

            // Plateformes sur pilotis dans l'eau (dessus au niveau du sol) : de rive à rive en sautant (4.5 m d'écart).
            float[] xs = { -22f, -11f, 11f, 22f };
            foreach (float x in xs)
            {
                Block($"Plateforme sur pilotis {x}", x - 2.5f, x + 2.5f, 12.5f, 17.5f, 7f, -7f);
                ArtPiece("SM_Mod_PasserellePilotis_8m", x, 15f, -7f, 90f, new Vector3(2.5f, 7f / 3f, 5f / 8f));   // plancher au ras du sol, sur la plateforme
            }

            // Piliers et arches brisés sur les rives.
            foreach (float sz in new[] { 1f, -1f })
                foreach (float x in new[] { -26f, -16f, 16f, 26f })
                {
                    float z = sz > 0f ? 24.5f : 6.5f;                       // sur les rives du canal (8 à 22)
                    if (sz < 0f)
                        Pilier(x, z);                                         // rive nord étroite : pas de pilier (le traqueur doit y passer)
                    if (x < 0 == sz > 0)
                        ArtPiece("SM_Mod_ArcheBrisee_8m", x, z, 0f, sz > 0f ? 90f : 0f);      // rive sud : le long de la voie (A2)
                }

            // Tuyaux qui déversent de l'eau (rideau d'eau toon) et roue à eau.
            Material water = WaterSetup.Water();
            foreach (float x in new[] { -18f, 18f })
            {
                P("Tuyau", x, h - 1f, 180f, 6f);
                GameObject fall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(fall.GetComponent<Collider>());
                fall.name = "Eau qui tombe du tuyau";
                fall.transform.SetParent(_m, false);
                fall.transform.localPosition = new Vector3(x, 1.5f, h - 2.5f);
                fall.transform.localScale = new Vector3(0.8f, 9f, 0.3f);
                fall.GetComponent<Renderer>().sharedMaterial = water;
                ArtPiece("SM_Mod_TuyauGros", x, h - 1.5f, 6f, 180f);
            }

            ArtPiece("SM_Mod_RoueEau_8m", -h + 6f, 15f, -4f, 90f);

            // Passages du traqueur : bonds par-dessus l'eau (rive à rive, 16 m).
            foreach (float x in new[] { -6f, 6f })
                Passage(StalkerLinkKind.Bond, new Vector3(x, 0f, 4f), new Vector3(x, 0f, 26f));

            // Objets à lancer sur la rive sud (caisses, tonneaux).
            foreach ((float x, float z) in new[] { (-20f, -18f), (-18.6f, -18f), (18f, -20f), (19.4f, -20f), (-8f, -26f), (9f, -26f) })
                P((int)x % 2 == 0 ? "Caisse_Attrapable" : "Tonneau_Attrapable", x, z);

            ChestAnchor(22f, 15f, 0f, 0f, 2, "plateforme au milieu de l'eau : sauts ou traction");
            ChestAnchor(-(h - 3f), h - 4f, 135f, 0f, 1, "rive nord, de l'autre côté de l'eau");
            ChestAnchor(h - 4f, -(h - 4f), 315f, 0f, 0, "rive sud");
            WallTorches(16f, 4f);
            Common(6, 3, 4);
        }

        // ---------- Puits circulaire ----------

        /// <summary>
        /// Puits circulaire : grand puits vertical (plafond à 30 m), coursives en anneau dans les angles à 11 m (pleines jusqu'au
        /// sol) puis à 17 m, reliées au-dessus des voies par des ponts, escalier qui tourne contre la paroi, pont qui traverse le
        /// vide, chaînes et cages suspendues, fosse au centre (fond dangereux : chute = retour au bord et dégâts). Traqueur : murs
        /// grimpables vers l'anneau, bonds d'un angle à l'autre au-dessus des voies, chaîne vers l'anneau haut.
        /// </summary>
        private static void Puits(RoomModule m)
        {
            Setup(m, "Puits circulaire", 64f, shape: ModuleShape.Etages);
            m.minPerMap = 1;                                              // au moins une par carte (point 9 : à tester)
            m.ceilingOverride = 30f;
            Ceil = 30f;
            m.hole = ModuleHole.Vide;
            m.holeRect = new Rect(-8f, -8f, 16f, 16f);
            m.holeDepth = 8f;
            float h = _h, lane = 5.5f;
            Quad((sx, sz, q) =>
            {
                // Anneau bas profond de 10 m (le traqueur y tient), anneau haut étroit (joueurs).
                Block($"Anneau bas, bras x {q}", sx * (lane + 1f), sx * h, sz * (h - 10f), sz * h, 11f);
                Block($"Anneau bas, bras z {q}", sx * (h - 10f), sx * h, sz * (lane + 1f), sz * (h - 10f), 11f);
                Block($"Anneau haut {q}", sx * (h - 3f), sx * h, sz * (h - 12f), sz * h, 17f);
                // Escalier contre la paroi (le long de x, sur la face intérieure du bras x) : du sol à l'anneau bas.
                if (q % 2 == 0)
                    Stairs($"Escalier en spirale {q}", new Vector3(sx * (lane + 2.5f), 0f, sz * (h - 11.6f)), new Vector3(sx, 0f, 0f), 11f);
                ArtPiece("SM_Mod_EscalierSpirale_45", sx * (h - 13f), sz * (h - 13f), 0f, q * 90f);
                ArtPiece("SM_Mod_Coursive_45", sx * (h - 4f), sz * (h - 4f), 11f - 1.3f, q * 90f);   // dalle au ras de l'anneau bas (11 m)
                // Traqueur : mur grimpable (sol vers anneau bas) dans trois angles, chaîne dans le quatrième.
                Passage(q == 3 ? StalkerLinkKind.ChaineGrimpe : StalkerLinkKind.MurGrimpe,
                    new Vector3(sx * (h - 14f), 0f, sz * (h - 14f)), new Vector3(sx * (h - 6f), 11f, sz * (h - 6f)));
            });

            // Ponts au-dessus des voies (anneau continu à 11 m) et pont qui traverse le vide.
            Bridge("anneau nord", new Vector3(-(lane + 1f), 0f, h - 5f), new Vector3(lane + 1f, 0f, h - 5f));
            Bridge("anneau sud", new Vector3(-(lane + 1f), 0f, -(h - 5f)), new Vector3(lane + 1f, 0f, -(h - 5f)));
            Bridge("anneau est", new Vector3(h - 5f, 0f, -(lane + 1f)), new Vector3(h - 5f, 0f, lane + 1f));
            Bridge("anneau ouest", new Vector3(-(h - 5f), 0f, -(lane + 1f)), new Vector3(-(h - 5f), 0f, lane + 1f));
            Bridge("au-dessus du vide", new Vector3(-(h - 10f), 0f, 0f), new Vector3(h - 10f, 0f, 0f));

            // Traqueur : bonds d'un angle à l'autre au-dessus des voies (11 m), chaîne vers l'anneau haut.
            Passage(StalkerLinkKind.Bond, new Vector3(10.5f, 11f, h - 5f), new Vector3(-10.5f, 11f, h - 5f));
            Passage(StalkerLinkKind.Bond, new Vector3(h - 5f, 11f, 10.5f), new Vector3(h - 5f, 11f, -10.5f));
            Passage(StalkerLinkKind.ChaineSaut, new Vector3(h - 6f, 11f, -(h - 6f)), new Vector3(14f, 0f, -14f));

            // Chaînes et cages suspendues au-dessus du vide.
            // Cages basses (dessus à 16 m au plus : la main du traqueur y arrive depuis le bord de la fosse).
            foreach ((float x, float z) in new[] { (6f, 6f), (-6f, -6f), (6f, -6f) })
            {
                P("Chaine", x, z, 0f, 20f);
                P("Cage", x, z, 0f, 13.5f);
            }

            ArtPiece("SM_Mod_ChaineTendue_16m", 0f, 0f, 20f, 45f);
            foreach ((float x, float z) in new[] { (-12f, -20f), (-10.6f, -20f), (12f, 20f), (13.4f, 20f), (10f, -24f) })
                P((int)x % 2 == 0 ? "Caisse_Attrapable" : "Tonneau_Attrapable", x, z);                  // objets à lancer

            ChestAnchor(-(lane + 4f), -(lane + 4f), 45f, 0f, 2, "au fond du puits, au bord de la fosse");
            ChestAnchor(h - 1.5f, h - 6f, 225f, 17f, 2, "anneau haut : chaîne ou traction");
            ChestAnchor(-(h - 3f), h - 14f, 135f, 11f, 1, "anneau bas par l'escalier");
            WallTorches(14f, 5f);
            Common(6, 4, 4);
        }
    }
}
