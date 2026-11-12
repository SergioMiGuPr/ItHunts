using System.Reflection;
using ItHunts.Orbs;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Tests
{
    /// <summary>
    /// Télékinésie avec de vrais corps physiques, dans une scène isolée (sa propre simulation : la scène ouverte n'est
    /// pas touchée) : un objet attrapé loin doit voler jusqu'au point de maintien puis y rester.
    /// </summary>
    public class TelekinesisPhysicsTests
    {
        private const float Dt = 1f / 60f;
        private Scene _scene;
        private TelekinesisOrbSettings _cfg;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            _cfg = ScriptableObject.CreateInstance<TelekinesisOrbSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _scene.GetRootGameObjects())
            {
                var g = go.GetComponent<Grabbable>();
                if (g != null)
                    Invoke(g, "OnDisable");                         // retire l'objet du registre
            }

            EditorSceneManager.ClosePreviewScene(_scene);
            Object.DestroyImmediate(_cfg);
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        private Grabbable Crate(Vector3 position, float mass)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.9f;
            go.AddComponent<Rigidbody>().mass = mass;
            var g = go.AddComponent<Grabbable>();
            Invoke(g, "Awake");                                      // pas de cycle de vie en mode édition
            Invoke(g, "OnEnable");
            return g;
        }

        [TestCase(10f, TestName = "Physique_ObjetA10m_VientEtResteAuPointDeMaintien")]
        [TestCase(55f, TestName = "Physique_ObjetA55m_VientEtResteAuPointDeMaintien")]
        public void ObjetAttrape_VoleJusquAuPointDeMaintien(float distance)
        {
            var owner = new GameObject("Porteur");
            SceneManager.MoveGameObjectToScene(owner, _scene);
            var cc = owner.AddComponent<CharacterController>();
            Grabbable crate = Crate(new Vector3(0f, 0.45f, distance), 25f);
            PhysicsScene physics = _scene.GetPhysicsScene();
            var holder = new TelekinesisHolder(cc, new PhysicsGrabProbe(~0, owner.transform));
            Vector3 eye = new Vector3(0f, 1.6f, 0f), aim = Vector3.forward;
            Vector3 hold = TelekinesisHolder.HoldPoint(_cfg, eye, aim);

            Assert.That(holder.Apply(new TelekinesisCommand { Kind = TelekinesisCommandKind.Attraper, TargetId = crate.Id },
                _cfg, eye, Vector3.zero), Is.True);
            Assert.That(crate.Body.isKinematic, Is.False);
            Assert.That(crate.Body.useGravity, Is.False, "pas de gravité tant qu'il est tenu");

            float start = Vector3.Distance(hold, crate.Body.position);
            float maxSpeed = 0f, afterHalfSecond = 0f;
            int arrivalTick = -1;
            for (int i = 1; i <= 180; i++)                           // 3 s
            {
                Assert.That(holder.Drive(_cfg, eye, aim, Vector3.zero, Dt), Is.True, $"lâché au tick {i}");
                physics.Simulate(Dt);
                float d = Vector3.Distance(hold, crate.Body.position);
                maxSpeed = Mathf.Max(maxSpeed, crate.Body.linearVelocity.magnitude);
                if (i == 30)
                    afterHalfSecond = d;
                if (arrivalTick < 0 && d < 0.3f)
                    arrivalTick = i;
            }

            Assert.That(afterHalfSecond, Is.LessThan(start - Mathf.Min(start - 0.3f, _cfg.approachSpeed * 0.3f)), "il vole vers le joueur");
            Assert.That(arrivalTick, Is.GreaterThan(0), "il arrive au point de maintien");
            Assert.That(arrivalTick * Dt, Is.LessThan(start / _cfg.approachSpeed + 0.6f), "à la vitesse d'approche");
            Assert.That(maxSpeed, Is.LessThanOrEqualTo(_cfg.approachSpeed + 0.5f), "sans dépasser la vitesse d'approche");
            Assert.That(Vector3.Distance(hold, crate.Body.position), Is.LessThan(0.1f), "il reste au point de maintien");
            Assert.That(holder.Held, Is.SameAs(crate));
        }
    }
}
