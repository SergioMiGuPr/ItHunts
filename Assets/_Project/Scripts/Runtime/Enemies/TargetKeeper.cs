using System;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Choix de cible d'une goule ou d'un volant en coop (D-041) : au départ, le joueur le plus proche par le chemin ; ensuite il
    /// garde sa cible et ne change que si un autre joueur est plus proche d'au moins targetSwitchMargin, ou si sa cible est à plus
    /// de targetGiveUpDistance, et au moins targetSwitchCooldown après son dernier changement. Exceptions immédiates : sa cible
    /// meurt, ou un autre joueur le blesse ou le lance (il se retourne contre lui). Un joueur mort ne compte pas.
    /// </summary>
    public sealed class TargetKeeper
    {
        private float _lastSwitch = float.NegativeInfinity;
        private float _nextEval;
        private float _attackSeen = float.NegativeInfinity;

        public PlayerHealth Target { get; private set; }
        /// <summary>Changements de cible (pas le premier choix) : mesures.</summary>
        public int Switches { get; private set; }
        public int AttackSwitches { get; private set; }

        public void Clear()
        {
            Target = null;
            _nextEval = 0f;
            _lastSwitch = float.NegativeInfinity;
        }

        /// <summary>Revoit la cible ; length : longueur du chemin vers un joueur (appelée seulement s'il y a plusieurs joueurs).</summary>
        public PlayerHealth Update(float now, Damageable self, EnemySettings cfg, float interval, Func<PlayerHealth, float> length)
        {
            // Exception : un autre joueur l'a blessé ou lancé → il se retourne contre lui, tout de suite.
            if (self != null && self.LastAttackTime > _attackSeen)
            {
                _attackSeen = self.LastAttackTime;
                PlayerHealth attacker = PlayerHealth.Find(self.LastAttackerId);
                if (attacker != null && !attacker.Dead && attacker != Target)
                {
                    if (Target != null && !Target.Dead)
                    {
                        Switches++;
                        AttackSwitches++;
                    }

                    Target = attacker;
                    _lastSwitch = now;
                    _nextEval = now + interval;
                    return Target;
                }
            }

            bool lost = Target == null || Target.Dead || !Target.isActiveAndEnabled;
            if (!lost && now < _nextEval)
                return Target;
            _nextEval = now + interval;

            int alive = 0;
            PlayerHealth only = null;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                {
                    alive++;
                    only = p;
                }

            if (alive <= 1)
            {
                if (only != Target)
                {
                    Target = only;
                    _lastSwitch = now;
                }

                return Target;
            }

            float current = float.PositiveInfinity, best = float.PositiveInfinity;
            PlayerHealth bestP = null;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                float l = length(p);
                if (p == Target)
                    current = l;
                else if (l < best)
                {
                    best = l;
                    bestP = p;
                }
            }

            if (lost)
            {
                Target = bestP;                                          // sa cible est morte (ou aucune) : la plus proche
                _lastSwitch = now;
                return Target;
            }

            if (bestP != null && EnemyMath.ShouldSwitchTarget(current, best, now - _lastSwitch, cfg))
            {
                Target = bestP;
                _lastSwitch = now;
                Switches++;
            }

            return Target;
        }
    }
}
