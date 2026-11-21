using System.Collections.Generic;
using System.Linq;
using ItHunts.Level;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Parcours (point 4 de carte-suite) : capacités de saut tirées des réglages de mouvement, parcours toujours faisables sans
    /// traction, toujours exigeants (un passage entre deux murs et un vide à dash, chacun plus long qu'un double saut), coffre
    /// atteint seulement depuis le parcours, réglages de la carte (une zone pour deux salles, un coffre sur trois).
    /// </summary>
    public class ParkourTests
    {
        private static ParkourCaps Caps()
        {
            var m = AssetDatabase.LoadAssetAtPath<MovementSettings>("Assets/_Project/Settings/Movement_Default.asset");
            Assert.That(m, Is.Not.Null);
            return ParkourCaps.From(m);
        }

        [Test]
        public void Capacites_SimpleDoubleDash_DansLOrdre()
        {
            ParkourCaps c = Caps();
            Assert.That(c.Single(0f), Is.EqualTo(5.55f).Within(0.2f), "saut simple en course (8.5 m/s, 1.3 m)");
            Assert.That(c.Double(0f), Is.GreaterThan(c.Single(0f)));
            Assert.That(c.Dash(0f), Is.GreaterThan(c.Double(0f)), "un dash va plus loin qu'un double saut");
            Assert.That(c.Single(c.JumpHeight + 0.1f), Is.EqualTo(0f), "trop haut pour un saut simple");
            Assert.That(c.Double(c.MaxRise + 0.1f), Is.EqualTo(0f));
        }

        [Test]
        public void Parcours_FaisablesEtExigeants_DeTouteLongueur()
        {
            ParkourCaps c = Caps();
            int built = 0;
            for (int seed = 1; seed <= 40; seed++)
                foreach (float len in new[] { 38f, 44f, 52f, 64f, 80f })
                    foreach (float height in new[] { 0f, 1f, 2f })
                    {
                        var jumps = new List<ParkourJump>();
                        List<ParkourPiece> p = ParkourCourse.Layout(len, height, -10f, 15f, null, 1f, 0f, c, new System.Random(seed), jumps);
                        if (p == null)
                            continue;
                        built++;
                        string tag = $"seed {seed}, {len} m, hauteur {height}";
                        Assert.That(jumps.All(j => j.Feasible), Is.True, tag + " : " + string.Join(" ; ", jumps.Where(j => !j.Feasible).Select(j => $"{j.From} → {j.To} {j.Gap:0.0}/{j.Capacity:0.0}")));
                        Assert.That(jumps.Any(j => j.Move == ParkourMove.Dash && j.Gap > j.DoubleReach), Is.True, tag + " : vide à dash");
                        Assert.That(jumps.Any(j => j.Move == ParkourMove.Murs && j.Gap > j.DoubleReach), Is.True, tag + " : passage entre deux murs");
                        Assert.That(p.Count(q => q.Wall), Is.EqualTo(2));
                        float end = p.Where(q => !q.Wall && !q.Chest).Max(q => q.Center.x + q.Size.x * 0.5f);
                        Assert.That(end, Is.EqualTo(len).Within(0.05f), tag + " : finit au bout du tracé");
                        Assert.That(p.Where(q => q.Crumble).All(q => q.Size.x <= 2.01f), Is.True, "plateformes qui tombent : 2 m au plus");
                        Assert.That(p.First().Crumble || p.Last(q => !q.Wall).Crumble, Is.False, "poutres de départ et d'arrivée fixes (à portée)");
                    }

            Assert.That(built, Is.GreaterThan(400), "presque toutes les longueurs de 44 m et plus");
            Assert.That(ParkourCourse.Layout(25f, 0f, -10f, 15f, null, 1f, 0f, c, new System.Random(1), null), Is.Null, "trop court : pas de parcours");
        }

        [Test]
        public void Coffre_SeulementDepuisLeParcours()
        {
            ParkourCaps c = Caps();
            var jumps = new List<ParkourJump>();
            // Galerie : tracé à 2 m, socle à 4.4 m au-dessus du couloir ; le parapet (1 m) en est à 3.4 m de haut.
            List<ParkourPiece> p = ParkourCourse.Layout(50f, 2f, -10f, 15f, "palier après les murs", -1f, 4.4f, c, new System.Random(3), jumps);
            Assert.That(p, Is.Not.Null);
            ParkourPiece socle = p.Single(q => q.Chest);
            float top = socle.Center.y + socle.Size.y * 0.5f;
            Assert.That(top, Is.EqualTo(4.4f).Within(0.01f));
            Assert.That(top - 1f, Is.GreaterThan(c.MaxRise), "hors d'atteinte depuis le parapet");
            ParkourJump j = jumps.Single(x => x.To == "socle du coffre");
            Assert.That(j.Feasible, Is.True, "atteint depuis le palier après les murs");
            Assert.That(socle.Crumble, Is.False);
        }

        [Test]
        public void Reglages_UneZonePourDeuxSalles_UnCoffreSurTrois()
        {
            var s = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>("Assets/_Project/Settings/MapLayout_Niveau1.asset");
            Assert.That(s.zonesPerRoom, Is.EqualTo(0.5f));
            Assert.That(s.parkourChestShare, Is.EqualTo(0.34f).Within(0.01f));
            Assert.That(s.parkourHalls, Is.EqualTo(6));
        }

        [Test]
        public void Emprises_GalerieAColeDuCouloir()
        {
            var l = new PlanLink { From = new Vector3(0f, 0f, 0f), To = new Vector3(60f, 0f, 0f), Width = 14f };
            Rect plus = MapPlanner.GalleryRect(l, 1), minus = MapPlanner.GalleryRect(l, -1);
            Assert.That(plus.Overlaps(minus), Is.False, "une de chaque côté");
            Assert.That(plus.Overlaps(MapPlanner.LinkRect(l, 0f, 7f)), Is.False, "à côté du couloir, pas dessus");
            Assert.That(plus.width, Is.EqualTo(56f).Within(0.01f));
            Assert.That(plus.height, Is.EqualTo(MapPlanner.GalleryOuter - 0.5f).Within(0.01f));
        }
    }
}
