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

        private WorldLabel _label;

        public bool Open { get; private set; }
        public int Price => settings != null ? settings.lockedDoorPrice : 50;
        public int PriceFor(PlayerHealth p) => Prices.For(settings, p, PriceKind.Porte, Price);
        public Vector3 Point => transform.position;
        public float Range => settings != null ? settings.interactRange : 4f;
        public bool AcceptsGrab => false;

        private void Awake() => _label = WorldLabel.Create(transform, new Vector3(0f, 0f, 0f), $"Salle verrouillée\n{Price} âmes", new Color(1f, 0.7f, 0.3f));

        private void OnEnable() => Interactables.All.Add(this);
        private void OnDisable() => Interactables.All.Remove(this);

        public bool Available(PlayerHealth p) => !Open && p != null && !p.Dead;
        public string Prompt(PlayerHealth p) => $"ouvrir la salle verrouillée ({PriceFor(p)} âmes)";

        public void Interact(PlayerHealth p)
        {
            RunState run = RunState.Current;
            if (Open || run == null || !Prices.TryBuy(settings, p, PriceKind.Porte, Price))
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
            if (_label != null)
                _label.gameObject.SetActive(!open);
            if (alsoBlocking != null)
                foreach (GameObject g in alsoBlocking)
                    if (g != null)
                        g.SetActive(!open);
        }
    }
}
