using System.Collections.Generic;
using ItHunts.Level;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Décor du niveau (étape 8, après playtest, 2 bis) : ameublement des pièces annexes et décor des couloirs. Règle de
    /// circulation : dans un couloir, le décor reste dans une bande de 3 m le long de chaque mur (voie centrale d'au moins
    /// minLane m) ; jamais devant une ouverture (porte d'annexe, passage, galerie). Torches tous les 12 m.
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        private static int _decorCount;

        private static readonly string[] SideProps = { "Tonneau", "Caisse", "PileDeCaisses", "Etagere", "Banc", "Gravats", "CoffreDeco", "Tonneau", "Caisse", "ColonneBrisee", "Cage", "Statue", "Armoire", "Tonneau_Attrapable", "Caisse_Attrapable" };

        /// <summary>Meuble une annexe selon son type (chambre, cellule, réserve, cuisine, armurerie, chapelle, crypte).</summary>
        private static void FurnishAnnex(RoomDef r, string type)
        {
            RoomFrame(r, out Vector3 face, out Vector3 into, out Vector3 along, out float y);
            float R = L.roomSize;
            Vector3 origin = face + Vector3.up * y;
            float back = R - 1.2f, wallYaw = Mathf.Atan2(-into.x, -into.z) * Mathf.Rad2Deg, sideYaw = Mathf.Atan2(along.x, along.z) * Mathf.Rad2Deg;
            var parent = new GameObject($"Mobilier : {r.Name}").transform;
            parent.SetParent(_level, false);
            void At(string id, float u, float v, float yaw, float up = 0f) =>
                PropLibrary.Place(id, parent, origin + along * u + into * v + Vector3.up * up, yaw);
            switch (type)
            {
                case "chambre":
                    At("Lit", -3.5f, back - 1.2f, wallYaw);
                    At("Armoire", 3.8f, back, wallYaw);
                    At("Table", 3f, 5f, sideYaw);
                    At("Chaise_Attrapable", 1.5f, 5f, 0f);
                    At("Tapisserie", 0f, R - 0.1f, wallYaw, 1f);
                    break;
                case "cellule":
                    At("Paille", -3f, back - 1f, 0f);
                    At("Chaine", 0f, R - 0.3f, wallYaw, 1.5f);
                    At("Chaine", 3f, R - 0.3f, wallYaw, 1.5f);
                    At("Tonneau_Attrapable", 4f, 3f, 0f);
                    At("Cage", 3.2f, back - 1.5f, 0f);
                    break;
                case "réserve":
                    At("Etagere", -R / 2f + 0.5f, 6f, sideYaw + 90f);
                    At("Etagere", R / 2f - 0.5f, 6f, sideYaw - 90f);
                    At("PileDeCaisses", -3f, back - 0.5f, 0f);
                    At("Tonneau", 2f, back, 0f);
                    At("Tonneau", 3.2f, back, 0f);
                    At("Caisse_Attrapable", 1f, 4f, 0f);
                    break;
                case "cuisine":
                    At("Table", 0f, 6f, sideYaw);
                    At("Chaudron", -3.5f, back - 0.8f, 0f);
                    At("Etagere", 3f, back + 0.4f, wallYaw);
                    At("Tonneau_Attrapable", 4.5f, 3f, 0f);
                    break;
                case "armurerie":
                    At("Ratelier", -R / 2f + 0.4f, 6f, sideYaw + 90f);
                    At("Ratelier", R / 2f - 0.4f, 6f, sideYaw - 90f);
                    At("Table", 0f, back - 1f, wallYaw);
                    At("Caisse_Attrapable", -2f, 3.5f, 0f);
                    break;
                case "chapelle":
                    At("Autel", 0f, back - 0.4f, wallYaw);
                    At("Banc", -2f, 5f, wallYaw);
                    At("Banc", 2.2f, 5f, wallYaw);
                    At("Statue", -4.5f, back - 0.5f, wallYaw);
                    break;
                default:
                    At("Sarcophage", -2.5f, 7f, sideYaw);
                    At("Sarcophage", 2.5f, 7f, sideYaw);
                    At("ColonneBrisee", 4.5f, back - 1f, 30f);
                    At("Statue", -4.5f, back - 0.4f, wallYaw);
                    break;
            }

            _decorCount += parent.childCount;
        }

        /// <summary>
        /// Décor d'un couloir plat : props dans une bande de 3 m le long des murs, colonnes engagées tous les 16 m, torches tous
        /// les 12 m (en quinconce), jamais devant une ouverture.
        /// </summary>
        private static void CorridorDecor(int e, float minLane)
        {
            CorridorEnds(e, out Vector3 start, out Vector3 end, out float ya, out _, out Vector3 dir, out bool alongX);
            float len = Vector3.Distance(start, end), w = Width(e);
            var parent = new GameObject($"Décor : {CorridorName(e)}").transform;
            parent.SetParent(_level, false);
            Vector3 lat = alongX ? Vector3.forward : Vector3.right;               // côté +1 du couloir
            float strip = Mathf.Min(3f, (w - minLane) / 2f);
            bool slope = IsSlope(e);
            bool Blocked(int side, float t, float margin)
            {
                Vector3 p = Vector3.Lerp(start, end, t);
                float a = alongX ? p.x : p.z;
                if (CorridorOpenings.TryGetValue((e, side), out List<Opening> list))
                    foreach (Opening o in list)
                        if (a > o.A - margin && a < o.B + margin)
                            return true;
                return false;
            }

            int k = 0;
            for (float d = 5f; d < len - 5f; d += 6f, k++)
            {
                float t = d / len;
                foreach (int side in new[] { 1, -1 })
                {
                    if (Blocked(side, t, 2.5f))
                        continue;
                    Vector3 basePos = CorridorPoint(e, t);
                    float yaw = Mathf.Atan2(-lat.x * side, -lat.z * side) * Mathf.Rad2Deg;   // face au couloir
                    Vector3 wallFoot = basePos + lat * side * (w / 2f - strip / 2f);
                    if (!slope && (k + (side > 0 ? 0 : 1)) % 2 == 0)
                    {
                        string id = SideProps[_rng.Next(SideProps.Length)];
                        PropLibrary.Def def = PropLibrary.Get(id);
                        float depth = def != null ? Mathf.Min(def.Size.x, def.Size.z) : 1f;
                        PropLibrary.Place(id, parent, basePos + lat * side * (w / 2f - Mathf.Min(strip, depth) / 2f - 0.2f), yaw + 90f * (id == "Etagere" || id == "Armoire" || id == "Banc" ? 1f : 0f));
                        _decorCount++;
                    }

                    if (!slope && k % 3 == 1)
                    {
                        PropLibrary.Place("Colonne", parent, basePos + lat * side * (w / 2f - 0.75f), 0f);
                        _decorCount++;
                    }

                    if (k % 2 == (side > 0 ? 0 : 1))
                    {
                        PropLibrary.Place("Torche", parent, basePos + lat * side * (w / 2f - 0.35f) + Vector3.up * 4f, yaw);
                        _decorCount++;
                    }
                }
            }
        }
    }
}
