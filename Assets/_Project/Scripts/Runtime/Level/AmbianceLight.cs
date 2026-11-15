using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Lumière réglée par l'ambiance de la scène (SceneAmbiance, D-067) : torche ou lampe. Intensité et portée prises dans
    /// l'ambiance active à l'activation (la carte aléatoire active son décor après l'avoir construit).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class AmbianceLight : MonoBehaviour
    {
        public enum Kind : byte
        {
            Torche,
            Lampe,
        }

        public Kind kind;

        private void OnEnable() => Apply();

        public void Apply()
        {
            var a = SceneAmbiance.Active;
            var l = GetComponent<Light>();
            if (a == null || l == null)
                return;
            l.intensity = kind == Kind.Torche ? a.torchIntensity : a.lampIntensity;
            l.range = kind == Kind.Torche ? a.torchRange : a.lampRange;
        }
    }
}
