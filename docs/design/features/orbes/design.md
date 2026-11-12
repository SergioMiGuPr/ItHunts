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
   point d'impact. **Onde (D-029), façon aimant qui repousse** : un **cône depuis le joueur**, dans la direction visée
   (**14 m**, ouverture **60°**), qu'un mur ou le décor arrêtent. **À l'appui** : une onde de choc repousse
   loin du joueur (et un peu vers le haut) les objets déplaçables et les ennemis : **22 m/s jusqu'à 2 m**
   pour un objet de 15 kg ou un ennemi de 80 kg (de ×0.35 à ×1.3 selon la masse), **×0.6 au bout de la
   portée** ; c'est le **seul** moment où le joueur est propulsé, et la charge est consommée. **Bouton
   maintenu** : la répulsion continue **au plus 0.8 s** et pousse en continu tout ce qui est dans le cône,
   **35 m/s² dans l'axe** (×0.5 au bout), sans jamais dépasser **25 m/s** dans l'axe et sans nouvelle
   propulsion ; elle s'arrête au relâchement, à 0.8 s, ou si l'on fait une autre action de l'orbe (traction,
   prise, lancer). La **recharge de 5 s part de la fin** de la répulsion. Le lustre se décroche (on le vise
   par en dessous). Retour : onde marquée à l'appui, anneaux continus dans le cône pendant le maintien,
   orbe orange, paume tendue, temps de maintien restant près du viseur. Jamais l'objet tenu, un ennemi
   tenu, les joueurs ni le traqueur. **Objet repoussé (D-031)** : il blesse ce qu'il percute comme un objet lancé,
   **×0.5**, sans dégâts de zone, et ne perd un lancer de durabilité que s'il blesse un ennemi ; dangereux **1 s** après
   la fin de la poussée ou jusqu'à passer **sous 6 m/s** ; jamais contre un joueur.
   tenu, les joueurs ni le traqueur. **Étourdissement** : un ennemi repoussé qui percute un mur
   ou un obstacle solide à **6 m/s** ou plus est **étourdi 1.5 s** : il ne bouge plus (le factice mobile
   s'arrête), trois étoiles tournent au-dessus de sa tête, il luit en jaune, gazouillis. Un ennemi
   étourdi pourra être attrapé par la télékinésie (prévu pour la goule : étape des ennemis). Plus tard :
   des dégâts contre le mur (D-031) : **5 PV par m/s au-delà de 6 m/s, 40 au plus par impact**, en même temps que
   l'étourdissement (donc une fois par impact, jamais pendant l'immunité ; un ennemi lancé garde ses propres dégâts
   d'impact). Seul l'impact compte : un ennemi
   maintenu plaqué contre un mur n'est ni ré-étourdi ni blessé en continu. **Sans effet sur le boss** (traqueur exclu de l'onde, de
   l'étourdissement, des dégâts de zone et de la prise). Après un étourdissement, **2 s d'immunité**.
5. **Attraper (télékinésie, violet) :** F / X attrape l'objet sélectionné (à portée, sous la
   masse maximale). **Aide à la visée (D-026)** : un objet est sélectionné s'il est vu dans un **cône** autour du
   viseur, de **8°** (demi-angle) jusqu'à 5 m, puis de plus en plus étroit jusqu'à **3° à 60 m** ; parmi plusieurs
   objets dans le cône, le plus proche **du viseur** (pas du joueur) ; jamais à travers un mur, ni derrière, ni loin
   sur le côté ; un objet trop lourd n'est sélectionné qu'au rayon exact (pour le montrer en rouge). L'objet
   sélectionné brille vivement en pulsant, avec l'anneau violet au viseur, avant l'appui. Une fois attrapé, il **vole jusqu'au joueur** (vitesse d'approche, il ralentit en arrivant), puis
   **reste tenu sans maintenir** la touche et flotte devant le joueur, un peu sous
   la visée ; il bute sur les murs au lieu de les traverser et ne touche jamais son porteur. **F de
   nouveau le relâche** : il est posé doucement. Un objet coincé loin du point de maintien est
   lâché. Plus tard : un petit ennemi, un projectile ennemi ; la goule seulement **étourdie**.
