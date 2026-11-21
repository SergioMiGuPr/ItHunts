using System.Reflection;
using ItHunts.Level;
using ItHunts.Menus;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Tests
{
    /// <summary>
    /// Menus de la DA (PR #18, 2026-11-18) : les écrans sont les prefabs livrés, pilotés par la logique du jeu (MainMenu,
    /// CampLobby, GameSettings) ; textes lus dans les données ; feu du campement remonté au mixage.
    /// </summary>
    public class MenuArtTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private static MenuSkinSettings Skin => AssetDatabase.LoadAssetAtPath<MenuSkinSettings>("Assets/_Project/Settings/MenuSkin_Default.asset");

        [Test]
        public void Habillage_PrefabsEtAudioDeLaDA()
        {
            MenuSkinSettings s = Skin;
            Assert.That(s, Is.Not.Null);
            foreach ((GameObject go, string name) in new[] { (s.titleScreen, "Menu_Titre"), (s.campScreen, "Menu_Campement"), (s.choiceScreen, "Menu_Choix"),
                         (s.pauseScreen, "Menu_Pause"), (s.settingsScreen, "Menu_Parametres") })
            {
                Assert.That(go, Is.Not.Null, name);
                Assert.That(AssetDatabase.GetAssetPath(go), Is.EqualTo($"Assets/Art/UI/Menus/Prefabs/{name}.prefab"));
            }
            Assert.That(AssetDatabase.GetAssetPath(s.music), Does.EndWith("Musique_Menu_Boucle.wav"));
            Assert.That(AssetDatabase.GetAssetPath(s.fire), Does.EndWith("Sfx_Feu_Boucle.wav"));
            Assert.That(s.fireGainDb, Is.GreaterThanOrEqualTo(18f), "feu livré à −46 dB RMS : remonté au mixage");
        }

        [Test]
        public void Artefacts_DescriptionCourte()
        {
            var lantern = AssetDatabase.LoadAssetAtPath<ArtifactDefinition>("Assets/_Project/Settings/FireLantern_Default.asset");
            var barrel = AssetDatabase.LoadAssetAtPath<ArtifactDefinition>("Assets/_Project/Settings/Barrel_Default.asset");
            Assert.That(lantern.description, Does.Contain("flammes").And.Contain("cône").And.Contain("brûle"));
            Assert.That(barrel.description, Does.Contain("6 balles").And.Contain("chambre active"));
        }

        [Test]
        public void GainDuFeu_SansSaturer()
        {
            Assert.That(ItHunts.Audio.AudioGain.Linear(24f), Is.EqualTo(15.85f).Within(0.01f));
            float prev = 0f;
            for (float x = 0f; x <= 30f; x += 0.01f)
            {
                float y = ItHunts.Audio.AudioGain.SoftClip(x);
                Assert.That(y, Is.LessThanOrEqualTo(1f));
                Assert.That(y, Is.GreaterThanOrEqualTo(prev - 1e-6f), "monotone, sans saut");
                Assert.That(y - prev, Is.LessThan(0.011f), "continue");
                prev = y;
            }
            Assert.That(ItHunts.Audio.AudioGain.SoftClip(0.5f), Is.EqualTo(0.5f), "les sons faibles ne sont pas déformés");
            Assert.That(ItHunts.Audio.AudioGain.SoftClip(-3f), Is.LessThan(-0.9f).And.GreaterThanOrEqualTo(-1f));

            // Limiteur : un souffle faible (−46 dB) monte de +28 dB, un claquement fort reste sous le plafond.
            var data = new float[14400];
            for (int i = 0; i < data.Length; i++)
                data[i] = 0.005f * Mathf.Sin(i * 0.3f);
            data[100] = 0.5f;
            float coef = ItHunts.Audio.AudioGain.ReleaseCoef(0.06f, 48000);
            ItHunts.Audio.AudioGain.Process(data, ItHunts.Audio.AudioGain.Linear(28f), 0.85f, coef, 0f);
            float peak = 0f;
            foreach (float v in data)
                peak = Mathf.Max(peak, Mathf.Abs(v));
            Assert.That(peak, Is.LessThanOrEqualTo(0.851f), "claquement limité");
            float quiet = 0f;
            for (int i = 12000; i < 14400; i++)                     // 0.25 s après : la relâche est passée
                quiet = Mathf.Max(quiet, Mathf.Abs(data[i]));
            Assert.That(quiet, Is.EqualTo(0.005f * ItHunts.Audio.AudioGain.Linear(28f)).Within(0.01f), "après la relâche, le souffle a tout le gain");
        }

        [Test]
        public void Parametres_PasBornesEtTextes()
        {
            float music = GameSettings.MusicVolume, sens = GameSettings.Sensitivity;
            bool reduce = GameSettings.ReduceMotion;
            try
            {
                GameSettings.MusicVolume = 0.95f;
                GameSettings.Step(GameSettings.Line.Musique, 1);
                GameSettings.Step(GameSettings.Line.Musique, 1);
                Assert.That(GameSettings.MusicVolume, Is.EqualTo(1f), "pas de retour à 0 % après 100 %");
                Assert.That(GameSettings.Value(GameSettings.Line.Musique), Is.EqualTo("100 %"));
                GameSettings.Sensitivity = 0.25f;
                GameSettings.Step(GameSettings.Line.Sensibilite, -1);
                Assert.That(GameSettings.Sensitivity, Is.EqualTo(0.25f));
                GameSettings.ReduceMotion = false;
                GameSettings.Step(GameSettings.Line.ReduireAnimations, 1);
                Assert.That(GameSettings.Value(GameSettings.Line.ReduireAnimations), Is.EqualTo("oui"));
                Assert.That(GameSettings.Labels, Is.EqualTo(new[] { "Volume général", "Musique", "Effets", "Sensibilité", "Plein écran", "Réduire les animations" }));
            }
            finally
            {
                GameSettings.MusicVolume = music;
                GameSettings.Sensitivity = sens;
                GameSettings.ReduceMotion = reduce;
            }
        }

        /// <summary>Menu principal construit en mode édition, avec sa vue de la DA.</summary>
        private static (MainMenu menu, MainMenuArt view, GameObject go) NewMenu()
        {
            var go = new GameObject("Menu (test)");
            var menu = go.AddComponent<MainMenu>();
            typeof(MainMenu).GetField("run", Private).SetValue(menu, AssetDatabase.LoadAssetAtPath<RunDefinition>("Assets/_Project/Settings/Run_Default.asset"));
            typeof(MainMenu).GetField("skin", Private).SetValue(menu, Skin);
            menu.Build();
            MainMenuArt view = MenuArtInstall.Attach(menu);
            Assert.That(view, Is.Not.Null, "vue de la DA posée");
            return (menu, view, go);
        }

        private static void ShowCamp(MainMenu menu)
        {
            typeof(MainMenu).GetField("_showCamp", Private).SetValue(menu, true);
            typeof(MainMenu).GetField("_page", Private).SetValue(menu, MainMenu.Page.Campement);
        }

        [Test]
        public void Campement_PlacesSuiventLeSalon()
        {
            (MainMenu menu, MainMenuArt view, GameObject go) = NewMenu();
            try
            {
                Assert.That(menu.Skinned, Is.True, "plus de repli greybox");
                view.Sync();
                Assert.That(view.TitleScreen.activeSelf, Is.True);
                Assert.That(view.CampScreen.activeSelf, Is.False, "un seul écran actif");
                ShowCamp(menu);
                view.Sync();
                GameObject camp = view.CampScreen;
                Assert.That(camp.activeSelf && !view.TitleScreen.activeSelf, Is.True);
                Assert.That(camp.transform.Find("Place_1/Corps").gameObject.activeSelf, Is.True, "J1 assis");
                Assert.That(camp.transform.Find("Place_2/Corps").gameObject.activeSelf, Is.False, "place 2 vide en solo");
                Assert.That(camp.transform.Find("Interface/Plus_3").gameObject.activeSelf && camp.transform.Find("Lanterne_Eteinte_3").gameObject.activeSelf, Is.True,
                    "place 3 : bouton + et lanterne éteinte");
                Assert.That(camp.transform.Find("Interface/Plus_4").gameObject.activeSelf && camp.transform.Find("Lanterne_Eteinte_4").gameObject.activeSelf, Is.True);
                Assert.That(camp.transform.Find("Interface/Chat").gameObject.activeSelf, Is.False, "chat masqué tant que le jeu en ligne n'existe pas");
                Assert.That(camp.transform.Find("Interface/Cartouche_1/Nom").GetComponent<Text>().text, Is.EqualTo("Joueur 1"));
                Assert.That(menu.CampReady.Label, Is.EqualTo("Jouer"), "solo : Prêt lance la partie");
                Assert.That(menu.CampMenu.Focus, Is.EqualTo(menu.CampMenu.Items.IndexOf(menu.CampReady)), "le focus part sur Prêt");

                // Un 2e joueur rejoint (place 2), prend le même personnage : Prêt grisé ; il change, se met prêt : coche.
                Assert.That(menu.Lobby.Join("manette 2"), Is.EqualTo(1));
                menu.Lobby[1].Character = menu.Lobby[0].Character;
                view.Sync();
                Assert.That(camp.transform.Find("Place_2/Corps").gameObject.activeSelf, Is.True, "J2 assis à la place 2");
                Assert.That(menu.ReadyBlocked, Is.True, "doublon : Prêt grisé");
                Assert.That(camp.transform.Find("Interface/Pret").GetComponent<Image>().canvasRenderer.GetColor(), Is.EqualTo(MenuArtKit.Disabled));
                menu.Lobby[1].Character = 1;
                menu.Lobby[1].Ready = true;
                view.Sync();
                Assert.That(menu.ReadyBlocked, Is.False);
                Assert.That(camp.transform.Find("Interface/Cartouche_2/Coche").gameObject.activeSelf, Is.True, "J2 prêt : coche");
                Assert.That(camp.transform.Find("Interface/Cartouche_1/Coche").gameObject.activeSelf, Is.False);

                menu.ChangeAct(1);
                view.Sync();
                Assert.That(camp.transform.Find("Interface/Selecteur_Acte/Acte").GetComponent<Text>().text, Does.Contain("ACTE II"));
                Assert.That(menu.ReadyBlocked, Is.True, "acte pas encore jouable");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Choix_TextesLusDansLesDonnees()
        {
            (MainMenu menu, MainMenuArt view, GameObject go) = NewMenu();
            try
            {
                ShowCamp(menu);
                menu.OpenSelect(0);
                CharacterRoster roster = menu.Roster;
                for (int i = 0; i < roster.Count; i++)
                {
                    menu.Pick.SetFocus(i);
                    view.Sync();
                    CharacterDefinition c = roster[i];
                    GameObject choice = view.ChoiceScreen;
                    Assert.That(choice.activeSelf, Is.True);
                    Assert.That(choice.transform.Find("Nom").GetComponent<Text>().text, Is.EqualTo(c.displayName));
                    string desc = choice.transform.Find("Description").GetComponent<Text>().text;
                    string id = char.ToUpperInvariant(c.id[0]) + c.id.Substring(1);
                    Assert.That(choice.transform.Find($"Vignettes/Vignette_{id}_Illuminee").gameObject.activeSelf, Is.True, "vignette sélectionnée illuminée");
                    Assert.That(choice.transform.Find($"Grands/Grand_{id}").gameObject.activeSelf, Is.True, "grand personnage");
                    if (c.artifact != null)
                    {
                        Assert.That(choice.transform.Find("NomArtefact").GetComponent<Text>().text, Is.EqualTo(c.artifact.displayName));
                        Assert.That(desc, Is.EqualTo(c.artifact.description));
                    }
                    else
                        Assert.That(desc, Is.EqualTo("à venir"), c.id);
                }

                menu.Pick.Close();
                view.Sync();
                Assert.That(view.ChoiceScreen.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Parametres_SixLignesDeLaDA()
        {
            (MainMenu menu, MainMenuArt view, GameObject go) = NewMenu();
            try
            {
                typeof(MainMenu).GetField("_page", Private).SetValue(menu, MainMenu.Page.Parametres);
                view.Sync();
                Assert.That(view.SettingsScreen.activeSelf, Is.True);
                for (int i = 0; i < GameSettings.Labels.Length; i++)
                {
                    Transform row = view.SettingsScreen.transform.Find("Option_" + i);
                    Assert.That(row.Find("Libelle").GetComponent<Text>().text, Is.EqualTo(GameSettings.Labels[i]));
                    Assert.That(row.Find("Valeur").GetComponent<Text>().text, Is.EqualTo("‹  " + GameSettings.Value((GameSettings.Line)i) + "  ›"));
                }
                Assert.That(menu.SettingsMenu.Items.Count, Is.EqualTo(7), "6 lignes et Retour");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void InterfaceEnJeu_PrefabsDeLaDA_DansLHabillageDuHud()
        {
            var skin = UnityEditor.AssetDatabase.LoadAssetAtPath<ItHunts.Settings.HudSkinSettings>("Assets/_Project/Settings/HudSkin_Default.asset");
            Assert.That(skin, Is.Not.Null);
            Assert.That(skin.reticlePrefab, Is.Not.Null, "réticule");
            Assert.That(skin.chargesPrefab, Is.Not.Null, "charges");
            Assert.That(skin.parryPrefab, Is.Not.Null, "parade");
            Assert.That(skin.merchantPanelPrefab, Is.Not.Null, "marchand");
            Assert.That(skin.chestPanelPrefab, Is.Not.Null, "coffre");
            var hud = UnityEditor.AssetDatabase.LoadAssetAtPath<ItHunts.Settings.HudSettings>("Assets/_Project/Settings/Hud_Default.asset");
            Assert.That(hud.showAmmoRow, Is.False, "recharge de tir retirée du HUD");
        }

        [Test]
        public void SonsDuJeu_DeLaDA_TousBranches()
        {
            var s = ItHunts.Audio.GameAudio.Settings;
            Assert.That(s, Is.Not.Null, "Resources/GameAudio_Default");
            foreach (var f in typeof(ItHunts.Settings.GameAudioSettings).GetFields())
            {
                object v = f.GetValue(s);
                if (f.FieldType == typeof(AudioClip))
                    Assert.That(v, Is.Not.Null, f.Name);
                else if (f.FieldType == typeof(AudioClip[]))
                    Assert.That(((AudioClip[])v).Length >= 2 && System.Array.TrueForAll((AudioClip[])v, c => c != null), Is.True, f.Name + " : 2 variantes");
            }

            float effects = GameSettings.EffectsVolume, music = GameSettings.MusicVolume;
            try
            {
                GameSettings.EffectsVolume = 0.5f;
                GameSettings.MusicVolume = 0.8f;
                Assert.That(GameSettings.MusicGain, Is.EqualTo(1.6f).Within(1e-3f), "la musique suit son réglage, pas celui des effets");
            }
            finally
            {
                GameSettings.EffectsVolume = effects;
                GameSettings.MusicVolume = music;
            }
        }
    }
}
