using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Artefacts qu'on peut tirer au début d'une run (D-032). Un seul pool pour l'instant ; les améliorations et la
    /// méta-progression pourront en débloquer d'autres. Modifiable en Play Mode (pris en compte à la run suivante).
    /// </summary>
    [CreateAssetMenu(fileName = "ArtifactPool_Default", menuName = "ItHunts/Réglages/Pool d'artefacts")]
    public sealed class ArtifactPool : ScriptableObject
    {
        public List<ArtifactDefinition> artifacts = new List<ArtifactDefinition>();
    }
}
