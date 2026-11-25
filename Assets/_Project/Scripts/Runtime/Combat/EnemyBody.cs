using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Corps d'un ennemi (factice pour l'instant) : recul quand l'onde de répulsion le pousse (corps cinématique déplacé à
    /// pas fixe, avec gravité et freinage au sol), étourdissement s'il percute un obstacle solide au-dessus de
    /// stunImpactSpeed, puis retour à son poste. Pendant le recul et l'étourdissement, il ne marche pas (TrainingDummyMover
    /// attend). Étourdi, il peut être attrapé par la télékinésie (D-030) : tenu, c'est un objet physique de sa masse ; à la
    /// libération (lancer, ou seul au bout du temps réglé), il vole jusqu'à se poser, puis reprend son corps
    /// cinématique ; l'étourdissement s'arrête et l'immunité commence.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class EnemyBody : MonoBehaviour
    {
        [SerializeField] private EnemySettings settings;
        [Tooltip("Revient seul à son poste après un recul (non : c'est TrainingDummyMover qui le ramène sur son trajet).")]
        [SerializeField] private bool returnHome = true;
        [Tooltip("Dégâts contre un mur même s'il est déjà étourdi (volant qui tombe après une répulsion, D-040), une fois par recul.")]
        [SerializeField] private bool wallDamageWhileStunned;
        private bool _wallHitThisKnock;
        private float _pushedAt = float.NegativeInfinity;

        /// <summary>Morts par un choc contre un mur ou le sol juste après une répulsion (volant, D-040 ; debug, mesures).</summary>
        public int PushedContactKills { get; private set; }

        /// <summary>Dans la fenêtre qui suit une répulsion où tout choc le tue (volant) ?</summary>
        public bool PushedContactLethal => settings != null && settings.PushedContactKillTime > 0f
                                           && Time.time - _pushedAt <= settings.PushedContactKillTime;

        private const float Skin = 0.03f;
        private const float RestSpeed = 0.2f;

        private Rigidbody _body;
        private Vector3 _home;
        private Vector3 _velocity;
        private bool _knocked;
        private float _restSince = float.NegativeInfinity;
        private float _stunnedUntil = float.NegativeInfinity;
        private float _immuneUntil = float.NegativeInfinity;
        private ItHunts.Orbs.Grabbable _grabbable;
        private Damageable _damageable;
        private bool _wasHeld;
        private bool _inFlight;
        private float _flightStart;

        public Vector3 Velocity => _velocity;
        public bool Knocked => _knocked;
        public bool Stunned => Time.time < _stunnedUntil;

        /// <summary>Immunisé : un étourdissement vient de finir (stunImmunity), pas de nouvel étourdissement.</summary>
        public bool StunImmune => !Stunned && Time.time < _immuneUntil;
        public float StunnedUntil => _stunnedUntil;
        public float Mass => settings != null ? settings.mass : 80f;
        public Rigidbody Rigidbody => _body;

        /// <summary>Tenu par la télékinésie.</summary>
        public bool Held => _grabbable != null && _grabbable.Held;

        /// <summary>Lâché ou lancé par la télékinésie, il vole (corps dynamique) jusqu'à se poser.</summary>
        public bool InFlight => _inFlight;
        public EnemySettings Settings => settings;

        /// <summary>Repoussé, étourdi, tenu ou en vol : il ne marche pas.</summary>
        public bool Busy => _knocked || Stunned || Held || _inFlight;

        /// <summary>Attrapable par la télékinésie : étourdi, pas déjà tenu, pas le traqueur (D-030).</summary>
        public bool CanBeGrabbed => Stunned && !Held && (_damageable == null || !_damageable.ImmuneToOrbs);

        /// <summary>Derniers dégâts contre un mur (D-031, debug et mesures).</summary>
        public float LastWallDamage { get; private set; }

        /// <summary>
        /// Améliorations du joueur qui a repoussé cet ennemi (nuit 4, D-100), branchées par Economy.PlayerUpgrades : multiplicateur
        /// des dégâts d'impact contre un mur (« Rebond », G16) et secondes ajoutées à ses étourdissements (« Briseur d'os » Or).
        /// Argument : identifiant du dernier joueur à l'avoir attaqué ; null ou 0 : rien.
        /// </summary>
        public static System.Func<int, float> WallDamageMultiplierFor;
        public static System.Func<int, float> StunBonusFor;

        /// <summary>Prolonge l'étourdissement en cours (et l'immunité qui le suit) ; rien s'il n'est pas étourdi.</summary>
        public void ExtendStun(float seconds)
        {
            if (seconds <= 0f || !Stunned || Held)
                return;
            _stunnedUntil += seconds;
            _immuneUntil += seconds;
        }

        /// <summary>
        /// Dégâts contre un mur d'un ennemi repoussé (D-031) : 5 PV par m/s au-delà de 6 m/s, au plus 40 ; appelé avec
        /// l'étourdissement réussi (donc une fois par impact, jamais pendant l'immunité ; traqueur exclu). Renvoie les dégâts.
        /// « Rebond » (G16) : × le multiplicateur du joueur qui l'a repoussé.
        /// </summary>
        public float ApplyWallDamage(float impactSpeed, Vector3 point)
        {
            if (settings == null || _damageable == null || _damageable.ImmuneToOrbs)
                return 0f;
            float damage = AreaEffects.WallDamage(impactSpeed, settings);
            if (WallDamageMultiplierFor != null && _damageable.LastAttackerId != 0 && Time.time - _damageable.LastAttackTime < 3f)
                damage *= Mathf.Max(0f, WallDamageMultiplierFor(_damageable.LastAttackerId));
            float before = _damageable.Health.Current;
            ImpactDamager.ApplyDirect(_damageable, -GetInstanceID() - 1, damage, impactSpeed, Mass, point, Time.time);
            return before - _damageable.Health.Current;
        }

        /// <summary>
        /// Choc juste après une répulsion (volant, D-040) : il meurt, quelle que soit la vitesse ; les dégâts passent par le
        /// chemin habituel (chiffres, son). Vrai s'il est mort.
        /// </summary>
        private bool KillOnContact(float speed, Vector3 point)
        {
            if (_damageable == null || _damageable.Dead || _damageable.ImmuneToOrbs)
                return false;
            ImpactDamager.ApplyDirect(_damageable, -GetInstanceID() - 2, _damageable.Health.Current, speed, Mass, point, Time.time);
            if (!_damageable.Dead)
                _damageable.ApplyDamage(_damageable.Health.Current, Time.time, out _);
            if (!_damageable.Dead)
                return false;
            PushedContactKills++;
            LastWallDamage = 0f;
            _knocked = false;
            _velocity = Vector3.zero;
            return true;
        }

        /// <summary>Dernier étourdissement : vitesse d'impact contre l'obstacle (debug, mesures).</summary>
        public float LastStunImpactSpeed { get; private set; }

        private void Awake() => Init();

        private void OnEnable() => EnemyPassThrough.Register(this);

        private void OnDisable() => EnemyPassThrough.Unregister(this);

        /// <summary>Prépare le corps (Awake ; aussi par les tests).</summary>
        public void Init()
        {
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            if (settings != null)
                _body.mass = settings.mass;
            _home = transform.position;
            _grabbable = GetComponent<ItHunts.Orbs.Grabbable>();
            _damageable = GetComponent<Damageable>();
        }

        /// <summary>Nouvelle place de départ (vagues, D-041 : un ennemi réutilisé apparaît ailleurs).</summary>
        public void SetHome(Vector3 home)
        {
            if (_body == null)
                Init();
            _home = home;
        }

        /// <summary>Ajoute une vitesse de recul (onde de répulsion).</summary>
        public void Push(Vector3 velocity)
        {
            if (Held)
                return;
            if (_inFlight)
            {
                _body.linearVelocity += velocity;
                return;
            }

            _velocity += velocity;
            bool was = _knocked;
            _knocked = _velocity.sqrMagnitude > RestSpeed * RestSpeed;
            if (_knocked && !was)
                _knockedSince = Time.time;
            _wallHitThisKnock = false;
            _pushedAt = Time.time;
        }

        /// <summary>
        /// Étourdi pendant la durée réglée (repère et son par AreaEvents.Stunned) ; refusé s'il l'est déjà ou s'il est
        /// encore immunisé (stunImmunity après la fin du précédent). Vrai s'il est étourdi par cet appel.
        /// </summary>
        public bool Stun(float impactSpeed)
        {
            if (settings == null || Held || (_damageable != null && _damageable.ImmuneToOrbs)
                || !AreaEffects.CanStun(Time.time, _stunnedUntil, _immuneUntil))
                return false;
            float duration = settings.stunDuration;
            AreaEffects.StunTimes(Time.time, settings, out _stunnedUntil, out _immuneUntil);
            // Briseur d'os Or (D-100) : les étourdissements infligés par ce joueur durent plus longtemps.
            float bonus = StunBonusFor != null && _damageable != null && _damageable.LastAttackerId != 0 && Time.time - _damageable.LastAttackTime < 3f
                ? Mathf.Max(0f, StunBonusFor(_damageable.LastAttackerId)) : 0f;
            if (bonus > 0f)
            {
                duration += bonus;
                _stunnedUntil += bonus;
                _immuneUntil += bonus;
            }

            LastStunImpactSpeed = impactSpeed;
            AreaEvents.RaiseStunned(new StunEvent { Target = transform, Point = transform.position + Vector3.up * 1f, Duration = duration });
            return true;
        }

        /// <summary>
        /// Étourdi pendant <paramref name="duration"/> s par une parade (D-075), même s'il sortait d'un étourdissement
        /// (l'immunité ne compte pas) ; jamais s'il est tenu ou intuable. Repère et son par AreaEvents.Stunned.
        /// </summary>
        public bool StunFor(float duration)
        {
            if (settings == null || Held || (_damageable != null && _damageable.ImmuneToOrbs) || duration <= 0f)
                return false;
            float now = Time.time;
            _stunnedUntil = Mathf.Max(_stunnedUntil, now + duration);
            _immuneUntil = _stunnedUntil + settings.stunImmunity;
            AreaEvents.RaiseStunned(new StunEvent { Target = transform, Point = transform.position + Vector3.up * 1f, Duration = duration });
            return true;
        }

        /// <summary>Réapparition : à son poste, immobile, pas étourdi.</summary>
        public void ResetToHome()
        {
            if (_body == null)
                Init();                                                  // réserve des vagues : jamais activé (pas d'Awake)
            _velocity = Vector3.zero;
            _knocked = false;
            _pushedAt = float.NegativeInfinity;
            _stunnedUntil = float.NegativeInfinity;
            _immuneUntil = float.NegativeInfinity;
            _inFlight = false;
            _wasHeld = false;
            _body.isKinematic = true;
            _body.constraints = RigidbodyConstraints.None;
            transform.SetPositionAndRotation(_home, Quaternion.identity);
            _body.position = _home;
            _body.rotation = Quaternion.identity;
        }

        /// <summary>Attrapé : il reste « pris » (étourdi) tant qu'il est tenu ; plus de recul.</summary>
        private void OnCaptured()
        {
            _stunnedUntil = float.PositiveInfinity;
            _knocked = false;
            _velocity = Vector3.zero;
            _body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        /// <summary>Lâché ou lancé : fin de l'étourdissement, début de l'immunité, vol jusqu'à se poser.</summary>
        private void OnReleasedFromHold()
        {
            float now = Time.time;
            _stunnedUntil = now;
            _immuneUntil = now + (settings != null ? settings.stunImmunity : 2f);
            _inFlight = !_body.isKinematic;
            _flightStart = now;
        }

        private void FlightStep()
        {
            bool slow = _body.linearVelocity.magnitude < 0.3f && Time.time - _flightStart > 0.2f;
            if (!slow && Time.time - _flightStart < 4f)
                return;
            _inFlight = false;
            if (!_body.isKinematic)
                _body.linearVelocity = Vector3.zero;
            _body.isKinematic = true;
            _body.constraints = RigidbodyConstraints.None;
            transform.rotation = Quaternion.identity;
            _body.rotation = Quaternion.identity;
            _restSince = Time.time;
        }

        private void FixedUpdate()
        {
            if (_body == null)
                Init();
            if (Held)
            {
                if (!_wasHeld)
                    OnCaptured();
                _wasHeld = true;
                return;
            }

            if (_wasHeld)
            {
                _wasHeld = false;
                OnReleasedFromHold();
            }

            if (_inFlight)
            {
                FlightStep();
                return;
            }

            Step(Time.fixedDeltaTime);
        }

        /// <summary>Volant relâché (pas lancé) : il reprend aussitôt son vol (corps cinématique), sans retomber.</summary>
        public void EndFlight()
        {
            _inFlight = false;
            _knocked = false;
            _velocity = Vector3.zero;
            if (!_body.isKinematic)
                _body.linearVelocity = Vector3.zero;                      // (un corps cinématique n'a pas de vitesse : avertissement Unity)
            _body.isKinematic = true;
            _body.constraints = RigidbodyConstraints.None;
        }

        /// <summary>Recul qui n'en finit pas (coincé dans un décor, tombé hors du monde) : relâché au bout de ce temps (s).</summary>
        public const float KnockTimeout = 4f;
        private float _knockedSince = float.NegativeInfinity;
        /// <summary>Reculs relâchés par le délai de sécurité (mesures : « goules figées après une répulsion », nuit 4 point 1).</summary>
        public static int KnockTimeouts { get; private set; }

        /// <summary>Un pas de recul (public pour les tests en scène isolée).</summary>
        public void Step(float dt)
        {
            if (!_knocked)
            {
                ReturnHome(dt);
                return;
            }

            // Sécurité (nuit 4, point 1) : un corps repoussé qui ne se pose jamais (coincé entre deux volumes, tombé dans un trou du
            // décor, posé sur une pente où le rayon de sol ne touche rien) restait « Busy » pour toujours : goule figée. Au-delà du délai,
            // il est considéré posé ; la goule reprend (et son déblocage la ramène sur le NavMesh).
            if (Time.time - _knockedSince > KnockTimeout)
            {
                _velocity = Vector3.zero;
                _knocked = false;
                _restSince = Time.time;
                KnockTimeouts++;
                return;
            }

            float gravity = settings != null ? settings.gravity : 20f;
            bool grounded = Grounded(out float groundY);
            if (grounded && _velocity.y < 0f && PushedContactLethal && KillOnContact(-_velocity.y, _body.position))
                return;                                                  // volant repoussé qui touche le sol
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
                    if (solid && !hit.collider.isTrigger && hit.collider.attachedRigidbody == null && PushedContactLethal && KillOnContact(Mathf.Max(0f, -Vector3.Dot(_velocity, n)), hit.point))
                        return;                                          // volant repoussé : mur, sol ou plafond, quelle que soit la vitesse
                    if (wall && solid && settings != null && AreaEffects.Stuns(into, settings))
                    {
                        if (Stun(into))
                            LastWallDamage = ApplyWallDamage(into, hit.point);   // une fois par impact, pas pendant l'immunité
                        else if (wallDamageWhileStunned && Stunned && !_wallHitThisKnock)
                        {
                            _wallHitThisKnock = true;
                            LastWallDamage = ApplyWallDamage(into, hit.point);
                        }
                    }
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
