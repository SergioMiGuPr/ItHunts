using System;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Menu d'interaction d'une porte (nuit du 2026-11-22) : petit cadre au-dessus du centre de l'écran, nom de la porte,
    /// prix en âmes (flamme + nombre), touche d'action. Payable : prix clair, invite « Ouvrir » ; trop cher : prix rouge,
    /// invite « Âmes insuffisantes », l'évènement Ouvrir n'est pas émis. Le jeu appelle Montre(nom, prix, âmes) quand le
    /// joueur regarde la porte, Cache() sinon, et Valide() à l'appui de la touche.
    /// </summary>
    public sealed class PanneauPorte : MonoBehaviour
    {
        public Text nom, prix, invite, touche;
        public Image flamme, cadre;
        public CanvasGroup groupe;
        public Color prixOk = new Color(0.86f, 0.9f, 0.96f), prixTrop = new Color(0.86f, 0.22f, 0.25f);
        public string toucheTexte = "E";
        [Header("Aperçu")] public string nomApercu = "Porte scellée";
        public int prixApercu = 40, amesApercu = 65;

        [Header("Sortie d'acte encore scellée (nuit 4)")]
        public Text temps, explication;
        public float secondesApercu = 161f;

        public event Action Ouvrir;
        private bool _payable;
        private float _cible = 1f;

        public void Montre(string nomPorte, int prixAmes, int ames)
        {
            nom.text = nomPorte;
            if (temps) temps.gameObject.SetActive(false);
            if (explication) explication.gameObject.SetActive(false);
            invite.gameObject.SetActive(true);
            if (touche && touche.transform.parent) touche.transform.parent.gameObject.SetActive(true);
            _payable = ames >= prixAmes;
            bool gratuit = prixAmes <= 0;
            prix.gameObject.SetActive(!gratuit);
            flamme.gameObject.SetActive(!gratuit);
            prix.text = prixAmes.ToString();
            prix.color = _payable ? prixOk : prixTrop;
            flamme.color = _payable ? Color.white : new Color(0.55f, 0.5f, 0.55f);
            invite.text = _payable ? "Ouvrir" : "Âmes insuffisantes";
            invite.color = _payable ? new Color(0.97f, 0.88f, 0.7f) : new Color(0.6f, 0.56f, 0.6f);
            touche.text = toucheTexte;
            _cible = 1f;
            gameObject.SetActive(true);
        }

        /// <summary>Porte de sortie pas encore ouverte : temps restant (m:ss) et explication ; pas d'achat possible. À rappeler à chaque
        /// image (ou chaque seconde) tant que le panneau est visible : le minuteur n'avance pas seul.</summary>
        public void MontreSortieScellee(float secondesRestantes, int prixAmes)
        {
            Montre("Sortie scellée", prixAmes, 0);
            _payable = false;
            int s = Mathf.Max(0, Mathf.CeilToInt(secondesRestantes));
            if (temps) { temps.gameObject.SetActive(true); temps.text = "S'ouvre dans " + (s / 60) + ":" + (s % 60).ToString("00"); }
            if (explication)
            {
                explication.gameObject.SetActive(true);
                explication.text = prixAmes > 0 ? "Le passage vers l'acte suivant se descelle avec le temps. Il faudra alors des âmes pour le franchir."
                                                : "Le passage vers l'acte suivant se descelle avec le temps.";
            }
            prix.color = prixOk; flamme.color = Color.white;
            invite.gameObject.SetActive(false);                                  // scellée : ni touche ni invite
            if (touche && touche.transform.parent) touche.transform.parent.gameObject.SetActive(false);
        }

        public void Cache() => _cible = 0f;

        public void Valide()
        {
            if (_payable) Ouvrir?.Invoke();
        }

        private void Update()
        {
            if (!groupe) return;
            groupe.alpha = Mathf.MoveTowards(groupe.alpha, _cible, Time.unscaledDeltaTime * 8f);
            if (_cible == 0f && groupe.alpha <= 0f) gameObject.SetActive(false);
        }

        public void MontreApercu() { Montre(nomApercu, prixApercu, amesApercu); if (groupe) groupe.alpha = 1f; }
    }
}
