using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Brûlure d'un ennemi touché par la lanterne de feu (D-065, correctifs-rendu point 8) : dps PV par seconde pendant duration
    /// secondes, par ticks de tick secondes (12 PV/s, 4 s, 0.25 s : 3 PV par tick, 48 PV). Un nouveau jet relance la durée sans
    /// cumuler. Le chiffre de dégâts s'affiche toutes les 0.5 s. Flammes sur l'ennemi tant qu'il brûle. Le traqueur n'est jamais
    /// touché (Damageable.ImmuneToOrbs). L'ennemi tué donne ses âmes comme pour toute attaque du joueur.
    /// Réseau (étape 11) : effet calculé par l'autorité.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Burning : MonoBehaviour
    {
        private Damageable _target;
        private float _dps, _until, _tick = 0.25f, _nextTick, _pending, _shownAt, _slow;
        private int _attacker;
        private ParticleSystem _fx;

        public float Dps => _dps;
        public float TickInterval => _tick;
        public float Remaining => Mathf.Max(0f, _until - Time.time);
        public bool Active => enabled && _target != null && !_target.Dead && Time.time < _until;
        /// <summary>Brûle (même mort depuis cet instant : les effets « meurt en feu » lisent l'état au moment du coup fatal).</summary>
        public bool Lit => enabled && _target != null && Time.time < _until;
        /// <summary>Brûlures cumulées (« Braises », R4) ; 1 d'ordinaire.</summary>
        public int Stacks { get; private set; }
        /// <summary>PV retirés par cette brûlure depuis le dernier jet (mesures).</summary>
        public float Dealt { get; private set; }
        /// <summary>Flammes visibles sur l'ennemi (mesures, tests).</summary>
        public bool EffectOn => _fx != null && _fx.isEmitting;

        /// <summary>Un ennemi prend feu ou reprend feu (cible, joueur, brûlures cumulées, première brûlure ?) : synergies (D-100).</summary>
        public static event System.Action<Damageable, int, int, bool> Ignited;

        /// <summary>
        /// Allume (ou relance) la brûlure de cet ennemi. Sans cumul par défaut (la durée repart, le dps est celui du nouveau jet) ;
        /// avec maxStacks &gt; 1 (« Braises »), une relance ajoute une brûlure jusqu'au plafond (le dps s'ajoute). slow : part de
        /// vitesse retirée tant qu'il brûle (« Allumette » Or).
        /// </summary>
        public static Burning Apply(Damageable target, float dps, float duration, int attackerId, float tick = 0.25f,
            Material effect = null, float now = -1f, int maxStacks = 1, float slow = 0f)
        {
            if (target == null || target.Dead || target.ImmuneToOrbs || dps <= 0f)
                return null;
            if (now < 0f)
                now = Time.time;
            var b = target.GetComponent<Burning>();
            if (b == null)
                b = target.gameObject.AddComponent<Burning>();
            bool running = b.enabled && b._target != null && now < b._until;
            b._target = target;
            b._tick = Mathf.Max(0.02f, tick);
            if (!running)
            {
                b._nextTick = now + b._tick;                             // premier tick un pas après le contact
                b._dps = dps;
                b.Stacks = 1;
            }
            else if (b.Stacks < Mathf.Max(1, maxStacks))
            {
                b._dps += dps;                                           // cumul (Braises)
                b.Stacks++;
            }
            else
                b._dps = Mathf.Max(dps, b._dps);                         // relance sans cumuler
            b._until = now + duration;
            b._attacker = attackerId;
            b._slow = Mathf.Max(b._slow, slow);
            b.Dealt = 0f;
            b.enabled = true;
            if (effect != null)
                b.ShowEffect(effect);
            Ignited?.Invoke(target, attackerId, b.Stacks, !running);
            return b;
        }

        /// <summary>Avance la brûlure jusqu'à now (dt : pas écoulé, gardé pour la compatibilité) ; renvoie les dégâts retirés.</summary>
        public float Step(float dt, float now)
        {
            float total = 0f;
            if (_slow > 0f && _target != null && !_target.Dead && now < _until)
                _target.Slow(_slow, _tick * 2f);
            while (_target != null && !_target.Dead && _nextTick <= now && _nextTick <= _until + 1e-4f)
            {
                _target.NoteAttacker(_attacker);
                float taken = _target.ApplyDamage(_dps * _tick, _nextTick, out bool killed);
                Dealt += taken;
                total += taken;
                _pending += taken;
                if (_pending > 0f && (killed || now - _shownAt >= 0.5f))
                {
                    DamageEvents.Raise(new DamageEvent { Target = _target, Damage = _pending, Point = _target.Center + Vector3.up * 0.3f, Killed = killed, Tier = HitTier.Petit, Time = now });
                    _pending = 0f;
                    _shownAt = now;
                }

                _nextTick += _tick;
                if (killed)
                    break;
            }

            if (_target == null || _target.Dead || now >= _until)
                Stop();
            return total;
        }

        private void Stop()
        {
            enabled = false;
            Stacks = 0;
            _slow = 0f;
            if (_fx != null)
                _fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_painted != null)
                _painted.Arrete();
        }

        private void FixedUpdate() => Step(Time.fixedDeltaTime, Time.time);

        private void OnDisable()
        {
            if (_fx != null && _fx.isEmitting)
                _fx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_painted != null)
                _painted.Arrete();
        }

        /// <summary>Flammes sur l'ennemi : petites bouffées additives qui montent, à la taille de ses bornes.</summary>
        private ItHunts.Art.VFX.VfxPeint _painted;

        private void ShowEffect(Material mat)
        {
            GameVfxSettings vfx = GameVfx.Settings;
            if (_fx == null && vfx != null && vfx.torchFire != null)
            {
                // Flamme peinte de la DA (VFX_Torche_Feu à 0.6) à la place des bouffées créées par code.
                if (_painted == null)
                {
                    GameObject go = GameVfx.Spawn(vfx.torchFire, transform.position + Vector3.up * 0.9f, Quaternion.identity, vfx.burningScale, transform, 0f);
                    _painted = go != null ? go.GetComponent<ItHunts.Art.VFX.VfxPeint>() : null;
                }

                if (_painted != null)
                {
                    _painted.gameObject.SetActive(true);
                    _painted.Joue();
                    return;
                }
            }

            if (_fx == null)
            {
                var go = new GameObject("Brûlure (flammes)");
                go.transform.SetParent(transform, false);
                _fx = go.AddComponent<ParticleSystem>();
                _fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = _fx.main;
                main.loop = true;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1.6f, 0.6f, 0.12f), new Color(1.2f, 0.25f, 0.05f));
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.gravityModifier = -0.4f;
                main.maxParticles = 60;
                var em = _fx.emission;
                em.rateOverTime = 28f;
                var shape = _fx.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                var col = _fx.colorOverLifetime;
                col.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.15f, 0.05f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                var sz = _fx.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
                var r = go.GetComponent<ParticleSystemRenderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            if (_target != null)
            {
                Bounds b = _target.MainCollider != null ? _target.MainCollider.bounds : new Bounds(_target.Center, Vector3.one);
                var shape = _fx.shape;
                shape.radius = Mathf.Max(0.2f, Mathf.Min(b.extents.x, b.extents.z));
                _fx.transform.position = b.center;
            }

            if (!_fx.isEmitting)
                _fx.Play(true);
        }
    }
}
