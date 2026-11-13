using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Type de volume (D-045, D-047, D-048).</summary>
    public enum RoomKind
    {
        Salle,
        /// <summary>Tronçon de couloir (environ 40 m, réglable dans le constructeur) : bloque aussi les tronçons voisins.</summary>
        Couloir,
        /// <summary>Salle de départ (D-048) : interdite aux ennemis et au traqueur tant qu'aucun joueur n'en est sorti.</summary>
        Depart,
        /// <summary>Arène du combat de boss.</summary>
        Arene,
    }

    /// <summary>
    /// Volume d'une salle ou d'un tronçon de couloir (D-045, D-047) : une boîte (size, centrée sur cet objet, orientée comme
    /// lui, sans collider). Posé par les constructeurs (Test_Arena, niveau 1), et à poser à la main au level design : un par
    /// salle et un par tronçon de couloir. Sert aux vagues (jamais d'apparition dans la salle ou le tronçon d'un joueur, ni dans
    /// les tronçons voisins du sien), à la salle de départ et à la minimap (salles découvertes).
    /// </summary>
    public sealed class RoomVolume : MonoBehaviour
    {
        [Tooltip("Nom de la salle (minimap, debug).")]
        [SerializeField] private string roomName = "Salle";
        [Tooltip("Taille de l'intérieur (m), centrée sur cet objet : du sol au plafond.")]
        [SerializeField] private Vector3 size = new Vector3(20f, 8f, 20f);
        [Tooltip("Salle, tronçon de couloir (bloque aussi les tronçons voisins), salle de départ ou arène.")]
        [SerializeField] private RoomKind kind = RoomKind.Salle;

        /// <summary>Deux tronçons sont voisins si leurs boîtes se touchent à cette distance près (m).</summary>
        public const float NeighbourMargin = 1.5f;

        public static readonly List<RoomVolume> All = new List<RoomVolume>();
        private static int _version;
        private int _neighboursVersion = -1;
        private readonly List<RoomVolume> _neighbours = new List<RoomVolume>();

        public string RoomName => roomName;
        public Vector3 Size => size;
        public RoomKind Kind => kind;
        public bool IsCorridor => kind == RoomKind.Couloir;

        private void OnEnable()
        {
            All.Add(this);
            _version++;
        }

        private void OnDisable()
        {
            All.Remove(this);
            _version++;
        }

        /// <summary>Réglages (constructeurs, tests).</summary>
        public void Configure(string name, Vector3 interior, RoomKind roomKind = RoomKind.Salle)
        {
            roomName = name;
            size = interior;
            kind = roomKind;
            _version++;
        }

        /// <summary>Le point est-il dans la salle ? (0.5 m de marge sous le sol et au-dessus du plafond, pour les pieds.)</summary>
        public bool Contains(Vector3 p)
        {
            Vector3 local = transform.InverseTransformPoint(p);
            Vector3 h = size * 0.5f;
            return Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.z) <= h.z && Mathf.Abs(local.y) <= h.y + 0.5f;
        }

        /// <summary>Boîte alignée sur les axes du monde qui contient le volume.</summary>
        public Bounds WorldBounds()
        {
            Vector3 h = size * 0.5f;
            var b = new Bounds(transform.TransformPoint(-h), Vector3.zero);
            for (int i = 1; i < 8; i++)
                b.Encapsulate(transform.TransformPoint(new Vector3((i & 1) == 0 ? -h.x : h.x, (i & 2) == 0 ? -h.y : h.y, (i & 4) == 0 ? -h.z : h.z)));
            return b;
        }

        /// <summary>Tronçons de couloir qui touchent celui-ci (vide pour une salle). Recalculés quand les volumes changent.</summary>
        public IReadOnlyList<RoomVolume> CorridorNeighbours
        {
            get
            {
                if (_neighboursVersion == _version)
                    return _neighbours;
                _neighboursVersion = _version;
                _neighbours.Clear();
                if (!IsCorridor)
                    return _neighbours;
                Bounds mine = WorldBounds();
                mine.Expand(2f * NeighbourMargin);
                foreach (RoomVolume r in All)
                    if (r != null && r != this && r.IsCorridor && mine.Intersects(r.WorldBounds()))
                        _neighbours.Add(r);
                return _neighbours;
            }
        }

        /// <summary>La salle qui contient ce point (la plus petite s'il y en a plusieurs), sinon null.</summary>
        public static RoomVolume At(Vector3 p)
        {
            RoomVolume best = null;
            float bestVolume = float.PositiveInfinity;
            foreach (RoomVolume r in All)
            {
                if (r == null || !r.Contains(p))
                    continue;
                float v = r.size.x * r.size.y * r.size.z;
                if (v < bestVolume)
                {
                    bestVolume = v;
                    best = r;
                }
            }

            return best;
        }

        /// <summary>
        /// Un des points others est-il dans la même salle ou le même tronçon que point, ou dans un tronçon voisin de celui de
        /// point ? (Hors de tout volume : non.) Règle des apparitions (D-045, D-047).
        /// </summary>
        public static bool SharesRoomWithAny(Vector3 point, IReadOnlyList<Vector3> others)
        {
            if (others == null || others.Count == 0)
                return false;
            foreach (RoomVolume r in All)
            {
                if (r == null || !r.Contains(point))
                    continue;
                foreach (Vector3 o in others)
                {
                    if (r.Contains(o))
                        return true;
                    if (!r.IsCorridor)
                        continue;
                    foreach (RoomVolume n in r.CorridorNeighbours)
                        if (n != null && n.Contains(o))
                            return true;
                }
            }

            return false;
        }

        /// <summary>Le point est-il dans un volume de ce type ?</summary>
        public static bool InKind(Vector3 p, RoomKind roomKind)
        {
            foreach (RoomVolume r in All)
                if (r != null && r.kind == roomKind && r.Contains(p))
                    return true;
            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = kind switch
            {
                RoomKind.Couloir => new Color(0.5f, 1f, 0.5f, 0.3f),
                RoomKind.Depart => new Color(1f, 1f, 0.3f, 0.5f),
                RoomKind.Arene => new Color(1f, 0.3f, 0.3f, 0.5f),
                _ => new Color(0.3f, 0.8f, 1f, 0.35f),
            };
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
