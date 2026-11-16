# -*- coding: utf-8 -*-
"""Texture peinte des bras FPS (une par personnage, partagée par les deux bras : le gauche est le miroir du droit).

Le maillage construit par sk_brasfps reçoit un dépliage « Peinture » (projection intelligente), puis chaque texel est
peint dans l'espace 3D de la main (repère d'ART-HANDOFF : x vers l'auriculaire de la main droite, y dos de la main, z vers
les doigts) selon la matière de sa face (clé du matériau en aplat) : cuir (marbrure, éraflures, usure), tissu (trame, plis,
bord effiloché), fourrure et duvet (mèches), écailles (cellules), plumes (barres), métal (laiton peint), griffes
(dégradé), peau (plis d'encre aux articulations). Coutures peintes en pointillés. Les couleurs sont celles de la planche,
relevées pour rester lisibles dans l'ambiance sombre (valeur minimale), sans émission.
"""
import os, sys, math
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Ennemis", "scripts"))
sys.modules.pop("peinture", None)
import peinture as W

LEATHER = ("gant", "cuir", "sangle")
FABRIC = ("manche", "lisere", "ourlet", "rayure1", "rayure2", "bandage", "fond")
FUR = ("fourrure", "poil", "duvet")


def raster(me, uv_name, size):
    """Texel -> (position 3D Blender, indice de matériau) ; NaN / -1 hors des triangles."""
    me.calc_loop_triangles()
    uv = me.uv_layers[uv_name].data
    pos = np.full((size, size, 3), np.nan, np.float32)
    mid = np.full((size, size), -1, np.int16)
    for tri in me.loop_triangles:
        P = np.array([me.vertices[i].co[:] for i in tri.vertices], np.float32)
        U = np.array([uv[l].uv[:] for l in tri.loops], np.float64) * size
        x0, y0 = np.maximum(np.floor(U.min(0)).astype(int), 0)
        x1, y1 = np.minimum(np.ceil(U.max(0)).astype(int), size - 1)
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        (ax, ay), (bx, by), (cx, cy) = U
        d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(d) < 1e-12:
            continue
        l0 = ((by - cy) * (xs - cx) + (cx - bx) * (ys - cy)) / d
        l1 = ((cy - ay) * (xs - cx) + (ax - cx) * (ys - cy)) / d
        l2 = 1 - l0 - l1
        inside = (l0 >= -0.02) & (l1 >= -0.02) & (l2 >= -0.02)
        if not inside.any():
            continue
        q = l0[..., None] * P[0] + l1[..., None] * P[1] + l2[..., None] * P[2]
        yy, xx = np.nonzero(inside)
        pos[y0 + yy, x0 + xx] = q[yy, xx]
        mid[y0 + yy, x0 + xx] = tri.material_index
    return pos, mid


def hand_space(B, elbow):
    """Blender (UH) -> repère de la main : x = -bx, y = bz, z = -by + elbow."""
    return np.stack([-B[:, 0], B[:, 2], -B[:, 1] + elbow], 1)


def lift(rgb, vmin):
    """Valeur relevée (lisibilité dans le noir) en gardant la teinte."""
    v = rgb.max(-1, keepdims=True)
    k = np.where(v < vmin, vmin / np.maximum(v, 1e-4), 1.0)
    return np.clip(rgb * k, 0, 1)


