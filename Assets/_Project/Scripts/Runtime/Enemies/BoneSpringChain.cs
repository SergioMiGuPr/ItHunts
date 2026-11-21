using System;
using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Ressort pour chaînes d'os (point 10 de la nuit du 2026-11-21, préparé pour le traqueur v4, inactif) : chaque os suit sa pose
    /// animée par un ressort (raideur, amortissement), subit la gravité et l'inertie de la racine, garde sa longueur et sort des
    /// collisions sphère et capsule. Intégration de Verlet à pas fixe ; LateUpdate après l'Animator. Testé sur une chaîne factice
    /// (vitrine) ; le calcul (Solve) est pur et testé en EditMode.
    /// </summary>
    public sealed class BoneSpringChain : MonoBehaviour
    {
        [Tooltip("Premier os de la chaîne (la racine reste animée) ; les enfants en ligne sont simulés.")]
        public Transform root;
        public BoneSpringSettings settings = BoneSpringSettings.Default;
        [Tooltip("Sphères (Transform : centre, échelle x : rayon) qui repoussent les os.")]
        public List<SphereCollider> sphereColliders = new List<SphereCollider>();
        public List<CapsuleCollider> capsuleColliders = new List<CapsuleCollider>();

        /// <summary>Obstacle posé sur des os (sans collider : rien ne change pour la physique du jeu) : sphère sur a, ou capsule de a à b.</summary>
        [System.Serializable]
        public struct BoneObstacle
        {
            public Transform a, b;
            public float radius;
        }

        [Tooltip("Obstacles sur les os du modèle (tête, cou, épaules, dos) : les cheveux n'y entrent jamais.")]
        public List<BoneObstacle> boneObstacles = new List<BoneObstacle>();
        [Tooltip("Pas de simulation (s).")]
        [Min(0.002f)] public float step = 1f / 60f;

        private Transform[] _bones = new Transform[0];
        private Particle[] _p = new Particle[0];
        private float _accum;
        private Vector3 _lastRoot;

        public struct Particle
        {
            public Vector3 Position, Previous;
            public float Length;
        }

        /// <summary>Un obstacle : sphère (a == b) ou capsule (segment a-b), rayon.</summary>
        public struct Obstacle
        {
            public Vector3 A, B;
            public float Radius;
        }

        public int Bones => _bones.Length;
        public Vector3 Tip => _p.Length > 0 ? _p[_p.Length - 1].Position : transform.position;

        private void OnEnable() => Setup();

        public void Setup()
        {
            var list = new List<Transform>();
            Transform t = root != null ? root : transform;
            while (t != null)
            {
                list.Add(t);
                t = NextBone(t);
            }

            _bones = list.ToArray();
            _localPos = new Vector3[_bones.Length];
            _localRot = new Quaternion[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
            {
                _localPos[i] = _bones[i].localPosition;
                _localRot[i] = _bones[i].localRotation;
            }

            _p = new Particle[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
            {
                _p[i].Position = _p[i].Previous = _bones[i].position;
                _p[i].Length = i > 0 ? Vector3.Distance(_bones[i].position, _bones[i - 1].position) : 0f;
            }

            _lastRoot = _bones.Length > 0 ? _bones[0].position : transform.position;
        }

        /// <summary>Os suivant : l'enfant qui porte le plus de descendants (un maillon visuel feuille n'est pas un os).</summary>
        private static Transform NextBone(Transform t)
        {
            Transform best = null;
            int most = -1;
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                int n = c.GetComponentsInChildren<Transform>(true).Length;
                if (n > most)
                {
                    most = n;
                    best = c;
                }
            }

            return best;
        }

        private readonly List<Obstacle> _obstacles = new List<Obstacle>();
        private Vector3[] _rest = new Vector3[0];

        private Vector3[] _localPos = new Vector3[0];
        private Quaternion[] _localRot = new Quaternion[0];

        /// <summary>Pose d'origine remise avant l'Animator (s'il y en a un, il la remplace) : la simulation part toujours de la
        /// pose animée de l'image, jamais de la sienne.</summary>
        private void Update()
        {
            for (int i = 1; i < _bones.Length; i++)
            {
                _bones[i].localPosition = _localPos[i];
                _bones[i].localRotation = _localRot[i];
            }

            if (_bones.Length > 0)
                _bones[0].localRotation = _localRot[0];
        }

        /// <summary>Coût mesuré (Profiler : « Traqueur.Cheveux »).</summary>
        public static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker(Unity.Profiling.ProfilerCategory.Scripts, "Traqueur.Cheveux");

        private void LateUpdate()
        {
            if (_bones.Length < 2)
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            if (_rest.Length != _bones.Length)
                _rest = new Vector3[_bones.Length];
            for (int i = 0; i < _bones.Length; i++)
                _rest[i] = _bones[i].position;                       // pose animée de cette image

            _obstacles.Clear();
            foreach (SphereCollider s in sphereColliders)
                if (s != null && s.enabled)
                {
                    Vector3 c = s.transform.TransformPoint(s.center);
                    _obstacles.Add(new Obstacle { A = c, B = c, Radius = s.radius * MaxScale(s.transform) });
                }

            foreach (CapsuleCollider c in capsuleColliders)
                if (c != null && c.enabled)
                {
                    Vector3 axis = c.direction == 0 ? Vector3.right : c.direction == 1 ? Vector3.up : Vector3.forward;
                    float half = Mathf.Max(0f, c.height * 0.5f - c.radius);
                    _obstacles.Add(new Obstacle
                    {
                        A = c.transform.TransformPoint(c.center + axis * half), B = c.transform.TransformPoint(c.center - axis * half),
                        Radius = c.radius * MaxScale(c.transform),
                    });
                }

            foreach (BoneObstacle o in boneObstacles)
                if (o.a != null)
                    _obstacles.Add(new Obstacle { A = o.a.position, B = o.b != null ? o.b.position : o.a.position, Radius = o.radius });

            _accum = Mathf.Min(_accum + Time.deltaTime, step * 4f);
            Vector3 rootMove = _bones[0].position - _lastRoot;
            _lastRoot = _bones[0].position;
            bool first = true;
            while (_accum >= step)
            {
                _accum -= step;
                Solve(_p, _rest, first ? rootMove : Vector3.zero, settings, _obstacles, step);
                first = false;
            }

            // Orientation : chaque os vise le suivant simulé (la racine reste animée).
            for (int i = 0; i < _bones.Length - 1; i++)
            {
                Vector3 animDir = _rest[i + 1] - _rest[i];
                Vector3 simDir = _p[i + 1].Position - _p[i].Position;
                if (animDir.sqrMagnitude > 1e-8f && simDir.sqrMagnitude > 1e-8f)
                    _bones[i].rotation = Quaternion.FromToRotation(animDir, simDir) * _bones[i].rotation;
                _bones[i + 1].position = _p[i + 1].Position;
            }
        }

        private static float MaxScale(Transform t) => Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));

        /// <summary>
        /// Un pas (pur, testable) : la racine suit sa pose ; chaque os : Verlet (vitesse amortie, gravité, inertie de la racine),
        /// ressort vers sa pose animée, longueur gardée, sortie des obstacles.
        /// </summary>
        public static void Solve(Particle[] p, Vector3[] rest, Vector3 rootMove, BoneSpringSettings s, IList<Obstacle> obstacles, float dt)
        {
            if (p.Length == 0)
                return;
            p[0].Previous = p[0].Position;
            p[0].Position = rest[0];
            for (int i = 1; i < p.Length; i++)
            {
                Vector3 velocity = (p[i].Position - p[i].Previous) * (1f - s.damping);
                p[i].Previous = p[i].Position;
                // Inertie : une part du mouvement de la racine est suivie tout de suite, le reste traîne (puis le ressort rattrape).
                p[i].Position += velocity + rootMove * s.inertia + s.gravity * (dt * dt);
                // Ressort vers la pose animée (relative au parent simulé).
                Vector3 target = p[i - 1].Position + (rest[i] - rest[i - 1]);
                p[i].Position = Vector3.Lerp(p[i].Position, target, 1f - Mathf.Pow(1f - s.stiffness, dt * 60f));
                // Longueur gardée.
                Vector3 d = p[i].Position - p[i - 1].Position;
                float len = d.magnitude;
                if (len > 1e-6f)
                    p[i].Position = p[i - 1].Position + d * (p[i].Length / len);
                // Obstacles.
                if (obstacles != null)
                    foreach (Obstacle o in obstacles)
                        p[i].Position = PushOut(p[i].Position, o, s.radius);
            }
        }

        public static Vector3 PushOut(Vector3 point, Obstacle o, float radius)
        {
            Vector3 ab = o.B - o.A;
            float t = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(point - o.A, ab) / ab.sqrMagnitude) : 0f;
            Vector3 closest = o.A + ab * t;
            Vector3 d = point - closest;
            float min = o.Radius + radius;
            float dist = d.magnitude;
            if (dist >= min)
                return point;
            Vector3 n = dist > 1e-6f ? d / dist : Vector3.up;
            return closest + n * min;
        }
    }
}
