using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Orbe v2 (SM_Orbe de la DA) sous « Orbe (vue) » : remplace la sphère greybox (masquée). Couleur du personnage en permanence
    /// (D-066) sur l'énergie et les glyphes ; anneaux qui tournent sur leur axe (vite pendant la traction), énergie qui tourbillonne
    /// et pulse ; tablettes = jauge (2 traction, 1 répulsion, 3 décor « mouvement », MATERIAUX.md). Le flottement et la rotation
    /// d'ensemble au-dessus de la paume restent dans FpsArms (FpsViewSettings). Visuel seulement : rien de simulé ici.
    /// </summary>
    public sealed class OrbModelView : MonoBehaviour
    {
        [SerializeField] private OrbModelSettings settings;
        [Tooltip("Instance de SM_Orbe (enfant de « Orbe (vue) »).")]
        [SerializeField] private Transform model;
        [Tooltip("Sphère greybox de l'orbe : masquée quand le modèle est là.")]
        [SerializeField] private Renderer greybox;
        [SerializeField] private Transform ring1, ring2, ring3, energy;
        [SerializeField] private Renderer energyRenderer;
        [Tooltip("Tablettes 01 à 06 (glyphe : matériau d'index 1).")]
        [SerializeField] private Renderer[] tablets = new Renderer[6];
        [Tooltip("Racine de la couronne (pivot au centre) : les tablettes y sont rangées pour tourner ensemble.")]
        [SerializeField] private Transform crown;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private PlayerMotor _motor;
        private ItHunts.Artifacts.ArtifactHolder _holder;
        private MaterialPropertyBlock _block;
        private float _ringBoost = 1f;
        private float _orbDiameter = 0.08f;

        public OrbModelSettings Settings => settings;
        /// <summary>Le modèle est affiché (tests).</summary>
        public bool ShowsModel => model != null && model.gameObject.activeInHierarchy;
        /// <summary>Couleur appliquée à l'énergie (sans l'intensité ; tests).</summary>
        public Color CurrentColor { get; private set; }
        /// <summary>Tablettes allumées (tests).</summary>
        public int LitTablets { get; private set; }

        private void Awake()
        {
            _motor = GetComponentInParent<PlayerMotor>();
            _holder = GetComponentInParent<ItHunts.Artifacts.ArtifactHolder>();
            _block = new MaterialPropertyBlock();
            var view = GetComponentInParent<FirstPersonView>();
            if (view != null && view.Settings != null)
                _orbDiameter = Mathf.Max(0.001f, view.Settings.orbDiameter);
            bool on = model != null && settings != null;
            if (model != null)
                model.gameObject.SetActive(on);
            if (greybox != null)
                greybox.enabled = !on;                                  // garde le renderer : MagnetismOrbView y lit sa taille
        }

        private void LateUpdate()
        {
            if (model == null || settings == null)
                return;
            float dt = Time.deltaTime;
            // Le parent (« Orbe (vue) ») a l'échelle du diamètre de la greybox : le modèle garde sa taille réglée.
            model.localScale = Vector3.one * (settings.modelScale / _orbDiameter);

            bool traction = _motor != null && _motor.State.Mode == MovementMode.Traction;
            float target = traction ? settings.tractionRingBoost : 1f;
            _ringBoost = Mathf.Lerp(_ringBoost, target, 1f - Mathf.Exp(-settings.ringResponse * dt));
            Spin(ring1, Vector3.right, settings.ringSpeeds.x * _ringBoost * dt);
            Spin(ring2, Vector3.up, settings.ringSpeeds.y * _ringBoost * dt);
            Spin(ring3, Vector3.forward, settings.ringSpeeds.z * _ringBoost * dt);
            Spin(energy, settings.energySpinAxis.sqrMagnitude > 1e-6f ? settings.energySpinAxis.normalized : Vector3.up, settings.energySpin * dt);
            if (traction && crown != null)
                Spin(crown, Vector3.forward, settings.tractionCrownSpin * dt);

            CharacterDefinition c = _holder != null ? _holder.Character : null;
            Color color = c != null ? c.color : settings.fallbackColor;
            CurrentColor = color;
            float pulse = 1f + settings.energyPulse * Mathf.Sin(Time.time * settings.energyPulseHz * 2f * Mathf.PI);
            float boost = traction ? settings.tractionEnergyBoost : 1f;
            SetEmission(energyRenderer, 0, color * (settings.energyIntensity * pulse * boost));

            // Jauge : 02 et 03 = charges de traction, 04 = répulsion prête, 01, 05, 06 = « mouvement » (pas de charge en jeu : allumées).
            MagnetismState m = _motor != null ? _motor.Magnetism : default;
            float charges = _motor != null ? m.AttractCharges : 2f;
            bool repelReady = _motor == null || m.RepelStatus == OrbStatus.Prete;
            LitTablets = 0;
            for (int i = 0; i < tablets.Length; i++)
            {
                bool lit = i switch
                {
                    1 => charges >= 1f,
                    2 => charges >= 2f,
                    3 => repelReady,
                    _ => true,
                };
                if (lit)
                    LitTablets++;
                SetEmission(tablets[i], 1, lit ? color * settings.glyphIntensity : Color.black);
            }
        }

        private static void Spin(Transform t, Vector3 axis, float degrees)
        {
            if (t != null)
                t.localRotation *= Quaternion.AngleAxis(degrees, axis);
        }

        private void SetEmission(Renderer r, int materialIndex, Color c)
        {
            if (r == null || materialIndex >= r.sharedMaterials.Length)
                return;
            r.GetPropertyBlock(_block, materialIndex);
            _block.SetColor(EmissionColorId, c);
            r.SetPropertyBlock(_block, materialIndex);
        }
    }
}
