using System.Collections.Generic;
using System.Linq;
using ItHunts.Economy;
using ItHunts.Enemies;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    public sealed partial class ActLevel
    {
        /// <summary>Case libre (sol, rien posé) de la salle, au hasard ; marquée occupée.</summary>
        private bool FreeCell(ActRoom r, out Vector2Int cell, bool allowTerrace = false, int clearance = 1)
        {
            var options = new List<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, CellUse> kv in r.Use)
            {
                if (kv.Value != CellUse.Sol && !(allowTerrace && kv.Value == CellUse.Terrasse))
                    continue;
                if (Occupied(kv.Key))
                    continue;
                bool ok = true;
                for (int dx = -clearance; dx <= clearance && ok; dx++)
                for (int dy = -clearance; dy <= clearance && ok; dy++)
                    ok = r.Use.ContainsKey(kv.Key + new Vector2Int(dx, dy));
                if (ok)
                    options.Add(kv.Key);
            }

            if (options.Count == 0)
            {
                cell = default;
                return false;
            }

            cell = options[_rng.Next(options.Count)];
            _occupied.Add(cell);
            return true;
        }

        private readonly HashSet<Vector2Int> _occupied = new HashSet<Vector2Int>();
        private bool Occupied(Vector2Int c) => _occupied.Contains(c);

        private float GroundAt(ActRoom r, Vector2Int c) =>
            r.Use.TryGetValue(c, out CellUse u) && u == CellUse.Terrasse ? r.Floor + r.TerraceHeight : r.Floor;

        private void PlaceGameplay()
        {
            Transform game = Group("Jeu du niveau");
            ActRoom start = Plan.Rooms[Plan.Start];
            // Départ : au milieu de la salle (cases réservées), regard vers la porte la plus proche du chemin.
            Vector3 startAt = Plan.CellCenter(Vector2Int.RoundToInt(start.CenterCell), start.Floor) + Vector3.up * 0.1f;
            if (start.Use.TryGetValue(Vector2Int.RoundToInt(start.CenterCell), out CellUse su) && su == CellUse.Terrasse)
                startAt.y += start.TerraceHeight;
            ActDoor firstDoor = start.Doors.Select(i => Plan.Doors[i]).FirstOrDefault(d => !d.Gate);
            float yaw = 45f;
            if (firstDoor != null)
            {
                Vector3 to = Plan.DoorCenter(firstDoor) - startAt;
                yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            }

            if (playerStart != null)
                playerStart.SetPositionAndRotation(startAt, Quaternion.Euler(0f, yaw, 0f));
            foreach (Player.PlayerHealth p in FindObjectsByType<Player.PlayerHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                p.transform.SetPositionAndRotation(startAt, Quaternion.Euler(0f, yaw, 0f));

            StartLandmark(game, start, startAt);
            // Marchand de l'artefact : salle de départ, face au joueur.
            if (artifactMerchant != null)
            {
                Vector3 m = startAt + Quaternion.Euler(0f, yaw + 35f, 0f) * Vector3.forward * 9f;
                m.y = start.Floor;
                GameObject am = Instantiate(artifactMerchant, m, Quaternion.LookRotation(startAt - m, Vector3.up), game);
                am.name = "Marchand de l'artefact (salle de départ)";
                DimMerchant(am);
            }

            int[] depth = Plan.Depths(Plan.Start);
            // Marchand des améliorations : salle calme à mi-chemin (sans creux), au centre.
            var calm = Plan.Rooms.Where(r => !r.Special && r.Role == RoomRole.Normale && r.Feature == RoomFeature.Aucun)
                .OrderBy(r => Mathf.Abs(depth[r.Id] - Plan.StartExitDepth * 0.5f)).ToList();
            for (int k = 0; k < settings.merchants && k < calm.Count && merchant != null; k++)
            {
                ActRoom r = calm[k * Mathf.Max(1, calm.Count / Mathf.Max(1, settings.merchants))];
                if (!FreeCell(r, out Vector2Int c, clearance: 2))
                    continue;
                Vector3 at = Plan.CellCenter(c, r.Floor);
                Vector3 look = Plan.CellCenter(Vector2Int.RoundToInt(r.CenterCell), r.Floor);
                Quaternion rot = (look - at).sqrMagnitude > 1f ? Quaternion.LookRotation(look - at, Vector3.up) : Quaternion.identity;
                GameObject mm = Instantiate(merchant, at, rot, game);
                mm.name = $"Marchand : {r.Name}";
                DimMerchant(mm);
                MarkRoom(r, "Marchand");
            }

            PlaceChests(game);
            PlaceStalker();
            PlaceResidents();
            PlaceSpawns(game);
            PlaceVolumes(game);
            if (moon != null && settings.moonlight > 0f)
            {
                moon.intensity = settings.moonlight;
                moon.color = settings.moonColor;
            }
        }

        /// <summary>Panneau admin : une goule (ou un volant) apparaît ici.</summary>
        public void SpawnEnemy(bool isFlyer, Vector3 at)
        {
            GameObject prefab = isFlyer ? flyer : ghoul;
            if (prefab == null)
                return;
            Transform parent = _residents != null ? _residents : _root;
            Instantiate(prefab, at, Quaternion.Euler(0f, _rng.Next(360), 0f), parent).name = isFlyer ? "Volant (admin)" : "Goule (admin)";
        }

        /// <summary>
        /// Marchand dans la DA (nuit 3, point 1) : plus d'enseigne au néon violet ni de lumière violette qui teint la salle ; une lanterne
        /// chaude à côté de lui. Son interface (offres, prix) reste telle quelle.
        /// </summary>
        private static void DimMerchant(GameObject m)
        {
            Transform sign = m.transform.Find("Enseigne");
            if (sign != null)
                sign.gameObject.SetActive(false);
            foreach (Light l in m.GetComponentsInChildren<Light>(true))
            {
                l.color = new Color(1f, 0.66f, 0.36f);
                l.intensity = 2.2f;
                l.range = 9f;
            }

            // Silhouette : silhouette encapuchonnée sombre (plus de bloc violet lumineux) en attendant le modèle de la DA.
            Transform body = m.transform.Find("Marchand (silhouette)");
            if (body != null && body.TryGetComponent(out Renderer rb))
            {
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", new Color(0.06f, 0.05f, 0.07f));
                block.SetColor("_EmissionColor", Color.black);
                rb.SetPropertyBlock(block);
            }
        }

        private readonly Dictionary<int, string> _roomTags = new Dictionary<int, string>();
        private void MarkRoom(ActRoom r, string tag) => _roomTags[r.Id] = tag;

        private void PlaceChests(Transform game)
        {
            if (chest == null)
                return;
            // Ancres : terrasses (difficiles), îlots (difficiles), sols des salles (faciles) ; jamais deux dans la même salle.
            var rooms = Plan.Rooms.Where(r => r.Role == RoomRole.Normale || r.Role == RoomRole.Sortie).OrderBy(_ => _rng.Next()).ToList();
            int placed = 0;
            foreach (ActRoom r in rooms)
            {
                if (placed >= settings.chests)
                    break;
                Vector2Int c;
                int difficulty = 0;
                var hard = r.Use.Where(kv => (kv.Value == CellUse.Terrasse || kv.Value == CellUse.Ilot) && !Occupied(kv.Key)).Select(kv => kv.Key).ToList();
                if (hard.Count > 0 && _rng.NextDouble() < 0.7)
                {
                    c = hard[_rng.Next(hard.Count)];
                    _occupied.Add(c);
                    difficulty = 2;
                }
                else if (!FreeCell(r, out c, clearance: 1))
                    continue;

                float y = r.Use[c] == CellUse.Terrasse ? r.Floor + r.TerraceHeight : r.Use[c] == CellUse.Ilot && r.Feature == RoomFeature.Vide ? r.Floor + 0.6f
                    : r.Use[c] == CellUse.Ilot ? r.Floor + 0.4f : r.Floor;
                Vector3 at = Plan.CellCenter(c, y);
                Vector3 look = Plan.CellCenter(Vector2Int.RoundToInt(r.CenterCell), y);
                GameObject g = Instantiate(chest, at, Quaternion.LookRotation((look - at).sqrMagnitude > 0.5f ? look - at : Vector3.forward, Vector3.up), game);
                g.name = $"Coffre : {r.Name}";
                if (difficulty >= 2 && _rng.NextDouble() < 0.4 && g.TryGetComponent(out Chest ch))
                    ch.Rare = true;
                placed++;
            }

            // Salles verrouillées : coffre gratuit (rare) au fond.
            foreach (ActRoom r in Plan.Rooms.Where(r => r.Role == RoomRole.Verrouillee))
            {
                ActDoor d = Plan.Doors[r.Doors[0]];
                Vector3 door = Plan.DoorCenter(d);
                Vector3 inward = (Plan.CellCenter(r.CenterCell, r.Floor) - door);
                inward.y = 0f;
                Vector3 at = Plan.CellCenter(r.CenterCell, r.Floor) + inward.normalized * 2f;
                GameObject g = Instantiate(freeChest != null ? freeChest : chest, at, Quaternion.LookRotation(-inward, Vector3.up), game);
                g.name = "Coffre de la salle verrouillée";
                if (g.TryGetComponent(out Chest ch))
                    ch.Rare = true;
            }
        }

        private void PlaceStalker()
        {
            if (stalker == null)
                return;
            int room = Plan.Arrival >= 0 ? Plan.Arrival : Plan.Lair;
            if (room < 0)
            {
                // Sans repaire (arène du boss) : contre le mur opposé au départ.
                ActRoom st = Plan.Rooms[Plan.Start];
                stalker.position = Plan.CellCenter(st.CenterCell, st.Floor) + new Vector3(0f, 0f, st.Box.height * Plan.Cell * 0.3f);
                return;
            }

            ActRoom r = Plan.Rooms[room];
            ActDoor d = Plan.Doors[r.Doors[0]];
            Vector3 door = Plan.DoorCenter(d);
            Vector3 c = Plan.CellCenter(r.CenterCell, r.Floor);
            Vector3 inward = c - door;
            inward.y = 0f;
            stalker.position = c + inward.normalized * 2f;
            stalker.rotation = Quaternion.LookRotation(-inward.normalized, Vector3.up);
        }

        /// <summary>Goules habitantes : selon la surface (aucune salle vide), hors du départ ; volants dans les salles hautes.</summary>
        private void PlaceResidents()
        {
            var parent = Group("Habitants des salles");
            parent.gameObject.SetActive(false);
            _residents = parent;
            GhoulsPlaced = 0;
            foreach (ActRoom r in Plan.Rooms)
            {
                if (r.Special || r.Role == RoomRole.Depart)
                    continue;
                float area = r.Use.Count(kv => kv.Value != CellUse.Creux) * Plan.Cell * Plan.Cell;
                int g = Mathf.Clamp(Mathf.RoundToInt(area / 100f * settings.ghoulsPer100 * RunSession.EnemyCount), settings.minGhoulsPerRoom, settings.maxGhoulsPerRoom);
                for (int k = 0; k < g && ghoul != null; k++)
                {
                    if (!FreeCell(r, out Vector2Int c, allowTerrace: true, clearance: 0))
                        break;
                    Instantiate(ghoul, Plan.CellCenter(c, GroundAt(r, c) + 0.05f), Quaternion.Euler(0f, _rng.Next(360), 0f), parent).name = $"Goule habitante, {r.Name} {k + 1}";
                    GhoulsPlaced++;
                }

                bool tall = r.Ceiling <= 0f || r.Ceiling >= 18f;
                if (tall && flyer != null)
                    for (int k = 0; k < settings.flyersPerTallRoom; k++)
                    {
                        Vector3 at = Plan.CellCenter(r.CenterCell, r.Floor + Mathf.Min(10f, (r.Ceiling > 0f ? r.Ceiling : 20f) * 0.6f));
                        Instantiate(flyer, at + new Vector3(k * 4f, 0f, 0f), Quaternion.identity, parent).name = $"Volant habitant, {r.Name} {k + 1}";
                    }
            }
        }

        private void PlaceSpawns(Transform game)
        {
            var parent = Group("Points d'apparition", game);
            foreach (ActRoom r in Plan.Rooms)
            {
                if (r.Special || r.Role == RoomRole.Depart)
                    continue;
                for (int k = 0; k < settings.spawnPointsPerRoom; k++)
                {
                    if (!FreeCell(r, out Vector2Int c, clearance: 0))
                        break;
                    _occupied.Remove(c);
                    var go = new GameObject("Apparition : goule");
                    go.transform.SetParent(parent, false);
                    go.transform.position = Plan.CellCenter(c, r.Floor + 0.1f);
                    go.AddComponent<SpawnPoint>().Configure(SpawnKinds.Goule);
                }

                if (r.Ceiling <= 0f || r.Ceiling >= 16f)
                {
                    var go = new GameObject("Apparition : volant");
                    go.transform.SetParent(parent, false);
                    go.transform.position = Plan.CellCenter(r.CenterCell, r.Floor + 9f);
                    go.AddComponent<SpawnPoint>().Configure(SpawnKinds.Volant);
                }
            }
        }

        /// <summary>Volumes de salle (vagues, minimap, salle de départ) : un par rectangle de la salle.</summary>
        private void PlaceVolumes(Transform game)
        {
            var parent = Group("Volumes des salles", game);
            foreach (ActRoom r in Plan.Rooms)
            {
                float h = r.Ceiling > 0f ? r.Ceiling : 30f;
                RoomKind kind = r.Role == RoomRole.Depart ? RoomKind.Depart : RoomKind.Salle;
                foreach (RectInt rc in Rects(new HashSet<Vector2Int>(r.Cells)))
                {
                    var go = new GameObject($"Volume de salle : {r.Name}");
                    go.transform.SetParent(parent, false);
                    go.transform.position = RectCenter(rc, r.Floor - 2f + (h + 2f) * 0.5f);
                    go.AddComponent<RoomVolume>().Configure(r.Name, RectSize(rc, h + 2f), kind);
                }
            }
        }

        private void ConfigureScene()
        {
            int n = Plan.Rooms.Count;
            var names = new string[n];
            var centers = new Vector3[n];
            var halves = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                ActRoom r = Plan.Rooms[i];
                names[i] = r.Name;
                // Centre : une case de sol de la salle proche du centre de gravité (pas le milieu d'un creux).
                Vector2Int best = r.Cells.OrderBy(c => r.Use[c] == CellUse.Creux ? 1e6f : (c - r.CenterCell).sqrMagnitude).First();
                centers[i] = Plan.CellCenter(best, r.Floor);
                float h = r.Ceiling > 0f ? r.Ceiling : 30f;
                halves[i] = new Vector3(r.Box.width * Plan.Cell * 0.5f, h * 0.5f, r.Box.height * Plan.Cell * 0.5f);
            }

            var edges = Plan.Doors.Select(d => new Vector2Int(d.A, d.B)).ToArray();
            var doors = Plan.Doors.Select(d => Plan.DoorCenter(d)).ToArray();
            if (graph != null)
            {
                graph.Configure(names, centers, halves, edges);
                var owner = new int[Plan.Size * Plan.Size];
                for (int z = 0; z < Plan.Size; z++)
                for (int x = 0; x < Plan.Size; x++)
                    owner[z * Plan.Size + x] = Plan.Owner[x, z];
                graph.ConfigureCells(Plan.Origin, Plan.Cell, Plan.Size, owner, doors);
            }

            if (zoneNav != null)
            {
                Vector3 min = _min - new Vector3(10f, 5f, 10f), max = _max + new Vector3(10f, 5f, 10f);
                min.y = Mathf.Max(min.y, settings.minFloor - 12f);
                zoneNav.Configure((min + max) * 0.5f, max - min);
            }
        }

        // ---------- Portes particulières ----------

        /// <summary>Battants d'une porte classique (bois sombre, ferrures) dans l'ouverture ; renvoie le parent des battants.</summary>
        private Transform DoorLeaves(Transform parent, string name, Vector3 c, Vector3 n, float w, float sill, float openTop)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = new Vector3(c.x, sill, c.z);
            root.rotation = Quaternion.LookRotation(n, Vector3.up);
            // Le NavMesh (construit au départ, portes fermées) ignore les battants : une fois la porte payée, traqueur et goules
            // suivent le joueur dans la salle (sinon une cachette).
            root.gameObject.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
            float h = openTop - sill;
            Material wood = settings.propMaterial != null ? settings.propMaterial : settings.wallMaterial;
            Material iron = settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial;
            foreach (float side in new[] { -1f, 1f })
            {
                var leaf = new GameObject(side < 0 ? "Battant gauche" : "Battant droit");
                leaf.transform.SetParent(root, false);
                leaf.transform.localPosition = new Vector3(side * w * 0.25f, h * 0.5f, 0f);
                leaf.AddComponent<BoxCollider>().size = new Vector3(w * 0.5f, h, 0.5f);
                Visual(leaf.transform, "Planches", CubeMesh, wood, leaf.transform.position, root.rotation, new Vector3(w * 0.5f - 0.05f, h, 0.45f), merge: false);
                for (int k = 0; k < 3; k++)
                    Visual(leaf.transform, "Ferrure", CubeMesh, iron, leaf.transform.position + Vector3.up * ((k - 1) * h * 0.33f), root.rotation,
                        new Vector3(w * 0.48f, 0.25f, 0.55f), merge: false);
            }

            return root;
        }

        private void BuildExitDoor(Transform parent, ActDoor d, Vector3 c, Vector3 n, float w, float sill, float openTop)
        {
            // Normale vers le hall de sortie.
            Vector3 toHall = Plan.Rooms[d.B].Role == RoomRole.HallDeSortie ? n : -n;
            Transform door;
            if (Art != null && Art.exitDoor != null)
            {
                // Porte de sortie du kit : encadrement et battants qui pivotent à leurs gonds (sans lumière colorée).
                GameObject kd = KitPiece(Art.exitDoor, parent, new Vector3(c.x, sill, c.z) - toHall * 0.55f, -toHall, Vector3.one, true);
                kd.name = "Porte de sortie";
                foreach (Collider col in kd.GetComponentsInChildren<Collider>(true))
                    if (col.transform == kd.transform)
                        Destroy(col);                                     // l'encadrement : les murs portent déjà la collision
                kd.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
                door = kd.transform;
            }
            else
                door = DoorLeaves(parent, "Porte de sortie", c, toHall, w, sill, openTop);
            ExitDoorObject = door.gameObject.AddComponent<ExitDoor>();
            ExitPoint = new Vector3(c.x, sill, c.z) - toHall * 3f;
            // Hall : déclencheur de fin d'acte au fond.
            ActRoom hall = Plan.Rooms[Plan.ExitHall];
            var trig = new GameObject("Fin de l'acte (déclencheur)");
            trig.transform.SetParent(_roomRoots[hall.Id], false);
            trig.transform.position = Plan.CellCenter(hall.CenterCell, hall.Floor + 3f) + toHall * 2f;
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(10f, 6f, 6f);
            trig.AddComponent<ZoneExit>();
            // Au-delà de la porte : une lueur froide (on devine la suite), pas une lampe de couleur vive.
            PointLight(_roomRoots[hall.Id], Plan.CellCenter(hall.CenterCell, hall.Floor + 6f), settings.theme == ActTheme.Jardin ? new Color(0.5f, 0.6f, 0.9f)
                : new Color(0.75f, 0.7f, 0.9f), 2.5f, 18f);
            if (Art != null && Art.exitHallPiece != null)
                KitPiece(Art.exitHallPiece, _roomRoots[hall.Id], Plan.CellCenter(hall.CenterCell, hall.Floor) - toHall * 6f, toHall, Vector3.one, true)
                    .name = "Descente vers l'arène";
            else if (settings.theme == ActTheme.Jardin)
                ExitDescent(hall, toHall);
        }

        /// <summary>Jardin : le hall de sortie est le haut d'un grand escalier qui descend vers l'arène du boss (en contrebas).</summary>
        private void ExitDescent(ActRoom hall, Vector3 dir)
        {
            Transform root = _roomRoots[hall.Id];
            Vector3 c = Plan.CellCenter(hall.CenterCell, hall.Floor);
            KitBuild.Ramp(root, "Descente vers l'arène", c + dir * 6f - Vector3.up * 5f, -dir, 9f, 5f, 8f, settings.kit);
            Visual(root, "Brume du contrebas", CubeMesh, settings.ceilingMaterial != null ? settings.ceilingMaterial : settings.wallMaterial,
                c + dir * 8f - Vector3.up * 5.4f, Quaternion.identity, new Vector3(10f, 0.2f, 6f));
        }

        private void BuildLockedDoor(Transform parent, ActDoor d, Vector3 c, Vector3 n, float w, float sill, float openTop)
        {
            Vector3 inward = Plan.Rooms[d.B].Role == RoomRole.Verrouillee ? n : -n;
            Transform door = DoorLeaves(parent, "Porte de la salle verrouillée", c, inward, w, sill, openTop);
            var ld = door.gameObject.AddComponent<LockedDoor>();
            if (economy != null)
                ld.Configure(economy);
        }

        [SerializeField] private ItHunts.Settings.EconomySettings economy;

        private void BuildArrivalGate(Transform parent, ActDoor d, Vector3 c, Vector3 n, float w, float sill, float openTop)
        {
            Vector3 toVestibule = Plan.Rooms[d.B].Role == RoomRole.Arrivee ? n : -n;
            var root = new GameObject("Herse d'arrivée du traqueur").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(c.x, sill, c.z);
            root.rotation = Quaternion.LookRotation(toVestibule, Vector3.up);
            float h = openTop - sill;
            Material iron = settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial;
            var bars = new GameObject("Barreaux").transform;
            bars.SetParent(root, false);
            var col = bars.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(w, h, 0.4f);
            col.center = new Vector3(0f, h * 0.5f, 0f);
            bars.gameObject.AddComponent<StalkerPassesThrough>();
            int count = Mathf.RoundToInt(w / 0.6f);
            for (int i = 0; i <= count; i++)
                Visual(bars, "Barreau", CubeMesh, iron, root.TransformPoint(new Vector3(-w * 0.5f + i * w / count, h * 0.5f, 0f)), root.rotation, new Vector3(0.14f, h, 0.14f), merge: false);
            for (int k = 1; k <= 4; k++)
                Visual(bars, "Traverse", CubeMesh, iron, root.TransformPoint(new Vector3(0f, h * k / 5f, 0f)), root.rotation, new Vector3(w, 0.16f, 0.18f), merge: false);
            var gate = root.gameObject.AddComponent<ArrivalGate>();
            gate.Configure(bars, h + 1f);
            ArrivalDoorPoint = new Vector3(c.x, sill, c.z);
        }
    }
}
