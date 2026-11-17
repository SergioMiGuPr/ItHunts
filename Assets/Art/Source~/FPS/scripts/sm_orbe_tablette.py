# -*- coding: utf-8 -*-
"""SM_Orbe_Tablette : UNE tablette de l'orbe (D-069 : une tablette par charge de traction ou de répulsion, créées et
réparties autour de l'orbe par le jeu). Pivot au centre, transformations nulles.

Repère Unity : X = rayon (largeur 0.035 m, vers l'extérieur de la couronne), Y = grand côté (0.05 m, tangent à la
couronne), Z = épaisseur (0.008 m). Rune gravée sur les DEUX faces (-Z vers le joueur, +Z), lisible sans miroir de chaque
côté. Matériaux : index 0 = M_Orbe_Pierre (bords), index 1 = M_Orbe_Tablette (atlas T_Orbe_Tablette_BaseColor +
masque T_Orbe_Tablette_Emission ; le jeu allume la rune par _EmissionColor sur l'index 1). Glyphe : case 0 de l'atlas ;
les 5 autres se choisissent par _BaseMap_ST (voir INTEGRATION-PROPS, « Orbe »).
"""
import sys, os
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
for m in ("fps_common", "sm_orbe", "orbe_glyphes"):
    sys.modules.pop(m, None)
import fps_common as F
import sm_orbe as O


def build(P):
    mats = {"pierre": F.tex_material("M_Orbe_Pierre", "T_Orbe_Pierre_BaseColor"),
            "rune": F.C.make_material("M_Orbe_Tablette", os.path.join(F.TEX_DIR, "T_Orbe_Tablette_BaseColor.png"))}
    ob = O.build_tablet_seule(P, mats, 0)
    print("SM_Orbe_Tablette : %d triangles" % F.C.tri_count(ob))
    return [ob]


if __name__ == "__main__":
    F.run("SM_Orbe_Tablette", build, O.PARAMS, views=("face", "cote", "34", "dos"))
