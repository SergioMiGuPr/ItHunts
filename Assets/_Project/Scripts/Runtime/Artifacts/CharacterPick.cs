namespace ItHunts.Artifacts
{
    /// <summary>
    /// Choix des personnages (D-064), logique pure : chaque joueur a un personnage jouable, et deux joueurs n'ont jamais le même.
    /// Le menu fait défiler les personnages libres ; un joueur qui n'a rien choisi (scène de test, 2e joueur qui rejoint en jeu)
    /// prend le personnage par défaut s'il est libre, sinon le premier personnage jouable libre.
    /// </summary>
    public static class CharacterPick
    {
        /// <summary>
        /// Personnage suivant dans le sens dir (+1 / −1) à partir de current, en sautant ceux qui ne sont pas jouables et ceux pris
        /// par les autres joueurs (taken, indices ; −1 ignoré). Renvoie current s'il n'y en a pas d'autre, −1 si aucun n'est libre.
        /// </summary>
        public static int Cycle(bool[] selectable, int current, int dir, int[] taken)
        {
            int n = selectable?.Length ?? 0;
            if (n == 0)
                return -1;
            int step = dir >= 0 ? 1 : -1;
            int start = current >= 0 && current < n ? current : (step > 0 ? n - 1 : 0);
            for (int k = 1; k <= n; k++)
            {
                int i = ((start + step * k) % n + n) % n;
                if (selectable[i] && !Contains(taken, i))
                    return i;
            }

            return current >= 0 && current < n && selectable[current] && !Contains(taken, current) ? current : -1;
        }

        /// <summary>Premier personnage libre : preferred s'il est jouable et libre, sinon le premier jouable libre (−1 si aucun).</summary>
        public static int FirstFree(bool[] selectable, int preferred, int[] taken)
        {
            int n = selectable?.Length ?? 0;
            if (preferred >= 0 && preferred < n && selectable[preferred] && !Contains(taken, preferred))
                return preferred;
            for (int i = 0; i < n; i++)
                if (selectable[i] && !Contains(taken, i))
                    return i;
            return -1;
        }

        /// <summary>
        /// Personnages de tous les joueurs (0 à count − 1) : le choix de chacun (requested[i], −1 ou absent : rien choisi) est gardé
        /// s'il est jouable et pas déjà pris par un joueur d'index plus petit ; sinon FirstFree. Plus de joueurs que de personnages
        /// jouables : les derniers reçoivent −1 (pas d'artefact).
        /// </summary>
        public static int[] Assign(bool[] selectable, int[] requested, int count, int defaultIndex)
        {
            var result = new int[count];
            for (int p = 0; p < count; p++)
            {
                int[] taken = Prefix(result, p);
                int want = requested != null && p < requested.Length ? requested[p] : -1;
                result[p] = want >= 0 && want < (selectable?.Length ?? 0) && selectable[want] && !Contains(taken, want)
                    ? want
                    : FirstFree(selectable, defaultIndex, taken);
            }

            return result;
        }

        private static int[] Prefix(int[] a, int length)
        {
            var r = new int[length];
            System.Array.Copy(a, r, length);
            return r;
        }

        private static bool Contains(int[] a, int v)
        {
            if (a == null || v < 0)
                return false;
            foreach (int x in a)
                if (x == v)
                    return true;
            return false;
        }
    }
}
