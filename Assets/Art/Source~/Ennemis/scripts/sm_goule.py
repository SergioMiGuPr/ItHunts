# -*- coding: utf-8 -*-
"""SM_Goule : goule quadrupède, version statique pour le jeu (sans squelette), modèle généré par IA avec Tripo
(Assets/Art/Source~/Tripo/tripo_goule_v01.glb), nettoyé dans Blender.

- Échelle : 1.1 m de haut (× 1.12 environ), dans la capsule de jeu de Ø 0.9 × 1.1 m ; pivot au sol, au centre ;
  avant vers -Y (Blender) = +Z (Unity).
- Décimée vers 22 000 triangles (29 654 au départ ; budget de la goule : 15 000 à 25 000), symétrie gauche-droite.
- Textures 2048 px : T_Goule_BaseColor (peau ramenée vers le gris-mauve, entrave et pagne inchangés) et
  T_Goule_Emission (masque : deux yeux ronds à pupille claire, lueur entre les dents) ; le jeu pilote _EmissionColor
  (signal avant le bond).
- Entrave de la cheville gardée (charte).

Lancer dans Blender (avec l'interface) :
    p = r"<dépôt>/Assets/Art/Source~/Ennemis/scripts/sm_goule.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("ennemis_common", None)
import ennemis_common as E
C = E.C
import bpy
import numpy as np

PARAMS = dict(
    glb="tripo_goule_v01",
    hauteur=1.1, triangles=22000, texture=2048,
    peau_mauve=True,
    # zones dans les coordonnées brutes du GLB (avant vers +X, hauteur ≈ 0.98)
    tete=dict(x_min=0.12, z_min=0.27),
    yeux=dict(z=(0.36, 0.46), x_min=0.20, y_min=0.02, sombre=0.14, rayon=0.9, pupille=0.38),
    gueule=dict(z=(0.27, 0.37), x_min=0.22, y_max=0.085, sombre=0.16),
)


def masks(px, pos, P):
    lum = px[..., :3].max(-1)
    x, y, z = pos[..., 0], pos[..., 1], pos[..., 2]
    ok = ~np.isnan(x)
    Y = P["yeux"]
    with np.errstate(invalid="ignore"):
        eye_px = ok & (z > Y["z"][0]) & (z < Y["z"][1]) & (x > Y["x_min"]) & (np.abs(y) > Y["y_min"]) & (lum < Y["sombre"])
    mask = np.zeros(lum.shape, dtype=np.float32)
    info = {}
    for side, sgn in (("droit", -1), ("gauche", 1)):
        sel = eye_px & (np.sign(y) == sgn)
        pts = pos[sel]
        if len(pts) < 10:
            info[side] = "introuvable"
            continue
        c = pts.mean(0)
        r = np.percentile(np.linalg.norm(pts - c, axis=1), 85) * Y["rayon"]
        d = np.linalg.norm(np.nan_to_num(pos) - c, axis=-1)
        disk = np.clip((r - d) / (0.15 * r), 0, 1) * ok
        pup = np.clip((Y["pupille"] * r - d) / (0.12 * r), 0, 1) * ok
        mask = np.maximum(mask, 0.55 * disk)
        mask = np.maximum(mask, pup)
        info[side] = "centre (%.3f, %.3f, %.3f), rayon %.4f, %d texels sombres" % (c[0], c[1], c[2], r, len(pts))
    G = P["gueule"]
    with np.errstate(invalid="ignore"):
        gap = ok & (z > G["z"][0]) & (z < G["z"][1]) & (x > G["x_min"]) & (np.abs(y) < G["y_max"]) & (lum < G["sombre"])
    glow = np.clip(E.blur(gap.astype(np.float32), 1) * 2.0, 0, 1) * 0.8
    mask = np.maximum(mask, glow)
    info["gueule"] = "%d texels entre les dents" % int(gap.sum())
    return mask, info


def build(P):
    ob = E.import_glb(P["glb"])
    S = P["texture"]
    px = E.source_pixels(ob, S)
    T = P["tete"]
    pos = E.rasterize(ob, S, lambda c: c.x > T["x_min"] and c.z > T["z_min"])
    mask, info = masks(px, pos, P)
    base = px
    part = 0.0
    if P["peau_mauve"]:
        base, part = E.skin_to_mauve(px, extra=mask > 0.05)
    em = np.zeros_like(px); em[..., 3] = 1
    em[..., 0] = em[..., 1] = em[..., 2] = mask
    p_base = E.save_png(base, "T_Goule_BaseColor")
    E.save_png(em, "T_Goule_Emission")
    t0, t1 = E.weld_decimate(ob, P["triangles"], symmetry="Y")
    k, size = E.face_front_and_ground(ob, height=P["hauteur"])
    E.smooth_shading(ob, 60)
    mat = C.make_material("M_Goule", p_base)
    ob.data.materials.clear(); ob.data.materials.append(mat)
    ob.name = ob.data.name = "SM_Goule"
    print("goule : %d -> %d triangles ; échelle × %.4f ; taille (x, y, z) = (%.3f, %.3f, %.3f) m ; peau recolorée %.1f %%"
          % (t0, t1, k, size[0], size[1], size[2], 100 * part))
    for kk, v in info.items():
        print("  %s : %s" % (kk, v))
    return [ob]


if __name__ == "__main__":
    C.run_piece("SM_Goule", build, PARAMS, "SM_Goule", views=("face", "cote", "34"))
