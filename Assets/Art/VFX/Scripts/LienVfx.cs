using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Lien du livre du hibou (drain rouge sang vers un ennemi, soin bleu-vert vers un allié) : une bande peinte ondulée
    /// (LineRenderer, texture raccordable qui défile) entre deux points, légèrement creusée et qui ondule. Le jeu donne
    /// source et cible (transforms ou positions) ; Ouvre / Ferme font naître et se défaire le lien (dissolution).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LienVfx : MonoBehaviour
    {
        public Transform source, cible;
        public Vector3 pointSource = new Vector3(-1.5f, 0, 0), pointCible = new Vector3(1.5f, 0, 0);
        [Tooltip("Nombre de points de la ligne.")]
        [Range(4, 64)] public int points = 24;
        [Tooltip("Creux au milieu (m) et ondulation latérale (m, cycles par seconde).")]
        public float creux = 0.15f;
        public Vector2 ondulation = new Vector2(0.08f, 1.3f);
        [Tooltip("Largeur (m).")]
        public float largeur = 0.18f;
        [Tooltip("Mètres de lien par répétition de la texture.")]
        public float pas = 0.9f;
        [Tooltip("Sens du flux : 1 = de la source vers la cible (soin), -1 = de la cible vers la source (drain).")]
        public float sens = 1f;
        [Tooltip("Vitesse du flux (m/s).")]
        public float vitesse = 2.2f;
        [Tooltip("Durée d'ouverture et de fermeture (s).")]
        public float fondu = 0.2f;

        private LineRenderer _l;
        private float _ouvert = 1f, _cibleOuvert = 1f, _phase;

        public void Regle(Vector3 a, Vector3 b) { pointSource = a; pointCible = b; source = null; cible = null; }
        public void Ouvre() => _cibleOuvert = 1f;
        public void Ferme() => _cibleOuvert = 0f;

        private void OnEnable() { _l = GetComponent<LineRenderer>(); _l.useWorldSpace = true; Met(0f); }

        private void Update() => Met(Application.isPlaying ? Time.deltaTime : 0f);

        private void Met(float dt)
        {
            if (_l == null) return;
            _ouvert = Mathf.MoveTowards(_ouvert, _cibleOuvert, dt / Mathf.Max(fondu, 0.01f));
            _phase += dt * vitesse / Mathf.Max(pas, 0.01f) * -sens;
            Vector3 a = source != null ? source.position : transform.TransformPoint(pointSource);
            Vector3 b = cible != null ? cible.position : transform.TransformPoint(pointCible);
            float L = Vector3.Distance(a, b);
            Vector3 dir = (b - a) / Mathf.Max(L, 1e-4f);
            Vector3 lat = Vector3.Cross(dir, Vector3.up);
            if (lat.sqrMagnitude < 1e-4f) lat = Vector3.right;
            lat.Normalize();
            float t0 = Application.isPlaying ? Time.time : 0f;
            _l.positionCount = points;
            for (int i = 0; i < points; i++)
            {
                float u = i / (points - 1f);
                float env = Mathf.Sin(u * Mathf.PI);
                Vector3 p = Vector3.Lerp(a, b, u) + Vector3.down * creux * env * Mathf.Min(L / 4f, 1f)
                          + lat * ondulation.x * env * Mathf.Sin((u * 3f - t0 * ondulation.y) * Mathf.PI * 2f);
                _l.SetPosition(i, p);
            }
            _l.widthMultiplier = largeur;
            _l.textureMode = LineTextureMode.Tile;
            _l.textureScale = new Vector2(1f / Mathf.Max(pas, 0.01f), 1f);
            var m = _l.sharedMaterial;
            if (m != null && Application.isPlaying)
                _l.material.mainTextureOffset = new Vector2(_phase, 0);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(_ouvert, 0), new GradientAlphaKey(_ouvert, 1) });
            _l.colorGradient = g;
            _l.enabled = _ouvert > 0.001f;
        }
    }
}
