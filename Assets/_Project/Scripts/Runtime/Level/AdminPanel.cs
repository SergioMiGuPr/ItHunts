using System.Collections.Generic;
using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Hud;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ItHunts.Level
{
    /// <summary>
    /// Panneau admin (nuit 3, point 2) : seulement dans une partie lancée depuis le menu Playtest (RunSession.AdminMode), jamais dans une
    /// partie normale. F1 l'ouvre et le ferme (le joueur ne bouge plus pendant ce temps). Timer accéléré ou sauté, âmes, amélioration
    /// précise ou au hasard, changement d'artefact, god mode, ennemis tués, goules ou volants qui apparaissent, traqueur actif ou non,
    /// acte suivant, téléportation à la sortie. Vue de la DA : AdminPanelArt (assembly Menus) ; repli greybox sinon.
    /// </summary>
    public sealed class AdminPanel : MonoBehaviour
    {
        public const Key ToggleKey = Key.F1;
        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private readonly int[] _soulSteps = { 50, 200, 1000 };
        private readonly float[] _speeds = { 1f, 4f, 10f };
        private int _soulStep = 1, _speed, _upgrade = -1, _artifact, _spawnKind;
        private List<UpgradeDefinition> _upgrades = new List<UpgradeDefinition>();
        private List<CharacterDefinition> _characters = new List<CharacterDefinition>();
        private string _note = "";
        private float _noteUntil;

        public static AdminPanel Current { get; private set; }
        public static event System.Action<AdminPanel> Created;
        public bool Open { get; private set; }
        public GreyboxMenu Menu => _menu;
        /// <summary>Habillé par la DA (pas de repli greybox).</summary>
        public bool Skinned { get; set; }
        public string Note => Time.unscaledTime < _noteUntil ? _note : "";
        public float TimerSpeed => _speeds[_speed];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
        }

        private static void OnScene(Scene scene, LoadSceneMode mode)
        {
            if (!Allowed || Current != null)
                return;
            if (FindFirstObjectByType<ZoneState>(FindObjectsInactive.Include) == null)
                return;
            new GameObject("Panneau admin (playtest)").AddComponent<AdminPanel>();
        }

        /// <summary>Le panneau existe seulement pour une partie lancée depuis le menu Playtest.</summary>
        public static bool Allowed => RunSession.AdminMode && RunSession.Active;

        private void Awake()
        {
            if (!Allowed)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            Build();
            Created?.Invoke(this);
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
                if (Open)
                    SetOpen(false);
            }
        }

        private void Build()
        {
            _menu.Items.Clear();
            GreyboxMenu.Item speed = _menu.Add("Vitesse du timer", () => _speed = (_speed + 1) % _speeds.Length);
            speed.Value = () => _speeds[_speed] <= 1f ? "normale" : $"× {_speeds[_speed]:0}";
            speed.Change = d => _speed = (_speed + d + _speeds.Length) % _speeds.Length;
            _menu.Add("Sauter le timer (+1 min)", () => Do(() => Zone?.DebugAdvance(60f), "Timer +1 min"));
            GreyboxMenu.Item souls = _menu.Add("Donner des âmes", () => Do(GiveSouls, $"+{_soulSteps[_soulStep]} âmes"));
            souls.Value = () => $"+{_soulSteps[_soulStep]}";
            souls.Change = d => _soulStep = (_soulStep + d + _soulSteps.Length) % _soulSteps.Length;
            GreyboxMenu.Item up = _menu.Add("Donner une amélioration", () => Do(GiveUpgrade, "Amélioration donnée"));
            up.Value = () => _upgrade < 0 || _upgrade >= _upgrades.Count ? "au hasard" : _upgrades[_upgrade].displayName;
            up.Change = d =>
            {
                RefreshLists();
                int n = _upgrades.Count + 1;
                _upgrade = ((_upgrade + 1 + d) % n + n) % n - 1;
            };
            GreyboxMenu.Item art = _menu.Add("Changer d'artefact", () => Do(SwapArtifact, "Artefact changé"));
            art.Value = () => _characters.Count == 0 ? "—" : _characters[Mathf.Clamp(_artifact, 0, _characters.Count - 1)].ArtifactName;
            art.Change = d =>
            {
                RefreshLists();
                if (_characters.Count > 0)
                    _artifact = (_artifact + d + _characters.Count) % _characters.Count;
            };
            GreyboxMenu.Item god = _menu.Add("Invincible (god mode)", () => PlayerHealth.DebugInvulnerable = !PlayerHealth.DebugInvulnerable);
            god.Value = () => PlayerHealth.DebugInvulnerable ? "oui" : "non";
            god.Change = _ => PlayerHealth.DebugInvulnerable = !PlayerHealth.DebugInvulnerable;
            _menu.Add("Tuer les ennemis", () => Do(KillEnemies, "Ennemis tués"));
            GreyboxMenu.Item spawn = _menu.Add("Faire apparaître", () => Do(Spawn, _spawnKind == 0 ? "5 goules" : "3 volants"));
            spawn.Value = () => _spawnKind == 0 ? "5 goules" : "3 volants";
            spawn.Change = _ => _spawnKind = 1 - _spawnKind;
            GreyboxMenu.Item stalker = _menu.Add("Traqueur", ToggleStalker);
            stalker.Value = () => StalkerActive ? "actif" : "inactif";
            stalker.Change = _ => ToggleStalker();
            _menu.Add("Acte suivant", () => Do(() => Zone?.LevelWon(), "Acte suivant"));
            _menu.Add("Téléportation à la sortie", () => Do(TeleportToExit, "Devant la sortie"));
            _menu.Add("Fermer (F1)", () => SetOpen(false));
            _menu.FocusFirst();
            RefreshLists();
        }

        private void RefreshLists()
        {
            _upgrades = Resources.FindObjectsOfTypeAll<UpgradeDefinition>().Where(u => u != null && !u.name.EndsWith("(Rare)"))
                .OrderBy(u => u.displayName).ToList();
            RunState run = RunState.Current;
            _characters = run != null && run.Characters != null
                ? run.Characters.characters.Where(c => c != null && c.artifact != null).ToList()
                : new List<CharacterDefinition>();
        }

        private ZoneState Zone => ZoneState.Active;
        private static PlayerHealth Player => PlayerHealth.All.FirstOrDefault(p => p != null);

        private void Do(System.Action a, string note)
        {
            a?.Invoke();
            _note = note;
            _noteUntil = Time.unscaledTime + 2f;
        }

        private void GiveSouls()
        {
            RunState run = RunState.Current;
            PlayerHealth p = Player;
            if (run != null && p != null)
                run.AddSouls(p.PlayerId, _soulSteps[_soulStep]);
        }

        private void GiveUpgrade()
        {
            RefreshLists();
            RunState run = RunState.Current;
            PlayerHealth p = Player;
            if (run == null || p == null || _upgrades.Count == 0)
                return;
            UpgradeDefinition d = _upgrade >= 0 && _upgrade < _upgrades.Count ? _upgrades[_upgrade] : _upgrades[Random.Range(0, _upgrades.Count)];
            run.Grant(p.PlayerId, d);
            PlayerUpgrades.Of(p).Apply(d);
            _note = $"Amélioration : {d.displayName}";
        }

        private void SwapArtifact()
        {
            RefreshLists();
            PlayerHealth p = Player;
            if (p == null || _characters.Count == 0)
                return;
            var holder = p.GetComponent<ArtifactHolder>();
            if (holder != null)
                holder.EquipCharacter(_characters[Mathf.Clamp(_artifact, 0, _characters.Count - 1)]);
        }

        private static void KillEnemies()
        {
            foreach (Damageable d in FindObjectsByType<Damageable>(FindObjectsSortMode.None))
                if (d != null && !d.Dead && !d.ImmuneToOrbs && (d.GetComponent<Ghoul>() != null || d.GetComponent<Flyer>() != null))
                    d.ApplyDamage(1e6f, Time.time, out _);
        }

        private void Spawn()
        {
            PlayerHealth p = Player;
            if (p == null || Acts.ActLevel.Current == null)
                return;
            Vector3 fwd = p.transform.forward;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            int n = _spawnKind == 0 ? 5 : 3;
            for (int i = 0; i < n; i++)
            {
                Vector3 at = p.transform.position + fwd * 12f + Quaternion.AngleAxis(i * 360f / n, Vector3.up) * Vector3.right * 3f;
                Acts.ActLevel.Current.SpawnEnemy(_spawnKind == 1, at + (_spawnKind == 1 ? Vector3.up * 6f : Vector3.zero));
            }
        }

        private static Stalker FindStalker() => FindFirstObjectByType<Stalker>(FindObjectsInactive.Include);
        public static bool StalkerActive
        {
            get
            {
                Stalker s = FindStalker();
                return s != null && s.gameObject.activeSelf;
            }
        }

        private void ToggleStalker()
        {
            Stalker s = FindStalker();
            if (s == null)
                return;
            bool on = !s.gameObject.activeSelf;
            s.gameObject.SetActive(on);
            Do(null, on ? "Traqueur actif" : "Traqueur inactif");
        }

        private static void TeleportToExit()
        {
            PlayerHealth p = Player;
            if (p == null || p.Motor == null || Acts.ActLevel.Current == null)
                return;
            Vector3 exit = Acts.ActLevel.Current.ExitPoint;
            if (exit == Vector3.zero)
                return;
            p.Motor.Teleport(exit + Vector3.up * 0.3f);
        }

        // ---------- Ouverture ----------

        public void SetOpen(bool open)
        {
            Open = open;
            PlayerLook.MenuOpen = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            foreach (PlayerInput pi in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
                if (open)
                    pi.DeactivateInput();
                else
                    pi.ActivateInput();
            if (open)
            {
                RefreshLists();
                _menu.FocusFirst();
            }
        }

        private void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k[ToggleKey].wasPressedThisFrame)
                SetOpen(!Open);
            if (Open)
            {
                _menu.Update();
                if (k != null && k.escapeKey.wasPressedThisFrame)
                    SetOpen(false);
            }

            ZoneState z = Zone;
            if (z != null && _speeds[_speed] > 1f && z.Started && !z.Finished)
                z.DebugAdvance(Time.deltaTime * (_speeds[_speed] - 1f));
        }

        private void OnGUI()
        {
            if (!Open || Skinned)
                return;
            _menu.Draw("Panneau admin (playtest)", Note, 560f);
        }
    }
}
