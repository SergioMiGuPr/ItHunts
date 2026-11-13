using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Interaction d'un joueur (D-043) : l'objet utilisable visé (le plus près du centre du regard, à portée, dans la tolérance de
    /// visée) ; E / Y l'utilise ; F aussi sur une orbe de choix ou d'offre. Quand un objet est visé, Y (manette) interagit au lieu
    /// de recharger le barillet. Ajouté au joueur par PlayerInputReader.
    /// </summary>
    public sealed class PlayerInteractor : MonoBehaviour
    {
        private PlayerHealth _health;
        private PlayerLook _look;
        private EconomySettings _cfg;

        public IInteractable Focus { get; private set; }
        public bool HasFocus => Focus != null;
        /// <summary>F sur l'objet visé (une orbe) : il le prend au lieu de la télékinésie.</summary>
        public bool WantsGrab => Focus != null && Focus.AcceptsGrab;
        public int Uses { get; private set; }

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _look = GetComponent<PlayerLook>();
        }

        private void Update()
        {
            if (_cfg == null)
            {
                var souls = FindFirstObjectByType<SoulSystem>();
                _cfg = souls != null ? souls.Settings : null;
            }

            Focus = Find();
        }

        /// <summary>Le joueur appuie sur E / Y (ou F sur une orbe).</summary>
        public void Press()
        {
            Focus = Find();
            if (Focus == null || _health == null || _health.Dead)
                return;
            Focus.Interact(_health);
            Uses++;
        }

        public Vector3 Eye => transform.position + Vector3.up * 1.6f;

        public Vector3 Forward => _look != null ? Quaternion.Euler(_look.Pitch, _look.Yaw, 0f) * Vector3.forward : transform.forward;

        private IInteractable Find()
        {
            if (_health == null || _health.Dead)
                return null;
            float tolerance = _cfg != null ? _cfg.interactAngle : 12f;
            Vector3 eye = Eye, fwd = Forward;
            IInteractable best = null;
            float bestAngle = float.PositiveInfinity;
            foreach (IInteractable i in Interactables.All)
            {
                if (i == null || !i.Available(_health))
                    continue;
                Vector3 to = i.Point - eye;
                float d = to.magnitude;
                if (d > i.Range)
                    continue;
                // Tolérance plus large de près (taille apparente).
                float angle = Vector3.Angle(fwd, to);
                float allowed = tolerance + Mathf.Atan2(0.6f, Mathf.Max(0.5f, d)) * Mathf.Rad2Deg;
                if (angle <= allowed && angle < bestAngle)
                {
                    bestAngle = angle;
                    best = i;
                }
            }

            return best;
        }
    }
}
