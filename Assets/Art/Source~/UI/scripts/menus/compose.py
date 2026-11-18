# -*- coding: utf-8 -*-
"""Composition de contrôle (même ordre et mêmes rects que le prefab Unity) : sert aux planches et à vérifier les
découpes avant Unity."""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
MEN = os.path.join(ROOT, "Assets/Art/UI/Menus")


def paste(dst, rgba, x, y, w=None, h=None, add=False):
    im = Image.fromarray((np.clip(rgba, 0, 1) * 255).astype(np.uint8), "RGBA" if rgba.shape[2] == 4 else "RGB")
    if w is not None and (int(round(w)) != im.width or int(round(h)) != im.height):
        # réduction en prémultiplié
        a = np.asarray(im, np.float32) / 255
        if a.shape[2] == 4:
            pre = np.concatenate([a[..., :3] * a[..., 3:], a[..., 3:]], -1)
        else:
            pre = a
        pre = reg.resize(pre, max(int(round(w)), 1), max(int(round(h)), 1))
        if pre.shape[2] == 4:
            rgba = np.concatenate([pre[..., :3] / np.maximum(pre[..., 3:], 1e-4), pre[..., 3:]], -1)
        else:
            rgba = pre
    H, W = dst.shape[:2]
    x, y = int(round(x)), int(round(y))
    hh, ww = rgba.shape[:2]
    sx0, sy0 = max(0, -x), max(0, -y)
    dx0, dy0 = max(0, x), max(0, y)
    dx1, dy1 = min(W, x + ww), min(H, y + hh)
    if dx1 <= dx0 or dy1 <= dy0:
        return
    src = rgba[sy0:sy0 + dy1 - dy0, sx0:sx0 + dx1 - dx0]
    if add:
        dst[dy0:dy1, dx0:dx1] = np.clip(dst[dy0:dy1, dx0:dx1] + src[..., :3], 0, 1)
    elif src.shape[2] == 4:
        a = src[..., 3:]
        dst[dy0:dy1, dx0:dx1] = dst[dy0:dy1, dx0:dx1] * (1 - a) + src[..., :3] * a
    else:
        dst[dy0:dy1, dx0:dx1] = src


def piece(dst, folder, name, rect, add=False, size=None):
    p = reg.load_rgba(os.path.join(MEN, folder, name + ".png"))
    x, y, w, h = rect
    if size:
        w, h = size
    paste(dst, p, x, y, w, h, add)


def assis_rects(lay, name, place):
    """Rect (x, y, w, h) du corps et de la lanterne d'un personnage à une place (lignes « place » de construction.txt,
    les mêmes que dans Unity)."""
    for l in open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "construction.txt"), encoding="utf-8"):
        t = l.split()
        if t and t[0] == "place" and t[1] == name and int(t[2]) == place:
            v = [float(x) for x in t[3:]]
            return tuple(v[:4]), tuple(v[4:])
    a = lay["assis"][name]
    pl = a["places"][str(place)] if str(place) in a["places"] else a["places"][place]
    cib = {1: {"capuche": 352, "cx": 569, "lanterne": (510, 676)}, 2: {"capuche": 346, "cx": 807, "lanterne": (918, 596)}}[place]
    s = pl["echelle"]
    rx, ry, rw, rh = a["corps"]["rect_image"]
    x = cib["cx"] - (pl["centre_epaules_x"] - rx) * s
    y = cib["capuche"] - (a["capuche_image"] - ry) * s
    body = (x, y, rw * s, rh * s)
    lx, ly, lw, lh = a["lanterne"]["rect_image"]
    lant = (cib["lanterne"][0] - lw * s / 2, cib["lanterne"][1] - lh * s, lw * s, lh * s)
    return body, lant


def campement(state):
    """state : {place: perso ou None}."""
    lay = json.load(open(os.path.join(MEN, "Campement/Campement_Layout.json"), encoding="utf-8"))
    P = lay["pieces"]
    img = reg.load(os.path.join(MEN, "Campement/Campement_Fond.png"))
    piece(img, "Campement", "Campement_Flammes", P["Campement_Flammes"], add=True)
    if not state.get(3):
        piece(img, "Campement", "Campement_Lanterne_Banc", P["Campement_Lanterne_Banc"])
    if not state.get(4):
        piece(img, "Campement", "Campement_Lanterne_Rocher", P["Campement_Lanterne_Rocher"])
    for place in (1, 2, 3, 4):
        n = state.get(place)
        if n:
            b, l = assis_rects(lay, n, place)
            piece(img, "Campement", "Campement_Assis_" + n, b[:2] + b[2:], size=b[2:])
            piece(img, "Campement", "Campement_Assis_%s_Lanterne" % n, l, size=l[2:])
    for k in ("Campement_Retour", "Campement_Parametres", "Campement_Amis", "Campement_Pret", "Campement_Modifier"):
        piece(img, "Campement", k, P[k])
    for place, rect in lay["places_cartouche"].items():
        if int(place) <= 2 or state.get(int(place)):
            piece(img, "Campement", "Campement_Cartouche_Nom", rect, size=rect[2:])
    for place, k in ((3, "Campement_Plus_3"), (4, "Campement_Plus_4")):
        if not state.get(place):
            piece(img, "Campement", k, P[k])
    piece(img, "Campement", "Campement_Coche", P["Campement_Coche"])
    return img


if __name__ == "__main__":
    out = sys.argv[1]
    st = {i + 1: (v if v != "-" else None) for i, v in enumerate(sys.argv[2:6])} if len(sys.argv) > 2 else {1: "Renard", 2: "Hibou"}
    reg.save(out, campement(st), "RGB")
