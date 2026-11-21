using System.Linq;
using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Traqueur, playtest du 2026-11-21 (point 11, en attendant le nouveau gréement) : bouche grande ouverte au cri (os de la mâchoire
    /// et forme), mâchoire qui pend et oscille ; mains sur un mur seulement à portée réelle ; os par rôle (prêt pour un Humanoid).
    /// </summary>
    public class StalkerFaceIkTests
    {
        private GameObject _model;
        private StalkerSettings _s;

        [SetUp]
        public void SetUp()
        {
            _model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx"));
            _s = Object.Instantiate(AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset"));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_model);
            Object.DestroyImmediate(_s);
        }

        private Transform[] Bones => _model.GetComponentsInChildren<Transform>(true);

        private StalkerFace NewFace()
        {
            SkinnedMeshRenderer head = _model.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(r => r.sharedMesh != null && r.sharedMesh.GetBlendShapeIndex("Cri") >= 0);
            Assert.IsNotNull(head, "tête avec la forme Cri");
            return new StalkerFace(Bones, _model.transform, head, _s);
        }

        [Test]
        public void Rig_TousLesRolesTrouves_EtEquivalentsHumanoid()
        {
            foreach (StalkerRig.Role r in System.Enum.GetValues(typeof(StalkerRig.Role)))
                Assert.IsNotNull(StalkerRig.Find(Bones, r), $"os du rôle {r} ({StalkerRig.V4Name(r)})");
            Assert.That(StalkerRig.Human(StalkerRig.Role.Machoire), Is.EqualTo(HumanBodyBones.Jaw));
            Assert.That(StalkerRig.Human(StalkerRig.Role.MainD), Is.EqualTo(HumanBodyBones.RightHand));
            Assert.That(StalkerRig.Human(StalkerRig.Role.ContactMainD), Is.Null, "points de contact : gardés par leur nom");
        }

        [Test]
        public void Cri_OuvreLaMachoireEnGrand_OsEtForme()
        {
            StalkerFace face = NewFace();
            Transform jaw = face.JawBone;
            Assert.IsNotNull(jaw, "os Machoire");
            Transform head = StalkerRig.Find(Bones, StalkerRig.Role.Tete);
            Vector3 chinLocal = head.InverseTransformPoint(jaw.position + _model.transform.forward * 0.6f - _model.transform.up * 0.2f);
            Vector3 chinBefore = head.TransformPoint(chinLocal);
            Vector3 jawPointLocal = jaw.InverseTransformPoint(chinBefore);
            for (int i = 0; i < 30; i++)
                face.Update(new StalkerFaceInput { Now = 10f + i / 60f, LastCry = 9.6f, LastGrowl = -99f }, _s, 1f / 60f);
            Assert.That(face.JawAngle, Is.EqualTo(_s.faceJawCryAngle).Within(0.5f), "ouverture au cri");
            Assert.That(face.Cry, Is.GreaterThan(90f), "forme Cri");
            Vector3 chinAfter = jaw.TransformPoint(jawPointLocal);
            Assert.That(chinAfter.y, Is.LessThan(chinBefore.y - 0.15f), "le menton descend : la bouche s'ouvre");
            Assert.That(jaw.localScale, Is.EqualTo(Vector3.one).Using(UnityEngine.TestTools.Utils.Vector3EqualityComparer.Instance), "aucune échelle d'os");
        }

        [Test]
        public void Machoire_PendEtOscilleEnContinu()
        {
            StalkerFace face = NewFace();
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < 600; i++)
            {
                face.Update(new StalkerFaceInput { Now = 20f + i / 60f, LastCry = -99f, LastGrowl = -99f }, _s, 1f / 60f);
                min = Mathf.Min(min, face.JawAngle);
                max = Mathf.Max(max, face.JawAngle);
            }

            Assert.That(min, Is.GreaterThan(0f), "toujours un peu pendante");
            Assert.That(max - min, Is.GreaterThan(6f), "oscillation visible (≥ 6°)");
            Assert.That(face.Jaw, Is.GreaterThan(10f), "forme MachoirePendante");
        }

        [Test]
        public void IK_MainSurUnMur_SeulementAPorteeReelle()
        {
            Transform a = StalkerRig.Find(Bones, StalkerRig.Role.BrasD), m = StalkerRig.Find(Bones, StalkerRig.Role.AvantBrasD),
                e = StalkerRig.Find(Bones, StalkerRig.Role.MainD), c = StalkerRig.Find(Bones, StalkerRig.Role.ContactMainD);
            float reach = StalkerLimbIK.Reach(a, m, e, c);
            Assert.That(reach, Is.GreaterThan(3f));
            Assert.That(_s.limbIkWallReach, Is.GreaterThan(reach * _s.limbIkReachMargin),
                "la recherche de mur porte plus loin que le bras : sans le contrôle, la main se tendait dans le vide");
            Vector3 shoulder = a.position, side = _model.transform.right;
            Assert.IsTrue(StalkerLimbIK.WithinReach(shoulder, shoulder + side * reach * 0.9f, reach, _s.limbIkReachMargin), "à portée");
            Assert.IsFalse(StalkerLimbIK.WithinReach(shoulder, shoulder + side * reach * 1.05f, reach, _s.limbIkReachMargin), "trop loin : aucune IK");
        }

        [Test]
        public void Prise_AligneeSurLeJoueur_Reglages()
        {
            Assert.That(_s.grabFaceRate, Is.GreaterThan(_s.turnRate), "pivote vers le joueur plus vite qu'en marche");
            Assert.That(_s.grabIkWeight, Is.InRange(0.5f, 1f), "main de prise tirée vers le joueur pendant la fente");
        }
    }
}
