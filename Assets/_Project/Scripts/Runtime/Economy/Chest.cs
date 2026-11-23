using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItHunts.Economy
{
    /// <summary>
    /// Coffre (D-043), **par joueur** : chaque joueur peut l'ouvrir une fois, en payant son prix (refus en rouge s'il manque des
    /// âmes ; prix monté à chaque achat selon les réglages), seulement avec E / Y. Ouvert pour un joueur : 3 orbes flottent
    /// au-dessus, visibles par lui seul, une par amélioration tirée avec la seed ; il en prend une (F ou E), les autres
    /// disparaissent ; sans choix, elles restent 30 s. Les autres joueurs peuvent encore l'ouvrir pour eux ; chacun voit son état
    /// (couvercle et texte : « ouvert » pour lui ou son prix). Prix 0 : coffre gratuit (salle verrouillée).
    /// </summary>
    public sealed class Chest : MonoBehaviour, IInteractable
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Prix (âmes) ; -1 : celui des réglages (30).")]
        [SerializeField] private int price = -1;
        [Tooltip("Couvercle (caché pour qui l'a ouvert) et matériau des orbes de choix.")]
        [SerializeField] private Transform lid;
        [SerializeField] private Material orbMaterial;

        public static readonly List<Chest> AllChests = new List<Chest>();

        /// <summary>Ouverture d'un joueur : ses orbes de choix, quand, a-t-il choisi.</summary>
        private sealed class Opening
        {
            public readonly List<OfferOrb> Choices = new List<OfferOrb>();
            public float At;
            public bool Chosen;
        }

        private readonly Dictionary<int, Opening> _openings = new Dictionary<int, Opening>();
        private WorldLabel _label;
        private Renderer[] _lidRenderers = new Renderer[0];

        public int Price => price >= 0 ? price : settings != null ? settings.chestPrice : 30;

        [Tooltip("Coffre rare (icône MiniCarte_Coffre_Rare sur la carte) : les coffres des parcours, qu'on n'atteint qu'en bougeant bien.")]
        [SerializeField] private bool rare;
        public bool Rare { get => rare; set => rare = value; }
        public int PriceFor(PlayerHealth p) => Prices.For(settings, p, PriceKind.Coffre, Price);
        public Vector3 Point => transform.position + Vector3.up * 0.6f;
        public float Range => settings != null ? settings.interactRange : 4f;
        public bool AcceptsGrab => false;

        /// <summary>Ouvert par ce joueur ?</summary>
        public bool OpenedFor(int playerId) => _openings.ContainsKey(playerId);
        /// <summary>Ce joueur a-t-il pris son amélioration ?</summary>
        public bool ChosenBy(int playerId) => _openings.TryGetValue(playerId, out Opening o) && o.Chosen;
        /// <summary>Orbes de choix (encore là) de ce joueur.</summary>
        public IReadOnlyList<OfferOrb> ChoicesFor(int playerId) =>
            _openings.TryGetValue(playerId, out Opening o) ? o.Choices : (IReadOnlyList<OfferOrb>)System.Array.Empty<OfferOrb>();
        /// <summary>Nombre de joueurs qui l'ont ouvert.</summary>
        public int OpenCount => _openings.Count;

        private void Awake()
        {
            if (OfferOrb.WorldVisible)
            {
                // Texte flottant greybox (nuit 4, point 1 : retiré ; l'invite d'interaction de la DA donne le prix).
                _label = WorldLabel.Create(transform, new Vector3(0f, 1.7f, 0f), BaseText(), new Color(1f, 0.85f, 0.4f));
                _label.TextFor = p => OpenedFor(p.PlayerId) ? "Coffre\nouvert" : PriceFor(p) > 0 ? $"Coffre\n{PriceFor(p)} âmes" : "Coffre";
            }
            if (lid != null)
                _lidRenderers = lid.GetComponentsInChildren<Renderer>(true);
        }

        private string BaseText() => Price > 0 ? $"Coffre\n{Price} âmes" : "Coffre";

        private void OnEnable()
        {
            Interactables.All.Add(this);
            AllChests.Add(this);
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
        }

        private void OnDisable()
        {
            Interactables.All.Remove(this);
            AllChests.Remove(this);
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        }

        /// <summary>Couvercle par caméra : ouvert (caché) pour le joueur qui l'a ouvert, fermé pour les autres.</summary>
        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            PlayerHealth viewer = Interactables.PlayerOfCamera(cam);
            bool closed = viewer == null ? _openings.Count == 0 : !OpenedFor(viewer.PlayerId);
            foreach (Renderer r in _lidRenderers)
                if (r != null)
                    r.enabled = closed;
        }

        /// <summary>Remise à zéro de la zone : fermé pour tous, plus d'orbes.</summary>
        public void ResetChest()
        {
            foreach (Opening o in _openings.Values)
                DestroyChoices(o, null);
            _openings.Clear();
            foreach (Renderer r in _lidRenderers)
                if (r != null)
                    r.enabled = true;
        }

        public bool Available(PlayerHealth p) => p != null && !p.Dead && (!OpenedFor(p.PlayerId) || Pending(p) != null) && ShopMenu.Of(p) == null;
        public string Prompt(PlayerHealth p) => Pending(p) != null ? "choisir une amélioration"
            : PriceFor(p) > 0 ? $"ouvrir le coffre ({PriceFor(p)} âmes)" : "ouvrir le coffre";

        public void Interact(PlayerHealth p)
        {
            Opening pending = Pending(p);
            if (pending != null)
                ShopMenu.Show(p, ShopMenu.Kind.Coffre, rare ? "Coffre rare" : "Coffre", pending.Choices);   // menu refermé sans choisir
            else
                TryOpen(p);
        }

        /// <summary>Ouverture de ce joueur dont le choix n'est pas encore fait (menu fermé sans choisir), sinon null.</summary>
        private Opening Pending(PlayerHealth p) =>
            p != null && _openings.TryGetValue(p.PlayerId, out Opening o) && !o.Chosen && o.Choices.Count > 0 ? o : null;

        /// <summary>Ouvre le coffre pour ce joueur s'il paie (refus en rouge sinon) et s'il ne l'a pas déjà ouvert.</summary>
        public bool TryOpen(PlayerHealth p)
        {
            if (!Available(p))
                return false;
            RunState run = RunState.Current;
            if (run == null || !Prices.TryBuy(settings, p, PriceKind.Coffre, Price))
                return false;
            var opening = new Opening { At = Time.time };
            _openings[p.PlayerId] = opening;
            int count = (settings != null ? settings.choiceCount : 3) + PlayerUpgrades.ExtraChestChoices(p.PlayerId);
            if (rare && settings != null)
                count += settings.rareExtraChoices;                        // coffre rare (D-071) : un choix en plus
            int act = ItHunts.Level.RunSession.Act;
            var up = PlayerUpgrades.OfId(p.PlayerId);
            bool rankUps = act >= (settings != null ? settings.chestRankUpFromAct : 1) || (up != null && up.AtLeast(UpgradeKind.OeilDuCollectionneur, 2));
            int startRank = rare || (up != null && up.Mythic(UpgradeKind.OeilDuCollectionneur)) ? 1 : 0;   // coffre rare / Collectionneur Mythique : Argent direct
            var drawn = ChestOffers(run, p, count, rankUps, startRank);
            for (int i = 0; i < drawn.Count; i++)
            {
                // Espacées de 2.4 m et en quinconce : les noms (deux lignes) ne se chevauchent pas, lisibles à 10 m.
                float x = (i - (drawn.Count - 1) * 0.5f) * 2.4f;
                Vector3 at = transform.position + transform.right * x + Vector3.up * (2.2f + (i % 2 == 1 ? 0.6f : 0f));
                OfferOrb orb = OfferOrb.Create(transform, at, drawn[i].u, 0, 0f, p.PlayerId, settings != null ? settings.orbInteractRange : 8f, orbMaterial,
                    drawn[i].from, drawn[i].to);
                orb.TakenBy += (chosen, by) => OnChosen(opening, chosen);
                orb.MenuOnly = true;                                   // choix dans le menu du coffre (point 4, 2026-11-21)
                opening.Choices.Add(orb);
            }

            ShopMenu.Show(p, ShopMenu.Kind.Coffre, rare ? "Coffre rare" : "Coffre", opening.Choices);
            return true;
        }

        /// <summary>
        /// Choix d'un coffre (D-100, pur) : count offres, au moins une nouvelle ; une montée de rang possible (dès l'acte II, ou avec
        /// Œil du collectionneur Or) ; emplacements pleins : montées seulement. startRank : la première nouvelle arrive directement
        /// à ce rang (coffre rare, Collectionneur Mythique). Renvoie (amélioration, rang de départ, rang visé).
        /// </summary>
        public static List<(UpgradeDefinition u, int from, int to)> ChestOffers(RunState run, PlayerHealth p, int count, bool rankUps, int startRank = 0)
        {
            var r = new List<(UpgradeDefinition, int, int)>();
            if (run == null || p == null || count <= 0)
                return r;
            int ups = rankUps ? 1 : 0;
            UpgradeDefinition[] news = run.DrawNewFor(p, count - ups);
            UpgradeDefinition[] raised = ups > 0 || news.Length < count ? run.DrawRankUpFor(p.PlayerId, count - news.Length) : new UpgradeDefinition[0];
            for (int i = 0; i < news.Length; i++)
                r.Add((news[i], -1, i == 0 ? Mathf.Clamp(startRank, 0, UpgradeDefinition.MaxRank) : 0));
            foreach (UpgradeDefinition u in raised)
            {
                int rank = run.RankOf(p.PlayerId, u);
                r.Add((u, rank, rank + 1));
            }

            return r;
        }

        private static void DestroyChoices(Opening o, OfferOrb keep)
        {
            foreach (OfferOrb orb in o.Choices)
                if (orb != null && orb != keep)
                    Interactables.Discard(orb.gameObject);
            o.Choices.Clear();
        }

        private static void OnChosen(Opening o, OfferOrb chosen)
        {
            o.Chosen = true;
            DestroyChoices(o, chosen);
        }

        private void Update()
        {
            if (settings == null)
                return;
            foreach (Opening o in _openings.Values)
                if (!o.Chosen && o.Choices.Count > 0 && Time.time - o.At >= settings.choiceLifetime)
                    DestroyChoices(o, null);                             // personne n'a choisi : elles s'en vont
        }
    }
}
