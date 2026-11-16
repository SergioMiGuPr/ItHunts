using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Modèle du barillet (SM_Barillet, étape 10) : état visuel entièrement dérivé de l'état simulé du joueur (BarrelState :
    /// munitions, nombre de tirs, rechargement), sans état à part, pour qu'il se synchronise de lui-même en ligne.
    /// - 6 chambres (SM_Barillet_Chambre_01 à _06), la 01 en haut au repos, puis 02, 03… dans le sens inverse des aiguilles d'une
    ///   montre vu du joueur. La chambre en haut est la prochaine à tirer ; après k tirs, c'est la chambre k (modulo 6).
    /// - Chaque tir fait tourner le barillet de −60° autour de son Z local (sens horaire vu du joueur) : la chambre suivante
    ///   vient en haut. La rotation finit toujours sur un multiple exact de 60°.
    /// </summary>
    public static class BarrelModel
    {
        public const int Chambers = 6;
        public const float StepDegrees = 360f / Chambers;

        /// <summary>Chambre en haut (0 = la 01) après shotCount tirs.</summary>
        public static int TopChamber(int shotCount) => Mod(shotCount, Chambers);

        /// <summary>
        /// Chambres allumées : les munitions restantes (au plus 6) ; pendant un rechargement, elles se rallument une à une au fil
        /// de l'avancement (sans jamais éteindre celles qui restaient chargées).
        /// </summary>
        public static int Lit(int rounds, bool reloading, float reloadProgress)
        {
            int lit = Mathf.Clamp(rounds, 0, Chambers);
            if (reloading)
                lit = Mathf.Max(lit, Mathf.Clamp(Mathf.FloorToInt(reloadProgress * Chambers + 1e-4f), 0, Chambers));
            return lit;
        }

        /// <summary>
        /// La chambre (0 à 5) est-elle chargée ? Les chargées sont la chambre du haut et les suivantes dans l'ordre de tir ; les
        /// vides, celles qui viennent de tirer.
        /// </summary>
        public static bool Loaded(int chamber, int shotCount, int lit) => Mod(chamber - shotCount, Chambers) < lit;

        /// <summary>Angle de repos (degrés, autour du Z local) après shotCount tirs : −60° par tir, ramené dans ]−360, 0].</summary>
        public static float RestAngle(int shotCount) => -StepDegrees * TopChamber(shotCount);

        private static int Mod(int a, int n) => ((a % n) + n) % n;
    }

    /// <summary>
    /// Animation de rotation du barillet, d'un cran par tir, sans dérive : la cible est toujours le nombre de tirs (entier) ; un
    /// tir pendant l'animation repart de l'angle affiché vers le nouveau cran. L'animation finie, l'angle vaut exactement
    /// −60° × (tirs modulo 6). Vue seulement (rien dans la simulation).
    /// </summary>
    public struct BarrelSpin
    {
        /// <summary>Nombre de tirs visé (le compteur de la simulation).</summary>
        public int Target;
        /// <summary>Position de départ de l'animation, en crans (peut être fractionnaire).</summary>
        public float From;
        public float StartTime;
        public bool Started;

        /// <summary>Suit le compteur de tirs : un nouveau tir lance l'animation depuis la position affichée.</summary>
        public void Follow(int shotCount, float now, float duration, AnimationCurve curve)
        {
            if (!Started)
            {
                Target = shotCount;
                From = shotCount;
                StartTime = float.NegativeInfinity;
                Started = true;
                return;
            }

            if (shotCount == Target)
                return;
            From = Steps(now, duration, curve);
            Target = shotCount;
            StartTime = now;
        }

        /// <summary>Position affichée, en crans ; exactement Target une fois l'animation finie.</summary>
        public float Steps(float now, float duration, AnimationCurve curve)
        {
            float u = duration <= 0f ? 1f : (now - StartTime) / duration;
            if (u >= 1f || float.IsNaN(u))
                return Target;
            float k = curve != null ? curve.Evaluate(Mathf.Clamp01(u)) : Mathf.Clamp01(u);
            return Mathf.LerpUnclamped(From, Target, k);
        }

        /// <summary>Angle affiché (degrés, Z local) : −60° par cran, ramené près de zéro (multiple exact de 60° au repos).</summary>
        public float Angle(float now, float duration, AnimationCurve curve)
        {
            float steps = Steps(now, duration, curve);
            int turns = Mathf.FloorToInt(Target / (float)BarrelModel.Chambers);
            return -BarrelModel.StepDegrees * (steps - turns * BarrelModel.Chambers);
        }

        public bool Done(float now, float duration) => duration <= 0f || now - StartTime >= duration;
    }
}
