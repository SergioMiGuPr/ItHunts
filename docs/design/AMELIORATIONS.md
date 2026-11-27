# Améliorations : la liste de référence (70)

**Statut :** conçue le 2026-11-22 (nuit 4, point 0), pour la DA (icônes, cadres de rang) et le code (ScriptableObjects, point 2).
Remplace la liste de `features/ameliorations/design.md` (8 de base + 4 lanterne + 5 barillet + 8 caméléon), qui est reprise
ici sous une forme à rangs. Inspirations : Risk of Rain 2 (effets qui se cumulent et se déclenchent les uns les autres),
Ravenswatch (talents à rangs, un passif « d'ultime » à la fois, peu d'emplacements), Hades (raretés et montée de rang d'un don
possédé). Chaque amélioration **change la façon de jouer**, pas seulement un chiffre.

## Règles communes

| Règle | Valeur | Note |
|---|---|---|
| **Rangs** | Bronze → Argent → Or → Diamant → Mythique | une amélioration nouvelle arrive toujours en **Bronze** ; elle **monte d'un rang** au marchand (« montée de niveau ») ou dans un coffre qui la propose de nouveau |
| **Cadre** | couleur du rang | cuivre, argent, or, bleu-blanc (diamant), violet-rose (mythique) : à fournir par la DA (HUD, marchand, écran de fin) |
| **Emplacements** | **10 par run**, dont **au plus 1 passif** | passé 10, le marchand et les coffres ne proposent plus que des montées de rang |
| **Passif** | 2 par personnage, **un seul porté à la fois** | en prendre un second remplace le premier (le rang est perdu), l'offre le dit clairement |
| **Qui voit quoi** | les 30 génériques pour tous ; les 10 d'un personnage pour son porteur seulement | coop : chacun voit son offre |
| **Marchand** | **1 amélioration nouvelle** (passif ou non) **+ 1 montée de rang** d'une amélioration possédée **+ le soin** | la montée proposée est tirée parmi les possédées non Mythiques ; sans rien à monter, 2 nouvelles |
| **Coffre** | choix parmi 3 : au moins 1 nouvelle ; une montée de rang possible dès l'acte II | gratuit après le prix du coffre |
| **Prix de référence** (à régler au bot, point 2) | nouvelle **60** ; montée : Argent **90**, Or **140**, Diamant **220**, Mythique **350** | âmes réellement gagnées par acte : à mesurer ; cible : tout le monde monte 5 à 7 améliorations haut, personne ne met tout en Mythique |
| **Effets de rang** | Bronze = base ; Argent ≈ ×1.5 ; Or ≈ ×2 et souvent un **petit ajout** ; Diamant ≈ ×2.75 ; **Mythique = ×3.5 et un effet nouveau** (en gras) | le Mythique doit se voir à l'écran |
| **Traqueur** | aucun effet ne le blesse, ne l'étourdit, ne le repousse ni ne le ralentit (il reste intuable, D-035) ; seules les améliorations qui le disent agissent sur sa **détection** | sauf dans l'arène du boss, où il est blessable (D-098) |
| **Plafonds** | la traction reste plafonnée à 18 m/s (D-036) ; les gains de mouvement au plafond commun (20) | les améliorations de mouvement jouent sur l'accélération, les charges, la conservation, jamais au-delà des plafonds |
| **États** | **étourdi** (parade, répulsion contre un mur, atterrissage fort), **paralysé** (balles, Mythiques), **en feu** (lanterne, braises), **saignant** (épée), **lié** (livre) | les synergies se lisent sur ces 5 états |

Les chiffres sont des **valeurs de départ** : tout est dans des ScriptableObjects réglables en Play Mode.

---

## Génériques (30) : pour tous les personnages

