# Artefacts

**Status:** implemented · **Updated:** 2026-11-12 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

Un artefact est une arme ou un pouvoir de combat porté en **main gauche**, en plus de l'orbe (main droite). Il donne
une façon de se battre quand l'arène ne fournit plus rien à lancer, et rend chaque run différente. Cohérent avec
D-008 : de base, l'orbe ne fait pas de dégâts directs ; les artefacts et les améliorations en ajoutent (D-022).
Décisions D-022 et D-032.

## Règles

1. **Tirage :** au début de chaque run, chaque joueur reçoit **1 artefact tiré au hasard avec la seed de la run**
   (même seed, même tirage : relance, partie en ligne). Le joueur *i* reçoit l'élément *i* d'une suite fixée par la
   seed ; son artefact ne dépend pas du nombre de joueurs.
2. **Coop :** **pas de doublon tant que le pool suffit**. Quand il y a plus de joueurs que d'artefacts, un nouveau
   tour du pool commence : les doublons n'apparaissent qu'à ce moment, répartis au mieux (avec un pool d'un seul
   artefact, tout le monde a le barillet) (validé, D-032).
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
6. **Tir ami** (réglage coop `Coop_Default`, **désactivé par défaut**) : les balles, les objets lancés (zone
   comprise) et les objets repoussés d'un joueur blessent les autres joueurs s'il est activé ; **jamais le joueur
   lui-même**. Un objet est attribué au dernier joueur qui l'a attrapé, lancé ou repoussé. PV du joueur provisoires :
   100, réapparition au point de départ à 0 PV (validés pour les tests ; la vraie mort vient à l'étape 8).
7. **Coop en ligne (étape 11) :** le contact entre un objet et un joueur est détecté par le jeu lui-même (Unity ne
   signale pas le choc d'un objet contre un `CharacterController`) ; en ligne, cette détection et les dégâts du tir
   ami devront être calculés **côté autorité réseau** (serveur ou hôte), comme les dégâts des objets.

## Valeurs

Réglages dans `Barrel_Default` (artefact), `ArtifactPool_Default`, `Coop_Default`, `PlayerHealth_Default` ; modifiables
en Play Mode.

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

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | même clic gauche / RT que le lancer ; barillet inactif quand l'orbe tient un objet |
| [Vue FPS et HUD](../vue-fps/design.md) | barillet dans la main gauche, munitions sous les jauges de l'orbe, PV du joueur en bas à gauche |
| [Boss traqueur](../boss-traqueur/design.md) | ignore les balles |
| Améliorations | pourront modifier un artefact (dégâts, munitions…) |
| Coop en ligne | tirage par la seed ; tir instantané à rejouer côté serveur avec compensation de latence ; contact objet-joueur (tir ami) calculé par l'autorité réseau |

## Questions ouvertes

- **Tir maintenu :** aujourd'hui un clic = un tir (un clic trop tôt est mémorisé 0.12 s) ; un tir automatique en
  maintenant le bouton reste possible.
- **Balle sur un objet :** elle s'arrête dessus sans le pousser ; une petite poussée serait possible.

## History

- 2026-11-12 : designed (D-022).
- 2026-11-12 : système générique, barillet, tir ami (D-032).
- 2026-11-12 : validé en playtest ; doublons au nouveau tour du pool validés.
