# -*- coding: utf-8 -*-
"""Artefacts de l'écran de choix du personnage (nuit 7) : détourage et calques d'animation.

Entrées : images générées sur fond noir et leur détourage Higgsfield (<nom>_v02_detoure.png, alpha seul utilisé) (Nano Banana 2.1, docs/art/ui/generees/ui_choix_artefact_<nom>_v02.png : lanterne,
épée, barillet, livre ; la lanterne v02 reprend le dessin de l'ancienne Choix_Artefact_Lanterne, qui servait de référence).

Sorties (Assets/Art/UI/Menus/Choix/, résolution x2 : 800 x 600 px pour un cadre de 400 x 300 de la maquette, centré
sur le centre visuel de la lanterne, 1050,342) :
  Choix_Art_<Nom>.png          corps (silhouette du détourage, trous internes bouchés)
  Choix_Art_<Nom>_Lueur.png    lumière peinte autour de l'objet (original hors silhouette) + halo large, additif
  Choix_Art_Lanterne_Verre.png éclat du verre (additif, par-dessus)
  Choix_Art_Epee_Runes.png     runes seules (additif)
  Choix_Art_Barillet_Reflet.png  poids du reflet (R : métal clair, G : rune)
  Choix_Art_Livre_Pages.png    poids du frémissement des pages (R)
  Choix_Art_Livre_Volutes.png  canaux des volutes (R)

python artefacts_choix.py <dossier des images générées>
"""
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from collections import deque


class ndimage:
    """Le strict nécessaire de scipy.ndimage, en numpy et PIL (scipy n'est pas installé)."""

    @staticmethod
    def gaussian_filter(x, s):
        x = np.asarray(x, np.float32)
        if s <= 0:
            return x
        r = int(3 * s + 1)
        k = np.exp(-0.5 * (np.arange(-r, r + 1) / s) ** 2); k /= k.sum()
        p = np.pad(x, r, mode="edge")
        h = np.zeros((p.shape[0], x.shape[1]), np.float32)
        for i in range(2 * r + 1):
            h += k[i] * p[:, i:i + x.shape[1]]
        v = np.zeros(x.shape, np.float32)
        for i in range(2 * r + 1):
            v += k[i] * h[i:i + x.shape[0], :]
        return v

    @staticmethod
    def _filtre(m, n, f):
        im = Image.fromarray((np.asarray(m) > 0).astype(np.uint8) * 255, "L")
        for _ in range(n):
            im = im.filter(f(3))
        return np.asarray(im) > 127

    @staticmethod
    def binary_erosion(m, iterations=1):
        return ndimage._filtre(m, iterations, ImageFilter.MinFilter)

    @staticmethod
    def binary_dilation(m, iterations=1):
        return ndimage._filtre(m, iterations, ImageFilter.MaxFilter)

    @staticmethod
    def binary_opening(m, iterations=1):
        return ndimage.binary_dilation(ndimage.binary_erosion(m, iterations), iterations)

    @staticmethod
    def grey_opening(x, size):
        im = Image.fromarray(np.clip(x * 255, 0, 255).astype(np.uint8), "L")
        im = im.filter(ImageFilter.MinFilter(size[0])).filter(ImageFilter.MaxFilter(size[0]))
        return np.asarray(im, np.float32) / 255.0

    @staticmethod
    def binary_fill_holes(m):
        m = np.asarray(m) > 0
        im = Image.fromarray(np.pad(m, 1).astype(np.uint8) * 255, "L").copy()   # copie : fromarray partage un tampon en lecture seule
        ImageDraw.floodfill(im, (0, 0), 128)
        dehors = np.asarray(im)[1:-1, 1:-1] == 128
        return ~dehors

    @staticmethod
    def label(m):
        m = np.asarray(m) > 0
        lab = np.zeros(m.shape, np.int32)
        n = 0
        H, W = m.shape
        for y0, x0 in zip(*np.nonzero(m)):
            if lab[y0, x0]:
                continue
            n += 1
            lab[y0, x0] = n
            q = deque([(y0, x0)])
            while q:
                y, x = q.popleft()
                for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                    if 0 <= yy < H and 0 <= xx < W and m[yy, xx] and not lab[yy, xx]:
                        lab[yy, xx] = n
                        q.append((yy, xx))
        return lab, n

    @staticmethod
    def find_objects(lab):
        out = []
        for i in range(1, lab.max() + 1):
            ys, xs = np.nonzero(lab == i)
            out.append((slice(ys.min(), ys.max() + 1), slice(xs.min(), xs.max() + 1)))
        return out

    @staticmethod
    def sum(m, lab, idx):
        return np.bincount(lab.ravel(), weights=np.asarray(m, np.float64).ravel(), minlength=max(idx) + 1)[list(idx)]

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.normpath(os.path.join(ICI, "..", "..", "..", "..", "..", ".."))
CHOIX = os.path.join(RACINE, "Assets", "Art", "UI", "Menus", "Choix")
ECH = 2                       # résolution des textures / pixels de la maquette
CADRE = (850, 192, 400, 300)  # x, y (haut), largeur, hauteur dans la maquette
W, H = CADRE[2] * ECH, CADRE[3] * ECH
CENTRE = (1050 - CADRE[0], 342 - CADRE[1])  # centre visuel (lanterne) dans le cadre, maquette

