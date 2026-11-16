using System.Collections.Generic;
using System.Linq;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Bras FPS de la DA (SK_BrasFPS_&lt;personnage&gt;_D et _G, lot du 2026-11-16) à la place des mains greybox : posés pour que
    /// leur Socket_Poignet coïncide avec la main greybox (que FirstPersonView continue de déplacer : balancement, gestes, recul),
    /// l'orbe sur Socket_Orbe. Les doigts prennent les poses cuites (FpsArmPoseSet) selon les gestes : repos, traction, répulsion,
    /// attraper, tenir un objet, lancer ; main gauche : pistolet (barillet), prise de l'anneau (lanterne). Selon le personnage du
    /// joueur (renard, caméléon) ; sans bras pour lui, les mains greybox restent. Affichage seulement.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public sealed class FpsArms : MonoBehaviour
    {
        [SerializeField] private FirstPersonView view;
        [SerializeField] private FpsArmPoseSet poses;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        [SerializeField] private HandsOutline outline;

        private ItHunts.Artifacts.ArtifactHolder _holder;
        private PlayerMotor _motor;
        private CharacterDefinition _built;
        private bool _builtOnce;
        private GameObject _right, _left;
        private Transform[] _rightBones, _leftBones;
        private readonly List<Renderer> _greybox = new List<Renderer>();
        private Transform _orbHome;
        private Quaternion[] _rightBind, _leftBind;
        private bool _clipsUsable;

        /// <summary>Faux : les clips Pose_* sont vides (export de la DA) ; les doigts se ferment de l'angle des mains greybox.</summary>
        public bool ClipsUsable => _clipsUsable;
        /// <summary>La pose de la main droite vient d'un clip (sinon du repli).</summary>
        public bool RightFromClip { get; private set; }

        /// <summary>Pose courante de chaque côté (debug, tests).</summary>
        public string RightPose { get; private set; } = "Pose_Repos";
        public string LeftPose { get; private set; } = "Pose_Repos";
        public bool HasArms => _right != null;
        public GameObject RightArm => _right;
        public GameObject LeftArm => _left;

        private void Awake()
        {
            _holder = GetComponentInParent<ItHunts.Artifacts.ArtifactHolder>();
            _motor = GetComponentInParent<PlayerMotor>();
        }

        private void Start() => Rebuild();

        /// <summary>Recrée les bras pour le personnage actuel du joueur.</summary>
        public void Rebuild()
        {
            CharacterDefinition c = _holder != null ? _holder.Character : null;
            _built = c;
            _builtOnce = true;
            if (_right != null)
                Destroy(_right);
            if (_left != null)
                Destroy(_left);
            _right = _left = null;
            if (_greybox.Count == 0)
            {
                foreach (Transform hand in new[] { rightHand, leftHand })
                    if (hand != null)
                        foreach (string part in new[] { "Avant-bras", "Paume", "Doigts (articulation)/Doigts", "Pouce" })
                        {
                            Transform t = hand.Find(part);
                            if (t != null && t.GetComponent<Renderer>() != null)
                                _greybox.Add(t.GetComponent<Renderer>());
                        }
            }

            bool arms = c != null && c.fpsArmRight != null && c.fpsArmLeft != null && poses != null;
            foreach (Renderer r in _greybox)
                if (r != null)
                    r.enabled = !arms;
            Transform orb = view != null ? view.Orb : null;
            if (!arms)
            {
                if (orb != null && _orbHome != null)
                    orb.SetParent(_orbHome, false);
                return;
            }

            _right = Place(c.fpsArmRight, rightHand, out _rightBones, poses.right);
            _left = Place(c.fpsArmLeft, leftHand, out _leftBones, poses.left);
            _rightBind = _rightBones.Select(b => b != null ? b.localRotation : Quaternion.identity).ToArray();
            _leftBind = _leftBones.Select(b => b != null ? b.localRotation : Quaternion.identity).ToArray();
            _clipsUsable = Distinct(poses.right) || Distinct(poses.left);
            // L'orbe greybox sur Socket_Orbe (en attendant l'orbe v2).
            Transform socket = FindDeep(_right.transform, "Socket_Orbe");
            _orbSocket = socket;
            if (orb != null && socket != null)
            {
                if (_orbHome == null)
                    _orbHome = orb.parent;
                Vector3 scale = orb.lossyScale;
                orb.SetParent(socket, false);
                orb.localPosition = Vector3.zero;
                Vector3 ps = socket.lossyScale;
                orb.localScale = new Vector3(scale.x / ps.x, scale.y / ps.y, scale.z / ps.z);
            }


            if (outline != null)
                outline.Apply();
        }

        private GameObject Place(GameObject prefab, Transform hand, out Transform[] bones, FpsArmPoseSet.Side side)
        {
            GameObject arm = Instantiate(prefab, hand, false);
            arm.name = prefab.name + " (vue)";
            foreach (Animator a in arm.GetComponentsInChildren<Animator>(true))
                Destroy(a);
            // Socket_Poignet sur la main greybox (position et rotation) : le bras se place par rapport à son repère.
            Transform wrist = FindDeep(arm.transform, "Socket_Poignet");
            if (wrist != null)
            {
                Matrix4x4 local = hand.worldToLocalMatrix * wrist.localToWorldMatrix;     // repère du poignet dans la main
                Matrix4x4 inv = local.inverse;
                arm.transform.localPosition = inv.GetColumn(3);
                arm.transform.localRotation = inv.rotation;
            }

            int layer = view != null && view.Layer >= 0 ? view.Layer : hand.gameObject.layer;
            uint rendering = 0;
            foreach (Renderer r in _greybox)
                if (r != null)
                    rendering |= r.renderingLayerMask;
            foreach (Transform t in arm.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
            foreach (Renderer r in arm.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (rendering != 0)
                    r.renderingLayerMask = rendering;                  // calque de lumière « FPS » (pas de lanterne)
                if (r is SkinnedMeshRenderer s)
                    s.updateWhenOffscreen = true;
            }

            bones = new Transform[side.bones != null ? side.bones.Length : 0];
            for (int i = 0; i < bones.Length; i++)
                bones[i] = FindDeep(arm.transform, side.bones[i]);
            return arm;
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name)
                return t;
            foreach (Transform c in t)
            {
                Transform f = FindDeep(c, name);
                if (f != null)
                    return f;
            }

            return null;
        }

        private void LateUpdate()
        {
            if (_holder != null && (!_builtOnce || _holder.Character != _built))
                Rebuild();
            if (_right == null || poses == null || _motor == null)
                return;

            // Main droite : geste en cours, sinon pose de fond (comme FirstPersonView).
            MagnetismState m = _motor.Magnetism;
            TelekinesisState t = _motor.Telekinesis;
            HandGesture g = view != null ? view.Gesture : HandGesture.Aucun;
            float gu = view != null ? view.GestureProgress : 1f;
            string right;
            bool gesture = true;
            switch (g)
            {
                case HandGesture.Repulsion: right = "Pose_Repulsion"; break;
                case HandGesture.Lancer: right = "Pose_Lancer"; break;
                case HandGesture.Prise: right = gu < 0.5f ? "Pose_Attraper_1" : "Pose_Attraper_2"; break;
                default:
                    gesture = false;
                    // Pose de fond lue dans la vue (état simulé, ou aperçu forcé de la vitrine).
                    bool repel = view != null ? view.ShowsRepelHold : m.RepelHolding;
                    bool traction = view != null ? view.ShowsTraction : _motor.State.Mode == MovementMode.Traction;
                    bool holding = view != null ? view.ShowsHolding : t.Holding;
                    right = repel ? "Pose_Repulsion" : traction ? "Pose_Traction" : holding ? "Pose_TenirObjet" : "Pose_Repos";
                    break;
            }

            // L'orbe et l'artefact flottent au-dessus des paumes (retours du lot, point 6) : paumes ouvertes vers le haut, doigts
            // recourbés (Pose_OrbePaume de la DA) ; plus de prise du pistolet ni de l'anneau.
            if (right == "Pose_Repos")
                right = RestPose(poses.right, "OrbePaume");
            string left = RestPose(poses.left, _holder.BarrelSettings != null ? "Pistolet" : _holder.LanternSettings != null ? "PriseAnneau" : "Repos");
            float dt = Time.deltaTime;
            RightPose = right;
            LeftPose = left;
            // Pose lue dans un clip (avec courbes) si elle existe ; sinon (clip vide à l'import) les doigts se ferment de l'angle des
            // mains greybox (FirstPersonView), autour de l'axe X de chaque phalange.
            FpsArmPoseSet.Pose pr = poses.right.Find(right) ?? poses.right.FindAnimation(right)?.FrameAt(Time.time),
                pl = poses.left.Find(left) ?? poses.left.FindAnimation(left)?.FrameAt(Time.time);
            if (pr != null && pr.fromClip)
                Apply(poses.right, _rightBones, right, 1f - Mathf.Exp(-(gesture ? poses.gestureBlendRate : poses.blendRate) * dt));
            else
                Curl(_rightBones, _rightBind, view != null ? view.RightCurl : 25f);
            if (pl != null && pl.fromClip)
                Apply(poses.left, _leftBones, left, 1f - Mathf.Exp(-poses.blendRate * dt));
            else
                Curl(_leftBones, _leftBind, view != null ? view.LeftCurl : 25f);
            RightFromClip = pr != null && pr.fromClip;
            FloatOrb();
        }

        /// <summary>
        /// Pose de fond (PR #6) : la boucle de la DA (Boucle_OrbePaume à droite ; Boucle_Pistolet, Boucle_PriseAnneau ou Boucle_Repos à
        /// gauche selon l'artefact : respiration, doigts qui ondulent), sinon la pose fixe du même nom, sinon Pose_Repos. Un clip
        /// « Anim_Repos » livré plus tard passe devant.
        /// </summary>
        private static string RestPose(FpsArmPoseSet.Side side, string name)
        {
            if (side.FindAnimation("Anim_Repos") != null)
                return "Anim_Repos";
            if (side.FindAnimation("Boucle_" + name) != null)
                return "Boucle_" + name;
            return side.Find("Pose_" + name) != null ? "Pose_" + name : "Pose_Repos";
        }

        private Transform _orbSocket;

        /// <summary>L'orbe flotte au-dessus de la paume (Socket_Orbe) : montée et descente lentes, rotation lente.</summary>
        private void FloatOrb()
        {
            Transform orb = view != null ? view.Orb : null;
            FpsViewSettings cfg = view != null ? view.Settings : null;
            if (orb == null || cfg == null || _orbSocket == null || orb.parent != _orbSocket)
                return;
            float sy = Mathf.Max(1e-4f, _orbSocket.lossyScale.y);
            orb.localPosition = new Vector3(0f, ViewmodelMotion.FloatHeight(Time.time, cfg) / sy, 0f);
            orb.localRotation = Quaternion.Euler(0f, Time.time * cfg.floatSpin, 0f);
        }

        private static bool Distinct(FpsArmPoseSet.Side side)
        {
            if (!side.fromClips)
                return false;
            FpsArmPoseSet.Pose rest = side.Find("Pose_Repos");
            if (rest == null)
                return false;
            foreach (FpsArmPoseSet.Pose p in side.poses)
                for (int i = 0; i < p.rotations.Length && i < rest.rotations.Length; i++)
                    if (Quaternion.Angle(p.rotations[i], rest.rotations[i]) > 1f)
                        return true;
            return false;
        }

        private static void Curl(Transform[] bones, Quaternion[] bind, float curl)
        {
            if (bones == null || bind == null)
                return;
            for (int i = 0; i < bones.Length && i < bind.Length; i++)
            {
                Transform b = bones[i];
                if (b == null)
                    continue;
                string n = b.name;
                if (!(n.StartsWith("Index") || n.StartsWith("Majeur") || n.StartsWith("Annulaire") || n.StartsWith("Auriculaire") || n.StartsWith("Pouce")))
                    continue;
                float w = n.EndsWith("_1") ? 0.9f : n.EndsWith("_2") ? 1.2f : 0.9f;
                if (n.StartsWith("Pouce"))
                    w *= 0.45f;
                b.localRotation = bind[i] * Quaternion.Euler(curl * w, 0f, 0f);
            }
        }

        private static void Apply(FpsArmPoseSet.Side side, Transform[] bones, string pose, float k)
        {
            // Animation de la DA (clip à plusieurs images, cuit par FpsArmsSetup) : jouée en boucle.
            FpsArmPoseSet.Animation anim = side.FindAnimation(pose);
            FpsArmPoseSet.Pose p = anim != null ? anim.FrameAt(Time.time) : side.Find(pose) ?? side.Find("Pose_Repos");
            if (p == null || bones == null)
                return;
            for (int i = 0; i < bones.Length && i < p.rotations.Length; i++)
                if (bones[i] != null)
                    bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, p.rotations[i], k);
        }
    }
}
