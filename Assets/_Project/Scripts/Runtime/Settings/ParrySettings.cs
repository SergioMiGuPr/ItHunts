using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Parade (D-075) : appuyer sur « Parer » (Alt gauche / R3, D-079) juste avant l'impact d'une attaque de goule annule ses dégâts et
    /// l'étourdit. Ne ralentit jamais le joueur. Sans effet contre le traqueur (sa prise passe). Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Parry_Default", menuName = "ItHunts/Réglages/Parade")]
    public sealed class ParrySettings : ScriptableObject
    {
        [Tooltip("Fenêtre (s) : un impact au plus ce temps après l'appui est paré (appui AVANT l'impact). Le coup de la goule tombe " +
                 "0.24 s après son départ (son d'attaque et élan du bras), mesuré sur Goule_Coup : main droite au plus loin à 0.23–0.24 s.")]
        [Min(0.01f)] public float window = 0.25f;
        [Tooltip("Tolérance après l'impact (s) : un appui jusqu'à ce temps APRÈS l'impact du coup au contact pare encore ; ses dégâts " +
                 "attendent d'autant. 0 : aucune (proposition du 2026-11-21 : window 0.24 et lateWindow 0.06, voir PROGRESS).")]
        [Min(0f)] public float lateWindow = 0.08f;
        [Tooltip("Étourdissement de la goule parée (s).")]
        [Min(0f)] public float stunDuration = 1.5f;
        [Tooltip("Temps de recharge (s) après un appui, réussi ou non (comme la répulsion).")]
        [Min(0f)] public float cooldown = 0.7f;
        [Tooltip("Une parade réussie rend la recharge plus courte (part gardée : 1 = inchangée).")]
        [Range(0f, 1f)] public float cooldownAfterSuccess = 0f;
        [Tooltip("Parade réussie : invulnérabilité courte du joueur (s) aux coups des ennemis (nuit 3, point 7).")]
        [Min(0f)] public float invulnerability = 0.5f;

        [Header("Retour (provisoire en attendant les effets d'art)")]
        [ColorUsage(false, true)] public Color flashColor = new Color(3f, 2.6f, 1.4f, 1f);
        [Tooltip("Taille de l'éclat, posé sur la goule (m).")]
        [Min(0f)] public float flashSize = 0.7f;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Parade ratée (appui dans le vide) : petit son sec, volume.")]
        [Range(0f, 1f)] public float missVolume = 0.45f;
        [Tooltip("Goule parée : repoussée (m/s) et éclair blanc sur elle (s).")]
        [Min(0f)] public float parryPush = 4f;
        [Min(0f)] public float parryFlash = 0.18f;
        [Tooltip("Couleur de l'icône de recharge provisoire du HUD.")]
        public Color hudColor = new Color(1f, 0.86f, 0.45f, 1f);
    }
}
