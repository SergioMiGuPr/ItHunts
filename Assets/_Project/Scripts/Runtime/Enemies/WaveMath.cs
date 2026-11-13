using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Calculs des vagues (D-041), purs pour les tests : tendance, pics, nombre voulu, rythme, distance, reconstitution.</summary>
    public static class WaveMath
    {
        /// <summary>Tendance à cette part du timer : interpolation linéaire entre les points, bornée aux extrémités.</summary>
        public static float Trend(float fraction, float[] times, int[] values)
        {
            if (times == null || values == null || values.Length == 0)
                return 0f;
            int n = Mathf.Min(times.Length, values.Length);
            if (n == 1 || fraction <= times[0])
                return values[0];
            for (int i = 1; i < n; i++)
                if (fraction <= times[i])
                {
                    float t = Mathf.InverseLerp(times[i - 1], times[i], fraction);
                    return Mathf.Lerp(values[i - 1], values[i], t);
                }

            return values[n - 1];
        }

        /// <summary>Niveau de la tendance (pic) : celui du moment, ou celui de l'Enragé après le timer.</summary>
        public static float Level(float elapsed, float timer, int[] trend, int enraged, WaveSettings s) =>
            elapsed >= timer ? enraged : Trend(timer > 0f ? elapsed / timer : 1f, s.trendTimes, trend);

        /// <summary>
        /// Pics et accalmies : la zone commence par une accalmie (lullDuration), puis un pic (peakDuration), et ainsi de suite.
        /// Renvoie vrai pendant un pic ; phaseLeft : temps restant dans la phase ; index : numéro du pic (0 avant le premier).
        /// </summary>
        public static bool InPeak(float elapsed, WaveSettings s, out float phaseLeft, out int index)
        {
            float cycle = s.lullDuration + s.peakDuration;
            int k = Mathf.FloorToInt(Mathf.Max(0f, elapsed) / cycle);
            float pos = Mathf.Max(0f, elapsed) - k * cycle;
            bool peak = pos >= s.lullDuration;
            phaseLeft = peak ? cycle - pos : s.lullDuration - pos;
            index = peak ? k + 1 : k;
            return peak;
        }

        /// <summary>Nombre voulu : la tendance pendant un pic, lullShare de la tendance pendant une accalmie (arrondi).</summary>
        public static int Wanted(float level, bool peak, WaveSettings s) =>
            Mathf.RoundToInt(peak ? level : level * s.lullShare);

        /// <summary>Intervalle entre deux apparitions : du début à la fin du timer, puis celui de l'Enragé.</summary>
        public static float SpawnInterval(float elapsed, float timer, WaveSettings s) =>
            elapsed >= timer ? s.spawnIntervalEnraged
                : Mathf.Lerp(s.spawnIntervalStart, s.spawnIntervalEnd, timer > 0f ? Mathf.Clamp01(elapsed / timer) : 1f);

        /// <summary>Plafond de chasse suivant la tendance : niveau du moment / niveau à la fin du timer (1 sans réglage).</summary>
        public static float HuntingScale(float level, int[] trend, WaveSettings s)
        {
            if (!s.scaleHuntingCaps || trend == null || trend.Length == 0)
                return 1f;
            float top = trend[trend.Length - 1];
            return top > 0f ? level / top : 1f;
        }

        /// <summary>Plafond mis à l'échelle (au moins 1 si le réglage n'est pas 0).</summary>
        public static int ScaledCap(int cap, float scale) => cap <= 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(cap * scale));

        /// <summary>Point à bonne distance : au moins min de tous les joueurs, au plus max du plus proche.</summary>
        public static bool DistanceOk(Vector3 point, IReadOnlyList<Vector3> players, float min, float max)
        {
            if (players == null || players.Count == 0)
                return false;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < players.Count; i++)
                nearest = Mathf.Min(nearest, Vector3.Distance(point, players[i]));
            return nearest >= min && nearest <= max;
        }

        /// <summary>Apparition normale : seulement si les présents (et ceux qui arrivent) restent sous le voulu moins la dette.</summary>
        public static bool NormalSpawnAllowed(int present, int pending, int wanted, int debt) => present + pending < wanted - debt;

        /// <summary>Reconstitution (ennemis tués) : une à la fois, seulement si la dette est là et qu'on reste sous le voulu.</summary>
        public static bool RefillAllowed(int present, int pending, int wanted, int debt) => debt > 0 && present + pending < wanted;

        /// <summary>La dette (ennemis tués à reconstituer) ne dépasse jamais ce qu'il manque pour atteindre le voulu.</summary>
        public static int ClampDebt(int debt, int present, int pending, int wanted) =>
            Mathf.Clamp(debt, 0, Mathf.Max(0, wanted - present - pending));

        /// <summary>Coop : multiplicateur du nombre voulu et des plafonds (1 + part × joueurs vivants en plus).</summary>
        public static float PlayerMultiplier(int alivePlayers, WaveSettings s) => 1f + s.extraPlayerShare * Mathf.Max(0, alivePlayers - 1);

        /// <summary>Plafond de chasse sans l'aggro : × lullHuntingShare pendant une accalmie.</summary>
        public static float PackShare(bool peak, WaveSettings s) => peak ? 1f : s.lullHuntingShare;

        /// <summary>Part de chaque joueur dans le nombre voulu (le reste aux premiers).</summary>
        public static int[] Shares(int wanted, int players)
        {
            if (players <= 0)
                return new int[0];
            var shares = new int[players];
            for (int i = 0; i < players; i++)
                shares[i] = wanted / players + (i < wanted % players ? 1 : 0);
            return shares;
        }

        /// <summary>
        /// À tour de rôle : à partir de start, le premier joueur à qui il manque des ennemis (part − les siens − ceux qui
        /// arrivent pour lui) ; -1 si personne.
        /// </summary>
        public static int NextPlayer(int[] deficits, int start)
        {
            int n = deficits.Length;
            for (int k = 0; k < n; k++)
            {
                int i = ((start % n) + n + k) % n;
                if (deficits[i] > 0)
                    return i;
            }

            return -1;
        }

        /// <summary>Point pour le joueur i : à minDistance au moins de tous les joueurs, à maxDistance au plus du joueur i.</summary>
        public static bool DistanceOkFor(Vector3 point, IReadOnlyList<Vector3> players, int index, float min, float max)
        {
            if (players == null || index < 0 || index >= players.Count)
                return false;
            for (int i = 0; i < players.Count; i++)
                if (Vector3.Distance(point, players[i]) < min)
                    return false;
            return Vector3.Distance(point, players[index]) <= max;
        }

        /// <summary>Hors du champ de vision : au-delà de la moitié du champ horizontal (plus une marge) autour du regard.</summary>
        public static bool OutsideView(Vector3 eye, Vector3 forward, Vector3 point, float verticalFov, float aspect, float marginDeg)
        {
            float half = Mathf.Atan(Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) * Mathf.Max(1f, aspect)) * Mathf.Rad2Deg;
            return Vector3.Angle(forward, point - eye) > half + marginDeg;
        }
    }
}
