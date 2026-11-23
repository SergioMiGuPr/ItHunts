using ItHunts.Artifacts;
using ItHunts.Level;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Scaling des ennemis sur la puissance du joueur (nuit 4, point 3 ; recherche § 4 : RoR2, Vampire Survivors). La puissance d'un joueur
    /// = nombre d'améliorations possédées + somme de leurs rangs (0 à 4) ; l'équipe compte la plus forte. Les goules gagnent par point de
    /// puissance moins que le joueur n'en gagne (il doit se sentir plus fort), et par acte (le stuff est gardé) : PV +12 % et dégâts +6 % par
    /// point (exposant 0.9), PV ×1.25 et dégâts ×1.15 par acte, nombre +6 % par point et ×1.2 par acte. La vitesse n'est jamais scalée.
    /// Réglable dans EnemyScalingSettings (Resources/EnemyScaling_Default) ; calcul pur, testable.
    /// </summary>
    public static class EnemyScaling
    {
        /// <summary>Somme des rangs des améliorations d'un joueur (posée par le système des rangs ; 0 sans rangs).</summary>
        public static System.Func<int, int> RankSum;

        public static EnemyScalingSettings Settings
        {
            get
            {
                if (_settings == null)
                    _settings = Resources.Load<EnemyScalingSettings>("EnemyScaling_Default");
                return _settings;
            }
            set => _settings = value;
        }

        private static EnemyScalingSettings _settings;

        /// <summary>Puissance d'un joueur : améliorations possédées + somme des rangs.</summary>
        public static int PowerOf(int playerId)
        {
            RunState run = RunState.Current;
            int n = run != null ? run.UpgradesOf(playerId).Count : 0;
            int ranks = RankSum != null ? RankSum(playerId) : 0;
            return n + ranks;
        }

        /// <summary>Puissance de l'équipe : le joueur le plus fort.</summary>
        public static int TeamPower()
        {
            int best = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null)
                    best = Mathf.Max(best, PowerOf(p.PlayerId));
            return best;
        }

        public static float HealthMultiplier() => Health(TeamPower(), RunSession.Act, Settings);
        public static float DamageMultiplier() => Damage(TeamPower(), RunSession.Act, Settings);
        public static float CountMultiplier() => Count(TeamPower(), RunSession.Act, Settings);

        public static float Health(int power, int act, EnemyScalingSettings s)
        {
            float perPoint = s != null ? s.healthPerPower : 0.12f, perAct = s != null ? s.healthPerAct : 1.25f, exp = s != null ? s.powerExponent : 0.9f;
            return (1f + perPoint * Mathf.Pow(Mathf.Max(0, power), exp)) * Mathf.Pow(perAct, Mathf.Clamp(act, 0, 3));
        }

        public static float Damage(int power, int act, EnemyScalingSettings s)
        {
            float perPoint = s != null ? s.damagePerPower : 0.06f, perAct = s != null ? s.damagePerAct : 1.15f, exp = s != null ? s.powerExponent : 0.9f;
            return (1f + perPoint * Mathf.Pow(Mathf.Max(0, power), exp)) * Mathf.Pow(perAct, Mathf.Clamp(act, 0, 3));
        }

        public static float Count(int power, int act, EnemyScalingSettings s)
        {
            float perPoint = s != null ? s.countPerPower : 0.06f, perAct = s != null ? s.countPerAct : 1.2f;
            return Mathf.Min(s != null ? s.countMax : 2.5f, (1f + perPoint * Mathf.Max(0, power)) * Mathf.Pow(perAct, Mathf.Clamp(act, 0, 3)));
        }
    }
}
