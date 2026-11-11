# Boss traqueur

**Status:** designed · **Updated:** 2026-11-11 · [GDD](../../GDD.md)

Un seul monstre, le même toute la run, presque aussi large que les couloirs. Il rôde dans la
zone, tue en un coup et ne peut être ni blessé, ni ralenti, ni traversé avant la zone finale. Il
est plus rapide que la course de base : seul un mouvement maîtrisé lui échappe. On l'entend avant
de le voir. Piliers : « le mouvement est la survie », « risque contre butin » (D-002).

## Règles

1. **États :** Rôde (patrouille entre les arènes) → Alerte (bruit entendu ou joueur aperçu,
   il va vers la dernière position connue) → Chasse (joueur en vue) → Recherche (cible perdue,
   il fouille autour) → Rôde. Après le timer : **Enragé**, une chasse permanente qui connaît
   toujours la position du joueur.
2. **Perception :** vue (cône + ligne de vue) et ouïe (bruits du joueur : atterrissages,
   impulsions, objets qui se brisent, vagues de combat).
3. **Déplacement :** NavMesh avec un **type d'agent large**. Les portes plus étroites que lui sont
   exclues de son NavMesh : il **fait le tour** par un autre accès. Toute arène a au moins un
   accès assez large pour lui : il doit toujours pouvoir venir.
4. **Contact = mort :** toucher la zone d'attaque du boss tue le joueur, quels que soient ses PV.
5. **Invulnérable :** objets lancés, ondes et ennemis projetés n'ont aucun effet sur lui.
   Son corps est solide pour le joueur et pour les objets.
6. **Annonce :** sons de pas, de souffle et de raclement avec atténuation 3D, audibles bien avant
   qu'il soit visible ; musique de tension qui monte en Alerte et en Chasse.
7. **Phase enragée :** tant qu'elle dure, les âmes gagnées sont multipliées (D-009).
8. **Zone finale :** il devient tuable, seulement par l'environnement (lustres, piliers, pièges,
   points faibles accessibles au magnétisme) et les dégâts ajoutés par les améliorations. Les
   règles détaillées iront dans « Combat final », à concevoir.

## Valeurs

ScriptableObject `StalkerBossSettings`. Valeurs de départ [ASSUMPTION].

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Vitesse Rôde | 3 | m/s | |
| Vitesse Chasse | 9 | m/s | > course (7.5), < bon mouvement enchaîné (12+) |
| Vitesse Enragé | 11 | m/s | encore possible à fuir pour un joueur adroit |
| Accélération / vitesse de rotation | 12 / 180 | m/s² / °/s | virages serrés = moyen de lui échapper |
| Taille (largeur × hauteur) | 2.5 × 3.5 | m | |
| Largeur minimale d'un passage pour lui | 3 | m | |
| Largeur des « petites portes » | 1.2 | m | |
| Portée de vue / angle | 30 / 110 | m / ° | |
| Portée d'ouïe (atterrissage dur / objet brisé) | 15 / 25 | m | |
| Durée de Recherche avant de retourner à Rôde | 10 | s | |
| Portée d'attaque | 2 | m | |
| Temps de grâce au début de la zone | 60 | s | le joueur peut s'orienter |
| Timer de zone avant Enragé | 20 (test : 4) | min | réglable ; 4 min pour les prototypes |
| Multiplicateur d'âmes en phase enragée | 2 | × | [ASSUMPTION] ; vaut-il le risque ? |

## Cas particuliers

- Joueur dans une salle accessible seulement par des petites portes : le boss attend à
  l'extérieur ou patrouille autour, il n'entre pas.
- Coop : il chasse le dernier joueur vu ou entendu (D-009) ; les joueurs « à terre » sont
  ignorés [ASSUMPTION].
- Sortie de zone pendant une Chasse : la transition se fait, le boss ne suit pas.

## Questions ouvertes

- Déroulé exact du combat final (phases, points faibles).
- [ASSUMPTION] Temps de grâce de 60 s au début de chaque zone.

## History

- 2026-11-11 : designed.
- 2026-11-11 : timer réglable (4 min en test), multiplicateur d'âmes en phase enragée, cible en
  coop, mort par l'environnement en zone finale (D-008 à D-010).
