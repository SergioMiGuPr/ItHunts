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

        /// <summary>Torches et lampes actives (fondu de l'ambiance enragée sans recherche dans la scène).</summary>
        public static readonly System.Collections.Generic.List<AmbianceLight> All = new System.Collections.Generic.List<AmbianceLight>();

        /// <summary>Intensité donnée par l'ambiance (le vacillement de HorrorLook la module) et position de départ de la flamme.</summary>
        public float BaseIntensity { get; private set; }
        public Vector3 BaseLocalPosition { get; private set; }
        public Light Light { get; private set; }

        private void OnEnable()
        {
            Light = GetComponent<Light>();
            BaseLocalPosition = transform.localPosition;
            All.Add(this);
            Apply();
        }

        private void OnDisable() => All.Remove(this);

        /// <summary>Calque de rendu de la vue FPS (mains, artefacts) : les torches, lampes et la lune l'éclairent, pas la lanterne.</summary>
        public const int FpsRenderingLayer = 1;

        /// <summary>Ajoute le calque de la vue FPS aux calques éclairés par cette lumière.</summary>
        public static void LightViewmodel(Light l)
        {
            if (l == null)
                return;
            l.renderingLayerMask |= 1 << FpsRenderingLayer;
            var data = l.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            if (data != null)
                data.renderingLayers |= 1u << FpsRenderingLayer;
        }

        public void Apply()
        {
            var a = SceneAmbiance.Active;
            var l = GetComponent<Light>();
            LightViewmodel(l);
            if (a == null || l == null)
                return;
            l.intensity = kind == Kind.Torche ? a.torchIntensity : a.lampIntensity;
            BaseIntensity = l.intensity;
            l.range = kind == Kind.Torche ? a.torchRange : a.lampRange;
        }
    }
}
