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
        [Header("Modèle SM_Levier (DA)")]
        [Tooltip("Cristal et anneau de rune (M_Levier_Lueur) : lueur par _EmissionColor, orange prêt, vert activé.")]
        [SerializeField] private Renderer[] glow;
        [Tooltip("Manche (SM_Levier_Manche) : basculé de +35° à −35° autour de son axe X à l'activation.")]
        [SerializeField] private Transform handle;
        [ColorUsage(false, true)] [SerializeField] private Color readyGlow = new Color(3f, 1.1f, 0.15f);
        [ColorUsage(false, true)] [SerializeField] private Color litGlow = new Color(0.35f, 3f, 0.5f);
        [SerializeField] private float handleTravel = -70f;
        [Min(0.01f)] [SerializeField] private float handleTime = 0.25f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _block;
        private Quaternion _handleRest;
        private float _litAt = float.NegativeInfinity;

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
            _block ??= new MaterialPropertyBlock();
            if (handle != null)
                _handleRest = handle.localRotation;
            Glow(readyGlow);
        }

        private void Glow(Color c)
        {
            if (glow == null)
                return;
            _block ??= new MaterialPropertyBlock();
            foreach (Renderer r in glow)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(EmissionId, c);
                r.SetPropertyBlock(_block);
            }
        }

        private void Update()
        {
            if (handle == null)
                return;
            float u = Lit ? Mathf.Clamp01((Time.time - _litAt) / handleTime) : 0f;
            u = 1f - (1f - u) * (1f - u);                                // coup franc, fin amortie
            handle.localRotation = _handleRest * Quaternion.Euler(handleTravel * u, 0f, 0f);
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
            _litAt = Time.time;
            if (_renderers == null)
                Awake();
            Glow(litGlow);
            foreach (Renderer r in _renderers)
                if (litMaterial != null)
                    r.sharedMaterial = litMaterial;
        }

        public void ResetLever()
        {
            Lit = false;
            if (_renderers == null)
                Awake();
            Glow(readyGlow);
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].sharedMaterial = _off[i];
        }
    }
}
