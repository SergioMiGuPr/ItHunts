# -*- coding: utf-8 -*-
"""Nuit 5, lot 3 : pièces pour de vraies cartes en 3 dimensions (importé par kit_caves.py, kit_chateau.py et kit_jardin.py).

La session main construit de grandes cartes faites à la main avec les kits ; ce module fournit ce qui manquait :
- verticalité : pont suspendu, rebord de mur, plateforme de traction (plaque magnétique qui luit), salle en hauteur,
  passerelle en pente, balcon de pierre ;
- acte II : fenêtres dont les vitres montrent l'extérieur (ciel, lune, paysage lointain : matériaux `M_Kit_VitreVue*`,
  shader ItHunts/VueExterieure) ;
- acte III : lisière de forêt dense (droite et en angle), sous-bois, rochers, grand arbre praticable ;
- sols percés : puits dans sa dalle, fosse dans sa dalle ;
- acte II : support de râtelier (petit présentoir).

Mesures (ART-HANDOFF § 3) : saut 1.3 m, hissage 3.7 m (un saut) ou 5.3 m (double saut), dessus d'au moins 0.6 m, pente
de marche 35° au plus ; traqueur : plateformes de moins de 10 m de profondeur. Conventions de kitlib : Z en haut, avant vers
-Y (Unity +Z), pivot au sol sauf mention ; `Col_*` = collision seule ; `Garde_Corps` ; `Deco_SansCollision`.
"""
import math, random
from mathutils import Vector
import kitlib as L
import kit_hauteur as H

COL = H.COL
MAGNET = "M_Kit_Magnetique"            # plaque de traction : fer sombre, runes bleu pâle qui luisent (créé par KitBuilder)
VUE = {"M_Kit_Vitrail": "M_Kit_VitreVue", "M_Kit_Vitrail_Rouge": "M_Kit_VitreVue_Rouge", "M_Kit_Vitrail_Ambre": "M_Kit_VitreVue_Ambre"}
FOND = "M_Fond_Noir"


def bornes(ob):
    """Emprise de toute la pièce (objet et enfants maillés, collisions `Col_*` exclues), en coordonnées de la pièce."""
    import bpy
    pts = []
    for o in [ob] + [c for c in ob.children_recursive if c.type == "MESH" and not c.name.startswith("Col_")]:
        mw = ob.matrix_world.inverted() @ o.matrix_world
        pts += [mw @ v.co for v in o.data.vertices]
    if not pts:
        return (0, 0), (0, 0), (0, 0)
    return tuple((min(p[i] for p in pts), max(p[i] for p in pts)) for i in range(3))


def _catenaire(x, Lx, sag):
    u = 2 * x / Lx
    return -sag * (1 - u * u)


