using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Visée de la télékinésie avec la physique Unity :
    /// 1. rayon exact : si la première surface touchée (hors joueur et hors objet tenu) porte un Grabbable, c'est lui
    ///    (même trop lourd : il brille alors en rouge terne) ;
    /// 2. sinon, aide à la visée (GrabAssist) : parmi les objets attrapables (pas trop lourds) vus dans le cône autour
    ///    du viseur, celui au plus petit angle, à condition que rien ne le cache (rayon des yeux vers son point le plus
    ///    proche du viseur : le premier collider touché doit être le sien).
    /// </summary>
    public sealed class PhysicsGrabProbe : IGrabProbe
    {
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Transform _owner;

        public LayerMask Mask { get; set; }

        /// <summary>Objet tenu, ignoré par le rayon (0 = aucun).</summary>
        public int IgnoreId { get; set; }

        /// <summary>Réglages (cône de l'aide, masse maximale) ; null : rayon exact seulement.</summary>
        public TelekinesisOrbSettings Settings { get; set; }

        /// <summary>Scène physique interrogée (par défaut, celle du jeu ; tests : une scène isolée).</summary>
        public PhysicsScene Scene { get; set; } = Physics.defaultPhysicsScene;

        public PhysicsGrabProbe(LayerMask mask, Transform owner)
        {
            Mask = mask;
            _owner = owner;
        }

        public bool FindGrabbable(Vector3 origin, Vector3 direction, float range, out GrabCandidate candidate)
        {
            candidate = default;
            direction = direction.normalized;
            if (Nearest(origin, direction, range, out RaycastHit hit))
            {
                Grabbable g = hit.collider.attachedRigidbody != null ? hit.collider.attachedRigidbody.GetComponent<Grabbable>() : null;
                if (g != null && g.isActiveAndEnabled && g.Id != IgnoreId)
                {
                    candidate = new GrabCandidate { Id = g.Id, Mass = g.Mass, Distance = hit.distance, Point = hit.point, Angle = 0f };
                    return true;
                }
            }

            return Settings != null && Settings.aimAssist && FindInCone(origin, direction, range, out candidate);
        }

        private bool FindInCone(Vector3 origin, Vector3 direction, float range, out GrabCandidate best)
        {
            best = default;
            bool found = false;
            float bestAngle = float.PositiveInfinity;
            float maxCone = Mathf.Max(Settings.assistNearAngle, Settings.assistFarAngle);
            foreach (Grabbable g in Grabbable.All)
            {
                if (g == null || !g.isActiveAndEnabled || g.Id == IgnoreId || g.Mass > Settings.maxMass || g.Colliders == null)
                    continue;
                foreach (Collider col in g.Colliders)
                {
                    if (col == null || !col.enabled || col.isTrigger)
                        continue;
                    Vector3 point = ClosestToRay(col, origin, direction);
                    float distance = Vector3.Distance(origin, point);
                    if (distance > range)
                        continue;
                    float angle = GrabAssist.AngleTo(origin, direction, point);
                    if (angle > maxCone || angle >= bestAngle || !GrabAssist.InCone(Settings, angle, distance))
                        continue;
                    if (!Visible(g, col, origin, point))
                        continue;
                    bestAngle = angle;
                    best = new GrabCandidate { Id = g.Id, Mass = g.Mass, Distance = distance, Point = point, Angle = angle };
                    found = true;
                }
            }

            return found;
        }

        /// <summary>Point du collider le plus proche du rayon de visée (deux passes de projection).</summary>
        private static Vector3 ClosestToRay(Collider col, Vector3 origin, Vector3 direction)
        {
            Vector3 point = col.bounds.center;
            for (int i = 0; i < 2; i++)
            {
                float t = Mathf.Max(0f, Vector3.Dot(point - origin, direction));
                point = col.ClosestPoint(origin + direction * t);
            }

            return point;
        }

        /// <summary>Rien ne cache l'objet : un rayon vers un point un peu à l'intérieur de son collider le touche en premier.</summary>
        private bool Visible(Grabbable g, Collider col, Vector3 origin, Vector3 point)
        {
            Vector3 target = Vector3.Lerp(point, col.bounds.center, 0.1f);
            Vector3 to = target - origin;
            float length = to.magnitude;
            if (length < 1e-3f)
                return true;
            if (!Nearest(origin, to / length, length + 0.5f, out RaycastHit hit))
                return false;
            return hit.collider.attachedRigidbody == g.Body;
        }

        /// <summary>Premier obstacle sur le rayon, hors joueur et hors objet tenu.</summary>
        public bool Nearest(Vector3 origin, Vector3 direction, float range, out RaycastHit nearest)
        {
            nearest = default;
            int count = Scene.Raycast(origin, direction, _hits, range, Mask, QueryTriggerInteraction.Ignore);
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                Collider col = _hits[i].collider;
                if (_owner != null && col.transform.IsChildOf(_owner))
                    continue;
                if (IgnoreId != 0 && col.attachedRigidbody != null)
                {
                    var g = col.attachedRigidbody.GetComponent<Grabbable>();
                    if (g != null && g.Id == IgnoreId)
                        continue;
                }

                if (best < 0 || _hits[i].distance < _hits[best].distance)
                    best = i;
            }

            if (best < 0)
                return false;
            nearest = _hits[best];
            return true;
        }
    }

    /// <summary>
    /// Côté physique de la télékinésie d'un joueur : applique les commandes de TelekinesisOrb aux corps (attraper,
    /// lancer, poser, lâcher) et fait suivre à l'objet tenu un point devant les yeux, en vitesse (il bute sur les murs au
    /// lieu de les traverser). L'objet tenu ne touche jamais son porteur, ni juste après avoir été relâché.
    /// Appelé par PlayerMotor après le pas du mouvement, avant la simulation physique.
    /// </summary>
    public sealed class TelekinesisHolder
    {
        private readonly Collider _ownerCollider;
        private readonly PhysicsGrabProbe _probe;
        private readonly List<(Grabbable grabbable, float until)> _ignored = new List<(Grabbable, float)>();
        private float _stuckTime;
        private bool _reached;
        private float _lastError;

        public Grabbable Held { get; private set; }

        /// <summary>Vitesse donnée au dernier objet lancé (debug, mesures).</summary>
        public Vector3 LastThrowVelocity { get; private set; }

        public TelekinesisHolder(Collider ownerCollider, PhysicsGrabProbe probe)
        {
            _ownerCollider = ownerCollider;
            _probe = probe;
        }

        /// <summary>Applique la commande ; faux si l'objet à attraper n'existe plus.</summary>
        public bool Apply(in TelekinesisCommand cmd, TelekinesisOrbSettings cfg, Vector3 eye, Vector3 playerVelocity)
        {
            switch (cmd.Kind)
            {
                case TelekinesisCommandKind.Attraper:
                {
                    Grabbable g = Grabbable.Find(cmd.TargetId);
                    if (g == null)
                        return false;
                    Held = g;
                    _stuckTime = 0f;
                    _reached = false;
                    _lastError = float.PositiveInfinity;
                    g.OnGrabbed();
                    SetIgnore(g, true);
                    _ignored.RemoveAll(e => e.grabbable == g);
                    return true;
                }
                case TelekinesisCommandKind.Lancer:
                {
                    Grabbable g = Take(cmd.TargetId);
                    if (g == null)
                        return true;
                    // Vers ce que vise le viseur (convergence), depuis la position de l'objet.
                    Vector3 target = _probe.Nearest(eye, cmd.AimDirection, 500f, out RaycastHit hit)
                        ? hit.point
                        : eye + cmd.AimDirection * 500f;
                    Vector3 dir = target - g.Body.position;
                    if (dir.sqrMagnitude < 1e-4f)
                        dir = cmd.AimDirection;
                    LastThrowVelocity = TelekinesisOrb.ThrowVelocity(dir, playerVelocity, cfg);
                    g.Body.linearVelocity = LastThrowVelocity;
                    g.OnReleased(thrown: true, noGravityFor: cfg.throwNoGravityTime);
                    IgnoreFor(g, cfg.ignoreHolderAfterRelease);
                    return true;
                }
                case TelekinesisCommandKind.Poser:
                {
                    Grabbable g = Take(cmd.TargetId);
                    if (g == null)
                        return true;
                    g.Body.linearVelocity = Vector3.ClampMagnitude(g.Body.linearVelocity, cfg.placeMaxSpeed);   // posé doucement
                    g.Body.angularVelocity = Vector3.zero;
                    g.OnReleased(thrown: false);
                    IgnoreFor(g, cfg.ignoreHolderAfterRelease);
                    return true;
                }
                case TelekinesisCommandKind.Lacher:
                {
                    Grabbable g = Take(cmd.TargetId);
                    if (g == null)
                        return true;
                    g.OnReleased(thrown: false);                         // garde sa vitesse actuelle
                    IgnoreFor(g, cfg.ignoreHolderAfterRelease);
                    return true;
                }
                default:
                    return true;
            }
        }

        /// <summary>
        /// Fait suivre le point de maintien à l'objet tenu ; faux s'il est perdu (coincé trop longtemps, détruit).
        /// eye est la position des yeux après le pas du mouvement de ce tick, eyeVelocity le déplacement réel des yeux
        /// pendant ce tick divisé par dt. L'objet reprend ce déplacement tel quel, plus une correction de l'écart qu'il
        /// avait AVANT ce déplacement : ainsi, il ne prend pas un tick d'avance à grande vitesse (avant correction :
        /// 0.5 m d'avance à 30 m/s, 0.67 m à 40 m/s).
        /// </summary>
        public bool Drive(TelekinesisOrbSettings cfg, Vector3 eye, Vector3 aimDir, Vector3 eyeVelocity, float dt)
        {
            if (Held == null)
                return false;
            if (!Held.isActiveAndEnabled)
            {
                Held = null;
                return false;
            }

            Vector3 holdPoint = HoldPoint(cfg, eye, aimDir);
            Vector3 error = holdPoint - eyeVelocity * dt - Held.Body.position;                    // écart avant ce tick
            Vector3 correction = Vector3.ClampMagnitude(error * cfg.holdGain, cfg.approachSpeed);   // vole vers le point, ralentit en arrivant
            Held.Body.linearVelocity = eyeVelocity + correction;
            Held.Body.angularVelocity *= 0.8f;

            // Coincé : après avoir rejoint le point de maintien, trop loin de lui ; avant (objet attrapé de loin, en
            // chemin), seulement s'il n'avance plus.
            float distance = error.magnitude;
            if (distance <= cfg.breakDistance)
                _reached = true;
            bool stuck = _reached ? distance > cfg.breakDistance : distance > _lastError - 0.01f;
            _lastError = distance;
            _stuckTime = stuck ? _stuckTime + dt : 0f;
            if (_stuckTime <= cfg.breakTime)
                return true;

            Grabbable lost = Held;
            Held = null;
            lost.OnReleased(thrown: false);
            IgnoreFor(lost, cfg.ignoreHolderAfterRelease);
            return false;
        }

        /// <summary>Rétablit les collisions avec le porteur une fois le délai passé.</summary>
        public void Tick(float time)
        {
            for (int i = _ignored.Count - 1; i >= 0; i--)
            {
                (Grabbable g, float until) = _ignored[i];
                if (g == null)
                {
                    _ignored.RemoveAt(i);
                    continue;
                }

                if (time < until || g == Held)
                    continue;
                SetIgnore(g, false);
                _ignored.RemoveAt(i);
            }
        }

        public static Vector3 HoldPoint(TelekinesisOrbSettings cfg, Vector3 eye, Vector3 aimDir) =>
            eye + aimDir * cfg.holdDistance + Vector3.down * cfg.holdDrop;

        private Grabbable Take(int id)
        {
            Grabbable g = Held != null && Held.Id == id ? Held : Grabbable.Find(id);
            if (Held == g)
                Held = null;
            return g;
        }

        private void IgnoreFor(Grabbable g, float seconds) => _ignored.Add((g, Time.time + seconds));

        private void SetIgnore(Grabbable g, bool ignore)
        {
            if (_ownerCollider == null)
                return;
            foreach (Collider c in g.Colliders)
                if (c != null)
                    Physics.IgnoreCollision(c, _ownerCollider, ignore);
        }
    }
}
