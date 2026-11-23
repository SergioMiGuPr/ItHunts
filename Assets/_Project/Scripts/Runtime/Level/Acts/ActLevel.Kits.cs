using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Habillage par les kits d'art (PR #36 à #39, nuit 3) : murs extérieurs couverts de pièces du kit (parois de roche, murs de
    /// château, lisière de forêt), murs entre salles du jardin en haies, fenêtres hautes avec leur rayon de lune, lustres, torches,
    /// porte de sortie à battants, repère de la salle de départ, ciel et brume. Les collisions restent celles des volumes simples
    /// (prévisibles pour le NavMesh et le mouvement) : les pièces du kit sont visuelles, leurs collisions sont retirées.
    /// </summary>
    public sealed partial class ActLevel
    {
        private ActKit Art => settings.art;

        /// <summary>Pièce du kit sans collision (visuel), pivot posé là, face +Z tournée vers forward.</summary>
        private GameObject KitPiece(GameObject prefab, Transform parent, Vector3 at, Vector3 forward, Vector3 scale, bool keepColliders = false)
        {
            if (prefab == null)
                return null;
            forward.y = 0f;
            GameObject go = Instantiate(prefab, at, Quaternion.LookRotation(forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward, Vector3.up), parent);
            go.name = prefab.name;
            go.transform.localScale = scale;
            if (!keepColliders)
                foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                    Destroy(c);
            foreach (Light l in go.GetComponentsInChildren<Light>(true))
            {
                if (l.GetComponent<AmbianceLight>() == null && l.type == LightType.Point)
                    l.gameObject.AddComponent<AmbianceLight>().kind = AmbianceLight.Kind.Torche;
                // Nuit 4 (perf) : les 100 et quelques torches d'un acte ne projettent pas d'ombre (l'atlas d'ombres URP saturait :
                // « Reduced additional punctual light shadows resolution by 8 » à chaque image).
                if (l.type == LightType.Point || l.type == LightType.Spot)
                    l.shadows = LightShadows.None;
            }

            return go;
        }

        private int _wallRun;

        /// <summary>Mur extérieur droit : pièces du kit en file, face vers la salle, à la longueur et à la hauteur du mur.</summary>
        private void DressOuterWall(Transform root, Vector3 pa, Vector3 pb, Vector3 inward, float floor, float top)
        {
            if (Art == null || Art.outerWalls.Count == 0)
                return;
            float len = Vector3.Distance(pa, pb);
            if (len < 5f)
                return;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / Art.wallPieceWidth));
            float w = len / n;
            float h = Mathf.Min(top - floor, 40f);
            Vector3 dir = (pb - pa) / len;
            for (int k = 0; k < n; k++)
            {
                _wallRun++;
                GameObject prefab = Art.wallAccent != null && Art.wallAccentEvery > 0 && _wallRun % Art.wallAccentEvery == 0
                    ? Art.wallAccent : Art.outerWalls[_rng.Next(Art.outerWalls.Count)];
                Vector3 mid = pa + dir * (w * (k + 0.5f));
                // Face visible sur la face intérieure du mur (le volume de collision) : la masse de la pièce reste dans la roche.
                Vector3 at = new Vector3(mid.x, floor, mid.z) - inward * (Art.wallFrontDepth - 0.05f);
                float sx = w / Art.wallPieceWidth * 1.04f, sy = h / Art.wallPieceHeight;
                if (prefab == Art.window)
                {
                    // Haute fenêtre : jamais étirée en hauteur (sa baie garde ses proportions), rayon de lune à sa baie.
                    KitPiece(prefab, root, at, inward, new Vector3(sx, 1f, 1f));
                    if (Art.moonRay != null && top - floor >= Art.wallPieceHeight - 0.5f)
                        KitPiece(Art.moonRay, root, new Vector3(mid.x, floor + Art.windowCenterHeight, mid.z) + inward * Art.rayOffset, inward, Vector3.one, true);
                    if (top - floor > Art.wallPieceHeight + 0.5f && Art.outerWalls.Count > 0)
                    {
                        // Au-dessus de la fenêtre : un mur plein jusqu'au plafond.
                        float extra = top - floor - Art.wallPieceHeight;
                        KitPiece(Art.outerWalls[0], root, at + Vector3.up * Art.wallPieceHeight, inward, new Vector3(sx, extra / Art.wallPieceHeight, 1f));
                    }

                    continue;
                }

                KitPiece(prefab, root, at, inward, new Vector3(sx, sy, Mathf.Clamp(sy, 0.8f, 1.4f)));
            }
        }

        /// <summary>Mur entre deux salles du jardin : haie (ou muret) en file, aux deux faces, à la hauteur du mur.</summary>
        private bool DressInnerWall(Transform root, Vector3 pa, Vector3 pb, float floor, float top)
        {
            if (Art == null || Art.innerWall == null)
                return false;
            float len = Vector3.Distance(pa, pb);
            if (len < 1f)
                return false;
            int n = Mathf.Max(1, Mathf.RoundToInt(len / Art.innerWallWidth));
            float w = len / n;
            Vector3 dir = (pb - pa) / len;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            for (int k = 0; k < n; k++)
            {
                Vector3 mid = pa + dir * (w * (k + 0.5f));
                KitPiece(Art.innerWall, root, new Vector3(mid.x, floor, mid.z), side,
                    new Vector3(w / Art.innerWallWidth * 1.08f, (top - floor) / Art.innerWallHeight, 1f));
            }

            return true;
        }

        /// <summary>Fenêtre haute du kit (acte II) sur un mur extérieur, avec son rayon de lune.</summary>
        private bool KitWindow(Transform root, Vector3 face, Vector3 inward, float floor)
        {
            if (Art == null || Art.window == null)
                return false;
            KitPiece(Art.window, root, new Vector3(face.x, floor, face.z) - inward * 0.15f, inward, Vector3.one);
            if (Art.moonRay != null)
                KitPiece(Art.moonRay, root, new Vector3(face.x, floor + Art.windowCenterHeight, face.z) + inward * Art.rayOffset, inward, Vector3.one, true);
            return true;
        }

        private bool KitChandelier(Transform root, Vector3 floor, float ceiling, float scale = 1f)
        {
            if (Art == null || Art.chandelier == null)
                return false;
            KitPiece(Art.chandelier, root, floor + Vector3.up * (ceiling - 0.2f), Vector3.forward, Vector3.one * scale);
            return true;
        }

        /// <summary>Ciel et brume (jardin) : une voûte céleste pour le niveau, des nappes de brume dans les grandes salles.</summary>
        private void DressSky()
        {
            if (Art == null)
                return;
            if (Art.sky != null)
            {
                ActRoom start = Plan.Rooms[Plan.Start];
                GameObject sky = KitPiece(Art.sky, _root, Plan.CellCenter(start.CenterCell, 0f), Vector3.forward, Vector3.one, true);
                sky.name = "Ciel";
                // Nuit 4, point 9 : la lune du menu (même sprite) ne doit pas réapparaître dans le ciel de l'acte III ; la lumière de lune
                // reste (Light directionnelle de la scène), seul le disque dessiné est retiré (retour fait à la DA dans ART-HANDOFF).
                Transform moonDisc = sky.transform.Find("Lune");
                if (moonDisc != null)
                    moonDisc.gameObject.SetActive(false);
            }

            if (Art.mist != null)
                foreach (ActRoom r in Plan.Rooms)
                    if (!r.Special && r.Area(Plan.Cell) > 1400f)
                        KitPiece(Art.mist, _roomRoots[r.Id], Plan.CellCenter(r.CenterCell, r.Floor), Vector3.forward, Vector3.one, true);
        }

        /// <summary>Repère de la salle de départ (puits des caves, statue du jardin) : un peu à côté du centre, jamais sur le joueur.</summary>
        private void StartLandmark(Transform root, ActRoom start, Vector3 startAt)
        {
            if (Art == null || Art.startLandmark == null)
                return;
            Vector3 c = Plan.CellCenter(start.CenterCell, start.Floor);
            Vector3 off = (c - startAt);
            off.y = 0f;
            Vector3 at = c + (off.sqrMagnitude > 1f ? off.normalized : Vector3.forward) * 7f;
            GameObject landmark = KitPiece(Art.startLandmark, root, at, startAt - at, Vector3.one, true);
            if (landmark != null && landmark.name.Contains("Puits"))
                DressWell(landmark, at);
        }

        /// <summary>
        /// Puits des caves (nuit 4, point 1) : il était vide (conduit noir) et sa margelle à facettes accrochait les sauts. Eau sombre à 1.2 m
        /// sous la margelle, collisions remplacées par un cylindre lisse (margelle) et deux montants.
        /// </summary>
        private void DressWell(GameObject well, Vector3 at)
        {
            foreach (Collider c in well.GetComponentsInChildren<Collider>(true))
                Destroy(c);
            var rim = well.AddComponent<CapsuleCollider>();
            rim.radius = 1.58f;
            rim.height = 4f;
            rim.center = new Vector3(0f, -1.1f, 0f);                      // cylindre lisse : dessus à 0.9 m, flancs sans arête
            foreach (float side in new[] { -1f, 1f })
            {
                var post = well.AddComponent<BoxCollider>();
                post.center = new Vector3(side * 1.35f, 1.6f, 0f);
                post.size = new Vector3(0.3f, 3.2f, 0.3f);
            }

            if (settings.waterMaterial != null)
            {
                GameObject water = Visual(well.transform, "Eau du puits", CylinderMesh, settings.waterMaterial, at + Vector3.up * -1.2f, Quaternion.identity,
                    new Vector3(2.7f, 0.02f, 2.7f), merge: false);
                water.name = "Eau du puits";
            }
        }

        /// <summary>Piliers d'une grande salle haute (acte II) : deux rangées symétriques le long du grand axe, hors des voies et des creux.</summary>
        private void HallPillars(ActRoom r, Transform root)
        {
            if (Art == null || Art.hallPillar == null || r.Ceiling < 20f || r.Area(Plan.Cell) < 1500f || r.Feature != RoomFeature.Aucun)
                return;
            bool alongX = r.Box.width >= r.Box.height;
            Vector2 c = r.CenterCell;
            float span = (alongX ? r.Box.width : r.Box.height) * 0.28f, gap = (alongX ? r.Box.height : r.Box.width) * 0.22f;
            foreach (float a in new[] { -1f, 1f })
            foreach (float b in new[] { -1f, 1f })
            {
                Vector2 p = c + (alongX ? new Vector2(a * span, b * gap) : new Vector2(b * gap, a * span));
                var cell = Vector2Int.RoundToInt(p);
                if (!r.Use.TryGetValue(cell, out CellUse u) || u != CellUse.Sol || Occupied(cell))
                    continue;
                Vector3 at = Plan.CellCenter(cell, r.Floor);
                GameObject pillar = KitPiece(Art.hallPillar, root, at, Vector3.forward, new Vector3(1f, r.Ceiling / 19.2f, 1f));
                var col = pillar.AddComponent<CapsuleCollider>();
                col.radius = 1.1f;
                col.height = 19.2f;
                col.center = Vector3.up * 9.6f;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    _occupied.Add(cell + new Vector2Int(dx, dy));
            }
        }

        /// <summary>Matériaux de la salle selon sa fonction (variété des matériaux de la DA).</summary>
        private FunctionMaterials _roomMaterials;
    }
}
