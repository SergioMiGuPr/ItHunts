using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Flash de lumière d'un effet ponctuel (nuit du 2026-11-22) : la lumière ponctuelle monte à `intensite` puis s'éteint en
    /// `duree` s (courbe rapide), et peut pulser en boucle (point faible du boss : `pulsation` > 0). Cosmétique, local.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class LumiereFlash : MonoBehaviour
    {
        public float intensite = 3f;
        public float duree = 0.25f;
        [Tooltip("0 : un seul flash ; > 0 : pulsation continue (cycles par seconde)")]
        public float pulsation;
        [Range(0f, 1f)] public float plancher = 0.35f;

        private Light _l;
        private float _t;

        private void OnEnable() { _l = GetComponent<Light>(); _t = 0f; Applique(); }

        private void Update() { _t += Time.deltaTime; Applique(); }

        /// <summary>Pose l'état à un instant donné (captures).</summary>
        public void Pose(float t) { _l = GetComponent<Light>(); _t = t; Applique(); }

        private void Applique()
        {
            if (!_l) return;
            if (pulsation > 0f)
            {
                float s = 0.5f + 0.5f * Mathf.Sin(_t * pulsation * Mathf.PI * 2f);
                _l.intensity = intensite * Mathf.Lerp(plancher, 1f, s * s);
            }
            else
            {
                float k = Mathf.Clamp01(_t / Mathf.Max(duree, 0.01f));
                _l.intensity = intensite * (1f - k) * (1f - k);
            }
        }
    }
}
