using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Panneau du marchand ou du coffre (aperçu DA) : cadre des menus (9 tranches), titre, cartes d'amélioration en colonne,
    /// zone de description (carte survolée), solde d'âmes, bouton Fermer (cartouche long). Le jeu appelle Remplit puis
    /// écoute Achat (index de la carte) et Ferme. Une carte indisponible ne déclenche pas Achat.
    /// </summary>
    public sealed class PanneauBoutique : MonoBehaviour
    {
        [Serializable]
        public sealed class Offre
        {
            public Sprite icone;
            public string nom;
            public int prix;
            [TextArea] public string description;
        }

        public Text titre;
        public Text description;
        public Text solde;
        public Button fermer;
        public CarteAmelioration[] cartes = new CarteAmelioration[0];
        [Tooltip("Texte de la description quand aucune carte n'est survolée.")]
        public string invite = "Survole une amélioration pour la lire.";
        [Tooltip("Aperçu : offres affichées au démarrage, âmes du joueur.")]
        public Offre[] apercu = new Offre[0];
        public int amesApercu = 120;

        public event Action<int> Achat;
        public event Action Ferme;

        private Offre[] _offres = new Offre[0];
        private bool _branche;

        private void Awake() => Branche();

        private void Branche()
        {
            if (_branche) return;
            _branche = true;
            for (int i = 0; i < cartes.Length; i++)
            {
                int k = i;
                if (cartes[i] == null) continue;
                cartes[i].GetComponent<Button>().onClick.AddListener(() => Clic(k));
                cartes[i].Survolee += (c, s) => { if (description != null) description.text = s ? c.description : invite; };
            }
            if (fermer != null)
                fermer.onClick.AddListener(() => Ferme?.Invoke());
        }

        private void Start()
        {
            if (apercu.Length > 0 && _offres.Length == 0)
                Remplit(apercu, amesApercu);
        }

        public void Remplit(IList<Offre> offres, int ames)
        {
            Branche();
            _offres = new Offre[offres.Count];
            offres.CopyTo(_offres, 0);
            for (int i = 0; i < cartes.Length; i++)
            {
                bool on = i < _offres.Length;
                cartes[i].gameObject.SetActive(on);
                if (on)
                    cartes[i].Configure(_offres[i].icone, _offres[i].nom, _offres[i].prix, _offres[i].description, _offres[i].prix <= ames);
            }
            if (solde != null) solde.text = ames.ToString();
            if (description != null) description.text = invite;
        }

        private void Clic(int i)
        {
            if (i >= _offres.Length || !cartes[i].disponible)
                return;
            Achat?.Invoke(i);
        }
    }
}
