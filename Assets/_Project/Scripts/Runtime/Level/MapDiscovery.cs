using System.Collections.Generic;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Brouillard de guerre de la minimap (D-053) : une salle ou un tronçon de couloir est découvert dès qu'un joueur vivant y entre.
    /// Découverte gardée par joueur (qui l'a vue) et partagée en coop (sharedInCoop, par défaut : la carte de chacun montre tout ce
    /// que l'équipe a vu ; sinon, chacun la sienne). Les points clés
    /// (coffres, marchand, salle verrouillée, sortie, départ) s'affichent quand le volume qui les contient est découvert.
    /// Réseau (étape 11) : découvertes envoyées par l'hôte (événement Discovered).
    /// </summary>
    public sealed class MapDiscovery : MonoBehaviour
    {
        private readonly HashSet<RoomVolume> _known = new HashSet<RoomVolume>();
        private readonly Dictionary<int, HashSet<RoomVolume>> _byPlayer = new Dictionary<int, HashSet<RoomVolume>>();

        [Tooltip("Coop : la carte de chaque joueur montre aussi ce que les autres ont découvert.")]
        [SerializeField] private bool sharedInCoop = true;

        public bool SharedInCoop { get => sharedInCoop; set => sharedInCoop = value; }

        public static MapDiscovery Current { get; private set; }
        public int KnownCount => _known.Count;
        public static event System.Action<RoomVolume> Discovered;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        private int _step;

        private void FixedUpdate()
        {
            if (++_step % 5 != 0)
                return;                                                 // 10 fois par seconde suffisent
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                    DiscoverAt(p.transform.position + Vector3.up * 0.5f, p.PlayerId);
        }

        /// <summary>Découvre tous les volumes qui contiennent ce point ; renvoie le nombre de nouveaux.</summary>
        public int DiscoverAt(Vector3 point, int playerId = int.MinValue)
        {
            int added = 0;
            HashSet<RoomVolume> mine = null;
            if (playerId != int.MinValue && !_byPlayer.TryGetValue(playerId, out mine))
                _byPlayer[playerId] = mine = new HashSet<RoomVolume>();
            foreach (RoomVolume r in RoomVolume.All)
                if (r != null && r.Contains(point))
                {
                    mine?.Add(r);
                    if (_known.Add(r))
                    {
                        added++;
                        Discovered?.Invoke(r);
                    }
                }

            return added;
        }

        /// <summary>Ce volume est-il sur la carte de ce joueur ? Coop partagée : ce que l'équipe a vu ; sinon : ce qu'il a vu lui-même.</summary>
        public bool IsKnownFor(RoomVolume r, int playerId) =>
            r != null && (sharedInCoop || playerId == int.MinValue ? _known.Contains(r) : _byPlayer.TryGetValue(playerId, out var mine) && mine.Contains(r));

        /// <summary>Nombre de volumes sur la carte de ce joueur (repeint quand il change).</summary>
        public int KnownCountFor(int playerId) =>
            sharedInCoop || playerId == int.MinValue ? _known.Count : _byPlayer.TryGetValue(playerId, out var mine) ? mine.Count : 0;

        /// <summary>Ce joueur a-t-il lui-même découvert ce volume ?</summary>
        public bool DiscoveredBy(RoomVolume r, int playerId) => r != null && _byPlayer.TryGetValue(playerId, out var mine) && mine.Contains(r);

        /// <summary>Un point est-il dans un volume de la carte de ce joueur ?</summary>
        public bool IsKnownPointFor(Vector3 point, int playerId)
        {
            foreach (RoomVolume r in _known)
                if (r != null && r.Contains(point) && IsKnownFor(r, playerId))
                    return true;
            return false;
        }

        public bool IsKnown(RoomVolume r) => r != null && _known.Contains(r);

        /// <summary>Un point (coffre, porte…) est-il dans un volume découvert ?</summary>
        public bool IsKnownPoint(Vector3 point)
        {
            foreach (RoomVolume r in _known)
                if (r != null && r.Contains(point))
                    return true;
            return false;
        }

        /// <summary>Remise à zéro (nouvelle partie du niveau).</summary>
        public void Clear()
        {
            _known.Clear();
            _byPlayer.Clear();
        }
    }
}
