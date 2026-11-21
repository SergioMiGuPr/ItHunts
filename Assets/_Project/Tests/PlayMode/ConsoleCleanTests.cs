using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ItHunts.Level;
using ItHunts.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ItHunts.Tests
{
    /// <summary>
    /// Playtest du 2026-11-21 : « Collection was modified; enumeration operation may not execute » en boucle dans la console de
    /// développement (PauseMenuArt parcourait les menus ouverts pendant qu'un clic en fermait un). 30 s de menu principal (souris qui
    /// survole), puis 30 s de niveau aléatoire où le menu pause s'ouvre et se referme à la souris (Reprendre, Paramètres, Retour) :
    /// aucune exception dans le journal.
    /// </summary>
    public class ConsoleCleanTests
    {
        private const string MenuScene = "Assets/Scenes/MainMenu.unity";
        private const string LevelScene = "Assets/Scenes/Level_01_Aleatoire.unity";
        private const float Seconds = 30f;

        private readonly List<string> _errors = new List<string>();
        private Mouse _mouse;

        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Exception || message.Contains("Collection was modified"))
                _errors.Add(message + "\n" + stack);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator AucuneException_30sDeMenu_Puis_30sDeNiveau()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur (scènes chargées par leur chemin).");
            yield break;
#else
            _errors.Clear();
            Application.logMessageReceived += OnLog;
            LogAssert.ignoreFailingMessages = true;                      // le test fait son propre bilan, plus lisible
            _mouse = InputSystem.AddDevice<Mouse>("Souris du test");
            _mouse.MakeCurrent();
            PlayerHealth.DebugInvulnerable = true;
            var behavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            // Les clics simulés vont au jeu même si la vue Game n'a pas le focus de la fenêtre.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            try
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(MenuScene, new LoadSceneParameters(LoadSceneMode.Single));
                float end = Time.realtimeSinceStartup + Seconds;
                int step = 0;
                while (Time.realtimeSinceStartup < end)
                {
                    // Survol (sans clic : un clic sur Jouer lancerait la partie) : le focus suit la souris.
                    step++;
                    Move(new Vector2(Screen.width * (0.2f + 0.6f * (step % 7) / 6f), Screen.height * (0.2f + 0.6f * (step % 5) / 4f)), false);
                    yield return new WaitForSecondsRealtime(0.25f);
                }

                Assert.IsEmpty(_errors, "menu principal :\n" + string.Join("\n---\n", _errors.Distinct().Take(5)));

                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(LevelScene, new LoadSceneParameters(LoadSceneMode.Single));
                float wait = Time.realtimeSinceStartup + 20f;
                while (PlayerInput.all.Count == 0 && Time.realtimeSinceStartup < wait)
                    yield return null;
                Assert.Greater(PlayerInput.all.Count, 0, "aucun joueur dans " + LevelScene);

                end = Time.realtimeSinceStartup + Seconds;
                int cycles = 0, closedByClick = 0;
                while (Time.realtimeSinceStartup < end)
                {
                    PlayerInput pi = PlayerInput.all.FirstOrDefault();
                    if (PauseMenu.Current != null && pi != null)
                    {
                        PauseMenu.Current.OpenFor(pi);
                        yield return Frames(4);
                        // Paramètres puis Retour, puis Reprendre : chaque clic ouvre ou ferme un écran pendant la vue de la DA.
                        yield return ClickButton("Bouton_1");
                        yield return Frames(4);
                        yield return ClickButton("Retour");
                        yield return Frames(4);
                        yield return ClickButton("Bouton_0");
                        yield return Frames(2);
                        if (!PauseMenu.Current.IsOpenFor(pi))
                            closedByClick++;
                        PauseMenu.Current.CloseFor(pi);
                        cycles++;
                    }

                    yield return new WaitForSecondsRealtime(0.5f);
                }

                Assert.Greater(cycles, 3, "le menu pause ne s'est pas ouvert");
                Assert.Greater(closedByClick, 0, "le clic sur Reprendre n'a jamais fermé le menu (clics simulés perdus)");
                Assert.IsEmpty(_errors, "niveau :\n" + string.Join("\n---\n", _errors.Distinct().Take(5)));
            }
            finally
            {
                Application.logMessageReceived -= OnLog;
                LogAssert.ignoreFailingMessages = false;
                PlayerHealth.DebugInvulnerable = false;
                InputSystem.settings.editorInputBehaviorInPlayMode = behavior;
                if (_mouse != null)
                    InputSystem.RemoveDevice(_mouse);
            }
#endif
        }

        private static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++)
                yield return null;
        }

        private void Move(Vector2 p, bool down)
        {
            var state = new MouseState { position = p, delta = new Vector2(3f, 3f) }.WithButton(MouseButton.Left, down);
            InputSystem.QueueStateEvent(_mouse, state);
        }

        /// <summary>Clic souris au centre du bouton actif de ce nom (vue de la DA, ou rien s'il n'y est pas).</summary>
        private IEnumerator ClickButton(string name)
        {
            RectTransform r = Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == name && t.gameObject.activeInHierarchy);
            if (r == null)
                yield break;
            Vector2 p = RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
            Move(p, false);
            yield return null;
            Move(p, true);
            yield return null;
            Move(p, false);
            yield return null;
        }
    }
}
