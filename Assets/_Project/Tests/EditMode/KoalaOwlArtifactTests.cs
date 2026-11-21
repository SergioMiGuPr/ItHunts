using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Épée en os du koala et livre à lien du hibou (D-077) : logique pure.</summary>
    public class KoalaOwlArtifactTests
    {
        private const float Dt = 1f / 60f;
        private BoneSwordSettings _sword;
        private SpiritBookSettings _book;

        [SetUp]
        public void SetUp()
        {
            _sword = ScriptableObject.CreateInstance<BoneSwordSettings>();
            _book = ScriptableObject.CreateInstance<SpiritBookSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sword);
            Object.DestroyImmediate(_book);
        }

        [Test]
        public void Assets_KoalaEtHibou_Selectionnables_AvecDescription()
        {
            var koala = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Settings/Personnages/Character_koala.asset");
            var hibou = AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/_Project/Settings/Personnages/Character_hibou.asset");
            Assert.That(koala.Selectable && hibou.Selectable, Is.True, "Valider n'est plus grisé");
            Assert.That(koala.artifact, Is.InstanceOf<BoneSwordSettings>());
            Assert.That(hibou.artifact, Is.InstanceOf<SpiritBookSettings>());
            Assert.That(koala.artifact.description, Is.EqualTo("Combo de 3 coups ; frappe plus fort en pleine vitesse."));
            Assert.That(hibou.artifact.description, Is.EqualTo("Lien qui draine un ennemi ou soigne un allié."));
            Assert.That(koala.artifact.viewModel, Is.Not.Null, "épée de la DA");
            Assert.That(hibou.artifact.viewModel, Is.Not.Null, "livre de la DA");
        }

        private int _tick = 100;

        private SwordStep Swing(ref SwordState s, bool lunge = false, bool holding = false)
        {
            int press = ++_tick;
            return BoneSword.Step(ref s, _tick, press, holding, holding, lunge, _sword, Dt);
        }

        private void Wait(float seconds) => _tick += Mathf.RoundToInt(seconds / Dt);

        [Test]
        public void Epee_ComboDeTroisCoups_PuisRetourAuPremier()
        {
            SwordState s = SwordState.Create();
            Assert.That(Swing(ref s).Index, Is.EqualTo(0));
            Wait(0.31f);
            Assert.That(Swing(ref s).Index, Is.EqualTo(1));
            Wait(0.31f);
            SwordStep third = Swing(ref s);
            Assert.That(third.Index, Is.EqualTo(2), "3e coup");
            Assert.That(BoneSword.Damage(2, false, 8.5f, _sword), Is.GreaterThan(BoneSword.Damage(0, false, 8.5f, _sword)), "le 3e plus fort");
            Wait(0.31f);
            Assert.That(Swing(ref s).Swung, Is.False, "pause après le 3e coup");
            Wait(0.3f);
            Assert.That(Swing(ref s).Index, Is.EqualTo(0), "nouveau combo");
            Wait(1.2f);
            Swing(ref s);
            Assert.That(s.LastIndex, Is.EqualTo(0), "trop longtemps sans frapper : le combo repart du 1er");
        }

        [Test]
        public void Epee_PasDeCoupEnTenantUnObjet()
        {
            SwordState s = SwordState.Create();
            Assert.That(Swing(ref s, holding: true).Swung, Is.False, "le clic est un lancer");
        }

        [Test]
        public void Epee_DegatsSelonLaVitesse_Plafonnes()
        {
            float slow = BoneSword.Damage(0, false, 5f, _sword);
            float run = BoneSword.Damage(0, false, 8.5f, _sword);
            float fast = BoneSword.Damage(0, false, 14f, _sword);
            float max = BoneSword.Damage(0, false, 20f, _sword);
            float over = BoneSword.Damage(0, false, 40f, _sword);
            Assert.That(slow, Is.EqualTo(run), "pas de malus sous la course");
            Assert.That(fast, Is.GreaterThan(run).And.LessThan(max));
            Assert.That(max, Is.EqualTo(_sword.comboDamage[0] * _sword.speedMaxMultiplier).Within(1e-3f));
            Assert.That(over, Is.EqualTo(max), "plafond");
        }

        [Test]
        public void Epee_FentePendantUnDash()
        {
            SwordState s = SwordState.Create();
            SwordStep st = Swing(ref s, lunge: true);
            Assert.That(st.Lunge, Is.True);
            Assert.That(BoneSword.Damage(st.Index, true, 8.5f, _sword), Is.EqualTo(_sword.comboDamage[2] * _sword.lungeDamageFactor).Within(1e-3f));
        }

        [Test]
        public void Epee_DegatsParSeconde_DuMemeOrdreQueLeBarillet()
        {
            // Un combo complet : 3 coups en 2 intervalles, puis l'intervalle et la pause avant le suivant.
            float combo = _sword.comboDamage[0] + _sword.comboDamage[1] + _sword.comboDamage[2];
            float cycle = 3f * _sword.swingInterval + _sword.comboRecovery;
            float dps = combo / cycle;
            var barrel = AssetDatabase.LoadAssetAtPath<BarrelSettings>("Assets/_Project/Settings/Barrel_Default.asset");
            float barrelDps = barrel.rounds * barrel.damage / (barrel.rounds * barrel.fireInterval + barrel.reloadTime);
            Assert.That(dps, Is.InRange(barrelDps * 0.7f, barrelDps * 1.4f), $"épée {dps:0.0} PV/s, barillet {barrelDps:0.0} PV/s");
        }

        [Test]
        public void Livre_DrainEtSoin_ReglagesParDefaut()
        {
            Assert.That(_book.range, Is.EqualTo(15f));
            Assert.That(_book.drainDps, Is.EqualTo(6f));
            Assert.That(_book.drainReturn, Is.EqualTo(0.5f));
            Assert.That(_book.maxDuration, Is.EqualTo(4f));
            Assert.That(_book.drainCooldown, Is.EqualTo(6f));
            Assert.That(_book.healDps, Is.EqualTo(8f));
            Assert.That(_book.healCooldown, Is.EqualTo(8f));
        }

        [Test]
        public void Livre_LienCasse_Relache_Portee_Vue_Duree()
        {
            BookState s = BookState.Create();
            SpiritBook.Start(ref s, BookLink.Drain, 10, 10);
            Assert.That(SpiritBook.Check(s, 11, true, true, 10f, true, _book, Dt), Is.EqualTo(BookEnd.Aucune));
            Assert.That(SpiritBook.Check(s, 11, false, true, 10f, true, _book, Dt), Is.EqualTo(BookEnd.Relache));
            Assert.That(SpiritBook.Check(s, 11, true, true, 15.5f, true, _book, Dt), Is.EqualTo(BookEnd.Portee));
            Assert.That(SpiritBook.Check(s, 11, true, true, 10f, false, _book, Dt), Is.EqualTo(BookEnd.Vue));
            Assert.That(SpiritBook.Check(s, 10 + 240, true, true, 10f, true, _book, Dt), Is.EqualTo(BookEnd.Duree), "4 s au plus");
            SpiritBook.End(ref s, 250, _book, Dt);
            Assert.That(SpiritBook.Ready(s, 250 + 300), Is.False, "recharge de 6 s");
            Assert.That(SpiritBook.Ready(s, 250 + 360), Is.True);
        }

        [Test]
        public void Livre_Soin_SansDepasserLeMaximum_EtRechargePlusLongue()
        {
            BookState s = BookState.Create();
            SpiritBook.Start(ref s, BookLink.Soin, 10, 10);
            Assert.That(SpiritBook.HealTick(s, 100f, _book, Dt), Is.EqualTo(8f * Dt).Within(1e-5f));
            Assert.That(SpiritBook.HealTick(s, 0.01f, _book, Dt), Is.EqualTo(0.01f).Within(1e-5f), "jamais au-delà du maximum");
            SpiritBook.End(ref s, 20, _book, Dt);
            Assert.That(SpiritBook.Ready(s, 20 + 420), Is.False, "recharge de 8 s");
            Assert.That(SpiritBook.Ready(s, 20 + 480), Is.True);
        }

        [Test]
        public void Livre_TraqueurInsensible()
        {
            var go = new GameObject("Traqueur factice");
            var d = go.AddComponent<Damageable>();
            var so = new SerializedObject(d);
            so.FindProperty("immuneToOrbs").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                Assert.That(SpiritBook.Targetable(d), Is.False, "aucun lien possible");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
