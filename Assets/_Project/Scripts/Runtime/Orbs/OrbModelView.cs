using System.Collections.Generic;
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
        [Tooltip("Tablette seule de la DA (SM_Orbe_Tablette, PR #12) : pivot au centre, X vers l'extérieur ; vide : copies des gabarits.")]
        [SerializeField] private GameObject tabletPrefab;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        private static readonly int SpinBoostId = Shader.PropertyToID("_SpinBoost");
        private float _spinBoost = 1f;
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
        /// <summary>Tablettes allumées et tablettes affichées (tests).</summary>
        public int LitTablets { get; private set; }
        public int TabletCount => _attractTabs.Count + _repelTabs.Count;

        /// <summary>Angle de chaque tablette du modèle dans la couronne (°, depuis +X ; MATERIAUX.md) : les clones tournent autour de Z.</summary>
        private static readonly float[] TemplateAngles = { 0f, 40f, 140f, 180f, 220f, 320f };

        private sealed class Tablet
        {
            public Transform Transform;
            public Renderer Renderer;
            public float BaseAngle, Angle, Target, Born;
            public bool Single;                                         // tablette seule : posée sur le cercle par son angle
        }

        private readonly List<Tablet> _attractTabs = new List<Tablet>();
        private readonly List<Tablet> _repelTabs = new List<Tablet>();

        /// <summary>
        /// Angles des tablettes (D-069) : une par charge de traction puis une par charge de répulsion, réparties régulièrement ; la
        /// dernière (répulsion) en bas (270°), les autres dans le sens trigonométrique à partir d'elle.
        /// </summary>
        public static float[] TabletAngles(int attract, int repel)
        {
            int n = Mathf.Max(0, attract) + Mathf.Max(0, repel);
            var a = new float[n];
            for (int i = 0; i < n; i++)
                a[i] = Mathf.Repeat(270f + (i + 1) * 360f / n, 360f);
            return a;
        }

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
            // Les 6 tablettes du modèle servent de gabarits (D-069) : la jauge en crée une par charge.
            foreach (Renderer t in tablets)
                if (t != null)
                    t.gameObject.SetActive(false);
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
            // Tourbillon (ItHunts/OrbeVortex, PR #12) : il accélère pendant la traction.
            _spinBoost = Mathf.Lerp(_spinBoost, traction ? settings.vortexTractionBoost : 1f, 1f - Mathf.Exp(-settings.ringResponse * dt));
            if (energyRenderer != null)
            {
                energyRenderer.GetPropertyBlock(_block, 0);
                _block.SetFloat(SpinBoostId, _spinBoost);
                energyRenderer.SetPropertyBlock(_block, 0);
            }

            // Jauge (D-069) : une tablette par charge de traction et de répulsion ; utilisée = rune éteinte, rechargée = rallumée ; une
            // amélioration qui augmente le maximum fait apparaître une tablette. Les charges de mouvement restent sous le réticule.
            MagnetismState m = _motor != null ? _motor.Magnetism : default;
            int attractMax = _motor != null && _motor.MagnetismSettings != null ? _motor.MagnetismSettings.attractCharges : 2;
            float charges = _motor != null ? (m.AttractCharges < 0f ? attractMax : m.AttractCharges) : attractMax;
            bool repelReady = _motor == null || m.RepelStatus == OrbStatus.Prete;
            SyncTablets(attractMax, 1, dt);
            LitTablets = 0;
            for (int i = 0; i < _attractTabs.Count; i++)
                LightTablet(_attractTabs[i], charges >= i + 1f, color);
            foreach (Tablet t in _repelTabs)
                LightTablet(t, repelReady, color);
        }

        private void LightTablet(Tablet t, bool lit, Color color)
        {
            if (lit)
                LitTablets++;
            SetEmission(t.Renderer, 1, lit ? color * settings.glyphIntensity : Color.black);
        }

        private void SyncTablets(int attract, int repel, float dt)
        {
            if (crown == null)
                return;
            float now = Time.time;
            Resize(_attractTabs, attract, new[] { 1, 2 }, now);           // gabarits 02 et 03 (glyphes de la traction)
            Resize(_repelTabs, repel, new[] { 3 }, now);                  // gabarit 04 (glyphe de la répulsion)
            float[] angles = TabletAngles(_attractTabs.Count, _repelTabs.Count);
            int k = 0;
            foreach (Tablet t in _attractTabs)
                t.Target = angles[k++];
            foreach (Tablet t in _repelTabs)
                t.Target = angles[k++];
            foreach (Tablet t in _attractTabs)
                Place(t, now, dt);
            foreach (Tablet t in _repelTabs)
                Place(t, now, dt);
        }

        private void Resize(List<Tablet> list, int count, int[] templates, float now)
        {
            while (list.Count > count)
            {
                Tablet last = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                if (last.Transform != null)
                    Destroy(last.Transform.gameObject);
            }

            while (list.Count < count)
            {
                int ti = templates[list.Count % templates.Length];
                Renderer template = ti < tablets.Length ? tablets[ti] : null;
                bool single = tabletPrefab != null;
                if (template == null && !single)
                    return;
                GameObject go = Instantiate(single ? tabletPrefab : template.gameObject, crown, false);
                go.layer = crown.gameObject.layer;
                foreach (Transform c in go.GetComponentsInChildren<Transform>(true))
                    c.gameObject.layer = crown.gameObject.layer;
                go.name = $"Tablette {list.Count + 1} ({(templates.Length == 1 ? "répulsion" : "traction")})";
                go.SetActive(true);
                float baseAngle = TemplateAngles[ti];
                // Une tablette ajoutée en cours de partie apparaît en grandissant ; celles du départ sont là d'emblée.
                bool first = Time.timeSinceLevelLoad < 0.5f;
                Renderer rend = go.GetComponentInChildren<Renderer>();
                if (single && rend != null)
                {
                    // Glyphe du gabarit (0 à 5) : décalage d'UV de l'atlas sur le matériau d'index 1 (rune), l'émission suit.
                    rend.GetPropertyBlock(_block, 1);
                    _block.SetVector(BaseMapStId, new Vector4(1f, 1f, (ti % 3) / 3f, -(ti / 3) / 2f));
                    rend.SetPropertyBlock(_block, 1);
                    if (template != null && rend.sharedMaterials.Length < template.sharedMaterials.Length)
                        rend.sharedMaterials = template.sharedMaterials.Length > 2
                            ? new[] { rend.sharedMaterials[0], rend.sharedMaterials[1], template.sharedMaterials[2] } : rend.sharedMaterials;   // contour
                }

                list.Add(new Tablet { Transform = go.transform, Renderer = rend, BaseAngle = single ? 0f : baseAngle, Single = single,
                    Angle = list.Count > 0 ? list[list.Count - 1].Angle : baseAngle, Born = first ? float.NegativeInfinity : now });
            }
        }

        private void Place(Tablet t, float now, float dt)
        {
            t.Angle = Mathf.MoveTowardsAngle(t.Angle, t.Target, settings.tabletSlideSpeed * dt);
            t.Transform.localRotation = Quaternion.Euler(0f, 0f, t.Angle - t.BaseAngle);
            if (t.Single)
                t.Transform.localPosition = new Vector3(Mathf.Cos(t.Angle * Mathf.Deg2Rad), Mathf.Sin(t.Angle * Mathf.Deg2Rad), 0f) * settings.crownRadius;
            float u = settings.tabletAppearSeconds > 0f ? Mathf.Clamp01((now - t.Born) / settings.tabletAppearSeconds) : 1f;
            // Apparition : de 0 à 1 avec un léger dépassement (sortie franche, MATERIAUX.md).
            float c = 1.7f, e = u - 1f;
            float scale = u >= 1f ? 1f : 1f + (c + 1f) * e * e * e + c * e * e;
            t.Transform.localScale = Vector3.one * Mathf.Max(0f, scale);
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
