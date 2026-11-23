using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Marquage au sol d'une attaque du boss (nuit 4, lot 7) : remplace les marques orange de FinalBoss. Bord net, remplissage
    /// rouge sang sombre et translucide, front qui se remplit pendant l'annonce, montée en intensité sur les 40 derniers pour
    /// cent, éclair de 0,1 s à l'impact puis disparition en 0,18 s. Couleur unique pour tout le boss ; le type d'attaque se lit
    /// à la forme.
    /// Utilisation (main) : MarquageBoss.Cercle(centre, rayon, duree), Couloir(depart, direction, longueur, largeur, duree),
    /// Cone(sommet, direction, portee, demiAngle, duree), Anneau(centre, rayonExt, rayonInt, duree). Le marquage se pose à plat
    /// à la hauteur du point donné (+3 cm), joue l'impact puis se détruit seul : ne pas l'effacer à la résolution de l'attaque.
    /// Annule() seulement si l'attaque est interrompue ; Prolonge(s) garde la marque pleine s secondes de plus après l'annonce
    /// (couloir pendant la course de la Charge).
    /// Matériau : Resources/M_VFX_Marquage_Boss (un seul matériau partagé, valeurs par marque dans un MaterialPropertyBlock) ;
    /// Configure(materiau) permet d'en passer un autre.
    /// </summary>
    public sealed class MarquageBoss : MonoBehaviour
    {
        public const string Ressource = "M_VFX_Marquage_Boss", RessourceApparition = "M_VFX_Apparition";
        static Material _base, _apparition;
        static Mesh _quad;
        static MaterialPropertyBlock _mpb;
        static readonly int PProg = Shader.PropertyToID("_Progression"), PInt = Shader.PropertyToID("_Intensite"),
            PFlash = Shader.PropertyToID("_Flash"), PFondu = Shader.PropertyToID("_Fondu"), PForme = Shader.PropertyToID("_Forme"),
            PAngle = Shader.PropertyToID("_DemiAngle"), PInt2 = Shader.PropertyToID("_RayonInterieur");

        /// <summary>Atténue l'éclair d'impact (réglage d'accessibilité, piloté par main : GameSettings.ReduceMotion). Commun à
        /// tous les joueurs : une marque au sol est vue par tous ; en coop, prendre le réglage le plus prudent.</summary>
        public static bool ReduireEffets;

        public float duree = 1f;
        float _t, _maintien, _demiAngle = 35f, _rayonInt = 0.55f;
        bool _apparitionSeule;
        int _forme;
        MeshRenderer _r;
        const float Eclair = 0.1f, Disparition = 0.18f;

        /// <summary>Matériau à utiliser (sinon Resources/M_VFX_Marquage_Boss).</summary>
        public static void Configure(Material materiau) { if (materiau != null) _base = materiau; }

        static Material Base
        {
            get
            {
                if (_base == null)
                {
                    _base = Resources.Load<Material>(Ressource);
                    if (_base == null)
                        Debug.LogError("MarquageBoss : matériau Resources/" + Ressource + " introuvable, marquage invisible.");
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
                _quad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
                _quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                _quad.RecalculateBounds();
                return _quad;
            }
        }

        static MarquageBoss Cree(string nom, Vector3 centre, Quaternion rot, Vector2 taille, int forme, float duree, Material mat = null)
        {
            var go = new GameObject(nom);
            go.transform.SetPositionAndRotation(centre + Vector3.up * 0.03f, rot);
            go.transform.localScale = new Vector3(taille.x, 1f, taille.y);
            go.AddComponent<MeshFilter>().sharedMesh = Quad;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.sharedMaterial = mat != null ? mat : Base;
            var mb = go.AddComponent<MarquageBoss>();
            mb._r = mr; mb._forme = forme;
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
            m._demiAngle = demiAngle; m.Applique(0f);
            return m;
        }

        public static MarquageBoss Anneau(Vector3 centre, float rayonExterieur, float rayonInterieur, float duree)
        {
            var m = Cree("Annonce (anneau)", centre, Quaternion.identity, Vector2.one * rayonExterieur * 2f, 3, duree);
            m._rayonInt = Mathf.Clamp01(rayonInterieur / Mathf.Max(rayonExterieur, 0.01f)); m.Applique(0f);
            return m;
        }

        /// <summary>
        /// Annonce d'une apparition (goules invoquées par le boss, Invocation) : cercle plein, bord fin, pulsé (matériau
        /// Resources/M_VFX_Apparition, le même que la marque d'apparition des vagues) ; ce n'est pas un coup, il ne monte pas.
        /// </summary>
        public static MarquageBoss Apparition(Vector3 centre, float rayon, float duree)
        {
            if (_apparition == null) _apparition = Resources.Load<Material>(RessourceApparition);
            var m = Cree("Annonce (apparition)", centre, Quaternion.identity, Vector2.one * rayon * 2f, 0, duree, _apparition);
            m._apparitionSeule = true; m.Applique(0f);
            return m;
        }

        /// <summary>
        /// Réoriente un couloir ou un cône autour de son point de départ (le boss) : à appeler chaque image si le boss pivote
        /// pendant l'annonce (Balayage : FinalBoss tourne vers la cible et frappe selon son orientation finale).
        /// </summary>
        public void Oriente(Vector3 direction)
        {
            direction.y = 0; if (direction.sqrMagnitude < 1e-6f || (_forme != 1 && _forme != 2)) return;
            direction.Normalize();
            float l = transform.localScale.z;
            Vector3 depart = transform.position - transform.forward * (l * 0.5f);
            transform.SetPositionAndRotation(depart + direction * (l * 0.5f), Quaternion.LookRotation(direction, Vector3.up));
        }

        /// <summary>Efface la marque tout de suite (attaque interrompue : boss étourdi, mort, autre attaque).</summary>
        public void Annule() => Destroy(gameObject);

        /// <summary>Garde la marque pleine s secondes de plus après la fin de l'annonce, puis joue l'impact (Charge : pendant la course).</summary>
        public void Prolonge(float s) => _maintien = Mathf.Max(_maintien, s);

        /// <summary>Aperçu (éditeur, captures) : pose l'état du marquage à l'instant t (secondes depuis le début de l'annonce).</summary>
        public void Pose(float t) => Applique(t);

        void Applique(float t)
        {
            if (_r == null) _r = GetComponent<MeshRenderer>();
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            float p = Mathf.Clamp01(t / duree);
            float apres = t - duree - _maintien;
            float flash = apres >= 0f && apres < Eclair ? (ReduireEffets ? 0.5f : 1f) * (1f - apres / Eclair) : 0f;
            _mpb.Clear();
            if (_apparitionSeule)
            {
                // apparition : pleine dès le début, pas de montée ni d'éclair, disparition douce à la fin
                _mpb.SetFloat(PForme, 0); _mpb.SetFloat(PProg, 1f); _mpb.SetFloat(PInt, 0.2f); _mpb.SetFloat(PFlash, 0f);
                _mpb.SetFloat(PFondu, apres > 0f ? 1f - Mathf.Clamp01(apres / Disparition) : Mathf.Clamp01(t / 0.15f));
                _r.SetPropertyBlock(_mpb);
                return;
            }
            _mpb.SetFloat(PForme, _forme);
            _mpb.SetFloat(PAngle, _demiAngle);
            _mpb.SetFloat(PInt2, _rayonInt);
            _mpb.SetFloat(PProg, p);
            _mpb.SetFloat(PInt, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, p)));
            _mpb.SetFloat(PFlash, flash);
            _mpb.SetFloat(PFondu, apres > Eclair ? 1f - Mathf.Clamp01((apres - Eclair) / Disparition) : 1f);
            _r.SetPropertyBlock(_mpb);
        }

        void Update()
        {
            _t += Time.deltaTime;
            Applique(_t);
            if (_t > duree + _maintien + Eclair + Disparition) Destroy(gameObject);
        }
    }
}
