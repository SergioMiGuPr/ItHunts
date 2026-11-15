using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Âmes et économie (étape 7, D-043) : tirage avec la seed, prix et refus, ×2 en Enragé, coop, morts sans joueur.</summary>
    public class EconomyTests
    {
        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
            {
                if (o is GameObject go)
                {
                    foreach (var p in go.GetComponents<PlayerHealth>())
                        Invoke(p, "OnDisable");
                    foreach (var d in go.GetComponents<Damageable>())
                        Invoke(d, "OnDisable");
                    foreach (var s in go.GetComponents<SoulSystem>())
                        Invoke(s, "OnDisable");
                    foreach (var c in go.GetComponents<Chest>())
                        Invoke(c, "OnDisable");
                }

                if (o != null)
                    Object.DestroyImmediate(o);
            }

            _made.Clear();
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        private T Track<T>(T o) where T : Object
        {
            _made.Add(o);
            return o;
        }

        private PlayerHealth Player(Vector3 at)
        {
            var go = Track(new GameObject("Joueur (test)"));
            go.transform.position = at;
            var p = go.AddComponent<PlayerHealth>();
            p.Init();
            Invoke(p, "OnEnable");
            return p;
        }

        [Test]
        public void Assets_ValeursValidees_HuitAmeliorationsDecrites()
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            Assert.That(e, Is.Not.Null);
            Assert.That(new[] { e.soulLifetime, e.soulBlinkTime, e.collectRadius, e.pullSpeed, e.involvementWindow, e.enragedMultiplier },
                Is.EqualTo(new[] { 30f, 5f, 8f, 25f, 5f, 2f }));
            Assert.That(new[] { e.chestPrice, e.lockedDoorPrice, e.healPrice, e.choiceCount, e.merchantOffers }, Is.EqualTo(new[] { 30, 50, 35, 3, 3 }));
            Assert.That(e.healAmount, Is.EqualTo(40f));
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePool>("Assets/_Project/Settings/UpgradePool_Default.asset");
            Assert.That(pool.upgrades.Count, Is.EqualTo(7), "7 améliorations de stats dans les coffres (le barillet passe au marchand, D-050)");
            Assert.That(pool.upgrades.All(u => u != null && !string.IsNullOrEmpty(u.description) && !u.description.Contains("\n")), Is.True, "une ligne chacune");
            Assert.That(pool.upgrades.Select(u => u.kind).Distinct().Count(), Is.EqualTo(7), "une par effet");
            var barrel = AssetDatabase.LoadAssetAtPath<UpgradePool>("Assets/_Project/Settings/UpgradePool_Barillet.asset");
            Assert.That(barrel.upgrades.Count, Is.EqualTo(5), "5 améliorations du barillet, provisoires (D-050)");
            Assert.That(barrel.upgrades.All(u => u.kind.ToString().EndsWith("Barillet")), Is.True);
            Assert.That(pool.upgrades.All(u => u.merchantPrice >= 40 && u.merchantPrice <= 80), Is.True, "prix au marchand 40 à 80");
            var ghoul = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            var flyer = AssetDatabase.LoadAssetAtPath<FlyerSettings>("Assets/_Project/Settings/Flyer_Default.asset");
            var stalker = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            Assert.That(new[] { ghoul.soulValue, flyer.soulValue, stalker.soulValue }, Is.EqualTo(new[] { 3, 2, 0 }), "le traqueur ne donne jamais d'âmes");
        }

        [Test]
        public void Tirage_MemeSeedMemeChoix_TroisDifferents()
        {
            int[] a = SoulMath.Draw(1234, 0, 8, 3), b = SoulMath.Draw(1234, 0, 8, 3);
            Assert.That(a, Is.EqualTo(b), "même seed, même numéro : même tirage");
            Assert.That(a.Distinct().Count(), Is.EqualTo(3), "trois améliorations différentes");
            Assert.That(a.All(i => i >= 0 && i < 8), Is.True);
            bool differs = false;
            for (int k = 1; k < 6 && !differs; k++)
                differs |= !SoulMath.Draw(1234, k, 8, 3).SequenceEqual(a);
            Assert.That(differs, Is.True, "les coffres suivants tirent autre chose");
            Assert.That(SoulMath.PickSpot(77, 5), Is.EqualTo(SoulMath.PickSpot(77, 5)), "marchand : même seed, même emplacement");
            Assert.That(SoulMath.Draw(1, 0, 2, 3).Length, Is.EqualTo(3), "pool trop petit : doublons permis");
        }

        [Test]
        public void Prix_EtRefus_RienNEstRetire()
        {
            var go = Track(new GameObject("Run (test)"));
            var run = go.AddComponent<RunState>();
            run.Configure(null, 42);
            int refused = 0;
            System.Action<int> onRefused = id => refused++;
            RunState.Refused += onRefused;
            try
            {
                run.AddSouls(5, 20);
                Assert.That(run.TrySpend(5, 30), Is.False, "20 âmes, coffre à 30 : refus");
                Assert.That(refused, Is.EqualTo(1), "refus signalé (rouge, son)");
                Assert.That(run.Souls(5), Is.EqualTo(20), "rien n'est retiré");
                run.AddSouls(5, 15);
                Assert.That(run.TrySpend(5, 30), Is.True);
                Assert.That(run.Souls(5), Is.EqualTo(5));
                Assert.That(run.TrySpend(5, 0), Is.True, "coffre gratuit");
                Assert.That(run.Souls(9), Is.EqualTo(0), "réserves individuelles : chacun dépense la sienne");
                Assert.That(SoulMath.CanAfford(50, 50), Is.True);
                Assert.That(SoulMath.CanAfford(49, 50), Is.False);
            }
            finally
            {
                RunState.Refused -= onRefused;
            }
        }

        [Test]
        public void HausseDuPrix_ParAchatParJoueurEtParCategorie()
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            Assert.That(new[] { e.chestPriceIncrease, e.merchantPriceIncrease, e.doorPriceIncrease }, Is.EqualTo(new[] { 0f, 25f, 0f }), "marchand de l artefact : +25 % par achat (D-050), le reste à 0 %");
            Assert.That(SoulMath.RaisedPrice(30, 0f, 5), Is.EqualTo(30), "0 % : prix fixe");
            Assert.That(SoulMath.RaisedPrice(30, 20f, 1), Is.EqualTo(36));
            Assert.That(SoulMath.RaisedPrice(30, 20f, 2), Is.EqualTo(43), "composée : 30 × 1,2²");
            Assert.That(SoulMath.RaisedPrice(0, 50f, 3), Is.EqualTo(0), "coffre gratuit");

            var go = Track(new GameObject("Run (test)"));
            var run = go.AddComponent<RunState>();
            run.Configure(null, 42);
            run.AddSouls(1, 200);
            run.AddSouls(2, 200);
            Assert.That(run.TryBuy(1, PriceKind.Coffre, 30, 20f), Is.True);
            Assert.That(run.Souls(1), Is.EqualTo(170));
            Assert.That(run.PriceFor(1, PriceKind.Coffre, 30, 20f), Is.EqualTo(36), "le coffre suivant coûte plus cher pour lui");
            Assert.That(run.PriceFor(2, PriceKind.Coffre, 30, 20f), Is.EqualTo(30), "pas pour l'autre joueur");
            Assert.That(run.PriceFor(1, PriceKind.Marchand, 60, 20f), Is.EqualTo(60), "ni au marchand");
            Assert.That(run.PriceFor(1, PriceKind.Porte, 50, 20f), Is.EqualTo(50), "ni aux portes");
            run.AddSouls(3, 10);
            Assert.That(run.TryBuy(3, PriceKind.Coffre, 30, 20f), Is.False, "refus");
            Assert.That(run.Purchases(3, PriceKind.Coffre), Is.EqualTo(0), "un refus ne compte pas");
            Assert.That(run.TryBuy(1, PriceKind.Coffre, 0, 20f), Is.True);
            Assert.That(run.Purchases(1, PriceKind.Coffre), Is.EqualTo(1), "un coffre gratuit ne compte pas");
        }

        [Test]
        public void Enrage_x2_AuMomentDeLaMort()
        {
            Assert.That(SoulMath.SoulsAtDeath(3, false, 2f), Is.EqualTo(3));
            Assert.That(SoulMath.SoulsAtDeath(3, true, 2f), Is.EqualTo(6), "goule en Enragé");
            Assert.That(SoulMath.SoulsAtDeath(2, true, 2f), Is.EqualTo(4), "volant en Enragé");
            Assert.That(SoulMath.SoulsAtDeath(0, true, 2f), Is.EqualTo(0), "traqueur, factice : jamais");
        }

        [Test]
        public void MortsSansJoueur_DelaiDe5s()
        {
            Assert.That(SoulMath.Involved(0, 10f, 10f, 5f), Is.False, "personne n'y est pour rien");
            Assert.That(SoulMath.Involved(7, 4.9f, 10f, 5f), Is.False, "il y a plus de 5 s");
            Assert.That(SoulMath.Involved(7, 5.5f, 10f, 5f), Is.True);
        }

        private SoulSystem Souls()
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            var sgo = Track(new GameObject("Âmes (test)"));
            var souls = sgo.AddComponent<SoulSystem>();
            var so = new SerializedObject(souls);
            so.FindProperty("settings").objectReferenceValue = e;
            so.ApplyModifiedPropertiesWithoutUndo();
            Invoke(souls, "OnEnable");
            return souls;
        }

        private RunState Run(int seed = 42)
        {
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePool>("Assets/_Project/Settings/UpgradePool_Default.asset");
            var run = Track(new GameObject("Run (test)")).AddComponent<RunState>();
            run.Configure(null, seed, pool);
            Assert.That(RunState.Current, Is.EqualTo(run));
            return run;
        }

        [Test]
        public void EclatsCommuns_SeulementSiUnJoueurYEstPourQuelqueChose()
        {
            var ghoulCfg = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            PlayerHealth near = Player(new Vector3(10f, 0f, 0f)), far = Player(new Vector3(60f, 0f, 0f));
            SoulSystem souls = Souls();

            Damageable Enemy()
            {
                var go = Track(GameObject.CreatePrimitive(PrimitiveType.Capsule));
                var d = go.AddComponent<Damageable>();
                var dso = new SerializedObject(d);
                dso.FindProperty("settings").objectReferenceValue = ghoulCfg;
                dso.FindProperty("respawns").boolValue = false;
                dso.ApplyModifiedPropertiesWithoutUndo();
                d.Init();
                return d;
            }

            Damageable alone = Enemy();
            alone.ApplyDamage(1000f, Time.time, out bool killed);
            Assert.That(killed, Is.True);
            Assert.That(souls.Count, Is.EqualTo(0), "morte sans joueur (chute, autre ennemi) : pas d'âme");

            Damageable hit = Enemy();
            hit.NoteAttacker(far.PlayerId);                              // le joueur lointain l'a touchée (balle)
            hit.ApplyDamage(1000f, Time.time, out _);
            Assert.That(souls.Count, Is.EqualTo(3), "3 éclats communs, une seule fois (plus d'éclats par joueur)");
            Assert.That(souls.Spawned, Is.EqualTo(3));
        }

        [Test]
        public void Coop_GagneesEnCommun_DepenseesIndividuellement_MortExclu()
        {
            RunState run = Run();
            PlayerHealth a = Player(new Vector3(0f, 0f, 0f)), b = Player(new Vector3(200f, 0f, 0f)), c = Player(new Vector3(-200f, 0f, 0f));
            c.Kill(Vector3.zero, 0f);
            SoulSystem souls = Souls();
            souls.Spawn(new Vector3(3f, 1.2f, 0f), 5, 0f);               // à 3 m de l'orbe de a, loin de b
            for (int i = 0; i < 120 && souls.Count > 0; i++)
                souls.Step(i / 60f, 1f / 60f);
            Assert.That(souls.Count, Is.EqualTo(0), "l'orbe de a les a ramassés");
            Assert.That(souls.CollectedBy(a.PlayerId), Is.EqualTo(5));
            Assert.That(run.Souls(a.PlayerId), Is.EqualTo(5), "celui qui ramasse reçoit ses âmes");
            Assert.That(run.Souls(b.PlayerId), Is.EqualTo(5), "l'allié loin reçoit autant : gagnées en commun");
            Assert.That(run.Souls(c.PlayerId), Is.EqualTo(0), "un joueur mort au moment de la collecte ne reçoit rien");
            Assert.That(run.TrySpend(a.PlayerId, 5), Is.True);
            Assert.That(run.Souls(b.PlayerId), Is.EqualTo(5), "dépensées individuellement : la réserve de b ne bouge pas");

            souls.Spawn(new Vector3(200f, 1.2f, 2f), 2, 1f);             // près de b : n'importe quelle orbe ramasse
            for (int i = 0; i < 120 && souls.Count > 0; i++)
                souls.Step(1f + i / 60f, 1f / 60f);
            Assert.That(souls.CollectedBy(b.PlayerId), Is.EqualTo(2));
            Assert.That(run.Souls(a.PlayerId), Is.EqualTo(2));
            Assert.That(run.Souls(b.PlayerId), Is.EqualTo(7));

            souls.Spawn(new Vector3(100f, 1.2f, 0f), 1, 2f);             // loin de tous : perdu au bout de 30 s
            souls.Step(2f + 29.9f, 1f / 60f);
            Assert.That(souls.Count, Is.EqualTo(1));
            souls.Step(2f + 30.1f, 1f / 60f);
            Assert.That(souls.Count, Is.EqualTo(0));
            Assert.That(souls.Lost, Is.EqualTo(1));
        }

        private Chest MakeChest(int price)
        {
            var e = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
            var go = Track(new GameObject("Coffre (test)"));
            var chest = go.AddComponent<Chest>();
            var so = new SerializedObject(chest);
            so.FindProperty("settings").objectReferenceValue = e;
            so.FindProperty("price").intValue = price;
            so.ApplyModifiedPropertiesWithoutUndo();
            Invoke(chest, "Awake");
            return chest;
        }

        [Test]
        public void Coffre_ParJoueur_UneFoisChacun_SonPropreChoix()
        {
            RunState run = Run();
            PlayerHealth a = Player(new Vector3(0f, 0f, -2f)), b = Player(new Vector3(1f, 0f, -2f));
            run.AddSouls(a.PlayerId, 100);
            run.AddSouls(b.PlayerId, 20);
            Chest chest = MakeChest(-1);
            Assert.That(chest.Available(a) && chest.Available(b), Is.True);

            Assert.That(chest.TryOpen(a), Is.True, "a paie 30");
            Assert.That(run.Souls(a.PlayerId), Is.EqualTo(70));
            Assert.That(chest.OpenedFor(a.PlayerId), Is.True);
            Assert.That(chest.OpenedFor(b.PlayerId), Is.False, "toujours fermé pour b");
            Assert.That(chest.ChoicesFor(a.PlayerId).Count, Is.EqualTo(3), "3 orbes pour a");
            Assert.That(chest.ChoicesFor(a.PlayerId).All(o => o.RestrictedTo == a.PlayerId && !o.Available(b)), Is.True, "b ne peut pas les prendre");
            Assert.That(chest.ChoicesFor(a.PlayerId).All(o => o.GetComponent<ViewerOnly>().PlayerId == a.PlayerId), Is.True, "vues par a seul");
            Assert.That(chest.Available(a), Is.False, "une fois par joueur");
            Assert.That(chest.TryOpen(a), Is.False);
            Assert.That(run.Souls(a.PlayerId), Is.EqualTo(70), "rien n'est retiré");

            Assert.That(chest.TryOpen(b), Is.False, "b n'a que 20 âmes : refus");
            Assert.That(chest.Available(b), Is.True, "il pourra l'ouvrir plus tard");
            run.AddSouls(b.PlayerId, 10);
            Assert.That(chest.TryOpen(b), Is.True, "ouvert pour b aussi");
            Assert.That(chest.ChoicesFor(b.PlayerId).Count, Is.EqualTo(3), "son propre choix parmi 3");
            Assert.That(chest.ChoicesFor(a.PlayerId).Count, Is.EqualTo(3), "le choix de a n'a pas bougé");
            Assert.That(chest.OpenCount, Is.EqualTo(2));

            OfferOrb take = chest.ChoicesFor(a.PlayerId)[1];
            UpgradeDefinition up = take.Upgrade;
            take.Interact(a);
            Assert.That(chest.ChosenBy(a.PlayerId), Is.True);
            Assert.That(chest.ChoicesFor(a.PlayerId).Count, Is.EqualTo(0), "les deux autres de a disparaissent");
            Assert.That(chest.ChoicesFor(b.PlayerId).Count, Is.EqualTo(3), "celles de b restent");
            Assert.That(run.UpgradesOf(a.PlayerId), Has.Member(up));
            Assert.That(run.UpgradesOf(b.PlayerId), Is.Empty);

            chest.ResetChest();
            Assert.That(chest.OpenCount, Is.EqualTo(0), "remise à zéro de la zone : fermé pour tous");
            Assert.That(chest.Available(a), Is.True);
        }

        [Test]
        public void Coffre_TexteParJoueur_OuvertPourToiOuSonPrix()
        {
            RunState run = Run();
            PlayerHealth a = Player(Vector3.zero), b = Player(Vector3.right);
            run.AddSouls(a.PlayerId, 50);
            Chest chest = MakeChest(-1);
            WorldLabel label = chest.GetComponentInChildren<WorldLabel>();
            Assert.That(label.TextFor(a), Is.EqualTo("Coffre\n30 âmes"));
            chest.TryOpen(a);
            Assert.That(label.TextFor(a), Is.EqualTo("Coffre\nouvert"), "ouvert pour a");
            Assert.That(label.TextFor(b), Is.EqualTo("Coffre\n30 âmes"), "fermé pour b, avec son prix");
            Assert.That(chest.Prompt(b), Is.EqualTo("ouvrir le coffre (30 âmes)"));
        }
    
        [Test]
        public void MarchandDeLArtefact_TroisOffresEtLeSoinEnQuatrieme()
        {
            var go = new GameObject("Marchand (test)");
            try
            {
                var m = go.AddComponent<ArtifactMerchant>();
                var so = new SerializedObject(m);
                so.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EconomySettings>("Assets/_Project/Settings/Economy_Default.asset");
                so.FindProperty("pool").objectReferenceValue = AssetDatabase.LoadAssetAtPath<UpgradePool>("Assets/_Project/Settings/UpgradePool_Barillet.asset");
                so.ApplyModifiedPropertiesWithoutUndo();
                m.Stock(42);
                var offers = m.OffersFor(42);
                Assert.That(offers.Count, Is.EqualTo(4), "3 améliorations du barillet + le soin");
                Assert.That(offers.Take(3).All(o => o.Upgrade != null && o.Upgrade.kind.ToString().EndsWith("Barillet")), Is.True);
                Assert.That(offers[3].Upgrade, Is.Null);
                Assert.That(offers[3].Heal, Is.EqualTo(40f), "soin +40 PV (D-050 révisée)");
                Assert.That(offers[3].Price, Is.EqualTo(35));
                Assert.That(offers.All(o => o.RestrictedTo == 42), Is.True, "offres de ce joueur seulement");
                m.ResetMerchant();
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TexteEnMonde_CacheParLeDecor_TestDeProfondeur()
        {
            var parent = new GameObject("Porteur (test)");
            try
            {
                WorldLabel label = WorldLabel.Create(parent.transform, Vector3.up * 2f, "Marchand", Color.white);
                Material m = label.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(m.shader.name, Is.EqualTo("ItHunts/WorldText"), "shader avec test de profondeur (pas la police intégrée)");
                Assert.That(m.mainTexture, Is.Not.Null, "texture de la police");
                string shader = System.IO.File.ReadAllText("Assets/_Project/Shaders/WorldText.shader");
                Assert.That(shader.Contains("ZTest LEqual"), Is.True, "caché par le décor");
                Assert.That(label.GetComponent<TextMesh>().characterSize * 6.4f, Is.EqualTo(0.28f).Within(0.001f), "lignes de 0.28 m : lisible à 10 m");
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void TexteEnMonde_SurUnePorteEtiree_ResteALEchelleEtUnTexteParFace()
        {
            // Cause des textes vus à travers les murs : posé sur une porte (cube étiré à 10 × 10 × 1.2 m), le texte héritait de
            // son échelle (lettres de 5 m, étirées, qui dépassaient des murs en se tournant vers la caméra).
            var level = new GameObject("Niveau (test)");
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                door.transform.SetParent(level.transform, false);
                door.transform.localScale = new Vector3(10f, 10f, 1.2f);
                WorldLabel[] labels = WorldLabel.CreateOnFaces(door.transform, "Sortie", Color.white, 0.5f);
                Assert.That(labels.Length, Is.EqualTo(2));
                foreach (WorldLabel l in labels)
                {
                    Assert.That(l.transform.lossyScale.x, Is.EqualTo(1f).Within(0.001f), "à l'échelle du monde");
                    Assert.That(l.transform.lossyScale.y, Is.EqualTo(1f).Within(0.001f));
                    Assert.That(Mathf.Abs(l.transform.position.z), Is.EqualTo(0.6f + 0.12f).Within(0.001f), "devant la face, à 0.12 m");
                    Assert.That(l.IgnoreRoot, Is.Null, "la porte cache le texte de l'autre face");
                }

                Assert.That(labels[0].transform.position.z * labels[1].transform.position.z, Is.LessThan(0f), "une face chacun");
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void LigneDeVue_DecorEntre_CacheMaisPasLesObjetsPhysiquesNiLePorteur()
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                wall.transform.position = new Vector3(0f, 0f, 5f);
                wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
                Physics.SyncTransforms();
                Assert.That(PlayerSight.DecorBetween(Vector3.zero, new Vector3(0f, 0f, 10f)), Is.True, "mur entre la caméra et le texte : caché");
                Assert.That(PlayerSight.DecorBetween(Vector3.zero, new Vector3(0f, 0f, 4f)), Is.False, "texte devant le mur : visible");
                Assert.That(PlayerSight.DecorBetween(Vector3.zero, new Vector3(0f, 0f, 10f), wall.transform), Is.False, "le porteur du texte ne le cache pas");

                wall.transform.position = new Vector3(0f, 100f, 0f);
                crate.transform.position = new Vector3(0f, 0f, 5f);
                crate.AddComponent<Rigidbody>().isKinematic = true;
                Physics.SyncTransforms();
                Assert.That(PlayerSight.DecorBetween(Vector3.zero, new Vector3(0f, 0f, 10f)), Is.False, "une caisse (objet physique) ne cache rien");
                Assert.That(PlayerSight.OutsideView(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -5f), 70f, 16f / 9f, 10f), Is.True, "derrière : hors du champ");
                Assert.That(PlayerSight.OutsideView(Vector3.zero, Vector3.forward, new Vector3(1f, 0f, 5f), 70f, 16f / 9f, 10f), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(wall);
                Object.DestroyImmediate(crate);
            }
        }

        [Test]
        public void Marchand_EnseigneAuDessusDeToutesLesEtiquettes()
        {
            // L'enseigne cachait une étiquette d'offre : elle est maintenant dans le plan des textes, au-dessus du plus haut.
            float signH = 0.4f;
            Vector3 sign = ArtifactMerchant.SignOffset(3, true, signH);
            for (int i = 0; i < 3; i++)
            {
                Vector3 o = ArtifactMerchant.OfferOffset(i, 3);
                float labelTop = o.y + 0.12f + 0.95f + 3f * 0.28f * 0.5f;    // flottement + texte de 3 lignes au-dessus de l'orbe
                Assert.That(sign.y - signH * 0.5f, Is.GreaterThanOrEqualTo(labelTop + 1.6f - 0.001f), "1.6 m au-dessus du texte le plus haut");
                Assert.That(sign.z, Is.EqualTo(o.z).Within(0.001f), "même plan que les offres");
            }

            Assert.That(sign.x, Is.EqualTo((ArtifactMerchant.OfferOffset(0, 3).x + ArtifactMerchant.HealOffset(3).x) * 0.5f).Within(0.001f), "au milieu de la rangée");
        }
    }
}
