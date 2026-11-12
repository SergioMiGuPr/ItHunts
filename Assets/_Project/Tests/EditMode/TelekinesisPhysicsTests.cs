using System.Collections.Generic;
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
    /// pas touchée) : un objet attrapé loin doit voler jusqu'au point de maintien puis y rester ; un objet tenu reste
    /// devant le joueur en traction rapide et en bunny hop (orbe unique, D-024).
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

        /// <summary>
        /// Fait suivre au porteur une trajectoire (position des yeux et visée à chaque tick), dans l'ordre du jeu : le
        /// joueur bouge, puis l'objet reçoit sa vitesse (Drive, avec le déplacement réel des yeux), puis la physique avance.
        /// L'écart est mesuré au même instant que la caméra : objet après la physique contre point de maintien des yeux de
        /// ce tick. Renvoie l'écart maximal et moyen (l'objet part du point de maintien, déjà tenu).
        /// </summary>
        private (float max, float mean) Follow(List<(Vector3 eye, Vector3 aim)> path, Vector3 startEye, Vector3 startAim)
        {
            var owner = new GameObject("Porteur");
            SceneManager.MoveGameObjectToScene(owner, _scene);
            var cc = owner.AddComponent<CharacterController>();
            Grabbable crate = Crate(TelekinesisHolder.HoldPoint(_cfg, startEye, startAim), 25f);
            PhysicsScene physics = _scene.GetPhysicsScene();
            var holder = new TelekinesisHolder(cc, new PhysicsGrabProbe(~0, owner.transform));
            Assert.That(holder.Apply(new TelekinesisCommand { Kind = TelekinesisCommandKind.Attraper, TargetId = crate.Id },
                _cfg, startEye, Vector3.zero), Is.True);

            float max = 0f, sum = 0f;
            int n = 0;
            Vector3 lastEye = startEye;
            foreach ((Vector3 eye, Vector3 aim) in path)
            {
                Assert.That(holder.Drive(_cfg, eye, aim, (eye - lastEye) / Dt, Dt), Is.True, $"lâché au tick {n}");
                lastEye = eye;
                physics.Simulate(Dt);
                float d = Vector3.Distance(TelekinesisHolder.HoldPoint(_cfg, eye, aim), crate.Body.position);
                max = Mathf.Max(max, d);
                sum += d;
                n++;
            }

            Assert.That(holder.Held, Is.SameAs(crate), "toujours tenu");
            return (max, sum / Mathf.Max(1, n));
        }

        /// <summary>Traction en ligne droite vers un point en hauteur : accélération 80 m/s² jusqu'à la vitesse donnée, 2 s.</summary>
        [TestCase(0f, 30f, TestName = "Physique_ObjetTenu_TractionDe0a30ms_ResteDevant")]
        [TestCase(40f, 40f, TestName = "Physique_ObjetTenu_TractionA40ms_ResteDevant")]
        public void ObjetTenu_TractionRapide(float startSpeed, float tractionSpeed)
        {
            Vector3 dir = new Vector3(0.3f, 0.5f, 0.8f).normalized;
            Vector3 eye = new Vector3(0f, 1.6f, 0f);
            float speed = startSpeed;
            var path = new List<(Vector3 eye, Vector3 aim)>();
            for (int i = 0; i < 120; i++)
            {
                speed = Mathf.Max(speed, Mathf.Min(tractionSpeed, speed + 80f * Dt));
                Vector3 v = dir * speed;
                eye += v * Dt;
                path.Add((eye, dir));
            }

            (float max, float mean) = Follow(path, new Vector3(0f, 1.6f, 0f), dir);
            TestContext.WriteLine($"Traction {startSpeed:0} -> {tractionSpeed:0} m/s : écart max {max:0.000} m, moyen {mean:0.000} m");
            Debug.Log($"[Mesure] Traction {startSpeed:0} -> {tractionSpeed:0} m/s : écart max {max:0.000} m, moyen {mean:0.000} m");
            Assert.That(max, Is.LessThan(0.15f), "l'objet reste devant (écart max)");
            Assert.That(mean, Is.LessThan(0.05f));
        }

        /// <summary>
        /// Bunny hop : sauts de 1.3 m (gravité 20, chute x1.5) enchaînés dès l'atterrissage, avec un virage en strafe (la
        /// visée tourne de 90°/s, la vitesse suit), 3 s.
        /// </summary>
        [TestCase(20f, TestName = "Physique_ObjetTenu_BunnyHopA20ms_ResteDevant")]
        [TestCase(26f, TestName = "Physique_ObjetTenu_BunnyHopA26ms_ResteDevant")]
        public void ObjetTenu_BunnyHop(float horizontalSpeed)
        {
            float jumpSpeed = Mathf.Sqrt(2f * 20f * 1.3f);
            Vector3 feet = Vector3.zero;
            float vy = jumpSpeed, yaw = 0f;
            var path = new List<(Vector3 eye, Vector3 aim)>();
            for (int i = 0; i < 180; i++)
            {
                yaw += 90f * Dt;
                Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                vy -= (vy < 0f ? 30f : 20f) * Dt;
                Vector3 v = forward * horizontalSpeed + Vector3.up * vy;
                feet += v * Dt;
                if (feet.y <= 0f)
                {
                    feet.y = 0f;
                    vy = jumpSpeed;                                   // saut dès l'atterrissage
                    v.y = vy;
                }

                path.Add((feet + Vector3.up * 1.6f, forward));
            }

            (float max, float mean) = Follow(path, new Vector3(0f, 1.6f, 0f), Vector3.forward);
            TestContext.WriteLine($"Bunny hop {horizontalSpeed:0} m/s : écart max {max:0.000} m, moyen {mean:0.000} m");
            Debug.Log($"[Mesure] Bunny hop {horizontalSpeed:0} m/s : écart max {max:0.000} m, moyen {mean:0.000} m");
            Assert.That(max, Is.LessThan(0.2f), "l'objet reste devant (écart max, atterrissages et virage compris)");
            Assert.That(mean, Is.LessThan(0.2f));
        }

        /// <summary>
        /// La visée du magnétisme ignore l'objet tenu (orbe unique : traction et répulsion en tenant une caisse), mais
        /// pas un autre objet.
        /// </summary>
        [Test]
        public void Physique_ViseeDuMagnetisme_IgnoreLObjetTenu()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = Vector3.forward * 20f;
            wall.transform.localScale = new Vector3(10f, 10f, 1f);
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(crate, _scene);
            crate.transform.position = Vector3.forward * 2.5f;
            crate.transform.localScale = Vector3.one * 0.9f;
            var body = crate.AddComponent<Rigidbody>();
            body.isKinematic = true;
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(Dt);

            var probe = new PhysicsAimProbe(~0) { Scene = _scene.GetPhysicsScene() };
            Assert.That(probe.Raycast(Vector3.zero, Vector3.forward, 60f, out AimHit hit), Is.True);
            Assert.That(hit.Distance, Is.EqualTo(2.05f).Within(0.01f), "sans objet tenu, la caisse arrête le rayon");

            probe.IgnoreBody = body;
            Assert.That(probe.Raycast(Vector3.zero, Vector3.forward, 60f, out hit), Is.True);
            Assert.That(hit.Distance, Is.EqualTo(19.5f).Within(0.01f), "tenue, la caisse est ignorée : le mur derrière");
        }

        // ---------- Aide à la visée (attraper) ----------

        private static readonly Vector3 Eye = new Vector3(0f, 1.6f, 0f);

        /// <summary>Caisse figée (cinématique) dont le centre est à cette distance devant et ce décalage latéral (m).</summary>
        private Grabbable StillCrate(float forward, float lateral, float mass = 25f)
        {
            Grabbable g = Crate(Eye + new Vector3(lateral, 0f, forward), mass);
            g.Body.isKinematic = true;
            return g;
        }

        private GameObject Wall(Vector3 center, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = center;
            wall.transform.localScale = size;
            return wall;
        }

        private bool Select(out GrabCandidate c, Vector3? aim = null)
        {
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(Dt);
            var probe = new PhysicsGrabProbe(~0, null) { Settings = _cfg, Scene = _scene.GetPhysicsScene() };
            return probe.FindGrabbable(Eye, aim ?? Vector3.forward, _cfg.range, out c);
        }

        [Test]
        public void Aide_SelectionneUnObjetDansLeCone_PasAuDela()
        {
            // À 10 m, le cône vaut 7.5° ; la caisse fait 0.9 m : son bord est à 2.6° de moins que son centre.
            Grabbable inside = StillCrate(10f, 10f * Mathf.Tan(9f * Mathf.Deg2Rad));     // centre à 9°, bord à ≈ 6.4°
            Assert.That(Select(out GrabCandidate c), Is.True, "dans le cône");
            Assert.That(c.Id, Is.EqualTo(inside.Id));
            Assert.That(c.Angle, Is.GreaterThan(0f).And.LessThan(GrabAssist.ConeAngle(_cfg, c.Distance)));

            inside.transform.position = Eye + new Vector3(10f * Mathf.Tan(13f * Mathf.Deg2Rad), 0f, 10f);   // bord à ≈ 10.5°
            Assert.That(Select(out _), Is.False, "hors du cône");

            _cfg.aimAssist = false;
            inside.transform.position = Eye + new Vector3(10f * Mathf.Tan(5f * Mathf.Deg2Rad), 0f, 10f);
            Assert.That(Select(out _), Is.False, "aide coupée : rayon exact seulement");
        }

        [Test]
        public void Aide_LePlusProcheDuViseur_PasLePlusProcheDuJoueur()
        {
            Grabbable near = StillCrate(4f, 0.9f);       // à 4 m, bord à ≈ 7.2° du viseur (dans le cône de 8°)
            Grabbable far = StillCrate(20f, 0.6f);       // à 20 m, bord à ≈ 0.4° du viseur
            Assert.That(Select(out GrabCandidate c), Is.True);
            Assert.That(c.Id, Is.EqualTo(far.Id), "le plus proche du viseur");
            Assert.That(near, Is.Not.Null);
        }

        [Test]
        public void Aide_JamaisATraversUnMur()
        {
            StillCrate(15f, 0.8f);
            Wall(Eye + new Vector3(0f, 0f, 8f), new Vector3(6f, 6f, 0.5f));
            Assert.That(Select(out _), Is.False, "caisse cachée par un mur");
        }

        [Test]
        public void Aide_ObjetVisibleACoteDUnMurViseDevant()
        {
            // Le viseur touche un muret à 6 m, la caisse est visible juste au-dessus : l'aide la prend.
            Grabbable crate = StillCrate(12f, 0f);
            crate.transform.position = Eye + new Vector3(0f, 1.2f, 12f);
            Wall(Eye + new Vector3(0f, -0.5f, 6f), new Vector3(4f, 1.4f, 0.3f));
            Assert.That(Select(out GrabCandidate c), Is.True);
            Assert.That(c.Id, Is.EqualTo(crate.Id));
        }

        [Test]
        public void Aide_PasDerriereNiLoinSurLeCote()
        {
            StillCrate(-5f, 0f);                         // derrière
            StillCrate(0.5f, 6f);                        // sur le côté, à 85°
            StillCrate(30f, 30f);                        // à 45°, loin
            Assert.That(Select(out _), Is.False);
        }

        [Test]
        public void Aide_PasPourLesObjetsTropLourds_MaisLeRayonExactLesMontre()
        {
            Grabbable heavy = StillCrate(10f, 0.9f, mass: 60f);                         // hors du rayon, dans le cône
            Assert.That(Select(out _), Is.False, "l'aide ignore les objets trop lourds");
            heavy.transform.position = Eye + new Vector3(0f, 0f, 10f);                  // sur le rayon exact
            Assert.That(Select(out GrabCandidate c), Is.True);
            Assert.That(c.Id, Is.EqualTo(heavy.Id));
            Assert.That(c.Angle, Is.EqualTo(0f), "rayon exact");
        }
    }
}
