# Coop locale (écran partagé)

**Status:** implemented · **Updated:** 2026-11-14 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-007, D-059

## Résumé

Deux joueurs sur le même écran, chacun sur sa moitié, avec son HUD, ses mains, sa minimap et son FOV. Le 2e joueur rejoint en
appuyant sur Start d'une manette, au menu ou en jeu. Un joueur à 0 PV tombe à terre au lieu de mourir. Un allié le relève en
maintenant E / Y 3 s à moins de 2 m, et il revient avec 50 PV. S'il n'est pas relevé en 30 s, il devient spectateur jusqu'à la
zone suivante. La partie se termine quand tous les joueurs sont à terre ou morts.

## Règles

1. **Rejoindre.** Le 2e joueur appuie sur **Start** d'une manette qui n'est pas celle du joueur 1 :
   - au menu, l'entrée « Joueur 2 (écran partagé) » le fait aussi ;
   - en jeu, il apparaît à côté du joueur 1.

   Entrées possibles : clavier-souris + une manette, ou deux manettes. Le joueur 1 est verrouillé sur son appareil dès qu'un
   2e joueur rejoint. Au plus 2 joueurs locaux (`maxLocalPlayers`).
2. **Découpe.** Par défaut, gauche / droite (joueur 1 à gauche). Un réglage (`splitHorizontal`) passe en haut / bas.
3. **FOV adapté.** Le FOV vertical est multiplié selon la découpe :
   - gauche / droite : ×1.1 (`splitSideFovScale`), parce que la moitié est étroite ;
   - haut / bas : ×0.75 (`splitStackedFovScale`), parce que la moitié est très large.

   L'élargissement à grande vitesse s'applique par-dessus.
4. **Chacun sa vue.** Chaque moitié a :
   - son HUD (charges, âmes, timer, minimap commune) ;
   - ses mains et son orbe ;
   - les lueurs de ce que **lui** vise et peut attraper.

   L'autre joueur apparaît comme une capsule de couleur : bleue pour le joueur 1, orange pour le joueur 2. Les coffres et les
   offres restent propres à chaque joueur (D-043). Le son vient du joueur 1 (un seul auditeur).
5. **Règles inchangées :**
   - pas de tir ami (`friendlyFire`) ;
   - âmes gagnées en commun et dépensées individuellement ;
   - coffres par joueur ;
   - porte de sortie payée une fois pour le groupe ;
   - minimap commune ;
   - choix de cible des ennemis inchangé.
6. **À terre (D-007, D-059).** À 0 PV, s'il reste un allié debout, le joueur tombe à terre :
   - il ne bouge plus et n'agit plus ;
   - il compte comme mort pour les ennemis et pour le traqueur, qui l'ignore ;
   - un coup du traqueur met à terre comme n'importe quel coup.

   Le dernier joueur debout, lui, meurt.
7. **Réanimation.** Un allié debout maintient **E / Y pendant 3 s à 2 m au plus** :
   - s'il relâche ou s'éloigne, le compte repart de zéro ;
   - le joueur relevé revient avec **50 PV**.

   Une étiquette « À terre, maintiens E / Y » apparaît au-dessus de lui, seulement pour les autres joueurs.
8. **Spectateur.** Au bout de **30 s** à terre sans être relevé, le joueur devient spectateur :
   - sa moitié montre la vue de l'allié vivant, avec le bandeau « Spectateur, tu reviendras à la zone suivante » ;
   - on ne peut plus le relever ;
   - il revient à la zone suivante, avec tous ses PV.
9. **Fin de partie.** Quand tous les joueurs sont à terre, spectateurs ou morts, c'est l'écran de fin de run.

## Réglages (`Coop_Default`)

| Réglage | Valeur | Rôle |
|---|---|---|
| `friendlyFire` | non | balles et objets lancés touchent les alliés |
| `splitHorizontal` | non | haut / bas au lieu de gauche / droite |
| `maxLocalPlayers` | 2 | joueurs locaux au plus |
| `splitSideFovScale` | 1.1 | FOV en gauche / droite |
| `splitStackedFovScale` | 0.75 | FOV en haut / bas |
| `downedSeconds` | 30 | à terre avant de devenir spectateur (s) |
| `reviveHoldSeconds` | 3 | durée de maintien pour relever (s) |
| `reviveRange` | 2 | distance pour relever (m) |
| `reviveHealth` | 50 | PV au retour |

10. **Menu pause** (Échap / Start), un par joueur, dans sa moitié et piloté par son seul appareil ; **le jeu continue**.
    Entrées : Reprendre ; **Quitter l'écran partagé** (joueur 2 seulement : le joueur 1 reprend tout l'écran ; s'il était
    à terre, il meurt, car plus personne ne peut le relever) ; Menu principal.

11. **Aide à la visée à la manette** (après le playtest, D-063) : près d'une cible (goule, volant ; main libre, objet
    attrapable à portée), la visée **ralentit** (×0.5 au centre, cône de 6°, 45 m) et **glisse légèrement** vers elle (8°/s,
    seulement quand le joueur vise ou se déplace). Cible visible seulement. **Activée par défaut à la manette, coupée à la
    souris** (`Camera_Default` : `aimAssistGamepad`, `aimAssistMouse`, angle, portée, ralentissement, attraction).

## Validé en playtest (2026-11-14)

- FOV ×1.1 et ×0.75 ; Start pour rejoindre ; capsules pour les corps (en attendant le vrai modèle, étape 10) ; spectateur
  avec les mains de l'allié.
- Son du joueur 1 seulement : **provisoire** (Unity n'a qu'un auditeur ; un mixage par joueur viendra plus tard).
