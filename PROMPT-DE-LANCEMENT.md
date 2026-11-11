# Prompt de lancement

Colle ce message dans Claude Code, lancé à la racine du projet Unity, avec Unity ouvert (Blender
n'est pas nécessaire au début).

---

On démarre le projet décrit dans CLAUDE.md : un roguelite FPS centré sur un mouvement très
dynamique (glissade, wall slide, wall jump, bunny hop) et deux orbes (magnétisme et télékinésie),
avec des vagues d'ennemis et un boss par zone.

Fais dans l'ordre, et arrête-toi à la fin de chaque point pour que je valide :

1. **Vérifie l'environnement** : `unity status`, version d'Unity, packages Input System,
   Cinemachine et URP, et serveur Blender (`/mcp`) s'il est lancé. Liste ce qui manque et
   comment le corriger. N'installe rien sans me demander.
2. **Lis** `docs/design/BRIEF.md` et les fiches de `docs/design/reference/`, puis pose-moi les
   questions ouvertes du brief par petits groupes, en me proposant pour chacune 2 ou 3 options
   avec leurs conséquences sur le mouvement, le combat et la coop. Mets le brief à jour avec mes
   réponses, puis écris un GDD court.
3. **Propose un plan** pour le prototype du mouvement de base en greybox : architecture du
   contrôleur (machine à états, pas fixe, entrées séparées pour la coop future),
   ScriptableObjects de réglages, scène `Test_Movement`, affichage de debug. Donne des valeurs de
   départ chiffrées pour chaque réglage, tirées des skills, et dis lesquelles viennent de jeux de
   référence.
4. Après mon accord, **construis seulement la course et le saut** (coyote time, saut mémorisé,
   hauteur variable, gravité de chute), vérifie que ça compile et que les tests passent, puis
   donne-moi une checklist de playtest précise.

Tiens `docs/PROGRESS.md` à jour à chaque étape.
