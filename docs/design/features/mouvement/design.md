# Mouvement

**Status:** designed · **Updated:** 2026-11-11 · [GDD](../../GDD.md)

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
5. **Saut :** valable pendant le coyote time après avoir quitté un bord ; une pression juste
   avant l'atterrissage est mémorisée (buffer). Relâcher tôt coupe la vitesse verticale (hauteur
   variable) ; la gravité est plus forte en descente.
6. **Bunny hop :** un saut pris dans la fenêtre d'atterrissage (buffer) ne subit **aucune
   friction** au sol ce pas-là. La vitesse horizontale est conservée intégralement.
7. **Glissade :** déclenchée au sol, au-dessus d'une vitesse minimale ; donne un petit bonus de
   vitesse (avec recharge), réduit la hitbox, et a une friction très faible. **En pente
   descendante, la gravité ajoute de la vitesse** le long de la pente. Elle s'arrête sous une
   vitesse de sortie, ou quand on la relâche. Sauter depuis une glissade garde la vitesse.
   **Elle ne coûte pas d'endurance** (D-010).
8. **Wall slide :** en l'air, contre un mur presque vertical, en poussant vers lui : la chute est
   plafonnée et la vitesse le long du mur est conservée.
9. **Wall jump :** depuis un wall slide (ou dans un court délai après l'avoir quitté) : impulsion
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
| Sensibilité souris | 0.1 | °/point | réglable |
| Rotation max à la manette / courbe | 270 / 2 | °/s / exposant | visée fine |

## Cas particuliers

- Atterrissage sur une pente pendant un bunny hop : la vitesse se projette sur la pente, sans perte.
- Plafond bas pendant une glissade : on ne peut pas se relever, la glissade continue.
- Mur heurté de face à grande vitesse : la composante vers le mur est perdue, le reste conservé.
- Pause et changement de pas fixe : le mouvement ne dépend que du pas fixe, jamais du framerate.

## Questions ouvertes

- [ASSUMPTION] Pas de plafond de vitesse global ; un plafond doux à 25 m/s si le bunny hop casse
  les arènes.

## History

- 2026-11-11 : designed.
- 2026-11-11 : glissade sans coût d'endurance (D-010).
