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
        [Tooltip("Poussière d'atterrissage du joueur, aux pieds.")]
        public GameObject playerDust;

        [Tooltip("Âmes dessinées avec l'effet peint (les plus proches) ; au-delà, l'ancien éclat.")]
        [Min(0)] public int soulVfxCap = 32;
        [Tooltip("Échelle de la flamme sur un ennemi qui brûle.")]
        [Min(0.05f)] public float burningScale = 0.6f;
    }
}
