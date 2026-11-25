using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Retour visuel de la télékinésie (greybox), réglé par TelekinesisFeedbackSettings :
    /// - lueur des objets attrapables : faible de loin (repérables), plus forte à portée, vive et pulsée quand ils sont
    ///   sélectionnés (visés, ou pris par l'aide à la visée),
    ///   rouge terne s'ils sont trop lourds, cernés du contour discret de la DA quand ils sont tenus (jour 5), verte qui s'éteint après un lancer ;
    /// - trait de l'orbe à l'objet tenu : le trait fin de la DA (GameVfx_Default.heldTrait, VFX_Trait_Prise, nuit 5 lot 4) ; sans lui,
    ///   l'ancien trait violet greybox (la couleur de l'orbe elle-même : MagnetismOrbView, règle OrbLook).
    /// Sur la racine du joueur. Écran partagé (étape 9) : la lueur des objets est recalculée juste avant le rendu de la caméra
    /// de chaque joueur, chacun voit donc ce que lui vise, tient et peut attraper.
    /// </summary>
    public sealed class TelekinesisView : MonoBehaviour
    {
        [SerializeField] private TelekinesisFeedbackSettings settings;
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private LineRenderer line;

        private PlayerMotor _motor;
        private FirstPersonView _view;
        private Camera _camera;
        private ItHunts.Art.VFX.LienVfx _trait;
        private Grabbable _traitFor;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _view = GetComponent<FirstPersonView>();
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            _camera = playerInput != null ? playerInput.camera : null;
            if (line != null)
                line.enabled = false;
        }

        private void OnEnable() => RenderPipelineManager.beginCameraRendering += BeforeCamera;

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
            DropTrait();
        }

        private bool Ready => _motor != null && settings != null && _motor.TelekinesisSettings != null;

        private void LateUpdate()
        {
            if (!Ready)
                return;
            if (_camera == null)
                UpdateObjects(_motor.Telekinesis);                       // sans caméra (tests) : chaque image
            UpdateLine();
        }

        /// <summary>Avant la caméra de ce joueur : lueurs des objets vues par lui.</summary>
        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam != null && cam == _camera && Ready)
                UpdateObjects(_motor.Telekinesis);
        }

        private void UpdateObjects(in TelekinesisState t)
        {
            TelekinesisOrbSettings cfg = _motor.TelekinesisSettings;
            Vector3 eye = _camera != null
                ? _camera.transform.position                               // la caméra du joueur
                : transform.position + Vector3.up * 1.6f;
            foreach (Grabbable g in Grabbable.All)
            {
                // Ennemi (volant, goule) : jamais de surbrillance au survol (nuit 3, point 1) ni de violet quand il est tenu (nuit 4, point 9) :
                // sa lueur vient de Damageable (étourdi, coups) ; le débattement se lit sur son animation, pas sur sa couleur.
                if (g.IsEnemy)
                {
                    if (g.Id == t.HeldId && Time.time >= g.HeldSince + g.HoldDuration(cfg) - cfg.enemyStruggleWarning)
                        g.SetGlow(Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? Color.white * settings.enemyStruggleGlow : Color.black);   // il va se libérer : clignote blanc
                    else
                        g.SetGlow(Color.black);
                    continue;
                }

                Color glow;

                // Objet tenu (jour 5, point 4) : le contour discret de la DA au lieu de l'objet tout violet (sans matériau : l'ancienne lueur).
                bool held = g.Id == t.HeldId;
                bool outline = held && settings.heldOutline != null;
                g.ShowOutline(outline ? settings.heldOutline : null);
                float sinceThrow = Time.time - g.ThrownAt;
                if (held)
                    glow = settings.grabColor * (settings.glowHeld * (outline ? settings.heldGlowWithOutline : 1f));
                else if (sinceThrow < settings.thrownGlowDuration)
                    glow = settings.throwColor * Mathf.Lerp(settings.glowThrown, settings.glowIdle, sinceThrow / settings.thrownGlowDuration);
                else if (t.AimHit && t.AimId == g.Id)
                    glow = t.AimTooHeavy
                        ? settings.tooHeavyColor * settings.glowTooHeavy
                        : settings.grabColor * (settings.glowAimed *
                                                (1f + settings.aimedPulse * Mathf.Sin(Time.time * settings.aimedPulseFrequency * 2f * Mathf.PI)));
                else if (g.Mass > cfg.maxMass)
                    glow = settings.tooHeavyColor * settings.glowIdle;
                else if (Vector3.Distance(eye, g.Body.position) <= cfg.range)
                    glow = settings.grabColor * settings.glowInRange;
                else
                    glow = settings.grabColor * settings.glowIdle;
                g.SetGlow(glow);
            }
        }

        private void UpdateLine()
        {
            Grabbable held = _motor.Holder != null ? _motor.Holder.Held : null;
            Vector3 start = _view != null ? _view.LineStart : orbRenderer != null ? orbRenderer.transform.position : transform.position;

            // Trait de la DA (nuit 5, lot 4) : instancié à la prise, réglé chaque image en positions monde, détruit au lâcher ou au lancer.
            GameObject prefab = ItHunts.Combat.GameVfx.Settings != null ? ItHunts.Combat.GameVfx.Settings.heldTrait : null;
            if (held != _traitFor || held == null || prefab == null)
                DropTrait();
            if (held != null && prefab != null)
            {
                if (_trait == null)
                {
                    GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                    _trait = go.GetComponent<ItHunts.Art.VFX.LienVfx>();
                    if (_trait == null)
                        Destroy(go);
                    _traitFor = held;
                }

                if (_trait != null)
                {
                    _trait.Regle(start, held.transform.position);
                    if (line != null)
                        line.enabled = false;
                    return;
                }
            }

            if (line == null)
                return;
            bool show = held != null && orbRenderer != null;
            line.enabled = show;
            if (!show)
                return;
            line.SetPosition(0, _view != null ? _view.LineStart : orbRenderer.transform.position);
            line.SetPosition(1, held.transform.position);
            Color c = settings.grabColor * settings.lineIntensity;
            line.startColor = c;
            line.endColor = c;
        }

        private void DropTrait()
        {
            if (_trait != null)
                Destroy(_trait.gameObject);
            _trait = null;
            _traitFor = null;
        }

        private void OnDestroy() => DropTrait();
    }
}
