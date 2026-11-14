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
        public static int CharacterIndex { get; private set; }
        /// <summary>Manette du 2e joueur, rejoint dans le menu (étape 9) ; l'écran partagé l'ajoute au chargement du niveau.</summary>
        public static UnityEngine.InputSystem.InputDevice CoopDevice { get; set; }
        public static bool Active => Definition != null;

        public static RunDifficulty Difficulty =>
            Definition != null && DifficultyIndex >= 0 && DifficultyIndex < Definition.difficulties.Count ? Definition.difficulties[DifficultyIndex] : null;

        public static float EnemyHealth => Difficulty != null ? Difficulty.enemyHealth : 1f;
        public static float EnemyDamage => Difficulty != null ? Difficulty.enemyDamage : 1f;
        public static float EnemySpeed => Difficulty != null ? Difficulty.enemySpeed : 1f;
        public static float EnemyCount => Difficulty != null ? Difficulty.enemyCount : 1f;

        /// <summary>Scène du menu (celle de la définition, sinon « MainMenu »).</summary>
        public static string MenuScene => Definition != null && !string.IsNullOrEmpty(Definition.menuScene) ? Definition.menuScene : "MainMenu";

        /// <summary>Démarre une run (menu) ; renvoie la scène du premier niveau, ou null si la difficulté n'est pas jouable.</summary>
        public static string Begin(RunDefinition definition, int difficulty, int character = 0)
        {
            if (definition == null || difficulty < 0 || difficulty >= definition.difficulties.Count || !definition.difficulties[difficulty].playable)
                return null;
            if (definition.levels.Count == 0 || !definition.levels[0].Ready)
                return null;
            ItHunts.Player.PlayerHealth.ResetDebugFlags();
            Definition = definition;
            DifficultyIndex = difficulty;
            CharacterIndex = character;
            LevelIndex = 0;
            return definition.levels[0].sceneName;
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
            return Definition.levels[next].sceneName;
        }

        /// <summary>Fin de la run (victoire ou mort) : retour au menu.</summary>
        public static void End()
        {
            Definition = null;
            LevelIndex = 0;
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
            CharacterIndex = 0;
        }
    }
}
