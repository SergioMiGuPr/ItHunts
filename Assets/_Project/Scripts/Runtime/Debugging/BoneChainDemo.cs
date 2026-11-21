using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>Vitrine : fait aller et venir l'accroche d'une chaîne d'os factice (BoneSpringChain, point 10).</summary>
    public sealed class BoneChainDemo : MonoBehaviour
    {
        public Vector3 amplitude = new Vector3(1.5f, 0f, 0.6f);
        public float period = 2.5f;
        private Vector3 _home;

        private void Start() => _home = transform.position;

        private void Update()
        {
            float t = Time.time * Mathf.PI * 2f / Mathf.Max(0.1f, period);
            transform.position = _home + new Vector3(Mathf.Sin(t) * amplitude.x, 0f, Mathf.Sin(t * 2f) * amplitude.z);
        }
    }
}
