# Orbes

**Status:** in progress · **Updated:** 2026-11-12 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

**Une seule orbe** (D-024), qui réunit deux pouvoirs : le magnétisme déplace le joueur (et bouscule les
ennemis) ; la télékinésie transforme l'arène en munitions. Les quatre actions sont disponibles **en même
temps**, chacune sur sa touche : on peut se tracter en tenant une caisse, puis la lancer en arrivant. De
base, l'orbe ne fait pas de dégâts directs ; des améliorations peuvent en ajouter, en plus de
l'environnement (D-008). Elle collecte aussi les âmes autour d'elle. Piliers : « l'arène est
l'arme », « le mouvement est la survie ».

## Règles

1. **Commandes (D-024) :**

   | Action | Clavier (AZERTY) | Manette |
   |---|---|---|
   | Traction (maintenir) | A (touche Q d'un QWERTY) | RB |
   | Répulsion | clic droit | LT |
   | Attraper l'objet visé / le relâcher (posé doucement) | F | X |
   | Lancer l'objet tenu (sans objet : artefact, plus tard) | clic gauche | RT |

   Mouvement inchangé : saut espace / A, glissade Ctrl ou C / B, dash Maj / LB. Épaules = mobilité (LB
   dash, RB traction), gâchettes = combat (RT lancer et artefact, LT répulsion), X = interagir ; Y reste
   libre. Plus d'orbe active ni d'échange : la molette, Y et les touches 1 et 2 ne font plus rien.
2. **États :** Prête → EnUsage → Recharge → Prête, par action. Coût : le **magnétisme** n'utilise
   **aucune charge de mouvement** (D-021) : l'attraction a ses propres **charges d'attraction**, la
   répulsion une **charge unique** ; la **télékinésie** n'a **pas d'endurance** (D-024), seulement un
   court délai après un lancer. Usage refusé (son et HUD) : sans charge, en recharge, rien à portée, objet
   trop lourd. **HUD** (sous les charges de mouvement) : les charges d'attraction en bleu, la répulsion en
   orange (pleine, ou en recharge avec le temps restant). Pas de barre pour la télékinésie.
3. **Attraction (magnétisme, mode 1) : traction linéaire continue.** Tant que la touche est tenue (A ou
   RB), le joueur est tiré **en ligne droite** vers le point visé au moment du clic, sur
   **n'importe quelle surface** (murs, sol, plafond ; jamais les ennemis). Pas de câble ni de
   balancement, **ni gravité ni friction** pendant la traction. La vitesse monte avec une forte
   accélération jusqu'à la vitesse de traction ; elle **ne baisse jamais** : plus vite que la vitesse de
   traction, on garde toute sa vitesse, redirigée vers le point. Quand le point est plus haut, la chute
   n'est pas comptée. **Pas de plafond des gains** (exception au plafond commun ; seul le plafond de
   sécurité de 50 m/s reste, D-021). Fin de la traction, avec toute la vitesse et la direction du
   moment : **relâchement**, **arrivée** au point, **ligne de vue coupée**, **durée max**, **saut**
   (le saut part aussitôt), **dash**, **répulsion**, ou obstacle qui bloque. **Chaque traction coûte
   1 charge d'attraction** (3 charges, séparées des charges de mouvement) ; elles reviennent une par
   une, **1 toutes les 4 s** (D-024). Pas d'autre délai entre deux tractions. Tenir un objet ne gêne pas
   la traction : la visée ignore l'objet tenu.
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
5. **Attraper (télékinésie, violet) :** F / X attrape l'objet visé (à portée, sous la
   masse maximale) : il **vole jusqu'au joueur** (vitesse d'approche, il ralentit en arrivant), puis
   **reste tenu sans maintenir** la touche et flotte devant le joueur, un peu sous
   la visée ; il bute sur les murs au lieu de les traverser et ne touche jamais son porteur. **F de
   nouveau le relâche** : il est posé doucement. Un objet coincé loin du point de maintien est
   lâché. Plus tard : un petit ennemi, un projectile ennemi ; la goule seulement **étourdie**.