### Parade (4)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G1 | **Riposte** | une parade réussie renvoie une onde qui blesse les ennemis autour | 15 PV, 3 m | 22 PV, 3.5 m | 30 PV, 4 m, repousse | 40 PV, 4.5 m | 55 PV, 5 m, **l'onde étourdit 1.5 s** | Détonateur (G20), Briseur d'os (G21) |
| G2 | **Garde large** | fenêtre de parade plus longue | +40 ms | +60 ms | +80 ms, la tolérance tardive double | +100 ms | +120 ms, **pare aussi les débris et les projectiles (boss compris)** | Rythme de fer (G4) |
| G3 | **Second souffle** | une parade réussie rend des PV | 6 PV | 9 PV | 12 PV + une charge de mouvement | 16 PV | 22 PV, **+ une charge de traction** | Charge en plus (G23), Traction rapide (G10) |
| G4 | **Rythme de fer** | chaque parade réussie d'affilée (6 s) augmente vos dégâts | +10 % par parade, max 3 | +15 %, max 3 | +20 %, max 4 | +25 %, max 4 | +30 %, max 5, **à 3 parades : le temps ralentit 1 s autour de vous** (pas en coop en ligne : effet local) | Prédateur (G22) |

### Orbe et âmes (5)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G5 | **Grande orbe** | rayon de collecte des âmes | +25 % (10 m) | +40 % | +60 % | +85 % | +120 % (17.6 m), **collecte à travers les murs** | Aspiration (G6), Flair (R-P2) |
| G6 | **Aspiration** | les âmes viennent plus vite, les coffres offrent leur choix plus longtemps | +50 % | +80 % | +110 %, les âmes au sol durent 45 s | +150 % | +200 %, **chaque âme ramassée soigne 1 PV** | Grande orbe (G5), Âmes gourmandes (G7) |
| G7 | **Âmes gourmandes** | plus d'âmes par ennemi tué | +10 % | +18 % | +25 %, +1 âme par kill à plus de 14 m/s | +35 % | +50 %, **kill à plus de 14 m/s : âmes doublées** | Vif-argent (G26), Semelles de vent (G24) |
| G8 | **Marchandage** | prix du marchand réduits | −8 % | −14 % | −20 %, la hausse par achat est divisée par 2 | −26 % | −33 %, **la première montée de rang de chaque acte est gratuite** | Œil du collectionneur (G30) |
| G9 | **Orbe vorace** | chaque âme ramassée réduit vos recharges (traction, artefact, dash) | 0.05 s par âme | 0.08 s | 0.11 s, aussi la recharge de la répulsion | 0.15 s | 0.2 s, **un kill rend une charge de traction** | Traction rapide (G10), Main leste (C5) |

### Traction (4)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G10 | **Traction rapide** | une charge de traction revient plus vite | −1 s (5 s) | −1.6 s | −2.2 s | −2.8 s | −3.4 s, **l'arrivée d'une traction rend la charge si vous atterrissez à plus de 12 m/s** | Second souffle (G3), Orbe vorace (G9) |
| G11 | **Harpon** | la traction vise aussi les **ennemis** : vous êtes tiré vers eux | goules | goules et volants | l'arrivée frappe 20 PV | 35 PV et étourdit 1 s | 50 PV, **l'arrivée explose (3 m)** | Briseur d'os (G21), Détonateur (G20), Fente (K3) |
| G12 | **Élan de traction** | la traction accélère plus vite et garde plus d'élan à l'arrivée (plafond 18 inchangé) | accélération +25 % | +50 % | +75 %, l'élan de sortie est gardé 1 s de plus | +100 % | +125 %, **4 s de tractions sans charge après une parade réussie** | Garde large (G2), Vif-argent (G26) |
| G13 | **Sillage** | pendant une traction, vous laissez une traînée qui blesse et ralentit les ennemis traversés | 8 PV, −20 % 2 s | 12 PV | 16 PV, −35 % | 22 PV | 30 PV, **la traînée enflamme** | Allumette (G28), Fourrure de braise (R-P1) |

