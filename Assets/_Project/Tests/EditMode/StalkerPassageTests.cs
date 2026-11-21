using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Passages du traqueur (point 9) : choix du lien, distances maximales, bond raté et récupération.</summary>
    public class StalkerPassageTests
    {
        private StalkerSettings _cfg;

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<StalkerSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        [Test]
        public void DistancesMaximales_ParType()
        {
            Vector3 a = Vector3.zero;
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.Bond, a, new Vector3(20f, 5f, 0f), _cfg), Is.True);
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.Bond, a, new Vector3(23f, 0f, 0f), _cfg), Is.False, "plus de 22 m");
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.Bond, a, new Vector3(10f, 7f, 0f), _cfg), Is.False, "plus de 6 m de dénivelé");
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.MurGrimpe, a, new Vector3(5f, 15f, 0f), _cfg), Is.True);
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.MurGrimpe, a, new Vector3(5f, 17f, 0f), _cfg), Is.False, "mur de 16 m au plus");
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.EscaladeRebord, a, new Vector3(5f, 9f, 0f), _cfg), Is.False, "rebord de 8 m au plus");
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.ChaineSaut, a, new Vector3(19f, -5f, 0f), _cfg), Is.False, "saut de chaîne de 18 m au plus");
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.ChaineSaut, new Vector3(0f, 11f, 0f), new Vector3(12f, 0f, 12f), _cfg), Is.True,
                "il se laisse tomber de 11 m depuis la chaîne (puits)");
            Assert.That(StalkerPassageMath.Allowed(StalkerLinkKind.MurGrimpe, a, new Vector3(11f, 11f, 11f), _cfg), Is.True, "bouts recalés : 16 m à plat au plus");
        }

        [Test]
        public void ChoixDuLien_SelonLeTempsDeTrajet()
        {
            // Coût du lien = temps du passage ramené à la marche : un bond de 16 m (télégraphe, vol, récupération) face à un détour.
            Vector3 a = Vector3.zero, b = new Vector3(16f, 0f, 0f);
            float t = StalkerPassageMath.TotalTime(StalkerLinkKind.Bond, a, b, _cfg);
            float cost = StalkerPassageMath.CostModifier(StalkerLinkKind.Bond, a, b, _cfg);
            Assert.That(cost * 16f / _cfg.chaseSpeed, Is.EqualTo(t).Within(1e-3f), "coût = temps × vitesse / longueur");
            float detourShort = 10f, detourLong = 60f;
            Assert.That(detourShort / _cfg.chaseSpeed < t, Is.True, "détour court : il contourne");
            Assert.That(detourLong / _cfg.chaseSpeed > t, Is.True, "long détour : il saute");
            Assert.That(StalkerPassageMath.CostModifier(StalkerLinkKind.MurGrimpe, a, new Vector3(4f, 12f, 0f), _cfg), Is.GreaterThanOrEqualTo(1f));
        }

        [Test]
        public void Bond_Arc_EtPrise()
        {
            Vector3 a = Vector3.zero, b = new Vector3(16f, 4f, 0f);
            Vector3 mid = StalkerPassageMath.Position(StalkerLinkKind.Bond, a, b, 0.5f, _cfg);
            Assert.That(mid.y, Is.EqualTo(2f + _cfg.leapArcHeight + 4f).Within(1e-3f), "sommet de l'arc");
            Assert.That(StalkerPassageMath.Position(StalkerLinkKind.Bond, a, b, 1f, _cfg), Is.EqualTo(b));
            Vector3 body = mid + Vector3.up * (_cfg.height * 0.45f);
            Assert.That(StalkerPassageMath.LeapHits(mid, body + Vector3.right * 2f, _cfg), Is.True, "le corps traverse le joueur : prise");
            Assert.That(StalkerPassageMath.LeapHits(mid, body + Vector3.right * 6f, _cfg), Is.False, "esquivé");
        }

        [Test]
        public void BondRate_Recuperation_EscaladeSans()
        {
            var t = new StalkerTraversal { Active = true, Kind = StalkerLinkKind.Bond };
            Assert.That(StalkerPassageMath.Land(ref t, 10f, _cfg), Is.True, "personne attrapé : bond raté");
            Assert.That(t.Active, Is.False);
            Assert.That(t.RecoverUntil, Is.EqualTo(10f + _cfg.leapRecovery).Within(1e-4f), "réception à quatre pattes");
            var caught = new StalkerTraversal { Active = true, Kind = StalkerLinkKind.Bond, Caught = true };
            Assert.That(StalkerPassageMath.Land(ref caught, 10f, _cfg), Is.False);
            var climb = new StalkerTraversal { Active = true, Kind = StalkerLinkKind.MurGrimpe };
            StalkerPassageMath.Land(ref climb, 10f, _cfg);
            Assert.That(climb.RecoverUntil, Is.EqualTo(0f), "pas de récupération après une escalade");
        }

        [Test]
        public void Escalade_MonteAvantDAvancer()
        {
            Vector3 a = Vector3.zero, b = new Vector3(6f, 10f, 0f);
            Vector3 p = StalkerPassageMath.Position(StalkerLinkKind.ChaineGrimpe, a, b, 0.4f, _cfg);
            Assert.That(p.x, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(p.y, Is.EqualTo(5f).Within(1e-3f));
        }

        [Test]
        public void Modules_DeLaNuit_Valides_AvecPassagesEtAncres()
        {
            foreach (string n in new[] { "Prison_Etages", "Caserne", "Citerne_Inondee", "Puits_Circulaire" })
            {
                var go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Modules/Module_{n}.prefab");
                Assert.That(go, Is.Not.Null, n);
                var m = go.GetComponent<ItHunts.Level.RoomModule>();
                Assert.That(m.validated, Is.True, n + " validé");
                Assert.That(go.GetComponentsInChildren<StalkerPassage>().Length, Is.GreaterThan(0), n + " : passages du traqueur");
                var spots = go.GetComponentsInChildren<ItHunts.Level.ChestSpot>();
                Assert.That(System.Array.Exists(spots, s => s.difficulty == 2), Is.True, n + " : une ancre difficile");
                // B3 : aucune porte étroite plus petite que sa plus petite porte (DA : 4 × 5.2 m) : il la franchit en se faufilant.
                var stalkerCfg = UnityEditor.AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
                Assert.That(stalkerCfg.narrowDoorWidth, Is.EqualTo(4f));
                Assert.That(stalkerCfg.narrowDoorHeight, Is.EqualTo(5.2f));
                foreach (StalkerPassage door in go.GetComponentsInChildren<StalkerPassage>(true))
                    if (door.kind == StalkerLinkKind.PorteEtroite)
                        Assert.That(StalkerPassageMath.DoorPassable(door.doorHalfSize, stalkerCfg), Is.True, $"{n} : {door.name}");
                // A2 : chaque pièce du kit de la DA a sa collision ; garde-corps, cordes et chaînes laissent passer la main du traqueur.
                foreach (Transform t in go.GetComponentsInChildren<Transform>())
                    if (t.name.StartsWith("Kit (DA)"))
                        Assert.That(t.GetComponentsInChildren<Collider>().Length, Is.GreaterThan(0), $"{n} : {t.name} sans collision");
                foreach (Collider c in go.GetComponentsInChildren<Collider>())
                    if (c.name.Contains("garde-corps") || c.name.Contains("cordes") || c.name.Contains("chaîne"))
                        Assert.That(c.GetComponentInParent<StalkerPassesThrough>(true), Is.Not.Null, $"{n} : {c.name}");
            }
        }
    }
}
