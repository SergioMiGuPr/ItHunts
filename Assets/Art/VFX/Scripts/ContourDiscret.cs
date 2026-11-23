using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Contour discret d'un ennemi (nuit 4, lot 7), collé à sa silhouette : remplace la boule orange de l'annonce du piqué des
    /// volants et la lueur violette du volant tenu. À poser sur la racine de l'ennemi. Au premier Montre(), une copie de chaque
    /// MeshRenderer (même maillage, matériau de coque inversée ItHunts/VFX/ContourCoque) est créée en enfant ; pour un
    /// SkinnedMeshRenderer, le matériau est ajouté en dernier (il cerne le dernier sous-maillage).
    /// Utilisation (main) : contour.Montre(contour.annonce) pendant FlyerMode.Annonce ; contour.Montre(contour.prise) tant
    /// que l'ennemi est tenu ; contour.Cache() sinon. Les rendus dont le nom commence par un préfixe de « ignorer » (par
    /// défaut « Annonce ») ne sont pas cernés.
    /// </summary>
    public sealed class ContourDiscret : MonoBehaviour
    {
        [Tooltip("Annonce du piqué : encre rouge sang éteint, pulsée.")] public Material annonce;
        [Tooltip("Ennemi tenu par la télékinésie : ton d'os neutre, fixe.")] public Material prise;
        public string[] ignorer = { "Annonce" };

        readonly List<MeshRenderer> _coques = new List<MeshRenderer>();
        readonly List<SkinnedMeshRenderer> _peaux = new List<SkinnedMeshRenderer>();
        readonly List<Material[]> _peauxOrigine = new List<Material[]>();
        Material _actuel;
        bool _pret;

        public Material Actuel => _actuel;

        void Prepare()
        {
            if (_pret) return;
            _pret = true;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (Ignore(r.name)) continue;
                if (r is MeshRenderer mr)
                {
                    var mf = mr.GetComponent<MeshFilter>();
                    if (mf == null || mf.sharedMesh == null) continue;
                    var go = new GameObject("Contour");
                    go.transform.SetParent(mr.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    var c = go.AddComponent<MeshRenderer>();
                    c.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    c.receiveShadows = false;
                    c.enabled = false;
                    _coques.Add(c);
                }
                else if (r is SkinnedMeshRenderer sr)
                {
                    _peaux.Add(sr);
                    _peauxOrigine.Add(sr.sharedMaterials);
                }
            }
        }

        bool Ignore(string n)
        {
            if (n == "Contour") return true;
            foreach (var p in ignorer) if (!string.IsNullOrEmpty(p) && n.StartsWith(p)) return true;
            return false;
        }

        /// <summary>Affiche le contour avec ce matériau (annonce ou prise) ; null : cache.</summary>
        public void Montre(Material m)
        {
            if (m == _actuel) return;
            if (m == null) { Cache(); return; }
            Prepare();
            _actuel = m;
            foreach (var c in _coques) if (c != null) { c.sharedMaterial = m; c.enabled = true; }
            for (int i = 0; i < _peaux.Count; i++)
            {
                if (_peaux[i] == null) continue;
                var o = _peauxOrigine[i];
                var mats = new Material[o.Length + 1];
                o.CopyTo(mats, 0); mats[o.Length] = m;
                _peaux[i].sharedMaterials = mats;
            }
        }

        public void Cache()
        {
            if (_actuel == null) return;
            _actuel = null;
            foreach (var c in _coques) if (c != null) c.enabled = false;
            for (int i = 0; i < _peaux.Count; i++) if (_peaux[i] != null) _peaux[i].sharedMaterials = _peauxOrigine[i];
        }

        void OnDisable() => Cache();
    }
}
