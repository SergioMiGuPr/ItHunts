using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Lueur pilotée par le jeu (_EmissionColor, masque d'émission du matériau) sur un rendu. Sur le shader toon de la DA
    /// (ItHunts/Toon) : une copie du matériau propre à ce rendu, créée au premier appel (le mot-clé _EMISSION et le masque y
    /// sont garantis ; la lueur change avant chaque caméra, en écran partagé). Ailleurs (URP Lit, Unlit), un bloc de propriétés.
    /// </summary>
    public static class EmissionGlow
    {
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly Dictionary<int, Material[]> Copies = new Dictionary<int, Material[]>();
        private static MaterialPropertyBlock _block;

        public static bool IsToon(Renderer r) => r != null && r.sharedMaterial != null && r.sharedMaterial.shader != null
                                                  && r.sharedMaterial.shader.name == "ItHunts/Toon";

        /// <summary>Allume (couleur HDR) ou éteint (noir) l'émission ; wholeModel : masque blanc (tout le modèle) au lieu du sien.</summary>
        public static void Set(Renderer r, Color color, bool wholeModel = false)
        {
            if (r == null)
                return;
            if (!IsToon(r))
            {
                _block ??= new MaterialPropertyBlock();
                r.GetPropertyBlock(_block);
                if (wholeModel)
                    _block.SetTexture(EmissionMapId, Texture2D.whiteTexture);
                _block.SetColor(EmissionId, color);
                r.SetPropertyBlock(_block);
                return;
            }

            int id = r.GetInstanceID();
            if (!Copies.TryGetValue(id, out Material[] mats) || mats == null || mats.Length == 0 || mats[0] == null
                || r.sharedMaterial != mats[0])                             // matériau remplacé entre-temps : nouvelles copies
            {
                mats = r.materials;                                       // copies propres à ce rendu (Unity les crée)
                foreach (Material m in mats)
                    if (m != null)
                        m.EnableKeyword("_EMISSION");
                Copies[id] = mats;
            }

            foreach (Material m in mats)
            {
                if (m == null || !m.HasProperty(EmissionId))
                    continue;
                if (wholeModel)
                    m.SetTexture(EmissionMapId, Texture2D.whiteTexture);
                m.SetColor(EmissionId, color);
            }
        }

        /// <summary>Oublie les copies d'un rendu détruit (évite la fuite dans le dictionnaire).</summary>
        public static void Forget(Renderer r)
        {
            if (r == null)
                return;
            int id = r.GetInstanceID();
            if (Copies.TryGetValue(id, out Material[] mats))
            {
                foreach (Material m in mats)
                    if (m != null)
                        Object.Destroy(m);
                Copies.Remove(id);
            }
        }
    }
}
