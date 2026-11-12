# Mouvement

**Status:** implemented · **Updated:** 2026-11-11 · [GDD](../../GDD.md) · [Implementation](implementation.md) · [Testing](testing.md)

Le joueur court, saute, glisse, rebondit sur les murs et enchaîne les sauts sans perdre son
élan. C'est sa seule défense contre le boss traqueur, plus rapide que la course de base : la
vitesse se gagne par un mouvement propre et se perd en heurtant un mur. Pilier : « le mouvement
est la survie ».

## Règles

1. **Contrôleur cinématique** à pas fixe (60 Hz) : la vitesse est calculée explicitement à
   partir d'une structure d'entrée, jamais par un Rigidbody dynamique. Les délais sont comptés
   en pas (ticks), pas en secondes décrémentées.
2. **États :** Sol, Air, Glissade, WallSlide, WallJump, Traction (magnétisme, [orbes](../orbes/design.md)). Une seule
   transition par pas, depuis une table de transitions explicite.
3. **Sol :** accélération vers la vitesse voulue, friction quand il n'y a pas d'entrée.
4. **Air (façon Source) :** l'accélération ne s'applique que sur la composante de la direction
   voulue, plafonnée à une petite vitesse. Tourner la souris en strafant fait donc gagner de la
   vitesse, sans plafond strict.
   **Contrôle aérien (façon CPMA / Deadlock) :** en avançant **sans strafer** (entrée à moins
   d'environ 19° de l'avant), la vitesse acquise tourne vers la direction de la caméra **sans
   changer de valeur**. En strafant, il ne s'applique pas : le gain du bunny hop reste intact.
   Il ne s'applique pas non plus si la caméra regarde à plus de 100° de la trajectoire : tenir W
   face au mur après un wall jump ne ramène pas vers le mur (D-014).
5. **Saut :** valable pendant le coyote time après avoir quitté un bord ; une pression juste
   avant l'atterrissage est mémorisée (buffer). Relâcher tôt coupe la vitesse verticale (hauteur
   variable) ; la gravité est plus forte en descente.
6. **Bunny hop :** un saut pris dans la fenêtre d'atterrissage (buffer) ne subit **aucune
   friction** au sol ce pas-là. La vitesse horizontale est conservée intégralement.
   **Auto-bunny hop** (option, activée par défaut) : maintenir le saut fait resauter à chaque
   atterrissage ; maintenir ne déclenche jamais de saut en l'air.
7. **Sauts multiples :** `maxJumps` sauts au total avant de retoucher le sol (2 par défaut : 1 au
   sol + 1 en l'air ; des améliorations pourront l'augmenter). **Chaque saut en l'air coûte aussi
   1 charge de mouvement** (règle 12). Un saut en l'air demande un nouvel
   appui, annule la chute et **garde la vitesse horizontale**. Les sauts se rechargent au sol et
   au contact d'un mur (branché avec la détection des murs). Tomber d'un bord sans sauter fait
   perdre le saut au sol une fois le coyote time passé.
8. **Vitesse :** pas de plafond bas sur la vitesse gagnée (bunny hop, glissade, murs). Seul un
   plafond de sécurité élevé existe ; la limite vient du nombre de sauts et des charges (D-012).
9. **Glissade :** déclenchée au sol, au-dessus d'une vitesse minimale. Elle **garde 100 % de la
   vitesse acquise** (bunny hop compris), plus un bonus s'il est rechargé ; aucun plafond à
   l'entrée. Elle réduit la hitbox et freine par une **décélération constante** (m/s²), pas
   proportionnelle à la vitesse : aller vite ne fait pas perdre plus vite (D-013). **En pente
   descendante, la gravité ajoute de la vitesse** le long de la pente. Elle s'arrête sous une
   vitesse de sortie, ou quand on la relâche. Sauter depuis une glissade garde la vitesse.
   **Elle ne coûte pas d'endurance** (D-010).
   - Touche **maintenue** (Ctrl gauche ou C ; B / Rond à la manette). Atterrir en la tenant,
     assez vite, enchaîne directement sur une glissade. Saut et glissade tenus ensemble à
     l'atterrissage : **le saut gagne** (le bunny hop n'est pas cassé).
   - En montée, la pente freine. Un léger braquage tourne la vitesse sans la changer.
   - **Sous un obstacle**, la glissade ne s'arrête pas, même touche relâchée : elle continue à
     une vitesse minimale jusqu'à la sortie. On ne se relève (et on ne saute) que s'il y a la place.
   - **Accroupi** : sous la vitesse minimale, la touche accroupit (capsule basse, marche lente).
