using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Surface non magnétique (D-051) : la traction ne s'y accroche pas (refus en rouge, comme les autres refus) ; la répulsion
    /// marche. Sert aux mini-zones de parcours, seuls endroits où l'on contourne le traqueur. Un composant sur l'objet (ou un
    /// parent) qui porte le collider ; en greybox, matériau gris pâle à rayures, sans lueur bleue.
    /// </summary>
    public sealed class NonMagneticSurface : MonoBehaviour
    {
    }
}
