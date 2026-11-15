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
            _renderers = GetComponentsInChildren<Renderer>(true);
            _lockedMaterials = new Material[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _lockedMaterials[i] = _renderers[i].sharedMaterial;
            // Un texte devant chaque face de la porte : la porte cache celui de l'autre face (règle des textes en monde).
            _labels = WorldLabel.CreateOnFaces(transform, "Sortie", new Color(1f, 0.5f, 0.3f), 0.5f);
            foreach (WorldLabel l in _labels)
                l.TextFor = _ => LabelText();
        }

        private void OnEnable() => Interactables.All.Add(this);
        private void OnDisable() => Interactables.All.Remove(this);

        private void Update()
        {
            bool u = Unlocked;
            if (u == _shownUnlocked || Open)
                return;
            _shownUnlocked = u;
            for (int i = 0; i < _renderers.Length; i++)
                if (_renderers[i] != null && _renderers[i].GetComponent<TextMesh>() == null)
                    _renderers[i].sharedMaterial = u && unlockedMaterial != null ? unlockedMaterial : _lockedMaterials[i];
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

        public bool Available(PlayerHealth p) => !Open && Unlocked && p != null && !p.Dead;
        public string Prompt(PlayerHealth p) => $"ouvrir la sortie ({Price} âmes)";

        public void Interact(PlayerHealth p)
        {
            RunState run = RunState.Current;
            if (Open || !Unlocked || run == null || p == null || !run.TrySpend(p.PlayerId, Price))
                return;
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
