using UnityEngine;

namespace ItHunts.Art.Menus
{
    /// <summary>
    /// Gèle les effets des menus (shaders ItHunts/UI/*) : brume et étincelles éteintes, lumières au repos, aucune
    /// déformation. Sert aux captures comparées aux maquettes et à l'option d'accessibilité « réduire les animations ».
    /// </summary>
    [ExecuteAlways]
    public sealed class MenuFige : MonoBehaviour
    {
        private static readonly int FigeId = Shader.PropertyToID("_ItHuntsMenuFige");
        private static readonly int TempsId = Shader.PropertyToID("_ItHuntsMenuTemps");

        [Tooltip("Effets gelés (rendu = maquette).")]
        public bool fige;

        private void OnEnable() => Applique();
        private void OnValidate() => Applique();
        private void OnDisable() => Regle(false);

        private void Applique() => Regle(fige);

        public static void Regle(bool gele, float decalageTemps = 0f)
        {
            Shader.SetGlobalFloat(FigeId, gele ? 1f : 0f);
            Shader.SetGlobalFloat(TempsId, decalageTemps);
        }
    }
}
