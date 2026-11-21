using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Carte d'amélioration d'un panneau de marchand ou de coffre : le cartouche des âmes (rond de l'icône, bandeau du nom),
    /// prix en âmes avec la flamme gris-bleu. Survol ou sélection (manette) : lueur, la carte grandit un peu, la description
    /// s'affiche dans le panneau. Indisponible (pas assez d'âmes) : carte assombrie, prix en rouge, achat refusé.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class CarteAmelioration : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        public Image icone;
        public Text nom;
        public Text prix;
        public Image flamme;
        public Image lueur;
        [Tooltip("Cadre et fond : assombris si indisponible.")]
        public Graphic[] corps = new Graphic[0];
        [TextArea] public string description;
        public bool disponible = true;
        public Color nomNormal = new Color(0.97f, 0.88f, 0.7f);
        public Color nomIndisponible = new Color(0.55f, 0.52f, 0.5f);
        public Color prixNormal = new Color(0.86f, 0.9f, 0.96f);
        public Color prixIndisponible = new Color(0.86f, 0.24f, 0.2f);
        public Color teinteIndisponible = new Color(0.42f, 0.4f, 0.44f, 1f);
        public Color couleurLueur = new Color(0.62f, 0.74f, 0.95f, 0.55f);

        public event Action<CarteAmelioration, bool> Survolee;     // (carte, survolée)

        private bool _survol, _selection;
        private float _k;

        public void Configure(Sprite ic, string n, int p, string desc, bool dispo)
        {
            if (icone != null) { icone.sprite = ic; icone.enabled = ic != null; }
            if (nom != null) nom.text = n;
            if (prix != null) prix.text = p > 0 ? p.ToString() : "";
            if (flamme != null) flamme.enabled = p > 0;
            description = desc;
            SetDisponible(dispo);
        }

        public void SetDisponible(bool dispo)
        {
            disponible = dispo;
            if (prix != null) prix.color = dispo ? prixNormal : prixIndisponible;
            foreach (Graphic g in corps)
                if (g != null) g.color = dispo ? Color.white : teinteIndisponible;
            if (icone != null) icone.color = dispo ? Color.white : teinteIndisponible;
            if (flamme != null) flamme.color = dispo ? Color.white : teinteIndisponible;
            if (nom != null) nom.color = dispo ? nomNormal : nomIndisponible;
        }

        public void OnPointerEnter(PointerEventData e) { _survol = true; Survolee?.Invoke(this, true); }
        public void OnPointerExit(PointerEventData e) { _survol = false; if (!_selection) Survolee?.Invoke(this, false); }
        public void OnSelect(BaseEventData e) { _selection = true; Survolee?.Invoke(this, true); }
        public void OnDeselect(BaseEventData e) { _selection = false; if (!_survol) Survolee?.Invoke(this, false); }

        /// <summary>Force l'état survolé sans transition (captures, aperçu).</summary>
        public void ForceSurvol(bool s)
        {
            _survol = s; _k = s ? 1f : 0f;
            Applique();
            Survolee?.Invoke(this, s);
        }

        private void Update()
        {
            float cible = (_survol || _selection) ? 1f : 0f;
            _k = Mathf.MoveTowards(_k, cible, Time.unscaledDeltaTime * 8f);
            Applique();
        }

        private void Applique()
        {
            if (lueur != null)
            {
                Color l = couleurLueur; l.a *= _k * (disponible ? 1f : 0.4f);
                lueur.color = l;
            }
            transform.localScale = Vector3.one * (1f + 0.025f * _k);
        }
    }
}
