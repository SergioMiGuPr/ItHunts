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
    ///   rouge terne s'ils sont trop lourds, violette quand ils sont tenus, verte qui s'éteint après un lancer ;
    /// - trait violet de l'orbe à l'objet tenu (la couleur de l'orbe elle-même : MagnetismOrbView, règle OrbLook).
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

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= BeforeCamera;

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
                // Ennemi (volant, goule) : jamais de surbrillance au survol de la visée (nuit 3, point 1) ; sa lueur vient de Damageable
                // (étourdi, coups), et la couleur de prise seulement quand il est tenu.
                if (g.IsEnemy && g.Id != t.HeldId)
                    continue;
                Color glow;
                if (g.IsEnemy && g.Id == t.HeldId
                    && Time.time >= g.HeldSince + g.HoldDuration(cfg) - cfg.enemyStruggleWarning)
                {
                    // Il va se libérer : il clignote (violet / blanc).
                    bool on = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
                    g.SetGlow(on ? Color.white * settings.glowHeld * 2f : settings.grabColor * settings.glowHeld);
                    continue;
                }

                float sinceThrow = Time.time - g.ThrownAt;
                if (g.Id == t.HeldId)
                    glow = settings.grabColor * settings.glowHeld;
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
            if (line == null)
                return;
            Grabbable held = _motor.Holder != null ? _motor.Holder.Held : null;
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
    }
}