def paint(key, H, base, joints, seams, sc=1.0):
    """H : positions dans le repère de la main (N, 3) ; base : couleur (3,) ; renvoie (N, 3)."""
    n = len(H)
    col = np.repeat(base[None], n, 0)
    ink = np.array([0.08, 0.06, 0.07], np.float32)
    if key in LEATHER:
        mott = W.fbm(H, 60.0, 3, 3)
        col = col * (0.82 + 0.36 * mott[:, None])
        scr = W.cracks(H, 140.0, 5)
        scratch = (1 - W.smoothstep(0.015, 0.04, scr)) * W.smoothstep(0.72, 0.82, W.vnoise(H, 50.0, 6))      # éraflures rares
        col = col * (1 - 0.35 * scratch[:, None]) + np.array([0.75, 0.62, 0.5]) * 0.35 * scratch[:, None] * 0.6
    elif key in FABRIC:
        weave = W.vnoise(H, 900.0, 11, stretch=(1, 1, 0.15)) * 0.5 + W.vnoise(H, 900.0, 12, stretch=(0.15, 1, 1)) * 0.5
        folds = W.fbm(H, 45.0, 13, 3, stretch=(1.0, 1.0, 0.3))
        col = col * (0.78 + 0.18 * weave[:, None] + 0.3 * (folds[:, None] - 0.5) + 0.12)
        fray = W.smoothstep(-0.185, -0.162, H[:, 2]) * (W.vnoise(H, 300.0, 14) > 0.55)
        col = col * (1 - 0.35 * fray[:, None])
    elif key in FUR:
        strands = W.vnoise(H, 700.0, 21, stretch=(1.0, 1.0, 0.12))
        clumps = W.fbm(H, 90.0, 22, 2)
        col = col * (0.7 + 0.45 * strands[:, None] + 0.2 * (clumps[:, None] - 0.5))
    if key == "ecaille":
        d = W.cracks(H, 260.0, 31)
        cell = W.vnoise(H, 260.0, 32)
        col = col * (0.85 + 0.25 * cell[:, None])
        col = col * (1 - 0.55 * (1 - W.smoothstep(0.03, 0.09, d))[:, None])
    elif key.startswith("plume"):
        bar = 0.5 + 0.5 * np.sin(H[:, 2] * 900.0 + W.vnoise(H, 120.0, 41) * 3)
        col = col * (0.8 + 0.3 * bar[:, None])
    elif key == "boucle":
        sh = W.vnoise(H, 400.0, 51)
        col = col * (0.75 + 0.5 * (sh[:, None] > 0.55))
    elif key == "griffe":
        col = col * (0.9 + 0.4 * W.vnoise(H, 200.0, 61)[:, None])
    elif key == "peau":
        col = col * (0.9 + 0.18 * W.fbm(H, 120.0, 71, 2)[:, None])
    # plis d'encre aux articulations des doigts (peau, gant, écailles)
    if key in ("peau", "gant", "ecaille") and joints:
        line = np.zeros(n, np.float32)
        for J, D, r in joints:
            rel = H - J
            along = rel @ D
            radial = np.linalg.norm(rel - along[:, None] * D, axis=1)
            line = np.maximum(line, (1 - W.smoothstep(0.0004, 0.0009, np.abs(along))) * (radial < r * 1.4))
        col = col * (1 - 0.75 * line[:, None]) + ink * 0.75 * line[:, None]
    # coutures peintes (pointillés clairs) sur le gant et la manchette
    if key == "gant" and seams is not None:
        sk = seams["couleur"]
        hit = np.zeros(n, np.float32)
        for (a, b) in seams["segments"]:
            ab = b - a; L2 = float(ab @ ab)
            t = np.clip(((H - a) @ ab) / L2, 0, 1)
            q = H - (a + t[:, None] * ab)
            dist = np.hypot(q[:, 0], q[:, 2]) + 10.0 * (H[:, 1] < 0.008)      # sur le dos de la main seulement
            dash = (np.mod(t * math.sqrt(L2) / 0.004, 1.0) < 0.55)
            hit = np.maximum(hit, (dist < 0.0007) * dash)
        ring = (np.abs(H[:, 2] + 0.0335) < 0.0006) & (np.mod(np.arctan2(H[:, 1], H[:, 0]) * 40 / math.pi, 1.0) < 0.55)
        hit = np.maximum(hit, ring)                                            # tour de la manchette
        col = col * (1 - hit[:, None]) + sk * hit[:, None]
    return np.clip(col, 0, 1)
