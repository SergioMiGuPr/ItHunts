using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Catalogue des modules d'une zone (carte aléatoire) : les prefabs de salle et de carrefour que le générateur peut tirer.
    /// Seuls les modules validés (ItHunts > Valider le module) et de poids non nul entrent dans le tirage ; l'ordre de la liste
    /// fait partie du tirage (même catalogue, même seed : même carte).
    /// </summary>
    [CreateAssetMenu(fileName = "ModuleCatalog_Niveau1", menuName = "ItHunts/Niveaux/Catalogue de modules")]
    public sealed class ModuleCatalog : ScriptableObject
    {
        public List<RoomModule> modules = new List<RoomModule>();

        /// <summary>Modules tirables, dans l'ordre du catalogue.</summary>
        public List<RoomModule> Usable()
        {
            var list = new List<RoomModule>();
            foreach (RoomModule m in modules)
                if (m != null && m.validated && m.weight > 0f)
                    list.Add(m);
            return list;
        }
    }
}
