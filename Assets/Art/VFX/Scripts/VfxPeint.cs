using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Réglages exposés d'un effet peint (racine de chaque prefab VFX_*) : teinte, intensité des tons clairs, échelle, vitesse.
    /// Appliqués aux systèmes de particules et aux lignes de l'effet par bloc de propriétés (les matériaux partagés ne
    /// changent pas). Joue() relance l'effet (effets ponctuels) ; detruireALaFin pour les instances jetables.
    /// </summary>
    [ExecuteAlways]
    public sealed class VfxPeint : MonoBehaviour
    {
        [Tooltip("Teinte multipliée aux tons (blanc = couleurs du matériau).")]
        public Color teinte = Color.white;
        [Tooltip("Multiplie l'intensité des tons clairs (lueur par le bloom).")]
        [Range(0f, 4f)] public float intensite = 1f;
        [Tooltip("Échelle de l'effet (taille et vitesse des particules).")]
        [Range(0.1f, 10f)] public float echelle = 1f;
        [Tooltip("Vitesse de lecture (1 = normale).")]
        [Range(0.1f, 3f)] public float vitesse = 1f;
        [Tooltip("Effet ponctuel : détruit l'objet quand tout est fini (en jeu).")]
        public bool detruireALaFin;

        private static readonly int Ton1 = Shader.PropertyToID("_Ton1"), Ton2 = Shader.PropertyToID("_Ton2"), Ton3 = Shader.PropertyToID("_Ton3"), Int = Shader.PropertyToID("_Intensite");
        private MaterialPropertyBlock _mpb;
        private ParticleSystem[] _ps;

        private void OnEnable() => Applique();
        private void OnValidate() => Applique();

        public void Joue()
        {
            Applique();
            foreach (var p in GetComponentsInChildren<ParticleSystem>(true))
                if (p.transform.parent == null || p.transform.parent.GetComponentInParent<ParticleSystem>() == null)
                    p.Play(true);
        }

        public void Arrete() { foreach (var p in GetComponentsInChildren<ParticleSystem>(true)) p.Stop(true, ParticleSystemStopBehavior.StopEmitting); }

        public void Applique()
        {
            _mpb ??= new MaterialPropertyBlock();
            _ps = GetComponentsInChildren<ParticleSystem>(true);
            foreach (var p in _ps)
            {
                var m = p.main;
                m.simulationSpeed = vitesse;
            }
            transform.localScale = Vector3.one * echelle;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                var mat = r.sharedMaterial;
                if (mat == null || !mat.HasProperty(Ton1)) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(Ton1, mat.GetColor(Ton1) * teinte);
                _mpb.SetColor(Ton2, mat.GetColor(Ton2) * teinte);
                _mpb.SetColor(Ton3, mat.GetColor(Ton3) * teinte);
                _mpb.SetFloat(Int, mat.GetFloat(Int) * intensite);
                r.SetPropertyBlock(_mpb);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || !detruireALaFin || _ps == null)
                return;
            foreach (var p in _ps)
                if (p.IsAlive(false))
                    return;
            Destroy(gameObject);
        }
    }
}
