# Mouvement

**Status:** in progress · **Updated:** 2026-11-11 · [GDD](../../GDD.md) · [Implementation](implementation.md) · [Testing](testing.md)

Le joueur court, saute, glisse, rebondit sur les murs et enchaîne les sauts sans perdre son
élan. C'est sa seule défense contre le boss traqueur, plus rapide que la course de base : la
vitesse se gagne par un mouvement propre et se perd en heurtant un mur. Pilier : « le mouvement
est la survie ».

## Règles

1. **Contrôleur cinématique** à pas fixe (60 Hz) : la vitesse est calculée explicitement à
   partir d'une structure d'entrée, jamais par un Rigidbody dynamique. Les délais sont comptés
   en pas (ticks), pas en secondes décrémentées.
2. **États :** Sol, Air, Glissade, WallSlide, WallJump (et plus tard Magnétisme). Une seule
   transition par pas, depuis une table de transitions explicite.
3. **Sol :** accélération vers la vitesse voulue, friction quand il n'y a pas d'entrée.
4. **Air (façon Source) :** l'accélération ne s'applique que sur la composante de la direction
   voulue, plafonnée à une petite vitesse. Tourner la souris en strafant fait donc gagner de la
   vitesse, sans plafond strict.
   **Contrôle aérien (façon CPMA / Deadlock) :** en avançant **sans strafer** (entrée à moins
   d'environ 19° de l'avant), la vitesse acquise tourne vers la direction de la caméra **sans
   changer de valeur**. En strafant, il ne s'applique pas : le gain du bunny hop reste intact.
5. **Saut :** valable pendant le coyote time après avoir quitté un bord ; une pression juste
   avant l'atterrissage est mémorisée (buffer). Relâcher tôt coupe la vitesse verticale (hauteur
   variable) ; la gravité est plus forte en descente.
6. **Bunny hop :** un saut pris dans la fenêtre d'atterrissage (buffer) ne subit **aucune
   friction** au sol ce pas-là. La vitesse horizontale est conservée intégralement.
   **Auto-bunny hop** (option, activée par défaut) : maintenir le saut fait resauter à chaque
   atterrissage ; maintenir ne déclenche jamais de saut en l'air.
7. **Sauts multiples :** `maxJumps` sauts au total avant de retoucher le sol (2 par défaut : 1 au
   sol + 1 en l'air ; des améliorations pourront l'augmenter). Un saut en l'air demande un nouvel
   appui, annule la chute et **garde la vitesse horizontale**. Les sauts se rechargent au sol et
   au contact d'un mur (branché avec la détection des murs). Tomber d'un bord sans sauter fait
   perdre le saut au sol une fois le coyote time passé.
8. **Vitesse :** pas de plafond bas sur la vitesse gagnée (bunny hop, glissade, murs). Seul un
   plafond de sécurité élevé existe ; la limite vient du nombre de sauts (D-012).
9. **Glissade :** déclenchée au sol, au-dessus d'une vitesse minimale ; donne un petit bonus de
   vitesse (avec recharge), réduit la hitbox, et a une friction très faible. **En pente
   descendante, la gravité ajoute de la vitesse** le long de la pente. Elle s'arrête sous une
   vitesse de sortie, ou quand on la relâche. Sauter depuis une glissade garde la vitesse.
   **Elle ne coûte pas d'endurance** (D-010).
10. **Wall slide :** en l'air, contre un mur presque vertical, en poussant vers lui : la chute est
   plafonnée et la vitesse le long du mur est conservée.
