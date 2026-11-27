using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ItHunts.Art.UIJeu;
using ItHunts.Artifacts;
using ItHunts.Economy;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ItHunts.Tests
{
    /// <summary>
    /// Améliorations par personnage (playtest du 26/11 : au coffre, le caméléon se voyait offrir une amélioration de feu, sans illustration).
    /// Sur chaque carte et pour les 4 personnages : coffres (ouverture et 8 relances), marchand, colonne du HUD et écran de fin ne montrent
    /// que des améliorations communes ou du personnage joué, chacune avec son illustration. Le personnage de la carte est choisi comme au
    /// menu (RunSession.Characters, chemin réel du jeu), les trois autres sont pris en jeu (EquipCharacter, comme un joueur qui rejoint).
    /// Compte rendu dans docs/design/nuit-6/cartes/.
    /// </summary>
    public class UpgradeOffersTests
    {
        private static IEnumerable<TestCaseData> Maps() => ActTestMaps.Maps("Ameliorations");

        [UnityTest, Timeout(900000), TestCaseSource(nameof(Maps))]
        public IEnumerator SeulementDuPersonnage_AvecIllustration(int act, int seed)
        {
            LogAssert.ignoreFailingMessages = true;
            int first = (act * 2 + (seed == 11 ? 0 : 1)) % 4;
            yield return ActTestMaps.Load(act, seed, new[] { first, -1 });
            RunState run = RunState.Current;
            Assert.IsNotNull(run, "pas d'état de run");
            PlayerHealth p = PlayerHealth.All.FirstOrDefault(x => x != null);
            Assert.IsNotNull(p, "pas de joueur");
            PlayerHealth.DebugInvulnerable = true;
            CharacterRoster roster = run.Characters;
            Assert.IsNotNull(roster, "pas de liste de personnages");
            Assert.GreaterOrEqual(roster.Count, 4, "4 personnages attendus");
            var holder = p.GetComponent<ArtifactHolder>();
            var fails = new List<string>();
            var log = new StringBuilder();
            Chest[] chests = Object.FindObjectsByType<Chest>(FindObjectsSortMode.None).OrderBy(c => c.name).ToArray();
            Assert.Greater(chests.Length, 0, "aucun coffre sur la carte");
            ArtifactMerchant merchant = Object.FindFirstObjectByType<ArtifactMerchant>();

            for (int k = 0; k < 4; k++)
            {
                int ci = (first + k) % 4;
                CharacterDefinition def = roster[ci];
                if (k > 0)
                    holder.EquipCharacter(def);
                run.Slots(p.PlayerId).Clear();
                UpgradeCharacter c = RunState.CharacterOfPlayer(p);
                string who = def != null ? def.displayName : "?";
                if (c != UpgradeRules.CharacterOf(def))
                    fails.Add($"{who} : personnage lu {c}, attendu {UpgradeRules.CharacterOf(def)}{(k == 0 ? " (choix du menu)" : "")}");
                int offers = 0, cards = 0;

                void fail_(string w, string at, string m) => fails.Add($"{w}, {at} : {m}");

                void CheckMenu(string where)
                {
                    ShopMenu menu = ShopMenu.Of(p);
                    if (menu == null)
                    {
                        fails.Add($"{who}, {where} : pas de menu");
                        return;
                    }

                    PanneauOffres panel = Object.FindObjectsByType<PanneauOffres>(FindObjectsSortMode.None).FirstOrDefault(x => x.isActiveAndEnabled);
                    if (panel == null)
                        fails.Add($"{who}, {where} : menu sans le panneau de la DA (aucune illustration)");
                    for (int i = 0; i < menu.Offers.Count; i++)
                    {
                        OfferOrb o = menu.Offers[i];
                        if (o == null || o.Upgrade == null)
                            continue;                                    // soin du marchand
                        offers++;
                        UpgradeDefinition u = o.Upgrade;
                        if (!u.Generic && u.character != c)
                            fails.Add($"{who}, {where} : {u.code} « {u.displayName} » d'un autre personnage ({u.character})");
                        if (u.needsFire && !UpgradeRules.SetsFire(c))
                            fail_(who, where, $"{u.code} « {u.displayName} » (feu) sans source de feu");
                        if (u.icon == null)
                            fails.Add($"{who}, {where} : {u.code} sans illustration dans ses données");
                        if (panel == null || panel.cartes == null || i >= panel.cartes.Length || panel.cartes[i] == null)
                            continue;
                        IconeAmelioration icon = panel.cartes[i].avant;
                        bool shown = icon != null && icon.icone != null && icon.icone.enabled && icon.icone.sprite != null;
                        if (!shown)
                            fails.Add($"{who}, {where} : carte {i + 1} ({u.code}) sans illustration à l'écran");
                        else if (icon.Id != u.code)
                            fails.Add($"{who}, {where} : carte {i + 1} montre {icon.Id} au lieu de {u.code}");
                        else
                            cards++;
                        int owner = CatalogueAmeliorations.Proprietaire(u.code);
                        if (owner != 0 && owner != (int)c)
                            fails.Add($"{who}, {where} : carte {i + 1} ({u.code}) au cadre d'un autre personnage");
                    }
                }

                // Coffres : trois au plus (le rare de la salle fermée compris), ouverture puis 8 relances.
                int used = 0;
                foreach (Chest chest in chests.Where(x => x != null && x.isActiveAndEnabled).OrderByDescending(x => x.name.Contains("verrouillée")).Take(3))
                {
                    chest.ResetChest();
                    run.AddSouls(p.PlayerId, 100000);
                    if (!chest.TryOpen(p))
                    {
                        fails.Add($"{who} : {chest.name} ne s'ouvre pas");
                        continue;
                    }

                    used++;
                    yield return null;
                    yield return null;
                    CheckMenu(chest.name);
                    for (int r = 0; r < 8; r++)
                    {
                        ShopMenu open = ShopMenu.Of(p);
                        if (open == null || !open.Reroll())
                        {
                            fails.Add($"{who}, {chest.name} : relance {r + 1} refusée");
                            break;
                        }
                        yield return null;
                        yield return null;
                        CheckMenu($"{chest.name}, relance {r + 1}");
                    }

                    ShopMenu m = ShopMenu.Of(p);
                    if (m == null || m.Offers.Count == 0 || !m.Buy(0))
                        fails.Add($"{who}, {chest.name} : choix de la première carte refusé");   // une amélioration possédée : HUD et fin
                    ShopMenu.CloseAll();
                    yield return null;
                }

                // Marchand.
                if (merchant != null)
                {
                    merchant.ResetMerchant();
                    yield return null;
                    yield return null;
                    run.AddSouls(p.PlayerId, 100000);
                    merchant.Interact(p);
                    yield return null;
                    yield return null;
                    CheckMenu("marchand");
                    ShopMenu m = ShopMenu.Of(p);
                    if (m != null)
                        for (int i = 0; i < m.Offers.Count; i++)
                            if (m.Offers[i] != null && m.Offers[i].Upgrade != null && !m.Offers[i].IsRankUp)
                            {
                                m.Buy(i);
                                break;
                            }

                    ShopMenu.CloseAll();
                    yield return null;
                }
                else
                    fails.Add("pas de marchand sur la carte");

                // Colonne du HUD.
                yield return null;
                yield return null;
                int owned = run.UpgradesOf(p.PlayerId).Count, hud = 0;
                ColonneAmeliorations col = Object.FindObjectsByType<ColonneAmeliorations>(FindObjectsSortMode.None).FirstOrDefault(x => x.isActiveAndEnabled);
                if (col == null)
                    fails.Add($"{who} : pas de colonne d'améliorations au HUD");
                else
                {
                    if (col.Visibles != owned)
                        fails.Add($"{who} : HUD, {col.Visibles} cases pour {owned} améliorations");
                    for (int i = 0; i < col.Visibles && i < col.cases.Length; i++)
                    {
                        IconeAmelioration icon = col.cases[i];
                        if (icon == null || icon.icone == null || !icon.icone.enabled || icon.icone.sprite == null)
                            fails.Add($"{who} : HUD, case {i + 1} ({icon?.Id}) sans illustration");
                        else
                            hud++;
                        int owner = icon != null ? CatalogueAmeliorations.Proprietaire(icon.Id) : 0;
                        if (owner != 0 && owner != (int)c)
                            fails.Add($"{who} : HUD, case {i + 1} ({icon.Id}) d'un autre personnage");
                    }
                }

                // Écran de fin.
                int end = 0;
                ItHunts.Level.RunEndScreen screen = ItHunts.Level.RunEndScreen.Show(false, "Contrôle des améliorations");
                yield return null;
                yield return null;
                FinDeRun view = Object.FindObjectsByType<FinDeRun>(FindObjectsSortMode.None).FirstOrDefault();
                if (view == null)
                    fails.Add($"{who} : écran de fin sans le panneau de la DA");
                else
                {
                    var active = view.ameliorations.Where(a => a != null && a.gameObject.activeInHierarchy).ToList();
                    if (active.Count != owned)
                        fails.Add($"{who} : écran de fin, {active.Count} illustrations pour {owned} améliorations");
                    foreach (IconeAmelioration icon in active)
                    {
                        if (icon.icone == null || !icon.icone.enabled || icon.icone.sprite == null)
                            fails.Add($"{who} : écran de fin, {icon.Id} sans illustration");
                        else
                            end++;
                        int owner = CatalogueAmeliorations.Proprietaire(icon.Id);
                        if (owner != 0 && owner != (int)c)
                            fails.Add($"{who} : écran de fin, {icon.Id} d'un autre personnage");
                    }
                }

                if (screen != null)
                    Object.Destroy(screen.gameObject);
                foreach (FinDeRun v in Object.FindObjectsByType<FinDeRun>(FindObjectsSortMode.None))
                    Object.Destroy(v.transform.root.gameObject);
                Time.timeScale = 1f;
                yield return null;
                log.Append($"- {who} ({c}{(k == 0 ? ", choisi au menu" : ", pris en jeu")}) : {used} coffres, {offers} offres, {cards} cartes illustrées, " +
                           $"{owned} possédées, HUD {hud}, fin {end}\n");
            }

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "design", "nuit-6", "cartes"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, $"ameliorations_{ActTestMaps.Scene}_s{seed}.md"),
                $"# Améliorations par personnage : {ActTestMaps.Scene}, graine {seed} ({ActTestMaps.MapName})\n\n{log}\n" +
                $"Défauts : {(fails.Count == 0 ? "aucun" : "\n- " + string.Join("\n- ", fails.Distinct()))}\n");
            Assert.AreEqual(0, fails.Count, string.Join("\n", fails.Distinct().Take(30)));
        }

        [TearDown]
        public void TearDown()
        {
            ShopMenu.CloseAll();
            PlayerHealth.DebugInvulnerable = false;
            ActTestMaps.TearDown();
        }
    }
}
