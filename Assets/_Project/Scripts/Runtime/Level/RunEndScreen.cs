using System.Collections.Generic;
using System.Linq;
using ItHunts.Hud;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ItHunts.Level
{
    /// <summary>
    /// Écran de fin de run (nuit 3, point 3), sur une mort comme sur une victoire, inspiré des roguelites (Risk of Rain 2, Hades,
    /// Dead Cells) : temps survécu, acte atteint, morts par type, âmes gagnées et dépensées, améliorations, artefact, caisses lancées,
    /// headshots, parades, distance, vitesse maximale, cause de la mort. Puis retour au campement (ou nouvelle run tout de suite).
    /// Logique ici ; vue de la DA : RunEndScreenArt (assembly Menus), repli greybox sinon.
    /// </summary>
    public sealed class RunEndScreen : MonoBehaviour
    {
        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private string _title, _text;

        public static RunEndScreen Current { get; private set; }
        public static event System.Action<RunEndScreen> Created;
        public bool Victory { get; private set; }
        public string Title => _title;
        public string Summary => _text;
        public GreyboxMenu Menu => _menu;
        /// <summary>Habillé par la DA (pas de repli greybox).</summary>
        public bool Skinned { get; set; }
        /// <summary>Lignes du bilan (libellé, valeur), dans l'ordre d'affichage.</summary>
        public List<(string label, string value)> Lines { get; private set; } = new List<(string, string)>();
        /// <summary>Phrase d'accroche (cause de la mort ou victoire).</summary>
        public string Headline { get; private set; } = "";

        /// <summary>Affiche l'écran (une seule fois par scène).</summary>
        public static RunEndScreen Show(bool victory, string summary)
        {
            if (Current != null)
                return Current;
            var go = new GameObject(victory ? "Écran de fin (victoire)" : "Écran de fin (mort)");
            var s = go.AddComponent<RunEndScreen>();
            s.Victory = victory;
            s._title = victory ? "Victoire" : "Mort";
            s._text = summary;
            s.Fill();
            s._menu.Add("Retour au campement", () => s.ToCamp());
            s._menu.Add("Nouvelle run", () => s.Replay());
            s._menu.FocusFirst();
            Current = s;
            PlayerLook.MenuOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            foreach (PlayerInput pi in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
                pi.DeactivateInput();
            Created?.Invoke(s);
            return s;
        }

        public static string Clock(float seconds)
        {
            int s = Mathf.FloorToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }

        /// <summary>
        /// Lignes « Améliorations » de l'écran de fin (D-100, pur) : les noms avec leur rang, 3 par ligne au plus ; plusieurs lignes
        /// « Améliorations (1/3) » quand il y en a beaucoup, pour que la ligne ne déborde plus du cadre de la DA.
        /// </summary>
        public static List<(string label, string value)> UpgradeLines(IReadOnlyList<string> owned, int perLine = 3)
        {
            var r = new List<(string, string)>();
            if (owned == null || owned.Count == 0)
            {
                r.Add(("Améliorations", "aucune"));
                return r;
            }

            int chunks = Mathf.CeilToInt(owned.Count / (float)Mathf.Max(1, perLine));
            for (int c = 0; c < chunks; c++)
            {
                var part = new List<string>();
                for (int i = c * perLine; i < Mathf.Min(owned.Count, (c + 1) * perLine); i++)
                    part.Add(owned[i]);
                string label = chunks == 1 ? $"Améliorations ({owned.Count})" : $"Améliorations ({c + 1}/{chunks})";
                r.Add((label, string.Join(", ", part)));
            }

            return r;
        }

        private void Fill()
        {
            string act = RunSession.Active && RunSession.Definition != null && RunSession.Act < RunSession.Definition.levels.Count
                ? RunSession.Definition.levels[RunSession.Act].displayName : SceneManager.GetActiveScene().name;
            Headline = Victory ? "Le traqueur est tombé. La run est gagnée." : string.IsNullOrEmpty(RunStats.DeathCause)
                ? "Tu es mort." : $"Tué par : {RunStats.DeathCause.ToLowerInvariant()}";
            Lines.Clear();
            Lines.Add(("Temps survécu", Clock(RunStats.TimeSurvived)));
            Lines.Add(("Acte atteint", act));
            string kills = RunStats.Kills.Count == 0 ? "aucun" : string.Join(", ", RunStats.Kills.OrderByDescending(k => k.Value).Select(k => $"{k.Key} {k.Value}"));
            Lines.Add(("Ennemis tués", $"{RunStats.TotalKills}  ({kills})"));
            Lines.Add(("Âmes gagnées / dépensées", $"{RunStats.SoulsGained} / {RunStats.SoulsSpent}"));
            var run = ItHunts.Artifacts.RunState.Current;
            var owned = new List<string>();
            if (run != null)
                foreach (PlayerHealth p in PlayerHealth.All)
                    if (p != null)
                        foreach (var u in run.Slots(p.PlayerId).Sorted())
                            owned.Add($"{u.displayName} ({ItHunts.Settings.UpgradeDefinition.RankName(run.RankOf(p.PlayerId, u))})");
            foreach (var line in UpgradeLines(owned))
                Lines.Add(line);
            Lines.Add(("Artefact", string.IsNullOrEmpty(RunStats.Artifact) ? "—" : RunStats.Artifact));
            Lines.Add(("Caisses lancées", RunStats.CratesThrown.ToString()));
            Lines.Add(("Headshots", RunStats.Headshots.ToString()));
            Lines.Add(("Parades réussies", RunStats.Parries.ToString()));
            Lines.Add(("Distance parcourue", RunStats.Distance >= 1000f ? $"{RunStats.Distance / 1000f:0.0} km" : $"{RunStats.Distance:0} m"));
            Lines.Add(("Vitesse maximale", $"{RunStats.MaxSpeed:0.0} m/s ({RunStats.MaxSpeed * 3.6f:0} km/h)"));
            if (RunStats.ActTimes.Count > 0)
                Lines.Add(("Durée des actes", string.Join(" · ", RunStats.ActTimes.Select(a => $"{a.act} {Clock(a.seconds)}"))));
            Lines.Add(("Cause de la mort", Victory ? "—" : string.IsNullOrEmpty(RunStats.DeathCause) ? "inconnue" : RunStats.DeathCause));
        }

        private void Update()
        {
            if (!Skinned)
                _menu.Update();
        }

        private void OnGUI()
        {
            if (Skinned)
                return;
            string body = Headline + "\n" + string.Join("\n", Lines.Select(l => $"{l.label} : {l.value}"));
            _menu.Draw(_title, body);
        }

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
                PlayerLook.MenuOpen = false;
            }
        }

        /// <summary>Retour au campement (menu principal, page du campement) : on relance une run d'un clic.</summary>
        public void ToCamp()
        {
            string menu = RunSession.MenuScene;
            RunSession.End();
            MainMenu.StartAtCamp = true;
            if (!RunSession.Load(menu))
            {
                MainMenu.StartAtCamp = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        /// <summary>Nouvelle run tout de suite : même difficulté, depuis l'acte I (playtest : même acte de départ).</summary>
        public void Replay()
        {
            if (RunSession.Active)
            {
                bool admin = RunSession.AdminMode;
                int act = admin ? RunSession.StartAct : 0;
                string first = RunSession.BeginAt(RunSession.Definition, RunSession.DifficultyIndex, act);
                RunSession.AdminMode = admin;
                if (RunSession.Load(first))
                    return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