### Répulsion (4)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G14 | **Onde blessante** | l'onde de répulsion blesse les ennemis touchés | 10 PV | 15 PV | 20 PV, ralentit 1.5 s | 28 PV | 38 PV, **double onde (une seconde 0.3 s après)** | Rebond (G16), Onde de choc (G15) |
| G15 | **Onde de choc** | répulsion plus large et plus longue | +10°, +2 m | +15°, +3 m | +20°, +4 m, les objets repoussés vont 20 % plus vite | +25°, +5 m | +30°, +6 m, **renvoie les débris et projectiles du boss** | Projectiles lourds (G18) |
| G16 | **Rebond** | un ennemi repoussé contre un mur ou un autre ennemi prend des dégâts d'impact | ×1.5 | ×1.8 | ×2.1, étourdit 1 s | ×2.5 | ×3, **l'ennemi projeté qui meurt explose (3.5 m)** | Détonateur (G20), Briseur d'os (G21) |
| G17 | **Charge de répulsion** | la répulsion a une charge en plus ; sa recharge est plus courte | +1 charge | recharge −1 s | −1.5 s | +2 charges | **la répulsion sur le sol vous propulse en hauteur (saut de 4 m) sans dépenser de charge de mouvement** | Pas de chat (G25), Chute amortie (G27) |

### Dégâts et combat (5)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G18 | **Projectiles lourds** | objets lancés ou repoussés : plus de dégâts (zone comprise) | +25 % | +40 % | +60 %, les objets ne se brisent plus au premier impact | +85 % | +120 %, **un objet lancé revient vers l'orbe après l'impact** | Onde de choc (G15), Langue de caméléon (C8) |
| G19 | **Cœur solide** | PV max | +25 (et autant tout de suite) | +40 | +55, régénération 1 PV / 4 s hors combat | +75 | +100, **une fois par run, vous revenez à 50 % de PV au lieu de mourir** (pas face à la prise du traqueur) | Second souffle (G3), Peau épaisse (K-P1) |
| G20 | **Détonateur** | un ennemi **étourdi ou paralysé** qui meurt explose | 20 PV, 3 m | 28 PV, 3.5 m | 36 PV, 4 m, l'explosion étourdit 0.8 s | 46 PV, 4.5 m | 60 PV, 5 m, **chaîne : les morts par explosion explosent (3 maillons)** | Riposte (G1), Rebond (G16), Balles paralysantes (C1) |
| G21 | **Briseur d'os** | dégâts contre les ennemis étourdis ou paralysés | ×1.25 | ×1.45 | ×1.65, l'étourdissement dure 0.5 s de plus | ×1.9 | ×2.2, **un étourdi sous 30 % de PV meurt du coup suivant** | Détonateur (G20), Harpon (G11), Brise-garde (K7) |
| G22 | **Prédateur** | chaque kill : +dégâts pendant 5 s (piles) | +6 %, max 5 | +8 %, max 5 | +10 %, max 6, + vitesse de course +0.5 m/s | +12 %, max 6 | +15 %, max 8, **à 5 piles : vos attaques étourdissent** | Rythme de fer (G4), Élan du bûcheron (K8) |

### Mouvement (5)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G23 | **Charge en plus** | charges de mouvement (dash, saut en l'air) | +1 | recharge au sol −0.5 s | +2 | recharge en l'air −1 s | +3, **2 s de dashs gratuits après un kill** | Second souffle (G3), Élan du bûcheron (K8) |
| G24 | **Semelles de vent** | course plus rapide, élan au sol gardé plus longtemps | +0.5 m/s | +0.8 m/s | +1.1 m/s, perte d'élan 30 % plus lente | +1.4 m/s | +1.7 m/s, **plafond de l'élan au sol +2 m/s** | Âmes gourmandes (G7), Vif-argent (G26) |
| G25 | **Pas de chat** | glissade plus longue, wall slide plus lent, wall jump plus fort | glissade −20 % de freinage | wall jump +1 m/s | wall slide 2× plus lent, hissage +0.5 m | wall jump +2 m/s | **wall jump sur le même mur autorisé**, les murs rendent aussi une charge | Griffes grimpantes (K-P2), Plumes (H-P2) |
| G26 | **Vif-argent** | bunny hop : gain par saut et contrôle en l'air | +15 % | +25 % | +35 %, contrôle en l'air +20 % | +50 % | +60 %, **au-delà de 16 m/s vous traversez les goules en les étourdissant** | Âmes gourmandes (G7), Briseur d'os (G21) |
| G27 | **Chute amortie** | chute dans le vide moins punitive, atterrissage fort qui frappe autour | −20 → −12 PV | −8 PV, l'atterrissage fort étourdit à 2 m | −4 PV, 15 PV de dégâts à 3 m | 0 PV | **l'atterrissage fort explose (30 PV, 4 m) et vous relance d'un petit saut** | Charge de répulsion (G17), Détonateur (G20) |