# taille visée de l'objet (boîte englobante, pixels de la maquette) et rotation (degrés, sens trigo)
OBJETS = {
    "Lanterne": dict(src="ui_choix_artefact_lanterne_v02.png", boite=(110, 200), rot=0.0),
    "Epee": dict(src="ui_choix_artefact_epee_v02.png", boite=(290, 195), rot=17.0),
    "Livre": dict(src="ui_choix_artefact_livre_v02.png", boite=(225, 170), rot=0.0),
    "Barillet": dict(src="ui_choix_artefact_barillet_v02.png", boite=(132, 152), rot=0.0),
}


def f32(im):
    return np.asarray(im.convert("RGB"), dtype=np.float32) / 255.0


def sauve(arr, nom, mode="RGBA"):
    a = np.clip(arr * 255.0 + 0.5, 0, 255).astype(np.uint8)
    Image.fromarray(a, mode).save(os.path.join(CHOIX, nom + ".png"), optimize=True)
    print("  ", nom)


def detoure(rgb, cut):
    """Image sur fond noir + son détourage (Higgsfield, alpha seul utilisé) -> corps RGBA et lumière peinte (RGB additif).
    Le corps est l'objet opaque (trous internes bouchés) ; la lumière est tout ce que l'objet ne couvre pas, telle que peinte."""
    a = np.where(cut[..., 3] < 0.03, 0, cut[..., 3])
    # seuls les petits trous internes (jours du verre, rivets) sont bouchés ; l'intérieur d'un anneau reste vide
    m = a > 0.5
    trous = ndimage.binary_fill_holes(m) & ~m
    lab, n = ndimage.label(trous)
    if n:
        petits = 1 + np.nonzero(np.bincount(lab.ravel())[1:] < 300)[0]
        a = np.maximum(a, ndimage.gaussian_filter(np.isin(lab, petits).astype(np.float32), 0.7))
    # lumière peinte : fond « noir » de l'image ôté (sinon un rectangle gris s'ajoute au panneau), bords de l'image fondus
    mx = rgb.max(axis=2)
    noir = np.percentile(np.concatenate([mx[:24].ravel(), mx[-24:].ravel(), mx[:, :24].ravel(), mx[:, -24:].ravel()]), 99.5)
    lum_ = np.clip(rgb - noir, 0, 1) / (1 - noir) * (1 - a[..., None])
    hh, ww = a.shape
    yy, xx = np.mgrid[0:hh, 0:ww]
    bord = np.clip(np.minimum(np.minimum(xx, ww - 1 - xx), np.minimum(yy, hh - 1 - yy)) / 60.0, 0, 1)
    lum_ *= bord[..., None]
    # étincelles peintes (points isolés) : écrêtées par un flou, la lumière reste lisse
    lum_ = np.minimum(lum_, np.dstack([ndimage.gaussian_filter(lum_[..., i], 4.0) for i in range(3)]) * 1.4)
    return np.dstack([rgb, a]), lum_


