# -*- coding: utf-8 -*-
"""Cadres d'amélioration par personnage (nuit 5, lot 2).

Le rang ne se lit plus au métal du cadre mais aux étoiles (1 à 5) : le cadre prend la couleur du personnage propriétaire
(couleur de sa lanterne), les génériques restent en fer neutre. Forme : carré = amélioration, losange = passif.

Base : les cadres « Argent » du lot 5 (nuit 4), dont on retire les gemmes de rang (on recopie la moulure voisine), puis
le métal est teinté en gardant ses valeurs (carte de dégradé sur la luminance : ombres presque noires teintées, tons moyens
à la couleur désaturée, reflets pâles). Étoile de rang : étoile à 4 branches dessinée ici en vectoriel, or vieilli et
trait d'encre, plus sa version éteinte (emplacement vide).

    python -I cadres5.py            -> Assets/Art/UI/Ameliorations/Cadres/Cadre_<Couleur>.png, Cadre_Passif_<Couleur>.png,
                                       Etoile_Rang.png, Etoile_Rang_Vide.png ; planche docs/art/ameliorations/planche_cadres_5.png
"""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.abspath(os.path.join(ICI, "..", "..", "..", "..", "..", ".."))
CAD = os.path.join(RACINE, "Assets", "Art", "UI", "Ameliorations", "Cadres")
ICO = os.path.join(RACINE, "Assets", "Art", "UI", "Ameliorations", "Icones")
DOC = os.path.join(RACINE, "docs", "art", "ameliorations")

# couleurs des lanternes (UI-HANDOFF, couleurs des joueurs), atténuées pour du métal émaillé
COULEURS = {
    "Neutre":   None,
    "Renard":   (0xDD, 0x37, 0x3F),
    "Koala":    (0x55, 0xC8, 0x51),
    "Cameleon": (0xE6, 0xAA, 0x2E),
    "Hibou":    (0x5F, 0xB9, 0xED),
}


def lin(c):
    c = np.asarray(c, np.float32) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def sans_gemmes_carre(a):
    """Bas du cadre carré : la zone des gemmes (x 196 à 320) est remplie par deux miroirs de la barre, pris de chaque côté
    (la barre est horizontale, le miroir raccorde sans marche), fondus au centre sur 16 px."""
    a = a.copy()
    y0, y1, g, d, m = 430, 512, 196, 320, 258
    gauche = a[y0:y1, 2 * g - d:g][:, ::-1].astype(np.float32)    # x g-1 … 2g-d, retourné : colonnes g … d-1
    droite = a[y0:y1, d:2 * d - g][:, ::-1].astype(np.float32)    # x 2d-g-1 … d, retourné : colonnes g … d-1
    x = np.arange(g, d)
    k = np.clip((x - (m - 8)) / 16.0, 0, 1)[None, :, None]
    a[y0:y1, g:d] = (gauche * (1 - k) + droite * k).astype(np.uint8)
    return a


def sans_gemmes_losange(a):
    """Losange : la gemme gauche est recouverte par la moulure prise plus haut sur la même arête, puis la moitié gauche est
    reprise en miroir à droite (le cadre devient symétrique, sans aucune gemme)."""
    a = a.copy()
    h, w = a.shape[:2]
    # arête bas-gauche : du sommet gauche (36, 250) au sommet bas (256, 500) ; pas de l'arête ~ (0.66, 0.75)
    dx, dy = -64, -73
    y0, y1, x0, x1 = 380, 470, 186, 262
    src = a[y0 + dy:y1 + dy, x0 + dx:x1 + dx].astype(np.float32)
    dst = a[y0:y1, x0:x1].astype(np.float32)
    yy, xx = np.mgrid[0:y1 - y0, 0:x1 - x0]
    k = np.clip(np.minimum.reduce([yy, xx, (y1 - y0 - 1) - yy, (x1 - x0 - 1) - xx]) / 10.0, 0, 1)[..., None]
    a[y0:y1, x0:x1] = (src * k + dst * (1 - k)).astype(np.uint8)
    c = w // 2
    a[:, c:] = a[:, :w - c][:, ::-1]
    return a


def teinte(a, rgb):
    """Carte de dégradé sur la luminance (linéaire) : fer sombre teinté → émail → reflets pâles ; alpha inchangé."""
    if rgb is None:
        # fer neutre : l'argent un peu refroidi et assombri, pour ne pas lire « rang argent »
        c = lin(a[..., :3])
        L = (c * np.array([0.2126, 0.7152, 0.0722])).sum(-1, keepdims=True)
        out = L * np.array([0.86, 0.86, 0.92]) * 0.92 + c * 0.0
        return np.dstack([(srgb(out) * 255).astype(np.uint8), a[..., 3]])
    c = lin(a[..., :3])
    L = (c * np.array([0.2126, 0.7152, 0.0722])).sum(-1, keepdims=True)
    col = lin(rgb)
    gris = (col * np.array([0.2126, 0.7152, 0.0722])).sum()
    sourd = col * 0.97 + gris * 0.03                     # couleur un peu désaturée : métal émaillé, pas néon
    sombre = sourd * 0.10 + 0.004                        # ombres : presque noires, teintées
    moyen = sourd * 0.7
    clair = sourd * 0.5 + np.array([1.0, 0.97, 0.92]) * 0.5   # reflets : pâles, chauds
    # L de l'argent : ~0 (encre) à ~0.6 (reflets)
    t = np.clip(L / 0.55, 0, 1)
    out = np.where(t < 0.5, sombre + (moyen - sombre) * (t / 0.5), moyen + (clair - moyen) * ((t - 0.5) / 0.5))
    out = out * np.clip(L / 0.04, 0, 1) + L * (1 - np.clip(L / 0.04, 0, 1))   # l'encre reste noire
    return np.dstack([(srgb(out) * 255).astype(np.uint8), a[..., 3]])


