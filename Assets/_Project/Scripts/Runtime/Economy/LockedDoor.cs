using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Porte de la salle verrouillée (D-043) : elle s'ouvre contre des âmes avec E / Y (refus en rouge sinon) ; derrière, un
    /// coffre gratuit. Ouverte, elle disparaît (collisions et rendu coupés) jusqu'à la remise à zéro de la zone.
    /// </summary>
    public sealed class LockedDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Autres pièces bloquées avec la porte (barreaux de la fenêtre).")]
        [SerializeField] private GameObject[] alsoBlocking;

        private WorldLabel[] _labels;

        public bool Open { get; private set; }

        /// <summary>Réglages (porte posée à l'exécution par le générateur d'un acte).</summary>
        public void Configure(EconomySettings economy) => settings = economy;
        public int Price => settings != null ? settings.lockedDoorPrice : 50;
        public int PriceFor(PlayerHealth p) => Prices.For(settings, p, PriceKind.Porte, Price);
        public Vector3 Point => transform.position + Vector3.up * 1.8f;   // à hauteur des yeux (porte posée au sol)
        public float Range => Mathf.Max(settings != null ? settings.interactRange : 4f, 8f);   // portes de 12 m (nuit 3)
        public bool AcceptsGrab => false;

        // Un texte devant chaque face de la porte : la porte cache celui de l'autre face (règle des textes en monde).
        // Nuit 3, point 1 : plus de texte en 3D ; l'invite d'interaction de la DA donne le prix.
        private void Awake() => _labels = new WorldLabel[0];

        private void OnEnable() => Interactables.All.Add(this);
        private void OnDisable() => Interactables.All.Remove(this);

        public bool Available(PlayerHealth p) => !Open && p != null && !p.Dead;
        public string Prompt(PlayerHealth p) => $"Ouvrir la salle verrouillée — {PriceFor(p)} âmes";

        public void Interact(PlayerHealth p)
        {
            RunState run = RunState.Current;
            if (Open || run == null || !Prices.TryBuy(settings, p, PriceKind.Porte, Price))
                return;
            SetOpen(true);
        }

        private Transform _grille;
        private Vector3 _grilleClosed;
        private float _lift, _openedAt = float.NegativeInfinity;

        /// <summary>Grille de fer (nuit 4, point 5) : elle se lève de lift m en 1.8 s à l'ouverture, au lieu de disparaître.</summary>
        public void ConfigureGrille(Transform grille, float lift)
        {
            _grille = grille;
            _grilleClosed = grille.localPosition;
            _lift = lift;
        }

        private void Update()
        {
            if (_grille == null || float.IsNegativeInfinity(_openedAt))
                return;
            float u = Mathf.Clamp01((Time.time - _openedAt) / 1.8f);
            float k = Open ? u * u * (3f - 2f * u) : 0f;
            _grille.localPosition = _grilleClosed + Vector3.up * (_lift * k) + (Open && u < 1f ? new Vector3(Mathf.Sin(Time.time * 41f) * 0.03f, 0f, 0f) : Vector3.zero);
            if (u >= 1f)
                _openedAt = float.NegativeInfinity;
        }

        public void SetOpen(bool open)
        {
            Open = open;
            foreach (Collider c in GetComponentsInChildren<Collider>(true))
                c.enabled = !open;
            if (_grille != null)
            {
                // La grille reste visible : elle monte dans le linteau (son d'ouverture de porte de la DA).
                _openedAt = Time.time;
                if (!open)
                    _grille.localPosition = _grilleClosed;
                if (open)
                    ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.doorOpen, transform.position, 1f);
            }
            else
                foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                    if (r.GetComponent<TextMesh>() == null)
                        r.enabled = !open;
            if (_labels != null)
                foreach (WorldLabel l in _labels)
                    if (l != null)
                        l.gameObject.SetActive(!open);
            if (alsoBlocking != null)
                foreach (GameObject g in alsoBlocking)
                    if (g != null)
                        g.SetActive(!open);
        }
    }
}
