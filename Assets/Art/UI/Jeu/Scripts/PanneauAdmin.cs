using System;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Panneau d'administration du playtest (nuit du 2026-11-22) : cadre compact en haut à gauche, titre, lignes de commandes
    /// (bascule : pastille allumée ou éteinte ; action : cartouche). Le jeu fournit les libellés et écoute Commande(index) ;
    /// Etat(index, bool) allume la pastille d'une bascule.
    /// </summary>
    public sealed class PanneauAdmin : MonoBehaviour
    {
        public Text[] libelles;
        public Image[] pastilles;      // null pour une action
        public Button[] boutons;
        public Text touche;
        public Color allume = new Color(0.55f, 0.85f, 1f), eteint = new Color(0.25f, 0.23f, 0.27f);

        public event Action<int> Commande;

        private void Awake()
        {
            for (int i = 0; i < boutons.Length; i++)
            {
                int k = i;
                if (boutons[i]) boutons[i].onClick.AddListener(() => Commande?.Invoke(k));
            }
        }

        public void Etat(int i, bool on)
        {
            if (i >= 0 && i < pastilles.Length && pastilles[i]) pastilles[i].color = on ? allume : eteint;
        }
    }
}
