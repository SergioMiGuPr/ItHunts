using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Objet que la télékinésie peut attraper et lancer (caisse, tonneau, lustre…). Corps physique dynamique ; sa
    /// masse est celle du Rigidbody. Un objet « accroché » (lustre) reste figé jusqu'à ce qu'on l'attrape. Il brille
    /// (émission) selon ce que le joueur peut en faire : la couleur est choisie par TelekinesisView.
    /// Registre des objets de la scène (pas un singleton de joueur : ce sont des objets du monde).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Grabbable : MonoBehaviour
    {
        [Tooltip("Figé (cinématique) jusqu'à ce qu'on l'attrape : objet suspendu ou accroché.")]
        [SerializeField] private bool startsAnchored;

        private static readonly List<Grabbable> AllList = new List<Grabbable>();
        private static readonly Dictionary<int, Grabbable> ById = new Dictionary<int, Grabbable>();
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private MaterialPropertyBlock _block;

        public static IReadOnlyList<Grabbable> All => AllList;
        public Rigidbody Body { get; private set; }
        public Collider[] Colliders => _colliders;
        public int Id => GetInstanceID();
        public float Mass => Body != null ? Body.mass : 0f;
        public bool Anchored { get; private set; }

        /// <summary>Temps (Time.time) du dernier lancer : lueur verte qui s'éteint.</summary>
        public float ThrownAt { get; private set; } = float.NegativeInfinity;

        public static Grabbable Find(int id) => ById.TryGetValue(id, out Grabbable g) ? g : null;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider>();
            _block = new MaterialPropertyBlock();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Anchored = startsAnchored;
            if (Anchored)
                Body.isKinematic = true;
        }

        private void OnEnable()
        {
            AllList.Add(this);
            ById[Id] = this;
        }

        private void OnDisable()
        {
            AllList.Remove(this);
            ById.Remove(Id);
        }

        /// <summary>Attrapé : libéré s'il était accroché, sans gravité, suit le point de maintien.</summary>
        public void OnGrabbed()
        {
            Anchored = false;
            Body.isKinematic = false;
            Body.useGravity = false;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>Relâché (posé, lâché ou lancé) : gravité normale.</summary>
        public void OnReleased(bool thrown)
        {
            Body.useGravity = true;
            if (thrown)
                ThrownAt = Time.time;
        }

        /// <summary>Couleur d'émission (HDR) de tous ses rendus.</summary>
        public void SetGlow(Color color)
        {
            _block.SetColor(EmissionId, color);
            foreach (Renderer r in _renderers)
                r.SetPropertyBlock(_block);
        }
    }
}
