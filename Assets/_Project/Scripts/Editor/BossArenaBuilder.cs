using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Arène du combat de boss (D-049), derrière le palier de sortie : 80 × 80 m, 30 m de haut. Trois pièges lisibles (cible
    /// qui brille, zone orange au sol) : herse (seul passage vers la poche ouest de 22 m, murs jusqu'au plafond), pont qui
    /// s'effondre (seul passage vers la poche est, au-dessus d'une fosse de 8 m qui traverse l'arène), lustre géant (au centre,
    /// treuil sur le mur nord). Le joueur attire le traqueur dans la poche, ou sous le lustre, puis touche la cible.
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        public const float ArenaSize = 80f, ArenaHeight = 30f;
        private const string TrapTargetPath = SettingsDir + "/TrapTarget.asset";

        /// <summary>Construit l'arène au nord du palier ; ajoute son nœud au graphe ; renvoie son emprise (NavMesh).</summary>
        private static Bounds BossArena(Vector3 landing)
        {
            float h = ArenaSize / 2f, H = ArenaHeight, y = landing.y;
            float z0 = landing.z + 6f + T;
            var c = new Vector3(landing.x, y, z0 + h);
            var root = new GameObject("Arène du combat de boss").transform;
            root.SetParent(_level, false);
            Material arenaFloor = Mat("Arena_BossFloor", new Color(0.2f, 0.17f, 0.2f));

            // Sol plein jusqu'en bas, coupé par la fosse du pont (x 10..28, sur toute la profondeur) : la poche est (x 28..40)
            // ne s'atteint que par le pont (ou à la traction, le traqueur non).
            float px0 = 10f, px1 = 28f, pz = h, pit = 8f;
            void Floor(string n, float xa, float xb, float za, float zb) =>
                Piece($"Arène : sol {n}", new Vector3(c.x + (xa + xb) / 2f, y / 2f, c.z + (za + zb) / 2f), new Vector3(xb - xa, y, zb - za), arenaFloor);
            Floor("ouest", -h - T, px0, -h - T, h + T);
            Floor("est", px1, h + T, -h - T, h + T);
            float pitY = y - pit;
            if (pitY > 0.1f)
                Piece("Arène : fond de la fosse", new Vector3(c.x + (px0 + px1) / 2f, pitY / 2f, c.z), new Vector3(px1 - px0, pitY, 2f * pz), _ceiling, walkable: false);
            else
                Piece("Arène : fond de la fosse", new Vector3(c.x + (px0 + px1) / 2f, pitY - 0.5f, c.z), new Vector3(px1 - px0, 1f, 2f * pz), _ceiling, walkable: false);
            Ramp("Arène : sortie de la fosse (ouest)", new Vector3(c.x + px0 + 2.5f, pitY, c.z - 30f), pit, 22f, 4f, 0f);
            Ramp("Arène : sortie de la fosse (est)", new Vector3(c.x + px1 - 2.5f, pitY, c.z + 30f), pit, 22f, 4f, 180f);

            // Murs (ouverture au sud, vers le palier), plafond, lumières.
            float top = y + H;
            var south = new List<Opening> { new Opening(c.x - ExitDoorWidth / 2f, c.x + ExitDoorWidth / 2f, y, y + ExitDoorHeight) };
            WallLine("Arène : mur sud", true, z0 - T / 2f, c.x - h - T, c.x + h + T, 0f, top, south);
            WallLine("Arène : mur nord", true, c.z + h + T / 2f, c.x - h - T, c.x + h + T, 0f, top, null);
            WallLine("Arène : mur ouest", false, c.x - h - T / 2f, c.z - h, c.z + h, 0f, top, null);
            WallLine("Arène : mur est", false, c.x + h + T / 2f, c.z - h, c.z + h, 0f, top, null);
            Piece("Arène : plafond", new Vector3(c.x, top + 0.5f, c.z), new Vector3(2f * h + 2f * T, 1f, 2f * h + 2f * T), _ceiling, walkable: false);
            for (int a = 0; a < 3; a++)
            for (int b = 0; b < 3; b++)
                Lamp($"Arène : lumière {a * 3 + b + 1}", c + new Vector3((a - 1) * h * 0.6f, H * 0.7f, (b - 1) * h * 0.6f), new Color(1f, 0.55f, 0.35f), 6f, 45f);
            var vol = RoomVolumeAt("Arène du combat de boss", new Vector3(c.x, y + H / 2f, c.z), Quaternion.identity, new Vector3(2f * h, H, 2f * h), RoomKind.Arene);
            vol.transform.SetParent(root, true);

            var tt = LoadOrCreate<TrainingDummySettings>(TrapTargetPath);
            tt.maxHealth = 1f;
            tt.soulValue = 0;
            tt.respawnDelay = 0f;
            EditorUtility.SetDirty(tt);
            Material glow = UnlitMat("Trap_Target", "Universal Render Pipeline/Unlit", new Color(3.5f, 1.4f, 0.2f));
            Material zoneMat = UnlitMat("Trap_Zone", "Universal Render Pipeline/Unlit", new Color(0.9f, 0.35f, 0.05f));
            Material iron = Mat("Trap_Iron", new Color(0.18f, 0.17f, 0.18f));
            Material leverOff = UnlitMat("Trap_Lever_Off", "Universal Render Pipeline/Unlit", new Color(3f, 0.9f, 0.1f));
            Material leverOn = UnlitMat("Trap_Lever_On", "Universal Render Pipeline/Unlit", new Color(0.4f, 3.2f, 0.6f));

            // Étages (après le playtest) : galeries pleines à 6 m le long des murs nord et sud (hors des entrées), tours de 11 m,
            // galerie à 6 m dans la poche est, escaliers ; la fosse se traverse aussi par des plateformes qui tombent (parcours).
            void Blk(string n, float x0, float x1, float z0, float z1, float hgt) =>
                Piece($"Arène : {n}", new Vector3(c.x + (x0 + x1) / 2f, y + hgt / 2f, c.z + (z0 + z1) / 2f), new Vector3(x1 - x0, hgt, z1 - z0), _platform);
            Blk("galerie nord ouest", -16.5f, -8f, h - 5f, h, 6f);
            Blk("galerie nord est", 7f, 9.5f, h - 5f, h, 6f);
            Blk("galerie sud ouest", -16.5f, -8f, -h, -h + 5f, 6f);
            Blk("galerie sud est", 7f, 9.5f, -h, -h + 5f, 6f);
            Blk("tour nord-ouest (11 m)", -16.5f, -10.5f, h - 11f, h - 5f, 11f);
            Blk("tour sud-est (11 m)", 3f, 9.5f, -h + 5f, -h + 11f, 11f);
            Blk("galerie de la poche est", 34f, h, 8f, 30f, 6f);
            void Steps(string n, float x, float z0, float dirZ, float top)
            {
                int k = Mathf.CeilToInt(top / 0.4f);
                for (int i = 0; i < k; i++)
                {
                    float hh = Mathf.Min(top, (i + 1) * 0.4f);
                    Piece($"Arène : {n} {i + 1}", new Vector3(c.x + x, y + hh / 2f, c.z + z0 + dirZ * 0.56f * (i + 0.5f)), new Vector3(3f, hh, 0.56f), _platform);
                }
            }

            Steps("escalier nord", -6.5f, h - 5f - 8.4f, 1f, 6f);
            Steps("escalier sud", -6.5f, -h + 5f + 8.4f, -1f, 6f);
            Steps("escalier de la poche est", 32.5f, 8f - 8.4f, 1f, 6f);
            for (int i = 0; i < 5; i++)
            {
                GameObject plat = NMPiece($"Arène : traversée de la fosse {i + 1}", root, new Vector3(c.x + 12.5f + i * 3.25f, y - 0.25f, c.z + 26f), new Vector3(2.4f, 0.5f, 2.4f));
                GameObjectUtility.SetStaticEditorFlags(plat, 0);
                plat.AddComponent<CrumblePlatform>();
            }

            // Décor (règles du 2 bis) : le long des murs, sur les galeries ; le centre reste libre (lustre).
            void Prop(string id, float x, float z, float yaw = 0f, float up = 0f) => PropLibrary.Place(id, root, new Vector3(c.x + x, y + up, c.z + z), yaw);
            foreach (float x in new[] { -13f, -9.5f, 3f })
            {
                Prop("Torche", x, h - 5.4f, 180f, 3f);
                Prop("Torche", x, -h + 5.4f, 0f, 3f);
            }

            foreach (float z in new[] { -30f, -15f, 15f, 30f })
            {
                Prop("Torche", -17.4f, z, -90f, 4f);
                Prop("Torche", 39.4f, z, -90f, 4f);
                Prop("Torche", -39.4f, z, 90f, 4f);
            }

            Prop("Tonneau", -15.5f, h - 2f, 0f, 6f); Prop("Caisse", -15.5f, h - 3.8f, 0f, 6f); Prop("Banc", -10.5f, h - 1.2f, 0f, 6f);
            Prop("Tonneau", -15.5f, -h + 2f, 0f, 6f); Prop("Etagere", -12f, -h + 0.5f, 0f, 6f);
            Prop("Statue", -15f, h - 9.5f, 180f, 11f); Prop("Statue", 6f, -h + 8f, 0f, 11f);
            Prop("Echafaudage", -12f, 16f); Prop("Echafaudage", -12f, -16f);
            Prop("ColonneBrisee", 0f, 20f, 40f); Prop("ColonneBrisee", 2f, -22f, 120f);
            Prop("Gravats", 36f, 0f); Prop("Cage", 37f, -12f); Prop("PileDeCaisses", 37f, -22f);
            Prop("Tonneau", 37f, 26f, 0f, 6f); Prop("Caisse", 38.5f, 12f, 0f, 6f);
            Prop("Tapisserie", 0f, h - 0.2f, 180f, 8f); Prop("Tapisserie", -30f, h - 0.2f, 180f, 6f);
            Prop("Chaine", -12f, 0f, 0f, 22f); Prop("Lanterne", -12f, 0f, 0f, 21f);
            Prop("Chaine", 4f, 8f, 0f, 22f); Prop("Lanterne", 4f, 8f, 0f, 21f);
            Prop("Grille", -30f, h - 0.3f, 0f); Prop("Grille", -30f, -h + 0.3f, 0f);
            Prop("Tonneau", -36f, 30f); Prop("Caisse", -37f, -30f); Prop("Sarcophage", -33f, 15f); Prop("Sarcophage", -33f, -15f);

            // Leviers : deux par piège, à l'écart du piège et à des hauteurs différentes.
            BossLever Lever(string n, float x, float z, float up, float yaw)
            {
                Vector3 feet = new Vector3(c.x + x, y + up, c.z + z);
                Piece($"Levier {n} : socle", feet + Vector3.up * 0.6f, new Vector3(0.5f, 1.2f, 0.5f), iron, walkable: false);
                GameObject plate = Box($"Levier {n}", root, feet + Vector3.up * 1.6f, new Vector3(1.2f, 0.8f, 1.2f), leverOff, collider: true);
                GameObjectUtility.SetStaticEditorFlags(plate, 0);
                BossLever lev = plate.AddComponent<BossLever>();
                SetRef(lev, "litMaterial", leverOn);
                Lamp($"Levier {n} : lumière", feet + Vector3.up * 2.8f, new Color(1f, 0.7f, 0.2f), 2.5f, 9f);
                return lev;
            }

            BossLever[] herseLevers = { Lever("herse 1 (galerie nord)", -12f, h - 2.5f, 6f, 0f), Lever("herse 2 (galerie sud)", -12f, -h + 2.5f, 6f, 0f) };
            BossLever[] pontLevers = { Lever("pont 1 (poche est, au sol)", 37f, -30f, 0f, 0f), Lever("pont 2 (galerie de la poche)", 38f, 20f, 6f, 0f) };
            BossLever[] lustreLevers = { Lever("lustre 1 (tour nord-ouest)", -12.5f, h - 7f, 11f, 0f), Lever("lustre 2 (tour sud-est)", 6f, -h + 8f - 2f, 11f, 0f) };

            // Herse : arche à l'ouest (piliers de 28 m, passage de 10 m), grille suspendue de 16 à 28 m.
            Vector3 hc = c + new Vector3(-18f, 0f, 0f);                // poche de 22 m : sa main ne passe pas l'arche
            Piece("Herse : pilier sud", hc + new Vector3(0f, 14f, -6.5f), new Vector3(3f, 28f, 3f), _wall, walkable: false);
            Piece("Herse : pilier nord", hc + new Vector3(0f, 14f, 6.5f), new Vector3(3f, 28f, 3f), _wall, walkable: false);
            // Murs de la poche ouest, jusqu'au plafond : seul passage, sous la herse (sa main ne passe pas par-dessus).
            Piece("Herse : mur de la poche (nord)", c + new Vector3(-18f, H / 2f, (8f + h) / 2f), new Vector3(1.5f, H, h - 8f), _wall, walkable: false);
            Piece("Herse : mur de la poche (sud)", c + new Vector3(-18f, H / 2f, -(8f + h) / 2f), new Vector3(1.5f, H, h - 8f), _wall, walkable: false);
            GameObject gate = Box("Herse : grille", root, hc + new Vector3(0f, 22f, 0f), new Vector3(1.2f, 12f, 10f), iron, collider: true);
            GameObjectUtility.SetStaticEditorFlags(gate, 0);
            gate.AddComponent<NotWalkable>();
            BuildTrap("Herse", root, TrapKind.Herse, hc + Vector3.up * 7f, new Vector3(8f, 14f, 10f), gate.transform, 16f,
                hc + new Vector3(0f, 7f, -8.6f), tt, glow, zoneMat, herseLevers);

            // Pont : tablier de 12 m sur la fosse (le traqueur y passe), s'effondre.
            GameObject deck = Box("Pont : tablier", root, c + new Vector3((px0 + px1) / 2f, -0.5f, 0f), new Vector3(px1 - px0 + 2f, 1f, 12f), _bridge, collider: true);
            GameObjectUtility.SetStaticEditorFlags(deck, 0);
            Piece("Pont : poteau de la clé", c + new Vector3(px0 - 2f, 1.5f, -7.5f), new Vector3(1f, 3f, 1f), _wall);
            BuildTrap("Pont", root, TrapKind.Pont, c + new Vector3((px0 + px1) / 2f, 7f, 0f), new Vector3(px1 - px0, 14f, 12f), deck.transform, 0f,
                c + new Vector3(px0 - 2f, 3.6f, -7.5f), tt, glow, zoneMat, pontLevers);

            // Lustre géant au centre, accroché à 24 m ; treuil sur le mur nord (la corde brille).
            GameObject lustre = Box("Lustre géant", root, c + new Vector3(0f, 24f, 0f), new Vector3(12f, 1.5f, 12f), iron, collider: true);
            GameObjectUtility.SetStaticEditorFlags(lustre, 0);
            lustre.AddComponent<NotWalkable>();
            Vector3 winch = c + new Vector3(0f, 6f, h - 1.2f);
            Vector3 hook = c + new Vector3(0f, 24.75f, 0f);
            GameObject rope = Box("Lustre : corde", root, (winch + hook) / 2f, new Vector3(0.25f, 0.25f, Vector3.Distance(winch, hook)), glow, collider: false);
            rope.transform.rotation = Quaternion.LookRotation(winch - hook);
            GameObjectUtility.SetStaticEditorFlags(rope, 0);
            BuildTrap("Lustre", root, TrapKind.Lustre, c + Vector3.up * 7f, new Vector3(12f, 14f, 12f), lustre.transform, 23.25f,
                winch, tt, glow, zoneMat, lustreLevers);

            // Objets à lancer contre les cibles.
            var props = new GameObject("Arène : objets").transform;
            props.SetParent(root, false);
            Vector3[] spots = { new Vector3(-12f, 0f, -20f), new Vector3(-20f, 0f, -18f), new Vector3(-20f, 0f, 20f), new Vector3(5f, 0f, 22f), new Vector3(4f, 0f, -24f), new Vector3(-8f, 0f, 10f), new Vector3(33f, 0f, 24f), new Vector3(33f, 0f, -26f) };
            float[] masses = { 5f, 15f, 5f, 25f, 15f, 5f, 15f, 5f };
            for (int i = 0; i < spots.Length; i++)
            {
                Crate(c + spots[i], masses[i], props);
                Crate(c + spots[i] + new Vector3(1.6f, 0f, 0f), 5f, props);
            }

            // Entrées et combat.
            var pe = new GameObject("Arène : entrée des joueurs").transform;
            pe.SetParent(root, false);
            pe.SetPositionAndRotation(c + new Vector3(0f, 0.1f, -h + 6f), Quaternion.identity);
            var se = new GameObject("Arène : entrée du traqueur").transform;
            se.SetParent(root, false);
            se.SetPositionAndRotation(c + new Vector3(0f, 0f, h - 9f), Quaternion.Euler(0f, 180f, 0f));
            var fight = root.gameObject.AddComponent<BossFight>();
            SetRef(fight, "playerEntry", pe);
            SetRef(fight, "stalkerEntry", se);
            var so = new SerializedObject(fight);
            SerializedProperty list = so.FindProperty("traps");
            BossTrap[] traps = root.GetComponentsInChildren<BossTrap>();
            list.arraySize = traps.Length;
            for (int i = 0; i < traps.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = traps[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Nœud du graphe (cul-de-sac derrière la salle de sortie, par les escaliers).
            Nodes.Add(new Node { Name = "Arène", C = c, Half = h, HalfZ = h, Ceil = H, Hall = true });
            Edges.Add(new Vector2Int(_exitNode, Nodes.Count - 1));
            return new Bounds(c + Vector3.up * (H / 2f), new Vector3(2f * h + 20f, H + 2f * y + 20f, 2f * h + 20f));
        }

        /// <summary>Piège : zone marquée au sol (lueur orange), cible qui brille (un PV), pièce qui bouge.</summary>
        private static void BuildTrap(string name, Transform root, TrapKind kind, Vector3 zoneCenter, Vector3 zoneSize, Transform moving, float drop,
            Vector3 targetAt, TrainingDummySettings targetSettings, Material glow, Material zoneMat, BossLever[] levers = null)
        {
            var go = new GameObject($"Piège : {name}");
            go.transform.SetParent(root, false);
            go.transform.position = zoneCenter;
            float floorY = zoneCenter.y - zoneSize.y / 2f;
            float lift = kind == TrapKind.Pont ? 0.02f : 0.03f;
            GameObject mark = Box($"{name} : zone (au sol)", root, new Vector3(zoneCenter.x, floorY + lift, zoneCenter.z), new Vector3(zoneSize.x, 0.02f, zoneSize.z), zoneMat, collider: false);
            GameObjectUtility.SetStaticEditorFlags(mark, 0);
            if (kind == TrapKind.Pont)
                mark.transform.SetParent(moving, true);                 // la zone du pont disparaît avec le tablier
            GameObject target = Box($"{name} : cible (à toucher)", root, targetAt, Vector3.one * 1.4f, glow, collider: true);
            GameObjectUtility.SetStaticEditorFlags(target, 0);
            var dmg = target.AddComponent<Damageable>();
            var dso = new SerializedObject(dmg);
            dso.FindProperty("settings").objectReferenceValue = targetSettings;
            dso.FindProperty("respawns").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();
            Lamp($"{name} : lumière de la cible", targetAt + Vector3.up * 1.5f, new Color(1f, 0.55f, 0.15f), 3f, 10f);
            var trap = go.AddComponent<BossTrap>();
            var so = new SerializedObject(trap);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("trigger").objectReferenceValue = dmg;
            so.FindProperty("zoneSize").vector3Value = zoneSize;
            so.FindProperty("moving").objectReferenceValue = moving;
            so.FindProperty("dropHeight").floatValue = drop;
            SerializedProperty lp = so.FindProperty("levers");
            lp.arraySize = levers != null ? levers.Length : 0;
            for (int i = 0; levers != null && i < levers.Length; i++)
                lp.GetArrayElementAtIndex(i).objectReferenceValue = levers[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
