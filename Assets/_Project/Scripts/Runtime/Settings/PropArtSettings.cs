using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Props en 3D de la DA (nuit 4, lot 4, PR #51) à la place des greybox : pour chaque greybox, les modèles candidats (le modèle et
    /// ses variantes) et les bornes d'échelle. Règle de pose de la DA (INTEGRATION-PROPS, « deuxième passe », Props4.Ajuste) :
    /// candidat aux proportions les plus proches, tourné de 90° si l'axe long diffère, échelle uniforme (moyenne géométrique des
    /// rapports d'axes), posé au centre de la boîte, au sol ; au-delà d'un écart de forme de 0.9, la greybox reste.
    /// </summary>
    [CreateAssetMenu(fileName = "PropArt_Default", menuName = "ItHunts/Props en 3D de la DA")]
    public sealed class PropArtSettings : ScriptableObject
    {
        [System.Serializable]
        public sealed class Entry
        {
            [Tooltip("Nom de la greybox (Prop_Tonneau…) ou début du nom (« Coffre : » pour les coffres des salles).")]
            public string greybox;
            public bool prefix;
            [Tooltip("Modèle et variantes de la DA (P_Prop_*).")]
            public GameObject[] models = new GameObject[0];
            [Tooltip("0 : échelle de la règle (moyenne géométrique). Sinon cette échelle fixe (attrapables : 1, coffres : 1.5).")]
            [Min(0f)] public float fixedScale;
            [Min(0.1f)] public float maxScale = 2f;
            [Tooltip("Coffre : le couvercle de la DA (enfant « Couvercle ») devient celui du coffre.")]
            public bool chest;
        }

        public bool enabledInGame = true;
        [Tooltip("Écart de forme au-delà duquel la greybox reste (somme des |log| des rapports d'axes restants).")]
        [Min(0f)] public float maxShapeError = 0.9f;
        public List<Entry> entries = new List<Entry>();

        public Entry Find(string name)
        {
            foreach (Entry e in entries)
                if (e != null && !string.IsNullOrEmpty(e.greybox) && (e.prefix ? name.StartsWith(e.greybox) : name == e.greybox))
                    return e;
            return null;
        }
    }
}
