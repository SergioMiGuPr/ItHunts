# Orbes

**Status:** in progress · **Updated:** 2026-11-12 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

Deux orbes, un seul actif, échange instantané. Le magnétisme déplace le joueur (et bouscule les
ennemis) ; la télékinésie transforme l'arène en munitions. De base, les orbes ne font pas de
dégâts directs ; des améliorations peuvent en ajouter, en plus de l'environnement (D-008). Elles
collectent aussi les âmes autour d'elles. Piliers : « l'arène est
l'arme », « le mouvement est la survie ».

## Règles

1. **Échange :** une touche (molette au clavier-souris, un bouton à la manette), sans recharge.
   Échanger lâche l'objet tenu.
2. **États de chaque orbe :** Prête → EnUsage → Recharge → Prête (pour le magnétisme, un état par
   mode). Coût : le **magnétisme** n'utilise **aucune charge de mouvement** (D-021) : l'attraction a
   ses propres **charges d'attraction**, la répulsion une **charge unique** ; la **télékinésie**
   consomme sa propre **endurance**. Usage refusé (son et HUD) : sans charge, en recharge, ou rien à
   portée. **HUD** (sous les charges de mouvement) : les charges d'attraction en bleu, la répulsion en
   orange (pleine, ou en recharge avec le temps restant).
3. **Attraction (magnétisme, mode 1) : traction linéaire continue.** Tant que le bouton est tenu (clic
   gauche ou RT), le joueur est tiré **en ligne droite** vers le point visé au moment du clic, sur
   **n'importe quelle surface** (murs, sol, plafond ; jamais les ennemis). Pas de câble ni de
   balancement, **ni gravité ni friction** pendant la traction. La vitesse monte avec une forte
   accélération jusqu'à la vitesse de traction ; elle **ne baisse jamais** : plus vite que la vitesse de
   traction, on garde toute sa vitesse, redirigée vers le point. Quand le point est plus haut, la chute
   n'est pas comptée. **Pas de plafond des gains** (exception au plafond commun ; seul le plafond de
   sécurité de 50 m/s reste, D-021). Fin de la traction, avec toute la vitesse et la direction du
   moment : **relâchement**, **arrivée** au point, **ligne de vue coupée**, **durée max**, **saut**
   (le saut part aussitôt), **dash**, **répulsion**, ou obstacle qui bloque. **Chaque traction coûte
   1 charge d'attraction** (3 charges, séparées des charges de mouvement) ; elles reviennent une par
   une, 1 toutes les 3 s. Pas d'autre délai entre deux tractions.
4. **Répulsion (magnétisme, mode 2) :** appui (clic droit ou LT), **impulsion instantanée** à
   l'opposé du point visé, sur **n'importe quelle surface** (rocket jump). La **vitesse horizontale ne
   baisse jamais** (au pire conservée et redirigée) ; son gain reste dans le plafond commun des gains
   (20 m/s). Quand l'impulsion monte, la chute est annulée. **Une seule charge, qui ne s'accumule
   pas** : elle revient 5 s après chaque usage ; un appui juste avant est mémorisé un court instant.
   **Marquage :** quand une surface est à portée, un disque orange marque au viseur le point où la
   répulsion s'appliquera (terne pendant la recharge) ; à l'usage, une marque s'étend puis s'efface au
   point d'impact. Au point visé, une **onde** bouscule les ennemis dans son rayon. Un ennemi qui
   percute un mur au-dessus d'une vitesse d'impact prend des dégâts proportionnels. **Sans effet
   sur le boss.**
5. **Attraper (télékinésie, mode 1) :** un rayon attrape un objet, un petit ennemi (sous la masse
   maximale) ou un projectile ennemi. La goule n'est attrapable qu'**étourdie**. L'objet tenu
   flotte devant le joueur.
6. **Lancer (télékinésie, mode 2) :** l'objet part dans la direction de visée ; ses dégâts
   dépendent de sa masse et de sa vitesse d'impact. Un projectile ennemi renvoyé blesse son tireur.