6. **Lancer (télékinésie, vert) :** clic gauche / RT : l'objet part vers ce que vise le viseur,
   à la vitesse de lancer **plus la vitesse du joueur** (lancer en pleine course frappe plus fort,
   D-023). Ses dégâts dépendent de sa masse et de sa vitesse d'impact (partie 2). Après un lancer, une
   courte recharge (0.3 s) avant d'attraper de nouveau ; c'est la seule limite (pas d'endurance). Sans objet
   en main, le clic gauche ne fait rien : il est réservé à l'artefact (étape 5 bis). Un projectile ennemi renvoyé blesse son tireur (plus tard).
   **Retour (greybox) :** objets attrapables qui brillent (faiblement de loin, plus à portée, vivement
   quand ils sont visés, rouge terne s'ils sont trop lourds, violet tenus, vert une fois lancés) ; trait
   violet vers l'objet tenu ; au viseur, un troisième anneau, à l'extérieur des anneaux bleu et orange :
   violet (objet visé attrapable) ou vert (objet tenu, lancer possible), rouge un court instant au refus.
7. **Couleur de l'orbe selon l'action (D-024) :** bleu = traction, orange = répulsion, violet = objet
   tenu, vert = lancer ; neutre quand elle est prête, terne et plus petite sans charge d'attraction.
   Quand des actions se superposent : **un éclat bref (lancer, répulsion : 0.15 s) l'emporte sur un état
   qui dure, et la traction l'emporte sur l'objet tenu.** Ordre : lancer > répulsion > traction > objet
   tenu > recharge > prête. Exemple : traction en tenant une caisse → orbe bleue (ce qui te déplace), la
   caisse et son trait restent violets ; lancer en arrivant → éclair vert, puis bleu ou neutre. Écarté :
   un mélange bleu-violet (illisible à pleine vitesse) ou une orbe coupée en deux couleurs.
8. **Âmes :** l'orbe aspire automatiquement les âmes dans un rayon autour d'elle.
9. **Dégâts par amélioration :** une amélioration peut donner des dégâts directs à l'orbe
   (par exemple l'onde de répulsion) ou un bonus aux objets lancés. Ces dégâts s'ajoutent à
   ceux de l'environnement, sans les remplacer.
10. **Retour du magnétisme (greybox) :**
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

ScriptableObjects `MagnetismOrbSettings`, `TelekinesisOrbSettings`, `MagnetismFeedbackSettings` et
`TelekinesisFeedbackSettings` (couleurs, éclat de l'orbe). Les charges de mouvement sont dans `MovementSettings`. Valeurs de départ [ASSUMPTION].

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Portée de l'attraction | 60 | m | plafond d'une grande salle depuis le sol |
| Accélération / vitesse de traction | 80 / 30 | m/s² / m/s | 30 m/s atteints en 0.38 s |
| Durée max d'une traction | 2.5 | s | |
| Arrivée au point | 1 | m | |
| Charges d'attraction / recharge d'une charge | 3 / 4 | charges / s | une par une ; séparées des charges de mouvement |
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
| Portée de la télékinésie | 60 | m | à peu près celle de l'attraction |
| Masse maximale attrapable | 40 | kg | quels ennemis sont « petits » |
| Vitesse de lancer | 30 | m/s | |
| Recharge télékinésie (après un lancer, avant d'attraper) | 0.3 | s | seule limite de la télékinésie |
| Éclat de couleur de l'orbe (lancer, répulsion) | 0.15 | s | lisible sans masquer la traction |
| Point de maintien : distance / sous la visée | 2.5 / 0.35 | m | l'objet tenu cache-t-il trop la vue ? |
| Rattrapage du point de maintien : gain / vitesse d'approche | 18 / 40 | /s / m/s | un objet à 55 m arrive en ≈ 1.35 s |
| Objet lâché s'il reste coincé à plus de / pendant | 4 / 0.3 | m / s | |
| Poser : vitesse max de l'objet | 4 | m/s | |
| Le lancer ajoute la vitesse du joueur | oui | — | mesuré : 43.5 m/s lancé à 15 m/s |
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
- Traction ou répulsion en tenant un objet : le rayon de visée ignore l'objet tenu (sinon on se
  tracterait vers sa propre caisse, ou on ferait un rocket jump gratuit dessus).
- Objet tenu à grande vitesse : il garde sa place devant le joueur (mesuré : 0.00 m d'écart en traction
  à 30 m/s, 0.05 m en bunny hop à 20 m/s en virage) ; contre un mur, il bute.
- Lancer pendant une traction : la traction continue ; l'objet part avec la vitesse du joueur en plus.
- Mort ou téléportation en tenant un objet : l'objet tombe avec sa vitesse actuelle.

## Questions ouvertes

- [ASSUMPTION] L'objet tenu ne bloque pas les projectiles (pas de bouclier).
- Toutes les actions en même temps : faut-il limiter les combos (par exemple, un lancer pendant une
  traction) ? Pour l'instant non ; à juger en playtest.
- Rester en l'air : 3 tractions de suite, puis 1 toutes les 4 s, et 1 répulsion toutes les 5 s. À
  revoir en playtest, notamment face au traqueur.
- Une traction vers le bas plus rapide que 35 m/s est ramenée à la chute max dès qu'elle s'arrête.
- L'onde de répulsion (ennemis bousculés) attend les ennemis (étape 6).
- Lustre : le faire tomber **à distance** (traction sèche, objet lancé dessus, ou chaîne visée) pour
  l'utiliser contre les ennemis et le boss final ; aujourd'hui, il ne se décroche qu'en l'attrapant.

## History

- 2026-11-11 : designed.
- 2026-11-11 : dégâts directs possibles via les améliorations (D-008).
- 2026-11-12 : le magnétisme consomme les charges de mouvement ; l'endurance ne sert plus qu'à la
  télékinésie (D-015).
- 2026-11-12 : in progress. Magnétisme partie 1 (attraction, répulsion, charges, recharge) : annulation de
  la vitesse opposée, plafond commun, surfaces magnétiques seulement, deux boutons (D-020).
- 2026-11-12 : après playtest : traction continue, toute surface, sans charges, recharges propres,
  aucune perte d'élan, orbe visible (D-021).
- 2026-11-12 : télékinésie et échange, partie 1 (D-023) : attraper, tenir, poser, lancer, endurance, objets
  qui brillent, HUD.
- 2026-11-12 : après playtest : portée 60 m, vitesse d'approche 40 m/s.
- 2026-11-12 : retour visuel et sonore : lueur, viseur à deux anneaux, éclats, refus en rouge, sons synthétisés
  (choix 1a 2a 3a ; réglages dans `MagnetismFeedbackSettings`).
- 2026-11-12 : après le playtest v2 : 3 charges d'attraction (1 toutes les 3 s), répulsion unique
  (40 m, 5 s), marquage de la répulsion, HUD des charges d'attraction et de la répulsion (D-021 mise à jour).
- 2026-11-12 : après le playtest de la télékinésie : **une seule orbe** (D-024) : plus d'échange, quatre actions
  simultanées sur des touches séparées (A, clic droit, F, clic gauche ; RB, LT, X, RT), couleur de l'orbe selon
  l'action, plus d'endurance, attraction toutes les 4 s ; l'objet tenu ne prend plus un tick d'avance à grande
  vitesse.
