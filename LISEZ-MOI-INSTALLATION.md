# Installation — kit roguelite FPS à deux orbes

Ce kit se copie **à la racine de ton projet Unity**, c'est-à-dire dans le dossier qui contient
`Assets/`, `Packages/` et `ProjectSettings/`. Tout est local : aucune clé API n'est nécessaire,
sauf ta clé Tripo si tu génères des modèles 3D (Tripo est payant).

## Contenu

```
CLAUDE.md                     règles et feuille de route lues par Claude Code à chaque session
.mcp.json                     connexion à Blender (Blender MCP, télémétrie coupée)
.claude/settings.json         garde-fous : demande avant Tripo, code Blender, commit, push
.claude/skills/               29 skills de awesome-gamedev-agent-skills (Unity, disciplines,
                              roguelike, génération procédurale, FPS)
docs/design/BRIEF.md          ton idée de jeu, ce qui est décidé, les questions à trancher
docs/design/reference/        fiches parkour, FPS, action-roguelite, vagues (survivors-like),
                              coop, local/en ligne, direction artistique, mélange de genres
                              (de claude-code-game-studios)
docs/design/references-visuelles/  dépose ici tes images d'inspiration
docs/PROGRESS.md              mémoire du projet entre les sessions
docs/assets/MANIFEST.md       suivi des modèles 3D (source, licence, échelle…)
LICENSES/                     licences des deux projets d'origine (Apache-2.0 et MIT)
PROMPT-DE-LANCEMENT.md        le premier message à envoyer à Claude Code
```

## 1. Unity

1. Installe **Unity Hub**, puis **Unity 6.3 LTS**.
2. Crée un projet avec le modèle **3D (URP)**.
3. **Ferme Unity**, puis prépare Git dans le dossier du projet, **avant le premier commit** :
   - ajoute le `.gitignore` Unity (modèle officiel `Unity.gitignore` du dépôt `github/gitignore`),
     sinon `Library/` (plusieurs Go) part dans le repo ;
   - `git init`, puis `git lfs install` et
     `git lfs track "*.fbx" "*.glb" "*.blend" "*.png" "*.psd" "*.wav" "*.ogg"` ;
   - crée sur GitHub un repo **privé et vide** (sans README ni .gitignore), puis
     `git remote add origin <url>`.
   Vérifie avec `git status` que `Library/`, `Temp/`, `Logs/` et `UserSettings/` n'apparaissent
   pas, et que `.claude/` et `.mcp.json` apparaissent. Claude Code peut faire cette étape pour toi.
4. Installe la **Unity CLI** en suivant la doc officielle :
   https://docs.unity.com/en-us/unity-cli/use-unity-cli
5. Dans le dossier du projet, lance `unity pipeline install`, ouvre le projet dans l'éditeur,
   puis vérifie avec `unity status`.

## 2. Plugin officiel Unity pour Claude Code

Dans Claude Code (lancé avec `claude`) :

```
/plugin marketplace add Unity-Technologies/unity-agent-plugin
/plugin install unity@unity-agent-plugin
```

## 3. Blender et Blender MCP

1. Installe **Blender** (3.0 ou plus récent ; prends la dernière version stable).
2. Installe **uv** avec son installeur officiel : https://docs.astral.sh/uv/
3. Installe le add-on : `uvx mcp-for-blender install-addon`
4. Dans Blender : **Edit → Preferences → Add-ons**, active **Interface: MCP for Blender**.
5. Dans la vue 3D, touche **N**, onglet **MCP for Blender**, bouton **Start MCP Server**.
6. **Tripo** : dans ce même panneau, section **AI Model Generation**, coche **Tripo** et colle ta
   clé API Tripo. Ne la donne jamais dans le chat de Claude.

Le serveur Blender est déjà déclaré dans `.mcp.json` : au premier lancement, Claude Code te
demandera d'approuver ce serveur. Vérifie ensuite avec `/mcp` qu'il apparaît connecté.

## 4. Copier le kit

Copie **tout le contenu** de ce dossier (y compris les dossiers cachés `.claude` et le fichier
`.mcp.json`) à la racine du projet Unity. Si le projet a déjà un `CLAUDE.md`, fusionne-les.

## 5. Lancer

Ouvre Unity et Blender, va dans le dossier du projet, lance `claude`, et colle le contenu de
`PROMPT-DE-LANCEMENT.md`.

## Ce que tu feras encore toi-même

- **Tester à la manette ou au clavier** et dire ce qui ne va pas : Claude ne peut pas sentir si
  un saut est agréable.
- Les décisions de game design et, plus tard, le dessin des niveaux.
- Approuver les générations Tripo (elles coûtent des crédits) et les commits.
- Garder Unity et Blender ouverts pendant que Claude travaille.

## Sécurité

- Le add-on Blender ouvre un port local sans authentification et peut exécuter du Python :
  garde-le sur ta machine, sauvegarde ton `.blend` avant les grosses opérations. Le réglage
  `.claude/settings.json` fait demander Claude avant chaque exécution de code dans Blender.
- Les skills ont été copiés depuis les commits audités (`25eec57` pour awesome-gamedev-agent-skills,
  `abc0de7` pour claude-code-game-studios). Ils ne se mettent pas à jour seuls.
