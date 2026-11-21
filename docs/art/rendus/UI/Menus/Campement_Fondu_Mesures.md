# Campement : mesures du fondu des personnages assis (2026-11-22)

Mesures sur les captures Unity gelées (1670 x 941), par `fondu.py` :

- **personnage** : pixels du personnage à plus de 3 px de son contour ;
- **décor** : anneau de 6 à 40 px autour de la silhouette, pris dans le décor peint seul (`Campement_Fond.png`), hors feu, cartouches et autres personnages ;
- **lum** : luminance moyenne (0 à 255) ; **sat** : saturation moyenne ((max − min) / max) ;
- **net** (netteté, acuité) : détails à 1 px divisés par détails à 3 px ; elle mesure la finesse, pas la quantité de traits ;
- **détail** : contraste moyen des détails à 1 px (0 à 255), donné pour information ;
- **lanterne** : pixels de la lanterne hors verre allumé ; son décor est l'anneau de 5 à 22 px autour d'elle.

« Avant » : PR #19 (anciens sprites et placements). « Après » : cette PR.

**Lecture.** Luminosité et saturation : égales à celles du décor à moins de 1 % près (16 couples). Netteté : les sprites réduits sont déjà un peu plus doux que le décor (acuité 0,52 à 0,61 contre 0,61 à 0,68) ; l'égaler demanderait de les renforcer, à l'inverse de la douceur demandée : douceur fixe de 0,5 px (contour adouci sur 1 px, grain de la peinture). Le contraste des traits (détail) reste celui du dessin, 2 à 3 fois celui du décor autour, **comme sur la maquette** (renard et hibou de la maquette : détail 11 et 14, acuité 0,68 et 0,64, pour un décor à 2,5 à 3 et 0,62 à 0,68) : l'atténuer pour l'égaler rendait les personnages ternes et boueux (essai rejeté, voir la PR).

Places 3 et 4 de la configuration « avant » : seules les configurations A et B ont été capturées avant ; « — » = pas de mesure avant.

| Personnage, place | lum avant | lum après | lum décor | sat avant | sat après | sat décor | net avant | net après | net décor | détail après / décor |
|---|---|---|---|---|---|---|---|---|---|---|
| Cameleon, place 1 | — | 27.9 | 27.9 | — | 0.380 | 0.380 | — | 0.530 | 0.677 | 6.0 / 2.3 |
| Cameleon, place 2 | — | 33.4 | 33.4 | — | 0.438 | 0.438 | — | 0.528 | 0.627 | 7.8 / 2.7 |
| Cameleon, place 3 | 42.4 | 42.0 | 42.0 | 0.550 | 0.541 | 0.541 | 0.560 | 0.533 | 0.641 | 9.4 / 3.3 |
| Cameleon, place 4 | 47.1 | 37.8 | 37.8 | 0.542 | 0.499 | 0.499 | 0.541 | 0.518 | 0.622 | 8.5 / 3.8 |
| Hibou, place 1 | 35.1 | 30.8 | 30.7 | 0.429 | 0.395 | 0.395 | 0.585 | 0.553 | 0.663 | 6.6 / 2.8 |
| Hibou, place 2 | 29.9 | 33.0 | 33.0 | 0.445 | 0.455 | 0.455 | 0.586 | 0.555 | 0.619 | 7.5 / 2.9 |
| Hibou, place 3 | — | 44.2 | 44.2 | — | 0.573 | 0.572 | — | 0.570 | 0.622 | 9.9 / 3.4 |
| Hibou, place 4 | — | 40.1 | 40.1 | — | 0.545 | 0.545 | — | 0.567 | 0.638 | 9.3 / 3.7 |
| Koala, place 1 | — | 30.6 | 30.6 | — | 0.394 | 0.394 | — | 0.537 | 0.681 | 5.5 / 2.8 |
| Koala, place 2 | — | 32.3 | 32.2 | — | 0.458 | 0.458 | — | 0.552 | 0.632 | 6.2 / 2.8 |
| Koala, place 3 | 44.3 | 45.1 | 45.0 | 0.497 | 0.571 | 0.572 | 0.583 | 0.554 | 0.607 | 8.0 / 3.3 |
| Koala, place 4 | 43.8 | 39.2 | 39.4 | 0.478 | 0.536 | 0.537 | 0.577 | 0.552 | 0.640 | 7.2 / 3.6 |
| Renard, place 1 | 35.5 | 29.8 | 29.8 | 0.617 | 0.394 | 0.394 | 0.624 | 0.599 | 0.678 | 6.1 / 2.7 |
| Renard, place 2 | 34.1 | 32.4 | 32.4 | 0.605 | 0.441 | 0.441 | 0.632 | 0.604 | 0.611 | 6.8 / 2.8 |
| Renard, place 3 | — | 43.0 | 43.0 | — | 0.552 | 0.552 | — | 0.612 | 0.637 | 10.3 / 3.2 |
| Renard, place 4 | — | 39.0 | 38.9 | — | 0.517 | 0.516 | — | 0.591 | 0.631 | 7.6 / 3.7 |

| Lanterne, place | lum avant | lum après | lum décor | sat avant | sat après | sat décor |
|---|---|---|---|---|---|---|
| Cameleon, place 1 | — | 49.2 | 49.3 | — | 0.590 | 0.590 |
| Cameleon, place 2 | — | 35.6 | 35.6 | — | 0.390 | 0.389 |
| Cameleon, place 3 | 59.7 | 31.5 | 31.5 | 0.289 | 0.447 | 0.447 |
| Cameleon, place 4 | 59.7 | 23.8 | 23.8 | 0.289 | 0.375 | 0.376 |
| Hibou, place 1 | 64.4 | 49.7 | 49.7 | 0.225 | 0.501 | 0.555 |
| Hibou, place 2 | 64.4 | 37.3 | 37.3 | 0.227 | 0.398 | 0.398 |
| Hibou, place 3 | — | 32.7 | 32.7 | — | 0.451 | 0.451 |
| Hibou, place 4 | — | 22.5 | 22.5 | — | 0.374 | 0.374 |
| Koala, place 1 | — | 53.3 | 53.1 | — | 0.470 | 0.601 |
| Koala, place 2 | — | 37.5 | 37.3 | — | 0.388 | 0.389 |
| Koala, place 3 | 71.2 | 45.7 | 34.9 | 0.310 | 0.435 | 0.468 |
| Koala, place 4 | 67.8 | 22.4 | 22.3 | 0.133 | 0.377 | 0.377 |
| Renard, place 1 | 75.4 | 51.4 | 51.4 | 0.405 | 0.577 | 0.577 |
| Renard, place 2 | 73.5 | 34.4 | 34.4 | 0.408 | 0.374 | 0.374 |
| Renard, place 3 | — | 31.8 | 31.8 | — | 0.447 | 0.447 |
| Renard, place 4 | — | 24.3 | 24.3 | — | 0.375 | 0.375 |
