using System;
using System.Collections;
using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Level
{
    /// <summary>États de la goule montrés dans la vitrine.</summary>
    public enum VitrineGhoulState : byte
    {
        Repos,
        Course,
        AnnonceDuBond,
        Bond,
        Reception,
        Escalade,
        Coup,
        Etourdie,
        Tenue,
        Morte,
    }

    /// <summary>États du volant montrés dans la vitrine.</summary>
    public enum VitrineFlyerState : byte
    {
        Ronde,
        AnnonceDuPique,
        Pique,
        EtourdiAuSol,
        Tenu,
    }

    /// <summary>
    /// Scène vitrine (Test_Vitrine, retours du lot, point 9) : tout ce que la DA a livré, rangé et étiqueté, pour vérifier vite.
    /// Ennemis « figés » par défaut (sans IA, le joueur ne prend aucun dégât) qui rejouent chaque état en boucle ; sans « figé »,
    /// leur IA reprend (on les voit attaquer, toujours sans dégâts). Une goule libre grimpe un mur puis un échafaudage ; un
    /// traqueur poursuit vraiment le joueur dans son enclos (activable). Panneau (F1) : personnage (bras, orbe, artefact), gestes,
    /// ambiance, figé, poursuite. Les animations de la DA arriveront par les mêmes composants (FpsArms, modèles des ennemis).
    /// </summary>
    public sealed class Vitrine : MonoBehaviour
    {
        [Serializable]
        public sealed class GhoulEntry
        {
            public Ghoul ghoul;
            public VitrineGhoulState state;
            [Tooltip("Mur (escalade) : pied et sommet de la paroi, face au mur.")]
            public Vector3 wallBottom, wallTop;
        }

        [Serializable]
        public sealed class FlyerEntry
        {
            public Flyer flyer;
            public VitrineFlyerState state;
        }

        [Serializable]
        public sealed class StalkerEntry
        {
            public Stalker stalker;
            public Stalker.ShowcasePose pose;
        }

        [Serializable]
        public sealed class PathPoint
        {
            public Vector3 position;
            [Tooltip("Comment la goule rejoint ce point : course, escalade (mur), bond.")]
            public GhoulMode mode;
            [Min(0.1f)] public float duration = 1f;
            [Tooltip("Escalade : direction de la paroi (de la goule vers le mur).")]
            public Vector3 wallNormal;
        }

        [SerializeField] private List<GhoulEntry> ghouls = new List<GhoulEntry>();
        [SerializeField] private List<FlyerEntry> flyers = new List<FlyerEntry>();
        [SerializeField] private List<StalkerEntry> stalkers = new List<StalkerEntry>();
        [Tooltip("Goule libre : elle suit ce parcours en boucle (mur, échafaudage).")]
        [SerializeField] private Ghoul freeGhoul;
        [SerializeField] private List<PathPoint> freePath = new List<PathPoint>();
        [Tooltip("Traqueur de l'enclos : il poursuit vraiment le joueur quand la poursuite est activée.")]
        [SerializeField] private Stalker chaser;
        [SerializeField] private List<CharacterDefinition> characters = new List<CharacterDefinition>();
        [SerializeField] private List<AmbianceSettings> ambiances = new List<AmbianceSettings>();
        [SerializeField, Min(0.5f)] private float loop = 2.4f;

        public static Vitrine Current { get; private set; }

        /// <summary>Figé : ennemis sans IA qui rejouent leurs états ; faux : leur IA reprend (toujours sans dégâts).</summary>
        public bool Frozen { get; private set; } = true;
        public bool Chasing { get; private set; }
        public bool PanelOpen { get; private set; }
        public IReadOnlyList<GhoulEntry> Ghouls => ghouls;
        public IReadOnlyList<FlyerEntry> Flyers => flyers;
        public IReadOnlyList<StalkerEntry> Stalkers => stalkers;

        private readonly Dictionary<Component, Vector3> _home = new Dictionary<Component, Vector3>();
        private readonly Dictionary<Component, Quaternion> _homeRot = new Dictionary<Component, Quaternion>();
        private readonly Dictionary<Rigidbody, bool> _kinematic = new Dictionary<Rigidbody, bool>();
        private bool _ready;
        private int _ambiance = 1;
        private string _status = "";

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        private IEnumerator Start()
        {
            PlayerHealth.DebugInvulnerable = true;                         // la vitrine ne blesse jamais
            yield return null;                                             // chaque ennemi a fait son Start (réglages, NavMesh)
            yield return null;
            foreach (GhoulEntry e in ghouls)
                Remember(e.ghoul);
            foreach (FlyerEntry e in flyers)
                Remember(e.flyer);
            foreach (StalkerEntry e in stalkers)
                Remember(e.stalker);
            Remember(freeGhoul);
            Remember(chaser);
            SetFrozen(true);
            SetChasing(false);
            if (ambiances.Count > 1)
                UseAmbiance(1);
            _ready = true;
        }

        private void Remember(Component c)
        {
            if (c == null)
                return;
            _home[c] = c.transform.position;
            _homeRot[c] = c.transform.rotation;
            var rb = c.GetComponent<Rigidbody>();
            if (rb != null && !_kinematic.ContainsKey(rb))
                _kinematic[rb] = rb.isKinematic;
        }

        // ---------- Commandes (panneau, tests) ----------

        public void SetFrozen(bool frozen)
        {
            Frozen = frozen;
            foreach (GhoulEntry e in ghouls)
                Drive(e.ghoul, !frozen);
            foreach (FlyerEntry e in flyers)
                Drive(e.flyer, !frozen);
            foreach (StalkerEntry e in stalkers)
                Drive(e.stalker, !frozen);
            Drive(freeGhoul, false);                                       // la goule libre suit toujours son parcours
            if (!frozen)
                foreach (GhoulEntry e in ghouls)
                    if (e.ghoul != null)
                    {
                        Damageable d = e.ghoul.GetComponent<Damageable>();
                        if (d != null && d.Dead)
                            d.Revive();
                    }
        }

        public void SetChasing(bool chasing)
        {
            Chasing = chasing;
            Drive(chaser, chasing);
            // Le traqueur reste au repaire pendant le temps de grâce : la poursuite le saute.
            var zone = FindFirstObjectByType<ZoneState>();
            if (chasing && zone != null && zone.GraceLeft > 0f)
                zone.DebugAdvance(zone.GraceLeft + 0.1f);
            if (!chasing && chaser != null && _home.TryGetValue(chaser, out Vector3 h))
            {
                chaser.transform.SetPositionAndRotation(h, _homeRot[chaser]);
                var rb = chaser.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.position = h;
                    rb.rotation = _homeRot[chaser];
                }
            }
        }

        /// <summary>IA de l'ennemi : active (corps rendu à sa physique) ou figée (corps cinématique, piloté par la vitrine).</summary>
        private void Drive(MonoBehaviour enemy, bool ai)
        {
            if (enemy == null)
                return;
            enemy.enabled = ai;
            var rb = enemy.GetComponent<Rigidbody>();
            if (rb != null && _kinematic.TryGetValue(rb, out bool k))
                rb.isKinematic = ai ? k : true;
            if (ai && _home.TryGetValue(enemy, out Vector3 h))
            {
                enemy.transform.SetPositionAndRotation(h, _homeRot[enemy]);
                if (rb != null)
                    rb.position = h;
            }
        }

        public void UseAmbiance(int index)
        {
            if (index < 0 || index >= ambiances.Count || ambiances[index] == null)
                return;
            _ambiance = index;
            var sa = FindFirstObjectByType<SceneAmbiance>();
            if (sa != null)
                sa.Use(ambiances[index]);
            _status = "Ambiance : " + ambiances[index].name.Replace("Ambiance_", "");
        }

        public void UseCharacter(int index)
        {
            PlayerHealth p = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0] : null;
            var holder = p != null ? p.GetComponent<ArtifactHolder>() : null;
            if (holder == null || index < 0 || index >= characters.Count || characters[index] == null)
                return;
            holder.EquipCharacter(characters[index]);
            _status = "Personnage : " + characters[index].displayName;
        }

        /// <summary>Geste ou état de la vue FPS : traction, répulsion, prise, objet tenu, lancer, tir, jet de la lanterne.</summary>
        public void Gesture(string name)
        {
            PlayerHealth p = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0] : null;
            var view = p != null ? p.GetComponent<FirstPersonView>() : null;
            if (view == null)
                return;
            switch (name)
            {
                case "Repos": view.Preview = ViewPreview.Aucun; break;
                case "Traction": view.Preview = ViewPreview.Traction; break;
                case "Répulsion": view.Preview = ViewPreview.Aucun; view.PlayGesture(HandGesture.Repulsion); break;
                case "Répulsion maintenue": view.Preview = ViewPreview.RepulsionTenue; break;
                case "Prise": view.Preview = ViewPreview.Aucun; view.PlayGesture(HandGesture.Prise); break;
                case "Objet tenu": view.Preview = ViewPreview.ObjetTenu; break;
                case "Lancer": view.Preview = ViewPreview.ObjetTenu; view.PlayGesture(HandGesture.Lancer); break;
                case "Tir": view.Preview = ViewPreview.Aucun; p.GetComponent<BarrelView>()?.PreviewShot(); break;
                case "Jet": view.Preview = view.Preview == ViewPreview.Jet ? ViewPreview.Aucun : ViewPreview.Jet; break;
            }

            _status = "Geste : " + name;
        }

        // ---------- Boucles des états ----------

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k.f1Key.wasPressedThisFrame)
            {
                PanelOpen = !PanelOpen;
                PlayerLook.MenuOpen = PanelOpen;
            }

            if (!_ready)
                return;
            float now = Time.time;
            if (Frozen)
            {
                foreach (GhoulEntry e in ghouls)
                    ShowGhoul(e, now);
                foreach (FlyerEntry e in flyers)
                    ShowFlyer(e, now);
                foreach (StalkerEntry e in stalkers)
                    if (e.stalker != null)
                        e.stalker.ShowPose(e.pose, Mathf.Repeat(now / (loop * 1.5f), 1f));
            }

            FreeGhoul(now);
            if (!Chasing && chaser != null)
                chaser.ShowPose(Stalker.ShowcasePose.MarcheVoutee, Mathf.Repeat(now / (loop * 1.5f), 1f));
        }

        private void ShowGhoul(GhoulEntry e, float now)
        {
            Ghoul g = e.ghoul;
            if (g == null || !_home.TryGetValue(g, out Vector3 home))
                return;
            Quaternion rot = _homeRot[g];
            Vector3 fwd = rot * Vector3.forward;
            float u = Mathf.Repeat(now / loop, 1f);
            Vector3 pos = home;
            var dmg = g.GetComponent<Damageable>();
            switch (e.state)
            {
                case VitrineGhoulState.Repos:
                    g.ForceClip(null);
                    g.ShowPose(GhoulMode.Poste, 0f, home, home);
                    break;
                case VitrineGhoulState.Course:
                    g.ForceClip(g.Animated ? "Goule_Course" : null);
                    if (g.Animated)
                    {
                        g.ShowPose(GhoulMode.Chasse, 0f, home, home);
                        break;
                    }
                    pos += Vector3.up * (Mathf.Abs(Mathf.Sin(u * Mathf.PI * 6f)) * 0.12f);
                    g.ShowPose(GhoulMode.Chasse, 0f, home, home, 1f - 0.06f * Mathf.Abs(Mathf.Cos(u * Mathf.PI * 6f)));
                    break;
                case VitrineGhoulState.AnnonceDuBond:
                    if (g.Animated && u * loop < Time.deltaTime * 1.5f)
                        g.RestartClip("Goule_Annonce");
                    g.ShowPose(GhoulMode.Annonce, Mathf.Min(u * loop, 0.6f), home, home);
                    break;
                case VitrineGhoulState.Bond:
                {
                    float t = Mathf.Clamp01(u * 1.6f);
                    pos = home + fwd * (5f * t) + Vector3.up * (4f * 1.2f * t * (1f - t));
                    g.ShowPose(t < 1f ? GhoulMode.Bond : GhoulMode.Chasse, 0f, home, home);
                    break;
                }
                case VitrineGhoulState.Reception:
                {
                    float t = Mathf.Clamp01(u * 3f);
                    pos += Vector3.up * Mathf.Max(0f, 1.2f * (1f - t * 2f));
                    float squash = t < 0.5f ? 1f : 1f - 0.35f * Mathf.Exp(-(t - 0.5f) * 8f) * Mathf.Sin((t - 0.5f) * 6.28f + 1.57f);
                    g.ShowPose(t < 0.5f ? GhoulMode.Bond : GhoulMode.Chasse, 0f, home, home, squash);
                    break;
                }
                case VitrineGhoulState.Escalade:
                {
                    float t = Mathf.PingPong(u * 2f, 1f);
                    pos = Vector3.Lerp(e.wallBottom, e.wallTop, t);
                    bool goingUp = Mathf.Repeat(u * 2f, 2f) < 1f;
                    g.ShowPose(GhoulMode.Mur, 0f, goingUp ? e.wallBottom : e.wallTop, goingUp ? e.wallTop : e.wallBottom);
                    break;
                }
                case VitrineGhoulState.Coup:
                    if (g.Animated && Mathf.Repeat(now, loop * 0.5f) < Time.deltaTime * 1.5f)
                        g.RestartClip("Goule_Coup");                               // le coup de la goule (impact à 0.24 s)
                    g.ShowPose(GhoulMode.Chasse, 0f, home, home);
                    if (dmg != null && Mathf.Repeat(now, loop * 0.5f) < Time.deltaTime * 1.5f)
                        dmg.Flash(new Color(2f, 2f, 2f), 0.12f);
                    break;
                case VitrineGhoulState.Etourdie:
                    g.ForceClip(g.Animated ? "Goule_Etourdie" : null);
                    g.ShowPose(GhoulMode.Chasse, 0f, home, home);
                    var body = g.GetComponent<EnemyBody>();
                    if (body != null && !body.Stunned)
                        body.Stun(20f);
                    break;
                case VitrineGhoulState.Tenue:
                    pos = home + Vector3.up * (1.6f + 0.08f * Mathf.Sin(now * 5f));
                    rot = rot * Quaternion.Euler(8f * Mathf.Sin(now * 7f), 0f, 10f * Mathf.Sin(now * 9f));
                    g.ForceClip(g.Animated ? "Goule_Etourdie" : null);                 // tenue : elle se débat
                    g.ShowPose(GhoulMode.Chasse, 0f, home, home);
                    if (dmg != null)
                        dmg.ExtraGlow = new Color(0.7f, 0.25f, 1.6f);                // lueur violette de la télékinésie
                    break;
                case VitrineGhoulState.Morte:
                    g.ShowPose(GhoulMode.Chasse, 0f, home, home);
                    if (dmg != null)
                    {
                        bool shouldBeDead = Mathf.Repeat(now, loop * 1.25f) > loop * 0.6f;
                        if (shouldBeDead && !dmg.Dead)
                            dmg.ApplyDamage(99999f, now, out _);
                        else if (!shouldBeDead && dmg.Dead)
                            dmg.Revive();
                    }

                    break;
            }

            Place(g, pos, e.state == VitrineGhoulState.Escalade ? Quaternion.LookRotation(Flat(e.wallTop - e.wallBottom, fwd)) : rot);
        }

        private static Vector3 Flat(Vector3 v, Vector3 fallback)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : fallback;
        }

        private void ShowFlyer(FlyerEntry e, float now)
        {
            Flyer f = e.flyer;
            if (f == null || !_home.TryGetValue(f, out Vector3 home))
                return;
            float u = Mathf.Repeat(now / loop, 1f);
            Vector3 pos = home;
            Quaternion rot = _homeRot[f];
            switch (e.state)
            {
                case VitrineFlyerState.Ronde:
                {
                    float a = u * Mathf.PI * 2f;
                    pos = home + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 2.5f;
                    rot = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)));
                    f.ShowPose(FlyerMode.Ronde);
                    break;
                }
                case VitrineFlyerState.AnnonceDuPique:
                    f.ShowPose(FlyerMode.Annonce);
                    break;
                case VitrineFlyerState.Pique:
                {
                    float t = Mathf.Clamp01(u * 1.4f);
                    pos = home + Vector3.down * (t * Mathf.Max(0f, home.y - 1f)) + rot * Vector3.forward * (t * 3f);
                    f.ShowPose(t < 1f ? FlyerMode.Pique : FlyerMode.Remontee);
                    break;
                }
                case VitrineFlyerState.EtourdiAuSol:
                    pos = new Vector3(home.x, 0.4f, home.z);
                    f.ShowPose(FlyerMode.AuSol);
                    var body = f.GetComponent<EnemyBody>();
                    if (body != null && !body.Stunned)
                        body.Stun(20f);
                    break;
                case VitrineFlyerState.Tenu:
                    pos = home + Vector3.up * (0.1f * Mathf.Sin(now * 6f));
                    f.ShowPose(FlyerMode.Tenu);
                    var dmg = f.GetComponent<Damageable>();
                    if (dmg != null)
                        dmg.ExtraGlow = new Color(0.7f, 0.25f, 1.6f);
                    break;
            }

            Place(f, pos, rot);
        }

        private float _freeStart;

        /// <summary>Goule libre : court, grimpe le mur, traverse, saute, grimpe l'échafaudage, saute, en boucle.</summary>
        private void FreeGhoul(float now)
        {
            if (freeGhoul == null || freePath.Count < 2)
                return;
            float total = 0f;
            foreach (PathPoint p in freePath)
                total += p.duration;
            float t = Mathf.Repeat(now - _freeStart, total);
            for (int i = 0; i < freePath.Count; i++)
            {
                PathPoint to = freePath[i];
                if (t > to.duration)
                {
                    t -= to.duration;
                    continue;
                }

                PathPoint from = freePath[(i + freePath.Count - 1) % freePath.Count];
                float k = Mathf.Clamp01(t / to.duration);
                Vector3 a = from.position, b = to.position;
                Vector3 pos = Vector3.Lerp(a, b, k);
                Quaternion rot;
                if (to.mode == GhoulMode.Bond)
                {
                    pos += Vector3.up * (Vector3.Distance(a, b) * 0.35f * 4f * k * (1f - k));
                    rot = Quaternion.LookRotation(Flat(b - a, freeGhoul.transform.forward));
                    freeGhoul.ShowPose(GhoulMode.Bond, 0f, a, b);
                }
                else if (to.mode == GhoulMode.Mur)
                {
                    rot = Quaternion.LookRotation(Flat(to.wallNormal, freeGhoul.transform.forward));
                    freeGhoul.ShowPose(GhoulMode.Mur, 0f, a, b);
                }
                else
                {
                    pos += Vector3.up * (Mathf.Abs(Mathf.Sin(now * Mathf.PI * 5f)) * 0.08f);
                    rot = Quaternion.LookRotation(Flat(b - a, freeGhoul.transform.forward));
                    freeGhoul.ShowPose(GhoulMode.Chasse, 0f, a, b);
                }

                Place(freeGhoul, pos, rot);
                return;
            }
        }

        private static void Place(Component c, Vector3 pos, Quaternion rot)
        {
            c.transform.SetPositionAndRotation(pos, rot);
            var rb = c.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = pos;
                rb.rotation = rot;
            }
        }

        // ---------- Panneau (F1) ----------

        private static readonly string[] Gestures = { "Repos", "Traction", "Répulsion", "Répulsion maintenue", "Prise", "Objet tenu", "Lancer", "Tir", "Jet" };

        private void OnGUI()
        {
            if (!PanelOpen)
            {
                GUI.Label(new Rect(Screen.width - 330f, Screen.height - 34f, 320f, 26f), "Vitrine : F1 pour le panneau");
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 120f, 360f, Screen.height - 160f), GUI.skin.box);
            GUILayout.Label("<b>Vitrine</b>  (F1 pour fermer)", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("Personnage (bras, orbe, artefact)");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < characters.Count; i++)
                if (characters[i] != null && GUILayout.Button(characters[i].displayName))
                    UseCharacter(i);
            GUILayout.EndHorizontal();
            GUILayout.Label("Gestes de la vue FPS");
            for (int i = 0; i < Gestures.Length; i += 3)
            {
                GUILayout.BeginHorizontal();
                for (int j = i; j < i + 3 && j < Gestures.Length; j++)
                    if (GUILayout.Button(Gestures[j]))
                        Gesture(Gestures[j]);
                GUILayout.EndHorizontal();
            }

            GUILayout.Label("Ambiance");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < ambiances.Count; i++)
                if (ambiances[i] != null && GUILayout.Toggle(_ambiance == i, ambiances[i].name.Replace("Ambiance_", ""), GUI.skin.button))
                    if (_ambiance != i)
                        UseAmbiance(i);
            GUILayout.EndHorizontal();
            if (GUILayout.Button(Frozen ? "Ennemis figés (cliquer : IA, sans dégâts)" : "IA active (cliquer : figer)"))
                SetFrozen(!Frozen);
            if (GUILayout.Button(Chasing ? "Traqueur de l'enclos : il poursuit (cliquer : arrêter)" : "Traqueur de l'enclos : immobile (cliquer : poursuite)"))
                SetChasing(!Chasing);
            GUILayout.Label(_status);
            GUILayout.EndArea();
        }
    }
}
