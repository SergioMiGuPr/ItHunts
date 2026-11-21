# -*- coding: utf-8 -*-
"""Personnages assis du campement : ui_assis_buche_<perso>_v01 (fond gris) détourés, caisse retirée, lanterne séparée,
mis à l'échelle du renard (place 1) et du hibou (place 2) de la maquette, lumière de la scène (feu orange à droite, lune
froide au-dessus, valeurs sombres). Sorties : Assets/Art/UI/Menus/Campement/Campement_Assis_<Perso>.png,
Campement_Assis_<Perso>_Lanterne.png et l'entrée "assis" de Campement_Layout.json.

Usage : python assis.py [dossier_controle]
"""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg
from reg import blur, erode, fill_holes, keep_large, smoothstep

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
UI = os.path.join(ROOT, "docs/art/ui")
OUT = os.path.join(ROOT, "Assets/Art/UI/Menus/Campement")
CTRL = sys.argv[1] if len(sys.argv) > 1 else None
K = 2  # traitement à mi-définition (les personnages finissent à ~0,25x)

# ---- cibles mesurées sur la maquette (pixels de la maquette 1670 x 941)
# haut de la capuche (y), centre des épaules (x), largeur d'épaules, écart capuche -> ligne d'épaules
CIBLES = {1: {"capuche": 352, "cx": 569, "epaules": 126, "dy": 98, "lanterne": (510, 676)},   # renard sur la bûche
          2: {"capuche": 346, "cx": 807, "epaules": 126, "dy": 86, "lanterne": (918, 596)}}   # hibou sur la pierre
# ---- mesures dans les images (pleine définition 2752 x 1536)
# capuche : haut de la capuche (y) ; caisse : graines (x, y) sur ses faces ; lanterne : rect (x0, y0, x1, y1) de la
# lanterne gardée, le pied de la lanterne est le bas du rect
PERSOS = {
    "Renard": {"capuche": 131, "caisse": [(948, 940), (1875, 876), (1890, 1360), (1285, 1405), (948, 1345)],
               "lanterne": (1079, 1098, 1297, 1502), "couleur": (0.87, 0.22, 0.25), "texture": True, "couleurs": True},
    "Koala": {"capuche": 121, "caisse": [(722, 978), (1100, 900), (1795, 905), (1795, 1260), (1040, 1378), (722, 1312)],
              "lanterne": (1818, 1045, 2045, 1445), "couleur": (0.33, 0.78, 0.32)},
    "Cameleon": {"capuche": 79, "caisse": [(664, 985), (1100, 925), (1880, 912), (1885, 1242), (1050, 1388), (664, 1288)],
                 "lanterne": (1036, 1036, 1245, 1491), "couleur": (0.87, 0.63, 0.25), "texture": True},
    "Hibou": {"capuche": 136, "caisse": [(215, 1003), (760, 1010), (1065, 1030), (1072, 1250), (645, 1405), (215, 1332)],
              "lanterne": (1304, 1036, 1532, 1464), "couleur": (0.37, 0.73, 0.93), "xmax": 1600,
              # l'aile déployée élargit la silhouette à hauteur d'épaules : échelle prise à largeur de capuche égale
              # (capuche 429 px dans l'image, 110 px sur le hibou de la maquette)
              "echelle": 0.256},
}


def bg_model(I):
    """Fond gris (dégradé, halo orange éventuel) : convolution normalisée des pixels de fond probables."""
    S, V = reg.sat(I)
    L = reg.gray(I)
    g0 = np.median(L)
    m = (np.abs(L - g0) < 0.08) & (S < 0.25)
    for _ in range(3):
        num = reg.blur(I * m[..., None], 30.0); den = reg.blur(m.astype(np.float32), 30.0)[..., None]
        B = num / np.maximum(den, 1e-4)
        e = reg.err_map(I, B)
        m = e < 0.035
    return B