def place(couches, boite, rot):
    """Même transformation pour toutes les couches (la première est le corps RGBA, qui donne la taille) : rotation,
    mise à l'échelle de l'objet plein dans la boîte, centrage sur CENTRE dans une toile W x H. Couches : RGBA, RGB ou 2D."""
    def vers_image(x):
        x = np.clip(x, 0, 1)
        mode = "L" if x.ndim == 2 else ("RGBA" if x.shape[2] == 4 else "RGB")
        return Image.fromarray((x * 255 + 0.5).astype(np.uint8), mode)
    ims = [vers_image(c) for c in couches]
    if rot:
        ims = [im.rotate(rot, resample=Image.BICUBIC, expand=True) for im in ims]
    al = np.asarray(ims[0])[..., 3]
    ys, xs = np.nonzero(al > 128)
    k = min(boite[0] * ECH / (xs.max() + 1 - xs.min()), boite[1] * ECH / (ys.max() + 1 - ys.min()))
    cx, cy = (xs.min() + xs.max() + 1) / 2, (ys.min() + ys.max() + 1) / 2
    out = []
    for im in ims:
        im = im.resize((round(im.width * k), round(im.height * k)), Image.LANCZOS)
        toile = Image.new(im.mode, (W, H), 0)
        toile.paste(im, (round(CENTRE[0] * ECH - cx * k), round(CENTRE[1] * ECH - cy * k)))
        out.append(np.asarray(toile, dtype=np.float32) / 255.0)
    return out


def additif(rgb):
    return np.dstack([np.clip(rgb, 0, 1), np.ones(rgb.shape[:2], np.float32)])


def bande(shape, a, b, demi):
    """Masque doux d'une bande de demi-largeur demi autour du segment a-b (coordonnées x, y de l'image)."""
    yy, xx = np.mgrid[0:shape[0], 0:shape[1]].astype(np.float32)
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    t = np.clip(((xx - ax) * dx + (yy - ay) * dy) / (dx * dx + dy * dy), 0, 1)
    d = np.hypot(xx - (ax + t * dx), yy - (ay + t * dy))
    return np.clip((demi - d) / 4.0, 0, 1)


def runes_source(rgb):
    """Runes gravées près de la garde de l'épée (image source v02) : traits clairs dans la bande des runes."""
    l = lum(rgb)
    fond = ndimage.grey_opening(l, size=(7, 7))
    chapeau = np.clip((l - fond - 0.08) / 0.15, 0, 1)
    vert = np.clip((rgb[..., 1] - rgb[..., 0] - 0.05) / 0.2, 0, 1)
    return chapeau * vert * bande(l.shape, (462, 534), (583, 446), 17)


def halo(rgba, poids, rayon, gain):
    """Lueur additive : couleur x poids, flou large + flou serré (RGB, alpha 1)."""
    src = rgba[..., :3] * rgba[..., 3:4] * poids[..., None]
    large = np.dstack([ndimage.gaussian_filter(src[..., i], rayon) for i in range(3)])
    serre = np.dstack([ndimage.gaussian_filter(src[..., i], rayon * 0.25) for i in range(3)])
    g = np.clip((large * 0.75 + serre * 0.25) * gain, 0, 1)
    return np.dstack([g, np.ones(g.shape[:2], np.float32)])


def lum(c):
    return c[..., 0] * 0.2126 + c[..., 1] * 0.7152 + c[..., 2] * 0.0722


def sat(c):
    mx, mn = c.max(axis=2), c.min(axis=2)
    return np.where(mx > 1e-3, (mx - mn) / np.maximum(mx, 1e-3), 0)


