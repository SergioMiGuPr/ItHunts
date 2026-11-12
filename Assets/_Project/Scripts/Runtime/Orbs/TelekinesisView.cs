using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Retour visuel de la télékinésie (greybox), réglé par TelekinesisFeedbackSettings :
    /// - lueur des objets attrapables : faible de loin (repérables), plus forte à portée, vive et pulsée quand ils sont
    ///   sélectionnés (visés, ou pris par l'aide à la visée),
    ///   rouge terne s'ils sont trop lourds, violette quand ils sont tenus, verte qui s'éteint après un lancer ;
    /// - trait violet de l'orbe à l'objet tenu (la couleur de l'orbe elle-même : MagnetismOrbView, règle OrbLook).
    /// Sur la racine du joueur. En écran partagé, la lueur des objets suivra le joueur 1 (à revoir à l'étape 9).
    /// </summary>
    public sealed class TelekinesisView : MonoBehaviour
    {
        [SerializeField] private TelekinesisFeedbackSettings settings;
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private LineRenderer line;

        private PlayerMotor _motor;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            if (line != null)
                line.enabled = false;
        }

        private void LateUpdate()
        {
            if (_motor == null || settings == null || _motor.TelekinesisSettings == null)
                return;

            TelekinesisState t = _motor.Telekinesis;
            UpdateObjects(t);
            UpdateLine();
        }

        private void UpdateObjects(in TelekinesisState t)
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
            line.SetPosition(0, orbRenderer.transform.position);
            line.SetPosition(1, held.transform.position);
            Color c = settings.grabColor * settings.lineIntensity;
            line.startColor = c;
            line.endColor = c;
        }
    }
}
