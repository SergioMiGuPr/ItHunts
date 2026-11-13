using ItHunts.Combat;
using ItHunts.Enemies;
using UnityEngine;

namespace ItHunts.Level
{
    public enum TrapKind
    {
        Herse,
        Pont,
        Lustre,
    }

    /// <summary>
    /// Piège du combat de boss (D-049). Une cible qui brille (Damageable d'un PV : un objet lancé, une balle du barillet ou une
    /// onde blessante la déclenche) et une zone marquée au sol (lueur orange) où il faut attirer le traqueur. Déclenché : la
    /// herse ou le lustre tombe, ou le tablier du pont s'effondre. Si le traqueur est dans la zone, le piège est gagné (il reste
    /// tombé) ; sinon il se réarme après rearmDelay secondes.
    /// </summary>
    public sealed class BossTrap : MonoBehaviour
    {
        [SerializeField] private TrapKind kind;
        [Tooltip("Cible à toucher (brille).")]
        [SerializeField] private Damageable trigger;
        [Tooltip("Zone où doit se trouver le traqueur (boîte centrée sur cet objet, axes du monde).")]
        [SerializeField] private Vector3 zoneSize = new Vector3(10f, 14f, 6f);
        [Tooltip("Pièce qui bouge : herse et lustre (tombent de dropHeight), tablier du pont (disparaît).")]
        [SerializeField] private Transform moving;
        [SerializeField] private float dropHeight = 12f;
        [SerializeField] private float dropTime = 0.35f;
        [Tooltip("Réarmement après un coup raté (s).")]
        [SerializeField] private float rearmDelay = 4f;

        private Vector3 _home;
        private float _firedAt = float.NegativeInfinity, _rearmAt = float.PositiveInfinity;
        private BossFight _fight;

        public TrapKind Kind => kind;
        public bool Sprung { get; private set; }
        public bool Down { get; private set; }
        public int Misses { get; private set; }
        public Damageable Trigger => trigger;
        public Bounds Zone => new Bounds(transform.position, zoneSize);

        private void Awake()
        {
            if (moving != null)
                _home = moving.position;
        }

        private void OnEnable() => Damageable.Died += OnDied;
        private void OnDisable() => Damageable.Died -= OnDied;

        public void Bind(BossFight fight) => _fight = fight;

        private void OnDied(Damageable d)
        {
            if (d == trigger && !Down && !Sprung)
                Fire();
        }

        /// <summary>Déclenche le piège (cible touchée) ; vrai si le traqueur était dans la zone.</summary>
        public bool Fire()
        {
            Down = true;
            _firedAt = Time.time;
            bool hit = _fight != null && _fight.Active && _fight.StalkerIn(Zone);
            if (kind == TrapKind.Pont && moving != null)
                SetDeck(false);
            if (hit)
            {
                Sprung = true;
                _fight.TrapSprung(this);
            }
            else
                Misses++;
            _rearmAt = Time.time + rearmDelay;                          // gagné : la pièce revient (passage rouvert), la cible reste éteinte

            return hit;
        }

        private void Update()
        {
            if (moving == null)
                return;
            if (kind != TrapKind.Pont)
            {
                float t = Down ? Mathf.Clamp01((Time.time - _firedAt) / Mathf.Max(0.01f, dropTime)) : 0f;
                moving.position = _home + Vector3.down * (dropHeight * t * t);
            }

            if (Down && Time.time >= _rearmAt)
                Rearm();
        }

        private void SetDeck(bool on)
        {
            foreach (Collider c in moving.GetComponentsInChildren<Collider>(true))
                c.enabled = on;
            foreach (Renderer r in moving.GetComponentsInChildren<Renderer>(true))
                r.enabled = on;
        }

        private void Rearm()
        {
            Down = false;
            _rearmAt = float.PositiveInfinity;
            if (moving != null)
            {
                moving.position = _home;
                if (kind == TrapKind.Pont)
                    SetDeck(true);
            }

            if (trigger != null && !Sprung)
                trigger.Revive();
        }

        /// <summary>Remise à zéro de la zone : armé, rien de tombé.</summary>
        public void ResetTrap()
        {
            Sprung = false;
            Misses = 0;
            _firedAt = float.NegativeInfinity;
            Rearm();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, zoneSize);
        }
    }
}
