using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Arène du combat de boss (D-049), derrière le palier de sortie : 80 × 80 m, 30 m de haut. Trois pièges lisibles (cible
    /// qui brille, zone orange au sol) : herse (seul passage vers la poche ouest, murs jusqu'au plafond), pont qui
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

            // Herse : arche à l'ouest (piliers de 28 m, passage de 10 m), grille suspendue de 16 à 28 m.
            Vector3 hc = c + new Vector3(-25f, 0f, 0f);
            Piece("Herse : pilier sud", hc + new Vector3(0f, 14f, -6.5f), new Vector3(3f, 28f, 3f), _wall, walkable: false);
            Piece("Herse : pilier nord", hc + new Vector3(0f, 14f, 6.5f), new Vector3(3f, 28f, 3f), _wall, walkable: false);
            // Murs de la poche ouest, jusqu'au plafond : seul passage, sous la herse (sa main ne passe pas par-dessus).
            Piece("Herse : mur de la poche (nord)", c + new Vector3(-25f, H / 2f, (8f + h) / 2f), new Vector3(1.5f, H, h - 8f), _wall, walkable: false);
            Piece("Herse : mur de la poche (sud)", c + new Vector3(-25f, H / 2f, -(8f + h) / 2f), new Vector3(1.5f, H, h - 8f), _wall, walkable: false);
            GameObject gate = Box("Herse : grille", root, hc + new Vector3(0f, 22f, 0f), new Vector3(1.2f, 12f, 10f), iron, collider: true);
            GameObjectUtility.SetStaticEditorFlags(gate, 0);
            gate.AddComponent<NotWalkable>();
            BuildTrap("Herse", root, TrapKind.Herse, hc + Vector3.up * 7f, new Vector3(8f, 14f, 10f), gate.transform, 16f,
                hc + new Vector3(0f, 7f, -8.6f), tt, glow, zoneMat);

            // Pont : tablier de 12 m sur la fosse (le traqueur y passe), s'effondre.
            GameObject deck = Box("Pont : tablier", root, c + new Vector3((px0 + px1) / 2f, -0.5f, 0f), new Vector3(px1 - px0 + 2f, 1f, 12f), _bridge, collider: true);
            GameObjectUtility.SetStaticEditorFlags(deck, 0);
            Piece("Pont : poteau de la clé", c + new Vector3(px0 - 2f, 1.5f, -7.5f), new Vector3(1f, 3f, 1f), _wall);
            BuildTrap("Pont", root, TrapKind.Pont, c + new Vector3((px0 + px1) / 2f, 7f, 0f), new Vector3(px1 - px0, 14f, 12f), deck.transform, 0f,
                c + new Vector3(px0 - 2f, 3.6f, -7.5f), tt, glow, zoneMat);

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
                winch, tt, glow, zoneMat);

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
            Vector3 targetAt, TrainingDummySettings targetSettings, Material glow, Material zoneMat)
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
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
