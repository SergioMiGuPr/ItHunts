using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Lumière « lanterne » portée par chaque joueur (RENDU.md § 3 bis, D-067) : une lumière ponctuelle sans ombres, à hauteur de
    /// hanche, qui rend lisible le mur qu'on longe en wall run sans éclairer le reste du décor. Sa couleur est celle du personnage
    /// (CharacterDefinition.lanternColor). Exclue des mains et des artefacts par les calques de rendu (fpsRenderingLayer).
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerLantern_Default", menuName = "ItHunts/Réglages/Lanterne du joueur")]
    public sealed class PlayerLanternSettings : ScriptableObject
    {
        public bool enabled = true;
        [Min(0f)] public float intensity = 0.6f;
        [Min(0.5f)] public float range = 6f;
        [Tooltip("Position dans le repère du joueur (m) : hanche, un peu devant.")]
        public Vector3 position = new Vector3(0f, 1.1f, 0.3f);
        [Tooltip("Couleur sans personnage (scène de test sans liste de personnages).")]
        public Color fallbackColor = new Color(0.62f, 0.16f, 0.07f);
        [Tooltip("Calque de rendu des mains et des artefacts (vue FPS) : la lanterne ne l'éclaire pas.")]
        [Range(1, 31)] public int fpsRenderingLayer = 1;
    }
}
