using UnityEngine;

namespace ItHunts.Art.VFX
{
    /// <summary>
    /// Lien du livre du hibou (drain rouge sombre vers un ennemi, soin bleu-vert vers un allié) : une bande peinte ondulée
    /// (LineRenderer, texture raccordable qui défile), effilée près du livre, légèrement creusée et qui ondule.
    ///
    /// Deux usages :
    /// - **Lancer** (nuit 5, à chaque appui du livre, même sans cible) : `Lance(origine, direction, portée, cible, point)`.
    ///   La tête file à `vitesseLancer`. Sans cible : elle va au bout de la portée, tient un instant, se rétracte vers le livre
    ///   et l'objet se détruit seul. Sur une cible : elle s'y **accroche** (gerbe `accroche`, puis boucle `tenue` autour du
    ///   point tant que le lien tient). Ensuite `Regle(livre, cible)` à chaque image fait suivre le lien ; `Ferme()` le
    ///   rétracte et le détruit. Si la cible disparaît (mort, Transform détruit) ou si `Regle` n'est plus appelé pendant
    ///   `delaiSansNouvelles` s, le lien se rétracte seul depuis la dernière position connue.
    /// - **Lien posé** (ancien usage) : `Regle(a, b)` à chaque image, `Ouvre()` / `Ferme()` (fondu).
    ///
    /// Toutes les positions données par le jeu sont des positions **monde** ; la position de l'objet n'a pas d'importance.
    /// Aucun matériau n'est instancié (défilement par MaterialPropertyBlock).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public sealed class LienVfx : MonoBehaviour
    {
        [Tooltip("Aperçu dans l'éditeur seulement : extrémités (repère de l'objet) ou transforms.")]
        public Transform source, cible;
        public Vector3 pointSource = new Vector3(-1.5f, 0, 0), pointCible = new Vector3(1.5f, 0, 0);
        [Tooltip("Nombre de points de la ligne.")]
        [Range(4, 64)] public int points = 24;
        [Tooltip("Creux au milieu (m) et ondulation latérale (m, cycles par seconde).")]
        public float creux = 0.15f;
        public Vector2 ondulation = new Vector2(0.08f, 1.3f);
        [Tooltip("Largeur (m) ; la courbe de largeur du LineRenderer effile le lien près du livre.")]
        public float largeur = 0.18f;
        [Tooltip("Mètres de lien par répétition de la texture.")]
        public float pas = 0.9f;
        [Tooltip("Sens du flux : 1 = de la source vers la cible (soin), -1 = de la cible vers la source (drain).")]
        public float sens = 1f;
        [Tooltip("Vitesse du flux (m/s).")]
        public float vitesse = 2.2f;
        [Tooltip("Durée d'ouverture et de fermeture du lien posé (s).")]
        public float fondu = 0.2f;

        [Header("Lancer (nuit 5)")]
        [Tooltip("Vitesse de la tête du lien, à l'aller et au retour (m/s).")]
        public float vitesseLancer = 42f;
        [Tooltip("Temps passé au bout de la portée avant de se rétracter, sans cible (s).")]
        public float tenueSansCible = 0.08f;
        [Tooltip("Sans appel à Regle pendant ce temps (s) après l'accroche, et sans Transform de cible, le lien se rétracte.")]
        public float delaiSansNouvelles = 0.3f;
        [Tooltip("Gerbe jouée à l'accroche sur la cible (enfant du prefab).")]
        public ParticleSystem accroche;
        [Tooltip("Gerbe jouée au bout de la course sans cible (petite dispersion).")]
        public ParticleSystem perte;
        [Tooltip("Boucle jouée à la cible tant que le lien y est accroché (crochets qui tournent autour du point).")]
        public ParticleSystem tenue;

        enum Etat { Pose, Aller, Tenu, Bout, Retour }

        static readonly int PST = Shader.PropertyToID("_MainTex_ST");
        LineRenderer _l;
        MaterialPropertyBlock _mpb;
        Gradient _grad;
        float _alphaGrad = -1f;
        Vector3[] _pos;

        float _ouvert = 1f, _cibleOuvert = 1f, _phase;
        Etat _etat = Etat.Pose;
        bool _monde, _sansCible, _cibleSuivie;
        float _tete = 1f, _chrono, _derniereRegle;
        Vector3 _a, _b, _decal;
        Transform _cible;

        /// <summary>Vrai une fois la tête arrivée sur une cible (jusqu'au retrait).</summary>
        public bool Accroche => _etat == Etat.Tenu;
        /// <summary>Vrai pendant le retour de la tête vers le livre (le lien va se détruire).</summary>
        public bool SeRetracte => _etat == Etat.Retour;

        /// <summary>Pose ou suit le lien entre deux positions monde (à chaque image). Après un lancer sans cible, seule la
        /// source est suivie (la tête garde sa course).</summary>
        public void Regle(Vector3 a, Vector3 b)
        {
            _monde = true;
            _a = a;
            _derniereRegle = Time.time;
            if (_etat == Etat.Pose || !_sansCible) { _b = b; _cibleSuivie = false; }
        }

        public void Ouvre() { _cibleOuvert = 1f; }

        /// <summary>Fin du lien : un lien lancé se rétracte vers le livre puis se détruit ; un lien posé se défait en fondu.</summary>
        public void Ferme()
        {
            if (_etat == Etat.Pose) { _cibleOuvert = 0f; return; }
            Retire();
        }

        /// <summary>
        /// Lancer du livre depuis `origine` (monde) dans `direction`, jusqu'à `portee` m ; `cibleTouchee` (ou null) et le point
        /// touché sur elle.
        /// </summary>
        public void Lance(Vector3 origine, Vector3 direction, float portee, Transform cibleTouchee, Vector3 pointTouche)
        {
            _monde = true;
            _a = origine;
            _cible = cibleTouchee;
            _sansCible = cibleTouchee == null;
            _cibleSuivie = !_sansCible;
            _decal = _sansCible ? Vector3.zero : pointTouche - cibleTouchee.position;
            _b = _sansCible ? origine + direction.normalized * Mathf.Max(portee, 0.5f) : pointTouche;
            _tete = 0f; _chrono = 0f; _derniereRegle = Time.time;
            _etat = Etat.Aller;
            _ouvert = 1f; _cibleOuvert = 1f;
            if (tenue) tenue.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>Instancie le prefab du lien (à l'origine du monde) et le lance. Le jeu peut aussi instancier le prefab par
        /// son propre outil (GameVfx.Spawn) puis appeler `Lance` sur l'instance.</summary>
        public static LienVfx Lance(GameObject prefab, Vector3 origine, Vector3 direction, float portee, Transform cibleTouchee, Vector3 pointTouche)
        {
            if (prefab == null) return null;
            var l = Instantiate(prefab).GetComponent<LienVfx>();
            if (l != null) l.Lance(origine, direction, portee, cibleTouchee, pointTouche);
            return l;
        }

        void Retire()
        {
            if (_etat == Etat.Retour || _etat == Etat.Pose) return;
            _etat = Etat.Retour;
            if (tenue && tenue.isEmitting) tenue.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void OnEnable()
        {
            _l = GetComponent<LineRenderer>();
            _l.useWorldSpace = true;
            _mpb ??= new MaterialPropertyBlock();
            Met(0f);
        }

        void OnValidate() { if (!Application.isPlaying && isActiveAndEnabled) { _l = GetComponent<LineRenderer>(); Met(0f); } }

        void Update()
        {
            if (!Application.isPlaying) { if (transform.hasChanged || source || cible) { transform.hasChanged = false; Met(0f); } return; }
            Met(Time.deltaTime);
        }

        void Met(float dt)
        {
            if (_l == null) return;
            _ouvert = Mathf.MoveTowards(_ouvert, _cibleOuvert, dt / Mathf.Max(fondu, 0.01f));
            _phase += dt * vitesse / Mathf.Max(pas, 0.01f) * -sens;

            Vector3 a, b;
            if (_monde) { a = _a; b = _b; }
            else
            {
                a = source != null ? source.position : transform.TransformPoint(pointSource);
                b = cible != null ? cible.position : transform.TransformPoint(pointCible);
            }

            if (_etat != Etat.Pose)
            {
                // cible suivie par son Transform (sans Regle) ; perdue : on garde la dernière position et on se retire
                if (_cibleSuivie)
                {
                    if (_cible != null) _b = _cible.position + _decal;
                    else Retire();
                    b = _b;
                }
                else if (_etat == Etat.Tenu && !_sansCible && Time.time - _derniereRegle > delaiSansNouvelles)
                    Retire();

                float L = Mathf.Max(Vector3.Distance(a, b), 0.01f);
                switch (_etat)
                {
                    case Etat.Aller:
                        _tete = Mathf.Min(1f, _tete + dt * vitesseLancer / L);
                        if (_tete >= 1f)
                        {
                            if (_sansCible)
                            {
                                _etat = Etat.Bout; _chrono = 0f;
                                if (perte) { perte.transform.position = b; perte.Play(true); }
                            }
                            else
                            {
                                _etat = Etat.Tenu;
                                if (accroche) { accroche.transform.position = b; accroche.Play(true); }
                                if (tenue) { tenue.transform.position = b; tenue.Play(true); }
                            }
                        }
                        break;
                    case Etat.Bout:
                        _chrono += dt;
                        if (_chrono >= tenueSansCible) Retire();
                        break;
                    case Etat.Tenu:
                        if (accroche && accroche.isPlaying) accroche.transform.position = b;
                        if (tenue) tenue.transform.position = b;
                        break;
                    case Etat.Retour:
                        _tete = Mathf.Max(0f, _tete - dt * vitesseLancer * 1.4f / L);
                        if (_tete <= 0f && Application.isPlaying)
                        {
                            _l.enabled = false;
                            Destroy(gameObject, 0.6f);          // laisse finir les gerbes
                            _etat = Etat.Pose; _monde = true; _cibleOuvert = 0f; _ouvert = 0f;
                            return;
                        }
                        break;
                }
                b = Vector3.Lerp(a, b, _tete);
            }
            Dessine(a, b);
        }

        void Dessine(Vector3 a, Vector3 b)
        {
            float L = Vector3.Distance(a, b);
            Vector3 dir = (b - a) / Mathf.Max(L, 1e-4f);
            Vector3 lat = Vector3.Cross(dir, Vector3.up);
            if (lat.sqrMagnitude < 1e-4f) lat = Vector3.right;
            lat.Normalize();
            float t0 = Application.isPlaying ? Time.time : 0f;
            if (_pos == null || _pos.Length != points) _pos = new Vector3[points];
            for (int i = 0; i < points; i++)
            {
                float u = i / (points - 1f);
                float env = Mathf.Sin(u * Mathf.PI);
                _pos[i] = Vector3.Lerp(a, b, u) + Vector3.down * creux * env * Mathf.Min(L / 4f, 1f)
                          + lat * ondulation.x * env * Mathf.Sin((u * 3f - t0 * ondulation.y) * Mathf.PI * 2f);
            }
            _l.positionCount = points;
            _l.SetPositions(_pos);
            _l.widthMultiplier = largeur;
            _l.textureMode = LineTextureMode.Tile;
            _l.textureScale = new Vector2(1f / Mathf.Max(pas, 0.01f), 1f);
            _mpb ??= new MaterialPropertyBlock();
            _l.GetPropertyBlock(_mpb);                         // garde les tons posés par VfxPeint
            _mpb.SetVector(PST, new Vector4(1f, 1f, _phase, 0f));
            _l.SetPropertyBlock(_mpb);
            if (_grad == null || !Mathf.Approximately(_alphaGrad, _ouvert))
            {
                _grad ??= new Gradient();
                _grad.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                              new[] { new GradientAlphaKey(_ouvert, 0), new GradientAlphaKey(_ouvert, 1) });
                _l.colorGradient = _grad;
                _alphaGrad = _ouvert;
            }
            _l.enabled = _ouvert > 0.001f && L > 0.02f;
        }
    }
}
