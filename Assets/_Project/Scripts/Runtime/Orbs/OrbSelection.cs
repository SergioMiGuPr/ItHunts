using ItHunts.Movement;

namespace ItHunts.Orbs
{
    /// <summary>Orbe active (une seule à la fois).</summary>
    public enum ActiveOrb : byte
    {
        Magnetisme,
        Telekinesie,
    }

    /// <summary>État de la sélection d'orbe d'un joueur : copiable (réseau).</summary>
    public struct OrbSelectionState
    {
        public ActiveOrb Active;
        public int LastConsumedSwapPressTick;
        public int LastConsumedSelectPressTick;

        /// <summary>Tick du dernier échange (retour visuel).</summary>
        public int LastSwapTick;

        public static OrbSelectionState Create() => new OrbSelectionState
        {
            Active = ActiveOrb.Magnetisme,
            LastConsumedSwapPressTick = MovementState.NeverTick,
            LastConsumedSelectPressTick = MovementState.NeverTick,
            LastSwapTick = MovementState.NeverTick,
        };
    }

    /// <summary>
    /// Échange instantané des orbes, sans recharge : molette ou Y (bascule), touches 1 et 2 (choix direct).
    /// Renvoie vrai si l'orbe active a changé ce tick (l'appelant arrête alors la traction ou lâche l'objet tenu).
    /// </summary>
    public static class OrbSelection
    {
        public static bool Step(ref OrbSelectionState o, in MovementInput input, int tick)
        {
            ActiveOrb before = o.Active;
            if (input.OrbSwapPressedTick > 0 && input.OrbSwapPressedTick > o.LastConsumedSwapPressTick
                                            && input.OrbSwapPressedTick <= tick)
            {
                o.LastConsumedSwapPressTick = input.OrbSwapPressedTick;
                o.Active = o.Active == ActiveOrb.Magnetisme ? ActiveOrb.Telekinesie : ActiveOrb.Magnetisme;
            }

            if (input.OrbSelectPressedTick > 0 && input.OrbSelectPressedTick > o.LastConsumedSelectPressTick
                                              && input.OrbSelectPressedTick <= tick)
            {
                o.LastConsumedSelectPressTick = input.OrbSelectPressedTick;
                if (input.OrbSelect == 1)
                    o.Active = ActiveOrb.Magnetisme;
                else if (input.OrbSelect == 2)
                    o.Active = ActiveOrb.Telekinesie;
            }

            if (o.Active == before)
                return false;
            o.LastSwapTick = tick;
            return true;
        }

        /// <summary>Entrée sans les boutons d'orbe (pour l'orbe inactive).</summary>
        public static MovementInput WithoutOrbButtons(in MovementInput input)
        {
            MovementInput copy = input;
            copy.AttractPressedTick = MovementState.NeverTick;
            copy.AttractHeld = false;
            copy.RepelPressedTick = MovementState.NeverTick;
            return copy;
        }
    }
}
