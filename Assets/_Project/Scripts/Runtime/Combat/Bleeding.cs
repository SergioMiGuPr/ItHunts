using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Saignement d'un ennemi (épée en os, « Os brisé », K5, D-100) : dps PV par seconde pendant duration s, par ticks de 0.5 s.
    /// Un nouveau coup relance la durée ; les saignements se cumulent jusqu'à maxStacks (le dps s'ajoute). État « saignant » lu
    /// par Damageable.Bleeding (synergies : Brise-garde). Le traqueur ne saigne jamais (ImmuneToOrbs). Réseau : effet de l'autorité.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Bleeding : MonoBehaviour
    {
        private const float Tick = 0.5f;
        private Damageable _target;
        private float _dps, _unitDps, _until, _nextTick;
        private int _attacker;

        public int Stacks { get; private set; }
        public float Dps => _dps;
        /// <summary>Saigne (même mort depuis cet instant : les abonnés à la mort lisent l'état au moment du coup fatal).</summary>
        public bool Lit => enabled && Time.time < _until;
        public float Dealt { get; private set; }

        /// <summary>Allume (ou relance, cumule jusqu'à maxStacks) le saignement de cet ennemi.</summary>
        public static Bleeding Apply(Damageable target, float dps, float duration, int attackerId, int maxStacks = 1, float now = -1f)
        {
            if (target == null || target.Dead || target.ImmuneToOrbs || dps <= 0f || duration <= 0f)
                return null;
            if (now < 0f)
                now = Time.time;
            var b = target.GetComponent<Bleeding>();
            if (b == null)
                b = target.gameObject.AddComponent<Bleeding>();
            bool running = b.enabled && b._target != null && now < b._until;
            b._target = target;
            b._attacker = attackerId;
            if (!running)
            {
                b.Stacks = 1;
                b._unitDps = dps;
                b._dps = dps;
                b._nextTick = now + Tick;
                b.Dealt = 0f;
            }
            else if (b.Stacks < Mathf.Max(1, maxStacks))
            {
                b.Stacks++;
                b._dps += dps;
            }
            else
                b._dps = Mathf.Max(b._dps, dps * b.Stacks);
            b._until = now + duration;
            b.enabled = true;
            return b;
        }

        /// <summary>Avance jusqu'à now (public pour les tests) ; renvoie les dégâts retirés.</summary>
        public float Step(float now)
        {
            float total = 0f;
            while (_target != null && !_target.Dead && _nextTick <= now && _nextTick <= _until + 1e-4f)
            {
                _target.NoteAttacker(_attacker);
                float taken = _target.ApplyDamage(_dps * Tick, _nextTick, out bool killed);
                Dealt += taken;
                total += taken;
                if (taken > 0f)
                    DamageEvents.Raise(new DamageEvent
                    {
                        Target = _target, Damage = taken, Point = _target.Center + Vector3.up * 0.2f, Killed = killed, Tier = HitTier.Petit, Time = now,
                        AttackerId = _attacker,
                    });
                _nextTick += Tick;
                if (killed)
                    break;
            }

            if (_target == null || _target.Dead || now >= _until)
            {
                enabled = false;
                Stacks = 0;
            }

            return total;
        }

        private void FixedUpdate() => Step(Time.time);
    }
}
