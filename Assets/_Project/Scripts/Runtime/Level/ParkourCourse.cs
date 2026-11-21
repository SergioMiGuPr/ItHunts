using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Capacités de saut du joueur (sans traction), calculées depuis les réglages de mouvement : distances au sol plat.</summary>
    public struct ParkourCaps
    {
        public float RunSpeed, Gravity, FallMultiplier, JumpHeight, AirJumpHeight, DashDistance, DashExitSpeed, WallJumpHeight;
        public bool AirJump;

        public static ParkourCaps From(MovementSettings m)
        {
            if (m == null)
                return Default;
            return new ParkourCaps
            {
                RunSpeed = m.runSpeed, Gravity = m.gravity, FallMultiplier = m.fallGravityMultiplier, JumpHeight = m.jumpHeight,
                AirJumpHeight = m.airJumpHeight, AirJump = m.maxJumps >= 2, DashDistance = m.dashDistance,
                DashExitSpeed = m.dashExitSpeed, WallJumpHeight = m.wallJumpHeight,
            };
        }

        /// <summary>Valeurs de Movement_Default (au cas où le générateur n'a pas de réglages).</summary>
        public static ParkourCaps Default => new ParkourCaps
        {
            RunSpeed = 8.5f, Gravity = 20f, FallMultiplier = 1.5f, JumpHeight = 1.3f, AirJumpHeight = 1.6f, AirJump = true,
            DashDistance = 6.5f, DashExitSpeed = 12f, WallJumpHeight = 1.8f,
        };

        private float Up(float h) => Mathf.Sqrt(2f * Gravity * Mathf.Max(0f, h)) / Gravity;          // temps de montée
        private float Down(float h) => h <= 0f ? 0f : Mathf.Sqrt(2f * h / (Gravity * FallMultiplier)); // temps de chute

        /// <summary>Saut simple en course : distance horizontale pour arriver dy plus haut (négatif : plus bas) ; 0 si trop haut.</summary>
        public float Single(float dy = 0f) => dy > JumpHeight ? 0f : RunSpeed * (Up(JumpHeight) + Down(JumpHeight - dy));

        /// <summary>Double saut (le 2e au sommet du 1er) : distance pour arriver dy plus haut ; 0 si trop haut.</summary>
        public float Double(float dy = 0f)
        {
            if (!AirJump)
                return Single(dy);
            float top = JumpHeight + AirJumpHeight;
            return dy > top ? 0f : RunSpeed * (Up(JumpHeight) + Up(AirJumpHeight) + Down(top - dy));
        }

        /// <summary>Saut puis dash au sommet (sans double saut) : distance pour arriver dy plus haut ; 0 si trop haut.</summary>
        public float Dash(float dy = 0f) =>
            dy > JumpHeight ? 0f : RunSpeed * Up(JumpHeight) + DashDistance + DashExitSpeed * Down(JumpHeight - dy);

        /// <summary>Hauteur la plus grande atteinte sans mur : saut et double saut.</summary>
        public float MaxRise => JumpHeight + (AirJump ? AirJumpHeight : 0f);
    }

    /// <summary>Ce que demande un saut du parcours.</summary>
    public enum ParkourMove : byte
    {
        Simple,
        Double,
        Murs,       // enchaînement de sauts muraux (ou course sur le mur) entre deux murs, sans sol
        Dash,       // plus long qu'un double saut : dash ou dash-saut obligatoire
    }

    /// <summary>Une pièce du parcours, dans le repère du parcours : x le long du tracé, y vers le haut, z sur le côté.</summary>
    public struct ParkourPiece
    {
        public string Name;
        public Vector3 Center, Size;
        /// <summary>Plateforme qui tombe (hors de portée du traqueur : jamais une cachette).</summary>
        public bool Crumble;
        /// <summary>Mur sans dessus (du fond du vide au plafond) : pour les sauts muraux.</summary>
        public bool Wall;
        /// <summary>Socle du coffre de parcours.</summary>
        public bool Chest;
    }

    /// <summary>Un saut du parcours : écart bord à bord, dénivelé, mouvement demandé, et ce que le joueur peut faire.</summary>
    public struct ParkourJump
    {
        public string From, To;
        public float Gap, Rise;
        public ParkourMove Move;
        /// <summary>Distance la plus longue possible avec ce mouvement (et, pour Dash et Murs, distance du double saut, qui ne doit pas suffire).</summary>
        public float Capacity, DoubleReach;
        public bool Feasible => Capacity > 0f && Gap <= Capacity * ParkourCourse.Margin;
        public bool Demanding => Move != ParkourMove.Dash && Move != ParkourMove.Murs || Gap > DoubleReach;
    }

    /// <summary>
    /// Parcours exigeant et toujours faisable sans traction (point 4 de carte-suite) : poutre étroite, marches qui tombent, passage
    /// entre deux murs (sauts muraux en chaîne ou course sur le mur, plus long qu'un double saut), vide qui demande un dash, poteaux
    /// étroits qui tombent, poutre d'arrivée. Les écarts sont calculés depuis les capacités du joueur (ParkourCaps) : chaque saut
    /// est vérifié (ParkourJump.Feasible). Logique pure : le générateur pose les pièces, les tests et les mesures relisent les sauts.
    /// </summary>
    public static class ParkourCourse
    {
        /// <summary>Part de la capacité utilisée au plus (marge pour l'élan, la manette, la latence).</summary>
        public const float Margin = 0.92f;
        public const float BeamWidth = 0.45f;

        /// <summary>
        /// Pièces d'un parcours de length mètres (de x = 0 à x = length), départ et arrivée à y = 0, tracé à y = height (marches
        /// d'un mètre pour y monter). wallBottom / wallTop : murs de saut du fond du vide au plafond (pas de dessus où se poser).
        /// chestAnchor : nom de la pièce à côté de laquelle poser le socle du coffre (null : pas de coffre), côté chestSide (z),
        /// dessus à chestTop (assez haut pour n'être atteint que depuis cette pièce). Renvoie null si la place manque.
        /// </summary>
        public static List<ParkourPiece> Layout(float length, float height, float wallBottom, float wallTop, string chestAnchor,
            float chestSide, float chestTop, ParkourCaps caps, System.Random rng, List<ParkourJump> jumps)
        {
            var p = new List<ParkourPiece>();
            var js = new List<ParkourJump>();
            float dbl = caps.Double(0f);
            float x = 0f;
            float beam = Mathf.Lerp(3f, 4f, (float)rng.NextDouble());
            void Jump(ParkourPiece a, ParkourPiece b, float gap, ParkourMove move)
            {
                float rise = (b.Center.y + b.Size.y * 0.5f) - (a.Center.y + a.Size.y * 0.5f);
                float cap = move switch
                {
                    ParkourMove.Simple => caps.Single(rise),
                    ParkourMove.Double => caps.Double(rise),
                    ParkourMove.Dash => caps.Dash(rise),
                    _ => caps.Double(rise) + 2f * caps.RunSpeed * Mathf.Sqrt(2f * caps.WallJumpHeight / caps.Gravity), // + 2 sauts muraux
                };
                js.Add(new ParkourJump { From = a.Name, To = b.Name, Gap = gap, Rise = rise, Move = move, Capacity = cap, DoubleReach = caps.Double(rise) });
            }

            void Add(string name, float len, float w, float y, bool crumble, float gap, ParkourMove move)
            {
                ParkourPiece prev = p.Count > 0 ? p[p.Count - 1] : default;
                float startX = x + gap;
                var piece = new ParkourPiece { Name = name, Center = new Vector3(startX + len * 0.5f, y - 0.25f, 0f), Size = new Vector3(len, 0.5f, w), Crumble = crumble };
                int last = p.FindLastIndex(q => !q.Wall);
                p.Add(piece);
                if (last >= 0)
                    Jump(p[last], piece, gap, move);
                x = startX + len;
            }

            // 1. Poutre de départ (étroite, fixe : à portée du traqueur).
            Add("poutre de départ", beam, BeamWidth, 0f, false, 0f, ParkourMove.Simple);
            // 2. Marches qui tombent jusqu'à la hauteur du tracé (1 m au plus par marche).
            int steps = Mathf.CeilToInt(height - 0.01f);
            for (int i = 1; i <= steps; i++)
            {
                float y = Mathf.Min(height, i);
                Add($"marche {i}", 1.6f, 1.6f, y, true, Fit(caps.Single(y - Mathf.Min(height, i - 1)), 0.7f), ParkourMove.Simple);
            }

            // 3. Entre deux murs : plus long qu'un double saut (sauts muraux en chaîne ou course sur le mur).
            float wallGap = dbl + Mathf.Lerp(1.2f, 2f, (float)rng.NextDouble());
            float wallStart = x + 1.2f;
            Add("palier après les murs", 2f, 2f, height, true, wallGap, ParkourMove.Murs);
            foreach (float s in new[] { -1f, 1f })
                p.Add(new ParkourPiece
                {
                    Name = s < 0f ? "mur de saut 1" : "mur de saut 2",
                    Center = new Vector3(wallStart + (wallGap - 2.4f) * 0.5f, (wallBottom + wallTop) * 0.5f, s * 2.2f),
                    Size = new Vector3(wallGap - 2.4f, wallTop - wallBottom, 0.6f), Wall = true,
                });

            // 4. Vide qui demande un dash : plus long qu'un double saut, plus court qu'un saut avec dash (marge comprise).
            float dashGap = Mathf.Lerp(dbl + 0.6f, caps.Dash(0f) * Margin, Mathf.Lerp(0.2f, 0.45f, (float)rng.NextDouble()));
            Add("îlot après le dash", 1.4f, 1.4f, height, true, dashGap, ParkourMove.Dash);
            // 5. Poteaux étroits qui tombent, tant qu'il reste la place de l'arrivée.
            float endGap = Fit(caps.Single(-height), 0.6f);
            int posts = 0;
            float postGap = Fit(caps.Single(0f), 0.75f);
            while (x + postGap + 0.9f + endGap + beam <= length && posts < 4)
            {
                posts++;
                Add($"poteau {posts}", 0.9f, 0.9f, height, true, postGap, ParkourMove.Simple);
            }

            // 6. Arrivée : on redescend sur la poutre (fixe, à portée), qui finit au bout du tracé.
            float endBeam = length - x - endGap;                       // la poutre d'arrivée prend la place qui reste
            if (endBeam < 2f)
                return null;
            Add("poutre d'arrivée", endBeam, BeamWidth, 0f, false, endGap, ParkourMove.Simple);

            // Coffre : socle à côté de la pièce d'accroche, plus haut qu'elle (double saut au plus).
            if (!string.IsNullOrEmpty(chestAnchor))
            {
                int a = p.FindIndex(q => q.Name == chestAnchor);
                if (a < 0)
                    return null;
                ParkourPiece land = p[a];
                var socle = new ParkourPiece
                {
                    Name = "socle du coffre", Chest = true,
                    Center = new Vector3(land.Center.x, chestTop - 0.25f, chestSide * 5f), Size = new Vector3(2.6f, 0.5f, 2.6f),
                };
                p.Add(socle);
                Jump(land, socle, 5f - land.Size.z * 0.5f - 1.3f, ParkourMove.Double);
            }

            jumps?.Clear();
            jumps?.AddRange(js);
            return p;
        }

        /// <summary>Écart utilisé : une part de la capacité, jamais moins de 1 m.</summary>
        private static float Fit(float capacity, float share) => Mathf.Max(1f, capacity * share);
    }
}
