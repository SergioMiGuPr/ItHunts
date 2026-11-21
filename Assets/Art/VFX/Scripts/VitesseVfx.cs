using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Traînées de vitesse en bord d'écran (sensation de vitesse). À poser sous la caméra du joueur (repère local) : des
    /// traits peints naissent sur un anneau devant la caméra et filent vers elle, donc vers les bords de l'écran. Le jeu
    /// donne la vitesse horizontale du joueur (champ <see cref="vitesse"/> ou <see cref="Regle"/>) ; sous seuilBas rien
    /// n'apparaît, au-delà de seuilHaut le débit et la longueur sont au maximum. Le centre de l'écran reste toujours libre.
    /// </summary>
    [ExecuteAlways]
    public sealed class VitesseVfx : MonoBehaviour
    {
        [Tooltip("Vitesse du joueur (m/s), donnée par le jeu à chaque image.")]
        public float vitesse;
        [Tooltip("En dessous : aucune traînée (m/s).")]
        public float seuilBas = 9f;
        [Tooltip("Au-dessus : débit et longueur au maximum (m/s).")]
        public float seuilHaut = 18f;
        [Tooltip("Traits par seconde au maximum.")]
        [Min(0f)] public float debitMax = 70f;
        [Tooltip("Étirement des traits au maximum (longueur relative à la vitesse).")]
        [Min(0f)] public float etirementMax = 0.06f;
        [Tooltip("Lissage de la vitesse (s) : les traînées ne clignotent pas aux petites variations.")]
        [Min(0f)] public float lissage = 0.15f;
        [Tooltip("Force globale (0 = coupé, réglage d'accessibilité).")]
        [Range(0f, 1f)] public float force = 1f;
        public ParticleSystem traits;

        private float _k;

        public void Regle(float vitesseHorizontale) => vitesse = vitesseHorizontale;

        /// <summary>Intensité actuelle (0 à 1), après seuils et lissage.</summary>
        public float Intensite => _k;

        private void LateUpdate() => Met(Application.isPlaying ? Time.deltaTime : 1f / 60f);

        /// <summary>Met à jour l'intensité (appelé à chaque image ; aussi par les captures).</summary>
        public void Met(float dt)
        {
            if (traits == null) return;
            float cible = Mathf.Clamp01((vitesse - seuilBas) / Mathf.Max(seuilHaut - seuilBas, 0.01f)) * force;
            _k = lissage > 0f ? Mathf.MoveTowards(_k, cible, dt / lissage) : cible;
            var em = traits.emission;
            em.rateOverTime = debitMax * _k * _k;
            var r = traits.GetComponent<ParticleSystemRenderer>();
            if (r != null) r.velocityScale = etirementMax * (0.4f + 0.6f * _k);
            if (_k > 0.001f && !traits.isPlaying) traits.Play();
        }
    }
}