### Synergies et utilitaires (3)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| G28 | **Allumette** (D-125 : proposée seulement au renard, seule source de feu) | un ennemi **en feu** qui meurt enflamme ses voisins | 2 m, 3 s | 2.5 m, 4 s | 3 m, 5 s, le feu ralentit 20 % | 3.5 m, 6 s | 4 m, 7 s, **le feu étourdit 0.5 s à la première brûlure** | Sillage (G13), lanterne (R1 à R8), Feu grégeois (R5) |
| G29 | **Souffle court** | le traqueur vous détecte de moins loin et perd votre trace plus vite (sa vitesse ne change pas) | −10 % | −16 % | −22 %, les goules alertent le traqueur 1 s plus tard | −28 % | −33 %, **2 s d'invisibilité pour le traqueur après une parade réussie** | Peau changeante (C-P1), Vision nocturne (H-P1) |
| G30 | **Œil du collectionneur** | les coffres offrent un choix en plus ; les salles verrouillées sont moins chères | +1 choix | porte −25 % | +1 choix, les coffres proposent une montée de rang dès l'acte I | porte −50 % | +2 choix, **un coffre peut offrir une amélioration directement en Argent** | Marchandage (G8) |

---

## Renard : lanterne de feu (10)

Identité : **feu, contrôle de zone, pression continue**. La lanterne brûle (4 s à 12 PV/s) ; le renard veut enflammer, étaler, entretenir.

### Passifs (2, un seul porté)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique |
|---|---|---|---|---|---|---|---|
| R-P1 | **Fourrure de braise** | insensible au feu ; un ennemi qui vous frappe s'enflamme | brûlure 2 s | 3 s | 4 s, le coup reçu est réduit de 10 % | 5 s | 6 s, **l'ennemi est repoussé de 4 m et étourdi 0.8 s** |
| R-P2 | **Flair** | coffres, marchand et âmes brillent à travers les murs ; rayon de collecte | 20 m, +10 % | 30 m, +15 % | 40 m, +20 %, la sortie d'acte est marquée dès l'entrée | 50 m, +25 % | partout, +30 %, **une goule qui porte des âmes à moins de 20 m les lâche d'avance (1 âme) quand elle vous voit** |

### Améliorations de la lanterne (8)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| R1 | **Long souffle** | portée du jet | +1.5 m | +2.5 m | +3.5 m, le jet traverse un objet | +4.5 m | +6 m, **le jet rebondit sur le décor (une fois)** | Flamme large (R2) |
| R2 | **Flamme large** | cône du jet | +15° | +25° | +35°, le jet pousse légèrement (1 m) | +45° | +60°, **cercle complet autour du renard** | Long souffle (R1), Fourrure de braise |
| R3 | **Mèche vive** | recharge du jet | −0.5 s | −0.8 s | −1.1 s, un kill en feu rend 0.5 s | −1.4 s | −1.7 s, **un kill en feu finit la recharge** | Souffle double (R7), Orbe vorace (G9) |
| R4 | **Braises** | durée de la brûlure ; les brûlures se cumulent (plafond) | +2 s | +3 s | +4 s, 2 brûlures se cumulent | +5 s | +6 s, **3 brûlures se cumulent, à 3 l'ennemi est paralysé 1 s** | Allumette (G28), Détonateur (G20) |
| R5 | **Feu grégeois** | le jet laisse des flaques de feu au sol | 3 s, 1.5 m | 4 s, 2 m | 5 s, 2.5 m, les flaques ralentissent 30 % | 6 s, 3 m | 8 s, 3.5 m, **la première entrée dans une flaque étourdit 0.6 s** | Allumette (G28), Briseur d'os (G21) |
| R6 | **Combustion** | un ennemi qui meurt **en feu** explose en flammes | 15 PV, 2.5 m | 22 PV, 3 m | 30 PV, 3.5 m, les voisins brûlent | 40 PV, 4 m | 55 PV, 4.5 m, **chaîne (3 maillons)** | Allumette (G28), Détonateur (G20) |
| R7 | **Souffle double** | charges de jet | 2 charges (la 2e revient 2× plus lentement) | la 2e revient à vitesse normale | 2 charges, le 2e jet consécutif +25 % de brûlure | 3 charges | 3 charges, **un jet tenu enchaîne les charges sans pause (jet long)** | Mèche vive (R3) |
| R8 | **Foulée ardente** | au-dessus de 14 m/s, vos pas laissent une traînée de feu | 1.5 s, 6 PV/s | 2 s | 2.5 s, 9 PV/s, aussi pendant une glissade | 3 s | 4 s, 12 PV/s, **le dash lâche une boule de feu devant (20 PV, 2.5 m)** | Vif-argent (G26), Sillage (G13), Semelles de vent (G24) |

