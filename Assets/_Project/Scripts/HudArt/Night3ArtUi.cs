using System.Linq;
using ItHunts.Art.UIJeu;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.HudArt
{
    /// <summary>
    /// Interface de la DA livrée par la PR #42 (art-ui-3), branchée sur le jeu (nuit 3) : écran de fin de run (Panneau_FinDeRun,
    /// statistiques, Recommencer, Campement), menu d'interaction des portes payantes (Panneau_Porte, prix en âmes), barre de vie du boss
    /// (HUD_BarreVieBoss, crans de phase), marqueur de critique (HUD_Critique) avec son et effet de la DA. Les prefabs viennent du skin
    /// du HUD ; sans eux, les vues provisoires de main restent.
    /// </summary>
    public static class Night3ArtUi
    {
        private static HudSkinSettings Skin()
        {
            foreach (PlayerHud h in Object.FindObjectsByType<PlayerHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (h.Skin != null)
                    return h.Skin;
            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            RunEndScreen.Created -= OnEnd;
            RunEndScreen.Created += OnEnd;
            FinalBoss.FightStarted -= OnBoss;
            FinalBoss.FightStarted += OnBoss;
        }

        private static Canvas Overlay(string name, int order)
        {
            Canvas c = UiKit.NewCanvas(name, order);
            return c;
        }

        // ---------- Fin de run ----------

        private static void OnEnd(RunEndScreen end)
        {
            HudSkinSettings skin = Skin();
            if (end == null || skin == null || skin.endOfRunPrefab == null)
                return;
            Canvas c = Overlay("Fin de run (DA)", 95);
            c.transform.SetParent(end.transform, false);
            GameObject go = Object.Instantiate(skin.endOfRunPrefab, c.transform, false);
            var view = go.GetComponent<FinDeRun>();
            if (view == null)
                return;
            var stats = end.Lines.Select(l => new FinDeRun.Stat { libelle = l.label, valeur = l.value }).ToArray();
            // Nuit 4 (PR #49) : la grille d'illustrations de la DA montre les améliorations ; la ligne texte disparaît des statistiques.
            stats = stats.Where(x => x.libelle == null || !x.libelle.StartsWith("Améliorations")).ToArray();
            FitRows(view, stats.Length);
            view.Remplit(end.Victory, end.Headline, stats);
            FillUpgrades(view);
            view.Recommencer += end.Replay;
            view.Campement += end.ToCamp;
            end.Skinned = true;
            end.gameObject.AddComponent<EndKeys>().End = end;
            foreach (MonoBehaviour m in end.GetComponents<MonoBehaviour>())
                if (m.GetType().Name == "RunEndScreenArt")
                    Object.Destroy(m);                                       // la vue provisoire laisse la place
        }

        /// <summary>Améliorations du premier joueur (codes de AMELIORATIONS.md et rangs), le passif en premier.</summary>
        private static void FillUpgrades(FinDeRun view)
        {
            var run = ItHunts.Artifacts.RunState.Current;
            ItHunts.Player.PlayerHealth p = ItHunts.Player.PlayerHealth.All.FirstOrDefault(x => x != null);
            if (run == null || p == null)
            {
                view.RemplitAmeliorations(new string[0], new int[0]);
                return;
            }

            var owned = run.UpgradesOf(p.PlayerId).Where(u => u != null).OrderByDescending(u => u.isPassive).ToList();
            view.RemplitAmeliorations(owned.Select(u => u.code).ToArray(), owned.Select(u => Mathf.Max(0, run.RankOf(p.PlayerId, u))).ToArray());
        }

        /// <summary>
        /// Toutes les statistiques dans le panneau de la DA : lignes de la maquette reproduites (même style), pas resserré pour tenir dans la
        /// hauteur des 7 lignes d'origine, police réduite d'autant.
        /// </summary>
        private static void FitRows(FinDeRun view, int n)
        {
            int have = view.libelles.Length;
            if (n <= have || have < 2)
                return;
            var lab = new System.Collections.Generic.List<UnityEngine.UI.Text>(view.libelles);
            var val = new System.Collections.Generic.List<UnityEngine.UI.Text>(view.valeurs);
            var r0 = (RectTransform)lab[0].transform;
            float y0 = r0.anchoredPosition.y, step0 = ((RectTransform)lab[1].transform).anchoredPosition.y - y0;
            float span = step0 * (have - 1) - 70f;                      // un peu plus bas que la maquette (la place sous la 7e ligne)
            float step = span / (n - 1);
            for (int i = have; i < n; i++)
            {
                lab.Add(Object.Instantiate(lab[have - 1], lab[have - 1].transform.parent, false));
                val.Add(Object.Instantiate(val[have - 1], val[have - 1].transform.parent, false));
            }

            float k = Mathf.Clamp(step / step0, 0.7f, 1f);
            for (int i = 0; i < n; i++)
            {
                foreach (UnityEngine.UI.Text t in new[] { lab[i], val[i] })
                {
                    var rt = (RectTransform)t.transform;
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, y0 + step * i);
                    t.resizeTextForBestFit = false;
                    t.fontSize = Mathf.Max(18, Mathf.RoundToInt(Mathf.Max(lab[0].fontSize, 24) * k));
                }
            }

            view.libelles = lab.ToArray();
            view.valeurs = val.ToArray();
        }

        /// <summary>Clavier et manette sur l'écran de fin : Entrée / A au campement, R / Y nouvelle run.</summary>
        private sealed class EndKeys : MonoBehaviour
        {
            public RunEndScreen End;

            private void Update()
            {
                var k = UnityEngine.InputSystem.Keyboard.current;
                var g = UnityEngine.InputSystem.Gamepad.current;
                if ((k != null && k.rKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame))
                    End.Replay();
                else if ((k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) || (g != null && g.buttonSouth.wasPressedThisFrame))
                    End.ToCamp();
            }
        }

        // ---------- Boss ----------

        private static void OnBoss(FinalBoss boss)
        {
            HudSkinSettings skin = Skin();
            if (boss == null || skin == null || skin.bossBarPrefab == null)
                return;
            Canvas c = Overlay("Barre du boss (DA)", 60);
            GameObject go = Object.Instantiate(skin.bossBarPrefab, c.transform, false);
            var bar = go.GetComponent<BarreVieBoss>();
            if (bar == null)
                return;
            FinalBoss.ArtBar = true;
            bar.Montre("Le Traqueur");
            c.gameObject.AddComponent<BossBarSync>().Init(boss, bar);
        }

        private sealed class BossBarSync : MonoBehaviour
        {
            private FinalBoss _boss;
            private BarreVieBoss _bar;
            private int _phase = 1;

            public void Init(FinalBoss b, BarreVieBoss bar)
            {
                _boss = b;
                _bar = bar;
            }

            private void Update()
            {
                if (_boss == null)
                {
                    Destroy(gameObject);
                    return;
                }

                _bar.Regle(_boss.MaxHealth > 0f ? _boss.Health / _boss.MaxHealth : 0f);
                if (_boss.Phase != _phase)
                {
                    _phase = _boss.Phase;
                    _bar.Phase(_phase);
                }

                if (_boss.Dead)
                    _bar.Cache();
            }
        }
    }
}
