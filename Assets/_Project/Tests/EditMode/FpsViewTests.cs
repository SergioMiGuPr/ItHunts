using ItHunts.Combat;
using ItHunts.Hud;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Tests
{
    /// <summary>
    /// Étape 5 : mouvement des mains (intensité de confort), HUD discret, prefab (caméra des mains, couches, HUD uGUI),
    /// plafond du bonus de lancer et immunité après un étourdissement.
    /// </summary>
    public class FpsViewTests
    {
        private FpsViewSettings _cfg;

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<FpsViewSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        // ---------- Mouvement des mains ----------

        [Test]
        public void Intensite_AgitSurBalancementBalancierPlongee()
        {
            ViewmodelMotion.Sway(5f, -3f, _cfg, 1f, out Vector3 full, out Vector3 fullEuler);
            ViewmodelMotion.Sway(5f, -3f, _cfg, 0.5f, out Vector3 half, out Vector3 halfEuler);
            ViewmodelMotion.Sway(5f, -3f, _cfg, 0f, out Vector3 none, out Vector3 noneEuler);
            Assert.That(full.magnitude, Is.GreaterThan(0f));
            Assert.That(Vector3.Distance(half, full * 0.5f), Is.LessThan(1e-5f), "50 % : moitié du balancement");
            Assert.That(Vector3.Distance(halfEuler, fullEuler * 0.5f), Is.LessThan(1e-4f));
            Assert.That(none.magnitude + noneEuler.magnitude, Is.EqualTo(0f), "0 % : rien");

            Vector3 bob = ViewmodelMotion.Bob(0.5f, 7.5f, true, _cfg, 1f);
            Assert.That(bob.magnitude, Is.GreaterThan(0f));
            Assert.That(ViewmodelMotion.Bob(0.5f, 7.5f, true, _cfg, 0f).magnitude, Is.EqualTo(0f));
            Assert.That(Vector3.Distance(ViewmodelMotion.Bob(0.5f, 7.5f, true, _cfg, 0.5f), bob * 0.5f), Is.LessThan(1e-5f));

            Assert.That(ViewmodelMotion.LandingDip(10f, _cfg, 1f), Is.EqualTo(0.04f).Within(1e-5f), "0.04 m pour 10 m/s");
            Assert.That(ViewmodelMotion.LandingDip(10f, _cfg, 0f), Is.EqualTo(0f));
            Assert.That(ViewmodelMotion.LandingDip(50f, _cfg, 1f), Is.EqualTo(_cfg.landingDipMax), "plafonnée");
        }

        [Test]
        public void Intensite_SansEffetSurLesGestesNiLesPoses()
        {
            // Les gestes et les poses d'état n'ont pas de paramètre d'intensité : mêmes valeurs quel que soit le réglage.
            Assert.That(ViewmodelMotion.Punch(0.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(ViewmodelMotion.Punch(0f) + ViewmodelMotion.Punch(1f), Is.EqualTo(0f));
            Assert.That(ViewmodelMotion.ThrowCurve(0.15f, _cfg.throwWindup, _cfg.throwPush), Is.LessThan(0f), "recul d'abord");
            Assert.That(ViewmodelMotion.ThrowCurve(0.55f, _cfg.throwWindup, _cfg.throwPush), Is.GreaterThan(0.1f), "puis poussée");
            Assert.That(ViewmodelMotion.SpeedLag(new Vector3(0f, 0f, 20f), _cfg).z, Is.EqualTo(-_cfg.speedLagAt20).Within(1e-5f));
        }

        [Test]
        public void Balancier_RienAlArretNiEnLAir_PlusRapideEnCourant()
        {
            Assert.That(ViewmodelMotion.Bob(0.3f, 0f, true, _cfg, 1f), Is.EqualTo(Vector3.zero), "à l'arrêt");
            Assert.That(ViewmodelMotion.Bob(0.3f, 10f, false, _cfg, 1f), Is.EqualTo(Vector3.zero), "en l'air");
            Assert.That(ViewmodelMotion.AdvanceBobPhase(1f, 10f, false, _cfg, 0.1f), Is.EqualTo(1f), "phase figée en l'air");
            float walk = ViewmodelMotion.AdvanceBobPhase(0f, 7.5f, true, _cfg, 1f);
            float fast = ViewmodelMotion.AdvanceBobPhase(0f, 30f, true, _cfg, 1f);
            Assert.That(walk, Is.EqualTo(1.8f).Within(1e-4f), "1.8 pas/s à 7.5 m/s");
            Assert.That(fast, Is.EqualTo(1.8f * _cfg.bobMaxSpeedFactor).Within(1e-4f), "borné au-delà");
        }

        // ---------- HUD discret ----------

        private static float Step(GaugeFade g, float value, float max, bool alert, float time) =>
            g.Update(value, max, alert, time, 1f / 60f, 0.4f, 1.5f, 1.5f, 0.2f);

        [Test]
        public void HudDiscret_PleineEtInutiliseeSEstompe_UsageEtRefusRemontentToutDeSuite()
        {
            var g = new GaugeFade();
            float t = 0f;
            for (; t < 1.4f; t += 1f / 60f)
                Step(g, 3f, 3f, false, t);
            Assert.That(g.Alpha, Is.EqualTo(1f), "pas avant 1.5 s");
            for (; t < 3f; t += 1f / 60f)
                Step(g, 3f, 3f, false, t);
            Assert.That(g.Alpha, Is.EqualTo(0.4f).Within(1e-3f), "estompée à 40 %");

            Step(g, 2f, 3f, false, t);
            Assert.That(g.Alpha, Is.EqualTo(1f), "charge utilisée : 100 % tout de suite");
            for (t += 0.1f; t < 5f; t += 1f / 60f)
                Step(g, 2.5f, 3f, false, t);
            Assert.That(g.Alpha, Is.EqualTo(1f), "en recharge : reste à 100 %");

            var r = new GaugeFade();
            for (float u = 0f; u < 3f; u += 1f / 60f)
                Step(r, 3f, 3f, false, u);
            Assume.That(r.Alpha, Is.LessThan(0.5f));
            Step(r, 3f, 3f, true, 3f);
            Assert.That(r.Alpha, Is.EqualTo(1f), "refus : 100 % tout de suite");
        }

        [Test]
        public void HudDiscret_UneChargeQuiRevientClignoteUneFois()
        {
            var g = new GaugeFade();
            Step(g, 1.9f, 3f, false, 0f);
            Assert.That(g.Blinking(0f), Is.False);
            Step(g, 2.0f, 3f, false, 1f);
            Assert.That(g.Blinking(1f), Is.True, "la charge revient : clignote");
            Assert.That(g.Blinking(1.25f), Is.False, "une seule fois (0.2 s)");
            Step(g, 2.5f, 3f, false, 2f);
            Assert.That(g.Blinking(2f), Is.False, "pas pendant la recharge");
        }

        // ---------- Ajouts G ----------

        [Test]
        public void Lancer_BonusDeVitessePlafonne()
        {
            var tk = ScriptableObject.CreateInstance<TelekinesisOrbSettings>();
            try
            {
                Assert.That(tk.throwSpeedBonusCap, Is.EqualTo(20f));
                Vector3 v = TelekinesisOrb.ThrowVelocity(Vector3.forward, new Vector3(0f, 0f, 40f), tk);
                Assert.That(v.magnitude, Is.EqualTo(tk.throwSpeed + 20f).Within(1e-3f), "40 m/s vers la cible : +20 seulement");
                v = TelekinesisOrb.ThrowVelocity(Vector3.forward, new Vector3(0f, 0f, 12f), tk);
                Assert.That(v.magnitude, Is.EqualTo(tk.throwSpeed + 12f).Within(1e-3f), "sous le plafond : tout");
            }
            finally
            {
                Object.DestroyImmediate(tk);
            }
        }

        [Test]
        public void Etourdissement_ImmunitéDe2sApresLaFin()
        {
            var dummy = ScriptableObject.CreateInstance<TrainingDummySettings>();
            try
            {
                Assert.That(dummy.stunImmunity, Is.EqualTo(2f));
                AreaEffects.StunTimes(10f, dummy, out float until, out float immune);
                Assert.That(until, Is.EqualTo(11.5f).Within(1e-4f));
                Assert.That(immune, Is.EqualTo(13.5f).Within(1e-4f), "2 s après la fin");
                Assert.That(AreaEffects.CanStun(11f, until, immune), Is.False, "déjà étourdi");
                Assert.That(AreaEffects.CanStun(12f, until, immune), Is.False, "immunisé");
                Assert.That(AreaEffects.CanStun(13.5f, until, immune), Is.True, "de nouveau possible");
                Assert.That(AreaEffects.CanStun(0f, float.NegativeInfinity, float.NegativeInfinity), Is.True, "jamais étourdi");
            }
            finally
            {
                Object.DestroyImmediate(dummy);
            }
        }

        // ---------- Prefab, couches, réglages ----------

        [Test]
        public void Couches_DesMains_UneParJoueur()
        {
            for (int i = 1; i <= 4; i++)
                Assert.That(LayerMask.NameToLayer(FirstPersonView.LayerPrefix + i), Is.EqualTo(7 + i), $"{FirstPersonView.LayerPrefix}{i}");
        }

        [Test]
        public void Prefab_MainsParPasseDeRendu_OrbeDansLaPaumeDroite_HudUgui()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var view = prefab.GetComponent<FirstPersonView>();
            Assert.That(view, Is.Not.Null);
            var so = new SerializedObject(view);
            foreach (string field in new[] { "settings", "viewRoot", "rightHand", "leftHand", "rightFingers", "leftFingers", "orb" })
                Assert.That(so.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(so.FindProperty("overlayCamera").objectReferenceValue, Is.Null, "option 2 : pas de caméra des mains");
            Assert.That(prefab.GetComponentsInChildren<Camera>(true).Length, Is.EqualTo(1), "une seule caméra par joueur");
            Camera main = prefab.GetComponentInChildren<UnityEngine.InputSystem.PlayerInput>().camera;
            Assert.That(main.GetUniversalAdditionalCameraData().cameraStack, Is.Empty);
            var orb = (Transform)so.FindProperty("orb").objectReferenceValue;
            Assert.That(orb.parent, Is.SameAs(so.FindProperty("rightHand").objectReferenceValue), "orbe dans la main droite");
            Assert.That(orb.localScale.x, Is.EqualTo(0.08f).Within(1e-4f));

            var hud = prefab.GetComponent<PlayerHud>();
            Assert.That(hud, Is.Not.Null);
            var hso = new SerializedObject(hud);
            foreach (string field in new[] { "settings", "feedback", "tkFeedback", "damageFeedback" })
                Assert.That(hso.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
            Assert.That(AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/_Project/Scripts/Runtime/Hud/MovementHud.cs"), Is.Null, "HUD provisoire retiré");
        }

        [Test]
        public void Renderers_PassesDesMains_ProfondeurEffaceeMainsA60_HUD()
        {
            int mask = 0;
            for (int i = 1; i <= 4; i++)
                mask |= 1 << LayerMask.NameToLayer(FirstPersonView.LayerPrefix + i);
            foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                Assert.That(data, Is.Not.Null, path);
                Assert.That(data.opaqueLayerMask.value & mask, Is.EqualTo(0), $"{path} : les passes normales ignorent les mains");
                Assert.That(data.transparentLayerMask.value & mask, Is.EqualTo(0));
                var clear = data.rendererFeatures.OfType<FirstPersonDepthClearFeature>().SingleOrDefault();
                Assert.That(clear, Is.Not.Null, $"{path} : profondeur effacée avant les mains");
                var passes = data.rendererFeatures.OfType<RenderObjects>().Where(r => r.name.StartsWith("ItHunts Mains")).ToList();
                Assert.That(passes.Count, Is.EqualTo(3), "mains, transparents des mains (verre), HUD");
                RenderObjects hands = passes.Single(r => r.settings.filterSettings.RenderQueueType == RenderQueueType.Opaque);
                Assert.That(hands.settings.filterSettings.PassNames, Does.Contain("ItHuntsOutline"), "contour des mains (coque inversée)");
                RenderObjects glass = passes.Single(r => r.name.EndsWith("transparents"));
                Assert.That(glass.settings.cameraSettings.overrideCamera && glass.settings.cameraSettings.cameraFieldOfView == 60f, Is.True, "verre au FOV des mains");
                Assert.That(glass.settings.enableWrite, Is.False);
                Assert.That(hands.settings.cameraSettings.overrideCamera, Is.True);
                Assert.That(hands.settings.cameraSettings.cameraFieldOfView, Is.EqualTo(60f), "mains à 60°");
                Assert.That(hands.settings.filterSettings.LayerMask.value, Is.EqualTo(mask));
                Assert.That(data.rendererFeatures.IndexOf(clear), Is.LessThan(data.rendererFeatures.IndexOf(hands)), "effacer avant de dessiner");
                RenderObjects hudPass = passes.Single(r => r.name.EndsWith("HUD"));
                Assert.That(hudPass.settings.filterSettings.PassNames, Is.EqualTo(new[] { "SRPDefaultUnlit" }), "le HUD ne redessine pas le verre");
                Assert.That(hudPass.settings.cameraSettings.overrideCamera, Is.False, "le HUD garde la caméra du joueur");
                Assert.That(data.rendererFeatures.IndexOf(hudPass), Is.GreaterThan(data.rendererFeatures.IndexOf(hands)), "HUD après les mains");
            }
        }

        [Test]
        public void Rendu_ContoursPrePasseGpuResidentDrawerAmbiance()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var contours = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().Single(f => f.name == "ItHunts Contours");
            Assert.That(contours.passMaterial, Is.Not.Null);
            Assert.That(contours.passMaterial.name, Is.EqualTo("M_Contours"));
            Assert.That(contours.injectionPoint, Is.EqualTo(FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents));
            Assert.That(contours.requirements, Is.EqualTo(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal));
            var so = new SerializedObject(data);
            Assert.That(so.FindProperty("m_PrepassLayerMask.m_Bits").longValue, Is.EqualTo(so.FindProperty("m_OpaqueLayerMask.m_Bits").longValue),
                "pré-passe sans les couches des mains (contours fantômes, SSAO)");
            var rp = new SerializedObject(AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset"));
            Assert.That(rp.FindProperty("m_GPUResidentDrawerMode").intValue, Is.EqualTo(0), "GPU Resident Drawer coupé : mesuré 37 ms contre 9 ms sur la carte aléatoire");
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            Assert.That(gs.FindProperty("m_BrgStripping").intValue, Is.EqualTo(2), "BatchRendererGroup Variants : Keep All");
            var mid = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Intermediaire.asset");
            Assert.That(mid.ambient.r, Is.EqualTo(0.055f).Within(1e-4f));
            Assert.That(mid.fogStart, Is.EqualTo(10f));
            Assert.That(mid.fogEnd, Is.EqualTo(85f));
            Assert.That(AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Horreur.asset"), Is.Not.Null);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            Assert.That(prefab.GetComponent<HandsOutline>(), Is.Not.Null, "contour des mains et des artefacts");
        }

        [Test]
        public void Reglages_ParDefaut()
        {
            var fps = AssetDatabase.LoadAssetAtPath<FpsViewSettings>("Assets/_Project/Settings/FpsView_Default.asset");
            var hud = AssetDatabase.LoadAssetAtPath<HudSettings>("Assets/_Project/Settings/Hud_Default.asset");
            Assert.That(fps, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            Assert.That(fps.viewmodelFov, Is.EqualTo(60f));
            Assert.That(fps.swayMaxOffset, Is.EqualTo(0.04f));
            Assert.That(fps.swayMaxAngle, Is.EqualTo(4f));
            Assert.That(fps.bobAmplitude, Is.EqualTo(0.015f));
            Assert.That(fps.landingDipPer10, Is.EqualTo(0.04f));
            Assert.That(fps.slideDrop, Is.EqualTo(0.06f));
            Assert.That(fps.slideRoll, Is.EqualTo(6f));
            Assert.That(fps.speedLagAt20, Is.EqualTo(0.03f));
            Assert.That(fps.repelGestureDuration, Is.EqualTo(0.15f));
            Assert.That(fps.throwGestureDuration, Is.EqualTo(0.2f));
            Assert.That(fps.grabGestureDuration, Is.EqualTo(0.15f));
            Assert.That(hud.idleAlpha, Is.EqualTo(0.4f));
            Assert.That(hud.idleDelay, Is.EqualTo(1.5f));
            Assert.That(hud.safeMargin, Is.EqualTo(0.05f));
            Assert.That(hud.scale, Is.EqualTo(1f));
        }
    }
}