def etoile(taille=128, eteinte=False):
    """Étoile à 4 branches (forme de l'étoile du réticule), or vieilli, encre noire ; éteinte : fer sombre."""
    S = taille * 4
    im = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    c = S / 2
    def pts(r_ext, r_int):
        p = []
        for i in range(8):
            ang = np.pi / 2 + i * np.pi / 4
            r = r_ext if i % 2 == 0 else r_int
            p.append((c + r * np.cos(ang), c - r * np.sin(ang)))
        return p
    d.polygon(pts(S * 0.48, S * 0.17), fill=(8, 6, 8, 255))                        # encre
    if eteinte:
        d.polygon(pts(S * 0.40, S * 0.12), fill=(46, 42, 50, 255))
        d.polygon(pts(S * 0.22, S * 0.07), fill=(64, 58, 66, 255))
    else:
        d.polygon(pts(S * 0.40, S * 0.12), fill=(176, 128, 52, 255))               # or vieilli
        d.polygon(pts(S * 0.30, S * 0.09), fill=(222, 178, 92, 255))
        d.polygon(pts(S * 0.15, S * 0.05), fill=(250, 232, 178, 255))              # cœur clair
        # moitié basse droite plus sombre : relief
        mask = Image.new("L", (S, S), 0)
        ImageDraw.Draw(mask).polygon([(c, c), (S, c), (S, S), (c, S)], fill=70)
        ombre = Image.new("RGBA", (S, S), (60, 30, 8, 255))
        base = im.copy()
        im = Image.composite(Image.blend(base, ombre, 0.35), base, mask)
        # garder l'alpha d'origine
        im.putalpha(base.split()[-1])
    return im.resize((taille, taille), Image.LANCZOS)


def main():
    carre = np.array(Image.open(os.path.join(CAD, "Cadre_Argent.png")).convert("RGBA"))
    losange = np.array(Image.open(os.path.join(CAD, "Cadre_Passif_Argent.png")).convert("RGBA"))
    carre = sans_gemmes_carre(carre)
    losange = sans_gemmes_losange(losange)
    sorties = []
    for nom, rgb in COULEURS.items():
        for forme, base in (("Cadre_", carre), ("Cadre_Passif_", losange)):
            p = os.path.join(CAD, forme + nom + ".png")
            Image.fromarray(teinte(base, rgb)).save(p)
            sorties.append(p)
    etoile(128).save(os.path.join(CAD, "Etoile_Rang.png"))
    etoile(128, True).save(os.path.join(CAD, "Etoile_Rang_Vide.png"))
    planche()
    print("\n".join(sorties))


ETOILE, PAS = 0.24, 0.17   # taille et pas des étoiles en fraction de la case (IconeAmelioration : mêmes valeurs)


def compose(cadre, icone, passif, etoiles, taille):
    """Aperçu d'une case (même composition que IconeAmelioration) : illustration, cadre, étoiles."""
    T = taille
    out = Image.new("RGBA", (T, int(T * 1.18)), (0, 0, 0, 0))
    f = 0.62 if passif else 0.80
    s = int(T * f)
    ic = icone.resize((s, s), Image.LANCZOS)
    if passif:
        m = Image.new("L", (s, s), 0)
        ImageDraw.Draw(m).polygon([(s / 2, 0), (s, s / 2), (s / 2, s), (0, s / 2)], fill=255)
        ic.putalpha(m)
    out.alpha_composite(ic, ((T - s) // 2, (T - s) // 2))
    out.alpha_composite(cadre.resize((T, T), Image.LANCZOS), (0, 0))
    e = Image.open(os.path.join(CAD, "Etoile_Rang.png"))
    ev = Image.open(os.path.join(CAD, "Etoile_Rang_Vide.png"))
    es = int(T * ETOILE)
    pas = int(T * PAS)
    x0 = T // 2 - (pas * (etoiles - 1) + es) // 2
    y = int(T * (1 - ETOILE * 0.55))
    for i in range(etoiles):
        out.alpha_composite(e.resize((es, es), Image.LANCZOS), (x0 + i * pas, y))
    return out


def planche():
    ids = {"Neutre": ("G11", "G5"), "Renard": ("R5", "R_P1"), "Koala": ("K4", "K_P1"), "Cameleon": ("C3", "C_P2"), "Hibou": ("H1", "H_P1")}
    T = 160
    W = 40 + len(COULEURS) * (T * 2 + 40)
    H = 60 + 5 * int(T * 1.25) + 40
    P = Image.new("RGBA", (W, H), (16, 14, 18, 255))
    for ci, nom in enumerate(COULEURS):
        a, b = ids[nom]
        ca = Image.open(os.path.join(CAD, "Cadre_" + nom + ".png"))
        cb = Image.open(os.path.join(CAD, "Cadre_Passif_" + nom + ".png"))
        ia = Image.open(os.path.join(ICO, "ICO_" + a + ".png")).convert("RGBA")
        ib = Image.open(os.path.join(ICO, "ICO_" + b + ".png")).convert("RGBA")
        for r in range(5):
            x = 40 + ci * (T * 2 + 40)
            y = 40 + r * int(T * 1.25)
            P.alpha_composite(compose(ca, ia, False, r + 1, T), (x, y))
            P.alpha_composite(compose(cb, ib, True, r + 1, T), (x + T, y))
    os.makedirs(DOC, exist_ok=True)
    P.convert("RGB").save(os.path.join(DOC, "planche_cadres_5.png"))


if __name__ == "__main__":
    main()
