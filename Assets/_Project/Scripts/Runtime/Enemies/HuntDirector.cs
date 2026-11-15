using System.Collections.Generic;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// État de chasse d'un ennemi (D-061, remplace les plafonds de chasse de D-037 et D-038) :
    /// - en attente tant qu'aucun joueur ne passe dans sa zone d'aggro ;
    /// - aggro (sans limite de nombre) : un joueur debout est vu à moins de aggroSightRange, ou à moins de aggroProximity même
    ///   sans le voir ; il le poursuit jusqu'à être semé (plus aucun joueur à moins de aggroLoseDistance pendant aggroLoseTime) ;
    /// - chasseur (HuntDirector) : il sait toujours où est son joueur et ne le lâche pas tant qu'il est debout.
    /// Pur pour les tests ; l'ennemi fournit ce qu'il voit.
    /// </summary>
    public sealed class HuntState
    {
        private float _farSince = float.NegativeInfinity;

        public bool Aggroed { get; private set; }
        /// <summary>Chasseur de ce joueur (null : pas chasseur).</summary>
        public PlayerHealth ChaserFor { get; private set; }
        public bool Hunting => Aggroed || ChaserFor != null;
        public int AggroWakes { get; private set; }
        /// <summary>Semé : aggro perdue (mesures).</summary>
        public int Losses { get; private set; }

        public void Clear()
        {
            Aggroed = false;
            ChaserFor = null;
            _farSince = float.NegativeInfinity;
        }

        /// <summary>
        /// Aggro (toutes les 0.2 s environ). seen : un joueur debout dans sa zone d'aggro ; nearest : distance au joueur debout
        /// le plus proche. Un chasseur dont le joueur tombe redevient un ennemi en aggro ordinaire.
        /// </summary>
        public void Step(bool seen, float nearest, float now, float loseDistance, float loseTime)
        {
            if (!ReferenceEquals(ChaserFor, null) && (ChaserFor == null || ChaserFor.Dead))
                Release();
            if (!ReferenceEquals(ChaserFor, null))
                return;
            if (!Aggroed)
            {
                if (seen)
                {
                    Aggroed = true;
                    AggroWakes++;
                    _farSince = float.NegativeInfinity;
                }

                return;
            }

            if (seen || nearest <= loseDistance)
            {
                _farSince = float.NegativeInfinity;
                return;
            }

            if (float.IsNegativeInfinity(_farSince))
                _farSince = now;
            if (now - _farSince < loseTime)
                return;
            Aggroed = false;                                             // semé : il attend là où il est
            Losses++;
            _farSince = float.NegativeInfinity;
        }

        /// <summary>Devient chasseur de ce joueur (HuntDirector).</summary>
        public void MakeChaser(PlayerHealth p)
        {
            ChaserFor = p;
            Aggroed = false;
            _farSince = float.NegativeInfinity;
        }

        /// <summary>N'est plus chasseur : il garde l'aggro (et peut être semé ensuite).</summary>
        public void Release()
        {
            if (!ReferenceEquals(ChaserFor, null))
            {
                ChaserFor = null;
                Aggroed = true;
                _farSince = float.NegativeInfinity;
            }
        }
    }

    /// <summary>Un ennemi qui chasse (goule, volant) : vu par HuntDirector.</summary>
    public interface IHunter
    {
        HuntState Hunt { get; }
        /// <summary>Actif, vivant, la zone éveillée.</summary>
        bool CanHunt { get; }
        Vector3 Position { get; }
        /// <summary>Longueur du chemin jusqu'à ce joueur (à défaut, une estimation).</summary>
        float PathLength(PlayerHealth p);
    }

    /// <summary>
    /// Chasseurs (D-061) : en plus de l'aggro, un petit groupe d'ennemis par joueur debout sait toujours où il est et le
    /// poursuit (pression continue). Leur nombre par joueur monte avec le temps de la zone (paliers de WaveSettings). Revu toutes
    /// les 0.5 s : s'il en manque (un chasseur est mort), l'ennemi en attente le plus proche du joueur par le chemin prend le
    /// relais (à défaut, un ennemi déjà en aggro). Réseau (étape 11) : sur l'autorité.
    /// </summary>
    public static class HuntDirector
    {
        private static readonly List<IHunter> Hunters = new List<IHunter>();
        private static readonly List<(IHunter h, float d)> Candidates = new List<(IHunter, float)>();

        /// <summary>Relais pris (un chasseur en plus pour un joueur) : mesures.</summary>
        public static int Assignments { get; private set; }
        public static int ChaserCount { get; private set; }
        public static int AggroCount { get; private set; }
        /// <summary>Chasseurs voulus par joueur au dernier passage.</summary>
        public static int PerPlayer { get; private set; }

        /// <summary>Tous les ennemis qui chassent (goules et volants actifs).</summary>
        public static List<IHunter> Gather()
        {
            Hunters.Clear();
            foreach (Ghoul g in Ghoul.All)
                if (g != null)
                    Hunters.Add(g);
            foreach (Flyer f in Flyer.All)
                if (f != null)
                    Hunters.Add(f);
            return Hunters;
        }

        public static int ChasersOf(PlayerHealth p, IReadOnlyList<IHunter> hunters)
        {
            int n = 0;
            foreach (IHunter h in hunters)
                if (h.CanHunt && h.Hunt.ChaserFor == p)
                    n++;
            return n;
        }

        /// <summary>Remet les chasseurs à jour pour ces joueurs (perPlayer chacun ; 0 : plus aucun chasseur).</summary>
        public static void Tick(IReadOnlyList<PlayerHealth> players, int perPlayer, IReadOnlyList<IHunter> hunters)
        {
            PerPlayer = perPlayer;
            foreach (IHunter h in hunters)
            {
                PlayerHealth c = h.Hunt.ChaserFor;
                if (!ReferenceEquals(c, null) && (c == null || c.Dead || perPlayer <= 0))
                    h.Hunt.Release();                                    // son joueur est tombé : aggro ordinaire
            }

            foreach (PlayerHealth p in players)
            {
                if (p == null || p.Dead)
                    continue;
                int have = ChasersOf(p, hunters);
                while (have > perPlayer)
                {
                    IHunter far = null;
                    float best = -1f;
                    foreach (IHunter h in hunters)
                        if (h.CanHunt && h.Hunt.ChaserFor == p && Vector3.Distance(h.Position, p.transform.position) > best)
                        {
                            best = Vector3.Distance(h.Position, p.transform.position);
                            far = h;
                        }

                    if (far == null)
                        break;
                    far.Hunt.Release();
                    have--;
                }

                for (int i = have; i < perPlayer; i++)
                {
                    IHunter pick = Best(p, hunters, waitingOnly: true) ?? Best(p, hunters, waitingOnly: false);
                    if (pick == null)
                        break;
                    pick.Hunt.MakeChaser(p);
                    Assignments++;
                }
            }

            int chasers = 0, aggro = 0;
            foreach (IHunter h in hunters)
            {
                if (!h.CanHunt)
                    continue;
                if (h.Hunt.ChaserFor != null)
                    chasers++;
                else if (h.Hunt.Aggroed)
                    aggro++;
            }

            ChaserCount = chasers;
            AggroCount = aggro;
        }

        /// <summary>Le plus proche du joueur par le chemin, parmi les 6 plus proches à vol d'oiseau.</summary>
        private static IHunter Best(PlayerHealth p, IReadOnlyList<IHunter> hunters, bool waitingOnly)
        {
            Candidates.Clear();
            foreach (IHunter h in hunters)
                if (h.CanHunt && h.Hunt.ChaserFor == null && (!waitingOnly || !h.Hunt.Aggroed))
                    Candidates.Add((h, Vector3.Distance(h.Position, p.transform.position)));
            if (Candidates.Count == 0)
                return null;
            Candidates.Sort((a, b) => a.d.CompareTo(b.d));
            IHunter best = null;
            float bestLen = float.PositiveInfinity;
            for (int i = 0; i < Mathf.Min(6, Candidates.Count); i++)
            {
                float len = Candidates[i].h.PathLength(p);
                if (len < bestLen)
                {
                    bestLen = len;
                    best = Candidates[i].h;
                }
            }

            return best;
        }

        /// <summary>Nouvelle zone : compteurs à zéro.</summary>
        public static void ResetCounters()
        {
            Assignments = ChaserCount = AggroCount = PerPlayer = 0;
        }
    }
}
