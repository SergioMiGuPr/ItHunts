# -*- coding: utf-8 -*-
"""Écrit le catalogue d'un kit dans docs/art/KITS.md (section remplacée si elle existe) et ses lignes au manifeste.

    python kit_doc.py <kit_xxx.json> <KitDossier> <préfixe SM_> "<titre de section>" "<introduction>"
"""
import json, os, sys, re
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
js, kit, pref, titre, intro = sys.argv[1:6]
M = json.load(open(os.path.join(HERE, js), encoding="utf-8"))
if intro.startswith("@"):
    intro = open(os.path.join(HERE, intro[1:]), encoding="utf-8").read().strip()

lignes = [f"## {titre}", "", intro, "",
          f"Modèles `Assets/Art/Models/{kit}/`, prefabs `Assets/Art/Prefabs/{kit}/` (collisions : maillage pour l'architecture, "
          "boîte pour les petits objets ; sources de lumière : feu `VFX_Torche_Feu` et lumière ponctuelle sur les repères `Feu*`), "
          f"scène `Assets/Art/Scenes/Vitrine_{kit}.unity`, planche `docs/art/rendus/{kit}/Planche_{kit.replace('Kit', 'Kit_')}.jpg`, "
          f"une capture par pièce dans `docs/art/rendus/{kit}/captures/`. Source : `Assets/Art/Source~/Kits/scripts/` "
          "(`kitlib.py` et le script du kit, Blender en ligne de commande).", "",
          "| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |", "|---|---|---|---|---|---|"]
for nom, m in M.items():
    t = m["taille"]
    lignes.append(f"| `{nom}` | {m['doc']} | {t[0]} × {t[1]} × {t[2]} | {m['bas']} | {m['triangles']} | "
                  + ", ".join(f"`{x}`" for x in m["materiaux"]) + " |")
lignes.append("")
sec = "\n".join(lignes)

p = os.path.join(ROOT, "docs", "art", "KITS.md")
s = open(p, encoding="utf-8").read() if os.path.exists(p) else (
    "# Kits de décor des actes I à III (nuit du 2026-11-22)\n\n"
    "Modélisés par script dans Blender (aucune génération IA, aucun crédit). Architecture en **UV monde** : les matériaux\n"
    "de la bibliothèque (`M_Bib_*`, docs/art/MATERIAUX.md) s'appliquent sans dépliage ; **peinture de sommets** sur chaque pièce\n"
    "(R saleté en bas, G mousse ou salpêtre en plaques, B teinte de la pièce) lue par les matériaux `*_Peint`. Conventions : Unity\n"
    "+Y en haut, face visible de la pièce vers **+Z** (dos des murs et des objets muraux vers −Z, contre le mur), pivot au sol au centre de\n"
    "l'emprise sauf mention, 1 unité = 1 m, échelle du jeu (joueur 1.8 m, traqueur 14 m, portes des modules 8 × 10 m).\n"
    "Matériaux en double face (surfaces ouvertes). Repères vides exportés : `Feu*` (flammes), `Accroche_*` (points de gameplay).\n"
    "Lumières : torches à l'ambiance intermédiaire du jeu (1.35 / 15.4 m, orangé (1, 0.6, 0.32)), bougies 0.6 / 5 m ; pour que\n"
    "`main` les règle avec l'ambiance de la scène, ajouter `AmbianceLight` (Torche ou Lampe) sur les objets « Lumiere ».\n\n")
pat = re.compile(r"## " + re.escape(titre) + r"\n.*?(?=\n## |\Z)", re.S)
s = pat.sub(sec.rstrip("\n"), s) if pat.search(s) else s.rstrip("\n") + "\n\n" + sec
open(p, "w", encoding="utf-8").write(s)

man = os.path.join(ROOT, "docs", "assets", "MANIFEST.md")
ms = open(man, encoding="utf-8").read().rstrip("\n").split("\n")
ms = [l for l in ms if not any(l.startswith(f"| `{n}`") for n in M)]
for nom, m in M.items():
    t = m["taille"]
    ms.append(f"| `{nom}` ({m['doc'].rstrip('.')}) | `Assets/Art/Models/{kit}/{nom}.fbx`, prefab `Assets/Art/Prefabs/{kit}/{nom.replace('SM_', '')}.prefab` | "
              f"Modélisé par script dans Blender (`Assets/Art/Source~/Kits/scripts/`), sans IA | non | Code du projet | Sans objet (aucune entrée externe) | "
              f"{t[0]} × {t[1]} × {t[2]} m | sol, centre (sauf mention) | {m['triangles']} | À valider DA |")
open(man, "w", encoding="utf-8").write("\n".join(ms) + "\n")
print("ok", len(M))
