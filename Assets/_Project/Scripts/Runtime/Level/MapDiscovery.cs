using System.Collections.Generic;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Brouillard de guerre de la minimap (D-053), état du niveau (pas d'un joueur) : une salle ou un tronçon de couloir est
    /// découvert dès qu'un joueur vivant y entre ; les découvertes sont communes à tous les joueurs (coop). Les points clés
    /// (coffres, marchand, salle verrouillée, sortie, départ) s'affichent quand le volume qui les contient est découvert.
    /// Réseau (étape 11) : découvertes envoyées par l'hôte (événement Discovered).
    /// </summary>
    public sealed class MapDiscovery : MonoBehaviour
    {
        private readonly HashSet<RoomVolume> _known = new HashSet<RoomVolume>();

        public static MapDiscovery Current { get; private set; }
        public int KnownCount => _known.Count;
        public static event System.Action<RoomVolume> Discovered;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        private void FixedUpdate()
        {
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                    DiscoverAt(p.transform.position + Vector3.up * 0.5f);
        }

        /// <summary>Découvre tous les volumes qui contiennent ce point ; renvoie le nombre de nouveaux.</summary>
        public int DiscoverAt(Vector3 point)
        {
            int added = 0;
            foreach (RoomVolume r in RoomVolume.All)
                if (r != null && r.Contains(point) && _known.Add(r))
                {
                    added++;
                    Discovered?.Invoke(r);
                }

            return added;
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
        public void Clear() => _known.Clear();
    }
}
