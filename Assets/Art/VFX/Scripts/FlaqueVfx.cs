using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Flaque d'ombre au sol d'une apparition (nuit 5, lot 4) : un disque du shader ItHunts/VFX/MarquageSol (indice sombre :
    /// le sol s'assombrit, fin trait d'encre fumée) qui s'ouvre, reste le temps de l'effet, puis se referme. Valeurs par
    /// instance dans un MaterialPropertyBlock (le matériau partagé n'est pas modifié).
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class FlaqueVfx : MonoBehaviour
    {
        [Tooltip("Ouverture, tenue et fermeture (s).")]
        public float ouverture = 0.18f, tenue = 0.7f, fermeture = 0.45f;
        [Tooltip("Le disque grandit de ce facteur à cette taille pendant l'ouverture.")]
        public float depart = 0.4f;

        static readonly int PFondu = Shader.PropertyToID("_Fondu");
        Renderer _r; MaterialPropertyBlock _b; float _t; Vector3 _echelle;

        void Awake() => _echelle = transform.localScale;

        void OnEnable()
        {
            _r = GetComponent<Renderer>(); _b ??= new MaterialPropertyBlock();
            _t = 0f; if (Application.isPlaying) Pose(0f);
        }

        void Update() { if (Application.isPlaying) { _t += Time.deltaTime; Pose(_t); } }

        /// <summary>État au temps t (s) depuis le début de l'effet (aussi pour les captures hors lecture).</summary>
        public void Pose(float t)
        {
            if (_r == null) _r = GetComponent<Renderer>();
            _b ??= new MaterialPropertyBlock();
            if (_echelle == Vector3.zero) _echelle = transform.localScale;
            float o = Mathf.Clamp01(t / Mathf.Max(ouverture, 0.01f));
            float f = 1f - Mathf.Clamp01((t - ouverture - tenue) / Mathf.Max(fermeture, 0.01f));
            float k = Mathf.SmoothStep(0f, 1f, o);
            transform.localScale = new Vector3(_echelle.x * Mathf.Lerp(depart, 1f, k), _echelle.y, _echelle.z * Mathf.Lerp(depart, 1f, k));
            _r.GetPropertyBlock(_b);
            _b.SetFloat(PFondu, k * f);
            _r.SetPropertyBlock(_b);
            _r.enabled = k * f > 0.001f;
        }
    }
}
