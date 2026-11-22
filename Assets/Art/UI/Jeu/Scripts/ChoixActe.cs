using System;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Menu de choix de l'acte pour le playtest (nuit du 2026-11-22) : vignette de l'acte (capture du décor), nom gothique
    /// (« Acte I »), sous-titre, flèches gauche et droite (et touches gauche/droite, manette), bouton Lancer. Le jeu écoute
    /// Lance(index).
    /// </summary>
    public sealed class ChoixActe : MonoBehaviour
    {
        [Serializable] public struct Acte { public string nom; public string sousTitre; public Sprite vignette; }

        public Acte[] actes;
        public Image vignette;
        public Text nom, sousTitre, compteur;
        public Button gauche, droite, lancer;
        public int index;

        public event Action<int> Lance;

        private void Awake()
        {
            if (gauche) gauche.onClick.AddListener(() => Va(-1));
            if (droite) droite.onClick.AddListener(() => Va(1));
            if (lancer) lancer.onClick.AddListener(() => Lance?.Invoke(index));
            Affiche();
        }

        public void Va(int d)
        {
            if (actes == null || actes.Length == 0) return;
            index = (index + d + actes.Length) % actes.Length;
            Affiche();
        }

        public void Affiche()
        {
            if (actes == null || actes.Length == 0) return;
            var a = actes[Mathf.Clamp(index, 0, actes.Length - 1)];
            vignette.sprite = a.vignette;
            nom.text = a.nom;
            sousTitre.text = a.sousTitre;
            if (compteur) compteur.text = $"{index + 1} / {actes.Length}";
        }
    }
}
