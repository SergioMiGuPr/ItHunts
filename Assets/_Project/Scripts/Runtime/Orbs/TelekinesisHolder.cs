using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Rayon de visée de la télékinésie avec la physique Unity : la première surface touchée (hors joueur et hors objet
    /// tenu) doit porter un Grabbable, sinon rien.
    /// </summary>
    public sealed class PhysicsGrabProbe : IGrabProbe
    {
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Transform _owner;

        public LayerMask Mask { get; set; }

        /// <summary>Objet tenu, ignoré par le rayon (0 = aucun).</summary>
        public int IgnoreId { get; set; }

        public PhysicsGrabProbe(LayerMask mask, Transform owner)
        {
            Mask = mask;
            _owner = owner;
        }

        public bool FindGrabbable(Vector3 origin, Vector3 direction, float range, out GrabCandidate candidate)
        {
            candidate = default;
            if (!Nearest(origin, direction, range, out RaycastHit hit))
                return false;
            Grabbable g = hit.collider.attachedRigidbody != null ? hit.collider.attachedRigidbody.GetComponent<Grabbable>() : null;
            if (g == null || !g.isActiveAndEnabled)
                return false;
            candidate = new GrabCandidate { Id = g.Id, Mass = g.Mass, Distance = hit.distance, Point = hit.point };
            return true;
        }

        /// <summary>Premier obstacle sur le rayon, hors joueur et hors objet tenu.</summary>
        public bool Nearest(Vector3 origin, Vector3 direction, float range, out RaycastHit nearest)
        {
            nearest = default;
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, range, Mask, QueryTriggerInteraction.Ignore);
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
                    g.OnReleased(thrown: true);
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

        /// <summary>Fait suivre le point de maintien à l'objet tenu ; faux s'il est perdu (coincé trop longtemps, détruit).</summary>
        public bool Drive(TelekinesisOrbSettings cfg, Vector3 eye, Vector3 aimDir, Vector3 playerVelocity, float dt)
        {
            if (Held == null)
                return false;
            if (!Held.isActiveAndEnabled)
            {
                Held = null;
                return false;
            }

            Vector3 holdPoint = HoldPoint(cfg, eye, aimDir);
            Vector3 error = holdPoint - Held.Body.position;
            Vector3 correction = Vector3.ClampMagnitude(error * cfg.holdGain, cfg.approachSpeed);   // vole vers le point, ralentit en arrivant
            Held.Body.linearVelocity = playerVelocity + correction;
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