---

## Caméléon : barillet (10, retravaillé)

Identité : **précision, patience, un coup qui change tout**. 6 chambres, 12 PV, headshot ×2. Le retravail donne un **rôle à chaque
chambre** et un **lien avec la télékinésie** (la balle-harpon). Les anciennes « Balles lourdes », « Gâchette vive », « Long canon »,
« Rechargement éclair » et « Barillet garni » deviennent des **rangs** des 8 ci-dessous ; « Balles incendiaires » disparaît (le feu
est au renard) ; « Œil du caméléon » et « Sang-froid » passent dans le passif **Œil fendu**.

### Passifs (2, un seul porté)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique |
|---|---|---|---|---|---|---|---|
| C-P1 | **Peau changeante** | immobile et sans tirer, vous vous fondez : les goules vous perdent, le traqueur vous détecte de moins loin | après 1.5 s, −30 % de détection | 1.2 s, −40 % | 1 s, −50 %, les goules proches s'arrêtent de chercher | 0.8 s, −60 % | 0.6 s, −70 %, **le premier tir depuis le camouflage est un critique ×3 garanti** |
| C-P2 | **Œil fendu** | zone de tête des ennemis plus grande ; les critiques s'enchaînent (2.5 s) : chacun augmente le suivant | zone +20 %, +0.25 par chaîne (max 3) | +30 %, +0.35 | +40 %, +0.45 (max 4), 8 % de chance de critique sur tout le corps | +50 %, +0.55 | +60 %, +0.7 (max 5), 15 % de critique sur le corps, **headshot sur un paralysé = kill (goule et volant)** |

### Améliorations du barillet (8)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| C1 | **Balles paralysantes** | chaque touche fige l'ennemi | 0.35 s | 0.5 s | 0.65 s, un paralysé prend +15 % | 0.8 s | 1 s, **un paralysé touché 3 fois d'affilée est projeté (rebond, G16)** | Détonateur (G20), Briseur d'os (G21), Œil fendu |
| C2 | **Cadavre explosif** | la balle qui tue fait exploser l'ennemi | 16 PV, 3.5 m | 24 PV, 4 m | 32 PV, 4.5 m, chaîne 2 | 40 PV, 5 m | 50 PV, 5.5 m, **chaîne 3 et l'explosion paralyse 0.5 s** | Main leste (C5), Détonateur (G20) |
| C3 | **Balle perforante** | la balle traverse des ennemis | +1 (75 % gardés) | +2 | +2 (90 % gardés), traverse les objets | +3 | +4, **100 % gardés, le dernier traversé est paralysé** | Ricochet (C4), Éventail (C7) |
| C4 | **Ricochet** | la balle rebondit vers l'ennemi le plus proche | 1 rebond, 60 % | 1, 75 % | 2 rebonds, 75 %, portée 18 m | 2, 90 % | 3 rebonds, **100 %, un ricochet sur un headshot est un headshot** | Perforante (C3), Œil fendu |
| C5 | **Main leste** | un kill remet des balles ; rechargement plus court | +2 balles par kill | rechargement −20 % | +3 balles, −35 % | +4 balles | **barillet de 8, un headshot qui tue recharge tout** | Cadavre explosif (C2), Sixième chambre (C6) |
| C6 | **Sixième chambre** | la dernière balle du barillet est spéciale | ×2 dégâts | ×2.5, étourdit 1 s | ×3, perce tout | ×3.5, explose (4 m) | ×4, **si elle tue, rechargement instantané et la suivante est aussi une « sixième »** (une fois) | Main leste (C5), Briseur d'os (G21) |
| C7 | **Éventail** | maintenir le clic vide le barillet en rafale (cadence ×3, dispersion) | dispersion 6°, dégâts ×0.8 | 5°, ×0.9 | 4°, ×1, la rafale pousse les goules | 3°, ×1.1 | 2°, ×1.2, **la rafale paralyse** et rend 2 balles si elle tue | Balles paralysantes (C1), Perforante (C3) |
| C8 | **Langue de caméléon** | la balle est un **harpon** : tirer un objet le ramène à l'orbe (prise à distance) | objets à 15 m | 25 m, l'objet arrive déjà « tenu » | 35 m, aussi les volants (tirés vers vous, étourdis) | 45 m, une goule touchée est tirée de 3 m | 60 m, **une goule touchée est tirée jusqu'à vous, étourdie 1 s** | Projectiles lourds (G18), Harpon (G11), Briseur d'os (G21) |

