using System.Collections;
using System.Collections.Generic;
using System.Text;
using ItHunts.Enemies;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Mesure de la parade (nuit 3, point 7) contre une vraie goule : le joueur est posé devant elle ; à chaque coup, la sonde appuie
    /// sur « parer » à un décalage donné de l'impact réel (début du geste + contactImpactDelay) et note : réussie ou non, dégâts
    /// reçus, goule étourdie, invulnérabilité. Outil de développement (mesures par la CLI), jamais dans une partie.
    /// </summary>
    public sealed class ParryProbe : MonoBehaviour
    {
        public static string Report { get; private set; } = "";
        public static bool Done { get; private set; }

        private float[] _offsets;

        public static ParryProbe Run(params float[] offsetsFromImpact)
        {
            Done = false;
            Report = "";
            var p = new GameObject("Sonde de parade").AddComponent<ParryProbe>();
            p._offsets = offsetsFromImpact;
            p.StartCoroutine(p.Measure());
            return p;
        }

        private IEnumerator Measure()
        {
            var sb = new StringBuilder();
            PlayerHealth player = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0] : null;
            var parry = player != null ? player.GetComponent<PlayerParry>() : null;
            if (player == null || parry == null)
            {
                Report = "pas de joueur";
                Done = true;
                yield break;
            }

            // Hors de la salle de départ et timer lancé (sinon les goules dorment).
            var zone = FindFirstObjectByType<ZoneState>();
            if (zone != null && !zone.Started)
                zone.DebugAdvance(0.01f);
            var level = ItHunts.Level.Acts.ActLevel.Current;
            if (level != null && level.Plan != null)
                foreach (ItHunts.Level.Acts.ActRoom r in level.Plan.Rooms)
                    if (r.Role == ItHunts.Level.Acts.RoomRole.Normale && r.Feature == ItHunts.Level.Acts.RoomFeature.Aucun)
                    {
                        player.Motor.Teleport(level.Plan.CellCenter(r.CenterCell, r.Floor) + Vector3.up * 0.2f);
                        break;
                    }

            yield return new WaitForSeconds(0.5f);
            foreach (float offset in _offsets)
            {
                // Une goule vivante, posée à 1.2 m devant le joueur, qui le cible.
                Ghoul ghoul = null;
                foreach (Ghoul g in FindObjectsByType<Ghoul>(FindObjectsSortMode.None))
                    if (g.isActiveAndEnabled && g.GetComponent<ItHunts.Combat.Damageable>() is { Dead: false })
                    {
                        ghoul = g;
                        break;
                    }

                if (ghoul == null)
                {
                    sb.AppendLine("plus de goule");
                    break;
                }

                player.Restore();
                player.InvulnerableUntil = float.NegativeInfinity;
                Vector3 at = player.transform.position + player.transform.forward * 1.4f;
                var rb = ghoul.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position = at;
                    rb.linearVelocity = Vector3.zero;
                }

                ghoul.transform.position = at;
                // Attend 2 s qu'elle frappe (un coup) : son début de geste.
                int strikes = ghoul.Strikes;
                float t0 = Time.time;
                while (ghoul.Strikes == strikes && Time.time - t0 < 6f)
                {
                    if (rb != null && Vector3.Distance(rb.position, at) > 0.8f)
                        rb.position = at;
                    yield return null;
                }

                if (ghoul.Strikes == strikes)
                {
                    sb.AppendLine($"décalage {offset * 1000f:+0;-0} ms : pas de coup en 6 s");
                    continue;
                }

                float impact = Time.time + ghoul.ImpactDelay;
                float hp0 = player.CurrentHealth;
                int parried0 = ghoul.Parried;
                float pressAt = impact + offset;
                while (Time.time < pressAt)
                    yield return null;
                bool accepted = parry.Press(Time.time);
                float actual = Time.time - impact;
                while (Time.time < Mathf.Max(impact, pressAt) + 0.3f)
                    yield return null;
                bool ok = ghoul.Parried > parried0;
                sb.AppendLine($"appui {actual * 1000f:+0;-0} ms de l'impact (voulu {offset * 1000f:+0;-0}) : {(ok ? "PARÉE" : "touchée")}" +
                              $", PV {hp0:0} → {player.CurrentHealth:0}, appui accepté {accepted}, debug « {parry.DebugLine} »");
                yield return new WaitForSeconds(1.2f);
            }

            Report = sb.ToString();
            Done = true;
            Destroy(gameObject);
        }
    }
}
