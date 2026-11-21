using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Traqueur (boss-traqueur, D-034) : son coup et sa prise à la main tuent le joueur quels que soient ses PV ; la zone de
    /// prise est la main (boîte orientée) : un joueur à côté d'elle survit.
    /// </summary>
    public class StalkerTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _objects)
                if (o != null)
                    Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private PlayerHealth Player(float maxHealth, Vector3 feet)
        {
            var go = new GameObject("Joueur (test)");
            _objects.Add(go);
            go.transform.position = feet;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var health = ScriptableObject.CreateInstance<PlayerHealthSettings>();
            var coop = ScriptableObject.CreateInstance<CoopSettings>();
            _objects.Add(health);
            _objects.Add(coop);
            health.maxHealth = maxHealth;
            var p = go.AddComponent<PlayerHealth>();
            p.Configure(health, coop);
            return p;
        }

        [Test]
        public void Traqueur_TueEnUnCoup_QuelsQueSoientLesPVDuJoueur()
        {
            foreach (float max in new[] { 1f, 100f, 1000f, 1e6f })
            {
                PlayerHealth p = Player(max, Vector3.zero);
                Assert.That(StalkerStrike.Hit(p, 1f), Is.True);
                Assert.That(p.Health.Dead, Is.True, $"{max} PV : tué en un coup");
                Assert.That(p.Health.Current, Is.EqualTo(0f));
                Assert.That(StalkerStrike.Hit(p, 2f), Is.False, "déjà mort");
            }
        }

        [Test]
        public void PriseALaMain_TueQuelsQueSoientLesPV_SeulementSousLaMain()
        {
            var cfg = ScriptableObject.CreateInstance<StalkerSettings>();
            _objects.Add(cfg);
            Vector3 half = cfg.handSize * 0.5f;                       // main de 4 × 2.2 × 4.5 m
            Vector3 handCenter = new Vector3(0f, half.y, 0f);          // posée au sol, sur le point visé
            Quaternion rot = Quaternion.Euler(0f, 30f, 0f);
            foreach (float max in new[] { 1f, 100f, 1e6f })
            {
                PlayerHealth under = Player(max, new Vector3(0.5f, 0f, 1f));
                Assert.That(StalkerStrike.Grab(under, handCenter, half, rot, 1f), Is.True, $"{max} PV sous la main : tué");
                Assert.That(under.Health.Dead, Is.True);
            }

            PlayerHealth beside = Player(100f, new Vector3(3.2f, 0f, -2.5f));
            Assert.That(StalkerStrike.Grab(beside, handCenter, half, rot, 1f), Is.False, "à côté de la main : vivant");
            PlayerHealth above = Player(100f, new Vector3(0f, 2.7f, 0f));
            Assert.That(StalkerStrike.Grab(above, handCenter, half, rot, 1f), Is.False, "au-dessus de la main (double saut) : vivant");
            Assert.That(beside.Health.Current, Is.EqualTo(100f));
        }

        [Test]
        public void BrasEtire_SeulLAxeDeLOsSAllonge()
        {
            // D-070 : le bras s'allonge par l'échelle de son os, le long de son axe seulement (épaisseur inchangée) ; la main reçoit
            // l'inverse et garde sa taille.
            Vector3 rest = new Vector3(1f, 1f, 1f);
            Assert.That(Stalker.StretchScale(rest, 1, 3.2f), Is.EqualTo(new Vector3(1f, 3.2f, 1f)));
            Assert.That(Stalker.StretchScale(new Vector3(2f, 0.5f, 1f), 0, 2f), Is.EqualTo(new Vector3(4f, 0.5f, 1f)));
            Vector3 arm = Stalker.StretchScale(rest, 2, 3.8f), handLocal = Stalker.StretchScale(rest, 2, 1f / 3.8f);
            Assert.That(arm.z * handLocal.z, Is.EqualTo(1f).Within(1e-5f), "main : taille gardée");
        }

        [Test]
        public void Clips_VitesseDeLecture_PiedsSansGlisser()
        {
            // PR #13 : Traqueur_Marche avance de 3.71 m/s à la vitesse 1, Traqueur_PassageBas de 2.29 m/s.
            var cfg = ScriptableObject.CreateInstance<StalkerSettings>();
            _objects.Add(cfg);
            Assert.That(Stalker.ClipPlayback(cfg.chaseSpeed, cfg.walkClipSpeed, cfg) * 3.71f, Is.EqualTo(cfg.chaseSpeed).Within(0.01f));
            Assert.That(Stalker.ClipPlayback(4.58f, cfg.crouchClipSpeed, cfg), Is.EqualTo(2f).Within(0.01f));
            Assert.That(Stalker.ClipPlayback(cfg.enragedBurstSpeed, cfg.walkClipSpeed, cfg) * cfg.walkClipSpeed,
                Is.EqualTo(cfg.enragedBurstSpeed).Within(0.01f), "même la ruée enragée ne glisse pas");
            Assert.That(Stalker.ClipPlayback(0.1f, cfg.walkClipSpeed, cfg), Is.EqualTo(cfg.clipPlaybackRange.x), "borne basse");
        }

        [Test]
        public void ModeleV3_32Os_SansOsDAide_TroisMateriauxDuCorps()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Ennemis/SK_Traqueur.fbx");
            Assert.That(model, Is.Not.Null);
            SkinnedMeshRenderer body = model.GetComponentsInChildren<SkinnedMeshRenderer>(true).OrderByDescending(r => r.sharedMaterials.Length).First();
            Assert.That(body.bones.Length, Is.EqualTo(32), "32 os");
            Assert.That(model.GetComponentsInChildren<Transform>(true).Any(t => t.name.EndsWith("_Aide")), Is.False, "plus d'os d'aide");
            // L'ordre des matériaux suit le FBX (PR #16 : la tête en dernier) ; la lueur est choisie par le nom du matériau.
            Assert.That(body.sharedMaterials.Select(m => m.name).ToArray(), Is.EquivalentTo(new[] { "M_Traqueur", "M_Traqueur_Tete", "M_Traqueur_Mains" }));
            Assert.That(Stalker.HasEmissionMask(body.sharedMaterials.First(m => m.name == "M_Traqueur")), Is.False, "le corps ne s'allume pas");
            Assert.That(Stalker.HasEmissionMask(body.sharedMaterials.First(m => m.name == "M_Traqueur_Tete")), Is.True, "les pupilles passent par la tête");
        }
    }
}
