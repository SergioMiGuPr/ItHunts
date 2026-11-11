using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>
    /// Formules du mouvement façon Quake/Source, sur la seule composante horizontale (x, z).
    /// La composante verticale (y) est toujours conservée telle quelle.
    /// </summary>
    public static class MovementMath
    {
        /// <summary>Friction au sol : freine d'au moins stopSpeed·friction·dt, jusqu'à l'arrêt complet.</summary>
        public static Vector3 ApplyFriction(Vector3 velocity, float friction, float stopSpeed, float dt)
        {
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontal.magnitude;
            if (speed < 1e-4f)
                return new Vector3(0f, velocity.y, 0f);

            float control = Mathf.Max(speed, stopSpeed);
            float newSpeed = Mathf.Max(0f, speed - control * friction * dt);
            horizontal *= newSpeed / speed;
            return new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>Accélération au sol vers wishSpeed dans la direction wishDir (normalisée, horizontale).</summary>
        public static Vector3 Accelerate(Vector3 velocity, Vector3 wishDir, float wishSpeed, float acceleration, float dt)
        {
            float currentSpeed = Vector3.Dot(velocity, wishDir);
            float addSpeed = wishSpeed - currentSpeed;
            if (addSpeed <= 0f)
                return velocity;

            float accelSpeed = Mathf.Min(acceleration * wishSpeed * dt, addSpeed);
            return velocity + accelSpeed * wishDir;
        }

        /// <summary>
        /// Accélération en l'air façon Source : seule la projection de la vitesse sur wishDir est
        /// plafonnée (à wishSpeedCap). Strafer en tournant ajoute donc de la vitesse sans plafond strict.
        /// </summary>
        public static Vector3 AirAccelerate(Vector3 velocity, Vector3 wishDir, float wishSpeed, float wishSpeedCap,
            float acceleration, float dt)
        {
            float cappedWish = Mathf.Min(wishSpeed, wishSpeedCap);
            float currentSpeed = Vector3.Dot(velocity, wishDir);
            float addSpeed = cappedWish - currentSpeed;
            if (addSpeed <= 0f)
                return velocity;

            float accelSpeed = Mathf.Min(acceleration * wishSpeed * dt, addSpeed);
            return velocity + accelSpeed * wishDir;
        }

        /// <summary>
        /// Contrôle aérien : fait tourner la vitesse horizontale vers targetDir (normalisée, horizontale)
        /// sans changer sa norme. strength = part de l'angle rattrapée par tick de 1/60 s (1 = instantané).
        /// </summary>
        public static Vector3 AirControl(Vector3 velocity, Vector3 targetDir, float strength, float dt)
        {
            if (strength <= 0f)
                return velocity;

            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontal.magnitude;
            if (speed < 1e-3f)
                return velocity;

            float alpha = strength >= 1f ? 1f : 1f - Mathf.Pow(1f - strength, dt * 60f);
            Vector3 dir = horizontal / speed;
            float angle = Vector3.Angle(dir, targetDir) * Mathf.Deg2Rad;
            Vector3 newDir = alpha >= 1f ? targetDir : Vector3.RotateTowards(dir, targetDir, angle * alpha, 0f);
            Vector3 result = newDir.normalized * speed;
            return new Vector3(result.x, velocity.y, result.z);
        }

        /// <summary>
        /// Accélération horizontale due à la pente (m/s²) : g·sin θ le long de la pente, projetée à
        /// l'horizontale (g·sin θ·cos θ), vers le bas de la pente. Nulle sur le plat.
        /// </summary>
        public static Vector3 SlopeAcceleration(Vector3 groundNormal, float gravity)
        {
            var downhill = new Vector3(groundNormal.x, 0f, groundNormal.z);
            float sinTheta = downhill.magnitude;
            if (sinTheta < 1e-4f)
                return Vector3.zero;
            float cosTheta = Mathf.Clamp01(groundNormal.y);
            return downhill / sinTheta * (gravity * sinTheta * cosTheta);
        }

        /// <summary>Retire une décélération constante (m/s²) à la vitesse horizontale, sans l'inverser.</summary>
        public static Vector3 Decelerate(Vector3 velocity, float deceleration, float dt)
        {
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontal.magnitude;
            if (speed < 1e-4f)
                return new Vector3(0f, velocity.y, 0f);
            float newSpeed = Mathf.Max(0f, speed - deceleration * dt);
            horizontal *= newSpeed / speed;
            return new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>
        /// Garantit une vitesse horizontale minimale, dans la direction actuelle (ou fallbackDir à l'arrêt).
        /// </summary>
        public static Vector3 EnsureMinHorizontalSpeed(Vector3 velocity, float minSpeed, Vector3 fallbackDir)
        {
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontal.magnitude;
            if (speed >= minSpeed)
                return velocity;
            Vector3 dir = speed > 1e-3f ? horizontal / speed : fallbackDir;
            horizontal = dir * minSpeed;
            return new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>Plafonne la vitesse horizontale (plafond de sécurité).</summary>
        public static Vector3 ClampHorizontal(Vector3 velocity, float maxSpeed)
        {
            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (horizontal.sqrMagnitude <= maxSpeed * maxSpeed)
                return velocity;
            horizontal = horizontal.normalized * maxSpeed;
            return new Vector3(horizontal.x, velocity.y, horizontal.z);
        }

        /// <summary>Entrée « avancer sans strafer » : avant, à moins d'environ 19° de l'axe.</summary>
        public static bool IsForwardOnly(Vector2 move) => move.y > 0.1f && Mathf.Abs(move.x) <= 0.35f * move.y;

        /// <summary>Direction voulue dans le monde à partir de l'entrée locale et du yaw. Longueur ≤ 1.</summary>
        public static Vector3 WishVector(Vector2 move, float yawDegrees)
        {
            var local = new Vector3(move.x, 0f, move.y);
            if (local.sqrMagnitude > 1f)
                local.Normalize();
            return Quaternion.Euler(0f, yawDegrees, 0f) * local;
        }
    }
}
