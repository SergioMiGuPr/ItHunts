using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Trace de balle du barillet : bande peinte effilée de la bouche du canon au point d'impact, qui se défait par morceaux
    /// (dissolution) en `duree` secondes, l'arrière d'abord. Tire(a, b) la relance ; une seule instance peut servir à
    /// toutes les balles (ou une par balle, avec detruire).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class TraceVfx : MonoBehaviour
    {
        public Vector3 depart = new Vector3(0, 0, 0), arrivee = new Vector3(0, 0, 12);
        [Tooltip("Durée de vie de la trace (s).")]
        public float duree = 0.14f;
        [Tooltip("Largeur (m).")]
        public float largeur = 0.045f;
        [Tooltip("Longueur maximale de la bande visible (m) : au-delà, seule la fin du trajet est dessinée.")]
        public float longueurMax = 14f;
        public bool detruire;

        private LineRenderer _l;
        private float _age = 999f;

        public void Tire(Vector3 a, Vector3 b) { depart = a; arrivee = b; _age = 0f; Met(); }

        /// <summary>Pose l'âge (0..1 de la durée) sans animation (captures).</summary>
        public void Pose(float age01) { _age = age01 * duree; Met(); }

        private void OnEnable() { _l = GetComponent<LineRenderer>(); _l.useWorldSpace = true; _l.positionCount = 2; Met(); }

        private void Update()
        {
            if (!Application.isPlaying) return;
            _age += Time.deltaTime;
            Met();
            if (detruire && _age > duree) Destroy(gameObject);
        }

        private void Met()
        {
            if (_l == null) return;
            float k = Mathf.Clamp01(_age / Mathf.Max(duree, 1e-3f));
            Vector3 a = depart, b = arrivee;
            if (Vector3.Distance(a, b) > longueurMax) a = b - (b - a).normalized * longueurMax;
            _l.SetPosition(0, a);
            _l.SetPosition(1, b);
            _l.widthMultiplier = largeur * (1f - 0.5f * k);
            _l.textureMode = LineTextureMode.Stretch;
            // alpha de sommet = seuil de dissolution : l'arrière (0) se défait avant l'avant (1)
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(Mathf.Clamp01(1f - k * 1.6f), 0), new GradientAlphaKey(Mathf.Clamp01(1.3f - k * 1.3f), 1) });
            _l.colorGradient = g;
            _l.enabled = k < 1f;
        }
    }
}