---

## Koala : épée en os (10)

Identité : **corps à corps, élan, tank qui frappe en pleine vitesse**. Trois coups (10 / 10 / 16), fente en dash ou en glissade, dégâts
jusqu'à ×1.8 à 20 m/s. Le koala veut **rester en mouvement** et **enchaîner**.

### Passifs (2, un seul porté)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique |
|---|---|---|---|---|---|---|---|
| K-P1 | **Peau épaisse** | dégâts reçus réduits ; PV max | −8 %, +10 PV | −12 %, +15 | −16 %, +20, pas de recul quand vous êtes touché | −20 %, +25 | −25 %, +30, **sous 25 % de PV : coups reçus divisés par 2 et dégâts ×1.3** |
| K-P2 | **Griffes grimpantes** | wall slide plus lent, wall jump plus haut, hissage plus haut | slide −30 %, hissage +0.4 m | wall jump +0.5 m | slide −50 %, hissage +0.8 m, les murs rendent une charge | wall jump +1 m | **s'accrocher au mur (1 s immobile) puis repartir d'un wall jump à pleine puissance** |

### Améliorations de l'épée (8)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| K1 | **Lame longue** | portée et arc des coups | +0.4 m, +5° | +0.7 m, +10° | +1 m, +15°, l'aide à la visée porte à 8 m | +1.3 m, +20° | +1.6 m, **le 3e coup frappe en cercle complet** | Enchaînement (K2) |
| K2 | **Enchaînement** | combo plus rapide, fenêtre plus longue | −10 %, +0.2 s | −18 % | −25 %, +0.4 s, le 3e coup ne casse plus le combo | −32 % | −40 %, **4e coup : tourbillon (16 PV, 360°, repousse)** | Lame longue (K1), Élan du bûcheron (K8) |
| K3 | **Fente** | la fente (dash, glissade) va plus loin et frappe plus fort | +1 m, ×1.3 | +2 m, ×1.4 | +3 m, ×1.5, traverse un ennemi | +4 m, ×1.6 | +5 m, ×1.8, **la fente traverse tous les ennemis et les étourdit** | Harpon (G11), Briseur d'os (G21) |
| K4 | **Onde de taille** | un coup porté à plus de 12 m/s envoie une onde tranchante devant | 6 m, 10 PV | 8 m, 14 PV | 10 m, 18 PV, l'onde traverse | 12 m, 24 PV | 14 m, 30 PV, **l'onde étourdit 1 s** | Vif-argent (G26), Semelles de vent (G24) |
| K5 | **Os brisé** | les coups font saigner (dégâts sur la durée) | 3 PV/s, 3 s | 4 PV/s, 4 s | 5 PV/s, 5 s, 2 saignements se cumulent | 6 PV/s, 6 s | 8 PV/s, 7 s, **un saignant qui meurt vous soigne 8 PV** | Brise-garde (K7) ; saigner et brûler se cumulent (Allumette G28) |
| K6 | **Garde d'os** | la parade avec l'épée : fenêtre plus longue, prochain coup critique après une parade | +30 ms, ×1.5 | +50 ms, ×1.75 | +70 ms, ×2, la parade frappe (10 PV) | +90 ms, ×2.25 | +110 ms, ×2.5, **riposte automatique : la parade porte le 3e coup** | Riposte (G1), Rythme de fer (G4) |
| K7 | **Brise-garde** | coups sur un ennemi étourdi, paralysé ou saignant | ×1.3 | ×1.5 | ×1.7, le coup prolonge l'étourdissement 0.5 s | ×1.9 | ×2.2, **exécution : un étourdi sous 40 % de PV meurt** | Briseur d'os (G21), Détonateur (G20) |
| K8 | **Élan du bûcheron** | un kill à l'épée rend une charge de mouvement et +vitesse | +1 m/s (2 s) | +1.5 m/s | +2 m/s (3 s), aussi un dash gratuit | +2.5 m/s | +3 m/s (4 s), **un kill rend aussi une charge de traction** | Charge en plus (G23), Prédateur (G22) |