6. **Lancer (télékinésie, vert) :** clic gauche / RT : l'objet part vers ce que vise le viseur,
   à la vitesse de lancer, **toujours exactement vers le point sous le viseur** au moment du clic (D-027) :
   rayon depuis les yeux au début du tick, direction depuis la position réelle de l'objet. La vitesse du
   joueur ne change jamais la direction : seule sa part dans la direction du lancer s'ajoute, si elle est
   positive (lancer en courant vers la cible frappe plus fort, D-023). **Tir tendu (D-025)** : pas de gravité pendant 0.5 s (≈ 15 m à l'arrêt, 20 m en course) ou jusqu'au
   premier choc, puis gravité normale.
   **Dégâts (D-025)** : tout impact d'un objet attrapable sur un ennemi blesse au-dessus d'un seuil : dégâts =
   **masse × (vitesse d'impact − 8 m/s) × 0.1** (vitesse relative au contact). Ça vaut pour un objet lancé, un
   rebond, une chute et un objet tenu qu'on percute. **Chute** (impact surtout vertical, objet au-dessus de la
   cible) : seuil de **3 m/s**. Un même objet ne blesse la même cible qu'une fois toutes les 0.3 s. Les objets ne
   blessent jamais les joueurs. Retour : chiffre de dégâts flottant (blanc, jaune au-dessus de 25, rouge au-dessus
   de 60, « K.O. » à la mort), barre de PV au-dessus de la cible, éclat au point d'impact, éclair blanc sur la
   cible, son 3D de coup (plus grave pour un objet lourd) et de mort.
   **Zone (D-027)** : au premier impact d'un objet lancé (au-dessus du seuil), les ennemis autour prennent des
   dégâts de zone : rayon **1 m + 0.06 m par kg** (5 kg : 1.3 m, 25 kg : 2.5 m, 40 kg : 3.4 m), **60 %** des dégâts
   d'impact au centre, **25 %** de ça au bord, jamais à travers un mur ; l'ennemi touché de plein fouet ne les prend
   pas en plus. Cercle orange bref au sol. **Durabilité** : un objet tient **1 lancer jusqu'à 5 kg, 2 jusqu'à 15 kg,
   3 jusqu'à 25 kg, 5 au-delà** (réglable par objet) ; un lancer compte à son premier impact au-dessus du seuil ; le
   dernier le casse : éclat, débris, son, puis il disparaît (dans la scène de test, il revient à sa place après 4 s).
   Repère : une rangée de carrés au-dessus de l'objet tenu (pleins : lancers restants). **Lustre** : il se casse au
   premier impact, lancé ou tombé, avec une zone de **4 m**.
   **Ennemi étourdi (D-030)** : F / X peut attraper un ennemi **étourdi** (et lui seul ; l'aide à la visée en
   cône l'inclut). Tenu, c'est un objet de sa masse (80 kg pour le factice, sans limite de masse ni
   durabilité) : même main levée, même lancer, mêmes dégâts et zone. Il se libère seul au bout de **3 s**
   (il se débat et clignote 0.5 s avant) ; ensuite, 2 s d'immunité : ni ré-étourdi, ni repris. Lancé, il
   subit aussi les dégâts de chaque impact, mur compris (mesuré : lancé à 30 m/s sur un factice à 11 m,
   les deux meurent ; contre un mur, il meurt : 80 kg × 22 × 0.1 = 176 dégâts). Un seul joueur le tient ;
   il ne blesse pas son lanceur. Après un lancer, une
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
| Tir tendu : sans gravité après un lancer | 0.5 | s | au-delà de 15 m, l'objet retombe (1.8 m à 30 m) |
| Gravité des objets | 20 | m/s² | comme le joueur (D-027) |
| Zone : rayon de base / par kg | 1 / 0.06 | m / m par kg | |
| Zone : dégâts au centre / au bord | 60 / 25 | % des dégâts d'impact / % du centre | |
| Durabilité : jusqu'à 5 / 15 / 25 kg / au-delà | 1 / 2 / 3 / 5 | lancers | |
| Lustre : zone | 4 | m | casse au premier impact |
| Répulsion (cône) : portée / ouverture | 14 / 60 | m / ° | |
| Onde de choc (appui) : vitesse jusqu'à 2 m (objet de 15 kg / ennemi de 80 kg), bout de portée, facteur de masse | 22 / 22, ×0.6, 0.35 à 1.3 | m/s | assez fort ? |
| Maintien : durée max / poussée dans l'axe / bout de portée / vitesse max | 0.8 / 35 / ×0.5 / 25 | s / m/s² / — / m/s | assez de contrôle ? |
| Recharge de la répulsion | 5, à partir de la fin du maintien | s | |
| Dégâts contre un mur (ennemi repoussé) : par m/s au-delà de 6 / plafond | 5 / 40 | PV | |
| Objet repoussé : multiplicateur / danger / vitesse mini | 0.5 / 1 / 6 | — / s / m/s | |
| Ennemi étourdi tenu : libération seule / avertissement / débattement | 3 / 0.5 / 0.06 | s / s / m | |
| Immunité après un étourdissement | 2 | s | |
| Étourdissement : vitesse d'impact / durée | 6 / 1.5 | m/s / s | |
| Ennemi factice : masse / retour au poste | 80 / 2 s puis 3 m/s | kg | |
| Aide à la visée : cône de près / jusqu'à / à 60 m | 8 / 5 / 3 | ° / m / ° | mesuré : écart toléré 1.8 m à 10 m, 3.5 m à 30 m, 3.6 m à 60 m |
| Dégâts d'impact : seuil / seuil de chute / facteur | 8 / 3 / 0.1 | m/s / m/s / par kg et m/s | caisse de 25 kg : 55 à l'arrêt, ≈ 80 en course |
| Intervalle entre deux coups d'un objet sur une cible | 0.3 | s | |
| Ennemi factice : PV / réapparition / vitesse du mobile | 100 / 3 / 6 | PV / s / m/s | |
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
- Traction ou répulsion visée sur un ennemi : refusée (l'ennemi arrête le rayon sans offrir de surface).
- Objet tenu qu'on fait percuter un ennemi (traction, course) : il blesse comme un objet lancé.
- Les objets tombent à 20 m/s², comme le joueur (D-027).
- Objet gros visé au ras du sol : son centre va vers le point visé, donc il touche le sol un peu avant.
- Onde vers un lustre : il est décroché et tombe droit ; on le vise par en dessous (dans la scène de test, à
  ≈ 5.9 m des yeux depuis 2 m à côté du factice placé dessous : 30 dégâts de chute).
- Un ennemi au repos ne blesse pas ce qui le percute : seul un ennemi tenu ou en vol est un projectile.
- Mort ou téléportation en tenant un objet : l'objet tombe avec sa vitesse actuelle.

## Questions ouvertes

- [ASSUMPTION] L'objet tenu ne bloque pas les projectiles (pas de bouclier).
- Objet tenu qui percute un ennemi : faut-il le garder (bélier en traction) ? À juger en playtest.
- Tir tendu de 0.5 s : avec la gravité à 20 m/s², l'objet retombe de 1.8 m à 30 m. Passer à 1 s (droit jusqu'à
  30 m à l'arrêt) ?
- Dégâts quand un ennemi repoussé percute un mur (prévus au design, pas encore faits).
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
- 2026-11-12 : partie 2 : dégâts d'impact (masse × vitesse au-dessus du seuil), tir tendu, seuil de chute, ennemis
  factices, chiffres de dégâts, éclats et sons (D-025) ; aide à la visée de la prise en cône (D-026).
- 2026-11-12 : après le playtest de la partie 2 (D-027) : lancer toujours vers le viseur, gravité des objets à
  20 m/s², dégâts de zone, durabilité, lustre fragile, onde de répulsion, étourdissement.
- 2026-11-12 : onde de répulsion en cône depuis le joueur (D-029) ; prise des ennemis étourdis (D-030).
- 2026-11-12 : répulsion façon aimant (D-029 mise à jour) : onde de choc à l'appui, poussée continue au maintien (0.8 s), cône de 14 m, recharge à partir de la fin.
- 2026-11-12 : dégâts contre un mur d'un ennemi repoussé et objets repoussés qui blessent (D-031).
