using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Barre de vie du boss (nuit du 2026-11-22) : en haut au centre, longue barre faite des pièces de la barre de vie du
    /// joueur (cadre et fond en 9 tranches), remplissage rouge sang et traînée claire qui rattrape la valeur, crans de
    /// phase sur le canal, nom gothique du boss au-dessus. Apparaît en fondu. Le jeu appelle Montre(nom), Regle(0..1),
    /// Phase(i) (le cran passé s'éteint et la barre tremble), Cache().
    /// </summary>
    public sealed class BarreVieBoss : MonoBehaviour
    {
        public Image remplissage, retard;
        public RectTransform[] crans;
        public Text nom;
        public CanvasGroup groupe;
        public RectTransform corps;
        public float vitesseRetard = 0.6f;

        private float _valeur = 1f, _retard = 1f, _cible, _secousse;

        public void Montre(string nomBoss) { nom.text = nomBoss; _cible = 1f; gameObject.SetActive(true); }
        public void Cache() => _cible = 0f;
        public void Regle(float v01) { _valeur = Mathf.Clamp01(v01); }
        public void Phase(int i)
        {
            if (i >= 0 && i < crans.Length) crans[i].GetComponent<Image>().color = new Color(0.3f, 0.26f, 0.3f, 0.8f);
            _secousse = 1f;
        }

        public void Pose(float v01, float retard01) { _valeur = v01; _retard = Mathf.Max(v01, retard01); Applique(0f); if (groupe) groupe.alpha = 1f; }

        private void Update() => Applique(Time.unscaledDeltaTime);

        private void Applique(float dt)
        {
            _retard = Mathf.Max(_valeur, Mathf.MoveTowards(_retard, _valeur, vitesseRetard * dt));
            remplissage.fillAmount = _valeur;
            retard.fillAmount = _retard;
            if (groupe) groupe.alpha = Mathf.MoveTowards(groupe.alpha, _cible, dt * 3f);
            _secousse = Mathf.MoveTowards(_secousse, 0f, dt * 2.5f);
            if (corps) corps.anchoredPosition = _secousse > 0 ? new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * 6f * _secousse, 0) : Vector2.zero;
        }
    }
}
