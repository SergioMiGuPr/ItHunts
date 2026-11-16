# Artefacts

**Status:** implemented · **Updated:** 2026-11-15 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

Un artefact est une arme ou un pouvoir de combat porté en **main gauche**, en plus de l'orbe (main droite). Il donne
une façon de se battre quand l'arène ne fournit plus rien à lancer. **Chaque personnage a son artefact** (D-064) : la
variété d'une run à l'autre vient des améliorations et de la carte. Cohérent avec D-008 : de base, l'orbe ne fait pas de
dégâts directs ; les artefacts et les améliorations en ajoutent (D-022). Décisions D-022, D-032, D-064 et D-065.

## Règles

1. **Personnages (D-064, remplace le tirage de D-032) :** le joueur choisit son personnage dans le menu, avant la run ;
   l'artefact vient avec lui.

   | Personnage | Couleur | Artefact | Jouable |
   |---|---|---|---|
   | Renard | rouge | lanterne de feu (D-065) | oui |
   | Caméléon | orange | barillet (D-032) | oui |
   | Koala | vert | à concevoir | grisé (« bientôt ») |
   | Hibou | bleu | à concevoir | grisé (« bientôt ») |

   Un asset par personnage (`CharacterDefinition`, dans `Settings/Personnages/`), listés dans `Characters_Default`
   (ordre du menu ; personnage par défaut : le caméléon). Un joueur qui n'a rien choisi (scène de test ouverte
   directement) prend le caméléon ; le choix du menu vaut aussi pour les scènes de test lancées depuis le menu.
