# -*- coding: utf-8 -*-
"""Nuit 4, lot 3, relecture : met à jour les lignes des pièces corrigées dans docs/art/KITS.md et docs/assets/MANIFEST.md
(lignes remplacées sur place, description = première phrase entière de la docstring), ajoute les pièces nouvelles après
`SM_Chateau_Candelabre_Pied` et corrige les incohérences relevées (gabarit, pente du colimaçon, plafond du petit chandelier).

    python kit_doc_lot3b.py <nom> [<nom> …]
"""
import json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
sys.path.insert(0, HERE)
KIT = {"Cave": "KitCaves", "Chateau": "KitChateau", "Jardin": "KitJardin", "Arene": "KitJardin"}
M = {}
for js in ("kit_caves.json", "kit_chateau.json", "kit_jardin.json"):
    M.update(json.load(open(os.path.join(HERE, js), encoding="utf-8")))
NOUVEAUX = ["SM_Chateau_Torche_Murale", "SM_Chateau_Torchere"]


def ligne_kits(n):
    m = M[n]
    t = m["taille"]
    return (f"| `{n}` | {m['doc']} | {t[0]} × {t[1]} × {t[2]} | {m['bas']} | {m['triangles']} | "
            + ", ".join(f"`{x}`" for x in m["materiaux"]) + " |")


def ligne_manifeste(n):
    m = M[n]
    t = m["taille"]
    kit = KIT[n.split("_")[1]]
    return (f"| `{n}` ({m['doc'].rstrip('.')}) | `Assets/Art/Models/{kit}/{n}.fbx`, prefab `Assets/Art/Prefabs/{kit}/{n.replace('SM_', '')}.prefab` | "
            f"Modélisé par script dans Blender (`Assets/Art/Source~/Kits/scripts/`), sans IA | non | Code du projet | Sans objet (aucune entrée externe) | "
            f"{t[0]} × {t[1]} × {t[2]} m | sol, centre (sauf mention, voir INTEGRATION-PROPS « Nuit 4, lot 3 ») | {m['triangles']} | À valider DA |")


def maj(path, noms, fabrique, motif):
    s = open(path, encoding="utf-8").read()
    for n in noms:
        pat = re.compile(motif(n), re.M)
        if pat.search(s):
            s = pat.sub(lambda _m, n=n: fabrique(n), s)
        elif n in NOUVEAUX:
            ancre = re.compile(motif("SM_Chateau_Candelabre_Pied"), re.M)
            mm = ancre.search(s)
            assert mm, path
            s = s[:mm.end()] + "\n" + fabrique(n) + s[mm.end():]
        else:
            print("absent", path, n)
    return s


noms = sys.argv[1:]
# KITS.md : toutes les lignes de la pièce (section de son acte ou du lot 3)
p = os.path.join(ROOT, "docs", "art", "KITS.md")
s = maj(p, noms, ligne_kits, lambda n: r"^\| `" + re.escape(n) + r"` \|.*$")
for a, b in (("34° au plus dans les colimaçons", "34.4° au plus dans les colimaçons (mesurée)"),
             ("le gabarit de 4 × 5.2 m du traqueur baissé", "le gabarit de 4 × 5.25 m du traqueur baissé")):
    s = s.replace(a, b)
open(p, "w", encoding="utf-8").write(s)
# intro du catalogue (source de kit_doc_lot3.py)
p = os.path.join(HERE, "..", "plans", "intro_lot3.md")
if os.path.exists(p):
    t = open(p, encoding="utf-8").read()
    t = t.replace("34° au plus dans les colimaçons", "34.4° au plus dans les colimaçons (mesurée)")
    t = t.replace("le gabarit de 4 × 5.2 m du traqueur baissé", "le gabarit de 4 × 5.25 m du traqueur baissé")
    open(p, "w", encoding="utf-8").write(t)
# manifeste
p = os.path.join(ROOT, "docs", "assets", "MANIFEST.md")
s = maj(p, noms, ligne_manifeste, lambda n: r"^\| `" + re.escape(n) + r"` \(.*$")
open(p, "w", encoding="utf-8").write(s)
print("ok", len(noms))
