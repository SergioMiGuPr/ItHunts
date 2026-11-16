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
- LOD (jusqu'à 80 goules en jeu) : SM_Goule_LOD0 (22 000 triangles), _LOD1 (≈ 11 000) et _LOD2 (≈ 5 000), décimés à
  partir du LOD0 (mêmes UV : même matériau, même masque d'émission), enfants du même FBX : Unity crée le LODGroup.

Lancer dans Blender (avec l'interface) :
    p = r"<dépôt>/Assets/Art/Source~/Ennemis/scripts/sm_goule.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("ennemis_common", None)
import ennemis_common as E
sys.modules.pop("peinture", None)
import peinture as W
C = E.C
import bpy
import numpy as np

PARAMS = dict(
    glb="tripo_goule_v01",
    hauteur=1.1, triangles=22000, texture=2048,
    lod=(11000, 5000),
    peau_mauve=True,
    # zones dans les coordonnées brutes du GLB (avant vers +X, hauteur ≈ 0.98)
    tete=dict(x_min=0.12, z_min=0.27),
    yeux=dict(z=(0.36, 0.46), x_min=0.20, y_min=0.02, sombre=0.14, rayon=0.9, pupille=0.38),
    gueule=dict(z=(0.27, 0.37), x_min=0.22, y_max=0.085, sombre=0.16),
    # repeinture (retours de playtest du 2026-11-16) : peau mauve pâle malade, plus sombre et saturée, aplats, encre
    repeindre=True,
    peau=dict(echelle=1.0, rampe=["#2F2530", "#4D3A50", "#6D5273", "#8E7493"], doux=0.018,
              ecchymoses_freq=9.0, ecchymoses_seuil=0.64, ecchymose="#4A1530", aureole="#7E7A46",
              craquelures_zone=0.66, craquelures_freq=40.0, craquelures_trait=0.06,
              plis=(0.30, 0.62), plis_force=0.9, encre="#160D19"),
    yeux_rouge="#5E0A10", yeux_petits=0.32, lueur_gueule=0.5,
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


def repaint(ob, px, P):
    """Texture repeinte (module peinture) et masque d'émission : yeux plus petits rouge sombre, gueule."""
    S = P["texture"]
    pos = E.rasterize(ob, S, lambda c: True)
    ok = ~np.isnan(pos[..., 0])
    rgb = px[..., :3].copy()
    h, s, v = E.hsv(rgb)
    lum = rgb @ np.array([0.3, 0.59, 0.11], np.float32)
    lum_b = W.blur2d(lum, 1)
    crease = np.clip((W.blur2d(lum, 10) - lum_b) / 0.12, 0, 1)
    x, y, z = pos[..., 0], pos[..., 1], pos[..., 2]
    with np.errstate(invalid="ignore"):
        zmin = np.nanmin(z)
        head = ok & (x > P["tete"]["x_min"]) & (z > P["tete"]["z_min"])
        cloth = ok & (h > 35) & (h < 95) & (s > 0.25) & (v > 0.1) & ~head
        teeth = head & (v > 0.55) & (s < 0.3)
        mouth = head & (v < 0.13)
        claws = ok & (v < 0.2) & (z < zmin + 0.06)
        metal = ok & (s < 0.08) & (z < zmin + 0.12) & (z > zmin + 0.03) & ~claws & (v < 0.62) & (v > 0.2)
        if metal.sum() > 50:                                     # entrave : le seul amas gris autour d'une cheville
            c0 = np.median(pos[metal], 0)
            metal &= np.linalg.norm(np.nan_to_num(pos, nan=9.0) - c0, axis=-1) < 0.06
    skin = ok & ~(cloth | teeth | mouth | claws | metal)
    out = rgb.copy()
    SK = dict(P["peau"])
    for k in ("ecchymose", "aureole", "encre"):
        SK[k] = W.hexrgb(SK[k])
    SK["rampe"] = [W.hexrgb(c) for c in SK["rampe"]]
    out[skin] = W.skin(pos[skin], W.norm_lum(lum_b, skin)[skin], crease[skin], SK)
    ink = SK["encre"]
    for m in (cloth, metal, teeth, claws):
        if m.any():
            f = W.flat(rgb[m], W.norm_lum(lum_b, m)[m])
            k = W.smoothstep(0.35, 0.7, crease[m])[:, None] * 0.85
            out[m] = f * (1 - k) + ink * k
    out[mouth] = rgb[mouth] * 0.3 + W.hexrgb("#120A0E") * 0.7
    # yeux : plus petits, rouge sombre peint, liseré d'encre ; émission sur le même disque, gueule plus faible
    mask = np.zeros(lum.shape, np.float32)
    Y = P["yeux"]
    with np.errstate(invalid="ignore"):
        eye_px = ok & (z > Y["z"][0]) & (z < Y["z"][1]) & (x > Y["x_min"]) & (np.abs(y) > Y["y_min"]) & (lum < Y["sombre"])
    info = {}
    red = W.hexrgb(P["yeux_rouge"])
    for side, sgn in (("droit", -1), ("gauche", 1)):
        pts = pos[eye_px & (np.sign(y) == sgn)]
        if len(pts) < 10:
            info[side] = "introuvable"; continue
        c = pts.mean(0)
        r = np.percentile(np.linalg.norm(pts - c, axis=1), 85) * Y["rayon"] * P["yeux_petits"]
        d = np.linalg.norm(np.nan_to_num(pos, nan=9.0) - c, axis=-1)
        disk = np.clip((r - d) / (0.12 * r), 0, 1) * ok
        ring = np.clip((r * 1.3 - d) / (0.1 * r), 0, 1) * ok * (1 - disk)
        out = out * (1 - ring[..., None]) + ink * ring[..., None]
        out = out * (1 - disk[..., None]) + red * disk[..., None]
        mask = np.maximum(mask, disk)
        info[side] = "centre (%.3f, %.3f, %.3f), rayon %.4f" % (c[0], c[1], c[2], r)
    G = P["gueule"]
    with np.errstate(invalid="ignore"):
        gap = ok & (z > G["z"][0]) & (z < G["z"][1]) & (x > G["x_min"]) & (np.abs(y) < G["y_max"]) & (lum < G["sombre"])
    mask = np.maximum(mask, np.clip(W.blur2d(gap.astype(np.float32), 1) * 2.0, 0, 1) * P["lueur_gueule"])
    out = W.dilate(np.clip(out, 0, 1), ok, 48)
    mask = W.dilate(mask[..., None], ok, 48)[..., 0]
    info["zones"] = "peau %d, pagne %d, entrave %d, dents %d, griffes %d, gueule %d texels" % tuple(
        int(m.sum()) for m in (skin, cloth, metal, teeth, claws, mouth))
    base = px.copy(); base[..., :3] = out
    return base, mask, info


def build(P):
    ob = E.import_glb(P["glb"])
    S = P["texture"]
    px = E.source_pixels(ob, S)
    T = P["tete"]
    pos = E.rasterize(ob, S, lambda c: c.x > T["x_min"] and c.z > T["z_min"])
    part = 0.0
    if P["repeindre"]:
        base, mask, info = repaint(ob, px, P)
    else:
        mask, info = masks(px, pos, P)
        base = px
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
    ob.name = ob.data.name = "SM_Goule_LOD0"
    lods = [ob]
    for i, target in enumerate(P["lod"], 1):
        o = ob.copy(); o.data = ob.data.copy()
        o.name = o.data.name = "SM_Goule_LOD%d" % i
        for col in ob.users_collection:
            col.objects.link(o)
        a, b = E.weld_decimate(o, target, symmetry="Y")
        E.smooth_shading(o, 60)
        print("LOD%d : %d -> %d triangles" % (i, a, b))
        o.hide_render = True                                      # rendus Blender : LOD0 seul (exportés quand même)
        lods.append(o)
    print("goule : %d -> %d triangles ; échelle × %.4f ; taille (x, y, z) = (%.3f, %.3f, %.3f) m ; peau recolorée %.1f %%"
          % (t0, t1, k, size[0], size[1], size[2], 100 * part))
    for kk, v in info.items():
        print("  %s : %s" % (kk, v))
    return lods


if __name__ == "__main__":
    C.run_piece("SM_Goule", build, PARAMS, "SM_Goule", views=("face", "cote", "34"))