def silhouette(I, B):
    e = reg.blur(reg.err_map(I, B), 0.8)
    fg = reg.smoothstep(0.035, 0.09, e)
    core = fg > 0.5
    core = reg.erode(reg.dilate(core.astype(np.float32), 2), 2) > 0.5
    small = reg.keep_large(core[::2, ::2], 2000)
    core = np.repeat(np.repeat(small, 2, 0), 2, 1)[:core.shape[0], :core.shape[1]] & core
    core = reg.fill_holes(core)
    return core, fg


def remove_seat(I, core, poly, neutre_texture=False, lanterne=None, couleurs_perso=False):
    """Dans le contour de la caisse : faces claires (grises, même teintées d'orange par la lanterne) retirées, puis les
    traits d'encre qui ne touchent pas une partie épaisse du personnage."""
    h, w = core.shape
    P = reg.poly_mask(h, w, np.array(poly, np.float64) / K, aa=1.0) > 0.5
    P = reg.dilate(P.astype(np.float32), 3) > 0.5
    S, V = reg.sat(I)
    r, g, b = I[..., 0], I[..., 1], I[..., 2]
    neutral = (S < 0.15) & (V > 0.36)
    orange = (V > 0.66) & (S < 0.74) & (r >= g) & (g >= b)
    gx, gy = reg.grads(reg.gray(I))
    smooth = reg.blur(np.hypot(gx, gy), 1.5) < 0.018
    # faces teintées par la lueur de la lanterne (rose, vert...) : peu saturées, claires, hors de la lanterne
    lit = (S < 0.5) & (V > 0.40)
    if lanterne is not None:
        lx0, ly0, lx1, ly1 = [v // K for v in lanterne]
        lit[ly0:ly1, lx0:lx1] = False
    face = ((neutral & (smooth | neutre_texture)) | (orange & smooth) | (lit & neutre_texture)) & P & core
    face = reg.keep_large(face[::2, ::2], 250)
    face = np.repeat(np.repeat(face, 2, 0), 2, 1)[:h, :w]
    for _ in range(3):  # bord des faces (le lissage les rogne)
        face = (reg.dilate(face.astype(np.float32), 1) > 0.5) & (neutral | orange) & P
    if couleurs_perso:
        # caisse en planches (grain du bois) : tout ce qui n'est ni saturé ni sombre est de la caisse, ainsi que ses
        # faces lisses rougies par la lanterne (hors de la lanterne)
        face |= P & core & ~((S > 0.30) | (V < 0.28))
        red_lit = smooth & (V > 0.30) & (S < 0.80)
        if lanterne is not None:
            red_lit[ly0:ly1, lx0:lx1] = False
        face |= P & core & red_lit
    seat = P & core & face
    rest = core & ~seat
    thick = reg.erode(rest.astype(np.float32), 3) > 0.5
    thick = reg.keep_large(thick[::2, ::2], 300)
    thick = np.repeat(np.repeat(thick, 2, 0), 2, 1)[:h, :w]
    near = reg.dilate(thick.astype(np.float32), 4) > 0.5
    char = rest & (near | ~P)
    char = reg.keep_large(char[::2, ::2], 1500)
    char = np.repeat(np.repeat(char, 2, 0), 2, 1)[:h, :w] & rest & (near | ~P)
    return char, seat | (P & core & ~char)


def up(m, shape):
    return np.repeat(np.repeat(m, K, 0), K, 1)[:shape[0], :shape[1]]


def width_at(mask, y):
    xs = np.nonzero(mask[int(round(y))])[0]
    return float(xs.max() - xs.min()) if len(xs) else 0.0, (float(xs.min() + xs.max()) / 2 if len(xs) else 0.0)


def solve_scale(mask, hood, cible):
    """Échelle s telle que la largeur de la silhouette, prise à dy/s sous le haut de la capuche, vaille la largeur
    d'épaules de la maquette."""
    best = None
    for s_ in np.linspace(0.40, 0.68, 281):  # plage plausible (0,20 à 0,34 en pleine définition)
        y = hood + cible["dy"] / s_
        if y >= mask.shape[0]:
            continue
        wd, cx = width_at(mask, y)
        err = abs(wd * s_ - cible["epaules"])
        if best is None or err < best[0]:
            best = (err, s_, cx, y)
    return best[1], best[2], best[3]


def maquette_stats(T, V, box):
    """Valeurs du personnage de la maquette (pixels qui diffèrent du campement vide, dans son rect)."""
    x0, y0, x1, y1 = box
    d = reg.blur(reg.err_map(T, V), 1.5)[y0:y1, x0:x1] > 0.14
    px = T[y0:y1, x0:x1][d]
    L = reg.gray(px[None])[0]
    return {"p10": float(np.percentile(L, 10)), "p50": float(np.percentile(L, 50)), "p90": float(np.percentile(L, 90)),
            "moy": px.mean(0)}


def grade(F, a, st, scale_px):
    """Lumière de la scène : valeurs ramenées à celles du personnage de la maquette (deux points), teinte moyenne
    rapprochée, liseré chaud du feu à droite, froid de la lune au-dessus."""
    m = a > 0.5
    L = reg.gray(np.clip(F, 1e-3, 1))
    p50, p90 = np.percentile(L[m], [50, 90])
    t50, t90 = st["p50"], st["p90"]
    g = (np.log(t90) - np.log(t50)) / max(np.log(p90) - np.log(p50), 1e-3)
    k = t50 / p50 ** g
    Fg = k * np.clip(F, 1e-3, 1) ** g
    # teinte : 35 % vers la couleur moyenne de la maquette (le feu et la nuit)
    mean_now = Fg[m].mean(0)
    tint = st["moy"] / np.maximum(mean_now, 1e-3)
    tint = tint / max(reg.gray(tint[None, None])[0, 0], 1e-3)
    Fg = Fg * (1 + 0.35 * (tint[None, None, :] - 1))
    # liserés : bord droit (feu) et bord haut (lune), en pixels de l'image finale
    sh = max(int(round(3.0 / scale_px)), 2)
    am = (a > 0.3).astype(np.float32)
    right = am * (1 - np.roll(am, -sh, axis=1))
    top = am * (1 - np.roll(am, sh, axis=0))
    rr = reg.blur(right, sh * 0.8) * am
    tt = reg.blur(top, sh * 0.8) * am
    fire = np.array([1.0, 0.55, 0.22]); moon = np.array([0.55, 0.65, 1.0])
    # le liseré du feu est maintenant dans le shader (côté du feu selon la place, palpite avec le feu) : seul le liseré
    # froid de la lune reste peint
    Fg = Fg + rr[..., None] * fire * 0.0 + tt[..., None] * moon * 0.0   # plus de liseré peint (2026-11-22)
    return np.clip(Fg, 0, 1)


def detoure(rgba, erosion=0):
    """Détourage propre d'un sprite réduit (alpha droit), lot du 2026-11-22 :
    - taches isolées et petits trous retirés ;
    - contour lissé puis anticrénelé : alpha = seuil doux (1 px) du masque flouté, ce qui arrondit les marches d'escalier
      (découpe droite de la caisse, bas de la cape) sans ronger les mèches ;
    - couleurs du bord décontaminées : tout pixel du bord (alpha < 1 ou à 1 px du contour) prend la couleur de l'intérieur
      voisin (plus aucun liseré clair ni orange venu du fond gris de l'image source) ; l'encre intérieure reste."""
    a = rgba[..., 3]
    rgb = rgba[..., :3].copy()
    m = a > 0.5
    m = keep_large(m, 400)
    trous = fill_holes(m) & ~m
    m = m | (trous & ~keep_large(trous, 40))
    for _ in range(int(erosion)):
        m = erode(m.astype(np.float32), 1) > 0.5
    # contour : flou puis seuil doux, transition d'environ 1 px
    b = blur(m.astype(np.float32), 0.9)
    an = smoothstep(0.32, 0.68, b)
    # intérieur sûr : opaque dans la source et à plus de 1 px du contour
    inner = (erode(m.astype(np.float32), 1) > 0.5) & (a > 0.97)
    w = inner.astype(np.float32)
    acc = rgb * w[..., None]
    known = w.copy()
    for _ in range(6):
        s = blur(acc, 1.0); n = blur(known, 1.0)
        fill = (known < 0.5) & (n > 1e-3)
        acc[fill] = s[fill] / n[fill, None]
        known = np.where(fill, 1.0, known)
    rgb = np.where(inner[..., None], rgb, acc)
    return np.concatenate([np.clip(rgb, 0, 1), an[..., None]], -1)


def composantes(m):
    """Étiquetage 4-connexe d'un masque binaire : (étiquettes, nombre)."""
    lab = np.zeros(m.shape, np.int32)
    H, W = m.shape
    cur = 0
    ys, xs = np.nonzero(m)
    for y0, x0 in zip(ys, xs):
        if lab[y0, x0]:
            continue
        cur += 1
        stack = [(y0, x0)]; lab[y0, x0] = cur
        while stack:
            y, x = stack.pop()
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < H and 0 <= xx < W and m[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = cur; stack.append((yy, xx))
    return lab, cur


def extrait_lanterne(I, rect, marge=40):
    """Lanterne seule (2026-11-22), délimitée par son contour encré : encre (sombre) -> trous bouchés -> les régions
    intérieures peu saturées (caisse, sol, intérieur de l'anneau) redeviennent du fond, le verre teinté reste ; ouverture
    morphologique pour couper les traits fins du décor qui touchent la lanterne (bords de la caisse), puis reconstruction
    du contour près du corps de la lanterne. Rend l'alpha pleine image (doux, 1 px) et le rect (x0, y0, x1, y1)."""
    H, W = I.shape[:2]
    x0, y0, x1, y1 = rect
    X0, Y0, X1, Y1 = max(x0 - marge, 0), max(y0 - marge, 0), min(x1 + marge, W), min(y1 + marge // 2, H)
    X = I[Y0:Y1, X0:X1]
    L = reg.gray(X)
    S, V = reg.sat(X)
    ink = reg.blur(L, 0.8) < 0.30
    # fermeture : bouche les coupures du contour (reflets, lueur) avant de remplir
    ink = reg.erode(reg.dilate(ink.astype(np.float32), 4), 4) > 0.5
    solid = fill_holes(ink)
    lab, n = composantes(solid & ~ink)
    fond = ~solid
    fond0 = fond.copy()
    for k in range(1, n + 1):
        c = lab == k
        if S[c].mean() < 0.16:
            fond |= c
            continue
        # trou de la couleur du fond voisin (intérieur de l'anneau éclairé par la lueur) : fond aussi
        ys, xs = np.nonzero(c)
        if len(ys) < 3000:
            yy0, yy1, xx0, xx1 = max(ys.min() - 40, 0), ys.max() + 40, max(xs.min() - 40, 0), xs.max() + 40
            voisin = fond0[yy0:yy1, xx0:xx1]
            if voisin.sum() > 50:
                d = np.abs(X[c].mean(0) - X[yy0:yy1, xx0:xx1][voisin].mean(0)).max()
                if d < 0.12:
                    fond |= c
    m = ~fond
    o = reg.dilate(reg.erode(m.astype(np.float32), 5), 5) > 0.5
    lab2, n2 = composantes(o)
    if n2:
        tailles = np.bincount(lab2.ravel())[1:]
        o = lab2 == (1 + int(np.argmax(tailles)))
    m = m & (reg.dilate(o.astype(np.float32), 6) > 0.5)
    # traits fins du décor encore accrochés (bord de la caisse) : ouverture fine
    m = reg.dilate(reg.erode(m.astype(np.float32), 2), 2) > 0.5
    # bornes : enveloppe convexe du corps de la lanterne (ouverture large : sans anneau ni traits), plus une colonne
    # au-dessus pour l'anneau
    big = reg.dilate(reg.erode(m.astype(np.float32), 9), 9) > 0.5
    lab3, n3 = composantes(big)
    if n3:
        big = lab3 == (1 + int(np.argmax(np.bincount(lab3.ravel())[1:])))
        ys, xs = np.nonzero(big)
        hull = reg.convex_hull(np.stack([xs, ys], -1).astype(np.float64))
        env = reg.poly_mask(m.shape[0], m.shape[1], hull, aa=0.0) > 0.5
        env = reg.dilate(env.astype(np.float32), 5) > 0.5
        cx, lw = (xs.min() + xs.max()) / 2.0, (xs.max() - xs.min())
        col = np.zeros_like(env)
        col[:ys.min() + 5, int(cx - 0.3 * lw):int(cx + 0.3 * lw) + 1] = True
        m = m & (env | col)
    a = np.zeros((H, W), np.float32)
    a[Y0:Y1, X0:X1] = smoothstep(0.3, 0.7, reg.blur(m.astype(np.float32), 1.2))
    ys, xs = np.nonzero(a > 0.02)
    return a, (int(xs.min()) - 6, int(ys.min()) - 6, int(xs.max()) + 7, int(ys.max()) + 7)


def _ouvre_vertical(m, r):
    """Ouverture par un segment vertical de 2r+1 px : retire ce qui est plus mince que lui en hauteur."""
    out = m.copy()
    for d in range(1, r + 1):
        out &= np.roll(m, d, 0) & np.roll(m, -d, 0)
    res = out.copy()
    for d in range(1, r + 1):
        res |= np.roll(out, d, 0) | np.roll(out, -d, 0)
    return res


def retire_bandes_caisse(a, caisse, I, r=16):
    """Restes de la caisse source (2026-11-22) : le dessus de la caisse laissait une fine bande claire horizontale de chaque
    côté du personnage, à hauteur d'assise (lue comme une planche pâle sur la bûche). Dans le polygone de la caisse
    (élargi) : les zones claires et peu saturées qui touchent l'extérieur et sont plus minces que 2r+1 px en hauteur
    (pleine définition, ~9 px du sprite) sont retirées, puis les traits fins qui restent (encre des arêtes de la caisse).
    Les parties claires cernées d'encre (spirale de la queue du caméléon) ne touchent pas l'extérieur : gardées."""
    H, W = a.shape
    P = reg.poly_mask(H, W, np.array(caisse, np.float64), aa=1.0) > 0.5
    P = reg.dilate(P[::4, ::4].astype(np.float32), 5) > 0.5
    P = np.repeat(np.repeat(P, 4, 0), 4, 1)[:H, :W]
    m = a > 0.5
    S, V = reg.sat(I)
    clair = m & P & (V > 0.5) & (S < 0.3)
    exterieur = reg.dilate((~m)[::2, ::2].astype(np.float32), 2) > 0.5
    exterieur = np.repeat(np.repeat(exterieur, 2, 0), 2, 1)[:H, :W]
    lab, n = composantes(clair[::2, ::2])
    touche = np.unique(lab[exterieur[::2, ::2] & (lab > 0)])
    clair_ext = np.isin(lab, touche[touche > 0])
    clair_ext = np.repeat(np.repeat(clair_ext, 2, 0), 2, 1)[:H, :W] & clair
    mince = clair_ext & ~_ouvre_vertical(clair_ext, r)
    m2 = m & ~mince
    # traits fins restants dans la caisse (arêtes encrées) : ouverture verticale courte
    reste = m2 & P & ~_ouvre_vertical(m2, 9)
    m2 &= ~reste
    return np.where(m & ~m2, 0.0, a).astype(np.float32)


def main():
    lay_p = os.path.join(OUT, "Campement_Layout.json")
    lay = json.load(open(lay_p, encoding="utf-8"))
    lay["assis"] = {}
    T = reg.load(os.path.join(UI, "maquettes/ui_campement_v01.png"))
    Mv = np.array(lay["recalage"]["vide"])
    Vg = reg.load(os.path.join(UI, "generees/ui_campement_vide_sans_lanternes_v01.png"))
    V = reg.warp(Vg, Mv, T.shape[0], T.shape[1])
    stats = {1: maquette_stats(T, V, (420, 300, 720, 690)), 2: maquette_stats(T, V, (672, 316, 900, 640))}
    lay["assis_maquette"] = {k: {kk: (vv.tolist() if hasattr(vv, "tolist") else vv) for kk, vv in v.items()} for k, v in stats.items()}
    for name, p in PERSOS.items():
        Ifull = reg.load(os.path.join(UI, "generees/ui_assis_buche_%s_v01.png" % name.lower()))
        H, W = Ifull.shape[:2]
        I = reg.resize(Ifull, W // K, H // K)
        B = bg_model(I)
        core, fg = silhouette(I, B)
        if "xmax" in p:
            core[:, p["xmax"] // K:] = False
        char, seat = remove_seat(I, core, p["caisse"], p.get("texture", False), p["lanterne"], p.get("couleurs", False))
        if CTRL:
            vis = I * 0.4 + np.stack([seat, char, np.zeros_like(char)], -1) * 0.6
            reg.save(os.path.join(CTRL, "assis_%s.png" % name), vis[::2, ::2], "RGB")
        # alpha pleine définition : différence douce avec le fond gris, limitée au personnage
        Bf = reg.resize(B, W, H)
        e = reg.blur(reg.err_map(Ifull, Bf), 0.7)
        cf = up(char, (H, W))
        a = reg.smoothstep(0.03, 0.10, e) * (reg.dilate(cf.astype(np.float32), 2) > 0.5)
        a = np.maximum(a, reg.erode(cf.astype(np.float32), 2))
        # la lanterne : séparée (posée au pied du personnage, à l'emplacement de la place)
        lx0, ly0, lx1, ly1 = p["lanterne"]
        lm = np.zeros((H, W), np.float32); lm[ly0:ly1, lx0:lx1] = 1
        # lanterne délimitée par son contour encré (2026-11-22 : plus de morceaux de caisse ni de personnage, anneau gardé)
        a_l, _ = extrait_lanterne(Ifull, p["lanterne"])
        a_c = a * (1 - lm) * (1 - a_l)
        a_c = retire_bandes_caisse(a_c, p["caisse"], Ifull)
        # couleur démêlée du gris
        F = reg.unmix(Ifull, Bf, a)
        # échelle (mi-définition pour la mesure)
        mask_c = (a_c[::K, ::K] > 0.5)
        info = {"places": {}}
        # une seule échelle par personnage (règle des épaules du renard, place 1) : le renard et le hibou de la maquette
        # ont la même taille, et la règle appliquée au hibou (cou plus court) grossit trop les capes larges
        s_, cx, ysh = solve_scale(mask_c, p["capuche"] / K, CIBLES[1])
        s_full = p.get("echelle", s_ / K)
        for place, cible in CIBLES.items():
            wd, cxp = width_at(mask_c, (p["capuche"] + cible["dy"] / s_full) / K)
            info["places"][place] = {"echelle": s_full, "centre_epaules_x": cxp * K, "ligne_epaules_y": p["capuche"] + cible["dy"] / s_full}
        smax = max(v["echelle"] for v in info["places"].values())
        st = stats[1]
        Fg = grade(F, a_c, st, smax)
        # recadrage + réduction à l'échelle la plus grande (Unity réduit encore pour l'autre place)
        for part, alpha, rgb, suffix in (("corps", a_c, Fg, ""), ("lanterne", a_l, F, "_Lanterne")):
            bx = reg.bbox(alpha, pad=6)
            x0, y0, x1, y1 = bx
            tw, th = max(int(round((x1 - x0) * smax)), 2), max(int(round((y1 - y0) * smax)), 2)
            pre = np.concatenate([rgb[y0:y1, x0:x1] * alpha[y0:y1, x0:x1, None], alpha[y0:y1, x0:x1, None]], -1)
            small = reg.resize(pre, tw, th)
            rgba = np.concatenate([small[..., :3] / np.maximum(small[..., 3:], 1e-4), small[..., 3:]], -1)
            rgba = detoure(rgba)
            reg.save(os.path.join(OUT, "Campement_Assis_%s%s.png" % (name, suffix)), np.clip(rgba, 0, 1), "RGBA")
            info[part] = {"rect_image": [x0, y0, x1 - x0, y1 - y0], "taille_sprite": [tw, th]}
        info["capuche_image"] = p["capuche"]
        info["couleur"] = list(p["couleur"])
        lay["assis"][name] = info
        print(name, {k: round(v["echelle"], 4) for k, v in info["places"].items()}, info["corps"]["taille_sprite"])
    with open(lay_p, "w", encoding="utf-8") as f:
        json.dump(lay, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