2. **Coop : chaque joueur prend un personnage différent** (remplace « pas de doublon d'artefact »). **Au menu**, le
   joueur 2 rejoint avec Start puis choisit **avec sa propre manette** (◀ ▶, A pour valider, B pour changer) ; sa
   manette ne pilote pas le menu du joueur 1 ; le personnage de l'autre est sauté ; **la partie ne se lance que quand il a
   validé**. **En jeu**, un joueur qui rejoint avec Start choisit dans un menu de sa moitié d'écran, piloté par sa
   manette (personnages pris : « pris », grisés) ; il ne bouge pas pendant le choix ; B garde le personnage proposé (le
   premier libre). Si le joueur 1 joue au clavier, l'appui sur Start d'une nouvelle manette ne la lui attribue plus (le
   changement automatique de l'Input System la lui prenait : le joueur 2 ne pouvait pas rejoindre). Plus de joueurs que de personnages jouables : **pas d'artefact** pour les
   derniers plutôt qu'un doublon (2 personnages jouables aujourd'hui, l'écran partagé est à 2).
3. **Emplacement :** un seul artefact à la fois, en main gauche. Système générique : chaque artefact a son asset
   (`ArtifactDefinition`) avec son tir, ses munitions et son rechargement ; le pool est `ArtifactPool_Default`.
4. **Touche (D-024) :** le **clic gauche / RT** est l'action principale : si l'orbe tient un objet, il le **lance** ;
   sinon, il **utilise l'artefact**. **Tir et lancer ne partent jamais au même tick** : le clic d'un lancer ne tire
   pas. **R / Y** recharge.
5. **Premier artefact, le barillet** : la main gauche tient un barillet de **6 coups**.
   - **Tir instantané** depuis les yeux, exactement sous le viseur, sans chute, **portée 60 m** ; **12 dégâts** ;
     au plus un tir toutes les **0.25 s** (un clic un peu trop tôt part dès que possible).
   - **Rechargement 1.2 s** : automatique quand le barillet est vide, ou plus tôt avec R / Y s'il manque une
     munition ; pas de tir pendant le rechargement.
   - **Objet tenu** : le barillet reste visible mais **inactif** (grisé, main abaissée) ; **un rechargement en cours
     est annulé** et reprend à zéro au relâchement s'il est vide (le plus simple ; réglable : `grabCancelsReload`
     décoché, il se termine pendant la prise).
   - **Retour :** traînée lumineuse brève de la bouche du canon au point touché, éclat à l'impact, recul de la main
     gauche, le barillet tourne d'un cran ; **munitions lisibles sur le barillet** (6 chambres allumées ou éteintes,
     qui se rallument une à une pendant le rechargement) et sous le viseur (HUD discret) ; sons provisoires (tir,
     rechargement, fin de rechargement).
   - Le **traqueur ignore les balles** (elles l'atteignent sans le blesser).
6. **Lanterne de feu** (renard, D-065) : la main gauche tient une lanterne.
   - **Clic gauche / RT sans objet tenu** : **jet de flammes** de **0.6 s** dans un **cône de 25°** (12.5° de chaque côté
     du viseur ; à 15 m, 6.6 m de large pour un couloir de 14 m), **portée 15 m** **depuis la lanterne** (main gauche : 0.22 m
     à gauche, 0.45 m sous les yeux, 0.35 m devant, `originOffset`) ; **pas de dégâts immédiats : chaque ennemi touché
     brûle** pendant **4 s à 12 PV/s, par ticks de 0.25 s** (3 PV par tick, 48 PV) ; le toucher de nouveau **relance la
     durée sans cumuler** ; flammes sur l'ennemi tant qu'il brûle. Touché **une fois par jet** (un ennemi qui entre dans le
     cône pendant le jet est touché aussi) ; **recharge de 2.5 s** comptée depuis la fin du jet ; un clic un peu trop tôt
     part dès que possible (0.12 s). Une goule de 60 PV meurt en 5 s (2 jets), à 5 comme à 15 m ; un groupe de 3 aussi.
     *Revu deux fois : 25 → 32 PV et 3 → 2.5 s après le premier playtest ; portée × 3, cône 25° et brûlure (correctifs-rendu,
     point 8).*
   - **Le jet traverse les ennemis, les joueurs et les objets (Rigidbody), pas le décor** (collider solide sans
     Rigidbody : même règle que les textes en monde).
   - Comme toute attaque d'un joueur : **un volant meurt en un coup**, le **traqueur n'est pas touché**, le **tir ami** est
     respecté (jamais soi-même), l'ennemi tué **donne ses âmes** et se retourne contre le joueur (D-041).
   - **Objet tenu** : le clic est un lancer, la lanterne est inactive (atténuée, main abaissée) ; **prendre un objet arrête
     le jet** (la recharge commence).
   - **Retour (greybox) :** bouffées de flammes du jaune au rouge, qui s'arrêtent sur le décor ; la main pousse et tremble
     pendant le jet ; **la lanterne s'éteint à la fin du jet et se rallume pendant la recharge** (petit son quand elle est
     prête) ; HUD : un segment sous le viseur qui se vide pendant le jet et se remplit pendant la recharge ; sons provisoires
     (souffle, rallumage). Le modèle de la DA arrivera par une PR (lanterne greybox de 6 × 9 × 6 cm en attendant).
   - **Marchand de l'artefact** (D-050) : il propose **les améliorations de l'artefact du joueur qui achète** (une liste par
     artefact, `ArtifactDefinition.upgrades`). Lanterne, 4 provisoires (`UpgradePool_Lanterne`) : **Long souffle** (portée
     +1.5 m, 45 âmes), **Flamme large** (cône +15°, 50), **Mèche vive** (recharge −0.5 s, 55), **Braises** (durée de la
     brûlure +2 s, 60 ; pas le traqueur ; chiffres toutes les 0.5 s). Elles se cumulent sur une copie des
     réglages du joueur.