10. **Wall slide (D-016) :** en l'air, au contact d'un mur presque vertical (plus d'environ 72°),
   **si l'entrée vise le mur** (tolérance d'angle) **ou si on le longe assez vite**, même sans
   pousser. Aucune perte de vitesse le long du mur : la part de vitesse qui entre dans le mur est
   reportée le long du mur (pousser contre le mur ne fait pas accélérer pour autant). Chute
   plafonnée, d'autant plus lente qu'on longe le mur vite (**effet wall run**). On ne s'accroche pas
   à un mur dont on s'éloigne. Toucher un mur **recharge le compteur de sauts**, pas les charges
   de mouvement (réglable).
11. **Wall jump (D-016) :** depuis le mur (ou dans un court délai après l'avoir quitté) : saut
   plus haut, et impulsion **ajoutée à la vitesse acquise** dans une direction qui **mélange la
   normale du mur et l'avant** (caméra sans sa part vers le mur, sinon trajectoire). La vitesse
   horizontale après le saut est au moins celle d'avant + un petit bonus. Pas deux wall jumps de
   suite sur le même mur sans toucher le sol ou un autre mur. Nouvel appui obligatoire (maintenir
   ne suffit pas) ; ne consomme pas de saut. Priorité : saut au sol (coyote compris), puis wall
   jump, puis saut en l'air. Pendant le verrou de direction, aucun contrôle en l'air.

12. **Charges de mouvement et dash (D-015) :** une barre de **3 charges** (réglable, les
    améliorations pourront l'augmenter), partagée par le **saut en l'air** et le **dash** : 1 charge
    chacun. Le magnétisme ne les utilise pas : il a ses propres recharges (D-021). Le saut au
    sol et le wall jump sont gratuits. Les charges se rechargent une par une avec le temps, **plus
    vite au sol** qu'en l'air. Sans charge, l'action est refusée (son et HUD). La télékinésie a sa
    propre jauge d'endurance ([orbes](../orbes/design.md)).
    - **Dash** (Maj gauche ; bouton d'épaule gauche à la manette) : direction de l'entrée par
      rapport à la caméra (vers l'avant de la caméra sans entrée). Il **dure** un court instant,
      **sans friction ni gravité**, avec un **profil progressif** : départ au pic, puis baisse
      régulière jusqu'à la vitesse de sortie (pic calculé pour parcourir la distance du dash ;
      profil constant possible). Vitesse de sortie, dans la direction du dash :
      - **au sol** : vitesse d'avant **+ 1 m/s** (gain plafonné), au moins la vitesse de sortie ;
      - **en l'air** : **aucun gain**, exactement la vitesse d'avant, redirigée (au moins la vitesse
        de sortie depuis l'arrêt).
      Puis un court moment sans friction au sol.
    - **Dash-jump** (au sol seulement) : un saut pendant un dash parti du sol, ou juste après sa fin
      (fenêtre), donne **+3 m/s** par rapport à la vitesse d'avant le dash, au lieu de +1. Le debug
      affiche « réussi » ou « raté, trop tard de N ms ».
    - **Plafond commun des gains** : wall jump, dash au sol, dash-jump et bonus de glissade
      n'ajoutent de la vitesse qu'en dessous de 20 m/s ; au-delà, la vitesse est conservée sans gain.
      La répulsion du magnétisme suit aussi ce plafond. **Exception : la traction du magnétisme** n'y
      est pas soumise (seul le plafond de sécurité de 50 m/s reste, D-021).
      Un saut pendant le dash le termine (vitesse de sortie). Délai entre deux débuts de dash ; un
      appui pendant un dash est mémorisé et part dès la fin du délai (un seul appui en attente).
    - **HUD** : segments sous le viseur, pleins, vides, ou en cours de recharge.

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
| Hauteur d'un saut en l'air | 1.6 | m | relevée de 1.3 à 1.6 m après le playtest de la grande scène |
| Auto-bunny hop | activé | — | |
| Plafond de sécurité de la vitesse horizontale | 50 | m/s | ne doit jamais se sentir en jeu |
| Seuil d'angle du contrôle aérien | 100 | ° | pas de demi-tour forcé (D-014) |
| Charges de mouvement | 3 | charges | réglable (améliorations) |
| Recharge d'une charge au sol / en l'air | 1.0 / 3.0 | s | [ASSUMPTION] rythme des enchaînements |
| Distance d'un dash (à l'arrêt) | 6.5 | m | |
| Durée d'un dash | 0.3 | s (18 ticks) | sans friction ni gravité |
| Profil du dash | progressif | — | à l'arrêt : pic 30.3 m/s, baisse d'≈ 1 m/s par tick jusqu'à 12 |
| Gain d'un dash au sol | +1 | m/s | plafonné ; en l'air : aucun gain |
| Fenêtre du dash-jump (après la fin du dash) | 0.15 | s (9 ticks) | timing ressenti ? |
| Gain d'un dash-jump | +3 | m/s | plafonné |
| Vitesse de sortie du dash (minimum) | 12 | m/s | |
| Délai entre deux débuts de dash | 0.3 | s | jamais avant la fin du dash précédent |
| Mémorisation de l'appui de dash | 0.3 | s | enchaînements |
| Sans friction au sol après un dash | 0.2 | s | pas de chute brutale à 7.5 m/s |
| Plafond commun des gains (wall jump, dash au sol, dash-jump, bonus de glissade, répulsion) | 20 | m/s | au-delà : vitesse conservée, pas de gain ; la traction n'y est pas soumise |
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
| Décélération de glissade (plat) | 3 | m/s² | 22 → 4 m/s en ≈ 6 s ; depuis la course ≈ 1.8 s |
| Vitesse minimale de glissade sous un obstacle | 5 | m/s | traverser un passage bas |
| Accélération en pente (part de g·sin θ) | 1.0 | × | gain en descente |
| Vitesse de sortie de glissade | 4 | m/s | |
| Braquage en glissade | 0.05 | part de l'angle / tick | [ASSUMPTION] |
| Hauteur de hitbox debout / en glissade et accroupi | 1.8 / 0.9 | m | passages bas |
| Hauteur des yeux en glissade et accroupi | 0.75 | m | [ASSUMPTION] |
| Vitesse accroupi | 3.5 | m/s | [ASSUMPTION] |
| Chute max en wall slide | 3 | m/s | |
| Wall jump : impulsion ajoutée | 7.5 | m/s | |
| Wall jump : hauteur | 1.8 | m | relevée de 1.05 à 1.8 m après playtest (→ 8.5 m/s) |
| Wall jump : direction (0 = loin du mur, 1 = vers l'avant) | 0.5 | — | |
| Wall jump : bonus de vitesse minimal | 1 | m/s | vitesse après ≥ vitesse avant + bonus |
| Wall slide : tolérance d'angle de l'entrée | 75 | ° | |
| Wall slide sans entrée à partir de | 3 | m/s le long du mur | |
| Wall run : chute ralentie de 8 à 15 m/s le long du mur | 3 → 0.5 | m/s de chute max | |
| Maintien au contact du mur | 1 | m/s (déplacement seulement) | |
| Délai après le mur pour wall jump (wall-coyote) | 0.10 | s | |
| Verrou de direction après wall jump | 0.15 | s | le joueur annule-t-il son propre élan ? |
| Deux wall jumps de suite sur le même mur | non | — | réglable (`allowSameWallJump`) |
| Recharge des sauts au contact d'un mur | oui | — | réglable (`wallRefillsJumps`) |
| Recharge des charges de mouvement au contact d'un mur | non | — | réglable (`wallRefillsCharges`) |
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
- 2026-11-11 : glissade et accroupi précisés (touche maintenue, saut prioritaire, braquage).
- 2026-11-11 : playtest de la glissade : décélération constante 3 m/s² au lieu d'une friction
  proportionnelle, élan conservé à l'entrée, vitesse minimale de 5 m/s sous un obstacle (D-013).
- 2026-11-11 : wall slide et wall jump implémentés ; recharge des sauts au mur.
- 2026-11-12 : seuil d'angle de 100° pour le contrôle aérien (D-014) ; charges de mouvement
  partagées par le saut en l'air, le dash et le magnétisme, 3 par défaut (D-015).
- 2026-11-12 : playtest des murs : wall jump plus haut (1.8 m), vers l'avant, sans perte ; wall
  slide sans perte, détecté en longeant le mur, tolérance d'angle, effet wall run ; charges non
  rechargées au mur (D-016). Dash intégré.
- 2026-11-12 : playtest du dash : dash v2 (durée 0.18 s, 6.5 m, sortie ≥ 12 m/s, buffer 0.3 s, grâce
  de friction 0.2 s), HUD des charges ; gain du wall jump plafonné à 20 m/s (D-017).
- 2026-11-12 : dash v3 : profil progressif sur 0.3 s, +1 m/s au sol, aucun gain en l'air, dash-jump
  (+3 m/s, fenêtre 0.15 s), plafond commun des gains à 20 m/s (D-018).
- 2026-11-12 : bonus de glissade soumis au plafond commun (D-019). Mouvement de base validé en playtest.
- 2026-11-12 : le magnétisme quitte les charges ; état Traction ; exception au plafond commun pour la traction (D-021).
