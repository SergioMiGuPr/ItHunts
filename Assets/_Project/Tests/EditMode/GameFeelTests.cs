using ItHunts.Level;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Sensations de jeu (points 4 et 9 du 2026-11-21) : courbes, échelles du joueur, réglages des Paramètres.</summary>
    public class GameFeelTests
    {
        private GameFeelSettings _s;
        private float _intensity;
        private bool _reduce;

        [SetUp]
        public void SetUp()
        {
            _s = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameFeelSettings>("Assets/_Project/Settings/GameFeel_Default.asset"));
            _intensity = GameSettings.FeelIntensity;
            _reduce = GameSettings.ReduceShake;
        }

        [TearDown]
        public void TearDown()
        {
            GameSettings.FeelIntensity = _intensity;
            GameSettings.ReduceShake = _reduce;
            Object.DestroyImmediate(_s);
        }

        [Test]
        public void Hissage_PlongeonPuisRemonteeEtInclinaison_RevientAZero()
        {
            Assert.IsTrue(_s.mantleCamera);
            float lowest = 0f, lowestT = 0f, highestAfter = float.NegativeInfinity, maxRoll = 0f;
            for (float t = 0f; t < _s.mantleCameraTime; t += 0.005f)
            {
                PlayerFeel.MantleCurve(_s, t, 1f, out Vector3 o, out Vector3 e);
                if (o.y < lowest)
                {
                    lowest = o.y;
                    lowestT = t;
                }

                if (t > _s.mantleCameraTime * _s.mantleDipShare)
                    highestAfter = Mathf.Max(highestAfter, o.y);
                maxRoll = Mathf.Max(maxRoll, Mathf.Abs(e.z));
            }

            Assert.That(lowest, Is.EqualTo(-_s.mantleDipDepth).Within(0.01f), "plongeon");
            Assert.That(lowestT, Is.EqualTo(_s.mantleCameraTime * _s.mantleDipShare).Within(0.02f), "au bout de la part de plongeon");
            Assert.That(highestAfter, Is.GreaterThan(0f), "remontée avec un léger dépassement");
            Assert.That(maxRoll, Is.EqualTo(_s.mantleRoll).Within(0.05f), "petite inclinaison");
            PlayerFeel.MantleCurve(_s, _s.mantleCameraTime, 1f, out Vector3 end, out Vector3 endE);
            Assert.That(end.magnitude + endE.magnitude, Is.LessThan(1e-4f), "fin : caméra à sa place");
            PlayerFeel.MantleCurve(_s, _s.mantleCameraTime * 0.99f, 1f, out Vector3 near, out _);
            Assert.That(Mathf.Abs(near.y), Is.LessThan(0.01f), "sans à-coup à la fin");
        }

        [Test]
        public void Echelles_IntensiteGlobaleEtReduireLesSecousses()
        {
            GameSettings.FeelIntensity = 1f;
            GameSettings.ReduceShake = false;
            Assert.That(GameFeel.MotionScale(_s), Is.EqualTo(1f));
            GameSettings.ReduceShake = true;
            Assert.That(GameFeel.MotionScale(_s), Is.EqualTo(_s.reducedShakeScale).Within(1e-4f));
            GameSettings.FeelIntensity = 0.5f;
            Assert.That(GameFeel.MotionScale(_s), Is.EqualTo(0.5f * _s.reducedShakeScale).Within(1e-4f));
            GameSettings.FeelIntensity = 0f;
            Assert.That(GameFeel.MotionScale(_s), Is.EqualTo(0f));
        }

        [Test]
        public void Parametres_DeuxLignesDeSensations()
        {
            CollectionAssert.Contains(GameSettings.Labels, "Intensité des effets");
            CollectionAssert.Contains(GameSettings.Labels, "Réduire les secousses");
            GameSettings.FeelIntensity = 1f;
            GameSettings.Step(GameSettings.Line.IntensiteEffets, -1);
            Assert.That(GameSettings.FeelIntensity, Is.EqualTo(0.9f).Within(1e-4f), "pas de 10 %");
            Assert.That(GameSettings.Value(GameSettings.Line.IntensiteEffets), Is.EqualTo("90 %"));
            GameSettings.ReduceShake = false;
            GameSettings.Step(GameSettings.Line.ReduireSecousses, 1);
            Assert.IsTrue(GameSettings.ReduceShake);
            Assert.That(GameSettings.Value(GameSettings.Line.ReduireSecousses), Is.EqualTo("oui"));
        }

        [Test]
        public void Joueur_PlayerFeelBrancheSurSesReglages()
        {
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var feel = player.GetComponent<PlayerFeel>();
            Assert.IsNotNull(feel, "PlayerFeel sur le prefab du joueur");
            Assert.IsNotNull(feel.Settings, "réglages GameFeel_Default");
            Assert.IsNotNull(player.GetComponent<CameraShake>(), "composée par CameraShake");
        }
    }
}
