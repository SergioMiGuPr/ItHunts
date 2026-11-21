using ItHunts.Art.VFX;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>Effets peints de la DA (Resources/GameVfx_Default) : apparition d'un effet ponctuel ou en boucle.</summary>
    public static class GameVfx
    {
        private static GameVfxSettings _settings;
        private static bool _loaded;

        public static GameVfxSettings Settings
        {
            get
            {
                if (!_loaded || _settings == null)
                {
                    _settings = Resources.Load<GameVfxSettings>("GameVfx_Default");
                    _loaded = true;
                }

                return _settings;
            }
        }

        /// <summary>Compteur des effets créés (mesure du coût, tests).</summary>
        public static int Spawned { get; private set; }

        /// <summary>
        /// Effet à cette position : ponctuel (détruit après <paramref name="life"/> s si la DA ne l'a pas marqué) ou suivi d'un
        /// parent. Null si le prefab manque.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1f, Transform parent = null, float life = 4f)
        {
            if (prefab == null)
                return null;
            GameObject go = Object.Instantiate(prefab, position, rotation, parent);
            var peint = go.GetComponent<VfxPeint>();
            if (peint != null && !Mathf.Approximately(scale, 1f))
            {
                peint.echelle *= scale;
                peint.Applique();
            }
            else if (!Mathf.Approximately(scale, 1f))
                go.transform.localScale *= scale;

            if (life > 0f)
                Object.Destroy(go, life);
            Spawned++;
            return go;
        }
    }
}
