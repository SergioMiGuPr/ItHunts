using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Marquage au sol d'une attaque du boss (nuit 4, lot 7) : remplace les zones rouge vif. Bord net, remplissage sombre et
    /// translucide, front qui se remplit pendant l'annonce, montée en intensité sur les 40 derniers pour cent, éclair de 0,1 s à
    /// l'impact puis disparition en 0,18 s. Couleur unique pour tout le boss ; le type d'attaque se lit à la forme.
    /// Utilisation (main) : MarquageBoss.Cercle(centre, rayon, duree), Couloir(depart, direction, longueur, largeur, duree),
    /// Cone(sommet, direction, portee, demiAngle, duree), Anneau(centre, rayonExt, rayonInt, duree). Le marquage se pose à
    /// plat à la hauteur donnée (+3 cm) et se détruit seul ; Annule() l'efface tout de suite.
    /// </summary>
    public sealed class MarquageBoss : MonoBehaviour
    {
        static Material _base;
        static Mesh _quad;
        public float duree = 1f;
        public bool reduireEffets;
        float _t;
        Material _m;
        const float Eclair = 0.1f, Disparition = 0.18f;

        static Material Base
        {
            get
            {
                if (_base == null)
                {
                    var s = Shader.Find("ItHunts/VFX/MarquageSol");
                    _base = new Material(s) { name = "Marquage du boss (sol)", hideFlags = HideFlags.DontSave };
                }
                return _base;
            }
        }

        static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                _quad = new Mesh { name = "Quad du marquage", hideFlags = HideFlags.DontSave };
                _quad.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f) };
                _quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                _quad.RecalculateBounds();
                return _quad;
            }
        }

        static MarquageBoss Cree(string nom, Vector3 centre, Quaternion rot, Vector2 taille, int forme, float duree)
        {
            var go = new GameObject(nom);
            go.transform.SetPositionAndRotation(centre + Vector3.up * 0.03f, rot);
            go.transform.localScale = new Vector3(taille.x, 1f, taille.y);
            go.AddComponent<MeshFilter>().sharedMesh = Quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var mb = go.AddComponent<MarquageBoss>();
            mb._m = new Material(Base) { hideFlags = HideFlags.DontSave };
            mb._m.SetFloat("_Forme", forme);
            mr.sharedMaterial = mb._m;
            mb.duree = Mathf.Max(0.2f, duree);
            mb.Applique(0f);
            return mb;
        }

        public static MarquageBoss Cercle(Vector3 centre, float rayon, float duree) =>
            Cree("Annonce (cercle)", centre, Quaternion.identity, Vector2.one * rayon * 2f, 0, duree);

        public static MarquageBoss Couloir(Vector3 depart, Vector3 direction, float longueur, float largeur, float duree)
        {
            direction.y = 0; if (direction.sqrMagnitude < 1e-6f) direction = Vector3.forward;
            var rot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            return Cree("Annonce (couloir)", depart + direction.normalized * (longueur * 0.5f), rot, new Vector2(largeur, longueur), 1, duree);
        }

        public static MarquageBoss Cone(Vector3 sommet, Vector3 direction, float portee, float demiAngle, float duree)
        {
            direction.y = 0; if (direction.sqrMagnitude < 1e-6f) direction = Vector3.forward;
            var rot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var m = Cree("Annonce (cône)", sommet + direction.normalized * (portee * 0.5f), rot, new Vector2(2f * portee, portee), 2, duree);
            m._m.SetFloat("_DemiAngle", demiAngle);
            return m;
        }

        public static MarquageBoss Anneau(Vector3 centre, float rayonExterieur, float rayonInterieur, float duree)
        {
            var m = Cree("Annonce (anneau)", centre, Quaternion.identity, Vector2.one * rayonExterieur * 2f, 3, duree);
            m._m.SetFloat("_RayonInterieur", Mathf.Clamp01(rayonInterieur / Mathf.Max(rayonExterieur, 0.01f)));
            return m;
        }

        public void Annule() => Destroy(gameObject);

        /// <summary>Aperçu (éditeur, captures) : pose l'état du marquage à l'instant t (secondes depuis le début de l'annonce).</summary>
        public void Pose(float t) => Applique(t);

        void Applique(float t)
        {
            float p = Mathf.Clamp01(t / duree);
            _m.SetFloat("_Progression", p);
            _m.SetFloat("_Intensite", Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, p)));
            float apres = t - duree;
            float flash = apres >= 0f && apres < Eclair ? (reduireEffets ? 0.5f : 1f) * (1f - apres / Eclair) : 0f;
            _m.SetFloat("_Flash", flash);
            _m.SetFloat("_Fondu", apres > Eclair ? 1f - Mathf.Clamp01((apres - Eclair) / Disparition) : 1f);
        }

        void Update()
        {
            _t += Time.deltaTime;
            Applique(_t);
            if (_t > duree + Eclair + Disparition) Destroy(gameObject);
        }

        void OnDestroy() { if (_m) { if (Application.isPlaying) Destroy(_m); else DestroyImmediate(_m); } }
    }
}
