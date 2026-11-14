using ItHunts.Combat;
using ItHunts.Orbs;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Levier lumineux d'un piège du combat de boss (étape 8, après playtest) : on l'active en le frappant avec un objet lancé
    /// (au-dessus de minImpactSpeed) ou avec l'onde de répulsion. Orange avant, vert une fois activé ; il reste allumé. Un piège
    /// s'arme quand tous ses leviers sont allumés.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BossLever : MonoBehaviour, IWaveTarget
    {
        [SerializeField] private Material litMaterial;
        [Min(0f)] [SerializeField] private float minImpactSpeed = 5f;

        private Renderer[] _renderers;
        private Material[] _off;
        private Collider _collider;

        public bool Lit { get; private set; }
        public int LitBy { get; private set; }
        public Collider WaveCollider => _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _off = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _off[i] = _renderers[i].sharedMaterial;
        }

        private void OnEnable() => RepulsionWave.Targets.Add(this);
        private void OnDisable() => RepulsionWave.Targets.Remove(this);

        public void OnWave(int playerId) => Hit(playerId);

        private void OnCollisionEnter(Collision c)
        {
            var g = c.rigidbody != null ? c.rigidbody.GetComponent<Grabbable>() : null;
            if (g != null && c.relativeVelocity.magnitude >= minImpactSpeed)
                Hit(g.LastOwnerId);
        }

        /// <summary>Activé (objet lancé, onde de répulsion, ou mesures).</summary>
        public void Hit(int playerId)
        {
            if (Lit)
                return;
            Lit = true;
            LitBy = playerId;
            if (_renderers == null)
                Awake();
            foreach (Renderer r in _renderers)
                if (litMaterial != null)
                    r.sharedMaterial = litMaterial;
        }

        public void ResetLever()
        {
            Lit = false;
            if (_renderers == null)
                Awake();
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].sharedMaterial = _off[i];
        }
    }
}