7. **Âmes :** chaque orbe aspire automatiquement les âmes dans un rayon autour d'elle.
8. **Dégâts par amélioration :** une amélioration peut donner des dégâts directs à une orbe
   (par exemple l'onde de répulsion) ou un bonus aux objets lancés. Ces dégâts s'ajoutent à
   ceux de l'environnement, sans les remplacer.
9. **Retour du magnétisme (greybox) :**
   - **Lueur** (bloom) : l'orbe, le trait de traction, l'ancre au point de traction et les marques
     brillent ; le décor non. Les surfaces ne brillent pas : toutes répondent au magnétisme.
   - **Viseur** : un anneau bleu quand une traction est possible (surface à portée, une charge
     d'attraction), un anneau orange autour quand la répulsion l'est (surface à portée, charge prête).
   - **Éclats** : bleu au point d'ancrage au début d'une traction, orange au point d'impact d'une
     répulsion. Les marques gardent la même taille à l'écran au loin.
   - **Refus** : le groupe concerné du HUD (bleu ou orange) clignote en rouge 0.3 s, avec un double bip.
   - **Sons provisoires** (synthétisés, 2D pour le joueur local ; vrais sons à l'étape direction
     artistique, mêmes événements) : « zip » au début de la traction,
     bourdonnement pendant la traction dont la hauteur monte avec la vitesse, clic à la fin, coup sourd
     et souffle à la répulsion, double bip au refus, tintement quand une charge revient.

## Valeurs

ScriptableObjects `MagnetismOrbSettings`, `TelekinesisOrbSettings`, `StaminaSettings` (endurance de
la télékinésie). Les charges de mouvement sont dans `MovementSettings`. Valeurs de départ [ASSUMPTION].

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Endurance max (télékinésie) | 100 | pts | |
| Régénération / délai avant régénération | 25 / 0.8 | pts/s / s | rythme d'usage |
| Portée de l'attraction | 60 | m | plafond d'une grande salle depuis le sol |
| Accélération / vitesse de traction | 80 / 30 | m/s² / m/s | 30 m/s atteints en 0.38 s |
| Durée max d'une traction | 2.5 | s | |
| Arrivée au point | 1 | m | |
| Charges d'attraction / recharge d'une charge | 3 / 3 | charges / s | une par une ; séparées des charges de mouvement |
| Impulsion / portée de la répulsion | 14 / 40 | m/s / m | hauteur du rocket jump (mesuré : 5.0 m ; 7.6 m avec un saut juste avant) |
| Recharge de la répulsion (charge unique, sans cumul) | 5 | s | |
| Mémorisation d'un appui pendant la recharge | 0.1 | s | |
| Durée de l'état « En usage » de la répulsion (retour visuel) | 0.15 | s | |
| Sans friction au sol après une répulsion | 0.2 | s | |
| Clignotement du refus / période | 0.3 / 0.1 | s | |
| Bourdonnement : hauteur à l'arrêt → à 40 m/s | 0.8 → 2 | × 110 Hz | |
| Bloom de la scène de test : seuil / intensité | 1 / 1.2 | — | lisibilité à pleine vitesse |
| Rayon de l'onde de répulsion / poussée | 4 / 12 | m / m/s | |
| Vitesse d'impact mini contre un mur / dégâts | 6 / 5 | m/s / PV par m/s | |
| Portée de la télékinésie | 15 | m | |
| Masse maximale attrapable | 40 | kg | quels ennemis sont « petits » |
| Vitesse de lancer | 30 | m/s | |
| Coût attraper / lancer | 10 / 15 | pts | |
| Recharge télékinésie | 0.3 | s | |
| Durée d'étourdissement de la goule | 1.5 | s | fenêtre du combo |
| Rayon de collecte des âmes | 6 | m | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Mouvement](../mouvement/design.md) | état Traction ; la traction et la répulsion ne font jamais perdre d'élan ; pas de charges de mouvement |
| [Boss traqueur](../boss-traqueur/design.md) | aucun effet sur lui |

## Cas particuliers

- Attraction ou répulsion pendant une glissade : aucune perte ; la traction sort de la glissade, la
  répulsion horizontale la laisse continuer.
- Pendant un dash : le dash se termine ; la vitesse gardée est celle de sortie du dash, pas le pic.
- Impulsion ou traction vers le haut depuis le sol : on décolle (pas de saut au sol via le coyote time).
- Répulsion pendant une traction : la traction s'arrête, la répulsion s'applique.
- Objet tenu qui touche le joueur : pas de collision avec son porteur (pas de « vol sur caisse »).
- Mort ou échange en tenant un objet : l'objet tombe avec sa vitesse actuelle.

## Questions ouvertes

- [ASSUMPTION] L'objet tenu ne bloque pas les projectiles (pas de bouclier).
- Rester en l'air : 3 tractions de suite, puis 1 toutes les 3 s, et 1 répulsion toutes les 5 s. À
  revoir en playtest, notamment face au traqueur.
- Une traction vers le bas plus rapide que 35 m/s est ramenée à la chute max dès qu'elle s'arrête.
- L'onde de répulsion (ennemis bousculés) attend les ennemis (étape 6).

## History

- 2026-11-11 : designed.
- 2026-11-11 : dégâts directs possibles via les améliorations (D-008).
- 2026-11-12 : le magnétisme consomme les charges de mouvement ; l'endurance ne sert plus qu'à la
  télékinésie (D-015).
- 2026-11-12 : in progress. Magnétisme partie 1 (attraction, répulsion, charges, recharge) : annulation de
  la vitesse opposée, plafond commun, surfaces magnétiques seulement, deux boutons (D-020).
- 2026-11-12 : après playtest : traction continue, toute surface, sans charges, recharges propres,
  aucune perte d'élan, orbe visible (D-021).
- 2026-11-12 : retour visuel et sonore : lueur, viseur à deux anneaux, éclats, refus en rouge, sons synthétisés
  (choix 1a 2a 3a ; réglages dans `MagnetismFeedbackSettings`).
- 2026-11-12 : après le playtest v2 : 3 charges d'attraction (1 toutes les 3 s), répulsion unique
  (40 m, 5 s), marquage de la répulsion, HUD des charges d'attraction et de la répulsion (D-021 mise à jour).
