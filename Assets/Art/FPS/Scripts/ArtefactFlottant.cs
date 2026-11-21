using UnityEngine;

namespace ItHunts.Art.FPS
{
    /// <summary>
    /// Artefact qui flotte (livre du hibou) : monte et descend doucement, se balance et tourne un peu autour de sa position
    /// de repos (repère local du parent : la main gauche). Réglable ; désactivable par le jeu pendant une action.
    /// </summary>
    [ExecuteAlways]
    public sealed class ArtefactFlottant : MonoBehaviour
    {
        [Tooltip("Position de repos (repère du parent, m).")]
        public Vector3 repos = new Vector3(0f, 0.2f, 0.06f);
        [Tooltip("Rotation de repos (degrés).")]
        public Vector3 rotationRepos = new Vector3(-18f, 0f, 0f);
        [Tooltip("Flottement vertical (m) et vitesse (cycles par seconde).")]
        public Vector2 flottement = new Vector2(0.012f, 0.35f);
        [Tooltip("Balancement (degrés) et vitesse (cycles par seconde).")]
        public Vector2 balancement = new Vector2(4f, 0.23f);

        [Tooltip("Échelle du livre dans la main (le modèle fait 0,23 m de large ouvert).")]
        public float echelle = 0.6f;

        private void OnEnable() => Update();
        private void OnValidate() => Update();

        private void Update()
        {
            transform.localScale = Vector3.one * echelle;
            float t = Application.isPlaying ? Time.time : 0f;
            float y = Mathf.Sin(t * flottement.y * 6.2832f) * flottement.x;
            float a = Mathf.Sin(t * balancement.y * 6.2832f) * balancement.x;
            float b = Mathf.Sin(t * balancement.y * 4.1f + 1.3f) * balancement.x * 0.6f;
            transform.localPosition = repos + new Vector3(0, y, 0);
            transform.localRotation = Quaternion.Euler(rotationRepos + new Vector3(b, a, 0));
        }
    }
}
