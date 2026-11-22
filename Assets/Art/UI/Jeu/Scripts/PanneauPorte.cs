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

        public event Action Ouvrir;
        private bool _payable;
        private float _cible = 1f;

        public void Montre(string nomPorte, int prixAmes, int ames)
        {
            nom.text = nomPorte;
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
