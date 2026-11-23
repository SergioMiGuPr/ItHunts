using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Animation d'ouverture des portes et cellules des kits (nuit 4, lot 3), posée par KitBuilder sur les prefabs :
    /// - Battants : portes de sortie, deux battants qui pivotent autour de Y (gauche -90°, droit +90°) ;
    /// - Guillotine : porte verrouillée, la serrure tourne de +90° autour de Z (déverrouillage), puis le vantail de fer monte
    ///   de 3,1 m ;
    /// - Herse : herses des cellules du traqueur, montée de 9,2 m ;
    /// - Trappes : trappes de la fosse, -110° et +110° autour de Z ;
    /// - Vantaux : barricade et volière, deux vantaux qui pivotent de -100° et +100° autour de Y (vers la salle).
    /// API (main) : Ouvre(), Ferme(), Ouverture (0..1, réglable directement pour un aperçu), EstOuverte. Pas de physique : les
    /// colliders des parties mobiles suivent leurs transforms (ils ne sont pas statiques). Courbe d'accélération douce,
    /// légère secousse de fin de course pour les herses et le vantail (poids du fer).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OuverturePorte : MonoBehaviour
    {
        public enum Genre { Battants, Guillotine, Herse, Trappes, Vantaux }

        public Genre genre = Genre.Battants;
        [Tooltip("Parties mobiles : battant gauche / vantail / herse / trappe 1 (selon le genre).")] public Transform partieA;
        [Tooltip("Battant droit / serrure / trappe 2 / vantail 2.")] public Transform partieB;
        [Min(0.1f)] public float duree = 1.6f;
        [Range(0, 1)] public float ouverture;
        public bool ouvrirAuDemarrage;

        Quaternion _rotA, _rotB;
        Vector3 _posA, _posB;
        float _cible;
        bool _pret;

        public bool EstOuverte => ouverture >= 0.999f;

        void Awake()
        {
            Prepare();
            _cible = ouvrirAuDemarrage ? 1f : ouverture;
        }

        void Prepare()
        {
            if (_pret) return;
            _pret = true;
            if (partieA) { _rotA = partieA.localRotation; _posA = partieA.localPosition; }
            if (partieB) { _rotB = partieB.localRotation; _posB = partieB.localPosition; }
        }

        public void Ouvre() => _cible = 1f;
        public void Ferme() => _cible = 0f;

        /// <summary>Pose l'état d'ouverture (0 fermée, 1 ouverte) sans animation : aperçus, captures, chargement.</summary>
        public void Pose(float k) { Prepare(); _cible = ouverture = Mathf.Clamp01(k); Applique(); }

        void Update()
        {
            if (Mathf.Approximately(ouverture, _cible)) return;
            ouverture = Mathf.MoveTowards(ouverture, _cible, Time.deltaTime / duree);
            Applique();
        }

        static float Douce(float t) => t * t * (3f - 2f * t);

        // fin de course des pièces lourdes : petit rebond amorti
        static float Lourde(float t)
        {
            float d = Douce(Mathf.Clamp01(t / 0.92f));
            if (t > 0.92f) d += Mathf.Sin((t - 0.92f) / 0.08f * Mathf.PI) * 0.015f;
            return d;
        }

        void Applique()
        {
            Prepare();
            float t = ouverture;
            switch (genre)
            {
                case Genre.Battants:
                    if (partieA) partieA.localRotation = _rotA * Quaternion.Euler(0f, -90f * Douce(t), 0f);
                    if (partieB) partieB.localRotation = _rotB * Quaternion.Euler(0f, 90f * Douce(t), 0f);
                    break;
                case Genre.Guillotine:
                    // 0-30 % : la serrure tourne ; 30-100 % : le vantail monte
                    if (partieB) partieB.localRotation = _rotB * Quaternion.Euler(0f, 0f, 90f * Douce(Mathf.Clamp01(t / 0.3f)));
                    if (partieA) partieA.localPosition = _posA + Vector3.up * 3.1f * Lourde(Mathf.Clamp01((t - 0.3f) / 0.7f));
                    break;
                case Genre.Herse:
                    if (partieA) partieA.localPosition = _posA + Vector3.up * 9.2f * Lourde(t);
                    break;
                case Genre.Trappes:
                    if (partieA) partieA.localRotation = _rotA * Quaternion.Euler(0f, 0f, -110f * Douce(t));
                    if (partieB) partieB.localRotation = _rotB * Quaternion.Euler(0f, 0f, 110f * Douce(t));
                    break;
                case Genre.Vantaux:
                    if (partieA) partieA.localRotation = _rotA * Quaternion.Euler(0f, -100f * Douce(t), 0f);
                    if (partieB) partieB.localRotation = _rotB * Quaternion.Euler(0f, 100f * Douce(t), 0f);
                    break;
            }
        }
    }
}
