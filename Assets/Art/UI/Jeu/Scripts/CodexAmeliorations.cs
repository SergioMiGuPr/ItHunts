using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Codex des améliorations (menu principal, nuit 5, lot 2). Onglets : « Communes » puis un par personnage (vignette de l'écran
    /// de choix : discrète, illuminée quand l'onglet est actif). Grille des améliorations de l'onglet (cases encadrées à la
    /// couleur du personnage ; passifs en losange, en premier). Fiche détaillée à droite : grande illustration, nom, personnage
    /// et type, effet, les 5 rangs (étoiles + effet précis de chaque rang ; le Mythique en or), synergies.
    /// Entrées : clic ou survol ; clavier et manette via OngletSuivant(±1) et Deplace(dx, dy) appelés par main ; Ferme.
    /// Données : <see cref="Remplit"/> (main : ses UpgradeDefinition) ou le catalogue de la DA (fiches de AMELIORATIONS.md).
    /// </summary>
    public sealed class CodexAmeliorations : MonoBehaviour
    {
        [Serializable]
        public sealed class Onglet
        {
            public Button bouton;
            public Image vignette, souligne;
            public Text libelle;
            public Sprite discrete, illuminee;
            [Tooltip("Autres images de l'onglet à éteindre avec la vignette (illustration de l'onglet Communes).")]
            public Graphic[] autres = new Graphic[0];
        }

        public CatalogueAmeliorations catalogue;
        public Onglet[] onglets = new Onglet[5];
        [Tooltip("Cases de la grille (30 au plus : les communes), de gauche à droite puis de haut en bas.")]
        public IconeAmelioration[] cases = new IconeAmelioration[0];
        public Image selection;
        public int colonnes = 6;
        [Header("Fiche")]
        public IconeAmelioration ficheIcone;
        public Text ficheNom, ficheType, ficheEffet, ficheSynergies, compteur;
        public Text[] ficheRangs = new Text[5];
        public Image[] ficheEtoilesRang = new Image[0];
        public Graphic[] teintes = new Graphic[0];
        public Button fermer;

        public event Action Ferme;

        private CatalogueAmeliorations.Fiche[] _donnees = new CatalogueAmeliorations.Fiche[0];
        private readonly List<CatalogueAmeliorations.Fiche> _vues = new List<CatalogueAmeliorations.Fiche>();
        private int _onglet, _sel;

        public int OngletActif => _onglet;
        public string Selection => _sel >= 0 && _sel < _vues.Count ? _vues[_sel].id : null;

        private void Awake()
        {
            for (int i = 0; i < onglets.Length; i++)
            {
                int k = i;
                if (onglets[i]?.bouton) onglets[i].bouton.onClick.AddListener(() => ChoisitOnglet(k));
            }
            for (int i = 0; i < cases.Length; i++)
            {
                if (cases[i] == null) continue;
                var s = cases[i].GetComponent<SurvolCodex>();
                if (s == null) s = cases[i].gameObject.AddComponent<SurvolCodex>();
                s.codex = this; s.index = i;
            }
            if (fermer) fermer.onClick.AddListener(() => Ferme?.Invoke());
        }

        private void OnEnable()
        {
            if (_donnees.Length == 0 && catalogue) Remplit(catalogue.fiches);
        }

        /// <summary>Toutes les améliorations (l'ordre de la liste est gardé dans chaque onglet, passifs d'abord).</summary>
        public void Remplit(CatalogueAmeliorations.Fiche[] fiches)
        {
            _donnees = fiches ?? new CatalogueAmeliorations.Fiche[0];
            ChoisitOnglet(_onglet, Selection);
        }

        public void OngletSuivant(int d) => ChoisitOnglet(((_onglet + d) % onglets.Length + onglets.Length) % onglets.Length);

        public void ChoisitOnglet(int i, string garder = null)
        {
            _onglet = Mathf.Clamp(i, 0, Mathf.Max(0, onglets.Length - 1));
            _vues.Clear();
            for (int pass = 0; pass < 2; pass++)
                foreach (var f in _donnees)
                {
                    if (f == null || CatalogueAmeliorations.Proprietaire(f.id) != _onglet) continue;
                    bool passif = catalogue && catalogue.EstPassif(f.id);
                    if (passif == (pass == 0)) _vues.Add(f);
                }
            for (int k = 0; k < onglets.Length; k++)
            {
                var o = onglets[k];
                if (o == null) continue;
                bool on = k == _onglet;
                Color teinte = on ? Color.white : new Color(0.62f, 0.6f, 0.64f);
                if (o.vignette) { o.vignette.sprite = on ? o.illuminee : o.discrete; o.vignette.color = teinte; }
                if (o.autres != null) foreach (var g in o.autres) if (g) g.color = teinte;
                if (o.souligne) o.souligne.enabled = on;
                if (o.libelle) o.libelle.color = on ? new Color(0.969f, 0.882f, 0.702f) : new Color(0.6f, 0.57f, 0.55f);
            }
            for (int k = 0; k < cases.Length; k++)
            {
                if (cases[k] == null) continue;
                if (k < _vues.Count) { cases[k].Configure(_vues[k].id, 0); cases[k].PoseEtoiles(0); }   // grille : sans rang (la fiche montre les 5)
                else cases[k].Vide();
            }
            Color c = catalogue ? catalogue.Couleur(_onglet) : Color.white;
            foreach (var g in teintes) if (g) g.color = new Color(c.r, c.g, c.b, g.color.a);
            if (compteur) compteur.text = _vues.Count + " améliorations";
            int sel = garder != null ? _vues.FindIndex(f => f.id == garder) : -1;
            Choisit(sel >= 0 ? sel : 0);
        }

        /// <summary>Manette ou flèches : déplacement dans la grille.</summary>
        public void Deplace(int dx, int dy)
        {
            if (_vues.Count == 0) return;
            int n = Mathf.Clamp(_sel + dx + dy * colonnes, 0, _vues.Count - 1);
            Choisit(n);
        }

        public void Choisit(int i)
        {
            if (_vues.Count == 0) { _sel = -1; if (selection) selection.enabled = false; return; }
            _sel = Mathf.Clamp(i, 0, _vues.Count - 1);
            var f = _vues[_sel];
            if (selection && cases.Length > _sel && cases[_sel])
            {
                selection.enabled = true;
                var rt = selection.rectTransform;
                var cible = (RectTransform)cases[_sel].transform;
                rt.SetParent(cible.parent, false);
                rt.SetSiblingIndex(0);
                rt.anchorMin = cible.anchorMin; rt.anchorMax = cible.anchorMax; rt.pivot = cible.pivot;
                rt.anchoredPosition = cible.anchoredPosition;
                rt.sizeDelta = cible.sizeDelta * 1.3f;
                Color c = catalogue ? catalogue.Couleur(CatalogueAmeliorations.Proprietaire(f.id)) : Color.white;
                selection.color = new Color(c.r, c.g, c.b, 0.38f);
            }
            bool passif = catalogue && catalogue.EstPassif(f.id);
            int prop = CatalogueAmeliorations.Proprietaire(f.id);
            if (ficheIcone) ficheIcone.Configure(f.id, 4);
            if (ficheNom) ficheNom.text = f.nom;
            if (ficheType) ficheType.text = (prop == 0 ? "Commune" : CatalogueAmeliorations.Personnages[prop]) + "  ·  " + (passif ? "passif (un seul porté à la fois)" : "amélioration");
            if (ficheEffet) ficheEffet.text = f.effet;
            for (int r = 0; r < ficheRangs.Length; r++)
                if (ficheRangs[r]) ficheRangs[r].text = f.rangs != null && r < f.rangs.Length ? f.rangs[r] : "";
            if (ficheSynergies) ficheSynergies.text = string.IsNullOrEmpty(f.synergies) ? "" : "Synergies : " + f.synergies;
        }
    }
}
