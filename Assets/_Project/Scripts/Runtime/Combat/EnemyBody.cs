using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Corps d'un ennemi (factice pour l'instant) : recul quand l'onde de répulsion le pousse (corps cinématique déplacé à
    /// pas fixe, avec gravité et freinage au sol), étourdissement s'il percute un obstacle solide au-dessus de
    /// stunImpactSpeed, puis retour à son poste. Pendant le recul et l'étourdissement, il ne marche pas (TrainingDummyMover
    /// attend). L'état « étourdi » prépare la prise d'ennemis par la télékinésie (goule, 1.5 s, étape des ennemis) :
    /// CanBeGrabbed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyBody : MonoBehaviour
    {
        [SerializeField] private TrainingDummySettings settings;
        [Tooltip("Revient seul à son poste après un recul (non : c'est TrainingDummyMover qui le ramène sur son trajet).")]
        [SerializeField] private bool returnHome = true;

        private const float Skin = 0.03f;
        private const float RestSpeed = 0.2f;

        private Rigidbody _body;
        private Vector3 _home;
        private Vector3 _velocity;
        private bool _knocked;
        private float _restSince = float.NegativeInfinity;
        private float _stunnedUntil = float.NegativeInfinity;

        public Vector3 Velocity => _velocity;
        public bool Knocked => _knocked;
        public bool Stunned => Time.time < _stunnedUntil;
        public float StunnedUntil => _stunnedUntil;
        public float Mass => settings != null ? settings.mass : 80f;
        public TrainingDummySettings Settings => settings;

        /// <summary>Repoussé ou étourdi : il ne marche pas.</summary>
        public bool Busy => _knocked || Stunned;

        /// <summary>Un ennemi étourdi pourra être attrapé par la télékinésie (étape des ennemis).</summary>
        public bool CanBeGrabbed => Stunned;

        /// <summary>Dernier étourdissement : vitesse d'impact contre l'obstacle (debug, mesures).</summary>
        public float LastStunImpactSpeed { get; private set; }

        private void Awake() => Init();

        /// <summary>Prépare le corps (Awake ; aussi par les tests).</summary>
        public void Init()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _home = transform.position;
        }

        /// <summary>Ajoute une vitesse de recul (onde de répulsion).</summary>
        public void Push(Vector3 velocity)
        {
            _velocity += velocity;
            _knocked = _velocity.sqrMagnitude > RestSpeed * RestSpeed;
        }

        /// <summary>Étourdi pendant la durée réglée (repère et son par AreaEvents.Stunned).</summary>
        public void Stun(float impactSpeed)
        {
            float duration = settings != null ? settings.stunDuration : 1.5f;
            _stunnedUntil = Time.time + duration;
            LastStunImpactSpeed = impactSpeed;
            AreaEvents.RaiseStunned(new StunEvent { Target = transform, Point = transform.position + Vector3.up * 1f, Duration = duration });
        }

        /// <summary>Réapparition : à son poste, immobile, pas étourdi.</summary>
        public void ResetToHome()
        {
            _velocity = Vector3.zero;
            _knocked = false;
            _stunnedUntil = float.NegativeInfinity;
            transform.position = _home;
            _body.position = _home;
        }

        private void FixedUpdate()
        {
            if (_body == null)
                Init();
            Step(Time.fixedDeltaTime);
        }

        /// <summary>Un pas de recul (public pour les tests en scène isolée).</summary>
        public void Step(float dt)
        {
            if (!_knocked)
            {
                ReturnHome(dt);
                return;
            }

            float gravity = settings != null ? settings.gravity : 20f;
            bool grounded = Grounded(out float groundY);
            if (!grounded)
                _velocity.y -= gravity * dt;
            else if (_velocity.y <= 0f)
            {
                _velocity.y = 0f;
                Vector3 h = new Vector3(_velocity.x, 0f, _velocity.z);
                float speed = h.magnitude;
                float friction = (settings != null ? settings.groundFriction : 15f) * dt;
                h = speed > friction ? h * ((speed - friction) / speed) : Vector3.zero;
                _velocity = new Vector3(h.x, 0f, h.z);
            }

            Vector3 move = _velocity * dt;
            float distance = move.magnitude;
            Vector3 position = _body.position;
            if (distance > 1e-5f)
            {
                Vector3 dir = move / distance;
                if (_body.SweepTest(dir, out RaycastHit hit, distance + Skin, QueryTriggerInteraction.Ignore))
                {
                    position += dir * Mathf.Max(0f, hit.distance - Skin);
                    Vector3 n = hit.normal;
                    float into = -Vector3.Dot(_velocity, n);
                    bool wall = n.y < 0.5f;
                    bool solid = hit.collider.GetComponent<CharacterController>() == null;
                    if (wall && solid && settings != null && AreaEffects.Stuns(into, settings))
                        Stun(into);
                    if (into > 0f)
                        _velocity += n * into;                         // glisse le long de l'obstacle, sans rebond
                }
                else
                {
                    position += move;
                }
            }

            if (grounded && position.y < groundY)
                position.y = groundY;
            _body.MovePosition(position);
            transform.position = position;

            if (grounded && new Vector3(_velocity.x, 0f, _velocity.z).magnitude < RestSpeed && _velocity.y <= 0f)
            {
                _velocity = Vector3.zero;
                _knocked = false;
                _restSince = Time.time;
            }
        }

        private void ReturnHome(float dt)
        {
            if (!returnHome || Stunned || settings == null || Time.time - _restSince < settings.returnHomeDelay)
                return;
            Vector3 to = _home - _body.position;
            to.y = 0f;
            if (to.magnitude < 0.05f)
                return;
            Vector3 step = Vector3.ClampMagnitude(to, settings.returnHomeSpeed * dt);
            Vector3 target = _body.position + step;
            _body.MovePosition(target);
            transform.position = target;
        }

        /// <summary>Sol sous les pieds (rayon court depuis le bas du collider) et hauteur du centre posé dessus.</summary>
        private bool Grounded(out float centerY)
        {
            centerY = _body.position.y;
            Collider col = GetComponent<Collider>();
            if (col == null)
                return false;
            Bounds b = col.bounds;
            float half = b.extents.y;
            var origin = new Vector3(b.center.x, b.min.y + 0.1f, b.center.z);
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 0.2f, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                if (h.collider == col || h.collider.attachedRigidbody == _body)
                    continue;
                centerY = _body.position.y + (h.point.y - b.min.y);
                return true;
            }

            return false;
        }
    }
}
