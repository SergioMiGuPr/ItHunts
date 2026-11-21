using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// IK des membres du traqueur v4 (C3, matin du 2026-11-21), purement visuelle, après l'Animator (lit la pose du clip de
    /// l'image, sans retard) : pieds et mains en appui collés au sol réel (marches, pentes, bords), mains plaquées sur un mur à portée
    /// pendant la course (frappes des courses enragées), mains sur la chaîne pendant l'escalade d'une chaîne, sur le mur pendant
    /// l'escalade d'un mur. IK analytique à deux os (rotations seules : aucune échelle, aucune longueur d'os ne change).
    /// Le bras qui attrape garde son IK de prise (Stalker.SolveArm), prioritaire.
    /// </summary>
    public sealed class StalkerLimbIK
    {
        private sealed class Limb
        {
            public Transform A, M, E, Contact;
            public bool Arm, Right;
            public float Weight, WallWeight;
        }

        private readonly Transform _self;
        private readonly Limb[] _limbs;
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// <summary>Membres en appui corrigés à cette image, mains au mur (tests, debug).</summary>
        public int Planted { get; private set; }
        public int OnWall { get; private set; }
        public bool Valid => _limbs.Length == 4;

        public StalkerLimbIK(Transform[] bones, Transform stalker)
        {
            _self = stalker;
            Transform B(string n) => System.Array.Find(bones, t => t.name == n);
            var list = new System.Collections.Generic.List<Limb>();
            foreach ((string side, bool right) in new[] { ("_D", true), ("_G", false) })
            {
                var leg = new Limb { A = B("Cuisse" + side), M = B("Jambe" + side), E = B("Pied" + side), Contact = B("Contact_Pied" + side), Right = right };
                var arm = new Limb { A = B("Bras" + side), M = B("AvantBras" + side), E = B("Main" + side), Contact = B("Contact_Main" + side), Arm = true, Right = right };
                foreach (Limb l in new[] { leg, arm })
                    if (l.A != null && l.M != null && l.E != null && l.Contact != null)
                        list.Add(l);
            }

            _limbs = list.ToArray();
        }

        /// <summary>
        /// Correction de l'image. groundY : sol sous le traqueur ; skipRightArm / skipLeftArm : bras tenus par l'IK de prise ;
        /// chain : point d'une chaîne qu'il grimpe (ou null) ; climbWall : il grimpe un mur (mains vers l'avant).
        /// </summary>
        public void Apply(StalkerSettings s, float dt, bool skipRightArm, bool skipLeftArm, bool running, Vector3? chain, bool climbWall)
        {
            Planted = 0;
            OnWall = 0;
            if (s == null || !s.limbIkEnabled)
                return;
            Vector3 up = _self.up, right = _self.right, fwd = _self.forward;
            float rate = dt / Mathf.Max(0.01f, s.limbIkBlend);
            foreach (Limb l in _limbs)
            {
                if (l.Arm && ((l.Right && skipRightArm) || (!l.Right && skipLeftArm)))
                {
                    l.Weight = l.WallWeight = 0f;
                    continue;
                }

                Vector3 contact = l.Contact.position;
                Vector3? goal = null;
                float wantWall = 0f;

                // Chaîne et mur grimpés : les mains vont sur la chaîne, ou sur le mur devant lui.
                if (l.Arm && chain.HasValue)
                {
                    Vector3 c = chain.Value;
                    goal = new Vector3(c.x, contact.y, c.z) + right * (l.Right ? 0.4f : -0.4f) - fwd * 0.3f;
                    wantWall = 1f;
                }
                else if (l.Arm && climbWall && Physics.Raycast(contact - fwd * 3f, fwd, out RaycastHit wall, 8f, ~0, QueryTriggerInteraction.Ignore)
                         && IsDecor(wall.collider))
                {
                    goal = wall.point - fwd * 0.15f;
                    wantWall = 1f;
                }
                // Course : la main écartée vers un mur à portée s'y plaque (frappes des courses enragées).
                else if (l.Arm && running)
                {
                    float lateral = Vector3.Dot(contact - _self.position, right) * (l.Right ? 1f : -1f);
                    Vector3 side = right * (l.Right ? 1f : -1f);
                    Vector3 shoulder = l.A.position;
                    if (lateral > s.limbIkWallSpread
                        && Physics.Raycast(shoulder, (side * 0.8f - up * 0.6f).normalized, out RaycastHit w, s.limbIkWallReach, ~0, QueryTriggerInteraction.Ignore)
                        && IsDecor(w.collider) && Mathf.Abs(w.normal.y) < 0.4f)
                    {
                        goal = w.point + w.normal * 0.15f;
                        wantWall = Mathf.Clamp01((lateral - s.limbIkWallSpread) / 1.5f);
                    }
                }

                // Appui au sol : le point de contact, s'il est près du sol animé, va sur le sol réel (marche, pente, bord).
                if (!goal.HasValue)
                {
                    float above = Vector3.Dot(contact - _self.position, up);
                    // Écart du sol réel au plan du sol du traqueur, sous ce contact : 0 sur un sol plat, la marche ou la pente sinon.
                    if (above < s.limbIkPlantHeight && Ground(contact + up * 3f, 6f, out Vector3 g))
                    {
                        float offset = Vector3.Dot(g - _self.position, up);
                        if (Mathf.Abs(offset) > 0.03f && Mathf.Abs(offset) < s.limbIkMaxReach)
                        {
                            goal = contact + up * offset;
                            Planted++;
                        }
                    }
                }

                l.WallWeight = Mathf.MoveTowards(l.WallWeight, wantWall, rate);
                l.Weight = Mathf.MoveTowards(l.Weight, goal.HasValue ? 1f : 0f, rate);
                if (l.WallWeight > 0.5f)
                    OnWall++;
                if (!goal.HasValue || l.Weight <= 0.001f)
                    continue;
                // Le point de contact (sous les griffes) va sur le but : la cheville ou le poignet se déplace d'autant.
                Vector3 tip = l.E.position + (goal.Value - contact);
                SolveTwoBone(l.A, l.M, l.E, tip, wantWall > 0f ? Mathf.Min(l.Weight, l.WallWeight) : l.Weight);
            }
        }

        private static bool IsDecor(Collider c) =>
            c != null && c.attachedRigidbody == null && c.GetComponentInParent<ItHunts.Player.PlayerHealth>() == null
            && c.GetComponentInParent<Stalker>() == null;

        private bool Ground(Vector3 from, float length, out Vector3 point)
        {
            int n = Physics.RaycastNonAlloc(from, -_self.up, Hits, length, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            point = from;
            for (int i = 0; i < n; i++)
            {
                if (!IsDecor(Hits[i].collider) || Hits[i].normal.y < 0.5f || Hits[i].distance >= best)
                    continue;
                best = Hits[i].distance;
                point = Hits[i].point;
            }

            return best < float.PositiveInfinity;
        }

        /// <summary>
        /// IK analytique à deux os (pure, testable) : oriente a puis m pour que e arrive sur target (au plus loin le long du membre
        /// tendu), le genou ou le coude restant du côté où l'animation le mettait ; poids 0 à 1. Rotations seules.
        /// </summary>
        public static void SolveTwoBone(Transform a, Transform m, Transform e, Vector3 target, float weight)
        {
            if (weight <= 0.001f)
                return;
            Quaternion ra = a.rotation, rm = m.rotation;
            Vector3 ap = a.position, mp = m.position, ep = e.position;
            float l1 = Vector3.Distance(ap, mp), l2 = Vector3.Distance(mp, ep);
            Vector3 toT = target - ap;
            float d = Mathf.Clamp(toT.magnitude, Mathf.Abs(l1 - l2) + 1e-3f, l1 + l2 - 1e-3f);
            Vector3 dir = toT.sqrMagnitude > 1e-6f ? toT.normalized : (ep - ap).normalized;
            Vector3 bend = Vector3.ProjectOnPlane(mp - ap, dir);                 // côté du genou ou du coude dans l'animation
            if (bend.sqrMagnitude < 1e-6f)
                bend = Vector3.ProjectOnPlane(mp - (ap + ep) * 0.5f, dir);
            if (bend.sqrMagnitude < 1e-6f)
                bend = Vector3.Cross(dir, Vector3.right);
            bend.Normalize();
            float cosA = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
            float sinA = Mathf.Sqrt(1f - cosA * cosA);
            Vector3 elbow = ap + (dir * cosA + bend * sinA) * l1;
            a.rotation = Quaternion.FromToRotation(mp - ap, elbow - ap) * a.rotation;
            m.rotation = Quaternion.FromToRotation(e.position - m.position, ap + dir * d - m.position) * m.rotation;
            if (weight < 0.999f)
            {
                a.rotation = Quaternion.Slerp(ra, a.rotation, weight);
                m.rotation = Quaternion.Slerp(rm, m.rotation, weight);
            }
        }
    }
}
