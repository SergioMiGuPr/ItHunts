using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    public enum EtatReticule { Neutre, Attrapable, Ennemi }

    /// <summary>
    /// Réticule (aperçu DA) : l'étoile encrée du cadre de la mini-carte. Neutre : petite étoile argentée ; Attrapable : lueur
    /// violette (couleur de la télékinésie), l'étoile grandit ; Ennemi : lueur rouge, l'étoile tourne de 45° (forme en X,
    /// lisible sans la couleur). Le jeu appelle SetEtat ; la transition est lissée.
    /// </summary>
    public sealed class ReticuleJeu : MonoBehaviour
    {
        public Image etoile;
        public Image lueur;
        public EtatReticule etat;
        [Header("Neutre / Attrapable / Ennemi")]
        public Color[] teinte = { new Color(1f, 0.98f, 1f, 0.95f), new Color(0.92f, 0.82f, 1f, 1f), new Color(1f, 0.74f, 0.72f, 1f) };
        public Color[] couleurLueur = { new Color(0.8f, 0.8f, 0.9f, 0.12f), new Color(0.66f, 0.35f, 1f, 0.6f), new Color(1f, 0.16f, 0.12f, 0.55f) };
        public float[] echelle = { 1f, 1.22f, 1.12f };
        public float[] angle = { 0f, 0f, 45f };
        [Tooltip("Vitesse de la transition (par seconde).")]
        public float vitesse = 14f;
        [Tooltip("Pulsation de la lueur (amplitude, cycles par seconde).")]
        public Vector2 pulsation = new Vector2(0.18f, 1.6f);

        private float _echelle = 1f, _angle;
        private Color _teinte, _lueur;

        public void SetEtat(EtatReticule e) => etat = e;

        /// <summary>Pose l'état sans transition (captures).</summary>
        public void Pose(EtatReticule e) { etat = e; OnEnable(); }

        private void OnEnable()
        {
            int i = (int)etat;
            _echelle = echelle[i]; _angle = angle[i]; _teinte = teinte[i]; _lueur = couleurLueur[i];
            Applique(0f);
        }

        private void Update() => Applique(Time.unscaledDeltaTime);

        private void Applique(float dt)
        {
            int i = (int)etat;
            float k = dt <= 0f ? 1f : 1f - Mathf.Exp(-vitesse * dt);
            _echelle = Mathf.Lerp(_echelle, echelle[i], k);
            _angle = Mathf.LerpAngle(_angle, angle[i], k);
            _teinte = Color.Lerp(_teinte, teinte[i], k);
            _lueur = Color.Lerp(_lueur, couleurLueur[i], k);
            if (etoile != null)
            {
                etoile.rectTransform.localScale = Vector3.one * _echelle;
                etoile.rectTransform.localEulerAngles = new Vector3(0, 0, _angle);
                etoile.color = _teinte;
            }
            if (lueur != null)
            {
                float p = dt <= 0f ? 1f : 1f + pulsation.x * Mathf.Sin(Time.unscaledTime * pulsation.y * 6.2832f);
                Color c = _lueur; c.a *= p;
                lueur.color = c;
                lueur.rectTransform.localScale = Vector3.one * _echelle;
            }
        }
    }
}
