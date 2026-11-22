# -*- coding: utf-8 -*-
"""Écrit les fiches des salles d'exemple (salles.FICHES) dans docs/art/INTEGRATION-PROPS.md (section remplacée)."""
import os, re, sys
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import salles as S
for fs in S.ACTES.values():
    for f in fs:
        f()
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
p = os.path.join(ROOT, "docs", "art", "INTEGRATION-PROPS.md")
titre = "## Salles d'exemple des trois actes (lot 5, nuit du 2026-11-22, PR art-salles-exemple)"
L = [titre, "",
     "Onze salles entièrement habillées et éclairées, assemblées avec les kits des lots 2 à 4, prêtes à servir de modèles. "
     "Prefabs dans `Assets/Art/Levels/Exemples/<Acte>/<Nom>.prefab` (pièces des kits avec collisions, blocs de sol et de plafond, "
     "lumières) ; scènes de contrôle `Assets/Art/Scenes/Exemples_<Acte>.unity` (éclairage de l'acte) ; captures dans "
     "`docs/art/rendus/Exemples/<Acte>/`. Les plans sont générés par `Assets/Art/Source~/Kits/scripts/salles.py` "
     "(texte lisible dans `Assets/Art/Source~/Kits/plans/`), assemblés par `SalleBuilder.cs`. **Pour `main` :** y ajouter le "
     "gameplay (points d'apparition, vagues, coffres, portes de module, NavMesh) ; les repères `Accroche_*` des pièces marquent "
     "les points de traction et les rebords. Coordonnées dans le repère du prefab (m), +Z vers le nord.", ""]
for nom, f in S.FICHES.items():
    L += [f"### {nom} ({f['acte']}, {f['taille']})", "",
          f"- **Identité :** {f['identite']}",
          f"- **Fonction :** {f['fonction']}",
          f"- **Entrées et sorties :** {f['entrees']}",
          f"- **Routes de parcours :** {f['route']}",
          f"- **Points de grab et de traction :** {f['prises']}",
          f"- **Moment de tension :** {f['tension']}",
          f"- **Captures :** `docs/art/rendus/Exemples/{f['acte'].replace('â', 'a')}/{nom}_*.jpg`", ""]
sec = "\n".join(L)
s = open(p, encoding="utf-8").read()
pat = re.compile(re.escape(titre) + r"\n.*?(?=\n## |\Z)", re.S)
s = pat.sub(sec.rstrip("\n"), s) if pat.search(s) else s.rstrip("\n") + "\n\n" + sec
open(p, "w", encoding="utf-8").write(s)
print("ok", len(S.FICHES))
