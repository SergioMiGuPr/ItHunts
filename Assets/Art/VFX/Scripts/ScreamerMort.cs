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
    /// grain faibles, vignette plus douce, ni sursaut ni rougeur. Durée totale 0,9 s (adoucie : 1,2 s). Le poids du volume suit
    /// la courbe « poids » : l'effet se mêle à l'étalonnage du jeu au lieu de l'écraser.
    /// Utilisation : un GameObject avec Volume (global, priorité haute) + ce composant ; le jeu appelle Joue(reduireEffets).
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public sealed class ScreamerMort : MonoBehaviour
    {
        public float duree = 0.9f, dureeReduite = 1.2f;
        [Tooltip("Poids du volume dans le temps (0..1 sur la durée).")]
        public AnimationCurve poids = new AnimationCurve(new Keyframe(0f, 0f, 0f, 14f), new Keyframe(0.08f, 1f), new Keyframe(1f, 1f));
        public Color vignette = new Color(0.16f, 0.01f, 0.02f);
        [Tooltip("Volume global (un joueur). En écran partagé : volume global sur une couche par joueur (volumeLayerMask de sa caméra), ou false et un collider de volume local autour de la caméra.")]
        public bool global = true;
        [Header("Aperçu (Play Mode seulement)")] [Range(0, 1)] public float apercu;
        public bool apercuReduit;

        Volume _v; VolumeProfile _p;
        Vignette _vig; ChromaticAberration _ab; FilmGrain _grain; LensDistortion _dist; ColorAdjustments _col;
        float _t = -1f; bool _reduit;

        public bool EnCours => _t >= 0f;

        void Awake()
        {
            if (!Application.isEditor) apercu = 0f;                       // un aperçu oublié dans un prefab ne part pas en build
            Prepare();
        }

        void Prepare()
        {
            if (_p != null) return;
            _v = GetComponent<Volume>();
            _v.isGlobal = global; _v.priority = 200f; _v.weight = 0f;
            _p = ScriptableObject.CreateInstance<VolumeProfile>();
            _p.name = "Screamer (mort)"; _p.hideFlags = HideFlags.DontSave;
            // seuls les paramètres réglés dans Pose sont forcés (les autres gardent l'étalonnage du jeu)
            _vig = _p.Add<Vignette>(); _ab = _p.Add<ChromaticAberration>(); _grain = _p.Add<FilmGrain>();
            _dist = _p.Add<LensDistortion>(); _col = _p.Add<ColorAdjustments>();
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
            k = Mathf.Clamp01(k);
            // le poids du volume suit la courbe : à 0 l'étalonnage du jeu (HorrorLook) reste intact, et l'effet s'y mêle en montant
            _v.weight = poids.Evaluate(k);
            float coup = reduit ? 0f : Mathf.Sin(Mathf.Clamp01(k / 0.35f) * Mathf.PI);           // distorsion : un coup qui relâche
            float sursaut = reduit ? 0f : Mathf.Sin(Mathf.Clamp01(k / 0.16f) * Mathf.PI);       // 0,15 s au début (version forte)
            float fin = Mathf.InverseLerp(0.5f, 1f, k);
            _vig.color.Override(vignette);
            _vig.intensity.Override(reduit ? 0.48f : 0.62f);
            _vig.smoothness.Override(reduit ? 0.7f : 0.45f);
            _ab.intensity.Override(reduit ? 0.25f : 1f);
            _grain.type.Override(FilmGrainLookup.Medium4);
            _grain.intensity.Override(reduit ? 0.25f : 0.8f);
            _grain.response.Override(0.6f);
            _dist.intensity.Override(-0.45f * coup);
            _dist.scale.Override(1f + 0.06f * coup);
            // image désaturée d'abord, puis teintée de rouge sang (pas de rouge sur un ciel bleu : il virerait au violet)
            _col.saturation.Override(-65f);
            _col.contrast.Override(28f * sursaut);
            _col.postExposure.Override(0.55f * sursaut - (reduit ? 1.2f : 1.8f) * fin);
            _col.colorFilter.Override(Color.Lerp(new Color(1f, 0.9f, 0.88f), new Color(1f, 0.5f, 0.42f), sursaut));
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

        /// <summary>Réglage qui pilote la version adoucie (main) : GameSettings.ReduceMotion (« Réduire les animations »).</summary>
        public const string ReglageAdouci = "GameSettings.ReduceMotion";

        void OnDestroy() { if (_p) { if (Application.isPlaying) Destroy(_p); else DestroyImmediate(_p); } }
    }
}