def lanterne(rgba, peinte):
    c = rgba[..., :3]
    verre = np.clip((c[..., 0] - 0.45) / 0.4, 0, 1) * np.clip((c[..., 0] - c[..., 1] - 0.15) / 0.3, 0, 1) * (rgba[..., 3] > 0.9)
    sauve(additif(peinte + halo(rgba, verre, 52, 1.3)[..., :3]), "Choix_Art_Lanterne_Lueur")   # halo peint + halo large
    v = ndimage.gaussian_filter(verre, 2.0)[..., None] * rgba[..., :3] * 1.3   # éclat du verre, par-dessus
    sauve(np.dstack([np.clip(v, 0, 1), np.ones(verre.shape, np.float32)]), "Choix_Art_Lanterne_Verre")
    ys, xs = np.nonzero(verre > 0.3)
    return dict(verre=[float(xs.mean()) / W, 1 - float(ys.mean()) / H, float(xs.std()) / W, float(ys.std()) / H],
                haut=1 - float(np.nonzero(rgba[..., 3] > 0.9)[0].min()) / H)


def epee(rgba, peinte, runes):
    c, a = rgba[..., :3], rgba[..., 3]
    runes = runes * (a > 0.9)
    print("  runes : %d px" % int((runes > 0.3).sum()))
    trait = ndimage.gaussian_filter(runes, 0.6) * 1.4 + ndimage.gaussian_filter(runes, 5.0) * 2.2
    r = np.dstack([np.array([0.5, 1.0, 0.6], np.float32) * trait[..., None], np.ones(a.shape, np.float32)])
    sauve(np.clip(r, 0, 1), "Choix_Art_Epee_Runes")
    vert = np.clip((c[..., 1] - c[..., 0] - 0.10) / 0.2, 0, 1)
    lame = np.clip((c[..., 1] - 0.35) / 0.4, 0, 1) * vert
    sauve(additif(peinte * 1.1 + halo(rgba, lame, 26, 0.8)[..., :3]), "Choix_Art_Epee_Lueur")
    ys, xs = np.nonzero(runes > 0.3)
    return dict(runes=[float(xs.min()) / W, float(xs.max()) / W])


def barillet(rgba, peinte):
    c, a = rgba[..., :3], rgba[..., 3]
    l = lum(c)
    metal = np.clip((l - 0.25) / 0.5, 0, 1) * (a > 0.95)
    rune = np.clip((c[..., 0] - 0.75) / 0.2, 0, 1) * np.clip((c[..., 0] - c[..., 2] - 0.45) / 0.3, 0, 1) * (a > 0.9)
    rune = ndimage.gaussian_filter(rune, 0.8)
    m = np.dstack([metal, np.clip(rune * 1.5, 0, 1), np.zeros_like(a), np.ones_like(a)])
    sauve(m, "Choix_Art_Barillet_Reflet")
    chaud = np.clip((c[..., 0] - c[..., 2] - 0.3) / 0.4, 0, 1)
    sauve(additif(peinte + halo(rgba, rune * 2.0, 14, 1.5)[..., :3]), "Choix_Art_Barillet_Lueur")
    return {}


