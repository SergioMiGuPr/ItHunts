using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>Catégorie d'achat : chacune a sa hausse de prix à chaque achat, comptée par joueur (D-043).</summary>
    public enum PriceKind
    {
        Coffre,
        Marchand,
        Porte
    }

    /// <summary>Calculs de l'économie (D-043), purs pour les tests : âmes à la mort, prix, tirages.</summary>
    public static class SoulMath
    {
        /// <summary>
        /// Un joueur y est-il pour quelque chose ? Dernier joueur à l'avoir blessé, attrapé, lancé ou repoussé (ou un objet qu'il a
        /// lancé, repoussé ou décroché), dans les involvementWindow dernières secondes. Le traqueur ne compte jamais.
        /// </summary>
        public static bool Involved(int lastAttackerId, float lastAttackTime, float now, float window) =>
            lastAttackerId != 0 && now - lastAttackTime <= window;

        /// <summary>Âmes d'un ennemi à sa mort : sa valeur, × le multiplicateur en Enragé (au moment de la mort), arrondi.</summary>
        public static int SoulsAtDeath(int value, bool enraged, float enragedMultiplier) =>
            value <= 0 ? 0 : Mathf.RoundToInt(value * (enraged ? enragedMultiplier : 1f));

        /// <summary>
        /// Prix après purchases achats de ce joueur dans la même catégorie : chaque achat le monte de increasePercent % (composé),
        /// arrondi. 0 % : prix fixe. Un prix de base nul (coffre gratuit) reste nul.
        /// </summary>
        public static int RaisedPrice(int basePrice, float increasePercent, int purchases) =>
            basePrice <= 0 ? 0 : Mathf.RoundToInt(basePrice * Mathf.Pow(1f + Mathf.Max(0f, increasePercent) / 100f, Mathf.Max(0, purchases)));

        /// <summary>Achat : assez d'âmes ? (sinon refus, en rouge).</summary>
        public static bool CanAfford(int souls, int price) => price <= 0 || souls >= price;

        /// <summary>
        /// Tirage de count améliorations différentes (si le pool suffit) parmi poolSize, déterminé par la seed de la run et un
        /// numéro de tirage (coffre, marchand) : même seed, même tirage.
        /// </summary>
        public static int[] Draw(int seed, int drawIndex, int poolSize, int count)
        {
            if (poolSize <= 0 || count <= 0)
                return new int[0];
            var rng = new System.Random(unchecked(seed * 486187739 + drawIndex * 16777619 + 0x5EED));
            var order = new List<int>();
            for (int i = 0; i < poolSize; i++)
                order.Add(i);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            var result = new int[count];
            for (int i = 0; i < count; i++)
                result[i] = order[i % poolSize];
            return result;
        }

        /// <summary>Choix d'un emplacement (marchand) parmi count, par la seed de la run.</summary>
        public static int PickSpot(int seed, int count) =>
            count <= 0 ? -1 : new System.Random(unchecked(seed * 7349 + 0x3E3C)).Next(count);

        /// <summary>Clignotement des dernières secondes : visible ou pas à cet âge.</summary>
        public static bool BlinkVisible(float age, EconomySettings s)
        {
            float left = s.soulLifetime - age;
            if (left > s.soulBlinkTime)
                return true;
            float rate = Mathf.Lerp(8f, 3f, Mathf.Clamp01(left / Mathf.Max(0.01f, s.soulBlinkTime)));
            return Mathf.Repeat(age * rate, 1f) < 0.55f;
        }
    }
}
