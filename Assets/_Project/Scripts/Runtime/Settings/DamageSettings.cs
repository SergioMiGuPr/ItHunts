using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Dégâts d'impact des objets (D-025) : dégâts = masse (kg) × (vitesse d'impact − seuil) × facteur, pour tout impact
    /// d'un objet attrapable sur un ennemi au-dessus du seuil (lancé, rebond, chute, objet tenu) ; seuil plus bas pour
    /// une chute. Valeurs de départ :
    /// docs/design/features/orbes/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Damage_Default", menuName = "ItHunts/Réglages/Dégâts d'impact")]
    public sealed class DamageSettings : ScriptableObject
    {
        [Tooltip("Sous cette vitesse d'impact (m/s, vitesse relative au contact), aucun dégât.")]
        [Min(0f)] public float speedThreshold = 8f;
        [Tooltip("Seuil pour une chute (m/s) : impact surtout vertical, objet au-dessus de la cible (lustre, caisse lâchée " +
                 "de haut). 3 : caisse de 25 kg tombée de 4 m ≈ 14 dégâts.")]
        [Min(0f)] public float fallSpeedThreshold = 3f;
        [Tooltip("Part verticale de la vitesse d'impact à partir de laquelle c'est une chute (0..1).")]
        [Range(0f, 1f)] public float fallVerticalShare = 0.7f;
        [Tooltip("Dégâts par kg et par m/s au-dessus du seuil. 0.1 : caisse de 25 kg à 30 m/s = 55.")]
        [Min(0f)] public float damageFactor = 0.1f;
        [Tooltip("Un même objet ne blesse le même ennemi qu'une fois par intervalle (s) : pas de dégâts répétés sur un " +
                 "contact qui frotte ou rebondit sur place.")]
        [Min(0f)] public float perTargetCooldown = 0.3f;

        [Header("Objet repoussé par la répulsion (D-031)")]
        [Tooltip("Un objet repoussé blesse ce qu'il percute comme un objet lancé, multiplié par ce facteur (sans zone).")]
        [Range(0f, 2f)] public float pushedDamageMultiplier = 0.5f;
        [Tooltip("Il reste dangereux ce temps après la fin de la poussée (s)…")]
        [Min(0f)] public float pushedDangerTime = 1f;
        [Tooltip("… ou jusqu'à ce que sa vitesse passe sous ce seuil (m/s).")]
        [Min(0f)] public float pushedMinSpeed = 6f;

        [Header("Dégâts de zone (objet lancé, premier impact ; D-027)")]
        [Tooltip("Rayon de la zone (m) = base + par kg × masse : 5 kg ≈ 1.3 m, 15 kg ≈ 1.9 m, 25 kg ≈ 2.5 m, 40 kg ≈ 3.4 m. " +
                 "Un objet peut imposer son propre rayon (Grabbable).")]
        [Min(0f)] public float splashBaseRadius = 1f;
        [Min(0f)] public float splashRadiusPerKg = 0.06f;
        [Tooltip("Dégâts au centre de la zone, en part des dégâts d'impact de l'objet (masse × (vitesse − seuil) × facteur).")]
        [Range(0f, 2f)] public float splashCenterShare = 0.6f;
        [Tooltip("Au bord de la zone, part des dégâts du centre (décroissance linéaire).")]
        [Range(0f, 1f)] public float splashEdgeShare = 0.25f;

        [Header("Durabilité : nombre de lancers tenus selon la masse (D-027)")]
        [Tooltip("Jusqu'à ces masses (kg) : 1, 2, 3 puis 5 lancers ; un objet peut imposer sa durabilité (Grabbable). " +
                 "Un lancer compte quand son premier impact dépasse le seuil de vitesse.")]
        [Min(0f)] public float durability1MaxMass = 5f;
        [Min(0f)] public float durability2MaxMass = 15f;
        [Min(0f)] public float durability3MaxMass = 25f;
        [Min(1)] public int heavyDurability = 5;

        [Header("Paliers (couleur des chiffres et des éclats)")]
        [Tooltip("À partir de ces dégâts : coup moyen (jaune), puis gros coup (rouge).")]
        [Min(0f)] public float mediumHit = 25f;
        [Min(0f)] public float bigHit = 60f;
    }
}
