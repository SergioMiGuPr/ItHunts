using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Retour visuel de la télékinésie (greybox), réglé par TelekinesisFeedbackSettings :
    /// - lueur des objets attrapables : faible de loin (repérables), plus forte à portée, vive quand ils sont visés,
    ///   rouge terne s'ils sont trop lourds, violette quand ils sont tenus, verte qui s'éteint après un lancer ;
    /// - orbe en bas à droite (télékinésie active) : violette prête ou en maintien, verte au lancer, terne en recharge ;
    /// - trait violet de l'orbe à l'objet tenu.
    /// Sur la racine du joueur. En écran partagé, la lueur des objets suivra le joueur 1 (à revoir à l'étape 9).
    /// </summary>
    public sealed class TelekinesisView : MonoBehaviour
    {
        [SerializeField] private TelekinesisFeedbackSettings settings;
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private LineRenderer line;
        [SerializeField, Min(0f)] private float throwFlashDuration = 0.15f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private PlayerMotor _motor;
        private MaterialPropertyBlock _block;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _block = new MaterialPropertyBlock();
            if (line != null)
                line.enabled = false;
        }

        private void LateUpdate()
        {
            if (_motor == null || settings == null || _motor.TelekinesisSettings == null)
                return;

            TelekinesisState t = _motor.Telekinesis;
            bool active = _motor.ActiveOrb == ActiveOrb.Telekinesie;
            UpdateObjects(t, active);
            UpdateOrb(t, active);
            UpdateLine(t, active);
        }

        private void UpdateObjects(in TelekinesisState t, bool active)
        {
            TelekinesisOrbSettings cfg = _motor.TelekinesisSettings;
            Vector3 eye = orbRenderer != null && orbRenderer.transform.parent != null
                ? orbRenderer.transform.parent.position                    // la caméra du joueur
                : transform.position + Vector3.up * 1.6f;
            foreach (Grabbable g in Grabbable.All)
            {
                Color glow;
                float sinceThrow = Time.time - g.ThrownAt;
                if (g.Id == t.HeldId)
                    glow = settings.grabColor * settings.glowHeld;
                else if (sinceThrow < settings.thrownGlowDuration)
                    glow = settings.throwColor * Mathf.Lerp(settings.glowThrown, settings.glowIdle, sinceThrow / settings.thrownGlowDuration);
                else if (active && t.AimHit && t.AimId == g.Id)
                    glow = t.AimTooHeavy ? settings.tooHeavyColor * settings.glowTooHeavy : settings.grabColor * settings.glowAimed;
                else if (g.Mass > cfg.maxMass)
                    glow = settings.tooHeavyColor * settings.glowIdle;
                else if (active && Vector3.Distance(eye, g.Body.position) <= cfg.range)
                    glow = settings.grabColor * settings.glowInRange;
                else
                    glow = settings.grabColor * settings.glowIdle;
                g.SetGlow(glow);
            }
        }

        private void UpdateOrb(in TelekinesisState t, bool active)
        {
            if (!active || orbRenderer == null)
                return;
            Color c;
            bool justThrown = t.LastAction == TelekinesisCommandKind.Lancer
                              && (_motor.State.Tick - t.LastActionTick) * Time.fixedDeltaTime < throwFlashDuration;
            if (justThrown)
                c = settings.throwColor * settings.orbUsageIntensity;
            else if (t.Status == OrbStatus.EnUsage)
                c = settings.grabColor * settings.orbUsageIntensity;
            else if (t.Status == OrbStatus.Recharge)
                c = Color.Lerp(new Color(0.16f, 0.16f, 0.18f), settings.grabColor, 0.2f);
            else
                c = settings.grabColor * settings.orbReadyIntensity;
            _block.SetColor(BaseColorId, c);
            orbRenderer.SetPropertyBlock(_block);
        }

        private void UpdateLine(in TelekinesisState t, bool active)
        {
            if (line == null)
                return;
            Grabbable held = _motor.Holder != null ? _motor.Holder.Held : null;
            bool show = active && held != null && orbRenderer != null;
            line.enabled = show;
            if (!show)
                return;
            line.SetPosition(0, orbRenderer.transform.position);
            line.SetPosition(1, held.transform.position);
            Color c = settings.grabColor * settings.lineIntensity;
            line.startColor = c;
            line.endColor = c;
        }
    }
}
