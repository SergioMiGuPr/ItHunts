using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Plateforme qui ne tient qu'un instant (zone de parcours) : quand un joueur se pose dessus, elle tombe au bout de holdTime
    /// (collision et rendu coupés), puis revient après returnTime. On la traverse, on ne s'y arrête pas.
    /// </summary>
    public sealed class CrumblePlatform : MonoBehaviour
    {
        [Min(0.05f)] [SerializeField] private float holdTime = 0.6f;
        [Min(0.5f)] [SerializeField] private float returnTime = 4f;

        private Collider _solid;
        private Renderer[] _renderers;
        private float _touchedAt = float.PositiveInfinity, _backAt = float.PositiveInfinity;
        private static readonly List<CrumblePlatform> AllList = new List<CrumblePlatform>();

        public static IReadOnlyList<CrumblePlatform> All => AllList;
        public float HoldTime => holdTime;
        public bool Down { get; private set; }
        public int Crumbles { get; private set; }

        private void Awake()
        {
            foreach (Collider c in GetComponents<Collider>())
                if (!c.isTrigger)
                    _solid = c;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnEnable() => AllList.Add(this);
        private void OnDisable() => AllList.Remove(this);

        private void FixedUpdate()
        {
            float now = Time.time;
            if (!Down && float.IsPositiveInfinity(_touchedAt))
                foreach (PlayerHealth p in PlayerHealth.All)
                    if (p != null && !p.Dead && OnTop(p.transform.position))
                    {
                        _touchedAt = now;
                        break;
                    }

            if (!Down && now >= _touchedAt + holdTime)
                Set(true);
            if (Down && now >= _backAt)
                Set(false);
        }

        /// <summary>Pieds du joueur sur le dessus (marge de 0.4 m).</summary>
        public bool OnTop(Vector3 feet)
        {
            if (_solid == null)
                return false;
            Bounds b = _solid.bounds;
            return feet.x >= b.min.x - 0.2f && feet.x <= b.max.x + 0.2f && feet.z >= b.min.z - 0.2f && feet.z <= b.max.z + 0.2f
                   && feet.y >= b.max.y - 0.4f && feet.y <= b.max.y + 0.6f;
        }

        private void Set(bool down)
        {
            Down = down;
            if (down)
            {
                Crumbles++;
                _backAt = Time.time + returnTime;
            }
            else
            {
                _touchedAt = _backAt = float.PositiveInfinity;
            }

            if (_solid != null)
                _solid.enabled = !down;
            foreach (Renderer r in _renderers)
                r.enabled = !down;
        }
    }
}
