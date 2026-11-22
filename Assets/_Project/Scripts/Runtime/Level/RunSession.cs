using ItHunts.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Level
{
    /// <summary>
    /// Déroulé de la run (D-048) : menu → niveau → combat de boss → niveau suivant ou fin. Vit d'une scène à l'autre (état de
    /// la run, pas d'un joueur). Sans run lancée par le menu (scène de test ouverte directement), les multiplicateurs valent 1
    /// et la fin ramène au menu s'il est dans le build. Réseau (étape 11) : choix de la difficulté et changements de niveau
    /// décidés par l'hôte.
    /// </summary>
    public static class RunSession
    {
        public static RunDefinition Definition { get; private set; }
        public static int DifficultyIndex { get; private set; }
        public static int LevelIndex { get; private set; }
        /// <summary>Acte en cours (D-092) : 0 acte I (caves), 1 acte II (château), 2 acte III (jardin), 3 boss final.</summary>
        public static int Act => LevelIndex;
        /// <summary>
        /// Personnage demandé par chaque joueur (indice dans la liste des personnages ; −1 : rien choisi), choisi dans le menu
        /// (D-064). Gardé d'une run à l'autre et pour les scènes de test ouvertes depuis le menu. RunState en déduit les
        /// personnages réels (CharacterPick : jamais deux fois le même).
        /// </summary>
        public static int[] Characters { get; set; } = { -1, -1 };
        /// <summary>Manette du 2e joueur, rejoint dans le menu (étape 9) ; l'écran partagé l'ajoute au chargement du niveau.</summary>
        public static UnityEngine.InputSystem.InputDevice CoopDevice { get; set; }
        public static bool Active => Definition != null;
        /// <summary>Les statistiques de la run (RunStats) ont été remises à zéro pour cette run.</summary>
        public static bool StatsStarted { get; set; }
        /// <summary>Acte de départ de la run (0, sauf playtest « Choisir l'acte »).</summary>
        public static int StartAct { get; private set; }
        /// <summary>
        /// Partie lancée depuis le menu Playtest (nuit 3, point 2) : le panneau admin est permis. Jamais dans une partie normale
        /// (Jouer au campement remet ce drapeau à faux).
        /// </summary>
        public static bool AdminMode { get; set; }
        /// <summary>Stuff d'un acte à l'autre (âmes, améliorations, achats) : écrit à la sortie, relu par l'acte suivant.</summary>
        public static ItHunts.Artifacts.RunCarry Carry { get; set; }
        /// <summary>Playtest du combat de boss (menu) : le niveau chargé lance directement son combat (BossFight).</summary>
        public static bool BossPlaytest { get; set; }

        public static RunDifficulty Difficulty =>
            Definition != null && DifficultyIndex >= 0 && DifficultyIndex < Definition.difficulties.Count ? Definition.difficulties[DifficultyIndex] : null;

        public static float EnemyHealth => Difficulty != null ? Difficulty.enemyHealth : 1f;
        public static float EnemyDamage => Difficulty != null ? Difficulty.enemyDamage : 1f;
        public static float EnemySpeed => Difficulty != null ? Difficulty.enemySpeed : 1f;
        public static float EnemyCount => Difficulty != null ? Difficulty.enemyCount : 1f;

        /// <summary>Scène du menu (celle de la définition, sinon « MainMenu »).</summary>
        public static string MenuScene => Definition != null && !string.IsNullOrEmpty(Definition.menuScene) ? Definition.menuScene : "MainMenu";

        /// <summary>Démarre une run (menu) ; renvoie la scène du premier niveau, ou null si la difficulté n'est pas jouable.</summary>
        public static string Begin(RunDefinition definition, int difficulty)
        {
            if (definition == null || difficulty < 0 || difficulty >= definition.difficulties.Count || !definition.difficulties[difficulty].playable)
                return null;
            if (definition.levels.Count == 0 || !definition.levels[0].Ready)
                return null;
            ItHunts.Player.PlayerHealth.ResetDebugFlags();
            Definition = definition;
            DifficultyIndex = difficulty;
            LevelIndex = 0;
            Carry = null;
            StatsStarted = false;
            StartAct = 0;
            return definition.levels[0].sceneName;
        }

        /// <summary>
        /// Playtest « Choisir l'acte » : la run démarre directement à cet acte (0 à 3), stuff vide, statistiques remises à zéro.
        /// Renvoie la scène, ou null si l'acte n'est pas prêt.
        /// </summary>
        public static string BeginAt(RunDefinition definition, int difficulty, int act)
        {
            if (Begin(definition, difficulty) == null || act < 0 || act >= definition.levels.Count || !definition.levels[act].Ready)
                return null;
            LevelIndex = act;
            StartAct = act;
            return definition.levels[act].sceneName;
        }

        /// <summary>Niveau gagné (combat de boss) : scène du niveau suivant, ou null si la run est finie (victoire).</summary>
        public static string NextLevelScene()
        {
            if (!Active)
                return null;
            int next = LevelIndex + 1;
            RunDifficulty d = Difficulty;
            if (d == null || next >= d.levelCount || next >= Definition.levels.Count || !Definition.levels[next].Ready)
                return null;
            LevelIndex = next;
            var run = ItHunts.Artifacts.RunState.Current;
            if (run != null)
                Carry = run.Snapshot();                                    // le stuff passe à l'acte suivant
            return Definition.levels[next].sceneName;
        }

        /// <summary>Fin de la run (victoire ou mort) : retour au menu.</summary>
        public static void End()
        {
            Definition = null;
            LevelIndex = 0;
            Carry = null;
            AdminMode = false;
            StatsStarted = false;
        }

        /// <summary>Charge une scène si elle est dans le build ; faux sinon.</summary>
        public static bool Load(string scene)
        {
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(scene))
                return false;
            SceneManager.LoadScene(scene);
            return true;
        }

        /// <summary>Remise à zéro (tests).</summary>
        public static void ResetForTests()
        {
            Definition = null;
            DifficultyIndex = 0;
            LevelIndex = 0;
            Carry = null;
            AdminMode = false;
            StatsStarted = false;
            Characters = new[] { -1, -1 };
        }
    }
}
