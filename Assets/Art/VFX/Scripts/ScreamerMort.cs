using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Screamer de la mort par le traqueur (nuit 4, lot 7) : effet d'écran bref et puissant, dans la DA, par le post-traitement
    /// URP (aucun écran plein de couleur vive) : l'image se resserre (distorsion en barillet), se dédouble en rouge et bleu
    /// sur les bords (aberration chromatique), le grain monte, une vignette rouge sang presque noire se referme, puis tout
    /// retombe vers le noir de l'écran de fin. Version adoucie (option « réduire les effets ») : pas de distorsion, aberration et
    /// grain faibles, vignette plus lente, aucune secousse. Durée totale 0,9 s (adoucie : 1,2 s).
    /// Utilisation : un GameObject avec Volume (global, priorité haute) + ce composant ; le jeu appelle Joue(reduireEffets).
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class ScreamerMort : MonoBehaviour
    {
        public float duree = 0.9f, dureeReduite = 1.2f;
        [Tooltip("Poids du volume dans le temps (0..1 sur la durée).")]
        public AnimationCurve poids = new AnimationCurve(new Keyframe(0f, 0f, 0f, 12f), new Keyframe(0.12f, 1f), new Keyframe(0.55f, 0.85f), new Keyframe(1f, 1f));
        public Color vignette = new Color(0.16f, 0.01f, 0.02f);
        [Header("Aperçu")] [Range(0, 1)] public float apercu;
        public bool apercuReduit;

        Volume _v; VolumeProfile _p;
        Vignette _vig; ChromaticAberration _ab; FilmGrain _grain; LensDistortion _dist; ColorAdjustments _col;
        float _t = -1f; bool _reduit;

        public bool EnCours => _t >= 0f;

        void Awake() => Prepare();

        void Prepare()
        {
            if (_p != null) return;
            _v = GetComponent<Volume>();
            _v.isGlobal = true; _v.priority = 200f; _v.weight = 0f;
            _p = ScriptableObject.CreateInstance<VolumeProfile>();
            _p.name = "Screamer (mort)"; _p.hideFlags = HideFlags.DontSave;
            _vig = _p.Add<Vignette>(true); _ab = _p.Add<ChromaticAberration>(true); _grain = _p.Add<FilmGrain>(true);
            _dist = _p.Add<LensDistortion>(true); _col = _p.Add<ColorAdjustments>(true);
            _v.sharedProfile = _p;
        }

        /// <summary>Lance l'effet (mort par le traqueur).</summary>
        public void Joue(bool reduireEffets)
        {
            Prepare();
            _reduit = reduireEffets; _t = 0f;
        }

        /// <summary>État de l'effet à la phase k (0..1) : pour l'aperçu et les captures.</summary>
        public void Pose(float k, bool reduit)
        {
            Prepare();
            float w = poids.Evaluate(Mathf.Clamp01(k));
            _v.weight = 1f;
            _vig.color.Override(vignette);
            _vig.intensity.Override(Mathf.Lerp(0.15f, reduit ? 0.5f : 0.62f, w));
            _vig.smoothness.Override(reduit ? 0.75f : 0.55f);
            _ab.intensity.Override(w * (reduit ? 0.25f : 1f));
            _grain.type.Override(FilmGrainLookup.Medium4);
            _grain.intensity.Override(w * (reduit ? 0.25f : 0.85f));
            _grain.response.Override(0.6f);
            // distorsion : un coup en barillet puis relâche (absente en version adoucie)
            float coup = reduit ? 0f : Mathf.Sin(Mathf.Clamp01(k / 0.35f) * Mathf.PI) * -0.42f;
            _dist.intensity.Override(coup);
            _dist.scale.Override(1f + (reduit ? 0f : 0.06f * Mathf.Sin(Mathf.Clamp01(k / 0.35f) * Mathf.PI)));
            // l'image se vide de couleur et s'assombrit vers la fin (enchaîne avec l'écran de fin)
            // sursaut (version forte) : l'image s'éclaire et rougit d'un coup pendant 0,15 s, puis se vide de couleur et
            // s'assombrit vers la fin (enchaîne avec l'écran de fin) ; la version adoucie n'a ni sursaut ni rougeur
            float sursaut = reduit ? 0f : Mathf.Sin(Mathf.Clamp01(k / 0.16f) * Mathf.PI);
            _col.saturation.Override(Mathf.Lerp(0f, -70f, w) + 50f * sursaut);
            _col.postExposure.Override(0.9f * sursaut + Mathf.Lerp(0f, reduit ? -1.2f : -1.8f, Mathf.InverseLerp(0.5f, 1f, k)));
            _col.colorFilter.Override(Color.Lerp(Color.Lerp(Color.white, new Color(1f, 0.82f, 0.82f), w), new Color(1f, 0.42f, 0.38f), sursaut));
            _col.contrast.Override(reduit ? 0f : 35f * sursaut);
        }

        void Update()
        {
            if (_t < 0f)
            {
                if (apercu > 0f) Pose(apercu, apercuReduit);      // réglage dans l'inspecteur ; sinon l'état reste tel quel
                return;
            }
            _t += Time.unscaledDeltaTime;
            float d = _reduit ? dureeReduite : duree;
            Pose(_t / d, _reduit);
            if (_t >= d) _t = -1f;       // l'état final reste (écran de fin) jusqu'au prochain Arrete()
        }

        /// <summary>Retour à l'image normale (nouvelle run).</summary>
        public void Arrete() { _t = -1f; apercu = 0f; if (_v) _v.weight = 0f; }

        void OnDestroy() { if (_p) { if (Application.isPlaying) Destroy(_p); else DestroyImmediate(_p); } }
    }
}
