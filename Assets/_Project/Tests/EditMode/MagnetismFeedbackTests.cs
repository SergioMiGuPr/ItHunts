using ItHunts.Audio;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Tests
{
    /// <summary>Retour visuel et sonore du magnétisme : règles du viseur et du refus, sons synthétisés, bloom.</summary>
    public class MagnetismFeedbackTests
    {
        private const float Dt = 1f / 60f;

        private static MagnetismState Ready()
        {
            MagnetismState m = MagnetismState.Create();
            m.AttractCharges = 3f;
            m.AimHit = true;
            m.RepelInRange = true;
            m.RepelStatus = OrbStatus.Prete;
            return m;
        }

        // ---------- Viseur ----------

        [Test]
        public void AnneauBleu_TractionPossible()
        {
            MagnetismState m = Ready();
            Assert.That(MagnetismFeedback.CanAttract(m), Is.True);
            m.AttractCharges = 0.9f;
            Assert.That(MagnetismFeedback.CanAttract(m), Is.False, "pas de charge entière");
            m = Ready();
            m.AimHit = false;
            Assert.That(MagnetismFeedback.CanAttract(m), Is.False, "rien à portée");
            m = Ready();
            m.Tracting = true;
            Assert.That(MagnetismFeedback.CanAttract(m), Is.False, "déjà en traction");
        }

        [Test]
        public void AnneauOrange_RepulsionPossible()
        {
            MagnetismState m = Ready();
            Assert.That(MagnetismFeedback.CanRepel(m), Is.True);
            m.RepelStatus = OrbStatus.Recharge;
            Assert.That(MagnetismFeedback.CanRepel(m), Is.False, "en recharge");
            m = Ready();
            m.RepelInRange = false;
            Assert.That(MagnetismFeedback.CanRepel(m), Is.False, "hors de portée (40 m)");
        }

        // ---------- Refus ----------

        [Test]
        public void Refus_ClignoteEnRouge03s_SeulementLeModeRefuse()
        {
            MagnetismState m = Ready();
            m.LastRefusalTick = 100;
            m.LastRefusalMode = MagnetMode.Attraction;
            bool On(int tick, MagnetMode mode) => MagnetismFeedback.RefusalBlinkOn(m, mode, tick, Dt, 0.3f, 0.1f);
            Assert.That(On(100, MagnetMode.Attraction), Is.True, "allumé tout de suite");
            Assert.That(On(104, MagnetMode.Attraction), Is.False, "éteint dans la 2e moitié de la période");
            Assert.That(On(107, MagnetMode.Attraction), Is.True, "rallumé à la période suivante");
            Assert.That(On(119, MagnetMode.Attraction), Is.False, "fini après 0.3 s");
            Assert.That(On(100, MagnetMode.Repulsion), Is.False, "l'autre groupe ne clignote pas");
        }

        [Test]
        public void Refus_ReelDeLOrbe_FaitClignoterLeGroupe()
        {
            var move = ScriptableObject.CreateInstance<MovementSettings>();
            var cfg = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            try
            {
                var probe = new FakeGroundProbe { Position = Vector3.zero };
                var aim = new FakeAimProbe();
                MovementState s = MovementState.Create(Vector3.zero);
                MagnetismState m = MagnetismState.Create();
                MovementInput input = MovementInput.Neutral;
                input.RepelPressedTick = s.Tick + 1;                     // rien à portée : refus
                MagnetismOrb.Step(ref m, ref s, input, cfg, move, Dt, aim);
                MovementMotor.Step(ref s, input, move, Dt, probe);
                Assume.That(m.LastRefusal, Is.EqualTo(MagnetRefusal.RienAPortee));
                Assert.That(MagnetismFeedback.RefusalBlinkOn(m, MagnetMode.Repulsion, s.Tick, Dt, 0.3f, 0.1f), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(move);
                Object.DestroyImmediate(cfg);
            }
        }

        // ---------- Sons synthétisés ----------

        private static float Peak(float[] s)
        {
            float max = 0f;
            foreach (float v in s)
                max = Mathf.Max(max, Mathf.Abs(v));
            return max;
        }

        [TestCase("TractionStart", 0.15f, 0.25f)]
        [TestCase("TractionEnd", 0.03f, 0.08f)]
        [TestCase("Repel", 0.3f, 0.5f)]
        [TestCase("Refusal", 0.15f, 0.25f)]
        [TestCase("Ready", 0.2f, 0.3f)]
        [TestCase("TractionHum", 0.99f, 1.01f)]
        public void Sons_NonVidesEtDansLesBornes(string name, float minSeconds, float maxSeconds)
        {
            float[] s = (float[])typeof(ProceduralSfx).GetMethod(name).Invoke(null, null);
            float seconds = s.Length / (float)ProceduralSfx.SampleRate;
            Assert.That(seconds, Is.InRange(minSeconds, maxSeconds));
            Assert.That(Peak(s), Is.InRange(0.3f, 1f), "audible sans saturer");
        }

        [Test]
        public void Bourdonnement_BoucleSansClaquement()
        {
            float[] s = ProceduralSfx.TractionHum();
            float maxStep = 0f;
            for (int i = 1; i < s.Length; i++)
                maxStep = Mathf.Max(maxStep, Mathf.Abs(s[i] - s[i - 1]));
            Assert.That(Mathf.Abs(s[0] - s[s.Length - 1]), Is.LessThanOrEqualTo(maxStep * 1.01f),
                "la fin rejoint le début comme deux échantillons voisins");
        }

        [Test]
        public void Sons_DeviennentDesAudioClips()
        {
            AudioClip clip = ProceduralSfx.ToClip("test", ProceduralSfx.Repel());
            try
            {
                Assert.That(clip.frequency, Is.EqualTo(ProceduralSfx.SampleRate));
                Assert.That(clip.channels, Is.EqualTo(1));
                Assert.That(clip.samples, Is.GreaterThan(1000));
            }
            finally
            {
                Object.DestroyImmediate(clip);
            }
        }

        // ---------- Lueur : bloom et caméra (assets générés par ItHunts > Reconstruire…) ----------

        [Test]
        public void Bloom_ConfigureDansLaSceneDeTest()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PostProcess_TestMovement.asset");
            Assert.That(profile, Is.Not.Null);
            Assert.That(profile.TryGet(out Bloom bloom), Is.True);
            Assert.That(bloom.active, Is.True);
            Assert.That(bloom.intensity.value, Is.GreaterThan(0f));
            Assert.That(bloom.threshold.value, Is.GreaterThanOrEqualTo(1f), "seules les couleurs HDR brillent");
        }

        [Test]
        public void PrefabJoueur_PostProcessEtRetourBranches()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            Assert.That(prefab, Is.Not.Null);
            var camera = prefab.GetComponentInChildren<Camera>(true);
            Assert.That(camera.GetUniversalAdditionalCameraData().renderPostProcessing, Is.True);
            Assert.That(prefab.GetComponent<MagnetismAudio>(), Is.Not.Null);
            foreach (var c in new Component[] { prefab.GetComponent<MagnetismAudio>(), prefab.GetComponent<MagnetismOrbView>(),
                         prefab.GetComponent<ItHunts.Hud.MovementHud>() })
            {
                var so = new SerializedObject(c);
                SerializedProperty p = so.FindProperty("settings") ?? so.FindProperty("feedback");
                Assert.That(p?.objectReferenceValue, Is.Not.Null, $"{c.GetType().Name} : réglages du retour assignés");
            }
        }
    }
}
