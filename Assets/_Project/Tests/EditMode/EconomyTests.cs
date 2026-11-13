using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Âmes et économie (étape 7, D-043) : tirage avec la seed, prix et refus, ×2 en Enragé, coop, morts sans joueur.</summary>
    public class EconomyTests
    {
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
            {
                if (o is GameObject go)
                {
                    foreach (var p in go.GetComponents<PlayerHealth>())
                        Invoke(p, "OnDisable");
                    foreach (var d in go.GetComponents<Damageable>())
                        Invoke(d, "OnDisable");
                    foreach (var s in go.GetComponents<SoulSystem>())
                        Invoke(s, "OnDisable");
                }

                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _made.Clear();
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        private T Track<T>(T o) where T : Object
        {
            _made.Add(o);
            return o;
        }

        private PlayerHealth Player(Vector3 at)
        {
            var go = Track(new GameObject("Joueur (test)"));
            go.transform.position = at;
            var p = go.AddComponent<PlayerHealth>();
            p.Init();
            Invoke(p, "OnEnable");
            return p;
        }

        [Test]
        public void Assets_ValeursValidees_HuitAmeliorationsDecrites()
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            Assert.That(e, Is.Not.Null);
            Assert.That(new[] { e.soulLifetime, e.soulBlinkTime, e.collectRadius, e.pullSpeed, e.shareRadius, e.involvementWindow, e.enragedMultiplier },
                Is.EqualTo(new[] { 30f, 5f, 8f, 25f, 40f, 5f, 2f }));
            Assert.That(new[] { e.chestPrice, e.lockedDoorPrice, e.healPrice, e.choiceCount, e.merchantOffers }, Is.EqualTo(new[] { 30, 50, 35, 3, 3 }));
            Assert.That(e.healAmount, Is.EqualTo(40f));
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePool>("Assets/_Project/Settings/UpgradePool_Default.asset");
            Assert.That(pool.upgrades.Count, Is.EqualTo(8), "8 premières améliorations");
            Assert.That(pool.upgrades.All(u => u != null && !string.IsNullOrEmpty(u.description) && !u.description.Contains("\n")), Is.True, "une ligne chacune");
            Assert.That(pool.upgrades.Select(u => u.kind).Distinct().Count(), Is.EqualTo(8), "une par effet");
            Assert.That(pool.upgrades.All(u => u.merchantPrice >= 40 && u.merchantPrice <= 80), Is.True, "prix au marchand 40 à 80");
            var ghoul = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            var flyer = AssetDatabase.LoadAssetAtPath<FlyerSettings>("Assets/_Project/Settings/Flyer_Default.asset");
            var stalker = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            Assert.That(new[] { ghoul.soulValue, flyer.soulValue, stalker.soulValue }, Is.EqualTo(new[] { 3, 2, 0 }), "le traqueur ne donne jamais d'âmes");
        }

        [Test]
        public void Tirage_MemeSeedMemeChoix_TroisDifferents()
        {
            int[] a = SoulMath.Draw(1234, 0, 8, 3), b = SoulMath.Draw(1234, 0, 8, 3);
            Assert.That(a, Is.EqualTo(b), "même seed, même numéro : même tirage");
            Assert.That(a.Distinct().Count(), Is.EqualTo(3), "trois améliorations différentes");
            Assert.That(a.All(i => i >= 0 && i < 8), Is.True);
            bool differs = false;
            for (int k = 1; k < 6 && !differs; k++)
                differs |= !SoulMath.Draw(1234, k, 8, 3).SequenceEqual(a);
            Assert.That(differs, Is.True, "les coffres suivants tirent autre chose");
            Assert.That(SoulMath.PickSpot(77, 5), Is.EqualTo(SoulMath.PickSpot(77, 5)), "marchand : même seed, même emplacement");
            Assert.That(SoulMath.Draw(1, 0, 2, 3).Length, Is.EqualTo(3), "pool trop petit : doublons permis");
        }

        [Test]
        public void Prix_EtRefus_RienNEstRetire()
        {
            var go = Track(new GameObject("Run (test)"));
            var run = go.AddComponent<RunState>();
            run.Configure(null, 42);
            int refused = 0;
            System.Action<int> onRefused = id => refused++;
            RunState.Refused += onRefused;
            try
            {
                run.AddSouls(5, 20);
                Assert.That(run.TrySpend(5, 30), Is.False, "20 âmes, coffre à 30 : refus");
                Assert.That(refused, Is.EqualTo(1), "refus signalé (rouge, son)");
                Assert.That(run.Souls(5), Is.EqualTo(20), "rien n'est retiré");
                run.AddSouls(5, 15);
                Assert.That(run.TrySpend(5, 30), Is.True);
                Assert.That(run.Souls(5), Is.EqualTo(5));
                Assert.That(run.TrySpend(5, 0), Is.True, "coffre gratuit");
                Assert.That(run.Souls(9), Is.EqualTo(0), "âmes individuelles");
                Assert.That(SoulMath.CanAfford(50, 50), Is.True);
                Assert.That(SoulMath.CanAfford(49, 50), Is.False);
            }
            finally
            {
                RunState.Refused -= onRefused;
            }
        }

        [Test]
        public void HausseDuPrix_ParAchatParJoueurEtParCategorie()
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            Assert.That(new[] { e.chestPriceIncrease, e.merchantPriceIncrease, e.doorPriceIncrease }, Is.EqualTo(new[] { 0f, 0f, 0f }), "0 % pour l'instant");
            Assert.That(SoulMath.RaisedPrice(30, 0f, 5), Is.EqualTo(30), "0 % : prix fixe");
            Assert.That(SoulMath.RaisedPrice(30, 20f, 1), Is.EqualTo(36));
            Assert.That(SoulMath.RaisedPrice(30, 20f, 2), Is.EqualTo(43), "composée : 30 × 1,2²");
            Assert.That(SoulMath.RaisedPrice(0, 50f, 3), Is.EqualTo(0), "coffre gratuit");

            var go = Track(new GameObject("Run (test)"));
            var run = go.AddComponent<RunState>();
            run.Configure(null, 42);
            run.AddSouls(1, 200);
            run.AddSouls(2, 200);
            Assert.That(run.TryBuy(1, PriceKind.Coffre, 30, 20f), Is.True);
            Assert.That(run.Souls(1), Is.EqualTo(170));
            Assert.That(run.PriceFor(1, PriceKind.Coffre, 30, 20f), Is.EqualTo(36), "le coffre suivant coûte plus cher pour lui");
            Assert.That(run.PriceFor(2, PriceKind.Coffre, 30, 20f), Is.EqualTo(30), "pas pour l'autre joueur");
            Assert.That(run.PriceFor(1, PriceKind.Marchand, 60, 20f), Is.EqualTo(60), "ni au marchand");
            Assert.That(run.PriceFor(1, PriceKind.Porte, 50, 20f), Is.EqualTo(50), "ni aux portes");
            run.AddSouls(3, 10);
            Assert.That(run.TryBuy(3, PriceKind.Coffre, 30, 20f), Is.False, "refus");
            Assert.That(run.Purchases(3, PriceKind.Coffre), Is.EqualTo(0), "un refus ne compte pas");
            Assert.That(run.TryBuy(1, PriceKind.Coffre, 0, 20f), Is.True);
            Assert.That(run.Purchases(1, PriceKind.Coffre), Is.EqualTo(1), "un coffre gratuit ne compte pas");
        }

        [Test]
        public void Enrage_x2_AuMomentDeLaMort()
        {
            Assert.That(SoulMath.SoulsAtDeath(3, false, 2f), Is.EqualTo(3));
            Assert.That(SoulMath.SoulsAtDeath(3, true, 2f), Is.EqualTo(6), "goule en Enragé");
            Assert.That(SoulMath.SoulsAtDeath(2, true, 2f), Is.EqualTo(4), "volant en Enragé");
            Assert.That(SoulMath.SoulsAtDeath(0, true, 2f), Is.EqualTo(0), "traqueur, factice : jamais");
        }

        [Test]
        public void MortsSansJoueur_EtAttributionEnCoop()
        {
            Assert.That(SoulMath.Involved(0, 10f, 10f, 5f), Is.False, "personne n'y est pour rien");
            Assert.That(SoulMath.Involved(7, 4.9f, 10f, 5f), Is.False, "il y a plus de 5 s");
            Assert.That(SoulMath.Involved(7, 5.5f, 10f, 5f), Is.True);
            var players = new List<Vector3> { new Vector3(10f, 0f, 0f), new Vector3(50f, 0f, 0f), new Vector3(0f, 0f, 30f) };
            Assert.That(SoulMath.Recipients(Vector3.zero, players, new List<bool> { true, true, true }, 40f), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(SoulMath.Recipients(Vector3.zero, players, new List<bool> { true, true, false }, 40f), Is.EqualTo(new[] { 0 }), "un joueur mort ne reçoit rien");
        }

        [Test]
        public void EclatsIndividuels_SeulementSiUnJoueurYEstPourQuelqueChose()
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            var ghoulCfg = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            PlayerHealth near = Player(new Vector3(10f, 0f, 0f)), far = Player(new Vector3(60f, 0f, 0f));
            var sgo = Track(new GameObject("Âmes (test)"));
            var souls = sgo.AddComponent<SoulSystem>();
            var so = new SerializedObject(souls);
            so.FindProperty("settings").objectReferenceValue = e;
            so.ApplyModifiedPropertiesWithoutUndo();
            Invoke(souls, "OnEnable");

            Damageable Enemy()
            {
                var go = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                var d = go.AddComponent<Damageable>();
                var dso = new SerializedObject(d);
                dso.FindProperty("settings").objectReferenceValue = ghoulCfg;
                dso.FindProperty("respawns").boolValue = false;
                dso.ApplyModifiedPropertiesWithoutUndo();
                d.Init();
                return d;
            }

            Damageable alone = Enemy();
            alone.ApplyDamage(1000f, Time.time, out bool killed);
            Assert.That(killed, Is.True);
            Assert.That(souls.Count, Is.EqualTo(0), "morte sans joueur (chute, autre ennemi) : pas d'âme");

            Damageable hit = Enemy();
            hit.NoteAttacker(far.PlayerId);                              // le joueur lointain l'a touchée (balle)
            hit.ApplyDamage(1000f, Time.time, out _);
            Assert.That(souls.CountFor(near.PlayerId), Is.EqualTo(3), "le joueur à moins de 40 m reçoit ses 3 éclats");
            Assert.That(souls.CountFor(far.PlayerId), Is.EqualTo(0), "à 60 m : rien, même s'il a tiré");
            Assert.That(souls.Count, Is.EqualTo(3), "chacun ses éclats, personne ne vole ceux d'un autre");
        }
    }
}
