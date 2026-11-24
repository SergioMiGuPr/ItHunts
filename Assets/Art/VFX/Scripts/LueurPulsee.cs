using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Lueur des objets interactifs (nuit 5, lot 3 : coffres) : une lumière chaude qui respire lentement, pour que l'objet se
    /// lise dans le noir à distance de jeu (« tout ce qui est interactif brille »), sans halo en boule. Désactivée (fondu)
    /// quand l'objet n'est plus interactif : `Eteint()` (coffre ouvert).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class LueurPulsee : MonoBehaviour
    {
        public float intensite = 1.4f;
        [Range(0, 1)] public float amplitude = 0.22f;
        public float frequence = 0.55f;
        float _phase, _facteur = 1f, _cible = 1f;
        Light _l;

        void Awake() { _l = GetComponent<Light>(); _phase = Random.value * 6.28f; }

        public void Eteint() => _cible = 0f;
        public void Allume() => _cible = 1f;

        void Update()
        {
            _facteur = Mathf.MoveTowards(_facteur, _cible, Time.deltaTime * 1.5f);
            float s = 1f + amplitude * Mathf.Sin(Time.time * frequence * 6.2832f + _phase);
            _l.intensity = intensite * s * _facteur;
            _l.enabled = _facteur > 0.01f;
        }
    }
}