# =================================================================== 1. pont suspendu
def pont_suspendu(S, nom, Lx=12.0, W=1.7, sag=0.8):
    """Pont suspendu de Lx m entre deux plateformes (dessus à 0 aux deux bouts, pivot au centre) : tablier de planches qui
    fléchit de `sag` au milieu, deux mains courantes et des suspentes ; poteaux aux bouts. Collision : `Col_Tablier` (bande
    lisse qui suit la flèche) et `Garde_Corps` (mains courantes)."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    kc = L.K("Col_Tablier")
    rnd = random.Random(S["seed"] + 501)
    act = S["act"]
    lien = S["fer"] if act == "Chateau" else S["corde"]
    planche = S["planche"] if act != "Chateau" else S["bois"]
    n = int(Lx / 0.34)
    for i in range(n):
        x = -Lx / 2 + (i + 0.5) * Lx / n
        z = _catenaire(x, Lx, sag)
        x2 = x + 0.01
        pente = math.atan2(_catenaire(x2, Lx, sag) - z, 0.01)
        manque = act == "Cave" and rnd.random() < 0.06 and 2 < i < n - 3
        if manque:
            continue
        w = W + rnd.uniform(-0.12, 0.08)
        # planche inclinée selon la flèche : obox entre deux points du câble
        a = Vector((x - Lx / n * 0.45, rnd.uniform(-0.04, 0.04), _catenaire(x - Lx / n * 0.45, Lx, sag) - 0.05))
        b = Vector((x + Lx / n * 0.45, a.y, _catenaire(x + Lx / n * 0.45, Lx, sag) - 0.05))
        k.obox(a, b, w, 0.08, planche, up=(0, 0, 1))
    up = Vector((0, 0, 1))
    seg = 24
    for sy in (-1, 1):
        y = sy * (W / 2 - 0.05)
        bas = [Vector((-Lx / 2 + Lx * t / seg, y, _catenaire(-Lx / 2 + Lx * t / seg, Lx, sag) - 0.14)) for t in range(seg + 1)]
        haut = [Vector((-Lx / 2 + Lx * t / seg, y, _catenaire(-Lx / 2 + Lx * t / seg, Lx, sag * 0.55) + 1.15)) for t in range(seg + 1)]
        for t in range(seg):
            k.obox(bas[t], bas[t + 1], 0.07, 0.07, lien)
            kg.obox(haut[t], haut[t + 1], 0.07, 0.07, lien)
        for t in range(0, seg + 1, 2):
            k.obox(bas[t], haut[t], 0.035, 0.035, lien)
        # lisse basse à 0.5 m (relecture : un joueur en glissade, 0.9 m, passait sous la main courante)
        mi = [Vector((-Lx / 2 + Lx * t / seg, y, _catenaire(-Lx / 2 + Lx * t / seg, Lx, sag) + 0.5)) for t in range(seg + 1)]
        for t in range(seg):
            kg.obox(mi[t], mi[t + 1], 0.05, 0.05, lien)
    # poteaux et ancrages aux bouts
    for sx in (-1, 1):
        x = sx * (Lx / 2 + 0.15)
        for sy in (-1, 1):
            y = sy * (W / 2 + 0.05)
            if act == "Chateau":
                k.box((x, y, 0.65), (0.34, 0.34, 1.4), S["pierre"])
                k.lathe((x, y, 1.35), [(0.2, 0), (0.24, 0.06), (0.12, 0.22), (0.0, 0.3)], S["pierre"], n=8)
            else:
                k.cyl((x, y, -0.4), 0.11, 2.0, S["bois"], n=8, rough=0.08, seed=rnd.randint(0, 99))
        k.box((x, 0, -0.08), (0.4, W + 0.3, 0.16), S["bois"] if act != "Chateau" else S["pierre"])
    # collision : bande lisse (dessous des planches au plus haut)
    for t in range(seg):
        x0, x1 = -Lx / 2 + Lx * t / seg, -Lx / 2 + Lx * (t + 1) / seg
        z0, z1 = _catenaire(x0, Lx, sag) - 0.01, _catenaire(x1, Lx, sag) - 0.01
        H.rampe_col(kc, [(x0, -W / 2, z0), (x1, -W / 2, z1), (x1, W / 2, z1), (x0, W / 2, z0)])
    # collision des poteaux et piliers d'ancrage (relecture : on les traversait)
    for sx in (-1, 1):
        x = sx * (Lx / 2 + 0.15)
        for sy in (-1, 1):
            kc.box((x, sy * (W / 2 + 0.05), 0.6), (0.4, 0.4, 1.6), COL)
    k.empty("Appui_A", (-Lx / 2, 0, 0))
    k.empty("Appui_B", (Lx / 2, 0, 0))
    ob = k.finish(S["seed"] + 501, H.PEINT_OBJET)
    H.enfant(ob, kg, S["seed"] + 502)
    H.enfant(ob, kc, 0)
    return ob


# =================================================================== 2. rebord de mur
def rebord(S, nom, Lx=3.0, D=0.9):
    """Rebord de mur de Lx × D m : dessus plat à 0 (pivot au ras du mur, au centre du bord arrière), saillie vers l'avant
    (-Y), consoles dessous. À poser en quinconce tous les 2.2 à 3.5 m pour monter (saut et hissage, wall jump)."""
    k = L.K(nom)
    rnd = random.Random(S["seed"] + 511)
    act = S["act"]
    ep = 0.3
    if act == "Chateau":
        # corniche moulurée : talon, tablette, larmier
        prof = [(0.0, 0.0), (-D, 0.0), (-D, -0.12), (-D + 0.08, -0.16), (-D + 0.08, -ep), (-0.25, -ep - 0.12), (0.0, -ep - 0.12)]
        H.prism(k, list(reversed(prof)), -Lx / 2, Lx / 2, S["pierre"], axis="x")
        for x in (-Lx / 2 + 0.35, 0.0, Lx / 2 - 0.35):
            prof_c = [(0.0, -ep - 0.12), (-D + 0.2, -ep - 0.12), (-D + 0.25, -ep - 0.3), (-0.3, -ep - 0.85), (0.0, -ep - 0.95)]
            H.prism(k, list(reversed(prof_c)), x - 0.14, x + 0.14, S["pierre"], axis="x")
    else:
        # dalle brute sur trois corbeaux de pierre (jardin : avec cartes de lierre qui pendent)
        k.box((0, -D / 2, -ep / 2 + H.dz()), (Lx, D, ep), S["pierre"])
        for i, x in enumerate((-Lx / 2 + 0.4, 0.0, Lx / 2 - 0.4)):
            for j in range(3):
                d = D * (0.82 - j * 0.24)
                k.box((x, -d / 2, -ep - 0.13 - j * 0.24), (0.32, d, 0.24), S["mur"], rot=rnd.uniform(-0.02, 0.02))
        if act == "Jardin":
            for x in (-Lx / 2 + 0.7, Lx / 2 - 0.9):
                H.lierre(k, S, [Vector((x, -D - 0.02, 0.02)), Vector((x + 0.1, -D - 0.03, -0.6)), Vector((x - 0.05, -D - 0.03, -1.3))],
                         lambda p: Vector((0, -1, 0)), l=0.28, seed=rnd.randint(0, 999))
    ob = k.finish(S["seed"] + 511, H.PEINT_ARCHI)
    return ob


# =================================================================== 3. plateforme de traction
def plateforme_traction(S, nom, A=3.0, chute=4.0):
    """Plateforme de traction de A × A m, suspendue par quatre chaînes (pivot au centre du dessus ; accroches des chaînes à
    `chute` m au-dessus) : sous elle et sur sa tranche avant, une plaque de fer gravée de runes qui luisent d'un bleu pâle
    (surface magnétique : on s'y tracte). Repère `Accroche_Traction` au centre de la plaque du dessous."""
    k = L.K(nom)
    km = L.K("Plaque_Magnetique")
    act = S["act"]
    ep = 0.45
    if act == "Chateau":
        k.box((0, 0, -ep / 2), (A, A, ep), S["pierre"])
        k.box((0, 0, 0.004), (A - 0.24, A - 0.24, 0.008), S["sol"], skip=("-z",))
    elif act == "Cave":
        n = 9
        for i in range(n):
            x = -A / 2 + (i + 0.5) * A / n
            k.box((x, 0, -0.06), (A / n - 0.03, A, 0.12), S["planche"])
        for sx in (-1, 1):
            k.box((sx * (A / 2 - 0.12), 0, -ep / 2 - 0.06), (0.24, A, ep - 0.12), S["bois"])
            k.box((0, sx * (A / 2 - 0.12), -ep / 2 - 0.06), (A, 0.24, ep - 0.12), S["bois"])
        # traverses en croix sous le centre : la plaque magnétique y est vissée (relecture : elle flottait)
        k.box((0, 0, -ep / 2 - 0.06), (A - 0.5, 0.26, ep - 0.12), S["bois"])
        k.box((0, 0, -ep / 2 - 0.055), (0.26, A - 0.5, ep - 0.13), S["bois"])
    else:
        k.box((0, 0, -ep / 2), (A, A, ep), S["pierre"])
        k.box((0, 0, 0.004), (A - 0.2, A - 0.2, 0.008), S["sol"], skip=("-z",))
    # plaque dessous : disque de fer sombre, anneau et glyphes de runes qui luisent (géométrie : pas de disque lumineux plein)
    R = A * 0.36
    nr = 32
    zp = -ep - 0.012
    for i in range(nr):
        a0, a1 = 2 * math.pi * i / nr, 2 * math.pi * (i + 1) / nr
        km.face([(0, 0, zp), (R * math.cos(a1), R * math.sin(a1), zp), (R * math.cos(a0), R * math.sin(a0), zp)], S["fer"])
        # anneau lumineux (R × 0.78 à 0.84), juste sous le disque
        r0, r1 = R * 0.78, R * 0.84
        km.face([(r0 * math.cos(a0), r0 * math.sin(a0), zp - 0.004), (r0 * math.cos(a1), r0 * math.sin(a1), zp - 0.004),
                 (r1 * math.cos(a1), r1 * math.sin(a1), zp - 0.004), (r1 * math.cos(a0), r1 * math.sin(a0), zp - 0.004)][::-1], MAGNET)
    # huit glyphes : losanges et traits entre 0.4 et 0.68 R
    for g in range(8):
        a = 2 * math.pi * g / 8 + math.pi / 8
        d = Vector((math.cos(a), math.sin(a), 0)); n_ = Vector((-d.y, d.x, 0))
        c = d * R * 0.55 + Vector((0, 0, zp - 0.004))
        if g % 2 == 0:
            q = [c + d * 0.11, c + n_ * 0.05, c - d * 0.11, c - n_ * 0.05]
        else:
            q = [c + d * 0.13 + n_ * 0.018, c - d * 0.13 + n_ * 0.018, c - d * 0.13 - n_ * 0.018, c + d * 0.13 - n_ * 0.018]
        km.face([tuple(v) for v in q][::-1], MAGNET)
    # tranche avant : bandeau de fer et filet lumineux
    km.box((0, -A / 2 - 0.012, -ep / 2), (A * 0.7, 0.02, ep * 0.55), S["fer"], skip=("+y",))
    km.box((0, -A / 2 - 0.024, -ep / 2), (A * 0.62, 0.006, 0.035), MAGNET, skip=("+y",))
    # chaînes
    for sx in (-1, 1):
        for sy in (-1, 1):
            a = Vector((sx * (A / 2 - 0.2), sy * (A / 2 - 0.2), 0.0))
            b = Vector((sx * (A / 2 - 0.5), sy * (A / 2 - 0.5), chute))
            H._maillons(k, a, b, int(chute / 0.22), 0.09, S["fer"])
            k.box((a.x, a.y, 0.06), (0.2, 0.2, 0.12), S["fer"])
    k.empty("Accroche_Traction", (0, 0, -ep - 0.02))
    k.empty("Accroche_Plafond", (0, 0, chute))
    ob = k.finish(S["seed"] + 521, H.PEINT_OBJET)
    H.enfant(ob, km, 0)
    return ob


# =================================================================== 4. salle en hauteur
def salle_haute(S, nom, A=8.0, Hs=5.0, Hm=3.4):
    """Salle en hauteur de A × A m, plancher à Hs m (5 m : double saut et hissage, comme les mezzanines) sur quatre appuis (pivot au sol, au centre) : mur du fond percé d'une
    fenêtre, mur gauche percé d'une porte de 2.4 × 3.2 m (vers une coursive ou un pont), côté droit ouvert à garde-corps,
    devant ouvert avec un passage de 1.2 m pour une échelle (`Accroche_Echelle`), plafond à poutres à Hs + Hm."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    act = S["act"]
    rnd = random.Random(S["seed"] + 531)
    ep = 0.5
    # plancher
    if act == "Cave":
        n = int(A / 0.3)
        for i in range(n):
            x = -A / 2 + (i + 0.5) * A / n
            k.box((x, 0, Hs - 0.06), (A / n - 0.025, A, 0.12), S["planche"])
        for y in (-A / 2 + 0.2, 0, A / 2 - 0.2):
            k.box((0, y, Hs - 0.32), (A, 0.3, 0.4), S["bois"])
    else:
        k.box((0, 0, Hs - ep / 2), (A, A, ep), S["pierre"], skip=("+z",))
        k.box((0, 0, Hs + 0.004), (A, A, 0.008), S.get("parquet", S["sol"]), skip=("-z",))
    # appuis
    for sx in (-1, 1):
        for sy in (-1, 1):
            x, y = sx * (A / 2 - 0.45), sy * (A / 2 - 0.45)
            if act == "Cave":
                k.box((x, y, (Hs - 0.5) / 2), (0.4, 0.4, Hs - 0.5), S["bois"], rot=rnd.uniform(-0.03, 0.03))
                k.obox((x, y, Hs - 1.6), (x - sx * 1.2, y, Hs - 0.5), 0.2, 0.2, S["bois"])
            else:
                k.box((x, y, (Hs - ep) / 2), (0.9, 0.9, Hs - ep), S["mur"])
                k.box((x, y, Hs - ep - 0.2), (1.1, 1.1, 0.4), S["pierre"])
                k.box((x, y, 0.2), (1.1, 1.1, 0.4), S["pierre"])
    # murs : fond (+Y) avec fenêtre, gauche (-X) avec porte
    t = 0.45
    zb, zt = Hs, Hs + Hm
    _mur_ouvert(k, (-A / 2, A / 2 - t), (A / 2, A / 2), zb, zt, [(-0.8, 0.8, zb + 1.1, zb + 2.6)], S)
    _mur_ouvert(k, (-A / 2, -A / 2), (-A / 2 + t, A / 2 - t), zb, zt, [(-1.2, 1.2, zb, zb + 3.2)], S, axe="y")
    # baie du fond fermée (relecture : on voyait ce qui était derrière) : vitre avec vue au château, volets de bois ailleurs
    yb = A / 2 - t / 2
    if act == "Chateau":
        k.face([(-0.8, yb, zb + 1.1), (0.8, yb, zb + 1.1), (0.8, yb, zb + 2.6), (-0.8, yb, zb + 2.6)], "M_Kit_VitreVue")
        k.box((0, yb - 0.02, zb + 1.85), (0.08, 0.06, 1.5), S["fer"])
        k.box((0, yb - 0.02, zb + 1.85), (1.6, 0.06, 0.06), S["fer"])
    else:
        for sx in (-1, 1):
            k.box((sx * 0.4, yb, zb + 1.85), (0.78, 0.08, 1.48), S["porte"] if "porte" in S else S["bois"])
            for z in (zb + 1.35, zb + 2.35):
                k.box((sx * 0.4, yb - 0.05, z), (0.7, 0.03, 0.07), S["fer"])
    # plafond : poutres + planches (château : caissons de pierre)
    for i in range(5):
        y = -A / 2 + 0.6 + i * (A - 1.2) / 4
        k.box((0, y, zt + 0.15), (A, 0.3, 0.3), S["bois"])
    k.box((0, 0, zt + 0.36), (A + 0.3, A + 0.3, 0.12), S["planche"] if act != "Chateau" else S["pierre"])
    # garde-corps : côté droit (+X) et devant (-Y) sauf le passage de l'échelle
    garde_kind = None
    H.garde(kg, S, (A / 2 - 0.25, -A / 2 + 0.25, Hs), (A / 2 - 0.25, A / 2 - t - 0.3, Hs), kind=garde_kind)
    H.garde(kg, S, (-A / 2 + t + 0.3, -A / 2 + 0.25, Hs), (-0.6, -A / 2 + 0.25, Hs), kind=garde_kind)
    H.garde(kg, S, (0.6, -A / 2 + 0.25, Hs), (A / 2 - 0.55, -A / 2 + 0.25, Hs), kind=garde_kind)
    k.empty("Accroche_Echelle", (0, -A / 2, Hs))
    k.empty("Passage_Porte", (-A / 2, 0, Hs))
    ob = k.finish(S["seed"] + 531, H.PEINT_ARCHI)
    H.enfant(ob, kg, S["seed"] + 532)
    return ob


def _mur_ouvert(k, a, b, z0, z1, ouvertures, S, axe="x"):
    """Mur en boîte de a=(x0, y0) à b=(x1, y1), de z0 à z1, percé d'ouvertures (u0, u1, zb, zh) le long de son axe (u = 0
    au milieu). Construit en blocs (montants, allège, linteau) : aucune face confondue."""
    (x0, y0), (x1, y1) = a, b
    m = S["mur"]
    if axe == "x":
        L_, c, ep = x1 - x0, (x0 + x1) / 2, y1 - y0
        def B(u0, u1, za, zb_, mm=m):
            k.box((c + (u0 + u1) / 2, (y0 + y1) / 2, (za + zb_) / 2), (u1 - u0, ep, zb_ - za), mm)
    else:
        L_, c, ep = y1 - y0, (y0 + y1) / 2, x1 - x0
        def B(u0, u1, za, zb_, mm=m):
            k.box(((x0 + x1) / 2, c + (u0 + u1) / 2, (za + zb_) / 2), (ep, u1 - u0, zb_ - za), mm)
    bords = [-L_ / 2]
    for (u0, u1, za, zh) in sorted(ouvertures):
        B(bords[-1], u0, z0, z1)
        if za > z0 + 0.01:
            B(u0, u1, z0, za, S["pierre"])
        if zh < z1 - 0.01:
            B(u0, u1, zh, z1, S["pierre"])
        bords.append(u1)
    B(bords[-1], L_ / 2, z0, z1)


# =================================================================== 5. passerelle en pente
def passerelle_pente(S, nom, Lx=10.0, Hm=3.0, W=2.4):
    """Passerelle en pente entre deux étages : monte de Hm m sur Lx m (16.7° pour 3 sur 10), dessus à 0 au bas (pivot au
    milieu du bord bas), garde-corps des deux côtés, appuis aux bouts. Collision : `Col_Rampe` lisse."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    kc = L.K("Col_Rampe")
    act = S["act"]
    pente = Hm / Lx
    n = int(Lx / 0.32)
    for i in range(n):
        x0 = i * Lx / n + 0.015
        x1 = (i + 1) * Lx / n - 0.015
        a = Vector((x0, 0, x0 * pente - 0.05))
        b = Vector((x1, 0, x1 * pente - 0.05))
        k.obox(a, b, W, 0.1, S["planche"] if act != "Chateau" else S["sol"])
    for sy in (-1, 1):
        a = Vector((0, sy * (W / 2 - 0.15), -0.32))
        b = Vector((Lx, sy * (W / 2 - 0.15), Hm - 0.32))
        k.obox(a, b, 0.26, 0.4, S["bois"] if act != "Chateau" else S["pierre"])
    for x in (0.2, Lx - 0.2):
        k.box((x, 0, x * pente - 0.35), (0.3, W + 0.2, 0.3), S["bois"] if act != "Chateau" else S["pierre"])
    kind = {"Cave": "corde", "Chateau": "balustre", "Jardin": "bois"}[act]
    e = 0.22 if kind == "balustre" else 0.1
    for sy in (-1, 1):
        H.garde(kg, S, (0.1, sy * (W / 2 - e), 0.1 * pente), (Lx - 0.1, sy * (W / 2 - e), (Lx - 0.1) * pente), kind=kind)
    H.rampe_col(kc, [(0, -W / 2, -0.005), (Lx, -W / 2, Hm - 0.005), (Lx, W / 2, Hm - 0.005), (0, W / 2, -0.005)])
    k.empty("Appui_Bas", (0, 0, 0))
    k.empty("Appui_Haut", (Lx, 0, Hm))
    ob = k.finish(S["seed"] + 541, H.PEINT_OBJET)
    H.enfant(ob, kg, S["seed"] + 542)
    H.enfant(ob, kc, 0)
    return ob


