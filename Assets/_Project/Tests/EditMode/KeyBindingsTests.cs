using System.IO;
using System.Linq;
using ItHunts.Hud;
using ItHunts.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Tests
{
    /// <summary>Menu des touches (A5) : lignes, sauvegarde JSON par joueur, conflit et échange, réinitialisation, deux joueurs.</summary>
    public class KeyBindingsTests
    {
        private string _folder;
        private InputActionAsset _a, _b;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "ithunts_touches_" + System.Guid.NewGuid().ToString("N"));
            RebindStore.Folder = _folder;
            var src = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/PlayerControls.inputactions");
            _a = Object.Instantiate(src);
            _b = Object.Instantiate(src);
        }

        [TearDown]
        public void TearDown()
        {
            if (KeyBindingsPanel.Current != null)
                Object.DestroyImmediate(KeyBindingsPanel.Current.gameObject);
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
            RebindStore.Folder = null;
            if (Directory.Exists(_folder))
                Directory.Delete(_folder, true);
        }

        private static RebindStore.Row Row(InputActionAsset a, string group, string label) =>
            RebindStore.Rows(a, group).First(r => r.Label == label);

        [Test]
        public void Lignes_ToutesLesActions_ParOnglet()
        {
            var kb = RebindStore.Rows(_a, RebindStore.Keyboard);
            var pad = RebindStore.Rows(_a, RebindStore.Gamepad);
            foreach (string l in new[] { "Sauter", "Glisser", "Dash", "Attirer (traction)", "Repousser", "Attraper / relâcher", "Lancer / artefact",
                         "Recharger", "Interagir", "Parer", "Carte", "Pause", "Avancer", "Reculer", "Aller à gauche", "Aller à droite" })
                Assert.That(kb.Any(r => r.Label == l), Is.True, "clavier : " + l);
            Assert.That(pad.Any(r => r.Label == "Parer" && r.Path == "<Gamepad>/rightStickPress"), Is.True, "R3 (D-079)");
            Assert.That(kb.First(r => r.Label == "Parer").Path, Is.EqualTo("<Keyboard>/leftAlt"));
            Assert.That(kb.Count(r => r.Label == "Avancer"), Is.EqualTo(1), "un seul composite de déplacement (pas les flèches)");
            Assert.That(pad.Any(r => r.Label.StartsWith("Avancer")), Is.False, "le stick ne se change pas");
        }

        [Test]
        public void Sauvegarde_Rechargement_AuDemarrage()
        {
            RebindStore.Set(Row(_a, RebindStore.Keyboard, "Sauter"), "<Keyboard>/j");
            RebindStore.Save(_a, 0);
            Assert.That(File.Exists(RebindStore.FilePath(0)), Is.True, "touches_joueur1.json");
            Assert.That(RebindStore.Load(_b, 0), Is.True);
            Assert.That(Row(_b, RebindStore.Keyboard, "Sauter").Path, Is.EqualTo("<Keyboard>/j"));
            Assert.That(RebindStore.Prompt(_b.FindAction("Player/Jump"), false), Is.EqualTo("J"), "invite à jour");
        }

        [Test]
        public void Conflit_PropositionDEchange_OuiEtNon()
        {
            var panel = KeyBindingsPanel.Open(_a, 0, false, null, null, null);
            RebindStore.Row jump = panel.Rows.First(r => r.Label == "Sauter");
            panel.ApplyCaptured(jump, "<Keyboard>/leftShift");                 // la touche du dash
            Assert.That(panel.Mode, Is.EqualTo(KeyBindingsPanel.State.Conflit));
            Assert.That(panel.Message, Does.Contain("Dash"));
            panel.AnswerConflict(true);
            Assert.That(Row(_a, RebindStore.Keyboard, "Sauter").Path, Is.EqualTo("<Keyboard>/leftShift"));
            Assert.That(Row(_a, RebindStore.Keyboard, "Dash").Path, Is.EqualTo("<Keyboard>/space"), "échangées");

            RebindStore.Row grab = panel.Rows.First(r => r.Label == "Attraper / relâcher");
            panel.ApplyCaptured(grab, "<Keyboard>/leftShift");
            panel.AnswerConflict(false);
            Assert.That(Row(_a, RebindStore.Keyboard, "Attraper / relâcher").Path, Is.EqualTo("<Keyboard>/f"), "non : l'ancienne touche revient");
            Assert.That(panel.Mode, Is.EqualTo(KeyBindingsPanel.State.Liste));
        }

        [Test]
        public void Reinitialiser_UneAction_Et_Tout()
        {
            var panel = KeyBindingsPanel.Open(_a, 0, false, null, null, null);
            RebindStore.Set(Row(_a, RebindStore.Keyboard, "Sauter"), "<Keyboard>/j");
            RebindStore.Set(Row(_a, RebindStore.Keyboard, "Carte"), "<Keyboard>/m");
            panel.Refresh();
            while (panel.Rows[panel.Focus].Label != "Sauter")
                panel.MoveFocus(1);
            panel.ResetFocused();
            Assert.That(Row(_a, RebindStore.Keyboard, "Sauter").Path, Is.EqualTo("<Keyboard>/space"));
            Assert.That(Row(_a, RebindStore.Keyboard, "Carte").Path, Is.EqualTo("<Keyboard>/m"));
            panel.ResetAll();
            Assert.That(Row(_a, RebindStore.Keyboard, "Carte").Path, Is.EqualTo("<Keyboard>/tab"));
            Assert.That(RebindStore.Load(_b, 0), Is.True);
            Assert.That(Row(_b, RebindStore.Keyboard, "Carte").Path, Is.EqualTo("<Keyboard>/tab"), "sauvegardé");
        }

        [Test]
        public void DeuxJoueurs_ChacunSesTouches()
        {
            RebindStore.Set(Row(_a, RebindStore.Gamepad, "Sauter"), "<Gamepad>/buttonNorth");
            RebindStore.Save(_a, 0);
            RebindStore.Set(Row(_b, RebindStore.Gamepad, "Sauter"), "<Gamepad>/rightShoulder");
            RebindStore.Save(_b, 1);
            var p1 = Object.Instantiate(_a);
            var p2 = Object.Instantiate(_a);
            try
            {
                RebindStore.Load(p1, 0);
                RebindStore.Load(p2, 1);
                Assert.That(Row(p1, RebindStore.Gamepad, "Sauter").Path, Is.EqualTo("<Gamepad>/buttonNorth"), "joueur 1");
                Assert.That(Row(p2, RebindStore.Gamepad, "Sauter").Path, Is.EqualTo("<Gamepad>/rightShoulder"), "joueur 2");
                Assert.That(RebindStore.Load(p1, 2), Is.False, "joueur 3 : touches par défaut");
                Assert.That(Row(p1, RebindStore.Gamepad, "Sauter").Path, Is.EqualTo("<Gamepad>/buttonSouth"));
            }
            finally
            {
                Object.DestroyImmediate(p1);
                Object.DestroyImmediate(p2);
            }
        }
    }
}
