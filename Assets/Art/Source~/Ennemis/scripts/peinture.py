# -*- coding: utf-8 -*-
"""Peinture des textures des ennemis selon la charte (rendu peint, aplats, encre), au texel près.

Tout est calculé dans l'espace 3D du modèle (position de chaque texel, par la rastérisation des UV d'ennemis_common) :
les motifs (ecchymoses, craquelures, coups de pinceau) sont continus d'un îlot UV à l'autre, et nets à la résolution de
la texture. La texture d'origine (Tripo, floue) ne sert qu'à lire les volumes (sa luminance : creux, côtes, muscles) et
les zones (peau, tissu, métal, dents, griffes) ; les couleurs sont repeintes.

Fonctions :
- vnoise, fbm : bruit de valeurs 3D (réseau haché, interpolation lissée) ;
- cracks : craquelures (distance F2 − F1 d'un diagramme de Voronoï 3D) ;
- skin : peau en aplats (rampe de 4 couleurs), coups de pinceau, ecchymoses, craquelures et plis à l'encre ;
- flat : autres matières en aplats (valeur en paliers, teinte gardée) ;
- dilate : marges des îlots UV remplies (pas de couture aux niveaux de mip).
"""
import numpy as np

_P1, _P2, _P3 = 73856093, 19349663, 83492791


def _hash(ix, iy, iz, seed):
    h = (ix * _P1) ^ (iy * _P2) ^ (iz * _P3) ^ (seed * 2654435761)
    h = (h ^ (h >> 13)) * 1274126177
    h = h ^ (h >> 16)
    return (h & 0xFFFFFF).astype(np.float32) / float(0xFFFFFF)


def vnoise(P, freq, seed=0, stretch=(1.0, 1.0, 1.0)):
    """Bruit de valeurs 3D dans [0, 1] ; P : (N, 3) ; stretch étire le motif par axe (coups de pinceau)."""
    p = P * (np.asarray(stretch, np.float32) * freq)
    i = np.floor(p).astype(np.int64)
    f = p - i
    f = f * f * (3 - 2 * f)
    out = np.zeros(len(P), np.float32)
    for dx in (0, 1):
        wx = f[:, 0] if dx else 1 - f[:, 0]
        for dy in (0, 1):
            wy = f[:, 1] if dy else 1 - f[:, 1]
            for dz in (0, 1):
                wz = f[:, 2] if dz else 1 - f[:, 2]
                out += wx * wy * wz * _hash(i[:, 0] + dx, i[:, 1] + dy, i[:, 2] + dz, seed)
    return out


def fbm(P, freq, seed=0, octaves=4, stretch=(1.0, 1.0, 1.0)):
    v, a, s = np.zeros(len(P), np.float32), 0.5, 0.0
    for o in range(octaves):
        v += a * vnoise(P, freq * (2 ** o), seed + 31 * o, stretch)
        s += a; a *= 0.5
    return v / s


def cracks(P, freq, seed=0, chunk=400000):
    """Distance au bord de cellule (F2 − F1) d'un Voronoï 3D, en unités de cellule : petite valeur = sur une fissure."""
    out = np.empty(len(P), np.float32)
    offs = np.array([(a, b, c) for a in (-1, 0, 1) for b in (-1, 0, 1) for c in (-1, 0, 1)], np.int64)
    for s0 in range(0, len(P), chunk):
        p = P[s0:s0 + chunk] * freq
        i = np.floor(p).astype(np.int64)
        f1 = np.full(len(p), 9.0, np.float32); f2 = np.full(len(p), 9.0, np.float32)
        for o in offs:
            c = i + o
            jit = np.stack([_hash(c[:, 0], c[:, 1], c[:, 2], seed + k) for k in range(3)], 1)
            d = np.linalg.norm(c + jit - p, axis=1).astype(np.float32)
            f2 = np.where(d < f1, f1, np.minimum(f2, d))
            f1 = np.minimum(f1, d)
        out[s0:s0 + chunk] = f2 - f1
    return out


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[k:k + 2], 16) / 255.0 for k in (0, 2, 4)], np.float32)


def blur2d(a, k):
    out = a.copy()
    for ax in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-k, k + 1):
            acc += np.roll(out, d, axis=ax)
        out = acc / (2 * k + 1)
    return out


def norm_lum(lum, sel, lo=4, hi=96):
    a, b = np.percentile(lum[sel], [lo, hi])
    return np.clip((lum - a) / max(b - a, 1e-4), 0, 1)


