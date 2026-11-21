using System;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>
    /// ICollisionProbe branché sur un CharacterController : sol par SphereCast sous les pieds,
    /// déplacement par CharacterController.Move, recollage au sol en descente.
    /// </summary>
    public sealed class CharacterControllerProbe : ICollisionProbe
    {
        private const float CastLift = 0.1f;        // départ du lancer un peu au-dessus des pieds
        private const float GroundTolerance = 0.08f; // distance sous la capsule encore considérée « au sol »
        private const float RadiusFactor = 0.9f;     // sphère un peu plus fine, pour ne pas toucher les murs voisins

        private readonly CharacterController _controller;
        private readonly MovementSettings _settings;
        private readonly LayerMask _mask;
        private bool _lastMoveHitBelow;

        public CharacterControllerProbe(CharacterController controller, MovementSettings settings, LayerMask mask)
        {
            _controller = controller;
            _settings = settings;
            _mask = mask;
        }

        public Vector3 Position => _controller.transform.position;

        public float Height => _controller.height;

        public void SetHeight(float height)
        {
            height = Mathf.Max(height, _controller.radius * 2f);
            if (Mathf.Approximately(_controller.height, height))
                return;
            _controller.height = height;
            _controller.center = new Vector3(0f, height * 0.5f, 0f);
        }

        public bool HasHeadroom(float targetHeight)
        {
            float grow = targetHeight - _controller.height;
            if (grow <= 0f)
                return true;

            // Lancer vers le haut depuis la sphère du sommet (départ dans notre propre capsule, ignorée).
            float radius = _controller.radius * RadiusFactor;
            Vector3 topSphere = Position + Vector3.up * (_controller.height - _controller.radius);
            return !Physics.SphereCast(topSphere, radius, Vector3.up, out _, grow + _controller.skinWidth, _mask,
                QueryTriggerInteraction.Ignore);
        }

        private const float WallDistance = 0.15f;   // distance de contact avec un mur (m)
        private const float MaxWallNormalY = 0.3f;  // |normale.y| max : murs à plus de ≈ 72°

        public bool ProbeWall(ReadOnlySpan<Vector3> directions, out Vector3 normal, out int wallId)
        {
            foreach (Vector3 dir in directions)
            {
                if (CastWall(dir, out normal, out wallId))
                    return true;
            }

            normal = Vector3.zero;
            wallId = 0;
            return false;
        }

        private Vector3 _sideNormal;
        private bool _hasSideNormal;

        /// <summary>Oublie les contacts mémorisés (après une téléportation).</summary>
        public void ResetContacts()
        {
            _lastMoveHitBelow = false;
            _hasSideNormal = false;
        }

        /// <summary>Appelé par OnControllerColliderHit pendant CharacterController.Move.</summary>
        public void ReportHit(Vector3 normal)
        {
            if (Mathf.Abs(normal.y) < 0.7f)
            {
                _sideNormal = normal;
                _hasSideNormal = true;
            }
        }

        private bool CastWall(Vector3 dir, out Vector3 normal, out int wallId)
        {
            normal = Vector3.zero;
            wallId = 0;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f)
                return false;
            dir.Normalize();

            float radius = _controller.radius * RadiusFactor;
            // Bas de la capsule relevé au-dessus de la marche franchissable : le sol n'est pas un mur.
            Vector3 p1 = Position + Vector3.up * (_controller.radius + _controller.stepOffset);
            Vector3 p2 = Position + Vector3.up * Mathf.Max(_controller.height - _controller.radius, p1.y - Position.y);
            if (!Physics.CapsuleCast(p1, p2, radius, dir, out RaycastHit hit, WallDistance + _controller.skinWidth,
                    _mask, QueryTriggerInteraction.Ignore))
                return false;
            if (Mathf.Abs(hit.normal.y) > MaxWallNormalY)
                return false;

            normal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            wallId = hit.collider.GetInstanceID();
            return true;
        }

        private readonly Collider[] _overlap = new Collider[8];

        public bool ProbeLedge(Vector3 dir, float minRise, float maxRise, float minDepth, out Vector3 standPoint)
        {
            standPoint = Vector3.zero;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f)
                return false;
            dir.Normalize();
            float r = _controller.radius;
            float height = _settings.capsuleHeight;
            Vector3 feet = Position;

            // 1. Le mur devant, bas sur la capsule (sous le dessus du rebord le plus bas accepté).
            Vector3 low = feet + Vector3.up * Mathf.Clamp(minRise * 0.5f, 0.05f, 0.3f);
            if (!Physics.Raycast(low, dir, out RaycastHit wall, r + 0.4f, _mask, QueryTriggerInteraction.Ignore)
                || Mathf.Abs(wall.normal.y) > MaxWallNormalY || IsSelf(wall.collider))
                return false;
            Vector3 n = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;

            // 2. Le dessus, juste après le bord, et 3. sa profondeur.
            float startY = feet.y + maxRise + 0.05f;
            float span = maxRise - minRise + 0.05f;
            Vector3 edge = new Vector3(wall.point.x, startY, wall.point.z) - n * (r + 0.1f);
            if (!TopAt(edge, span, out RaycastHit top))
                return false;
            Vector3 deep = new Vector3(wall.point.x, startY, wall.point.z) - n * Mathf.Max(minDepth, r * 2f + 0.1f);
            if (!TopAt(deep, span + 0.3f, out RaycastHit far) || Mathf.Abs(far.point.y - top.point.y) > 0.3f)
                return false;
            float rise = top.point.y - feet.y;
            if (rise < minRise || rise > maxRise)
                return false;

            // 4. La place debout sur le dessus, et 5. pour monter le long du mur.
            Vector3 stand = new Vector3(edge.x, top.point.y, edge.z);
            if (Blocked(stand + Vector3.up * 0.05f, height, r * 0.95f))
                return false;
            Vector3 raised = new Vector3(feet.x, top.point.y, feet.z) + n * 0.02f;
            if (Blocked(raised + Vector3.up * 0.05f, height, r * 0.95f))
                return false;

            standPoint = stand;
            return true;
        }

        private bool TopAt(Vector3 from, float distance, out RaycastHit hit)
        {
            hit = default;
            if (Physics.CheckSphere(from, 0.02f, _mask, QueryTriggerInteraction.Ignore))
                return false;                                       // départ dans un mur plus haut que le rebord accepté
            return Physics.Raycast(from, Vector3.down, out hit, distance, _mask, QueryTriggerInteraction.Ignore)
                   && IsWalkable(hit.normal) && !IsSelf(hit.collider);
        }

        private bool Blocked(Vector3 feet, float height, float radius)
        {
            int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * radius, feet + Vector3.up * (height - radius), radius,
                _overlap, _mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!IsSelf(_overlap[i]) && _overlap[i].attachedRigidbody == null)
                    return true;
            return false;
        }

        private bool IsSelf(Collider c) => c == _controller || c.transform.IsChildOf(_controller.transform);

        public bool ProbeGround(out Vector3 normal)
        {
            float distance = CastLift + _controller.skinWidth + GroundTolerance;
            if (CastDown(distance, out RaycastHit hit) && IsWalkable(hit.normal))
            {
                normal = hit.normal;
                return true;
            }

            // Au bord d'une marche, la sphère peut toucher l'arête avec une normale penchée alors que
            // la capsule repose bien sur quelque chose : on se fie alors au dernier contact par-dessous.
            normal = Vector3.up;
            return _lastMoveHitBelow;
        }

        public MoveResult Move(Vector3 delta, bool snapToGround)
        {
            Vector3 start = Position;
            _hasSideNormal = false;
            CollisionFlags flags = _controller.Move(delta);
            bool hitBelow = (flags & CollisionFlags.Below) != 0;

            if (snapToGround && !hitBelow)
            {
                float distance = CastLift + _controller.skinWidth + _settings.groundSnapDistance;
                if (CastDown(distance, out RaycastHit hit) && IsWalkable(hit.normal))
                {
                    float drop = hit.distance - CastLift;
                    CollisionFlags snapFlags = _controller.Move(Vector3.down * Mathf.Max(drop, _controller.skinWidth));
                    hitBelow = (snapFlags & CollisionFlags.Below) != 0;
                }
            }

            _lastMoveHitBelow = hitBelow;
            return new MoveResult
            {
                HitBelow = hitBelow,
                HitSides = (flags & CollisionFlags.Sides) != 0,
                HitAbove = (flags & CollisionFlags.Above) != 0,
                ActualDelta = Position - start,
                SideNormal = _sideNormal,
                HasSideNormal = _hasSideNormal,
            };
        }

        private bool CastDown(float distance, out RaycastHit hit)
        {
            float radius = _controller.radius * RadiusFactor;
            Vector3 bottomSphere = Position + _controller.center
                                   + Vector3.down * (_controller.height * 0.5f - _controller.radius);
            Vector3 origin = bottomSphere + Vector3.up * CastLift;
            return Physics.SphereCast(origin, radius, Vector3.down, out hit, distance, _mask,
                QueryTriggerInteraction.Ignore);
        }

        private bool IsWalkable(Vector3 normal) => Vector3.Angle(normal, Vector3.up) <= _settings.slopeLimit + 0.5f;
    }
}