def livre(rgba, peinte):
    c, a = rgba[..., :3], rgba[..., 3]
    l = lum(c)
    pages = np.clip((l - 0.38) / 0.2, 0, 1) * np.clip((0.55 - sat(c)) / 0.2, 0, 1) * (a > 0.95)
    # tons de la scène : couverture et tranche dans l'ombre (x0,62), pages un peu baissées (x0,85)
    k = 0.62 + 0.23 * ndimage.gaussian_filter(pages, 1.5)
    rgba[..., :3] = c * k[..., None]
    sauve(rgba, "Choix_Art_Livre")
    c = rgba[..., :3]
    pages = ndimage.binary_opening(pages > 0.5, iterations=2)
    pages = ndimage.binary_erosion(ndimage.binary_dilation(pages, iterations=4), iterations=4)   # fermeture : texte peint
    lab, n = ndimage.label(pages)
    if n:
        tailles = ndimage.sum(pages, lab, range(1, n + 1))
        pages = np.isin(lab, 1 + np.nonzero(tailles > 0.02 * tailles.max())[0])
    ys, xs = np.nonzero(pages)
    y0, y1 = ys.min(), ys.max()
    # poids : nul au bas des pages (reliure), maximal sur les bords libres du haut ; adouci, rentré dans les pages
    haut = np.clip((y1 - np.arange(H)[:, None]) / max(1.0, (y1 - y0)), 0, 1) ** 1.6
    interieur = ndimage.gaussian_filter(ndimage.binary_erosion(pages, iterations=3).astype(np.float32), 3.0)
    p = np.clip(interieur * haut * 1.4, 0, 1)
    sauve(np.dstack([p, p, p, np.ones_like(p)]), "Choix_Art_Livre_Pages")
    large = halo(rgba, pages.astype(np.float32), 44, 1.0)[..., :3] * np.array([0.45, 0.75, 1.0], np.float32)
    sauve(additif(peinte * 0.55 + large), "Choix_Art_Livre_Lueur")                    # bleu glacé
    # volutes : quatre filets qui partent du haut des pages, montent de 55 à 85 px (maquette) en ondulant et s'effilent
    img = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(img)
    rng = np.random.default_rng(7)
    for fx, haut, larg in ((0.22, 105, 15), (0.45, 125, 17), (0.62, 118, 14), (0.82, 90, 11)):
        x0 = xs.min() + fx * (xs.max() - xs.min())
        colonne = np.nonzero(pages[:, int(x0)])[0]
        y0b = (colonne.min() if colonne.size else ys.min()) + 6
        ph, sens = rng.uniform(0, 6.28), (1 if fx > 0.5 else -1)
        pts = [(x0 + sens * 26 * u * u + 9 * np.sin(u * 8.0 + ph) * u, y0b - u * haut) for u in np.linspace(0, 1, 41)]
        for k in range(40):
            u = k / 40.0
            d.line([pts[k], pts[k + 1]], fill=int(255 * (1 - u) ** 0.7 * min(1, k / 4)), width=max(2, int(larg * (1 - 0.7 * u))))
    v = np.asarray(img.filter(ImageFilter.GaussianBlur(7)), dtype=np.float32) / 255.0
    v *= (1 - ndimage.gaussian_filter((a > 0.5).astype(np.float32), 4) * 0.6)
    sauve(np.dstack([v, v, v, np.ones_like(v)]), "Choix_Art_Livre_Volutes")
    return dict(pages=[float(y0) / H, float(y1) / H])


def main():
    gen = sys.argv[1] if len(sys.argv) > 1 else os.path.join(RACINE, "docs", "art", "ui", "generees")
    info = {"cadre": CADRE, "echelle": ECH}
    for nom, o in OBJETS.items():
        print(nom)
        rgb = f32(Image.open(os.path.join(gen, o["src"])))
        cut = np.asarray(Image.open(os.path.join(gen, o["src"].replace(".png", "_detoure.png"))).convert("RGBA"), np.float32) / 255.0
        assert cut.shape[:2] == rgb.shape[:2], nom
        corps, peinte = detoure(rgb, cut)
        if nom == "Epee":
            rgba, peinte, runes = place([corps, peinte, runes_source(rgb)], o["boite"], o["rot"])
            sauve(rgba, "Choix_Art_" + nom)
            info["epee"] = epee(rgba, peinte, runes)
            continue
        rgba, peinte = place([corps, peinte], o["boite"], o["rot"])
        if nom != "Livre":
            sauve(rgba, "Choix_Art_" + nom)
        info[nom.lower()] = {"Lanterne": lanterne, "Barillet": barillet, "Livre": livre}[nom](rgba, peinte)
    with open(os.path.join(ICI, "artefacts_choix.json"), "w", encoding="utf-8") as f:
        json.dump(info, f, indent=1)
    print(json.dumps(info))


if __name__ == "__main__":
    main()
