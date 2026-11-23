# -*- coding: utf-8 -*-
"""Nuit 4, lot 3 : catalogue des pièces pour la hauteur dans docs/art/KITS.md (section remplacée si elle existe), mise à
jour des lignes des pièces modifiées dans les sections des actes, et lignes du manifeste (ajoutées ou remplacées sur place).

    python kit_doc_lot3.py <liste des noms, un par ligne>
"""
import json, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
noms = [l.strip().split("/")[-1].replace(".fbx", "") for l in open(sys.argv[1], encoding="utf-8") if l.strip()]
MODIFIES = {"SM_Cave_Puits", "SM_Cave_PorteSortie", "SM_Chateau_GrandeEntree"}
KIT = {"Cave": "KitCaves", "Chateau": "KitChateau", "Jardin": "KitJardin"}
M = {}
for js in ("kit_caves.json", "kit_chateau.json", "kit_jardin.json"):
    M.update(json.load(open(os.path.join(HERE, js), encoding="utf-8")))


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


TITRE = "Nuit 4, lot 3 : pièces pour un level design en hauteur (branche art-kits-3)"
INTRO = open(os.path.join(HERE, "..", "plans", "intro_lot3.md"), encoding="utf-8").read().strip()
nouveaux = [n for n in noms if n not in MODIFIES]
sec = [f"## {TITRE}", "", INTRO, ""]
for a, t in (("Cave", "Acte I : caves"), ("Chateau", "Acte II : château"), ("Jardin", "Acte III : jardin")):
    g = [n for n in nouveaux if n.split("_")[1] == a]
    sec += [f"### {t} ({len(g)} pièces, `{KIT[a]}`)", "",
            "| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |", "|---|---|---|---|---|---|"]
    sec += [ligne_kits(n) for n in g] + [""]
sec += ["Pièces existantes modifiées par ce lot : `SM_Cave_Puits` (refait : margelle lisse, eau, fond), `SM_Cave_PorteSortie` et "
        "`SM_Chateau_GrandeEntree` (sceau de lune de sortie sur les battants) ; leurs lignes sont à jour dans les sections des actes.", ""]
sec = "\n".join(sec)

p = os.path.join(ROOT, "docs", "art", "KITS.md")
s = open(p, encoding="utf-8").read()
for n in MODIFIES:
    s = re.sub(r"^\| `" + re.escape(n) + r"` \|.*$", lambda _m, n=n: ligne_kits(n), s, flags=re.M)
pat = re.compile(r"## " + re.escape(TITRE) + r"\n.*?(?=\n## |\Z)", re.S)
s = pat.sub(lambda _m: sec.rstrip("\n"), s) if pat.search(s) else s.rstrip("\n") + "\n\n" + sec
open(p, "w", encoding="utf-8").write(s.rstrip("\n") + "\n")

man = os.path.join(ROOT, "docs", "assets", "MANIFEST.md")
ms = open(man, encoding="utf-8").read().rstrip("\n").split("\n")
deja = {}
for i, l in enumerate(ms):
    m_ = re.match(r"\| `(SM_[A-Za-z0-9_]+)` \(", l)
    if m_:
        deja[m_.group(1)] = i
for n in noms:
    if n in deja:
        ms[deja[n]] = ligne_manifeste(n)
    else:
        ms.append(ligne_manifeste(n))
open(man, "w", encoding="utf-8").write("\n".join(ms) + "\n")
print("ok", len(nouveaux), "nouvelles pièces,", len(MODIFIES), "modifiées")
