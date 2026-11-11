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
