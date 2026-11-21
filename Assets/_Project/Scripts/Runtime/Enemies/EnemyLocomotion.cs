using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Déplacement au sol d'un ennemi, à pas fixe (D-033) : chemin sur le NavMesh de son type (recalculé à intervalle),
    /// cap qui tourne au plus à turnRate, vitesse qui suit la vitesse voulue avec une accélération, position ramenée sur
    /// le NavMesh à chaque pas (il glisse le long des murs au lieu de les traverser). Corps cinématique déplacé à la main
    /// (comme EnemyBody) : rien à voir avec NavMeshAgent, pour garder le contrôle des vitesses et du pas fixe (réseau).
    /// </summary>
    public sealed class EnemyLocomotion
    {
        private NavMeshPath _path;               // créé au premier chemin (interdit dans un initialiseur de MonoBehaviour)
        private readonly Vector3[] _corners = new Vector3[64];
        private int _count;
        private int _index;
        private float _nextRepath = float.NegativeInfinity;

        public const int NoAgent = int.MinValue;
        public int AgentType = NoAgent;             // les types créés à l'exécution ont des identifiants négatifs
        public Vector3 Heading = Vector3.forward;
        public float Speed;
        public Vector3 Destination { get; private set; }
        /// <summary>Dernier chemin partiel : le but n'est pas atteignable (ou trop loin pour le calcul de chemin).</summary>
        public bool Partial { get; private set; }
        /// <summary>Dernier coin du chemin calculé.</summary>
        public Vector3 PathEnd => _count > 0 ? _corners[_count - 1] : Destination;
        public bool HasPath => _count > 0 && _index < _count;
        public Vector3 Velocity => Heading * Speed;
        /// <summary>Coin du chemin vers lequel il va (valable si HasPath).</summary>
        public Vector3 CurrentCorner => HasPath ? _corners[_index] : Destination;

        /// <summary>Coin actuel et suivant du chemin (passages du traqueur : un lien du NavMesh est entre deux coins).</summary>
        public bool PeekCorners(out Vector3 current, out Vector3 next)
        {
            current = next = Vector3.zero;
            if (!HasPath || _index + 1 >= _count)
                return false;
            current = _corners[_index];
            next = _corners[_index + 1];
            return true;
        }
        /// <summary>Passe au coin suivant (fin d'un passage par un mur).</summary>
        public void Advance()
        {
            if (HasPath)
                _index++;
        }

        private NavMeshQueryFilter Filter => new NavMeshQueryFilter { agentTypeID = AgentType, areaMask = NavMesh.AllAreas };

        public void Clear()
        {
            _count = 0;
            _index = 0;
            Speed = 0f;
            _nextRepath = float.NegativeInfinity;
        }

        /// <summary>Nouveau chemin vers <paramref name="to"/> si l'intervalle est passé (ou forcé). Faux si aucun chemin.</summary>
        /// <summary>Debug : prochain recalcul permis, et résultat du dernier appel.</summary>
        public float NextRepath => _nextRepath;
        public string LastSet { get; private set; } = "";

        public bool SetDestination(Vector3 from, Vector3 to, float now, float interval, bool force = false)
        {
            if (!force && now < _nextRepath)
            {
                LastSet = "attente";
                return HasPath;
            }
            _nextRepath = now + interval;
            if (AgentType == NoAgent)
                return false;
            if (!NavMesh.SamplePosition(from, out NavMeshHit a, 3f, Filter) || !NavMesh.SamplePosition(to, out NavMeshHit b, 6f, Filter))
            {
                _count = 0;
                LastSet = "hors NavMesh";
                return false;
            }

            Destination = b.position;
            _path ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(a.position, b.position, Filter, _path) || _path.status == NavMeshPathStatus.PathInvalid)
            {
                _count = 0;
                Partial = true;
                return false;
            }

            Partial = _path.status == NavMeshPathStatus.PathPartial;
            LastSet = _path.status.ToString();
            _count = _path.GetCornersNonAlloc(_corners);
            _index = _count > 1 ? 1 : 0;
            return HasPath;
        }

        private NavMeshPath _probe;

        /// <summary>
        /// Chemin d'essai (sans le suivre) : longueur et direction du premier tronçon. Faux s'il n'y a pas de chemin complet.
        /// </summary>
        public bool Probe(Vector3 from, Vector3 to, out float length, out Vector3 firstLeg)
        {
            length = float.PositiveInfinity;
            firstLeg = Vector3.zero;
            if (AgentType == NoAgent || !NavMesh.SamplePosition(from, out NavMeshHit a, 4f, Filter)
                || !NavMesh.SamplePosition(to, out NavMeshHit b, 4f, Filter))
                return false;
            _probe ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(a.position, b.position, Filter, _probe) || _probe.status != NavMeshPathStatus.PathComplete)
                return false;
            int n = _probe.GetCornersNonAlloc(_corners2);
            length = 0f;
            for (int i = 1; i < n; i++)
                length += Vector3.Distance(_corners2[i - 1], _corners2[i]);
            if (n > 1)
            {
                firstLeg = _corners2[1] - _corners2[0];
                firstLeg.y = 0f;
            }

            return true;
        }

        private readonly Vector3[] _corners2 = new Vector3[64];

        /// <summary>Point du NavMesh le plus proche (faux s'il n'y en a pas à moins de radius).</summary>
        public bool Snap(Vector3 position, float radius, out Vector3 onMesh)
        {
            onMesh = position;
            if (AgentType == NoAgent || !NavMesh.SamplePosition(position, out NavMeshHit hit, radius, Filter))
                return false;
            onMesh = hit.position;
            return true;
        }

        /// <summary>Distance restante le long du chemin (m).</summary>
        public float RemainingDistance(Vector3 position)
        {
            if (!HasPath)
                return 0f;
            float d = Vector3.Distance(position, _corners[_index]);
            for (int i = _index + 1; i < _count; i++)
                d += Vector3.Distance(_corners[i - 1], _corners[i]);
            return d;
        }

        /// <summary>
        /// Un pas : avance vers le coin suivant du chemin à la vitesse voulue ; renvoie la nouvelle position (pieds, sur le
        /// NavMesh). Arrivé (ou sans chemin) : il freine sur place.
        /// </summary>
        /// <summary>Multiplicateur de vitesse (difficulté, D-048) : goules et volants ; 1 pour le traqueur (réglé avec la traction).</summary>
        public float SpeedScale = 1f;
        /// <summary>
        /// Vrai (goules) : un pas qui sortirait du NavMesh (bord d'une fosse, d'un étage) est refusé ; elle reste où elle est.
        /// Faux (traqueur) : le pas est gardé tel quel.
        /// </summary>
        public bool StayOnMesh;

        public Vector3 Step(Vector3 position, float targetSpeed, float acceleration, float turnRate, float dt)
        {
            targetSpeed *= SpeedScale;
            Vector3 desired = Vector3.zero;
            while (HasPath)
            {
                Vector3 to = _corners[_index] - position;
                to.y = 0f;
                if (to.magnitude > 0.35f)
                {
                    desired = to.normalized;
                    break;
                }

                _index++;
            }

            if (desired == Vector3.zero)
                targetSpeed = 0f;
            else
                Heading = Vector3.RotateTowards(Heading, desired, turnRate * Mathf.Deg2Rad * dt, 0f).normalized;

            // Virage serré : il ralentit tant qu'il ne fait pas face à sa direction (on lui échappe en tournant court).
            float facing = desired == Vector3.zero ? 1f : Mathf.Clamp01(Vector3.Dot(Heading, desired));
            float wanted = targetSpeed * Mathf.Lerp(0.3f, 1f, facing);
            Speed = Mathf.MoveTowards(Speed, wanted, acceleration * dt);
            Vector3 next = position + Heading * (Speed * dt);
            if (Snap(next, 1.5f, out Vector3 onMesh))
            {
                // Ramené sur le NavMesh : s'il a été bloqué (mur), sa vitesse suit ce qu'il a vraiment parcouru.
                Vector3 moved = onMesh - position;
                moved.y = 0f;
                if (Speed > 0.01f && moved.magnitude < Speed * dt * 0.5f)
                    Speed = moved.magnitude / dt;
                return onMesh;
            }

            if (StayOnMesh)
            {
                Speed = 0f;
                return position;
            }

            return next;
        }
    }
}
