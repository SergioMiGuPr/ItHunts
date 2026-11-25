using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Effets visuels peints de la DA (PR #22, docs/art/VFX-HANDOFF.md) à la place des particules provisoires. Chargé depuis
    /// Resources/GameVfx_Default ; un effet absent : l'ancien reste.
    /// </summary>
    [CreateAssetMenu(fileName = "GameVfx_Default", menuName = "ItHunts/Réglages/Effets visuels")]
    public sealed class GameVfxSettings : ScriptableObject
    {
        public GameObject torchFire;
        public GameObject lanternJet;
        public GameObject sparks;
        public GameObject barrelMuzzle;
        public GameObject barrelTrace;
        public GameObject soul;
        public GameObject ghoulDeath;
        public GameObject flyerDeath;
        public GameObject stun;
        public GameObject parry;
        public GameObject drainLink;
        public GameObject healLink;
        public GameObject stalkerLandingDust;

        [Header("Lot 2 de la DA (PR #35, VFX-HANDOFF « Lot 2 »)")]
        [Tooltip("Traînées des 3 coups d'épée (posées à la caméra), éclat d'impact vert de l'épée.")]
        public GameObject swordSlashH;
        public GameObject swordSlashD;
        public GameObject swordThrust;
        public GameObject swordImpact;
        [Tooltip("Traits de vitesse au bord de l'écran (enfant de la caméra, VitesseVfx).")]
        public GameObject speedEdges;
        [Tooltip("Éclats d'impact : léger (balle, petit objet), lourd (objet lourd lancé).")]
        public GameObject impactLight;
        public GameObject impactHeavy;
        [Header("Nuit 3 (PR #43)")]
        public GameObject critical;
        public GameObject paralysis;
        public GameObject explosionKill;
        public GameObject parryReadable;
        public GameObject bossClaw, bossBite, bossGroundImpact, bossWound, bossWeakPoint;
        [Tooltip("Poussière d'atterrissage du joueur, aux pieds.")]
        public GameObject playerDust;

        [Header("Nuit 5, lot 4 (PR #57)")]
        [Tooltip("Trait fin de l'orbe à l'objet tenu (LienVfx, Regle chaque image), à la place du trait violet greybox.")]
        public GameObject heldTrait;
        [Tooltip("Goule qui sort d'un trou d'ombre au sol (pivot au sol) : elle monte de spawnHoleDepth m entre spawnHoleRise.x et .y s.")]
        public GameObject spawnHole;
        [Tooltip("Goule qui sort d'un coin sombre (fumée d'encre) : elle est activée à spawnShadowDelay s.")]
        public GameObject spawnShadow;
        [Tooltip("Goule qui passe une grille ou un soupirail (pivot au centre, +Y côté salle). Aucune grille posée à ce jour.")]
        public GameObject spawnGrate;
        [Min(0f)] public float spawnHoleDepth = 1f;
        public Vector2 spawnHoleRise = new Vector2(0.25f, 0.9f);
        [Min(0f)] public float spawnShadowDelay = 0.45f;

        [Tooltip("Âmes dessinées avec l'effet peint (les plus proches) ; au-delà, l'ancien éclat.")]
        [Min(0)] public int soulVfxCap = 32;
        [Tooltip("Échelle de la flamme sur un ennemi qui brûle.")]
        [Min(0.05f)] public float burningScale = 0.6f;
    }
}
