using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Flux de particules d'une source vers une cible (traction du magnétisme : fumée et éclats bleus qui filent de l'orbe
    /// vers le point visé, pendant toute la traction ; remplace le trait). Chaque particule suit le trajet selon son âge :
    /// elle naît à la source et arrive à la cible à la fin de sa vie, en tournant légèrement autour de l'axe (spirale). Le
    /// trajet suit la source et la cible à chaque image (l'orbe bouge avec le joueur). Ouvre / Ferme : début et fin du flux
    /// (les particules déjà parties finissent leur course).
    /// Le jeu donne la source (transform de l'orbe) et la cible (Regle(point) chaque image, ou un transform).
    /// </summary>
    [ExecuteAlways]
    public sealed class FluxVfx : MonoBehaviour
    {
        [Tooltip("Départ du flux (l'orbe). Vide : pointSource, dans le repère de cet objet.")]
        public Transform source;
        [Tooltip("Arrivée du flux. Vide : pointCible (monde, posé par Regle).")]
        public Transform cible;
        public Vector3 pointSource = Vector3.zero;
        public Vector3 pointCible = new Vector3(0, 0, 8);
        [Tooltip("Vitesse des particules le long du trajet (m/s).")]
        [Min(0.5f)] public float vitesse = 16f;
        [Tooltip("Particules émises par seconde et par mètre de trajet, pour chaque système (multiplié par son 'poids').")]
        [Min(0f)] public float densite = 2.2f;
        [Tooltip("Rayon de la spirale autour de l'axe (m) au milieu du trajet, et nombre de tours sur le trajet.")]
        public Vector2 spirale = new Vector2(0.07f, 1.5f);
        [Tooltip("Taille des particules à la source (fraction de leur taille) : l'orbe est tout près de la caméra.")]
        [Range(0.05f, 1f)] public float tailleSource = 0.25f;
        [Tooltip("Débit maximal par système (particules par seconde).")]
        [Min(1f)] public float debitMax = 160f;
        [Tooltip("Systèmes du flux et leur poids de débit (1 = densite).")]
        public ParticleSystem[] systemes = new ParticleSystem[0];
        public float[] poids = new float[0];

        private bool _ouvert = true;
        private ParticleSystem.Particle[] _tampon;

        /// <summary>Point visé (monde) ; la source reste le transform 'source'.</summary>
        public void Regle(Vector3 cibleMonde) { pointCible = cibleMonde; cible = null; }
        /// <summary>Source et cible en coordonnées monde (sans transform).</summary>
        public void Regle(Vector3 sourceMonde, Vector3 cibleMonde) { source = null; pointSource = transform.InverseTransformPoint(sourceMonde); Regle(cibleMonde); }
        public void Ouvre() { _ouvert = true; foreach (var p in systemes) if (p != null && !p.isPlaying) p.Play(true); }
        public void Ferme() => _ouvert = false;

        private Vector3 A => source != null ? source.position : transform.TransformPoint(pointSource);
        private Vector3 B => cible != null ? cible.position : pointCible;

        private void OnEnable() => RegleTailles();
        private void OnValidate() => RegleTailles();

        private void RegleTailles()
        {
            // taille au fil du trajet : petite près de l'orbe (près de la caméra), pleine ensuite
            var c = new AnimationCurve(new Keyframe(0f, tailleSource), new Keyframe(0.35f, 1f), new Keyframe(1f, 0.8f));
            foreach (var ps in systemes)
            {
                if (ps == null) continue;
                var sz = ps.sizeOverLifetime;
                sz.enabled = true;
                sz.size = new ParticleSystem.MinMaxCurve(1f, c);
            }
        }

        private void LateUpdate() => Pose();

        /// <summary>Place les particules sur le trajet (appelé à chaque image ; aussi après une simulation forcée).</summary>
        public void Pose()
        {
            Vector3 a = A, b = B;
            float L = Vector3.Distance(a, b);
            Vector3 dir = L > 1e-4f ? (b - a) / L : Vector3.forward;
            Vector3 u = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.95f ? Vector3.right : Vector3.up).normalized;
            Vector3 v = Vector3.Cross(dir, u);
            float vie = Mathf.Max(L / Mathf.Max(vitesse, 0.5f), 0.05f);
            for (int k = 0; k < systemes.Length; k++)
            {
                var ps = systemes[k];
                if (ps == null) continue;
                var main = ps.main;
                main.startLifetime = vie;
                var em = ps.emission;
                float w = k < poids.Length ? poids[k] : 1f;
                em.rateOverTime = _ouvert ? Mathf.Min(densite * w * L, debitMax) : 0f;
                int n = ps.particleCount;
                if (n == 0) continue;
                if (_tampon == null || _tampon.Length < n) _tampon = new ParticleSystem.Particle[Mathf.Max(n, main.maxParticles)];
                n = ps.GetParticles(_tampon, n);
                for (int i = 0; i < n; i++)
                {
                    ref var p = ref _tampon[i];
                    float t = 1f - p.remainingLifetime / Mathf.Max(p.startLifetime, 1e-4f);   // 0 à la source, 1 à la cible
                    float graine = (p.randomSeed % 1000) / 1000f;
                    float env = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                    float ang = (graine + t * spirale.y) * Mathf.PI * 2f;
                    float r = spirale.x * env * (0.5f + graine);
                    p.position = Vector3.LerpUnclamped(a, b, t) + (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * r;
                    p.velocity = dir * vitesse;      // pour l'étirement des éclats
                }
                ps.SetParticles(_tampon, n);
            }
        }
    }
}
