using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// La lanterne de feu (artefact du renard, D-065) : sur le clic gauche / RT sans objet tenu, un jet de flammes en cône devant
    /// le joueur, pendant une courte durée. Chaque ennemi pris dans le cône est touché une fois par jet ; le jet traverse les
    /// ennemis mais pas les murs. Puis la lanterne s'éteint et se rallume pendant la recharge. Valeurs de départ :
    /// docs/design/features/artefacts/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "FireLantern_Default", menuName = "ItHunts/Artefacts/Lanterne de feu")]
    public sealed class FireLanternSettings : ArtifactDefinition
    {
        [Header("Jet")]
        [Tooltip("Portée du jet (m), mesurée depuis les yeux.")]
        [Min(0.5f)] public float range = 5f;
        [Tooltip("Ouverture totale du cône (°) : 40 = 20° de chaque côté du viseur.")]
        [Range(1f, 170f)] public float coneAngle = 40f;
        [Tooltip("Durée d'un jet (s) ; un ennemi qui entre dans le cône pendant le jet est touché aussi.")]
        [Min(0.02f)] public float jetDuration = 0.6f;
        [Tooltip("Dégâts par ennemi touché, une seule fois par jet (un volant meurt en un coup, comme pour toute attaque).")]
        [Min(0f)] public float damage = 25f;
        [Tooltip("Recharge entre deux jets (s), comptée depuis la fin du jet : la lanterne s'éteint puis se rallume.")]
        [Min(0f)] public float cooldown = 3f;
        [Tooltip("Un clic arrivé un peu trop tôt (fin de recharge) part dès que possible pendant ce délai (s).")]
        [Min(0f)] public float pressBuffer = 0.12f;
        [Tooltip("Ce que le jet peut toucher ou ce qui l'arrête (décor solide sans Rigidbody). Le joueur qui tire est toujours ignoré.")]
        public LayerMask hitMask = ~0;

        [Header("Retour visuel (greybox)")]
        [Tooltip("Flammes du jet : nombre de bouffées par seconde, durée de vie d'une bouffée (s), taille au départ et à l'arrivée (m).")]
        [Min(1f)] public float puffsPerSecond = 30f;
        [Min(0.05f)] public float puffLifetime = 0.35f;
        [Min(0f)] public float puffStartSize = 0.06f;
        [Min(0f)] public float puffEndSize = 0.9f;
        [Tooltip("Couleur des flammes (HDR, lueur, additive : les bouffées s'additionnent) : au départ (cœur jaune) et à la fin (rouge sombre).")]
        [ColorUsage(false, true)] public Color flameStartColor = new Color(1.3f, 0.55f, 0.12f);
        [ColorUsage(false, true)] public Color flameEndColor = new Color(0.5f, 0.08f, 0.02f);
        [Tooltip("Flamme de la lanterne (en main) : couleur HDR allumée ; éteinte pendant la recharge, elle se rallume peu à peu.")]
        [ColorUsage(false, true)] public Color lanternGlow = new Color(4f, 1.7f, 0.4f);
        [Tooltip("Part de la lueur quand la lanterne est inactive (l'orbe tient un objet).")]
        [Range(0f, 1f)] public float inactiveFactor = 0.25f;
        [Tooltip("Main gauche poussée vers l'avant pendant le jet (m).")]
        [Min(0f)] public float jetPush = 0.03f;

        [Header("Sons (provisoires)")]
        [Range(0f, 1f)] public float jetVolume = 0.6f;
        [Range(0f, 1f)] public float relightVolume = 0.45f;

        public float HalfAngle => coneAngle * 0.5f;
    }
}
