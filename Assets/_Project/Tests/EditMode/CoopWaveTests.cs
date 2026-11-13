using System.Collections.Generic;
using System.Reflection;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Vagues en coop et accalmie (D-041) : multiplicateur, parts, tour de rôle, distance, plafond d'accalmie, choix de cible.</summary>
    public class CoopWaveTests
    {
        private WaveSettings _s;
        private GhoulSettings _g;
        private readonly List<Object> _made = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _s = ScriptableObject.CreateInstance<WaveSettings>();
            _g = ScriptableObject.CreateInstance<GhoulSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
                if (o is GameObject go)
                {
                    foreach (var p in go.GetComponents<PlayerHealth>())
                        Invoke(p, "OnDisable");
                    Object.DestroyImmediate(go);
                }
                else
                    Object.DestroyImmediate(o);
            _made.Clear();
            Object.DestroyImmediate(_s);
            Object.DestroyImmediate(_g);
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        private PlayerHealth Player(Vector3 at)
        {
            var go = new GameObject("Joueur (test)");
            go.transform.position = at;
            var p = go.AddComponent<PlayerHealth>();
            p.Init();
            Invoke(p, "OnEnable");
            _made.Add(go);
            return p;
        }

        [Test]
        public void Coop_Multiplicateur_Parts_TourDeRole()
        {
            Assert.That(WaveMath.PlayerMultiplier(1, _s), Is.EqualTo(1f));
            Assert.That(WaveMath.PlayerMultiplier(2, _s), Is.EqualTo(1.5f), "×1.5 avec un joueur en plus");
            Assert.That(WaveMath.PlayerMultiplier(4, _s), Is.EqualTo(2.5f));
            Assert.That(WaveMath.PlayerMultiplier(0, _s), Is.EqualTo(1f), "un joueur mort ne compte pas");
            Assert.That(WaveMath.Shares(10, 3), Is.EqualTo(new[] { 4, 3, 3 }));
            Assert.That(WaveMath.Shares(53, 2), Is.EqualTo(new[] { 27, 26 }));
            // À tour de rôle parmi ceux à qui il manque des ennemis.
            Assert.That(WaveMath.NextPlayer(new[] { 2, 2 }, 0), Is.EqualTo(0));
            Assert.That(WaveMath.NextPlayer(new[] { 2, 2 }, 1), Is.EqualTo(1));
            Assert.That(WaveMath.NextPlayer(new[] { 0, 3, 1 }, 2), Is.EqualTo(2));
            Assert.That(WaveMath.NextPlayer(new[] { 0, 3, 0 }, 2), Is.EqualTo(1), "le suivant à qui il en manque");
            Assert.That(WaveMath.NextPlayer(new[] { 0, 0 }, 0), Is.EqualTo(-1));
            // Simulation : 2 joueurs, 10 voulus, apparitions à tour de rôle : 5 chacun.
            var owned = new int[2];
            int rr = 0;
            for (int i = 0; i < 10; i++)
            {
                int[] shares = WaveMath.Shares(10, 2);
                int who = WaveMath.NextPlayer(new[] { shares[0] - owned[0], shares[1] - owned[1] }, rr);
                owned[who]++;
                rr = who + 1;
            }

            Assert.That(owned, Is.EqualTo(new[] { 5, 5 }));
        }

        [Test]
        public void Coop_Point_25mDeChacun_80mDuJoueurChoisi()
        {
            var players = new List<Vector3> { Vector3.zero, new Vector3(150f, 0f, 0f) };
            Assert.That(WaveMath.DistanceOkFor(new Vector3(60f, 0f, 0f), players, 0, 25f, 80f), Is.True);
            Assert.That(WaveMath.DistanceOkFor(new Vector3(60f, 0f, 0f), players, 1, 25f, 80f), Is.False, "à 90 m du joueur 2");
            Assert.That(WaveMath.DistanceOkFor(new Vector3(130f, 0f, 0f), players, 0, 25f, 80f), Is.False, "à 20 m du joueur 2");
            Assert.That(WaveMath.DistanceOkFor(new Vector3(100f, 0f, 0f), players, 1, 25f, 80f), Is.True);
        }

        [Test]
        public void Accalmie_PlafondDeChasse60PourCent_ReculDe10m()
        {
            var a = AssetDatabase.LoadAssetAtPath<WaveSettings>("Assets/_Project/Settings/Waves_Default.asset");
            Assert.That(new[] { a.lullHuntingShare, a.extraPlayerShare }, Is.EqualTo(new[] { 0.6f, 0.5f }));
            Assert.That(WaveMath.PackShare(true, _s), Is.EqualTo(1f));
            Assert.That(WaveMath.PackShare(false, _s), Is.EqualTo(0.6f));
            Assert.That(WaveMath.ScaledCap(25, 1f * WaveMath.PackShare(false, _s)), Is.EqualTo(15), "25 en chasse → 15 pendant l'accalmie");
            Assert.That(WaveMath.ScaledCap(25, 1.5f * WaveMath.PackShare(false, _s)), Is.EqualTo(22), "à 2 joueurs : 25 × 1.5 × 0.6 = 22.5, arrondi à 22");
            Assert.That(_g.lullRetreatDistance, Is.EqualTo(10f));
            Vector3 r = EnemyMath.RetreatPoint(new Vector3(5f, 0f, 0f), Vector3.zero, 10f);
            Assert.That(r, Is.EqualTo(new Vector3(15f, 0f, 0f)), "il recule de 10 m, loin du joueur");
        }

        [Test]
        public void Cible_Marge15m_Abandon60m_Delai3s()
        {
            Assert.That(new[] { _g.targetSwitchMargin, _g.targetGiveUpDistance, _g.targetSwitchCooldown }, Is.EqualTo(new[] { 15f, 60f, 3f }));
            Assert.That(EnemyMath.ShouldSwitchTarget(30f, 20f, 10f, _g), Is.False, "10 m plus près seulement : il garde sa cible");
            Assert.That(EnemyMath.ShouldSwitchTarget(30f, 15f, 10f, _g), Is.True, "15 m plus près");
            Assert.That(EnemyMath.ShouldSwitchTarget(30f, 5f, 2.9f, _g), Is.False, "moins de 3 s depuis le dernier changement");
            Assert.That(EnemyMath.ShouldSwitchTarget(61f, 55f, 5f, _g), Is.True, "cible à plus de 60 m, l'autre plus près");
            Assert.That(EnemyMath.ShouldSwitchTarget(61f, 70f, 5f, _g), Is.False, "l'autre est plus loin encore");
        }

        [Test]
        public void Cible_PasDOscillation_ExceptionsImmediates()
        {
            PlayerHealth a = Player(new Vector3(-20f, 0f, 0f)), b = Player(new Vector3(20f, 0f, 0f));
            var enemy = new GameObject("Goule (test)");
            _made.Add(enemy);
            Damageable self = enemy.AddComponent<Damageable>();
            self.Init();
            Vector3 me = Vector3.zero;
            float Len(PlayerHealth p) => Vector3.Distance(me, p.transform.position);
            var keeper = new TargetKeeper();

            PlayerHealth first = keeper.Update(0f, self, _g, 0.5f, Len);
            Assert.That(first, Is.Not.Null, "premier choix");
            PlayerHealth other = first == a ? b : a;
            // À égale distance, l'autre qui s'approche ou s'éloigne un peu : aucun changement.
            for (int i = 1; i <= 120; i++)
            {
                me = new Vector3(Mathf.Sin(i * 0.3f) * 6f, 0f, 0f);
                keeper.Update(i * 0.5f, self, _g, 0.5f, Len);
            }

            Assert.That(keeper.Switches, Is.EqualTo(0), "pas d'oscillation à égale distance");
            Assert.That(keeper.Target, Is.EqualTo(first));

            // L'autre joueur le blesse : il se retourne contre lui tout de suite (même sous 3 s).
            self.NoteAttacker(other.PlayerId);
            Assert.That(keeper.Update(61f, self, _g, 0.5f, Len), Is.EqualTo(other), "blessé par l'autre joueur : immédiat");
            Assert.That(keeper.AttackSwitches, Is.EqualTo(1));

            // Sa cible meurt : la plus proche, tout de suite.
            other.Kill(other.transform.position, 61.1f);
            Assert.That(keeper.Update(61.2f, self, _g, 0.5f, Len), Is.EqualTo(first), "cible morte : immédiat");

            // Seul joueur vivant : il le garde, un joueur mort ne compte pas.
            Assert.That(keeper.Update(70f, self, _g, 0.5f, Len), Is.EqualTo(first));
        }
    }
}
