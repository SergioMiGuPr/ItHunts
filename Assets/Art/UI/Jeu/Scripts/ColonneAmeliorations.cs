using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// HUD (nuit 5, lot 2) : colonne verticale des améliorations possédées, **de bas en haut**, posée au-dessus de la barre de vie.
    /// Le passif (losange) en bas, puis les améliorations dans l'ordre d'obtention. Chaque case : illustration, cadre à la couleur
    /// du personnage, étoiles du rang. Une case qui change (nouvelle amélioration, montée de rang) grandit puis se pose (0,35 s)
    /// avec une lueur à la couleur du personnage. Survol (curseur libre) : infobulle. Remplace la rangée horizontale de main.
    /// </summary>
    public sealed class ColonneAmeliorations : MonoBehaviour
    {
        public CatalogueAmeliorations catalogue;
        [Tooltip("Cases de bas en haut (10).")]
        public IconeAmelioration[] cases = new IconeAmelioration[0];
        [Tooltip("Lueur derrière chaque case (même index), pour l'apparition.")]
        public Image[] lueurs = new Image[0];
        public InfobulleAmelioration infobulle;
        public float dureePop = 0.35f;

        private string[] _ids = new string[0];
        private int[] _rangs = new int[0];
        private float[] _pop = new float[0];

        public int Visibles { get; private set; }

        /// <summary>
        /// Textes de main pour l'infobulle : (code, rang) → { nom, effet, effet au rang } (UpgradeDefinition.displayName,
        /// description, RankText(rang)). Sans source, les fiches de la DA (catalogue) servent.
        /// </summary>
        public Func<string, int, string[]> SourceTextes;
        private int _survol = -1;

        /// <summary>ids et rangs (0 Bronze … 4 Mythique) ; le passif est mis en bas quel que soit son index.</summary>
        public void Remplit(string[] ids, int[] rangs)
        {
            ids = ids ?? Array.Empty<string>();
            int n = Mathf.Min(ids.Length, cases.Length);
            var ordreIds = new string[n];
            var ordreRangs = new int[n];
            int k = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < ids.Length && k < n; i++)
                {
                    bool passif = catalogue && catalogue.EstPassif(ids[i]);
                    if (passif != (pass == 0)) continue;
                    ordreIds[k] = ids[i];
                    ordreRangs[k] = rangs != null && i < rangs.Length ? rangs[i] : 0;
                    k++;
                }
            if (_pop.Length != cases.Length) _pop = new float[cases.Length];
            for (int i = 0; i < cases.Length; i++)
            {
                var c = cases[i];
                if (c == null) continue;
                if (i < n)
                {
                    bool change = i >= _ids.Length || _ids[i] != ordreIds[i] || _rangs[i] != ordreRangs[i];
                    c.Configure(ordreIds[i], ordreRangs[i]);
                    if (change && Application.isPlaying) _pop[i] = 1;
                    if (i < lueurs.Length && lueurs[i]) lueurs[i].color = Teinte(ordreIds[i], 0);
                }
                else
                {
                    c.Vide();
                    if (i < lueurs.Length && lueurs[i]) lueurs[i].color = Color.clear;
                }
            }
            _ids = ordreIds;
            _rangs = ordreRangs;
            Visibles = n;
        }

        private Color Teinte(string id, float a)
        {
            Color c = catalogue ? catalogue.Couleur(CatalogueAmeliorations.Proprietaire(id)) : Color.white;
            return new Color(c.r, c.g, c.b, a);
        }

        /// <summary>Infobulle de la case i (survol souris ou sélection à la manette dans la pause).</summary>
        public void Survole(int i)
        {
            _survol = i >= 0 && i < Visibles && i < cases.Length && cases[i] != null ? i : -1;
            if (infobulle == null) return;
            if (_survol < 0) { infobulle.Cache(); return; }
            var ancre = (RectTransform)cases[i].transform;
            var t = SourceTextes?.Invoke(_ids[i], _rangs[i]);
            if (t != null && t.Length >= 3) infobulle.Montre(_ids[i], _rangs[i], t[0], t[1], t[2], ancre);
            else infobulle.Montre(_ids[i], _rangs[i], ancre);
        }

        private void Update()
        {
            // case survolée ou sélectionnée à la manette : lueur douce à la couleur du personnage
            for (int i = 0; i < lueurs.Length && i < _ids.Length; i++)
                if (lueurs[i] && (i >= _pop.Length || _pop[i] <= 0))
                    lueurs[i].color = Teinte(_ids[i], i == _survol ? 0.4f : 0f);
            for (int i = 0; i < _pop.Length && i < cases.Length; i++)
            {
                if (_pop[i] <= 0 || cases[i] == null) continue;
                _pop[i] = Mathf.Max(0, _pop[i] - Time.unscaledDeltaTime / Mathf.Max(0.01f, dureePop));
                float t = _pop[i];
                cases[i].transform.localScale = Vector3.one * (1f + 0.22f * Mathf.Sin(t * Mathf.PI) * t);
                if (i < lueurs.Length && lueurs[i]) lueurs[i].color = Teinte(_ids[Mathf.Min(i, _ids.Length - 1)], 0.55f * t);
            }
        }
    }
}
