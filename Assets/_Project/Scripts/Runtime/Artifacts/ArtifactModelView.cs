using ItHunts.Art.VFX;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Vue FPS des artefacts sans vue dédiée (D-077) : épée en os du koala et livre à lien du hibou. Le modèle de la DA
    /// (ArtifactDefinition.viewModel : Artefact_Epee_Koala, Artefact_Livre_Hibou) flotte au-dessus de la main gauche comme les
    /// autres artefacts ; sans modèle, une forme provisoire. Épée : coup animé par le code (arc rapide, plus ample au 3e coup et
    /// en fente). Livre : lien peint de la DA (VFX_Lien_Drain rouge, VFX_Lien_Soin bleu-vert) vers la cible. Les mains ne bougent
    /// pas (gelées en attendant les références de l'utilisateur).
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class ArtifactModelView : MonoBehaviour
    {
        private ArtifactHolder _holder;
        private FirstPersonView _view;
        private Transform _hand;
        private Transform _root;
        private ArtifactDefinition _shownFor;
        private int _seenSwings;
        private float _swingAt = float.NegativeInfinity;
        private int _swingIndex;
        private bool _swingLunge;
        private LienVfx _link;
        private BookLink _linkKind;

        public Transform Model => _root;
        public bool LinkShown => _link != null && _link.gameObject.activeSelf && _linkKind != BookLink.Aucun;

        private void Awake()
        {
            _holder = GetComponent<ArtifactHolder>();
            _view = GetComponent<FirstPersonView>();
        }

        private Transform Hand()
        {
            if (_hand == null && _view != null)
                _hand = _view.LeftHand;                                  // la vue FPS vit hors du joueur (rig de caméra)
            if (_hand == null)
                foreach (Transform t in GetComponentsInChildren<Transform>(true))
                    if (t.name == "Main gauche")
                    {
                        _hand = t;
                        break;
                    }

            return _hand;
        }

        private void Rebuild(ArtifactDefinition a)
        {
            if (_root != null)
                Destroy(_root.gameObject);
            _root = null;
            _shownFor = a;
            if (!(a is BoneSwordSettings) && !(a is SpiritBookSettings))
                return;
            Transform hand = Hand();
            if (hand == null)
                return;
            GameObject go;
            if (a.viewModel != null)
                go = Instantiate(a.viewModel, hand, false);
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);     // forme provisoire
                go.transform.SetParent(hand, false);
                go.transform.localScale = a is BoneSwordSettings ? new Vector3(0.05f, 0.62f, 0.02f) : new Vector3(0.22f, 0.04f, 0.16f);
                go.GetComponent<Renderer>().material.color = a is BoneSwordSettings ? new Color(0.85f, 0.82f, 0.7f) : new Color(0.55f, 0.75f, 0.95f);
            }

            go.name = (a is BoneSwordSettings ? "Épée en os" : "Livre à lien") + " (vue)";
            int layer = hand.gameObject.layer;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
            foreach (Collider c in go.GetComponentsInChildren<Collider>(true))
                Destroy(c);
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _root = go.transform;
        }

        private void LateUpdate()
        {
            ArtifactDefinition a = _holder != null ? _holder.Current : null;
            if (a != _shownFor)
                Rebuild(a);
            UpdateLink();
            if (_root == null || _view == null || _view.Settings == null)
                return;
            FpsViewSettings fps = _view.Settings;
            BarrelView.PlaceFloating(_root, a.viewOffset, a.viewEuler, a.viewScale, fps, _view);

            if (a is BoneSwordSettings sw)
            {
                SwordState s = _holder.Sword;
                if (s.Swings != _seenSwings)
                {
                    _seenSwings = s.Swings;
                    _swingAt = Time.time;
                    _swingIndex = s.LastIndex;
                    _swingLunge = s.LastLunge;
                    SpawnArtSlash();
                    SwingSound();
                }

                float u = (Time.time - _swingAt) / Mathf.Max(0.05f, sw.swingAnimDuration);
                Transform cam = _view.ViewRoot;
                bool swinging = u >= 0f && u < 1f && cam != null;
                if (swinging)
                {
                    // L'épée flottante frappe (mains gelées) : du flottement vers la trajectoire du coup, puis retour.
                    SwingTiming(u, out float w, out float sp);
                    SwordPose(_swingIndex, _swingLunge, sp, out Vector3 hilt, out Vector3 dir, out Vector3 up);
                    Vector3 pos = cam.TransformPoint(hilt);
                    Quaternion rot = cam.rotation * Quaternion.LookRotation(dir, up);
                    _root.SetPositionAndRotation(Vector3.Lerp(_root.position, pos, w), Quaternion.Slerp(_root.rotation, rot, w));
                }

                UpdateSwordTrail(sw, cam, swinging && !_artSlash ? u : -1f);
            }
            else if (_trail != null)
                _trail.enabled = false;
        }

        private bool _artSlash;

        /// <summary>Lame dans le vide (son de la DA, GameAudio_Default.swordSwing / swordSwingHeavy) : silence tant qu'il manque.</summary>
        private void SwingSound()
        {
            GameAudioSettings art = ItHunts.Audio.GameAudio.Settings;
            if (art == null)
                return;
            bool heavy = _swingLunge || _swingIndex >= 2;
            AudioClip clip = heavy && art.swordSwingHeavy != null ? art.swordSwingHeavy : art.swordSwing;
            ItHunts.Audio.GameAudio.PlayAt(clip, transform.position + Vector3.up * 1.4f, art.swordVolume, 25f);
        }

        /// <summary>Traînée de la DA (PR #35) pour ce coup, posée à la caméra : taille horizontale, diagonale, estoc (et fente).</summary>
        private void SpawnArtSlash()
        {
            GameVfxSettings vfx = GameVfx.Settings;
            Transform cam = _view != null ? _view.ViewRoot : null;
            GameObject prefab = vfx == null ? null : _swingLunge || _swingIndex == 2 ? vfx.swordThrust : _swingIndex == 1 ? vfx.swordSlashD : vfx.swordSlashH;
            _artSlash = prefab != null && cam != null;
            if (!_artSlash || ItHunts.Player.GameFeel.Intensity <= 0.01f)
                return;
            GameObject go = GameVfx.Spawn(prefab, cam.position, cam.rotation, 1f, cam, 1.5f);
            if (go != null && _root != null)
                foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = _root.gameObject.layer;         // dessinée avec la vue FPS
        }

        private LineRenderer _trail;
        private MaterialPropertyBlock _trailBlock;
        private const int TrailPoints = 12;
        private readonly Vector3[] _trailPts = new Vector3[TrailPoints];

        /// <summary>Longueur de la lame (m) pour la pointe de la traînée (SM_Epee_Koala : pointe à ≈ 0.53 m de la garde).</summary>
        public const float BladeLength = 0.53f;

        /// <summary>
        /// Déroulé d'un coup (u : 0 à 1) : w = poids de la trajectoire face au flottement (montée rapide, retour à la fin),
        /// s = avancée sur la trajectoire (rapide au milieu : l'impact, strikeDelay ≈ 0.06 s après le clic).
        /// </summary>
        public static void SwingTiming(float u, out float w, out float s)
        {
            w = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(u / 0.12f)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.62f) / 0.38f)));
            s = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - 0.06f) / 0.42f));
        }

        /// <summary>
        /// Trajectoires des trois coups dans le repère de la caméra (garde, direction de la lame, normale du plat) à l'avancée s :
        /// 0 taille horizontale de droite à gauche, 1 diagonale du haut à droite vers le bas à gauche, 2 (et la fente) estoc vers l'avant.
        /// </summary>
        public static void SwordPose(int index, bool lunge, float s, out Vector3 hilt, out Vector3 dir, out Vector3 up)
        {
            if (lunge || index == 2)
            {
                float thrust = Mathf.Sin(Mathf.Clamp01(s) * Mathf.PI * 0.5f);
                hilt = Vector3.Lerp(new Vector3(0.05f, -0.27f, 0.3f), new Vector3(0f, -0.2f, 0.78f), thrust);
                dir = Vector3.Slerp(new Vector3(0.08f, 0.12f, 1f).normalized, new Vector3(0f, 0.02f, 1f).normalized, thrust);
                up = Vector3.right;                                      // tranchant vertical
                return;
            }

            if (index == 1)
            {
                hilt = Vector3.Lerp(new Vector3(0.24f, -0.06f, 0.5f), new Vector3(-0.16f, -0.32f, 0.52f), s);
                Vector3 a = new Vector3(0.6f, 0.65f, 0.45f).normalized, b = new Vector3(-0.55f, -0.6f, 0.55f).normalized;
                dir = Vector3.Slerp(a, b, s);
                up = Vector3.Cross(a, b).normalized;                     // plat dans le plan du coup
                return;
            }

            hilt = Vector3.Lerp(new Vector3(0.22f, -0.24f, 0.48f), new Vector3(-0.22f, -0.26f, 0.48f), s);
            dir = Vector3.Slerp(new Vector3(0.88f, 0.06f, 0.48f).normalized, new Vector3(-0.88f, -0.04f, 0.48f).normalized, s);
            up = Vector3.up;
        }

        /// <summary>Traînée de lame : ruban dans le repère de la caméra le long de l'arc parcouru par la pointe (matériau de la DA).</summary>
        private void UpdateSwordTrail(BoneSwordSettings sw, Transform cam, float u)
        {
            if (u < 0f || cam == null || sw.trailMaterial == null)
            {
                if (_trail != null)
                    _trail.enabled = false;
                return;
            }

            if (_trail == null)
            {
                var go = new GameObject("Traînée de lame");
                _trail = go.AddComponent<LineRenderer>();
                _trail.useWorldSpace = false;
                _trail.positionCount = TrailPoints;
                _trail.alignment = LineAlignment.View;
                _trail.textureMode = LineTextureMode.Stretch;
                _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _trail.receiveShadows = false;
                _trail.numCapVertices = 2;
                _trailBlock = new MaterialPropertyBlock();
            }

            if (_trail.transform.parent != cam)
            {
                _trail.transform.SetParent(cam, false);
                _trail.gameObject.layer = _root != null ? _root.gameObject.layer : cam.gameObject.layer;
            }

            _trail.sharedMaterial = sw.trailMaterial;
            SwingTiming(u, out _, out float s);
            float from = Mathf.Max(0f, s - sw.trailLength);
            bool visible = s - from > 0.04f && u < 0.75f;
            _trail.enabled = visible;
            if (!visible)
                return;
            for (int i = 0; i < TrailPoints; i++)
            {
                float t = Mathf.Lerp(from, s, i / (float)(TrailPoints - 1));
                SwordPose(_swingIndex, _swingLunge, t, out Vector3 hilt, out Vector3 dir, out _);
                _trailPts[i] = hilt + dir * BladeLength;
            }

            _trail.SetPositions(_trailPts);
            float fade = 1f - Mathf.Clamp01((u - 0.45f) / 0.3f);
            _trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, sw.trailWidth * Mathf.Max(0.2f, fade)));
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.3f * fade, 0f), new GradientAlphaKey(fade, 1f) });
            _trail.colorGradient = g;
            _trailBlock.SetColor("_Ton1", sw.trailTone * 0.45f);
            _trailBlock.SetColor("_Ton2", sw.trailTone * 0.75f);
            _trailBlock.SetColor("_Ton3", sw.trailTone);
            _trail.SetPropertyBlock(_trailBlock);
        }

        /// <summary>Lien du livre : bande peinte entre le livre (vue FPS ramenée dans le monde) et la cible.</summary>
        private void UpdateLink()
        {
            BookLink kind = _holder != null && _holder.BookSettings != null ? _holder.Book.Link : BookLink.Aucun;
            int links = _holder != null ? _holder.Book.Links : 0;
            Transform target = _holder != null ? _holder.BookTarget : null;
            if ((kind != _linkKind || links != _linkCount) && (_link != null || _fallbackLink != null))
                DestroyLink();

            _linkKind = kind;
            _linkCount = links;
            if (kind == BookLink.Aucun || target == null)
            {
                DestroyLink();
                return;
            }

            if (_link == null && _fallbackLink == null)
            {
                _linkAt = Time.time;                                     // un nouveau lancer : le lien part du livre
                GameVfxSettings vfx = GameVfx.Settings;
                GameObject prefab = vfx == null ? null : kind == BookLink.Drain ? vfx.drainLink : vfx.healLink;
                // Le lien de la DA lit ses points dans son propre repère (LienVfx.Regle puis TransformPoint) : il est donc posé à l'origine,
                // sans rotation (avant : posé sur le joueur, le lien était décalé d'autant et n'apparaissait pas sur la cible).
                GameObject go = GameVfx.Spawn(prefab, Vector3.zero, Quaternion.identity, 1f, null, 0f);
                _link = go != null ? go.GetComponent<LienVfx>() : null;
                if (_link != null)
                    _link.Ouvre();
                else
                {
                    if (go != null)
                        Destroy(go);
                    _fallbackLink = FallbackLine(kind);                  // sans le prefab de la DA : un trait sobre
                }
            }

            Vector3 from = _root != null && _view != null ? _view.ViewToWorld(_root.position) : transform.position + Vector3.up * 1.4f;
            var col = target.GetComponentInChildren<Collider>();
            Vector3 to = col != null ? col.bounds.center : target.position + Vector3.up;
            float throwTime = _holder.BookSettings != null ? _holder.BookSettings.linkThrowTime : 0f;
            Vector3 tip = LinkTip(from, to, Time.time - _linkAt, throwTime);
            if (_link != null)
            {
                Transform t = _link.transform;
                if (t.parent == null && (t.position != Vector3.zero || t.rotation != Quaternion.identity || t.localScale != Vector3.one))
                {
                    t.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    t.localScale = Vector3.one;
                }

                // Drain : le flux va de l'ennemi vers le hibou (source = livre, cible = ennemi ; sens réglé dans le prefab).
                _link.Regle(from, tip);
            }
            else if (_fallbackLink != null)
            {
                _fallbackLink.SetPosition(0, from);
                _fallbackLink.SetPosition(1, tip);
            }
        }

        /// <summary>Bout du lien qui part du livre (from) et atteint la cible (to) en throwTime s (0 : tout de suite).</summary>
        public static Vector3 LinkTip(Vector3 from, Vector3 to, float since, float throwTime) =>
            throwTime <= 0f ? to : Vector3.Lerp(from, to, Mathf.Clamp01(since / throwTime));

        private int _linkCount;
        private float _linkAt;
        private LineRenderer _fallbackLink;

        private LineRenderer FallbackLine(BookLink kind)
        {
            var go = new GameObject("Lien du livre (repli)");
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.05f;
            line.endWidth = 0.03f;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh != null)
                line.material = new Material(sh) { name = "Lien du livre (repli)" };
            Color c = kind == BookLink.Drain ? new Color(0.55f, 0.08f, 0.1f, 0.85f) : new Color(0.3f, 0.85f, 0.75f, 0.85f);
            line.startColor = line.endColor = c;
            return line;
        }

        private void DestroyLink()
        {
            if (_link != null)
                Destroy(_link.gameObject);
            _link = null;
            if (_fallbackLink != null)
            {
                if (_fallbackLink.material != null)
                    Destroy(_fallbackLink.material);
                Destroy(_fallbackLink.gameObject);
            }

            _fallbackLink = null;
        }

        private void OnDestroy() => DestroyLink();
    }
}
