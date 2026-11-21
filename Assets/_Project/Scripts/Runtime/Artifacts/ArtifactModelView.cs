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
                }

                float u = (Time.time - _swingAt) / Mathf.Max(0.05f, sw.swingAnimDuration);
                if (u >= 0f && u < 1f)
                {
                    // Arc de taille : de l'épaule vers le bas-centre ; le 3e coup et la fente plus amples, le 2e en revers.
                    float amp = _swingLunge ? 70f : _swingIndex == 2 ? 110f : 80f;
                    float dir = _swingIndex == 1 ? -1f : 1f;
                    float k = Mathf.Sin(u * Mathf.PI);
                    _root.rotation *= Quaternion.Euler(amp * k * 0.6f, 0f, dir * amp * (u - 0.5f));
                    _root.position += _root.rotation * Vector3.forward * (_swingLunge ? 0.12f : 0.05f) * k;
                }
            }
        }

        /// <summary>Lien du livre : bande peinte entre le livre (vue FPS ramenée dans le monde) et la cible.</summary>
        private void UpdateLink()
        {
            BookLink kind = _holder != null && _holder.BookSettings != null ? _holder.Book.Link : BookLink.Aucun;
            Transform target = _holder != null ? _holder.BookTarget : null;
            if (kind != _linkKind && _link != null)
            {
                Destroy(_link.gameObject);
                _link = null;
            }

            _linkKind = kind;
            if (kind == BookLink.Aucun || target == null)
                return;
            GameVfxSettings vfx = GameVfx.Settings;
            if (_link == null && vfx != null)
            {
                GameObject go = GameVfx.Spawn(kind == BookLink.Drain ? vfx.drainLink : vfx.healLink, transform.position, Quaternion.identity, 1f, null, 0f);
                _link = go != null ? go.GetComponent<LienVfx>() : null;
                if (_link != null)
                    _link.Ouvre();
            }

            if (_link == null)
                return;
            Vector3 from = _root != null && _view != null ? _view.ViewToWorld(_root.position) : transform.position + Vector3.up * 1.4f;
            var col = target.GetComponentInChildren<Collider>();
            Vector3 to = col != null ? col.bounds.center : target.position + Vector3.up;
            // Drain : le flux va de l'ennemi vers le hibou (source = livre, cible = ennemi ; sens réglé dans le prefab).
            _link.Regle(from, to);
        }

        private void OnDestroy()
        {
            if (_link != null)
                Destroy(_link.gameObject);
        }
    }
}