11. **Wall jump :** depuis un wall slide (ou dans un court délai après l'avoir quitté) : impulsion
   loin du mur + vers le haut, en **gardant la vitesse tangentielle**. Pas deux wall jumps de
   suite sur le même mur sans toucher le sol ou un autre mur.

## Valeurs

Toutes dans un ScriptableObject `MovementSettings`, modifiables en Play Mode. Valeurs de départ
[ASSUMPTION], à valider en playtest ; les sources seront détaillées dans le plan de l'étape 3.

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Vitesse de course | 7.5 | m/s | doit être nettement **plus lente** que le boss (9) |
| Accélération au sol | 10 | ×vitesse/s | démarrage net, sans être instantané |
| Friction au sol | 6 | /s | l'arrêt ne doit pas glisser (arrêt complet en ≈ 0.35 s) |
| Vitesse d'arrêt (friction minimale) | 2.5 | m/s | |
| Accélération en l'air | 10 | ×vitesse/s | contrôle du strafe |
| Plafond de vitesse voulue en l'air | 1 | m/s | gain du bunny hop |
| Contrôle aérien (0 = Source pur, 1 = instantané) | 0.15 | part de l'angle / tick | ≈ 60 % du virage en 0.1 s, 90 % en 0.25 s |
| Sauts au total (sol + air) | 2 | sauts | |
| Hauteur d'un saut en l'air | 1.3 | m | |
| Auto-bunny hop | activé | — | |
| Plafond de sécurité de la vitesse horizontale | 50 | m/s | ne doit jamais se sentir en jeu |
| Gravité | 20 | m/s² | sensation de poids |
| Multiplicateur de gravité en chute | 1.5 | × | chute nerveuse, pas brutale |
| Vitesse de chute max | 35 | m/s | chutes longues |
| Hauteur de saut | 1.3 | m | franchir une caisse de 1 m (→ impulsion 7.2 m/s, sommet en 0.36 s) |
| Coupe de saut au relâchement | 0.5 | × | contrôle de hauteur |
| Coyote time | 0.10 | s | sauts ratés au bord |
| Buffer de saut | 0.12 | s | bunny hop régulier |
| Vitesse minimale de glissade | 6 | m/s | |
| Bonus de vitesse en entrée de glissade | 2 | m/s | |
| Recharge du bonus de glissade | 0.8 | s | spam de glissade |
| Friction en glissade | 0.5 | /s | durée d'une glissade à plat |
| Accélération en pente (part de g·sin θ) | 1.0 | × | gain en descente |
| Vitesse de sortie de glissade | 4 | m/s | |
| Hauteur de hitbox debout / en glissade | 1.8 / 0.9 | m | passages bas |
| Chute max en wall slide | 3 | m/s | |
| Wall jump : impulsion loin du mur | 7.5 | m/s | |
| Wall jump : hauteur | 1.05 | m | 0.8 × saut au sol (→ 6.5 m/s) |
| Délai après le mur pour wall jump (wall-coyote) | 0.10 | s | |
| Verrou de direction après wall jump | 0.15 | s | le joueur annule-t-il son propre élan ? |
| Capsule : rayon / hauteur des yeux | 0.4 / 1.6 | m | |
| Pente max marchable / marche franchie | 45 / 0.3 | ° / m | |

### Caméra

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| FOV vertical de base | 70 | ° | ≈ 103° horizontal en 16:9 |
| FOV en plus à pleine vitesse | +12 | ° | atteint à 20 m/s, à partir de 8 m/s |
| Vitesse d'ajustement du FOV | 8 | /s | sans à-coups |
| Inclinaison max de la vue (haut / bas) | ±89 | ° | viser le sol sous soi (répulsion) |
| Sensibilité souris | 0.1 | °/point | réglable ; inversion Y possible |
| Manette : rotation horizontale / verticale max | 270 / 180 | °/s | réglable ; inversion Y possible |
| Manette : courbe de réponse du stick | 2 | exposant | visée fine au centre |

## Cas particuliers

- Atterrissage sur une pente pendant un bunny hop : la vitesse se projette sur la pente, sans perte.
- Plafond bas pendant une glissade : on ne peut pas se relever, la glissade continue.
- Mur heurté de face à grande vitesse : la composante vers le mur est perdue, le reste conservé.
- Pause et changement de pas fixe : le mouvement ne dépend que du pas fixe, jamais du framerate.

## Questions ouvertes

- Saut « un peu flottant » au premier playtest : gravité et hauteur à revoir plus tard
  (accepté tel quel pour l'instant).
- **Écart de vitesse avec le traqueur (à traiter avec le traqueur, pas maintenant) :** pic de
  26 m/s en bunny hop au playtest, soit presque 3 × la vitesse du traqueur (9-11 m/s). Pistes :
  le traqueur coupe les trajectoires, prend des raccourcis, accélère quand il voit le joueur ;
  des arènes qui cassent l'élan. Voir [boss traqueur](../boss-traqueur/design.md).

## History

- 2026-11-11 : designed.
- 2026-11-11 : glissade sans coût d'endurance (D-010).
- 2026-11-11 : playtest de l'étape 1 ; ajout du contrôle aérien, de l'auto-bunny hop, des sauts
  multiples et du plafond de sécurité à 50 m/s (D-012).
