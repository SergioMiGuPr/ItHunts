using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Éclaboussure de l'eau (PR #24, docs/art/EAU.md) : sur le déclencheur d'une eau (canal, fosse), un joueur, un ennemi ou un
    /// objet qui y tombe fait jaillir l'éclaboussure peinte de la DA à la surface, à sa taille (1.2 par mètre, entre 0.4 et 8).
    /// Le traqueur n'entre pas dans l'eau (NavMesh) ; un objet lancé, si.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WaterSplash : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [Tooltip("Hauteur de la surface (m, monde) ; NaN : le haut du déclencheur.")]
        [SerializeField] private float surfaceY = float.NaN;
        [Tooltip("Délai minimal entre deux éclaboussures (s).")]
        [Min(0f)] [SerializeField] private float interval = 0.15f;

        private float _last = float.NegativeInfinity;
        public int Splashes { get; private set; }
        public GameObject Prefab => prefab;

        public static float Scale(float size) => Mathf.Clamp(1.2f * size, 0.4f, 8f);

        public static WaterSplash Add(GameObject trigger, GameObject prefab, float surfaceWorldY)
        {
            if (trigger == null || prefab == null)
                return null;
            var s = trigger.AddComponent<WaterSplash>();
            s.prefab = prefab;
            s.surfaceY = surfaceWorldY;
            return s;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (prefab == null || other.isTrigger || Time.time - _last < interval)
                return;
            _last = Time.time;
            Bounds b = other.bounds;
            float y = float.IsNaN(surfaceY) ? GetComponent<BoxCollider>().bounds.max.y : surfaceY;
            GameObject fx = Instantiate(prefab, new Vector3(b.center.x, y, b.center.z), Quaternion.identity);
            fx.transform.localScale = Vector3.one * Scale(Mathf.Max(b.size.x, b.size.z));
            Destroy(fx, 3f);
            Splashes++;
        }
    }
}
