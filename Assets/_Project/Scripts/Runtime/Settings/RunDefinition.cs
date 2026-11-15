using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>Un niveau de la run (D-048) : scène à charger ; vide = pas encore fait.</summary>
    [Serializable]
    public sealed class RunLevel
    {
        public string displayName = "Niveau";
        [Tooltip("Nom de la scène (dans la liste des scènes du build). Vide : niveau pas encore fait.")]
        public string sceneName = "";
        public bool Ready => !string.IsNullOrEmpty(sceneName);
    }

    /// <summary>Une difficulté (D-046, D-048) : nombre de niveaux joués et multiplicateurs des ennemis.</summary>
    [Serializable]
    public sealed class RunDifficulty
    {
        public string displayName = "Facile";
        [TextArea] public string description = "";
        [Tooltip("Jouable maintenant (sinon grisée dans le menu).")]
        public bool playable;
        [Tooltip("Nombre de niveaux joués depuis le début de la liste (Facile 1, Moyenne 2, Difficile 4 : niveaux 1 à 3 et " +
                 "zone finale).")]
        [Min(1)] public int levelCount = 1;
        [Header("Ennemis (multiplicateurs)")]
        [Min(0.1f)] public float enemyHealth = 1f;
        [Min(0.1f)] public float enemyDamage = 1f;
        [Min(0.1f)] public float enemySpeed = 1f;
        [Tooltip("Nombre d'ennemis des vagues.")]
        [Min(0.1f)] public float enemyCount = 1f;
    }

    /// <summary>
    /// Structure de la run (D-048) : niveaux dans l'ordre (niveau 1 rempli ; 2, 3 et zone finale vides pour l'instant),
    /// difficultés (seule Facile est jouable), scène du menu principal.
    /// </summary>
    [CreateAssetMenu(fileName = "Run_Default", menuName = "ItHunts/Réglages/Run")]
    public sealed class RunDefinition : ScriptableObject
    {
        [Tooltip("Scène du menu principal (retour après l'écran de fin).")]
        public string menuScene = "MainMenu";
        public List<RunLevel> levels = new List<RunLevel>();
        public List<RunDifficulty> difficulties = new List<RunDifficulty>();
        [Tooltip("Personnages du menu (D-064) : chacun apporte son artefact ; les non jouables sont affichés grisés.")]
        public CharacterRoster characters;
    }
}
