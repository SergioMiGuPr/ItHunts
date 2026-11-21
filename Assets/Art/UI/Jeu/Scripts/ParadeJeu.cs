using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Recharge de la parade : le losange cerclé des maquettes. En recharge : un voile sombre recule comme une aiguille et
    /// découvre l'icône (remplissage radial), l'icône est atténuée ; prête : icône pleine, brille une fois ; Contre() : éclat
    /// blanc-bleu quand une parade réussit. Le jeu appelle Regle(0..1) chaque image (1 = prête).
    /// </summary>
    public sealed class ParadeJeu : MonoBehaviour
    {
        public Image icone;
        public Image voile;          // Filled, Radial 360, origine Top, sens horaire
        public Image lueur;
        public Color couleurLueur = new Color(0.8f, 0.9f, 1f, 1f);
        [Range(0f, 1f)] public float attenuation = 0.45f;
        public float dureeEclat = 0.4f;

        private float _valeur = 1f, _eclat, _contre;

        public void Regle(float recharge01)
        {
            recharge01 = Mathf.Clamp01(recharge01);
            if (recharge01 >= 1f && _valeur < 1f)
                _eclat = 1f;
            _valeur = recharge01;
        }

        public void Contre() => _contre = 1f;

        /// <summary>Applique l'état tout de suite (captures) ; contre = éclat du contre réussi (0..1).</summary>
        public void Pose(float recharge01, float contre)
        {
            _valeur = Mathf.Clamp01(recharge01); _eclat = 0f; _contre = contre;
            Applique(0f);
        }

        private void Update() => Applique(Time.unscaledDeltaTime);

        private void Applique(float dt)
        {
            _eclat = Mathf.Max(0f, _eclat - dt / dureeEclat);
            _contre = Mathf.Max(0f, _contre - dt / (dureeEclat * 1.5f));
            bool prete = _valeur >= 1f;
            if (voile != null)
                voile.fillAmount = prete ? 0f : 1f - _valeur;
            if (icone != null)
            {
                float v = prete ? 1f : attenuation;
                icone.color = new Color(v, v, v, 1f);
                icone.rectTransform.localScale = Vector3.one * (1f + 0.12f * Mathf.Max(_eclat, _contre));
            }
            if (lueur != null)
            {
                Color l = couleurLueur; l.a = Mathf.Max(_eclat * 0.7f, _contre * 0.85f);
                lueur.color = l;
                lueur.rectTransform.localScale = Vector3.one * (1f + 0.4f * _contre);
            }
        }
    }
}