7. **Tir ami** (réglage coop `Coop_Default`, **désactivé par défaut**) : les balles, les objets lancés (zone
   comprise) et les objets repoussés d'un joueur blessent les autres joueurs s'il est activé ; **jamais le joueur
   lui-même**. Un objet est attribué au dernier joueur qui l'a attrapé, lancé ou repoussé. PV du joueur provisoires :
   100, réapparition au point de départ à 0 PV (validés pour les tests ; la vraie mort vient à l'étape 8).
8. **Coop en ligne (étape 11) :** le contact entre un objet et un joueur est détecté par le jeu lui-même (Unity ne
   signale pas le choc d'un objet contre un `CharacterController`) ; en ligne, cette détection et les dégâts du tir
   ami devront être calculés **côté autorité réseau** (serveur ou hôte), comme les dégâts des objets.

## Valeurs

Réglages dans `Barrel_Default` et `FireLantern_Default` (artefacts), `Characters_Default` (personnages), `Coop_Default`,
`PlayerHealth_Default` ; modifiables en Play Mode. `ArtifactPool_Default` ne sert plus qu'aux anciennes scènes sans liste
de personnages.

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Munitions | 6 | balles | |
| Dégâts d'une balle | 12 | PV | factice de 100 PV : 9 balles, 2.95 s avec une recharge |
| Temps entre deux tirs | 0.25 | s | cadence ressentie |
| Rechargement | 1.2 | s | |
| Portée | 60 | m | |
| Mémorisation d'un clic | 0.12 | s | |
| Prise d'un objet pendant un rechargement | annulé | | |
| Traînée | 0.06 s, 2 → 5 cm | | lisible sans gêner |
| Tir ami | désactivé, ×1 | | |
| PV du joueur (provisoire) | 100, réapparition 1 s | PV | |
| **Lanterne :** portée | 5 | m | |
| Cône (ouverture totale) | 40 | ° | goules basses à moins de 2 m : viser un peu vers le bas |
| Durée du jet | 0.6 | s | |
| Dégâts par ennemi et par jet | 32 | PV | goule de 60 PV : 2 jets (3.1 s) ; barillet : 1 s |
| Recharge (depuis la fin du jet) | 2.5 | s | rythme ressenti |
| Départ du cône (par rapport aux yeux) | (−0.22, −0.45, 0.35) | m | goule qui rampe à 1 m : touchée |
| Bouffées de flammes | 30 par s, 0.35 s, 0.06 → 0.9 m | | lisible sans masquer la vue |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | même clic gauche / RT que le lancer ; barillet inactif quand l'orbe tient un objet |
| [Vue FPS et HUD](../vue-fps/design.md) | barillet dans la main gauche, munitions sous les jauges de l'orbe, PV du joueur en bas à gauche |
| [Boss traqueur](../boss-traqueur/design.md) | ignore les balles ; pas touché par la lanterne |
| Menu | choix du personnage de chaque joueur (D-064) |
| [Âmes et économie](../ames/design.md) | marchand de l'artefact : améliorations du barillet seulement (le renard n'a que le soin) |
| Améliorations | pourront modifier un artefact (dégâts, munitions…) |
| Coop en ligne | tirage par la seed ; tir instantané à rejouer côté serveur avec compensation de latence ; contact objet-joueur (tir ami) calculé par l'autorité réseau |

## Questions ouvertes

- **Équilibre de la lanterne** (après réglage) : 3.1 s pour une goule seule (barillet : 1 s), 3.1 s pour un groupe de 3
  (barillet : 5.4 s) ; à confirmer en playtest.
- **Artefacts du koala et du hibou** : à concevoir.

- **Tir maintenu :** aujourd'hui un clic = un tir (un clic trop tôt est mémorisé 0.12 s) ; un tir automatique en
  maintenant le bouton reste possible.
- **Balle sur un objet :** elle s'arrête dessus sans le pousser ; une petite poussée serait possible.

## History

- 2026-11-12 : designed (D-022).
- 2026-11-12 : système générique, barillet, tir ami (D-032).
- 2026-11-12 : validé en playtest ; doublons au nouveau tour du pool validés.
- 2026-11-15 : un artefact par personnage, choisi dans le menu, jamais deux fois le même en coop (D-064) ; lanterne de feu
  du renard (D-065) ; orbe à la couleur du personnage avec l'orbe v2 (D-066).
