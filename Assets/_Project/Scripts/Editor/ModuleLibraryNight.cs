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
    /// étroite) ; ancres de coffre avec leur difficulté. Pièces du kit de la DA (PR #25) posées en habillage quand elles existent
    /// (sans collision : la collision reste celle des blocs).
    /// </summary>
    public static partial class ModuleLibrary
    {
        private const string KitDir = "Assets/Art/Models/Modules/";

        /// <summary>Pièce du kit de la DA (habillage, sans collision), posée par ses bornes : centre en x et z, base à baseY.</summary>
        private static GameObject ArtPiece(string fbx, float x, float z, float baseY, float yaw = 0f, float scale = 1f)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(KitDir + fbx + ".fbx");
            if (model == null)
                return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(model, _m);
            go.name = "Kit (DA) : " + fbx;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
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
            return go;
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
            ArtPiece(width <= 4.5f ? "SM_Mod_GrandePorte_4x10" : "SM_Mod_GrandePorte_6x10", x, z, 0f, yaw, Mathf.Max(1f, width / 6f));
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
                Passage(StalkerLinkKind.MurGrimpe, new Vector3(sx * (h - 18f), 0f, sz0(sx) * 14f), new Vector3(sx * (h - 8f), 5.5f, sz0(sx) * 14f));
                ArtPiece("SM_Mod_MurGrimpe_8x10", sx * (h - 14.2f), sz0(sx) * 14f, 0f, sx > 0 ? -90f : 90f, 0.6f);
                Passage(StalkerLinkKind.ChaineSaut, new Vector3(sx * (h - 8f), 5.5f, -sz0(sx) * 18f), new Vector3(sx * 9f, 0f, -sz0(sx) * 18f));
                ArtPiece("SM_Mod_ChaineGrimpe_12m", sx * (h - 13f), -sz0(sx) * 18f, 5.5f);
                Passage(StalkerLinkKind.Bond, new Vector3(sx * 6f, 0f, sz0(sx) * 24f), new Vector3(sx * (h - 8f), 5.5f, sz0(sx) * 24f));
            }

            // Pont de planches à 11 m entre les deux côtés, cages suspendues au-dessus du vide.
            Bridge("planches", new Vector3(-(h - 4f), 0f, 16f), new Vector3(h - 4f, 0f, 16f));
            ArtPiece("SM_Mod_PontCordes_12m", 0f, 16f, 10f, 90f, 2.2f);
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
                    P("Lit", sx * x, h - 4f, 180f, 6f);
                Passage(StalkerLinkKind.EscaladeRebord, new Vector3(sx * 16f, 0f, h - 16f), new Vector3(sx * 16f, 6f, h - 6f));
                ArtPiece("SM_Mod_Rebord_6m", sx * 14f, h - 11.2f, 0f, 180f);
                ArtPiece("SM_Mod_LitsSuperposes", sx * (h - 6f), h - 3f, 6f, 180f);
            }

            Passage(StalkerLinkKind.Bond, new Vector3(11f, 6f, h - 6f), new Vector3(-11f, 6f, h - 6f));
            ArtPiece("SM_Mod_Balcon_6m", 0f, h - 11.6f, 6f, 180f);

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
                ArtPiece("SM_Mod_PasserellePilotis_8m", x, 15f, -7f, 90f, 0.7f);
            }

            // Piliers et arches brisés sur les rives.
            foreach (float sz in new[] { 1f, -1f })
                foreach (float x in new[] { -26f, -16f, 16f, 26f })
                {
                    float z = sz > 0f ? 24.5f : 6.5f;                       // sur les rives du canal (8 à 22)
                    if (sz < 0f)
                        Pilier(x, z);                                         // rive nord étroite : pas de pilier (le traqueur doit y passer)
                    if (x < 0 == sz > 0)
                        ArtPiece("SM_Mod_ArcheBrisee_8m", x, z, 0f, 90f);
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
                ArtPiece("SM_Mod_Coursive_45", sx * (h - 4f), sz * (h - 4f), 11f, q * 90f);
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
