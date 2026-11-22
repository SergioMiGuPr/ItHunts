using ItHunts.Artifacts;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Grande porte de sortie du niveau (D-048) : verrouillée (rouge) jusqu'à exitUnlockAt du timer, puis payée en âmes avec E / Y
    /// (orange, prix exitPrice ; un joueur paie, elle s'ouvre pour tous ; refus en rouge s'il manque des âmes). Ouverte, elle
    /// disparaît et laisse passer vers les escaliers. Le texte montre le compte à rebours.
    /// </summary>
    public sealed class ExitDoor : MonoBehaviour, IInteractable
    {
        [Tooltip("Matériau quand elle est payable (lueur orange) ; verrouillée, elle garde son matériau d'origine (rouge).")]
        [SerializeField] private Material unlockedMaterial;
        [SerializeField] private float interactRange = 6f;

        private ZoneState _zone;
        private WorldLabel[] _labels;
        private Renderer[] _renderers;
        private Material[] _lockedMaterials;
        private bool _shownUnlocked;

        public bool Open { get; private set; }
        public bool Unlocked => Zone != null && Zone.ExitUnlocked;
        public int Price => Zone != null && Zone.Settings != null ? Zone.Settings.exitPrice : 200;
        public Vector3 Point => transform.position;
        public float Range => interactRange;
        public bool AcceptsGrab => false;

        private ZoneState Zone => _zone != null ? _zone : _zone = FindFirstObjectByType<ZoneState>();

        private void Awake()
        {
            // Nuit 3, point 1 : porte classique, plus de texte en 3D ni de lueur rouge ou orange ; l'invite d'interaction de la DA
            // donne l'état (verrouillée et compte à rebours, ou prix en âmes).
            _renderers = GetComponentsInChildren<Renderer>(true);
            _labels = new WorldLabel[0];
        }

        private void OnEnable() => Interactables.All.Add(this);
        private void OnDisable() => Interactables.All.Remove(this);

        private void Update()
        {
        }

        private string LabelText()
        {
            if (Zone == null)
                return "Sortie";
            if (!Zone.Started)
                return "Sortie\nverrouillée";
            if (!Unlocked)
            {
                int s = Mathf.CeilToInt(Zone.ExitUnlockLeft);
                return $"Sortie\nverrouillée {s / 60}:{s % 60:00}";
            }

            return $"Sortie\n{Price} âmes";
        }

        public bool Available(PlayerHealth p) => !Open && p != null && !p.Dead && Zone != null && Zone.Started;
        public string Prompt(PlayerHealth p)
        {
            if (!Unlocked)
            {
                int s = Mathf.CeilToInt(Zone != null ? Zone.ExitUnlockLeft : 0f);
                return $"Sortie verrouillée — {s / 60}:{s % 60:00}";
            }

            return $"Ouvrir la sortie — {Price} âmes";
        }

        public void Interact(PlayerHealth p)
        {
            RunState run = RunState.Current;
            if (Open || run == null || p == null)
                return;
            if (!Unlocked)
            {
                ItHunts.Artifacts.RunState.RaiseRefused(p.PlayerId);
                return;
            }

            if (!run.TrySpend(p.PlayerId, Price))
                return;
            ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.doorOpen, transform.position, 1f);
            SetOpen(true);
        }

        public void SetOpen(bool open)
        {
            Open = open;
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
                c.enabled = !open;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                if (r.GetComponent<TextMesh>() == null)
                    r.enabled = !open;
            if (_labels != null)
                foreach (WorldLabel l in _labels)
                    if (l != null)
                        l.gameObject.SetActive(!open);
        }

        /// <summary>Remise à zéro de la zone : refermée et verrouillée.</summary>
        public void ResetDoor()
        {
            SetOpen(false);
            _shownUnlocked = !Unlocked;
            Update();
        }
    }
}
