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
        public static Vector3 AirControl(Vector3 velocity, Vector3 targetDir, float strength, float dt,
            float maxAngleDegrees = 180f)
        {
            if (strength <= 0f)
                return velocity;

            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            float speed = horizontal.magnitude;
            if (speed < 1e-3f)
                return velocity;

            Vector3 dir = horizontal / speed;
            float angleDegrees = Vector3.Angle(dir, targetDir);
            if (angleDegrees > maxAngleDegrees)
                return velocity;   // caméra trop loin de la trajectoire : pas de demi-tour forcé

            float alpha = strength >= 1f ? 1f : 1f - Mathf.Pow(1f - strength, dt * 60f);
            float angle = angleDegrees * Mathf.Deg2Rad;
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

        /// <summary>
        /// Reporte le long du mur la part de vitesse horizontale qui entre dedans : longer un mur ne fait jamais
        /// perdre de vitesse. La norme gardée est au plus speedBefore (vitesse avant l'accélération du tick) :
        /// pousser contre le mur ne fait pas non plus accélérer ; un gain le long du mur (strafe) est gardé.
        /// </summary>
        public static Vector3 ProjectAlongWall(Vector3 velocity, Vector3 wallNormal, float speedBefore)
        {
            var h = new Vector3(velocity.x, 0f, velocity.z);
            float into = Vector3.Dot(h, wallNormal);
            if (into >= 0f)
                return velocity;
            Vector3 along = h - wallNormal * into;
            float speed = Mathf.Max(along.magnitude, Mathf.Min(h.magnitude, speedBefore));
            h = along.sqrMagnitude > 1e-6f ? along.normalized * speed : along;
            return new Vector3(h.x, velocity.y, h.z);
        }

        /// <summary>
        /// Vitesse après un wall jump : l'impulsion (kick) s'ajoute à la vitesse acquise, dans une direction qui
        /// mélange la normale du mur (bias 0) et l'avant (bias 1 : caméra sans sa part vers le mur, sinon
        /// trajectoire le long du mur). Vitesse horizontale finale ≥ vitesse d'avant + bonus. L'impulsion et le bonus sont réduits
        /// par gainScale (D-044 : −15 %), l'élan acquis est gardé tel quel ; le gain ne monte pas au-delà de bonusCap, et rien ne
        /// freine un joueur déjà plus rapide.
        /// </summary>
        public static Vector3 WallJumpVelocity(Vector3 velocity, Vector3 wallNormal, float yawDegrees, float forwardBias,
            float kick, float speedBonus, float jumpVelocity, float bonusCap = float.PositiveInfinity, float gainScale = 1f)
        {
            var h = new Vector3(velocity.x, 0f, velocity.z);
            float speedBefore = h.magnitude;
            float into = Vector3.Dot(h, wallNormal);
            if (into < 0f)
                h -= wallNormal * into;                       // rien vers le mur

            Vector3 forward = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
            float forwardInto = Vector3.Dot(forward, wallNormal);
            if (forwardInto < 0f)
                forward -= wallNormal * forwardInto;          // la caméra regarde le mur : on garde sa part le long du mur
            if (forward.sqrMagnitude < 0.01f)
                forward = h - wallNormal * Vector3.Dot(h, wallNormal);   // sinon : la trajectoire le long du mur
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : wallNormal;

            Vector3 dir = wallNormal * (1f - forwardBias) + forward * forwardBias;
            dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : wallNormal;
            h += dir * (kick * gainScale);                     // impulsion réduite sur un mur (D-044)

            // Gain plafonné : au-delà de bonusCap, la vitesse est conservée mais plus augmentée.
            float limit = Mathf.Max(speedBefore, bonusCap);
            float minSpeed = Mathf.Min(speedBefore + speedBonus * gainScale, limit);
            if (h.magnitude < minSpeed)
                h = h.sqrMagnitude > 1e-6f ? h.normalized * minSpeed : dir * minSpeed;
            if (h.magnitude > limit)
                h = h.normalized * limit;

            return new Vector3(h.x, Mathf.Max(velocity.y, jumpVelocity), h.z);
        }

        /// <summary>
        /// Gain de vitesse sur un mur (strafe le long du mur en wall slide / wall run, D-044) : la part gagnée depuis speedBefore
        /// est réduite par scale et s'arrête à cap ; une perte n'est pas touchée, et un joueur déjà au-delà de cap garde sa vitesse.
        /// </summary>
        public static Vector3 WallGain(Vector3 velocity, float speedBefore, float scale, float cap)
        {
            var h = new Vector3(velocity.x, 0f, velocity.z);
            float speed = h.magnitude;
            if (speed <= speedBefore || speed < 1e-6f)
                return velocity;
            float target = speedBefore >= cap ? speedBefore : Mathf.Min(speedBefore + (speed - speedBefore) * scale, cap);
            h *= target / speed;
            return new Vector3(h.x, velocity.y, h.z);
        }

        /// <summary>Gain de vitesse plafonné : rien au-delà de cap, et jamais au-dessus de cap en gagnant.</summary>
        public static float CappedGain(float speed, float gain, float cap) =>
            speed >= cap ? speed : Mathf.Min(speed + gain, cap);

        /// <summary>
        /// Profil du dash : vitesse au tick index (0 = départ) sur ticks, entre le pic et la sortie. Progressif :
        /// baisse linéaire du pic vers la sortie ; Constant : le pic pendant tout le dash.
        /// </summary>
        public static float DashSpeedAt(int index, int ticks, float peak, float exit, bool progressive) =>
            progressive ? exit + (peak - exit) * (1f - index / (float)ticks) : peak;

        /// <summary>Pic de vitesse du dash pour parcourir distance en ticks (jamais sous la vitesse de sortie).</summary>
        public static float DashPeakSpeed(float distance, int ticks, float dt, float exit, bool progressive)
        {
            float duration = ticks * dt;
            if (!progressive)
                return Mathf.Max(exit, distance / duration);
            float extra = distance - duration * exit;        // distance à couvrir au-dessus de la vitesse de sortie
            return extra <= 0f ? exit : exit + extra / (dt * (ticks + 1) * 0.5f);
        }

        /// <summary>
        /// Traction du magnétisme : toute la vitesse est redirigée vers dir (ligne droite) et ne baisse jamais ;
        /// sous tractionSpeed, elle monte de acceleration·dt. Quand dir monte, la vitesse vers le bas (chute) n'est
        /// pas comptée. Aucun plafond des gains (exception à movementGainCap).
        /// </summary>
        public static Vector3 TractionVelocity(Vector3 velocity, Vector3 dir, float acceleration, float tractionSpeed, float dt)
        {
            if (dir.y > 0f && velocity.y < 0f)
                velocity.y = 0f;
            float speed = velocity.magnitude;
            float target = speed >= tractionSpeed ? speed : Mathf.Min(speed + acceleration * dt, tractionSpeed);
            return dir * target;
        }

        /// <summary>
        /// Répulsion du magnétisme : impulsion ajoutée dans dir. La vitesse horizontale ne baisse jamais (au pire elle
        /// est conservée et redirigée) ; son gain suit le plafond commun des gains. Quand dir monte, la chute est
        /// d'abord annulée.
        /// </summary>
        public static Vector3 RepelVelocity(Vector3 velocity, Vector3 dir, float impulse, float gainCap)
        {
            if (dir.y > 0f && velocity.y < 0f)
                velocity.y = 0f;
            var before = new Vector3(velocity.x, 0f, velocity.z);
            float speedBefore = before.magnitude;
            Vector3 raw = velocity + dir * impulse;
            var h = new Vector3(raw.x, 0f, raw.z);
            float speedRaw = h.magnitude;
            float target = speedRaw > speedBefore
                ? Mathf.Min(speedRaw, CappedGain(speedBefore, speedRaw - speedBefore, gainCap))
                : speedBefore;
            Vector3 hDir = speedRaw > 1e-4f ? h / speedRaw
                : speedBefore > 1e-4f ? before / speedBefore
                : Vector3.zero;
            return hDir * target + Vector3.up * raw.y;
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