def posterize(t, ramp, soft=0.025, cuts=None):
    """t dans [0, 1] -> couleur d'une rampe de n teintes, paliers à bords nets (soft : largeur du fondu)."""
    n = len(ramp)
    cuts = cuts or [(k + 1) / n for k in range(n - 1)]
    col = np.repeat(ramp[0][None], len(t), 0)
    for k, c in enumerate(cuts):
        w = smoothstep(c - soft, c + soft, t)[:, None]
        col = col * (1 - w) + ramp[k + 1][None] * w
    return col


def skin(P, lum, crease, S):
    """Peau peinte. P : positions (N, 3) ; lum : luminance d'origine normalisée [0, 1] ; crease : creux d'origine
    (0 à 1, plis et côtes) ; S : réglages (rampe, échelles, couleurs). Renvoie (N, 3) sRGB."""
    sc = S["echelle"]                                         # taille du modèle brut (m de modèle par unité)
    brush = fbm(P, 3.0 / sc, 11, 3, stretch=(1.0, 1.0, 0.25))  # traînées verticales larges
    brush_f = vnoise(P, 40.0 / sc, 12, stretch=(1.0, 1.0, 0.2))
    t = 0.72 * lum + 0.28 * fbm(P, 2.2 / sc, 13, 3) + 0.10 * (brush - 0.5) + 0.06 * (brush_f - 0.5)
    col = posterize(np.clip(t, 0, 1), S["rampe"], soft=S.get("doux", 0.02))
    # ecchymoses : taches sombres violacées, auréole jaune-vert maladive
    b = fbm(P + 3.7, S["ecchymoses_freq"] / sc, 21, 4)
    core = smoothstep(S["ecchymoses_seuil"], S["ecchymoses_seuil"] + 0.035, b)
    halo = smoothstep(S["ecchymoses_seuil"] - 0.06, S["ecchymoses_seuil"] - 0.01, b) * (1 - core)
    col = col * (1 - 0.55 * core[:, None]) + S["ecchymose"][None] * (0.55 * core[:, None])
    col = col * (1 - 0.22 * halo[:, None]) + S["aureole"][None] * (0.22 * halo[:, None])
    # craquelures à l'encre, par plaques
    zone = smoothstep(S["craquelures_zone"], S["craquelures_zone"] + 0.06, fbm(P + 9.1, 1.6 / sc, 31, 3))
    d = cracks(P, S["craquelures_freq"] / sc, 41)
    w = S["craquelures_trait"] * (0.35 + 0.9 * vnoise(P, 30.0 / sc, 42))     # trait de pinceau d'épaisseur variable
    broken = smoothstep(0.38, 0.52, vnoise(P, 22.0 / sc, 43))                # fissures interrompues
    line = (1 - smoothstep(w * 0.5, w, d)) * zone * broken
    # plis et creux d'origine, redessinés en traits d'encre nets
    ink_c = smoothstep(S["plis"][0], S["plis"][1], crease)
    ink = np.clip(np.maximum(line, ink_c * S["plis_force"]), 0, 1)
    col = col * (1 - ink[:, None]) + S["encre"][None] * ink[:, None]
    return np.clip(col, 0, 1)


def flat(rgb, lum, levels=3, lo=0.6, hi=1.2):
    """Aplats pour les matières gardées : valeur ramenée à quelques paliers autour de la médiane de la matière (teinte
    et saturation conservées)."""
    v = rgb.max(-1)
    m = float(np.median(v)) if len(v) else 0.5
    q = (np.floor(np.clip(lum, 0, 0.999) * levels) + 0.5) / levels
    newv = m * (lo + (hi - lo) * q)
    return np.clip(rgb / np.maximum(v, 1e-4)[:, None] * newv[:, None], 0, 1)


def dilate(img, ok, n=8):
    """Remplit les texels hors des triangles (ok faux) par la moyenne des voisins valides, n passes."""
    img = img.copy(); ok = ok.copy()
    for _ in range(n):
        acc = np.zeros_like(img); cnt = np.zeros(ok.shape, np.float32)
        for ax, d in ((0, 1), (0, -1), (1, 1), (1, -1)):
            acc += np.roll(img * ok[..., None], d, axis=ax); cnt += np.roll(ok.astype(np.float32), d, axis=ax)
        new = (~ok) & (cnt > 0)
        img[new] = acc[new] / cnt[new][:, None]
        ok = ok | new
    return img
