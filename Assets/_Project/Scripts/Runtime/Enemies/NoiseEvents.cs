using UnityEngine;

namespace ItHunts.Enemies
{
    public enum NoiseKind : byte
    {
        AtterrissageDur,
        Repulsion,
        Tir,
        ObjetBrise,
        Combat,
    }

    /// <summary>Un bruit (ouïe des ennemis) : où, quoi, et le joueur qui l'a fait (0 : inconnu, le plus proche).</summary>
    public struct Noise
    {
        public Vector3 Point;
        public NoiseKind Kind;
        public int PlayerId;
        public float Time;
    }

    /// <summary>
    /// Canal des bruits du joueur (atterrissage dur, répulsion, tir). Les objets brisés et les coups portés sont entendus
    /// directement sur leurs propres canaux (AreaEvents, DamageEvents). Chaque ennemi filtre selon sa portée d'ouïe.
    /// </summary>
    public static class NoiseEvents
    {
        public static event System.Action<Noise> Heard;
        public static void Raise(in Noise n) => Heard?.Invoke(n);

        /// <summary>Vitesse de chute (m/s) à partir de laquelle un atterrissage s'entend (saut d'environ 3.6 m).</summary>
        public const float HardLandingSpeed = 12f;
    }
}
