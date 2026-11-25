# Cartes faites à la main (D-110)

Depuis le jour du 2026-11-24, les actes ne tirent plus leur plan au hasard : chaque acte a 2 cartes écrites à la main (dans
`Assets/_Project/Settings/Cartes/`) qui tournent d'une run à l'autre (`seed % nombre de cartes`). Le contenu reste tiré au
hasard : ennemis et vagues, coffres sur leurs ancres, marchand sur un de ses emplacements, intérieur des salles (fosses, îlots,
ponts, props, lumières).

Les fichiers texte sont écrits par les scripts `Tools/Cartes/*.py` (un par carte) : on y décrit les salles en rectangles, et
`carte.py` dessine le plan, vérifie les contacts des portes, les sorties de chaque salle, l'accès depuis le départ, puis écrit le
fichier. Lancer `python Tools/Cartes/<carte>.py` après chaque modification.

## Format du fichier

```
nom: Les puits noyés
acte: 1
echelle: 8            # mètres par caractère du plan (multiple de 2)
organique: 12         # bords des salles rongés jusqu'à 12 m (caves, jardin) ; 0 : bords droits (château)

[plan]                # nord en haut ; un caractère par salle, « . » = roche
|aaaabbbb....|

[salles]
a : Ossuaire ; sol 8 ; vide                       # sol (m), fosse
S : Grande porte ; sol 8 ; sortie ; plafond 20 ; fonction Salle des gardes
D : Salle du puits ; sol 0 ; depart
P : Grand puits ; sol -8 ; vide ; galerie n,s,e,o 8 ; plafond 26   # galerie le long des côtés, à 8 m au-dessus du sol
t : Salle de torture ; sol 0 ; galerie e 5 sans-escalier          # on y monte par traction, wall jump, hissage
C : Cloître ; sol 0 ; ciel ; eau                  # ciel ouvert

[portes]
H-S sortie            # porte de sortie (vers le hall)
t-A arrivee           # porte d'arrivée du traqueur
d-L verrou            # porte de la salle verrouillée
g-P haute             # porte en hauteur : palier sans escalier dans la salle basse, à la hauteur de la salle haute
S-g rampe             # rampe imposée (sinon palier à escalier ou rampe selon la place)
a-b @n à 0.3          # côté de a où percer la porte, position le long de la limite

[ancres]
coffre b i j n r      # salles où les coffres peuvent apparaître (3 à 4 tirés)
marchand k m v        # un seul marchand, dans une de ces salles
```

Rôles : `depart`, `sortie`, `hall`, `arrivee` (le traqueur entre par cette porte), `verrouillee`. Une seule hauteur de galerie
par salle (paliers des portes hautes compris).

## Règles appliquées (docs/design/RECHERCHE-CARTES.md)

- Chaque salle a au moins 2 sorties (test `HandMapTests`) ; les grandes en ont 3 ou plus, dont une en hauteur.
- Le traqueur (agent de 3,5 m de rayon, 7 m de large) atteint chaque zone accessible au joueur : audit `StalkerReachAudit`
  (sa main porte à 14,8 m de l'épaule). Les ponts au-dessus des fosses font 14 m, les îlots 12 × 12 m, une escalade du traqueur
  est posée à chaque palier et chaque galerie, un bond vers chaque îlot.
- Encoche de 16 m dans une galerie devant chaque porte du sol (sinon le traqueur reste bloqué derrière la porte).
- Surface : au moins ×4 celle des actes tirés au hasard pour les actes I et II, ×6 pour l'acte III.
- Les liens d'escalade des goules sont dans la zone « Jump » : le bot de playtest (qui joue comme un joueur) ne les prend pas.

## Vérifier une carte

- `HandMapTests` (EditMode) : lecture, plan sans échec, sorties, surface, ancres.
- Vue de dessus et audit du traqueur : voir `docs/design/jour-5/cartes/`.
- Bot de playtest : `BotRunTests.Acte{n}_Seed{11,22}` (les deux cartes de l'acte).