# =================================================================== 6. balcon de pierre (château)
def balcon_pierre(S, nom, Lx=4.0, D=1.8):
    """Balcon de pierre de Lx × D m en encorbellement (dessus à 0, pivot au ras du mur), trois consoles moulurées,
    balustrade sur trois côtés."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    k.box((0, -D / 2, -0.15), (Lx, D, 0.3), S["pierre"], skip=("+z",))
    k.box((0, -D / 2, 0.004), (Lx - 0.06, D - 0.06, 0.008), S.get("marbre", S["sol"]), skip=("-z",))
    k.box((0, -D / 2, -0.32), (Lx + 0.1, D + 0.08, 0.06), S["pierre"])
    for x in (-Lx / 2 + 0.4, 0.0, Lx / 2 - 0.4):
        prof = [(0.0, -0.35), (-D + 0.25, -0.35), (-D + 0.3, -0.55), (-0.4, -1.3), (0.0, -1.45)]
        H.prism(k, list(reversed(prof)), x - 0.17, x + 0.17, S["pierre"], axis="x")
    H.garde(kg, S, (-Lx / 2 + 0.22, -0.02, 0), (-Lx / 2 + 0.22, -D + 0.22, 0), kind="balustre", bout=True)
    H.garde(kg, S, (-Lx / 2 + 0.22, -D + 0.22, 0), (Lx / 2 - 0.22, -D + 0.22, 0), kind="balustre", bout=True)
    H.garde(kg, S, (Lx / 2 - 0.22, -D + 0.22, 0), (Lx / 2 - 0.22, -0.02, 0), kind="balustre", bout=True)
    ob = k.finish(S["seed"] + 551, H.PEINT_ARCHI)
    H.enfant(ob, kg, S["seed"] + 552)
    return ob


# =================================================================== 7. fenêtres avec vue (acte II)
def fenetre_vue(S, nom, **kw):
    """Même fenêtre que les lancettes du lot 3 (nuit 4), mais ses vitres ouvrent sur l'extérieur : matériaux
    `M_Kit_VitreVue*` (shader ItHunts/VueExterieure : ciel étoilé, lune et paysage lointain vus selon la direction du
    regard, comme à l'infini ; jamais le mur ou la salle qui est derrière). Plombs et meneaux inchangés."""
    ob = H.fenetre(S, nom, **kw)
    for o in [ob] + [c for c in ob.children_recursive if c.type == "MESH"]:
        for i, m in enumerate(o.data.materials):
            if m is not None and m.name in VUE:
                o.data.materials[i] = L.mat(VUE[m.name])
    # posée contre un mur plein (cas des cartes de main) : la vitre doit être DEVANT la surface du mur, sinon le mur de la
    # salle la cache et l'on voit le papier peint dans la baie. Toute la pièce avance de 0.77 m vers la salle : le dos du
    # panneau entre de 0.23 m dans le mur, la vitre est à 5 cm devant lui, la baie dépasse de 0.77 m (embrasure profonde).
    d = Vector((0, -0.77, 0))
    for v in ob.data.vertices:
        v.co += d
    for c in ob.children:
        c.location += d
    return ob


# ------------------------------------------------------------------- sous-bois (refait la nuit 7, lot 2)
SOUSBOIS = "M_Kit_SousBois"   # atlas peint de la végétation (T_Ens_Feuillage_BaseColor), faces arrière masquées
CELL = {"buis": (0, 2), "lierre": (1, 2), "fougere": (2, 2), "herbe": (0, 1), "rose": (1, 1), "chene": (2, 1),
        "sapin": (0, 0), "ronce": (1, 0), "champi": (2, 0)}


class Cartes:
    """Cartes de feuillage texturées (atlas 3 x 3 de T_Ens_Feuillage_BaseColor). Chaque carte a deux faces dos à dos, à 3 mm
    l'une de l'autre, chacune avec sa normale forcée vers le ciel et l'extérieur de la touffe : avec le culling des faces
    arrière (M_Kit_SousBois), aucune face n'est vue de dos, donc aucune carte noire à contre-jour (le shader toon retourne
    la normale des faces vues de dos) et aucun z-fighting ; `faces_superposees` ne les voit pas comme superposées."""

    def __init__(self, rnd):
        import bmesh
        self.rnd = rnd
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.col = self.bm.verts.layers.float_color.new("Col")
        self.normales = {}

    def carte(self, base, w, h, cell, d, normale, roulis=0.0, plis=0.0, seg=3):
        """Carte w x h dont le pied (milieu du bas) est en `base`, tournée vers `d` (penchée si d.z > 0), courbée de `plis` m
        vers l'avant au sommet (fronde qui retombe). Le pied est enfoncé de 4 cm (jamais de jour sous la plante)."""
        from mathutils import Matrix
        i, j = CELL[cell]
        m = 0.012
        u0, u1, v0, v1 = i / 3 + m, (i + 1) / 3 - m, j / 3 + m, (j + 1) / 3 - m
        d = Vector(d).normalized()
        up = Vector((0, 0, 1)) if abs(d.z) < 0.95 else Vector((0, 1, 0))
        x = up.cross(d).normalized()
        y = d.cross(x).normalized()
        if y.z < 0:
            y = -y
        if roulis:
            R = Matrix.Rotation(math.radians(roulis), 3, d)
            x, y = R @ x, R @ y
        nn = Vector(normale).normalized()
        teinte = self.rnd.random()
        o = Vector(base) + Vector((0, 0, -0.04))
        for sens in (1, -1):
            dec = d * (0.0015 * sens)
            rows = []
            for k in range(seg + 1):
                t = k / seg
                off = d * (plis * t * t) + dec
                rows.append([self.bm.verts.new(o + x * (sx * w / 2) + y * (h * t) + off) for sx in (-1, 1)])
            for r in rows:
                for v in r:
                    v[self.col] = (0.0, 0.0, teinte, 1.0)
            for k in range(seg):
                q = [rows[k][0], rows[k][1], rows[k + 1][1], rows[k + 1][0]]
                uvs = [(u0, v0 + (v1 - v0) * k / seg), (u1, v0 + (v1 - v0) * k / seg),
                       (u1, v0 + (v1 - v0) * (k + 1) / seg), (u0, v0 + (v1 - v0) * (k + 1) / seg)]
                if sens < 0:
                    q, uvs = q[::-1], uvs[::-1]
                f = self.bm.faces.new(q)
                f.smooth = True
                for l, uv in zip(f.loops, uvs):
                    l[self.uv].uv = uv
                self.normales[f] = nn

    def touffe(self, c, h, cell, n=3, larg=None, penche=0.0, etale=0.0):
        """n cartes croisées autour de c (herbes, ronces, rameaux), légèrement ouvertes vers l'extérieur."""
        a0 = self.rnd.uniform(0, 180)
        for i in range(n):
            a = math.radians(a0 + i * 180 / n + self.rnd.uniform(-12, 12))
            o = Vector(c) + Vector((self.rnd.uniform(-etale, etale), self.rnd.uniform(-etale, etale), 0))
            k = self.rnd.uniform(0.82, 1.15)
            d = Vector((math.cos(a), math.sin(a), penche * self.rnd.uniform(0.6, 1.2)))
            self.carte(o, (larg or h) * k, h * k, cell, d, (math.cos(a) * 0.15, math.sin(a) * 0.15, 1.0), self.rnd.uniform(-6, 6))

    def fougere(self, c, taille, n=6):
        """Touffe de fougère : n frondes en rosette, penchées vers l'extérieur, qui retombent au bout."""
        a0 = self.rnd.uniform(0, 6.28)
        for i in range(n):
            a = a0 + i * 6.2832 / n + self.rnd.uniform(-0.2, 0.2)
            s = taille * self.rnd.uniform(0.78, 1.12)
            d = Vector((math.cos(a), math.sin(a), self.rnd.uniform(0.45, 0.75)))
            base = Vector(c) + Vector((math.cos(a), math.sin(a), 0)) * 0.08 * taille
            self.carte(base, s * 0.9, s, "fougere", d, (math.cos(a) * 0.25, math.sin(a) * 0.25, 1.0),
                       self.rnd.uniform(-8, 8), plis=0.16 * s)

    def buisson(self, c, r, cell, n=26, taille=1.6, bas=False, repere=None):
        """Houppier : n cartes réparties sur un ellipsoïde de rayons r (x, y, z), tournées vers l'extérieur ; le dessous
        (z < -0.35) n'a pas de cartes sauf `bas`. Normales : direction radiale relevée vers le ciel."""
        c = Vector(c)
        for i in range(n):
            t = (i + 0.5) / n
            z = 1 - 2 * t
            if not bas and z < -0.35:
                continue
            a = i * 2.39996 + self.rnd.uniform(-0.3, 0.3)
            sz = math.sqrt(max(0.0, 1 - z * z))
            d = Vector((math.cos(a) * sz, math.sin(a) * sz, z))
            if repere is None:
                off = Vector((d.x * r[0], d.y * r[1], d.z * r[2]))
            else:      # repere : direction « avant » de la lisière ; r[1] s'applique le long de -repere (vers l'avant)
                av = Vector(repere).normalized()
                lat = Vector((-av.y, av.x, 0))
                a_ = d.dot(av)
                off = lat * d.dot(lat) * r[0] + av * a_ * (r[1] if a_ > 0 else r[0]) + Vector((0, 0, d.z * r[2]))
            p = c + off * self.rnd.uniform(0.78, 0.98)
            dd = (d + Vector((self.rnd.uniform(-.35, .35), self.rnd.uniform(-.35, .35), self.rnd.uniform(-.2, .35)))).normalized()
            w = taille * self.rnd.uniform(0.8, 1.2)
            self.carte(p - Vector((0, 0, w / 2)), w, w, cell, dd, d + Vector((0, 0, 0.6)), self.rnd.uniform(-35, 35), seg=1)

    def objet(self, nom, materiau):
        import bpy
        me = bpy.data.meshes.new(nom)
        N = []
        for f in self.bm.faces:
            N += [tuple(self.normales[f])] * len(f.loops)
        self.bm.to_mesh(me)
        self.bm.free()
        me.normals_split_custom_set(N)
        me.color_attributes.active_color = me.color_attributes.get("Col")
        ob = bpy.data.objects.new(nom, me)
        bpy.context.scene.collection.objects.link(ob)
        me.materials.append(L.mat(materiau))
        return ob


# =================================================================== 8. lisière de forêt (acte III)
def lisiere(S, nom, Lx=12.0, coin=False):
    """Lisière de forêt dense de Lx m (pivot au sol, au milieu du bord avant ; la forêt s'étend vers +Y sur 6.5 m) : trois rangs
    de troncs (Ø 0.35 à 1.1 m, 7 à 15.5 m, chaque rang finit dans sa couche de feuillage), trois couches de houppiers en cartes
    peintes (enfant `Deco_Feuillage`, contenues dans l'emprise des troncs et derrière 0.7 m), sous-bois en cartes, racines,
    fond noir à 6.5 m. Infranchissable : `Col_Lisiere` de -0.7 à 6.7 m de profondeur et 22 m de haut (troncs compris). `coin` : quart de cercle de 90° (angle d'une clairière)."""
    k = L.K(nom)
    kc = L.K("Col_Lisiere")
    rnd = random.Random(S["seed"] + 561 + (7 if coin else 0))
    def place(u, prof):
        """u : abscisse le long de la lisière (0..1) ; prof : profondeur (m) ; renvoie (point, normale vers l'avant)."""
        if not coin:
            return Vector((-Lx / 2 + u * Lx, prof, 0)), Vector((0, -1, 0))
        R = Lx * 0.66
        a = math.pi / 2 * u
        n_ = Vector((-math.cos(a), -math.sin(a), 0))
        return Vector((R * math.cos(a), R * math.sin(a), 0)) + Vector((-R, -R, 0)) - n_ * prof, n_
    # troncs sur trois rangs
    hauts = ((7.0, 9.5), (9.5, 12.5), (12.0, 15.5))    # nuit 7 : chaque rang finit dans sa couche de feuillage (pas de perche nue)
    for rang, (prof, nb) in enumerate(((0.6, int(Lx / 1.1)), (2.6, int(Lx / 1.0)), (4.6, int(Lx / 1.2)))):
        for i in range(nb):
            u = (i + 0.5 + rnd.uniform(-0.35, 0.35)) / nb
            p, n_ = place(u, prof + rnd.uniform(-0.5, 0.5))
            r = rnd.uniform(0.18, 0.55) * (1.0 if rang == 0 else 0.85)
            h = rnd.uniform(*hauts[rang])
            pts = [p + Vector((0, 0, -0.3))]
            lean = Vector((rnd.uniform(-0.6, 0.6), rnd.uniform(-0.3, 0.6), 0))
            for s in range(1, 6):
                t = s / 5
                pts.append(p + lean * t * t + Vector((rnd.uniform(-0.1, 0.1), rnd.uniform(-0.1, 0.1), h * t)))
            L.tube(k, pts, [r * 1.5, r, r * 0.9, r * 0.75, r * 0.6, r * 0.45], S["ecorce"], sides=9, rough=0.12, seed=rnd.randint(0, 999))
            for a_ in range(4):
                ang = rnd.uniform(0, 2 * math.pi)
                d = Vector((math.cos(ang), math.sin(ang), 0))
                L.tube(k, [p + d * r * 0.6 + Vector((0, 0, 0.5)), p + d * (r + 0.9) + Vector((0, 0, -0.1)), p + d * (r + 1.5) + Vector((0, 0, -0.35))],
                       [r * 0.35, r * 0.22, r * 0.08], S["ecorce"], sides=6, rough=0.15, seed=rnd.randint(0, 999))
    # feuillage (refait la nuit 7) : houppiers en cartes peintes de l'atlas (chêne et buis devant, sapin au fond), trois
    # couches de plus en plus hautes et serrées ; plus d'aplats dentelés qui se lisaient en éclats sombres
    C = Cartes(random.Random(S["seed"] + 563 + (7 if coin else 0)))
    # houppiers contenus : en largeur dans l'emprise des troncs (u de 0.06 à 0.94, rayon borné par la distance au bout + 1 m),
    # en profondeur derrière 0.7 m (devant Col_Lisiere, rien au-dessus de 2 m) ; centrés à hauteur du haut des troncs du rang
    couches = ((2.2, 3.0, 8.4, ("chene", "chene", "buis"), 2.6, 2.6), (3.8, 3.8, 11.0, ("chene", "sapin"), 2.3, 3.1), (5.4, 4.6, 13.5, ("sapin",), 2.1, 3.4))
    for prof, r, zc, cells, dens, tc in couches:
        nb = int(Lx / r * dens) + 1
        for i in range(nb):
            u = 0.06 + 0.88 * ((i + 0.5) / nb + rnd.uniform(-0.03, 0.03))
            pr = prof + rnd.uniform(-0.4, 0.4)
            p, n_ = place(min(max(u, 0.06), 0.94), pr)
            bout = (min(u, 1 - u) * (Lx if not coin else Lx * 0.66 * 1.57)) + 1.0
            rr = min(r * rnd.uniform(0.85, 1.1), bout)
            rp = min(rr, pr - 0.7)                     # en profondeur, vers l'avant : jamais devant 0.7 m
            cc = p + Vector((0, 0, zc + rnd.uniform(-1.2, 1.2)))
            cell = rnd.choice(cells)
            C.buisson(cc, (rr, rp, rr * rnd.uniform(0.8, 1.0)), cell, n=int(34 + rr * 6), taille=tc, repere=n_)
            C.buisson(cc, (rr * 0.55, rp * 0.6, rr * 0.5), cell, n=14, taille=tc * 0.9, bas=True, repere=n_)    # cœur
    # sous-bois au pied des troncs : fougères, ronces et herbes (mêmes cartes que les sous-bois du kit)
    nb = int(Lx / 0.8)
    for i in range(nb):
        u = (i + rnd.uniform(0.1, 0.9)) / nb
        p, n_ = place(u, rnd.uniform(-0.3, 1.6))
        x = rnd.random()
        if x < 0.4:
            C.fougere(p, rnd.uniform(0.8, 1.2), n=rnd.randint(5, 7))
        elif x < 0.65:
            C.touffe(p, rnd.uniform(0.7, 1.1), "ronce", n=2, penche=0.15)
        elif x < 0.8:
            C.touffe(p, rnd.uniform(0.6, 0.9), "chene", n=2, penche=0.3)
        else:
            C.touffe(p, rnd.uniform(0.45, 0.8), "herbe", n=3)
    # fond noir : on ne voit jamais au travers
    if coin:
        R = Lx * 0.66 + 6.5
        for s in range(8):
            a0, a1 = math.pi / 2 * s / 8, math.pi / 2 * (s + 1) / 8
            P0 = Vector((R * math.cos(a0), R * math.sin(a0), 0)) + Vector((-Lx * 0.66, -Lx * 0.66, 0))
            P1 = Vector((R * math.cos(a1), R * math.sin(a1), 0)) + Vector((-Lx * 0.66, -Lx * 0.66, 0))
            k.face([P0, P1, P1 + Vector((0, 0, 18)), P0 + Vector((0, 0, 18))], FOND)
    else:
        k.face([(-Lx / 2, 6.5, -0.5), (-Lx / 2, 6.5, 18), (Lx / 2, 6.5, 18), (Lx / 2, 6.5, -0.5)], FOND)
    # collision : boîte(s) infranchissable(s) juste derrière le sous-bois
    if coin:
        for s in range(6):
            p0, _ = place(s / 6, 3.0)
            p1, _ = place((s + 1) / 6, 3.0)
            c = (p0 + p1) / 2
            d = p1 - p0
            kc.box((c.x, c.y, 11.0), (d.length + 0.8, 7.4, 22.0), COL, rot=math.atan2(d.y, d.x))
    else:
        kc.box((0, 3.0, 11.0), (Lx, 7.4, 22.0), COL)          # de -0.7 à 6.7 m de profondeur, 22 m de haut
    ob = k.finish(S["seed"] + 561, dict(sale_h=0.6, mousse=0.3))
    H.enfant(ob, kc, 0)
    deco = C.objet("Deco_Feuillage", SOUSBOIS)
    deco.parent = ob
    return ob


# =================================================================== 9. sous-bois, rochers, grand arbre
def sous_bois(S, nom, R=2.5, var=0):
    """Sous-bois de Ø 2R m (pivot au sol, au centre), en cartes peintes de l'atlas de végétation : fougères en rosette au
    cœur, rameaux de chêne bas, ronces et herbes hautes en couronne, champignons pâles au pied d'une branche morte (A) ou
    d'une souche (B) (taches claires lisibles de nuit), deux ou trois pierres moussues. Feuillage traversable
    (`Deco_SansCollision`) ; branche, souche et pierres gardent leur collision (corps de la pièce)."""
    k = L.K(nom)
    rnd = random.Random(S["seed"] + 571 + var * 13)
    C = Cartes(rnd)

    def pos(rmin, rmax):
        a = rnd.uniform(0, 6.2832)
        dd = math.sqrt(rnd.uniform((rmin / R) ** 2, (rmax / R) ** 2)) * R
        return Vector((math.cos(a) * dd, math.sin(a) * dd, 0)), a
    # 1. cœur : fougères (une grande au centre, d'autres autour), 0.7 à 1.25 m
    C.fougere((rnd.uniform(-0.2, 0.2), rnd.uniform(-0.2, 0.2), 0), 1.15 if var == 0 else 0.95, n=7)
    for i in range(5 if var == 0 else 3):
        p, _ = pos(0.35 * R, 0.72 * R)
        C.fougere(p, rnd.uniform(0.7, 1.0) * (1.0 if var == 0 else 0.9), n=rnd.randint(5, 7))
    # 2. rameaux de chêne bas (feuilles larges, contraste avec les fougères)
    for i in range(3 if var == 0 else 2):
        p, a = pos(0.2 * R, 0.6 * R)
        C.touffe(p, rnd.uniform(0.55, 0.8), "chene", n=2, penche=0.35)
    # 3. couronne : ronces et herbes hautes, plus clairsemées vers le bord
    for i in range(int(7 * R)):
        p, a = pos(0.55 * R, 1.0 * R)
        if rnd.random() < 0.35:
            C.touffe(p, rnd.uniform(0.55, 0.9), "ronce", n=2, penche=0.15)
        else:
            C.touffe(p, rnd.uniform(0.4, 0.75), "herbe", n=3)
    # 4. touffes d'herbe basses entre les fougères (le sol ne se lit pas en trous)
    for i in range(int(5 * R)):
        p, a = pos(0.15 * R, 0.8 * R)
        C.touffe(p, rnd.uniform(0.28, 0.45), "herbe", n=2)
    # 5. bois mort au sol : branche (A) ou souche basse (B), enfoncé ; champignons pâles à son pied
    a = rnd.uniform(0, math.pi)
    dx, dy = math.cos(a), math.sin(a)
    if var == 0:
        p0, p1 = Vector((-dx * R * 0.85, -dy * R * 0.85, 0.06)), Vector((dx * R * 0.55, dy * R * 0.55, 0.05))
        L.tube(k, [p0, p0.lerp(p1, 0.5) + Vector((0, 0, 0.05)), p1], [0.13, 0.11, 0.07], S["ecorce"], sides=8, rough=0.18, seed=rnd.randint(0, 99))
        m_ = p0.lerp(p1, 0.35)
        L.tube(k, [m_ + Vector((0, 0, 0.12)), m_ + Vector((-dy * 0.6, dx * 0.6, 0.18)), m_ + Vector((-dy * 0.95, dx * 0.9, 0.1))],
               [0.05, 0.035, 0.015], S["ecorce"], sides=6, rough=0.15, seed=rnd.randint(0, 99))
        pied = [p0.lerp(p1, t) + Vector((-dy, dx, 0)) * 0.22 * (1 if i % 2 else -1) for i, t in enumerate((0.15, 0.3, 0.62))]
    else:
        c0 = Vector((dx * R * 0.35, dy * R * 0.35, 0))
        L.tube(k, [c0 + Vector((0, 0, -0.08)), c0 + Vector((0, 0, 0.22)), c0 + Vector((0.02, 0.0, 0.38))], [0.24, 0.2, 0.17], S["ecorce"], sides=9, rough=0.2, seed=rnd.randint(0, 99))
        for r_ in range(4):
            b_ = r_ * 1.57 + rnd.uniform(-0.3, 0.3)
            d_ = Vector((math.cos(b_), math.sin(b_), 0))
            L.tube(k, [c0 + d_ * 0.12 + Vector((0, 0, 0.12)), c0 + d_ * 0.38, c0 + d_ * 0.6 + Vector((0, 0, -0.06))],
                   [0.07, 0.05, 0.02], S["ecorce"], sides=6, rough=0.15, seed=rnd.randint(0, 99))
        pied = [c0 + Vector((math.cos(b_), math.sin(b_), 0)) * 0.36 for b_ in (a + 1.0, a + 2.4, a + 4.2)]
    for q in pied:
        C.touffe(q, rnd.uniform(0.22, 0.32), "champi", n=2)
    # 6. pierres moussues (posées, base enfoncée)
    for i in range(3 if var == 0 else 2):
        p, _ = pos(0.45 * R, 0.85 * R)
        t_ = rnd.uniform(0.28, 0.45)
        L.rock_blob(k, (p.x, p.y, t_ * 0.25 - 0.06), (t_ * 1.3, t_, t_ * 0.6), S["roche"], seed=rnd.randint(0, 999), res=10, amp=0.2)
    k.empty("Centre", (0, 0, 0))
    ob = k.finish(S["seed"] + 571 + var, dict(sale_h=0.3, mousse=0.8))
    deco = C.objet("Deco_SansCollision", SOUSBOIS)
    deco.parent = ob
    return ob


def rocher(S, nom, taille=2.5, var=0):
    """Rocher taillé de `taille` m (pivot au sol, au centre) : bloc organique recoupé par 6 à 8 plans de clivage (facettes
    nettes, arêtes vives, lisibles au cel-shading), base enfoncée de 10 cm ; mousse et herbe sèche sur le dessus par
    peinture de sommets ; collision par le maillage."""
    k = L.K(nom)
    rnd = random.Random(S["seed"] + 581 + var)
    def bloc(c, sz, nb):
        k.bm.verts.ensure_lookup_table()
        n0 = len(k.bm.verts)
        blocs.append(n0)
        L.rock_blob(k, c, sz, S["roche"], seed=rnd.randint(0, 999), res=20, amp=0.22)
        k.bm.verts.ensure_lookup_table()
        vs = [k.bm.verts[i] for i in range(n0, len(k.bm.verts))]
        cc = Vector(c) + Vector((0, 0, sz[2] * 0.3))
        for _ in range(nb):
            az = rnd.uniform(0, 2 * math.pi); el = rnd.uniform(-0.15, 0.9)
            nrm = Vector((math.cos(az) * math.cos(el), math.sin(az) * math.cos(el), math.sin(el))).normalized()
            ext = abs(nrm.x) * sz[0] / 2 + abs(nrm.y) * sz[1] / 2 + abs(nrm.z) * sz[2] / 2
            dmax = ext * rnd.uniform(0.62, 0.8)
            for v in vs:
                t = (v.co - cc).dot(nrm)
                if t > dmax:
                    v.co -= nrm * (t - dmax)
    blocs = []
    bloc((0, 0, taille * 0.32 - 0.1), (taille, taille * rnd.uniform(0.75, 0.95), taille * 0.68), 8)
    if taille > 3:
        bloc((taille * 0.42, taille * 0.2, taille * 0.12), (taille * 0.42, taille * 0.38, taille * 0.32), 6)
    # nuit 7 : les plans de clivage rognaient le bas du bloc (rochers à 0.3 - 0.7 m du sol). Recalage par bloc : le bas réel
    # (1er centile de ses sommets) passe à -0.08 m (base plane enfoncée de 8 cm), rien ne descend sous -0.12 m.
    # nuit 7, relecture : chaque bloc (corps, épaulement) est calé séparément, sinon le plus bas des deux portait l'autre
    k.bm.verts.ensure_lookup_table()
    bornes_ = blocs + [len(k.bm.verts)]
    for b0, b1 in zip(bornes_[:-1], bornes_[1:]):
        vs = [k.bm.verts[i] for i in range(b0, b1)]
        zs = sorted(v.co.z for v in vs)
        z0 = zs[len(zs) // 100]
        for v in vs:
            v.co.z = max(v.co.z - z0 - 0.08, -0.12)
    ob = k.finish(S["seed"] + 581 + var, dict(sale_h=0.4, mousse=0.6))
    return ob


def grand_arbre(S, nom, h=20.0):
    """Grand arbre praticable (pivot au sol, au centre du tronc) : tronc de Ø 2.2 m, racines en contreforts, trois grosses
    branches **plates sur le dessus** (largeur 1.2 m) à 3.4, 6.6 et 9.8 m qui montent en spirale (on saute de l'une à l'autre,
    3.2 m d'écart vertical : saut et hissage), couronne de houppiers en cartes peintes au-dessus de 12 m (enfant `Deco_Feuillage`). Collision : tronc par le
    maillage, `Col_Branche_*` (dessus des branches)."""
    k = L.K(nom)
    C = Cartes(random.Random(S["seed"] + 593))
    kc = L.K("Col_Branches")
    rnd = random.Random(S["seed"] + 591)
    R0 = 1.1
    pts = [Vector((0, 0, -0.4))] + [Vector((rnd.uniform(-0.15, 0.15), rnd.uniform(-0.15, 0.15), h * t)) for t in (0.2, 0.45, 0.7, 0.88, 1.0)]
    L.tube(k, pts, [R0 * 1.35, R0, R0 * 0.88, R0 * 0.7, R0 * 0.5, R0 * 0.28], S["ecorce"], sides=14, rough=0.1, seed=7)
    # collision du tronc (relecture : il se traversait) : prisme octogonal par tronçons qui suit le rayon
    for z0, z1, r in ((-0.4, 4.0, R0 * 1.05), (4.0, 9.0, R0 * 0.85), (9.0, 15.0, R0 * 0.65)):
        poly = [(math.cos(2 * math.pi * i / 8) * r, math.sin(2 * math.pi * i / 8) * r) for i in range(8)]
        H.prism(kc, poly, z0, z1, COL, axis="z")
    for i in range(7):
        a = 2 * math.pi * i / 7 + rnd.uniform(-0.2, 0.2)
        d = Vector((math.cos(a), math.sin(a), 0))
        L.tube(k, [d * R0 * 0.7 + Vector((0, 0, 1.6)), d * (R0 + 1.1) + Vector((0, 0, 0.3)), d * (R0 + 2.4) + Vector((0, 0, -0.3))],
               [0.7, 0.42, 0.12], S["ecorce"], sides=8, rough=0.15, seed=rnd.randint(0, 999))
    # branches praticables : poutre organique + dessus plat (planche d'écorce) ; montée en spirale
    for i, (z, ang, Lb) in enumerate(((3.4, 0.3, 5.5), (6.6, 2.4, 5.0), (9.8, 4.4, 4.6))):
        d = Vector((math.cos(ang), math.sin(ang), 0))
        a = d * R0 * 0.6 + Vector((0, 0, z - 0.55))
        b = d * (R0 + Lb) + Vector((0, 0, z - 0.25))
        # branche sous le dessus plat (relecture : l'écorce dépassait de 10 à 27 cm au-dessus)
        a = d * R0 * 0.6 + Vector((0, 0, z - 0.75))
        b = d * (R0 + Lb) + Vector((0, 0, z - 0.42))
        L.tube(k, [a, a.lerp(b, 0.5) + Vector((0, 0, 0.05)), b], [0.72, 0.55, 0.38], S["ecorce"], sides=10, rough=0.05, seed=rnd.randint(0, 999))
        # dessus plat : dalle d'écorce aplanie 1.4 m de large
        n_ = Vector((-d.y, d.x, 0))
        p0 = d * (R0 * 0.9) + Vector((0, 0, z))
        p1 = d * (R0 + Lb - 0.2) + Vector((0, 0, z))
        w = 1.2
        k.face([p0 - n_ * w / 2, p1 - n_ * 0.34, p1 + n_ * 0.34, p0 + n_ * w / 2], S["ecorce"])
        # tranche du dessus plat (0.12 m) : on lit une planche d'écorce taillée, pas une languette
        for (u0, u1) in ((p0 - n_ * w / 2, p1 - n_ * 0.34), (p1 + n_ * 0.34, p0 + n_ * w / 2), (p1 - n_ * 0.34, p1 + n_ * 0.34)):
            k.face([u0, u1, u1 - Vector((0, 0, 0.14)), u0 - Vector((0, 0, 0.14))], S["ecorce"])
        H.rampe_col(kc, [p0 - n_ * w / 2 + Vector((0, 0, 0.01)), p1 - n_ * 0.34 + Vector((0, 0, 0.01)),
                         p1 + n_ * 0.34 + Vector((0, 0, 0.01)), p0 + n_ * w / 2 + Vector((0, 0, 0.01))])
        k.empty(f"Appui_Branche_{i + 1}", tuple(p1))
        # petites branches et feuillage au bout
    # couronne
    for i in range(9):
        a = 2 * math.pi * i / 9
        d = Vector((math.cos(a), math.sin(a), 0))
        L.tube(k, [Vector((0, 0, 11 + i * 0.5)), d * 3.5 + Vector((0, 0, 13 + i * 0.4))], [0.4, 0.15], S["ecorce"], sides=6, rough=0.1, seed=i)
        rr = rnd.uniform(2.4, 3.4)
        cc = d * rnd.uniform(3.0, 5.0) + Vector((0, 0, rnd.uniform(13.5, 17.5)))
        C.buisson(cc, (rr, rr, rr * 0.85), "chene" if i % 3 else "sapin", n=int(34 + rr * 6), taille=2.6)
        C.buisson(cc, (rr * 0.55, rr * 0.55, rr * 0.5), "chene", n=14, taille=2.2, bas=True)
    C.buisson(Vector((0, 0, h - 0.5)), (3.6, 3.6, 3.0), "chene", n=56, taille=2.9)
    C.buisson(Vector((0, 0, h - 0.5)), (2.0, 2.0, 1.6), "sapin", n=18, taille=2.4, bas=True)
    ob = k.finish(S["seed"] + 591, dict(sale_h=0.8, mousse=0.5))
    H.enfant(ob, kc, 0)
    deco = C.objet("Deco_Feuillage", SOUSBOIS)
    deco.parent = ob
    return ob


# =================================================================== 10. sols percés : puits et fosse
def _dalle_percee(k, A, ep, trou, m_dessus, m_tranche, rond=False, n=24):
    """Dalle de A × A m (dessus à 0) percée au centre : carré de côté `trou` ou disque de Ø `trou` (rond). Faces en
    éventail entre le bord carré et le contour du trou (aucune face confondue)."""
    h = A / 2
    contour = []
    if rond:
        for i in range(n):
            a = 2 * math.pi * i / n + math.pi / 4
            contour.append(Vector((math.cos(a) * trou / 2, math.sin(a) * trou / 2, 0)))
    else:
        t = trou / 2
        for (x, y) in ((t, t), (-t, t), (-t, -t), (t, -t)):
            contour.append(Vector((x, y, 0)))
    coins = [Vector((h, h, 0)), Vector((-h, h, 0)), Vector((-h, -h, 0)), Vector((h, -h, 0))]
    # chaque point du contour est relié au coin du même quadrant
    per = len(contour) // 4
    for q in range(4):
        c0, c1 = coins[q], coins[(q + 1) % 4]
        seg = contour[q * per:(q + 1) * per + 1] if q < 3 else contour[3 * per:] + [contour[0]]
        # éventail : coin c0 vers la moitié du segment, coin c1 vers l'autre moitié
        mid = len(seg) // 2
        for i in range(mid):
            k.face([c0, seg[i], seg[i + 1]][::-1], m_dessus)
        for i in range(mid, len(seg) - 1):
            k.face([c1, seg[i], seg[i + 1]][::-1], m_dessus)
        k.face([c0, seg[mid], c1][::-1], m_dessus)
    # dessous et tranches extérieures
    for q in range(4):
        c0, c1 = coins[q], coins[(q + 1) % 4]
        k.face([c0, c1, c1 - Vector((0, 0, ep)), c0 - Vector((0, 0, ep))], m_tranche)
    return contour


def sol_puits(S, nom, A=8.0):
    """Dalle de sol de A × A m (dessus à 0, pivot au centre) percée au Ø 3.2 m du puits, avec le puits du lot 3 (margelle,
    eau `Deco_Eau` à -1.5 m, fond à -2.4 m) posé dans l'ouverture : à poser à la place d'une dalle de sol, le puits a son trou."""
    ob = H.puits(S, nom)
    k = L.K("Dalle")
    contour = _dalle_percee(k, A, 0.6, 3.25, S["sol"] if S["act"] != "Jardin" else "M_Jardin_SolMelange", S["mur"], rond=True)
    for v in k.bm.verts:          # dessus à -4 mm : jamais confondu avec la base de la margelle
        v.co.z -= 0.004
    # paroi du trou sous la dalle (jusqu'au fût du puits)
    for i in range(len(contour)):
        a, b = contour[i], contour[(i + 1) % len(contour)]
        k.face([a, b, b - Vector((0, 0, 0.6)), a - Vector((0, 0, 0.6))], S["mur"])
    H.enfant(ob, k, S["seed"] + 601, peinture=H.PEINT_ARCHI)
    return ob


def sol_fosse(S, nom, A=8.0, B=4.0, prof=3.4):
    """Dalle de sol de A × A m (dessus à 0, pivot au centre) percée d'une fosse de B × B m et `prof` m (3.4 m : on en
    ressort par un saut et un hissage, 3.7 m) : parois appareillées, fond de terre et d'éboulis, quatre pieux, une échelle
    (décor : main n'a pas de mécanique d'escalade ; `Accroche_Echelle` si elle en ajoute une)."""
    k = L.K(nom)
    _dalle_percee(k, A, 0.6, B, S["sol"] if S["act"] != "Jardin" else "M_Jardin_SolMelange", S["mur"])
    t = B / 2
    for (x0, y0, x1, y1) in ((t, t, -t, t), (-t, t, -t, -t), (-t, -t, t, -t), (t, -t, t, t)):
        k.face([(x0, y0, 0), (x1, y1, 0), (x1, y1, -prof), (x0, y0, -prof)], S["mur"])
    k.face([(-t, -t, -prof), (t, -t, -prof), (t, t, -prof), (-t, t, -prof)], S.get("terre", S["sol"]))
    rnd = random.Random(S["seed"] + 611)
    for i in range(7):
        L.rock_blob(k, (rnd.uniform(-t + 0.4, t - 0.4), rnd.uniform(-t + 0.4, t - 0.4), -prof + 0.1), (rnd.uniform(0.3, 0.7),) * 3,
                    S.get("roche", S["mur"]), seed=rnd.randint(0, 999), res=6, amp=0.3)
    for i in range(4):
        x, y = rnd.uniform(-t + 0.6, t - 0.6), rnd.uniform(-t + 0.6, t - 0.6)
        k.cyl((x, y, -prof), 0.07, 1.1, S["bois"], n=6, r2=0.0)
    # bordure de la fosse : pierres de taille (lisible de loin : on voit le trou)
    for (cx, cy, sx, sy) in ((0, t + 0.2, B + 0.8, 0.4), (0, -t - 0.2, B + 0.8, 0.4), (t + 0.2, 0, 0.4, B), (-t - 0.2, 0, 0.4, B)):
        k.box((cx, cy, 0.05 + H.dz()), (sx, sy, 0.1), S["pierre"])
    # échelle contre la paroi nord
    kg = L.K("Echelle")
    for sx in (-1, 1):
        kg.box((sx * 0.3, t - 0.12, -prof / 2 + 0.4), (0.08, 0.08, prof + 0.8), S["bois"])
    for i in range(int(prof / 0.3)):
        kg.box((0, t - 0.12, -prof + 0.3 + i * 0.3), (0.6, 0.05, 0.05), S["bois"])
    k.empty("Accroche_Echelle", (0, t - 0.2, -prof))
    ob = k.finish(S["seed"] + 611, H.PEINT_ARCHI)
    H.enfant(ob, kg, S["seed"] + 612)
    return ob


def sol_dalle(S, nom, A=8.0, ep=0.6):
    """Dalle de sol de A × A m (dessus à 0, pivot au centre) en sol mélangé du jardin (`M_Jardin_SolMelange` : terre,
    gravier, herbe sèche, mousse, transitions douces) : à poser bord à bord, les plaques du mélange suivent les
    coordonnées du monde (aucune coupure entre deux dalles)."""
    k = L.K(nom)
    k.box((0, 0, -ep / 2), (A, A, ep), S["mur"], skip=("+z",))
    k.face([(-A / 2, -A / 2, 0), (A / 2, -A / 2, 0), (A / 2, A / 2, 0), (-A / 2, A / 2, 0)], "M_Jardin_SolMelange")
    return k.finish(S["seed"] + 631, H.PEINT_ARCHI)


# =================================================================== 11. support de râtelier (acte II)
def support_ratelier(S, nom):
    """Petit support de râtelier (présentoir) de 0.6 × 0.5 × 1.6 m (hampes à 1.4 m, fers à 1.6 m) : pied en croix, montant, traverse à trois encoches où
    reposent deux lances et une hallebarde (pivot au sol, au centre). Remplace la boîte greybox `Prop_Ratelier` de petite
    taille (acte II)."""
    k = L.K(nom)
    b, fer = S["bois"], S["fer"]
    k.box((0, 0, 0.04), (0.6, 0.1, 0.08), b)
    k.box((0, 0, 0.04), (0.1, 0.5, 0.08), b)
    k.box((0, 0, 0.62), (0.09, 0.09, 1.16), b)
    k.box((0, 0, 1.18), (0.56, 0.12, 0.08), b)
    k.box((0, 0, 0.36), (0.5, 0.1, 0.06), b)
    for i, x in enumerate((-0.18, 0.0, 0.18)):
        y = 0.0
        k.cyl((x, y, 0.08), 0.022, 1.32, b, n=6)       # posée sur la traverse du pied (dessus à 0.08)
        if i == 1:
            k.face([(x, y, 1.4), (x + 0.12, y, 1.32), (x + 0.12, y, 1.22), (x, y, 1.3)], fer)
            k.face([(x, y, 1.3), (x + 0.12, y, 1.22), (x + 0.12, y, 1.32), (x, y, 1.4)], fer)
            k.cyl((x, y, 1.4), 0.03, 0.12, fer, n=4, r2=0.0)
        else:
            k.cyl((x, y, 1.4), 0.035, 0.18, fer, n=4, r2=0.0)
    ob = k.finish(S["seed"] + 621, H.PEINT_OBJET)
    return ob


# =================================================================== enregistrement par acte
def pieces_caves(S):
    return [
        ("SM_Cave_PontSuspendu_12m", pont_suspendu, dict(), "Pont suspendu de 12 m (planches de bois pourri sur cordes, flèche de 0.8 m, deux planches manquantes), poteaux aux bouts."),
        ("SM_Cave_Rebord_3m", rebord, dict(), "Rebord de mur de 3 × 0.9 m : dalle brute sur trois corbeaux, dessus plat à 0 (saut, hissage, wall jump)."),
        ("SM_Cave_PlateformeTraction", plateforme_traction, dict(), "Plateforme de traction de 3 × 3 m suspendue par quatre chaînes : plaque magnétique gravée dessous et sur la tranche avant."),
        ("SM_Cave_SalleHaute_8x8", salle_haute, dict(), "Salle en hauteur de 8 × 8 m, plancher à 5 m sur quatre poteaux : fenêtre au fond, porte à gauche, garde-corps, passage d'échelle."),
        ("SM_Cave_Passerelle_Pente_10m", passerelle_pente, dict(), "Passerelle en pente : monte de 3 m sur 10 m (16.7°), rampe lisse, garde-corps de corde."),
        ("SM_Cave_Sol_Puits_8x8", sol_puits, dict(), "Dalle de sol de 8 × 8 m percée, avec le puits dans son ouverture (eau à -1.5 m, fond à -2.4 m)."),
        ("SM_Cave_Sol_Fosse_8x8", sol_fosse, dict(), "Dalle de sol de 8 × 8 m percée d'une fosse de 4 × 4 × 3.4 m : éboulis, pieux, échelle de décor."),
    ]


def pieces_chateau(S):
    return [
        ("SM_Chateau_PontSuspendu_12m", pont_suspendu, dict(), "Pont suspendu de 12 m (planches sur chaînes, flèche de 0.8 m), piliers de pierre aux bouts."),
        ("SM_Chateau_Rebord_3m", rebord, dict(), "Rebord de mur de 3 × 0.9 m : corniche moulurée sur trois consoles, dessus plat à 0."),
        ("SM_Chateau_PlateformeTraction", plateforme_traction, dict(), "Plateforme de traction de 3 × 3 m suspendue par quatre chaînes : plaque magnétique gravée dessous et sur la tranche avant."),
        ("SM_Chateau_SalleHaute_8x8", salle_haute, dict(), "Salle en hauteur de 8 × 8 m, plancher à 5 m sur quatre piliers : fenêtre au fond, porte à gauche, balustrade, passage d'échelle."),
        ("SM_Chateau_Passerelle_Pente_10m", passerelle_pente, dict(), "Passerelle en pente : monte de 3 m sur 10 m (16.7°), dallée, balustrade, rampe lisse."),
        ("SM_Chateau_Balcon_Pierre_4m", balcon_pierre, dict(), "Balcon de pierre de 4 × 1.8 m en encorbellement, trois consoles moulurées, balustrade."),
        ("SM_Chateau_Sol_Fosse_8x8", sol_fosse, dict(), "Dalle de sol de 8 × 8 m percée d'une fosse de 4 × 4 × 3.4 m : éboulis, pieux, échelle de décor."),
        ("SM_Chateau_Fenetre_Vue_Simple", fenetre_vue, dict(w=4.0, h=12.0, bw=1.6, sill=3.0, top=9.6, nl=1),
         "Fenêtre en lancette simple qui montre l'extérieur (ciel étoilé, lune, paysage lointain) : baie de 1.6 × 6.6 m dans un panneau de 4 × 12 m."),
        ("SM_Chateau_Fenetre_Vue_Double", fenetre_vue, dict(w=8.0, h=12.0, bw=4.0, sill=2.5, top=9.8, nl=2, oculus=(8.95, 0.5, 4)),
         "Fenêtre à deux lancettes et oculus qui montre l'extérieur : baie de 4 × 7.3 m dans un panneau de 8 × 12 m."),
        ("SM_Chateau_Fenetre_Vue_Triple", fenetre_vue, dict(w=8.0, h=20.0, bw=5.6, sill=3.5, top=17.8, nl=3, oculus=(16.0, 0.85, 6)),
         "Haute fenêtre à trois lancettes et rose qui montre l'extérieur : baie de 5.6 × 14.3 m dans un panneau de 8 × 20 m."),
        ("SM_Chateau_SupportRatelier", support_ratelier, dict(), "Petit support de râtelier de 0.6 × 0.5 × 1.6 m : deux lances et une hallebarde."),
    ]


def pieces_jardin(S):
    return [
        ("SM_Jardin_PontSuspendu_12m", pont_suspendu, dict(), "Pont suspendu de 12 m (planches sur cordes, flèche de 0.8 m), poteaux de rondins aux bouts."),
        ("SM_Jardin_Rebord_3m", rebord, dict(), "Rebord de mur de 3 × 0.9 m : dalle sur trois corbeaux, lierre qui pend, dessus plat à 0."),
        ("SM_Jardin_PlateformeTraction", plateforme_traction, dict(), "Plateforme de traction de 3 × 3 m suspendue par quatre chaînes : plaque magnétique gravée dessous et sur la tranche avant."),
        ("SM_Jardin_SalleHaute_8x8", salle_haute, dict(), "Salle en hauteur de 8 × 8 m (pavillon), plancher à 5 m sur quatre piliers : fenêtre, porte, garde-corps, passage d'échelle."),
        ("SM_Jardin_Passerelle_Pente_10m", passerelle_pente, dict(), "Passerelle en pente : monte de 3 m sur 10 m (16.7°), planches, garde-corps de bois, rampe lisse."),
        ("SM_Jardin_Lisiere_12m", lisiere, dict(), "Lisière de forêt dense de 12 m : troncs sur trois rangs, houppiers et sous-bois en cartes peintes, fond noir ; infranchissable."),
        ("SM_Jardin_Lisiere_Coin", lisiere, dict(coin=True), "Lisière de forêt en quart de cercle (angle d'une clairière, rayon 8 m) ; infranchissable."),
        ("SM_Jardin_SousBois_A", sous_bois, dict(var=0), "Touffe de sous-bois de Ø 5 m : fougères et buissons bas, branche morte ; traversable."),
        ("SM_Jardin_SousBois_B", sous_bois, dict(R=1.6, var=1), "Petite touffe de sous-bois de Ø 3.2 m ; traversable."),
        ("SM_Jardin_Rocher_Petit", rocher, dict(taille=1.2, var=0), "Rocher de 1.2 m (s'enjambe d'un saut), mousse sur le dessus."),
        ("SM_Jardin_Rocher_Moyen", rocher, dict(taille=2.6, var=1), "Rocher de 2.6 m (on s'y hisse), mousse et herbe sèche sur le dessus."),
        ("SM_Jardin_Rocher_Grand", rocher, dict(taille=5.0, var=2), "Gros rocher de 5 m à épaulement (double saut), mousse sur le dessus."),
        ("SM_Jardin_GrandArbre_Praticable", grand_arbre, dict(), "Grand arbre de 20 m, tronc de Ø 2.2 m : trois branches plates à 3.4, 6.6 et 9.8 m qui montent en spirale."),
        ("SM_Jardin_Sol_Puits_8x8", sol_puits, dict(), "Dalle de sol de 8 × 8 m percée, avec le puits de jardin dans son ouverture."),
        ("SM_Jardin_Sol_Fosse_8x8", sol_fosse, dict(), "Dalle de sol de 8 × 8 m percée d'une fosse de 4 × 4 × 3.4 m : éboulis, pieux, échelle de décor."),
        ("SM_Jardin_Sol_8x8", sol_dalle, dict(), "Dalle de sol de 8 × 8 m en sol mélangé (terre, gravier, herbe sèche, mousse)."),
    ]
