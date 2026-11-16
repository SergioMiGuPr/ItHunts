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
        public void Ambiance_PhaseEnragee_FonduDe2sVersLHorreur_PuisRetour()
        {
            var mid = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Intermediaire.asset");
            var horror = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Horreur.asset");
            Assert.That(mid.enraged, Is.EqualTo(horror), "phase enragée : ambiance horreur");
            Assert.That(mid.enragedTransition, Is.EqualTo(2f));

            Color ambient = RenderSettings.ambientLight, fog = RenderSettings.fogColor;
            float fs = RenderSettings.fogStartDistance, fe = RenderSettings.fogEndDistance;
            bool fogOn = RenderSettings.fog;
            FogMode fm = RenderSettings.fogMode;
            var am = RenderSettings.ambientMode;
            var go = new GameObject("ambiance (test)");
            try
            {
                var sa = go.AddComponent<ItHunts.Level.SceneAmbiance>();
                typeof(ItHunts.Level.SceneAmbiance).GetField("ambiance", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(sa, mid);
                sa.StepEnraged(true, 1f);
                Assert.That(sa.EnragedBlend, Is.EqualTo(0.5f).Within(1e-4f), "à mi-chemin après 1 s");
                Assert.That(RenderSettings.fogEndDistance, Is.EqualTo((mid.fogEnd + horror.fogEnd) * 0.5f).Within(1e-3f));
                sa.StepEnraged(true, 1.5f);
                Assert.That(sa.EnragedBlend, Is.EqualTo(1f));
                Assert.That(RenderSettings.ambientLight.r, Is.EqualTo(horror.ambient.r).Within(1e-4f));
                Assert.That(ItHunts.Level.SceneAmbiance.Active.torchRange, Is.EqualTo(horror.torchRange).Within(1e-4f));
                sa.StepEnraged(false, 2f);
                Assert.That(sa.EnragedBlend, Is.EqualTo(0f), "remise à zéro de la zone : retour");
                Assert.That(ItHunts.Level.SceneAmbiance.Active, Is.EqualTo(mid), "l'asset lui-même, réglable en Play Mode");
                Assert.That(RenderSettings.fogEndDistance, Is.EqualTo(mid.fogEnd).Within(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(go);
                RenderSettings.ambientLight = ambient; RenderSettings.fogColor = fog; RenderSettings.fogStartDistance = fs;
                RenderSettings.fogEndDistance = fe; RenderSettings.fog = fogOn; RenderSettings.fogMode = fm; RenderSettings.ambientMode = am;
            }
        }

        [Test]
        public void TextesEnMonde_LisiblesA20m_InvisiblesA35m_JamaisATraversLeBrouillard()
        {
            float A(float d, float fogEnd) => ItHunts.Settings.AmbianceSettings.LabelAlpha(d, 20f, 35f, fogEnd, 0.8f);
            Assert.That(A(5f, 85f), Is.EqualTo(1f));
            Assert.That(A(20f, 85f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(A(27.5f, 85f), Is.InRange(0.3f, 0.7f));
            Assert.That(A(35f, 85f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(A(60f, 85f), Is.EqualTo(0f));
            Assert.That(A(25f, 30f), Is.EqualTo(0f).Within(1e-4f), "brouillard court : effacé avant sa fin (0.8 × 30 m)");
            var ghoul = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            var flyer = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.FlyerSettings>("Assets/_Project/Settings/Flyer_Default.asset");
            Assert.That(ghoul.eyeColor.maxColorComponent, Is.LessThan(1f), "yeux sous le seuil du bloom hors annonce");
            Assert.That(flyer.eyeColor.maxColorComponent, Is.LessThan(1f));
            Assert.That(flyer.diveGlowColor.maxColorComponent, Is.GreaterThan(1f));
        }

        [Test]
        public void Vitrine_SceneDansLeBuild_EnnemisDansChaqueEtat_AccesMenus()
        {
            const string path = "Assets/Scenes/Test_Vitrine.unity";
            Assert.That(EditorBuildSettings.scenes.Any(s => s.path == path && s.enabled), Is.True, "dans le build (menu principal, menu pause)");
            string text = System.IO.File.ReadAllText(path);
            Assert.That(text.Contains("m_Name: \"Vitrine (panneau F1"), Is.True, "composant de la vitrine");
            foreach (string label in new[] { "Annonce du bond", "Escalade sur un mur", "Tenue (t", "Morte", "Piqu", "Main plant", "Mains lev", "Revers",
                         "Baiss", "Enclos", "10 m", "50 m", "Architecture", "Mobilier", "Ar\\u00E8ne du boss", "Objets attrapables" })
                Assert.That(text.Contains(label) || System.Text.RegularExpressions.Regex.Unescape(text).Contains(System.Text.RegularExpressions.Regex.Unescape(label)), Is.True, label);
            Assert.That(System.Enum.GetValues(typeof(ItHunts.Level.VitrineGhoulState)).Length, Is.EqualTo(10));
            Assert.That(System.Enum.GetValues(typeof(ItHunts.Level.VitrineFlyerState)).Length, Is.EqualTo(5));
            Assert.That(ItHunts.Level.MainMenu.VitrineScene, Is.EqualTo("Test_Vitrine"));
            string menu = System.IO.File.ReadAllText("Assets/_Project/Scripts/Runtime/Level/MainMenu.cs");
            Assert.That(menu.Contains("\"Playtest : vitrine\""), Is.True);
            string pause = System.IO.File.ReadAllText("Assets/_Project/Scripts/Runtime/Level/PauseMenu.cs");
            Assert.That(pause.Contains("MainMenu.VitrineScene"), Is.True, "accès depuis le menu pause des scènes de test");
        }

        [Test]
        public void VueFps_OrbeEtArtefactFlottent_BarilletSansCanon_PosesPaumeVersLeHaut()
        {
            var fps = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.FpsViewSettings>("Assets/_Project/Settings/FpsView_Default.asset");
            Assert.That(fps.floatAmplitude, Is.GreaterThan(0f));
            float lo = float.MaxValue, hi = float.MinValue;
            for (float t = 0f; t < fps.floatPeriod; t += 0.05f)
            {
                float h = ItHunts.Player.ViewmodelMotion.FloatHeight(t, fps);
                lo = Mathf.Min(lo, h);
                hi = Mathf.Max(hi, h);
            }
            Assert.That(hi - lo, Is.EqualTo(2f * fps.floatAmplitude).Within(0.002f), "monte et descend");
            Assert.That(lo, Is.GreaterThan(0f), "toujours au-dessus de la paume");
            Assert.That(fps.rightHandRest.y, Is.GreaterThan(-0.2f), "mains plus hautes");

            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            Assert.That(player.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Canon"), Is.False, "plus de canon greybox");
            var so = new SerializedObject(player.GetComponent<ItHunts.Artifacts.BarrelView>());
            var muzzle = (Transform)so.FindProperty("muzzle").objectReferenceValue;
            Assert.That(muzzle.localPosition.y, Is.EqualTo(0.031f).Within(0.005f), "bouche sur la chambre du haut (active)");
            Assert.That(muzzle.localPosition.z, Is.LessThan(0.08f), "à l'avant du barillet, pas au bout d'un canon");

            var poses = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.FpsArmPoseSet>("Assets/_Project/Settings/FpsArmPoses.asset");
            foreach (var side in new[] { poses.right, poses.left })
            {
                Assert.That(side.fromClips, Is.True, "clips de la PR #5 avec leurs courbes");
                Assert.That(side.Find("Pose_OrbePaume"), Is.Not.Null);
                Assert.That(side.Find("Pose_OrbePaume").fromClip, Is.True);
            }

            // Animations à venir (clips « Anim_* ») : jouées en boucle à leur cadence.
            var anim = new ItHunts.Settings.FpsArmPoseSet.Animation { name = "Anim_Repos", frameRate = 10f };
            for (int i = 0; i < 4; i++)
                anim.frames.Add(new ItHunts.Settings.FpsArmPoseSet.Pose { name = "f" + i });
            Assert.That(anim.FrameAt(0.25f).name, Is.EqualTo("f2"));
            Assert.That(anim.FrameAt(0.45f).name, Is.EqualTo("f0"), "en boucle");
        }

        [Test]
        public void Goule_1m80_CapsuleEtLODReglables_NavMeshDesGoules()
        {
            var ghoul = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            Assert.That(ghoul.bodyHeight, Is.EqualTo(1.8f).Within(1e-3f), "à la taille du joueur");
            Assert.That(ghoul.BodyScale, Is.EqualTo(1.8f / 1.1f).Within(1e-3f));
            Assert.That(ghoul.bodyRadius, Is.InRange(0.4f, 0.7f));
            Vector3 lod = ghoul.lodTransitions;
            Assert.That(lod.x, Is.GreaterThan(lod.y));
            Assert.That(lod.y, Is.GreaterThan(lod.z));
            Assert.That(lod.z, Is.GreaterThan(0f));
            // Seuils conseillés par la DA pour la goule de 1.8 m (PR #6) : LOD1 à ≈ 5 m, LOD2 à ≈ 13 m, cachée au-delà de ≈ 105 m.
            Assert.That(lod.x, Is.EqualTo(0.25f).Within(1e-3f));
            Assert.That(lod.y, Is.EqualTo(0.10f).Within(1e-3f));
            Assert.That(lod.z, Is.EqualTo(0.012f).Within(1e-4f));
            // Modèle animé : SK_Goule, Animator et son contrôleur (9 clips), trois LOD skinnés.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Niveau/Goule.prefab");
            var anim = prefab.GetComponentInChildren<Animator>(true);
            Assert.That(anim, Is.Not.Null);
            Assert.That(anim.runtimeAnimatorController, Is.Not.Null);
            Assert.That(anim.runtimeAnimatorController.animationClips.Length, Is.EqualTo(9));
            Assert.That(prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length, Is.EqualTo(3), "trois LOD qui suivent le squelette");
            Assert.That(ghoul.contactImpactDelay, Is.EqualTo(0.24f).Within(1e-4f), "dégâts à l'impact du clip Goule_Coup");
            foreach (string scene in new[] { "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Level_01_Aleatoire.unity" })
            {
                string text = System.IO.File.ReadAllText(scene);
                Assert.That(text.Contains("smallRadius: 0.45"), Is.True, scene);
                Assert.That(text.Contains("smallHeight: 1.5"), Is.True, scene + " : agent des goules 0.45 × 1.5 m");
            }
        }

        [Test]
        public void Lueurs_YeuxRougeSombre_GriffesIvoire_TroisLODDeLaGoule()
        {
            var ghoul = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            var stalker = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            Color rest = ghoul.eyeGlowColor * ghoul.eyeGlowRest, announce = ghoul.eyeGlowColor * ghoul.eyeGlowAnnounce;
            Assert.That(announce.maxColorComponent, Is.LessThan(1f), "jamais un rouge vif (sous le seuil du bloom), même pendant l'annonce");
            Assert.That(announce.maxColorComponent, Is.GreaterThan(rest.maxColorComponent * 1.3f), "légère montée pendant l'annonce");
            Assert.That(ghoul.eyeGlowColor.r, Is.GreaterThan(ghoul.eyeGlowColor.g * 5f), "rouge");
            Color pupils = stalker.pupilGlow * stalker.pupilGlowIntensity;
            Assert.That(pupils.maxColorComponent, Is.LessThan(1f));
            Assert.That(stalker.clawGlowGrab, Is.GreaterThan(stalker.clawGlowRest * 2f), "griffes plus vives pendant la prise");
            Assert.That(stalker.clawGlow.b, Is.GreaterThan(stalker.clawGlow.r * 0.6f), "ivoire, pas rouge");

            // Les trois LOD de la goule sont des rendus de la goule (Damageable écrit l'émission sur chacun), même matériau à masque.
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Niveau/Goule.prefab");
            var lods = prefab.GetComponentsInChildren<LODGroup>(true);
            Assert.That(lods.Length, Is.EqualTo(1));
            LOD[] levels = lods[0].GetLODs();
            Assert.That(levels.Length, Is.EqualTo(3));
            foreach (LOD l in levels)
                foreach (Renderer r in l.renderers)
                {
                    Assert.That(r.transform.IsChildOf(prefab.transform), Is.True);
                    Assert.That(r.sharedMaterial.GetTexture("_EmissionMap"), Is.Not.Null, r.name + " : masque des yeux et de la gueule");
                }

            // Bras et mains du traqueur : M_Traqueur_Mains dans les scènes de jeu.
            string guid = AssetDatabase.AssetPathToGUID("Assets/Art/Materials/Ennemis/M_Traqueur_Mains.mat");
            foreach (string scene in new[] { "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Level_01_Aleatoire.unity" })
                Assert.That(System.Text.RegularExpressions.Regex.Matches(System.IO.File.ReadAllText(scene), guid).Count, Is.GreaterThanOrEqualTo(4), scene);
        }

        [Test]
        public void Ambiance_SombreParDefaut_EntreIntermediaireEtHorreur()
        {
            var mid = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Intermediaire.asset");
            var dark = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Sombre.asset");
            var horror = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.AmbianceSettings>("Assets/_Project/Settings/Ambiance_Horreur.asset");
            Assert.That(dark.ambient.r, Is.EqualTo(0.022f).Within(1e-4f));
            Assert.That(dark.fogStart, Is.EqualTo(6f));
            Assert.That(dark.fogEnd, Is.EqualTo(38f));
            Assert.That(dark.ambient.r, Is.LessThan(mid.ambient.r).And.GreaterThan(horror.ambient.r));
            Assert.That(dark.fogEnd, Is.LessThan(mid.fogEnd).And.GreaterThan(horror.fogEnd));
            Assert.That(dark.enraged, Is.EqualTo(horror), "phase enragée : horreur");
            foreach (string scene in new[] { "Assets/Scenes/Test_Movement.unity", "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Base.unity", "Assets/Scenes/Level_01_Aleatoire.unity" })
                Assert.That(System.IO.File.ReadAllText(scene).Contains(AssetDatabase.AssetPathToGUID("Assets/_Project/Settings/Ambiance_Sombre.asset")), Is.True, scene);
        }

        [Test]
        public void BrasDeLaDA_RenardEtCameleon_FpsArmsSurLeJoueur_CalqueFPS()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var arms = prefab.GetComponentInChildren<FpsArms>(true);
            Assert.That(arms, Is.Not.Null, "bras de la DA posés par FpsArms");
            var poses = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.FpsArmPoseSet>("Assets/_Project/Settings/FpsArmPoses.asset");
            Assert.That(poses.right.bones.Length, Is.EqualTo(17));
            Assert.That(poses.right.Find("Pose_Attraper_1"), Is.Not.Null);
            Assert.That(poses.right.Find("Pose_Attraper_1").fromClip, Is.True, "Pose_Attraper a des courbes");
            foreach (string id in new[] { "renard", "cameleon" })
            {
                var c = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.CharacterDefinition>($"Assets/_Project/Settings/Personnages/Character_{id}.asset");
                Assert.That(c.fpsArmRight, Is.Not.Null, id);
                Assert.That(c.fpsArmLeft, Is.Not.Null, id);
            }

            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            Assert.That(tags.FindProperty("m_RenderingLayers").GetArrayElementAtIndex(1).stringValue, Is.EqualTo("FPS"), "calque de lumière des mains");
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
