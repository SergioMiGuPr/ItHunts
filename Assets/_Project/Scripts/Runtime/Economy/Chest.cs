using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Coffre (D-043) : il s'ouvre contre des âmes, seulement avec E / Y (refus en rouge s'il manque des âmes ; prix monté à
    /// chaque achat selon les réglages). Ouvert : 3 orbes flottent au-dessus, une par amélioration tirée avec la seed ; celui
    /// qui l'a ouvert en prend une (F ou E), les autres disparaissent ; sans choix, elles restent 30 s. Prix 0 : coffre gratuit
    /// (salle verrouillée).
    /// </summary>
    public sealed class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Prix (âmes) ; -1 : celui des réglages (30).")]
        [SerializeField] private int price = -1;
        [Tooltip("Couvercle (caché à l'ouverture) et matériau des orbes de choix.")]
        [SerializeField] private Transform lid;
        [SerializeField] private Material orbMaterial;

        public static readonly List<Chest> AllChests = new List<Chest>();

        private readonly List<OfferOrb> _choices = new List<OfferOrb>();
        private float _openedAt = float.NegativeInfinity;
        private WorldLabel _label;

        public bool Opened { get; private set; }
        public bool Chosen { get; private set; }
        public int OpenedBy { get; private set; }
        public int Price => price >= 0 ? price : settings != null ? settings.chestPrice : 30;
        public IReadOnlyList<OfferOrb> Choices => _choices;
        public int PriceFor(PlayerHealth p) => Prices.For(settings, p, PriceKind.Coffre, Price);
        public Vector3 Point => transform.position + Vector3.up * 0.6f;
        public float Range => settings != null ? settings.interactRange : 4f;
        public bool AcceptsGrab => false;

        private void Awake()
        {
            _label = WorldLabel.Create(transform, new Vector3(0f, 1.7f, 0f), Price > 0 ? $"Coffre\n{Price} âmes" : "Coffre", new Color(1f, 0.85f, 0.4f));
        }

        private void OnEnable()
        {
            Interactables.All.Add(this);
            AllChests.Add(this);
        }

        private void OnDisable()
        {
            Interactables.All.Remove(this);
            AllChests.Remove(this);
        }

        /// <summary>Remise à zéro de la zone : fermé, plus d'orbes.</summary>
        public void ResetChest()
        {
            foreach (OfferOrb o in _choices)
                if (o != null)
                    Destroy(o.gameObject);
            _choices.Clear();
            Opened = Chosen = false;
            OpenedBy = 0;
            if (lid != null)
                lid.gameObject.SetActive(true);
            if (_label != null)
                _label.gameObject.SetActive(true);
        }

        public bool Available(PlayerHealth p) => !Opened && p != null && !p.Dead;
        public string Prompt(PlayerHealth p) => PriceFor(p) > 0 ? $"ouvrir le coffre ({PriceFor(p)} âmes)" : "ouvrir le coffre";
        public void Interact(PlayerHealth p) => TryOpen(p);

        /// <summary>Ouvre le coffre pour ce joueur s'il paie (refus en rouge sinon). Vrai s'il s'est ouvert.</summary>
        public bool TryOpen(PlayerHealth p)
        {
            if (Opened || p == null || p.Dead)
                return false;
            RunState run = RunState.Current;
            if (run == null || !Prices.TryBuy(settings, p, PriceKind.Coffre, Price))
                return false;
            Opened = true;
            OpenedBy = p.PlayerId;
            _openedAt = Time.time;
            if (lid != null)
                lid.gameObject.SetActive(false);
            if (_label != null)
                _label.gameObject.SetActive(false);
            int count = settings != null ? settings.choiceCount : 3;
            UpgradeDefinition[] drawn = run.DrawUpgrades(count);
            for (int i = 0; i < drawn.Length; i++)
            {
                // Espacées de 2.4 m et en quinconce : les noms (deux lignes) ne se chevauchent pas, lisibles à 10 m.
                float x = (i - (drawn.Length - 1) * 0.5f) * 2.4f;
                Vector3 at = transform.position + transform.right * x + Vector3.up * (2.2f + (i % 2 == 1 ? 0.6f : 0f));
                OfferOrb orb = OfferOrb.Create(transform, at, drawn[i], 0, 0f, p.PlayerId, settings != null ? settings.orbInteractRange : 8f, orbMaterial);
                orb.TakenBy += OnChosen;
                _choices.Add(orb);
            }

            return true;
        }

        private void OnChosen(OfferOrb chosen, PlayerHealth p)
        {
            Chosen = true;
            foreach (OfferOrb o in _choices)
                if (o != null && o != chosen)
                    Destroy(o.gameObject);
            _choices.Clear();
        }

        private void Update()
        {
            if (Opened && !Chosen && _choices.Count > 0 && settings != null && Time.time - _openedAt >= settings.choiceLifetime)
            {
                foreach (OfferOrb o in _choices)
                    if (o != null)
                        Destroy(o.gameObject);
                _choices.Clear();                                       // personne n'a choisi : elles s'en vont
            }
        }
    }
}
