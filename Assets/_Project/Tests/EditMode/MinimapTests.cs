using System.Collections.Generic;
using ItHunts.Hud;
using ItHunts.Level.Acts;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Minimap précise (D-101, nuit 4, point 6) : le peintre pur (ActMapPainter) sur un plan d'acte construit en test : sols des
    /// salles connues peints, hors des salles transparent, trait d'encre le long des contours, porte = trou dans le trait (barre si
    /// verrouillée), nord en haut, salles inconnues non peintes ; règle « dans le champ de vision » (angle, distance) en pur.
    /// </summary>
    public sealed class MinimapTests
    {
        private static ActLayoutSettings Settings()
        {
            var s = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>("Assets/_Project/Settings/Actes/Act_1.asset");
            return s != null ? s : ScriptableObject.CreateInstance<ActLayoutSettings>();   // valeurs par défaut : caves, 16 salles
        }

        private static (ActPlan plan, Bounds all, int size) Make(int seed)
        {
            ActPlan p = ActPlan.Make(Settings(), seed);
            Assert.That(p.Rooms.Count, Is.GreaterThanOrEqualTo(4), "plan construit");
            Bounds all = ActMapPainter.BoundsOf(p);
            return (p, all, ActMapPainter.TextureSize(all));
        }

        private static Color32 At(Color32[] px, Bounds all, int size, Vector3 w)
        {
            Vector2Int q = ActMapPainter.PixelOf(all, size, w);
            Assert.That(q.x, Is.InRange(0, size - 1));
            Assert.That(q.y, Is.InRange(0, size - 1));
            return px[q.y * size + q.x];
        }

        private static Vector3 World(ActPlan p, Vector2 u) => p.Origin + new Vector3(u.x * p.Cell, 0f, u.y * p.Cell);

        /// <summary>Aucune salle à moins de r cases (Tchebychev) de la case c.</summary>
        private static bool Far(ActPlan p, Vector2Int c, int r)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
                if (p.OwnerAt(c + new Vector2Int(dx, dy)) != -1)
                    return false;
            return true;
        }

        [Test]
        public void Resolution_AuMoins4PxParMetre_TextureBornee()
        {
            (ActPlan plan, Bounds all, int size) = Make(11);
            Assert.That(plan.Rooms.Count, Is.GreaterThan(0));
            Assert.That(size, Is.InRange(ActMapPainter.MinTexture, ActMapPainter.MaxTexture));
            Assert.That(Mathf.IsPowerOfTwo(size), Is.True);
            Assert.That(all.size.x, Is.EqualTo(all.size.z).Within(0.01f), "emprise carrée : pixels isotropes");
            Assert.That(size / all.size.x, Is.GreaterThanOrEqualTo(2f), "au moins 2 px par mètre (4 visés)");
        }

        [Test]
        public void Sol_DesSallesConnues_Peint_HorsDesSalles_Transparent()
        {
            (ActPlan p, Bounds all, int size) = Make(11);
            Color32[] px = ActMapPainter.Paint(p, all, size, _ => true);
            Assert.That(px.Length, Is.EqualTo(size * size));
            int floors = 0;
            foreach (ActRoom r in p.Rooms)
            foreach (Vector2Int c in r.Cells)
            {
                if (r.Use[c] != CellUse.Sol && r.Use[c] != CellUse.Passage)
                    continue;
                Color32 k = At(px, all, size, p.CellCenter(c));
                Assert.That(k.a, Is.GreaterThan(150), $"« {r.Name} » case {c} : sol peint");
                floors++;
            }

            Assert.That(floors, Is.GreaterThan(100));

            int outside = 0;
            for (int x = 0; x < p.Size; x++)
            for (int y = 0; y < p.Size; y++)
            {
                var c = new Vector2Int(x, y);
                if (p.OwnerAt(c) != -1 || !Far(p, c, 2))
                    continue;
                Vector3 w = p.CellCenter(c);
                if (!all.Contains(new Vector3(w.x, all.center.y, w.z)))
                    continue;
                Assert.That(At(px, all, size, w).a, Is.EqualTo(0), $"case {c} hors des salles : transparente");
                outside++;
            }

            Assert.That(outside, Is.GreaterThan(100));
        }

        [Test]
        public void SalleInconnue_NonPeinte_BrumeParSalle()
        {
            (ActPlan p, Bounds all, int size) = Make(22);
            int[] depth = p.Depths(p.Start);
            int far = -1;
            for (int i = 0; i < p.Rooms.Count; i++)
                if (depth[i] >= 2 && !p.Rooms[i].Special)
                    far = i;
            Assert.That(far, Is.GreaterThanOrEqualTo(0), "une salle à 2 portes du départ");
            Color32[] px = ActMapPainter.Paint(p, all, size, id => id == p.Start);
            foreach (Vector2Int c in p.Rooms[p.Start].Cells)
                if (p.Rooms[p.Start].Use[c] == CellUse.Sol)
                    Assert.That(At(px, all, size, p.CellCenter(c)).a, Is.GreaterThan(150), "départ connu : peint");
            foreach (Vector2Int c in p.Rooms[far].Cells)
                Assert.That(At(px, all, size, p.CellCenter(c)).a, Is.EqualTo(0), $"« {p.Rooms[far].Name} » inconnue : rien");
        }

        [Test]
        public void Mur_TraitDEncre_LeLongDuContour()
        {
            (ActPlan p, Bounds all, int size) = Make(11);
            Color32[] px = ActMapPainter.Paint(p, all, size, _ => true);
            int walls = 0;
            foreach (ActRoom r in p.Rooms)
            foreach (ActContour.Segment s in ActContour.Of(p, r))
            {
                if (s.Diagonal || s.Length < 2f || s.Neighbor != -1)
                    continue;                                            // mur plein sur la roche : jamais de porte
                Vector2 mid = (s.A + s.B) * 0.5f;
                Vector2 inner = mid - s.Out * 0.1f;                      // 0.2 m à l'intérieur : dans l'épaisseur du trait (1 m)
                Color32 k = At(px, all, size, World(p, inner));
                Assert.That(k.a, Is.EqualTo(255), $"« {r.Name} » mur {s.A}-{s.B} : trait plein");
                Assert.That(k.r, Is.GreaterThan(170), $"« {r.Name} » mur {s.A}-{s.B} : encre claire");
                Assert.That(k.b, Is.LessThan(k.r), "encre chaude (#CDBEA5)");
                walls++;
            }

            Assert.That(walls, Is.GreaterThan(20));
        }

        [Test]
        public void Porte_LaisseUnTrou_DansLeTrait_Verrouillee_Barree()
        {
            (ActPlan p, Bounds all, int size) = Make(11);
            Color32[] px = ActMapPainter.Paint(p, all, size, _ => true);
            int open = 0, locked = 0;
            foreach (ActDoor d in p.Doors)
            {
                Color32 k = At(px, all, size, World(p, ActMapPainter.DoorMiddle(d)));
                if (d.Locked)
                {
                    Assert.That(k.r, Is.GreaterThan(200), "porte verrouillée : barre orange");
                    Assert.That(k.b, Is.LessThan(100));
                    locked++;
                }
                else if (!d.Gate && !d.Exit)
                {
                    Assert.That(k.a, Is.GreaterThan(0), "seuil peint");
                    Assert.That(k.r, Is.LessThan(120), $"porte {d.A}→{d.B} : pas d'encre au milieu de l'ouverture (trou dans le trait)");
                    open++;
                }
            }

            Assert.That(open, Is.GreaterThan(3));
            Assert.That(locked, Is.GreaterThanOrEqualTo(1), "l'acte I a au moins une salle verrouillée");
        }

        [Test]
        public void Reliefs_Bassin_Fosse_Terrasse_AuxCouleursPrevues()
        {
            int hollow = 0, terraces = 0;
            foreach (int seed in new[] { 11, 22, 33, 44, 55 })
            {
                (ActPlan p, Bounds all, int size) = Make(seed);
                Color32[] px = ActMapPainter.Paint(p, all, size, _ => true);
                foreach (ActRoom r in p.Rooms)
                foreach (Vector2Int c in r.Cells)
                {
                    Color32 k = At(px, all, size, p.CellCenter(c));
                    switch (r.Use[c])
                    {
                        case CellUse.Creux:
                            Color32 want = r.Feature == RoomFeature.Eau ? ActMapPainter.Water : ActMapPainter.Void;
                            Assert.That(k.b, Is.EqualTo(want.b), $"« {r.Name} » creux {c}");
                            hollow++;
                            break;
                        case CellUse.Terrasse:
                            Assert.That(k.r == ActMapPainter.Terrace.r || k.r == ActMapPainter.TerraceStripe.r, Is.True, $"« {r.Name} » terrasse {c}");
                            terraces++;
                            break;
                    }
                }
            }

            Assert.That(hollow, Is.GreaterThan(0), "au moins un bassin ou une fosse sur 5 seeds");
            Assert.That(terraces, Is.GreaterThan(0), "au moins une terrasse sur 5 seeds");
        }

        [Test]
        public void NordEnHaut_UneCasePlusAuNord_EstPlusHaute()
        {
            (ActPlan p, Bounds all, int size) = Make(11);
            Vector2Int c = p.Rooms[0].Cells[0];
            Vector2Int a = ActMapPainter.PixelOf(all, size, p.CellCenter(c));
            Vector2Int b = ActMapPainter.PixelOf(all, size, p.CellCenter(c + Vector2Int.up));
            Vector2Int e = ActMapPainter.PixelOf(all, size, p.CellCenter(c + Vector2Int.right));
            Assert.That(b.y, Is.GreaterThan(a.y), "nord (z+) : ligne plus haute dans la texture");
            Assert.That(b.x, Is.EqualTo(a.x));
            Assert.That(e.x, Is.GreaterThan(a.x), "est (x+) : colonne plus à droite");
        }

        [Test]
        public void ChampDeVision_Angle_APlat_EtDistance()
        {
            var eye = new Vector3(0f, 1.6f, 0f);
            Vector3 fwd = Vector3.forward;
            const float fov = 90f, margin = 5f, range = 60f;
            Assert.That(MinimapVision.InSight(eye, fwd, new Vector3(0f, 1f, 30f), fov, margin, range), Is.True, "devant, 30 m");
            Assert.That(MinimapVision.InSight(eye, fwd, new Vector3(0f, 1f, -10f), fov, margin, range), Is.False, "derrière");
            Assert.That(MinimapVision.InSight(eye, fwd, new Vector3(0f, 1f, 70f), fov, margin, range), Is.False, "trop loin");
            Vector3 at48 = eye + Quaternion.Euler(0f, 48f, 0f) * Vector3.forward * 20f;
            Vector3 at55 = eye + Quaternion.Euler(0f, 55f, 0f) * Vector3.forward * 20f;
            Assert.That(MinimapVision.InSight(eye, fwd, at48, fov, margin, range), Is.True, "48° ≤ 45° + 5°");
            Assert.That(MinimapVision.InSight(eye, fwd, at55, fov, margin, range), Is.False, "55° > 50°");
            Assert.That(MinimapVision.InSight(eye, fwd, new Vector3(0f, 12f, 20f), fov, margin, range), Is.True, "angle mesuré à plat : une goule en hauteur devant reste vue");
            Assert.That(MinimapVision.InSight(eye, fwd, new Vector3(0f, 50f, 40f), fov, margin, range), Is.False, "la portée se mesure en 3D");
            Assert.That(MinimapVision.InSight(eye, fwd, eye, fov, margin, range), Is.True, "cible sur l'œil");
        }

        [Test]
        public void Reglages_ParDefaut_DeLHabillage()
        {
            var skin = ScriptableObject.CreateInstance<HudSkinSettings>();
            try
            {
                Assert.That(skin.enemySightRange, Is.EqualTo(60f));
                Assert.That(skin.enemySightMargin, Is.EqualTo(5f));
                Assert.That(skin.enemyFade, Is.EqualTo(0.5f));
                Assert.That(skin.stalkerIconSize, Is.GreaterThan(skin.enemyIconSize), "le traqueur se voit plus gros qu'une goule");
            }
            finally
            {
                Object.DestroyImmediate(skin);
            }
        }
    }
}
