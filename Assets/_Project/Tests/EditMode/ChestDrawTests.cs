using System.Collections.Generic;
using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Economy;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Coffres du point 3 : 3 choix, 25 % d'amélioration du personnage chacun, garantie d'au moins 4 par run (compteur de pitié dans
    /// l'état de run), emplacements pleins : montées de rang. Tirages purs (RunState.DrawChest), avec des améliorations de test.
    /// </summary>
    public class ChestDrawTests
    {
        private readonly List<Object> _made = new List<Object>();
        private List<UpgradeDefinition> _common, _mine;

        [SetUp]
        public void SetUp()
        {
            _common = new List<UpgradeDefinition>();
            _mine = new List<UpgradeDefinition>();
            for (int i = 0; i < 30; i++)
                _common.Add(Upgrade("G" + (i + 1), UpgradeCharacter.Aucun, false));
            for (int i = 0; i < 2; i++)
                _mine.Add(Upgrade("R-P" + (i + 1), UpgradeCharacter.Renard, true));
            for (int i = 0; i < 8; i++)
                _mine.Add(Upgrade("R" + (i + 1), UpgradeCharacter.Renard, false));
            // Celles d'un autre personnage ne sortent jamais pour le renard.
            _common.Add(Upgrade("K1", UpgradeCharacter.Koala, false));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
                if (o != null)
                    Object.DestroyImmediate(o);
            _made.Clear();
        }

        private UpgradeDefinition Upgrade(string code, UpgradeCharacter c, bool passive)
        {
            var u = ScriptableObject.CreateInstance<UpgradeDefinition>();
            u.code = code;
            u.displayName = code;
            u.character = c;
            u.isPassive = passive;
            _made.Add(u);
            return u;
        }

        private RunState Run(int seed)
        {
            var go = new GameObject("Run (test)");
            _made.Add(go);
            var run = go.AddComponent<RunState>();
            run.Configure(null, seed);
            return run;
        }

        private IEnumerable<UpgradeDefinition> Visible => _common.Concat(_mine);

        [Test]
        public void TroisChoixDifferents_UnSurQuatreDuPersonnage()
        {
            RunState run = Run(4242);
            int character = 0, total = 0;
            for (int n = 0; n < 600; n++)
            {
                var offers = run.DrawChest(7, Visible, UpgradeCharacter.Renard, 3, 0.25f, false, false);
                Assert.That(offers.Count, Is.EqualTo(3), "3 choix");
                Assert.That(offers.Select(o => o.u).Distinct().Count(), Is.EqualTo(3), "trois améliorations différentes");
                Assert.That(offers.All(o => o.from == -1 && o.to == 0), Is.True, "nouvelles, en Bronze");
                Assert.That(offers.All(o => o.u.Generic || o.u.character == UpgradeCharacter.Renard), Is.True, "jamais celles d'un autre personnage");
                character += offers.Count(o => !o.u.Generic);
                total += offers.Count;
            }

            float share = character / (float)total;
            Assert.That(share, Is.InRange(0.21f, 0.29f), "25 % d'améliorations du personnage (mesuré : " + share + ")");
        }

        [Test]
        public void MemeSeed_MemeTirage()
        {
            var a = Run(99).DrawChest(7, Visible, UpgradeCharacter.Renard, 3, 0.25f, false, false).Select(o => o.u.code).ToArray();
            var b = Run(99).DrawChest(7, Visible, UpgradeCharacter.Renard, 3, 0.25f, false, false).Select(o => o.u.code).ToArray();
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void Garantie_SeulementLesSiennes_SansSecondPassif()
        {
            RunState run = Run(5);
            var offers = run.DrawChest(7, Visible, UpgradeCharacter.Renard, 3, 0.25f, true, false);
            Assert.That(offers.Count, Is.EqualTo(3));
            Assert.That(offers.All(o => o.u.character == UpgradeCharacter.Renard), Is.True, "garantie : rien que des améliorations du renard");
            run.Grant(7, _mine[0]);                                          // un passif porté
            for (int n = 0; n < 50; n++)
                Assert.That(run.DrawChest(7, Visible, UpgradeCharacter.Renard, 3, 0.25f, true, false).Any(o => o.u.isPassive), Is.False,
                    "garantie : pas un passif qui en remplacerait un autre");
        }

        [Test]
        public void Pitie_ApresDeuxCoffresSansAmeliorationDuPersonnage()
        {
            RunState run = Run(11);
            const int id = 7;
            Assert.That(run.CharacterPityDue(id, 4, 2), Is.False, "premier coffre : tirage normal");
            run.NoteChestOpened(id);
            run.Grant(id, _common[0]);
            Assert.That(run.CharacterPityDue(id, 4, 2), Is.False);
            run.NoteChestOpened(id);
            run.Grant(id, _common[1]);
            Assert.That(run.DryChests(id), Is.EqualTo(2));
            Assert.That(run.CharacterPityDue(id, 4, 2), Is.True, "deux coffres d'affilée sans : le troisième est garanti");
            run.NoteChestOpened(id);
            run.Grant(id, _mine[3]);
            Assert.That(run.DryChests(id), Is.EqualTo(0), "une amélioration du personnage remet le compteur à zéro");
            Assert.That(run.CharacterUpgradesOwned(id), Is.EqualTo(1));
            Assert.That(run.CharacterPityDue(id, 4, 2), Is.False);
            Assert.That(run.CharacterPityDue(id, 1, 2), Is.False, "garantie atteinte : plus de pitié");
        }

        /// <summary>
        /// Run simulée de 12 coffres avec un joueur qui prend toujours une commune quand il le peut (le pire cas) : la garantie lui
        /// donne quand même au moins 4 améliorations de son personnage. Plusieurs seeds.
        /// </summary>
        [Test]
        public void RunSimulee_AuMoinsQuatreDuPersonnage([Values(1, 2, 3, 42, 4242, 90210)] int seed)
        {
            RunState run = Run(seed);
            const int id = 7;
            for (int chest = 0; chest < 12; chest++)
            {
                bool force = run.CharacterPityDue(id, 4, 2);
                run.NoteChestOpened(id);
                var offers = run.DrawChest(id, Visible, UpgradeCharacter.Renard, 3, 0.25f, force, chest >= 6);
                Assert.That(offers.Count, Is.GreaterThan(0));
                var pick = offers.FirstOrDefault(o => o.u.Generic);
                if (pick.u == null)
                    pick = offers[0];
                Assert.That(pick.from < 0 ? run.Grant(id, pick.u, pick.to).Ok : run.Grant(id, pick.u).Ok, Is.True);
            }

            Assert.That(run.CharacterUpgradesOwned(id), Is.GreaterThanOrEqualTo(4), "garantie : au moins 4 améliorations du personnage");
        }

        [Test]
        public void EmplacementsPleins_MonteesDeRangSeulement()
        {
            RunState run = Run(3);
            const int id = 7;
            UpgradeSlots slots = run.Slots(id);
            for (int i = 0; i < UpgradeSlots.MaxSlots; i++)
                slots.Set(_common[i], i % 5);                                 // dont deux Mythiques (rang 4)
            Assert.That(slots.Full, Is.True);
            for (int n = 0; n < 40; n++)
            {
                var offers = run.DrawChest(id, Visible, UpgradeCharacter.Renard, 3, 0.25f, n % 2 == 0, false);
                Assert.That(offers.Count, Is.EqualTo(3));
                Assert.That(offers.All(o => slots.Has(o.u) && o.from == slots.RankOf(o.u) && o.to == o.from + 1), Is.True, "montées de rang");
                Assert.That(offers.All(o => o.from < UpgradeDefinition.MaxRank), Is.True, "jamais au-delà de Mythique");
                Assert.That(offers.Select(o => o.u).Distinct().Count(), Is.EqualTo(3));
            }

            Assert.That(run.CharacterPityDue(id, 4, 2), Is.False, "pleins : la garantie ne joue plus");
        }

        [Test]
        public void MonteeDeRangPossible_LaDerniereCarte()
        {
            RunState run = Run(8);
            const int id = 7;
            run.Grant(id, _common[0]);
            var offers = run.DrawChest(id, Visible, UpgradeCharacter.Renard, 3, 0.25f, false, true);
            Assert.That(offers.Count, Is.EqualTo(3));
            Assert.That(offers.Take(2).All(o => o.from == -1), Is.True, "deux nouvelles");
            Assert.That(offers[2].u, Is.SameAs(_common[0]));
            Assert.That((offers[2].from, offers[2].to), Is.EqualTo((0, 1)), "Bronze → Argent");
        }
    }
}
