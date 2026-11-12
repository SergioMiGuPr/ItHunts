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

        /// <summary>Inclinaison verticale de la vue (degrés ; positif = vers le bas, comme la caméra). Sert à viser.</summary>
        public float Pitch;

        /// <summary>Bouton de saut maintenu au moment du tick.</summary>
        public bool JumpHeld;

        /// <summary>Tick auquel le dernier appui de saut a été pris en compte (MovementState.NeverTick si aucun).</summary>
        public int JumpPressedTick;

        /// <summary>Bouton de glissade / accroupi maintenu.</summary>
        public bool SlideHeld;

        /// <summary>Tick du dernier appui de dash (0 ou MovementState.NeverTick si aucun ; les ticks commencent à 1).</summary>
        public int DashPressedTick;

        /// <summary>
        /// Orbe unique (D-024) : chaque action a sa touche, toutes disponibles en même temps.
        /// Attraction (A en AZERTY / RB, maintenu) : tick du dernier appui (0 ou moins : aucun).
        /// </summary>
        public int AttractPressedTick;

        /// <summary>Bouton d'attraction maintenu (la traction dure tant qu'il est tenu).</summary>
        public bool AttractHeld;

        /// <summary>Répulsion (clic droit / LT) : tick du dernier appui.</summary>
        public int RepelPressedTick;

        /// <summary>Répulsion maintenue (D-029) : la poussée continue dure tant qu'il est tenu (au plus repelHoldMaxDuration).</summary>
        public bool RepelHeld;

        /// <summary>Attraper l'objet visé, ou relâcher l'objet tenu (F / X) : tick du dernier appui.</summary>
        public int GrabPressedTick;

        /// <summary>
        /// Action principale (clic gauche / RT) : lance l'objet tenu ; sans objet, tire avec l'artefact (D-032). Tick du
        /// dernier appui.
        /// </summary>
        public int ThrowPressedTick;

        /// <summary>Recharger l'artefact (R / Y) : tick du dernier appui.</summary>
        public int ReloadPressedTick;

        public static MovementInput Neutral => new MovementInput
        {
            JumpPressedTick = MovementState.NeverTick,
            DashPressedTick = MovementState.NeverTick,
            AttractPressedTick = MovementState.NeverTick,
            RepelPressedTick = MovementState.NeverTick,
            GrabPressedTick = MovementState.NeverTick,
            ThrowPressedTick = MovementState.NeverTick,
            ReloadPressedTick = MovementState.NeverTick,
        };
    }
}
