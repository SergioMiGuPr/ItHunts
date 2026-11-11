using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>
    /// Entrée du mouvement pour un tick de simulation. Structure simple et copiable :
    /// c'est ce qui sera envoyé au serveur et rejoué en prédiction réseau.
    /// </summary>
    public struct MovementInput
    {
        /// <summary>Déplacement voulu, repère local : x = droite, y = avant. Longueur ≤ 1.</summary>
        public Vector2 Move;

        /// <summary>Orientation horizontale de la vue (degrés, monde).</summary>
        public float Yaw;

        /// <summary>Bouton de saut maintenu au moment du tick.</summary>
        public bool JumpHeld;

        /// <summary>Tick auquel le dernier appui de saut a été pris en compte (MovementState.NeverTick si aucun).</summary>
        public int JumpPressedTick;

        /// <summary>Bouton de glissade / accroupi maintenu.</summary>
        public bool SlideHeld;

        public static MovementInput Neutral => new MovementInput { JumpPressedTick = MovementState.NeverTick };
    }
}
