using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Jour 5, points 4 et 5 : recul décroissant de l'épée, drain du hibou (calcul, ligne de vue), lien qui part du livre, parade au
    /// réflexe, boss blessable partout (relais du corps, critique du point faible), charge bornée à son couloir.
    /// </summary>
    public class Day5CharactersBossTests
    {
        private const float Dt = 1f / 60f;

        // ---------- Épée du koala : recul décroissant ----------

        [Test]
        public void Epee_Recul_DecroitSurLeMemeEnnemi_PuisRepart()
        {
            var cfg = ScriptableObject.CreateInstance<BoneSwordSettings>();
            try
            {
                Assert.That(cfg.hitKnockback, Is.GreaterThan(0f), "léger recul à chaque coup");
                BoneSword.KnockStreak s = BoneSword.KnockStreak.Create();
                Assert.That(BoneSword.KnockbackScale(ref s, 10f, cfg), Is.EqualTo(1f).Within(1e-5f), "1er coup : recul entier");
                Assert.That(BoneSword.KnockbackScale(ref s, 10.3f, cfg), Is.EqualTo(0.6f).Within(1e-5f), "2e coup à la suite");
                Assert.That(BoneSword.KnockbackScale(ref s, 10.6f, cfg), Is.EqualTo(0.36f).Within(1e-5f), "3e coup à la suite");
                Assert.That(BoneSword.KnockbackScale(ref s, 10.9f, cfg), Is.EqualTo(0f), "4e coup : plus aucun recul (anti-martèlement)");
                Assert.That(BoneSword.KnockbackScale(ref s, 11.2f, cfg), Is.EqualTo(0f), "tant qu'on frappe sans pause");
                Assert.That(BoneSword.KnockbackScale(ref s, 11.2f + cfg.knockbackMemory + 0.01f, cfg), Is.EqualTo(1f).Within(1e-5f),
                    "après une pause, le recul repart entier");
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }

        [Test]
        public void Epee_Recul_ParEnnemi_IndependantDUnAutre()
        {
            var cfg = ScriptableObject.CreateInstance<BoneSwordSettings>();
            var a = new GameObject("Goule A").AddComponent<Damageable>();
            var b = new GameObject("Goule B").AddComponent<Damageable>();
            try
            {
                BoneSword.ResetKnockback();
                Assert.That(BoneSword.KnockbackScale(a, 5f, cfg), Is.EqualTo(1f).Within(1e-5f));
                Assert.That(BoneSword.KnockbackScale(a, 5.2f, cfg), Is.EqualTo(0.6f).Within(1e-5f));
                Assert.That(BoneSword.KnockbackScale(b, 5.3f, cfg), Is.EqualTo(1f).Within(1e-5f), "un autre ennemi recule entièrement");
            }
            finally
            {
                BoneSword.ResetKnockback();
                Object.DestroyImmediate(a.gameObject);
                Object.DestroyImmediate(b.gameObject);
                Object.DestroyImmediate(cfg);
            }
        }

        // ---------- Livre du hibou ----------

        [Test]
        public void Livre_Drain_UneSeconde_Rend20Pourcent()
        {
            var cfg = ScriptableObject.CreateInstance<SpiritBookSettings>();
            try
            {
                BookState s = BookState.Create();
                SpiritBook.Start(ref s, BookLink.Drain, 10, 10);
                float drained = 0f;
                for (int i = 0; i < 60; i++)
                    drained += SpiritBook.DrainTick(s, cfg, Dt);
                Assert.That(drained, Is.EqualTo(15f).Within(1e-3f), "15 PV/s");
                Assert.That(SpiritBook.Returned(drained, cfg), Is.EqualTo(3f).Within(1e-3f), "20 % rendus au hibou");
                Assert.That(SpiritBook.Returned(-4f, cfg), Is.EqualTo(0f), "rien sans dégâts pris");
                float missing = 20f, healed = 0f;
                s = BookState.Create();
                SpiritBook.Start(ref s, BookLink.Soin, 10, 10);
                for (int i = 0; i < 600; i++)
                {
                    float h = SpiritBook.HealTick(s, missing - healed, cfg, Dt);
                    healed += h;
                    s.Healed += h;
                }

                Assert.That(healed, Is.EqualTo(20f).Within(1e-3f), "soin : jamais au-delà des PV manquants");
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }

        [Test]
        public void Livre_LigneDeVue_UnMurDuNiveau_CoupeLeLien()
        {
            // Avant : le décor rangé sous la même racine que l'ennemi (IsChildOf(target.root)) était ignoré : le lien passait les murs.
            var cfg = ScriptableObject.CreateInstance<SpiritBookSettings>();
            var level = new GameObject("Niveau");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Mur";
            wall.transform.SetParent(level.transform, false);
            wall.transform.position = new Vector3(0f, 1f, 5f);
            wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
            var enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.name = "Goule";
            enemy.transform.SetParent(level.transform, false);
            enemy.transform.position = new Vector3(0f, 1f, 10f);
            enemy.AddComponent<Damageable>();
            try
            {
                Physics.SyncTransforms();
                Vector3 eye = new Vector3(0f, 1f, 0f), point = enemy.transform.position;
                Assert.That(SpiritBook.InSight(eye, point, enemy.transform, null, cfg), Is.False, "un mur entre les deux");
                wall.SetActive(false);
                Physics.SyncTransforms();
                Assert.That(SpiritBook.InSight(eye, point, enemy.transform, null, cfg), Is.True, "rien entre les deux (son propre collider ne compte pas)");
            }
            finally
            {
                Object.DestroyImmediate(level);
                Object.DestroyImmediate(cfg);
            }
        }

        [Test]
        public void Livre_Lien_PartDuLivre_PuisResteAccroche()
        {
            Vector3 from = Vector3.zero, to = new Vector3(10f, 0f, 0f);
            Assert.That(ArtifactModelView.LinkTip(from, to, 0f, 0.12f), Is.EqualTo(from));
            Assert.That(Vector3.Distance(ArtifactModelView.LinkTip(from, to, 0.06f, 0.12f), new Vector3(5f, 0f, 0f)), Is.LessThan(1e-4f));
            Assert.That(ArtifactModelView.LinkTip(from, to, 2f, 0.12f), Is.EqualTo(to), "accroché à la cible");
            Assert.That(ArtifactModelView.LinkTip(from, to, 0f, 0f), Is.EqualTo(to), "sans temps de lancer : tout de suite");
        }

        // ---------- Parade ----------

        [Test]
        public void Parade_AppuiAuReflexe_ApresLAnnonce_Pare()
        {
            var cfg = ScriptableObject.CreateInstance<ParrySettings>();
            try
            {
                // Coup annoncé 0.55 s avant l'impact ; appui 0.22 s après l'annonce, soit 0.33 s avant l'impact.
                float window = PlayerParry.WindowFor(cfg);
                Assert.That(window, Is.EqualTo(cfg.window + cfg.earlyGrace).Within(1e-5f));
                ParryState s = ParryState.Create();
                Assert.That(ParryMath.Press(ref s, 10f, cfg), Is.True);
                Assert.That(ParryMath.TryParry(ref s, 10.33f, cfg, true, cfg.window), Is.False, "fenêtre seule : « trop tôt » (la cause)");
                s = ParryState.Create();
                ParryMath.Press(ref s, 10f, cfg);
                Assert.That(ParryMath.TryParry(ref s, 10.33f, cfg, true, window), Is.True, "avec la marge de réflexe : parée");
                s = ParryState.Create();
                ParryMath.Press(ref s, 10f, cfg);
                Assert.That(ParryMath.TryParry(ref s, 10.5f, cfg, true, window), Is.False, "appui avant l'annonce : toujours trop tôt");
                Assert.That(PlayerParry.WindowFor(cfg, 0.2f), Is.EqualTo(window + 0.2f).Within(1e-5f), "bond : fenêtre du vol en plus");
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }

        // ---------- Boss final ----------

        [Test]
        public void Boss_PointFaible_Critique_CorpsPartout()
        {
            var cfg = ScriptableObject.CreateInstance<FinalBossSettings>();
            try
            {
                Assert.That(FinalBoss.HitDamage(10f, true, false, cfg), Is.EqualTo(10f * cfg.weakPointMultiplier).Within(1e-4f), "point faible : critique");
                Assert.That(FinalBoss.HitDamage(10f, false, false, cfg), Is.EqualTo(10f * cfg.bodyDamageShare).Within(1e-4f), "corps : dégâts de l'arme");
                Assert.That(cfg.bodyDamageShare, Is.GreaterThan(0f), "chaque arme le blesse");
                Assert.That(FinalBoss.HitDamage(10f, false, true, cfg), Is.EqualTo(10f * cfg.bodyThrowShare).Within(1e-4f), "objet lancé sur le corps");
                Assert.That(FinalBoss.HitDamage(10f, true, true, cfg), Is.EqualTo(10f * cfg.weakPointMultiplier).Within(1e-4f), "objet lancé sur un point faible");
                Assert.That(FinalBoss.HitDamage(-3f, true, false, cfg), Is.EqualTo(0f));
                Assert.That(FinalBoss.IsImpact(new DamageEvent { SourceMass = 20f }), Is.True);
                Assert.That(FinalBoss.IsImpact(new DamageEvent { SourceMass = 20f, Melee = true }), Is.False);
                Assert.That(FinalBoss.IsImpact(new DamageEvent { Bullet = true }), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }

        [Test]
        public void Boss_Relais_RecoitLesCoups_DuTraqueurIntouchable()
        {
            var boss = new GameObject("Traqueur factice");
            var root = boss.AddComponent<Damageable>();
            var so = new SerializedObject(root);
            so.FindProperty("immuneToOrbs").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.transform.SetParent(boss.transform, false);
            var proxyGo = new GameObject("Corps du boss (cible)");
            proxyGo.transform.SetParent(boss.transform, false);
            var proxy = proxyGo.AddComponent<Damageable>();
            proxy.Unkillable = true;
            try
            {
                Collider c = arm.GetComponent<Collider>();
                Assert.That(Damageable.Of(c), Is.EqualTo(root), "hors du combat : le traqueur");
                root.HitProxy = proxy;
                Assert.That(Damageable.Of(c), Is.EqualTo(proxy), "pendant le combat : le relais");
                Assert.That(root.ApplyDamage(50f, 1f, out _), Is.EqualTo(0f), "le traqueur reste intouchable");
                Assert.That(proxy.ApplyDamage(500f, 1f, out bool killed), Is.EqualTo(500f), "le relais compte tout");
                Assert.That(killed || proxy.Dead, Is.False, "le relais ne meurt jamais");
                Assert.That(Damageable.Of(null), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(boss);
            }
        }

        [Test]
        public void Boss_Charge_BorneeAuCouloir_EtALArene()
        {
            Vector3 from = Vector3.zero, dir = Vector3.forward;
            Assert.That(FinalBoss.ClampToLane(new Vector3(3f, 5f, 25f), from, dir, 20f, 0f), Is.EqualTo(new Vector3(0f, 5f, 20f)), "jamais au-delà du bout");
            Assert.That(FinalBoss.ClampToLane(new Vector3(2f, 1f, -4f), from, dir, 20f, 0f), Is.EqualTo(new Vector3(0f, 1f, 0f)), "jamais derrière le départ");
            Assert.That(FinalBoss.ClampToLane(new Vector3(3f, 0f, 10f), from, dir, 20f, 1f), Is.EqualTo(new Vector3(1f, 0f, 10f)), "jamais hors de la largeur");
            Assert.That(FinalBoss.ClampToLane(new Vector3(-0.5f, 0f, 7f), from, dir, 20f, 1f), Is.EqualTo(new Vector3(-0.5f, 0f, 7f)), "dans le couloir : inchangé");

            Vector3 c = new Vector3(0f, 2f, 0f);
            Assert.That(FinalBoss.ChargeLength(Vector3.zero, Vector3.right, c, 30f, 40f), Is.EqualTo(30f).Within(1e-4f), "coupée au bord");
            Assert.That(FinalBoss.ChargeLength(new Vector3(20f, 0f, 0f), Vector3.right, c, 30f, 40f), Is.EqualTo(10f).Within(1e-4f));
            Assert.That(FinalBoss.ChargeLength(new Vector3(20f, 0f, 0f), Vector3.left, c, 30f, 40f), Is.EqualTo(40f).Within(1e-4f), "au plus chargeDistance");
            Assert.That(FinalBoss.ChargeLength(Vector3.zero, Vector3.right, c, 30f, 25f), Is.EqualTo(25f).Within(1e-4f));
            Assert.That(FinalBoss.ChargeLength(new Vector3(40f, 0f, 0f), Vector3.right, c, 30f, 40f), Is.EqualTo(0f), "déjà dehors");
            Assert.That(FinalBoss.ChargeLength(Vector3.zero, Vector3.up, c, 30f, 40f), Is.EqualTo(0f), "direction verticale : nulle à plat");
        }
    }
}
