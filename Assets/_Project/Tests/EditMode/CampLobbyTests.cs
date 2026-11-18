using ItHunts.Level;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Campement (menus) : places, rejoindre et quitter, doublons (choisis, mais Prêt et Jouer refusés), dernier personnage enregistré.
    /// </summary>
    public class CampLobbyTests
    {
        private static readonly bool[] Playable = { true, true, false, false };   // renard, caméléon, koala et hibou « bientôt »

        [Test]
        public void Rejoindre_Quitter_PremierePlaceLibre_LimiteLocale()
        {
            var lobby = new CampLobby(Playable, 1, maxLocal: 2);
            Assert.That(lobby.Count, Is.EqualTo(1), "J1 assis d'emblée");
            Assert.That(lobby[0].Character, Is.EqualTo(1), "J1 : le personnage proposé (dernier joué)");
            Assert.That(lobby.Join("manette 2"), Is.EqualTo(1));
            Assert.That(lobby[1].Character, Is.EqualTo(0), "J2 : le premier personnage jouable libre");
            Assert.That(lobby.Join("manette 2"), Is.EqualTo(-1), "déjà assis");
            Assert.That(lobby.Join("manette 3"), Is.EqualTo(-1), "écran partagé à 2 : les places 3 et 4 attendent le jeu en ligne");
            Assert.That(lobby.Leave("manette 2"), Is.True);
            Assert.That(lobby.Occupied(1), Is.False);
            Assert.That(lobby.Join("manette 3"), Is.EqualTo(1), "la place libérée se reprend");
            Assert.That(lobby.Leave("inconnue"), Is.False);
        }

        [Test]
        public void Doublon_ChoisiMaisPretEtJouerRefuses()
        {
            var lobby = new CampLobby(Playable, 0);
            lobby.Join("manette 2");
            Assert.That(lobby.SetCharacter(1, 0), Is.True, "le même personnage se choisit");
            Assert.That(lobby.Duplicate(0) && lobby.Duplicate(1), Is.True);
            Assert.That(lobby.TakenBy(0, 1), Is.EqualTo(new[] { 0 }), "pris par J1 (grisé dans le choix de J2)");
            Assert.That(lobby.ToggleReady(0), Is.False, "Prêt refusé tant qu'il y a un doublon");
            Assert.That(lobby.CanLaunch, Is.False);
            lobby.SetCharacter(1, 1);
            Assert.That(lobby.ToggleReady(0) && lobby.ToggleReady(1), Is.True);
            Assert.That(lobby.CanLaunch, Is.True);
            lobby.SetCharacter(1, 0);
            Assert.That(lobby[1].Ready, Is.False, "changer de personnage fait tomber Prêt");
            Assert.That(lobby.CanLaunch, Is.False, "un doublon après coup bloque Jouer");
            Assert.That(lobby.SetCharacter(1, 2), Is.False, "un personnage « bientôt » ne se choisit pas");
        }

        [Test]
        public void Solo_JouerDirect_Coop_RaisonDuRefus_DernierPersonnageEnregistre()
        {
            var lobby = new CampLobby(Playable, 3);
            Assert.That(lobby[0].Character, Is.EqualTo(0), "proposé non jouable : le premier jouable");
            Assert.That(lobby.CanLaunch, Is.True, "solo : pas de Prêt, Jouer lance directement");
            lobby.Join("manette 2");
            Assert.That(lobby.Blocker(i => i == 0 ? "Renard" : "Caméléon"), Is.EqualTo("J1 n'est pas prêt"), "coop : chacun se met prêt");
            lobby.SetCharacter(1, 0);
            Assert.That(lobby.Blocker(i => i == 0 ? "Renard" : "Caméléon"), Is.EqualTo("Renard pris deux fois"), "le doublon d'abord");
            lobby.SetCharacter(1, 1);
            lobby.ToggleReady(0);
            lobby.ToggleReady(1);
            Assert.That(lobby.CanLaunch, Is.True);
            string keep = CampLobby.LoadLast();
            try
            {
                CampLobby.SaveLast("renard");
                Assert.That(CampLobby.LoadLast(), Is.EqualTo("renard"));
            }
            finally
            {
                if (string.IsNullOrEmpty(keep))
                    PlayerPrefs.DeleteKey(CampLobby.LastCharacterKey);
                else
                    CampLobby.SaveLast(keep);
            }
        }
    }
}
