using ItHunts.Movement;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>Ce que montre l'orbe unique (D-024) : une couleur par action.</summary>
    public enum OrbLookKind : byte
    {
        Prete,      // neutre
        Recharge,   // terne : plus de charge d'attraction
        Traction,   // bleu
        Repulsion,  // orange
        Tenu,       // violet
        Lancer,     // vert
    }

    /// <summary>
    /// Couleur de l'orbe quand plusieurs actions se superposent (D-024) : un éclat bref l'emporte sur un état qui dure,
    /// et la traction l'emporte sur l'objet tenu.
    ///   lancer (vert, éclat) &gt; répulsion (orange, éclat) &gt; traction (bleu) &gt; objet tenu (violet) &gt; recharge &gt; prête.
    /// Exemple : traction en tenant une caisse → orbe bleue (ce qui te déplace), la caisse et son trait restent violets ;
    /// lancer en arrivant → éclair vert, puis bleu ou neutre. Logique pure (tests).
    /// </summary>
    public static class OrbLook
    {
        public static OrbLookKind Resolve(in MovementState s, in MagnetismState m, in TelekinesisState t,
            int flashTicks)
        {
            int tick = s.Tick;
            if (t.LastAction == TelekinesisCommandKind.Lancer && t.LastActionTick != MovementState.NeverTick
                                                              && tick - t.LastActionTick < flashTicks)
                return OrbLookKind.Lancer;
            if (m.RepelHolding || (m.LastRepelTick != MovementState.NeverTick && tick - m.LastRepelTick < flashTicks))
                return OrbLookKind.Repulsion;                            // orange pendant toute la répulsion maintenue
            if (s.Mode == MovementMode.Traction)
                return OrbLookKind.Traction;
            if (t.Holding)
                return OrbLookKind.Tenu;
            if (m.AttractCharges >= 0f && m.AttractCharges < 1f)
                return OrbLookKind.Recharge;
            return OrbLookKind.Prete;
        }
    }
}
