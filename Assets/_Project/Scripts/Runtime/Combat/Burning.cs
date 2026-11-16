using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Brûlure d'un ennemi (amélioration « Braises » de la lanterne de feu, D-065) : dps PV par seconde pendant duration secondes,
    /// au pas fixe. Un nouveau jet relance la durée (sans cumuler) ; le chiffre de dégâts s'affiche toutes les 0.5 s. Le traqueur
    /// n'est jamais touché (Damageable.ImmuneToOrbs). L'ennemi tué donne ses âmes comme pour toute attaque du joueur.
    /// Réseau (étape 11) : effet calculé par l'autorité.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Burning : MonoBehaviour
    {
        private Damageable _target;
        private float _dps, _until, _pending, _shownAt;
        private int _attacker;

        public float Dps => _dps;
        public float Remaining => Mathf.Max(0f, _until - Time.time);
        /// <summary>PV retirés par cette brûlure depuis le dernier jet (mesures).</summary>
        public float Dealt { get; private set; }

        /// <summary>Allume (ou relance) la brûlure de cet ennemi.</summary>
        public static Burning Apply(Damageable target, float dps, float duration, int attackerId)
        {
            if (target == null || target.Dead || target.ImmuneToOrbs || dps <= 0f)
                return null;
            var b = target.GetComponent<Burning>();
            if (b == null)
                b = target.gameObject.AddComponent<Burning>();
            b._target = target;
            b._dps = Mathf.Max(b.Remaining > 0f ? b._dps : 0f, dps);
            b._until = Time.time + duration;
            b._attacker = attackerId;
            b.Dealt = 0f;
            b.enabled = true;
            return b;
        }

        /// <summary>Un pas de brûlure (dt secondes) ; renvoie les dégâts retirés. Public pour les tests.</summary>
        public float Tick(float dt, float now)
        {
            if (_target == null || _target.Dead || now >= _until)
            {
                enabled = false;
                return 0f;
            }

            _target.NoteAttacker(_attacker);
            float taken = _target.ApplyDamage(_dps * Mathf.Min(dt, _until - now), now, out bool killed);
            Dealt += taken;
            _pending += taken;
            if (_pending > 0f && (killed || now - _shownAt >= 0.5f))
            {
                DamageEvents.Raise(new DamageEvent { Target = _target, Damage = _pending, Point = _target.Center + Vector3.up * 0.3f, Killed = killed, Tier = HitTier.Petit, Time = now });
                _pending = 0f;
                _shownAt = now;
            }

            if (killed)
                enabled = false;
            return taken;
        }

        private void FixedUpdate() => Tick(Time.fixedDeltaTime, Time.time);
    }
}