---

## Hibou : livre à lien (10)

Identité : **distance, contrôle, soutien**. Drain continu 15 PV/s (20 % rendus), soin d'allié. Le hibou veut **tenir ses liens** en
restant hors de portée et **gérer un groupe**.

### Passifs (2, un seul porté)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique |
|---|---|---|---|---|---|---|---|
| H-P1 | **Vision nocturne** | les ennemis ont un contour visible à travers l'obscurité ; le hibou lit l'état d'un ennemi (PV) | 20 m | 30 m | 40 m, et à travers les murs à moins de 15 m | 50 m | partout, **le traqueur est visible à travers les murs à moins de 30 m** |
| H-P2 | **Plumes** | chute ralentie en tenant saut (plané), pas de dégâts de chute | plané ×0.7 de gravité | ×0.55 | ×0.4, le plané garde la vitesse horizontale | ×0.3 | **+1 saut en l'air et le plané dirige (contrôle complet)** |

### Améliorations du livre (8)

| # | Nom | Effet | Bronze | Argent | Or | Diamant | Mythique | Synergies |
|---|---|---|---|---|---|---|---|---|
| H1 | **Lien multiple** | liens simultanés sur plusieurs ennemis | 2 | 2, chacun à 100 % | 3 | 3, +10 % de drain par lien | 4, **tous les liés partagent les dégâts reçus (dégâts sur l'un = 30 % sur les autres)** | Chaînage (H2), Lien vorace (H3) |
| H2 | **Chaînage** | le drain saute à l'ennemi le plus proche de la cible | 1 saut, 6 m, 50 % | 1 saut, 8 m, 65 % | 2 sauts, 10 m, 75 % | 2 sauts, 90 % | 3 sauts, 12 m, 100 %, **la cible morte passe le lien au suivant sans recharge** | Lien multiple (H1), Pages brûlantes (H7) |
| H3 | **Lien vorace** | drain par seconde | +4 PV/s | +7 | +10, la cible ralentit 25 % | +13 | +16, **la cible ralentit 50 % et ne peut plus bondir** | Sangsue (H4), Lien d'ancrage (H6) |
| H4 | **Sangsue** | part du drain rendue au hibou | 30 % | 40 % | 50 %, le surplus au-dessus du max devient un bouclier (max +20) | 60 % | 75 %, **bouclier max +40, il absorbe un coup du traqueur dans l'arène du boss** | Lien vorace (H3), Cœur solide (G19) |
| H5 | **Longue portée** | portée de lancement et de maintien | +5 m / +5 m | +8 / +8 | +12 / +12, la vue peut se perdre 1.5 s | +15 / +15 | +20 / +20, **le lien traverse les murs 1 s** | Vision nocturne (H-P1) |
| H6 | **Lien d'ancrage** | la cible liée sert d'**ancre de traction** : vous pouvez vous tracter vers elle | traction vers la cible | la cible est freinée de 30 % | la traction arrive avec un coup (20 PV) | la cible est tirée de 2 m vers vous à l'accroche | **la cible est clouée au sol 2 s à l'accroche** | Harpon (G11), Élan de traction (G12) |
| H7 | **Pages brûlantes** | un ennemi qui meurt sous le lien lâche des âmes en plus et rend la recharge | +1 âme, recharge −50 % | +1, −75 % | +2, −100 %, le lien se relance sur le voisin (6 m) | +2 | +3, **la mort libère une onde qui étourdit (4 m)** | Chaînage (H2), Âmes gourmandes (G7) |
| H8 | **Soin lié** | le soin d'allié est plus fort ; en solo, le drain soigne aussi un allié lié par le regard… et vous : +régénération pendant un drain | soin 10 PV/s, +1 PV/s pour vous | 12 PV/s, +2 | 14 PV/s, +3, le soin a 2 charges | 16 PV/s, +4 | 20 PV/s, +6, **le soin ressuscite un allié tombé (coop) une fois par acte** | Sangsue (H4), Cœur solide (G19) |

---

## Synergies à montrer en jeu (les « combos » que le joueur découvre)

| Combo | Pièces | Ce qui se passe |
|---|---|---|
| **Le feu d'artifice** | Balles paralysantes (C1) + Détonateur (G20) + Cadavre explosif (C2) | chaque touche paralyse, chaque mort explose deux fois, les explosions paralysent les voisins : une salle se vide en chaîne |
| **L'incendie** | Flamme large (R2) + Allumette (G28) + Combustion (R6) + Feu grégeois (R5) | tout brûle, le feu se propage de mort en mort, le sol reste en feu |
| **Le bélier** | Fente (K3) + Harpon (G11) + Briseur d'os (G21) + Élan du bûcheron (K8) | se tracter sur une goule, la traverser en fente, l'exécuter, repartir avec une charge |
| **La toile** | Lien multiple (H1) + Chaînage (H2) + Lien vorace (H3) + Sangsue (H4) | 4 ennemis liés et ralentis, le hibou gagne un bouclier sans s'approcher |
| **Le fantôme** | Souffle court (G29) + Peau changeante (C-P1) + Garde large (G2) | parer, disparaître pour le traqueur, repartir |
| **L'acrobate** | Charge de répulsion (G17) + Pas de chat (G25) + Vif-argent (G26) + Chute amortie (G27) | répulsion au sol pour monter, wall jumps, bunny hop, atterrissage qui explose |
| **La tirelire** | Âmes gourmandes (G7) + Aspiration (G6) + Marchandage (G8) + Œil du collectionneur (G30) | plus d'âmes, moins cher, plus de choix : une run « économie » qui monte tout en Diamant |

## Pour la DA

- **70 icônes** (30 génériques, 10 par personnage) : une par amélioration, le rang ne change que le **cadre** (5 cadres).
- Les icônes des **passifs** ont une forme distincte (par exemple un losange) pour se lire dans le HUD.
- HUD : améliorations possédées **au-dessus de la barre de vie en bas à gauche**, illustration + cadre de rang (façon Ravenswatch) ;
  le passif en premier.
- Marchand : 3 cartes (nouvelle, montée de rang, soin), le rang **avant → après** sur la carte de montée.
- Écran de fin : la liste des améliorations tient dans son cadre (10 au plus, avec rang).

## Ce qui change dans le code (point 2 de la nuit 4)

- `UpgradeDefinition` : rang (0 à 4), `isPassive`, `character` (vide : générique), effets **par rang** (tableau de 5 valeurs par
  paramètre) ; `rarity` disparaît (remplacée par le rang).
- `PlayerUpgrades` : au plus 10 améliorations, 1 passif ; `RankOf(u)` ; les effets lisent la valeur du rang courant.
- Marchand : 1 nouvelle + 1 montée + soin ; prix par rang (`EconomySettings.rankPrices`) ; coffres : montée de rang possible.
- Les 5 états (étourdi, paralysé, en feu, saignant, lié) exposés par `Damageable` pour les synergies.
