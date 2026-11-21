using UnityEngine;

namespace ItHunts.Art.Eau
{
    /// <summary>
    /// Éclaboussure quand un objet tombe dans l'eau : sur le déclencheur de la surface (BoxCollider en trigger, à hauteur
    /// de l'eau), un corps qui entre crée une éclaboussure peinte (couronne + onde) au point d'entrée, dont la taille suit
    /// la taille de l'objet (bornes réglables : une caisse ~1, le traqueur ~6). Le jeu peut aussi appeler Eclabousse.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class EauEclaboussure : MonoBehaviour
    {
        public GameObject eclaboussure;
        [Tooltip("Hauteur de la surface (y monde) ; NaN = haut du collider.")]
        public float surface = float.NaN;
        [Tooltip("Échelle de l'éclaboussure par mètre de taille de l'objet, et bornes.")]
        public Vector3 echelle = new Vector3(1.2f, 0.4f, 8f);
        [Tooltip("Délai minimal entre deux éclaboussures du même objet (s).")]
        public float delai = 0.4f;

        private void OnTriggerEnter(Collider other)
        {
            if (eclaboussure == null) return;
            float taille = other.bounds.size.magnitude;
            Vector3 p = other.bounds.center;
            Eclabousse(p, taille);
        }

        public void Eclabousse(Vector3 point, float taille)
        {
            float y = float.IsNaN(surface) ? GetComponent<Collider>().bounds.max.y : surface;
            var go = Instantiate(eclaboussure, new Vector3(point.x, y, point.z), Quaternion.identity);
            go.transform.localScale = Vector3.one * Mathf.Clamp(taille * echelle.x, echelle.y, echelle.z);
            Destroy(go, 3f);
        }
    }
}
