# -*- coding: utf-8 -*-
"""SM_Cage (+ SM_Cage_Os) : cage de prison (Prop_Cage, 2.2 × 3 × 2.2 m), d'après prop_cage_v01.

Cylindrique à dôme, en fer rouillé : base ronde pleine (Ø 2.2 m), 44 barreaux verticaux ronds de Ø 0.04 m
(≈ 0.15 m d'écart), 22 barreaux de dôme qui convergent vers un moyeu, anneau fixe au sommet (3 m), 3 cerclages
plats (bas, milieu, base du dôme). Porte grillagée fermée sur l'avant (-Y Blender = +Z Unity) : cadre plat,
traverse au milieu (le cerclage du milieu s'interrompt au droit de la porte), 2 charnières à gauche, moraillon
et cadenas à droite. Os simples sur le plancher : objet séparé SM_Cage_Os (même pivot), matériau M_Os.

Pivot : au sol, au centre.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_cage.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F
from mathutils import Vector

PARAMS = dict(
    base=dict(r=1.1, h=0.12, segs=24),
    R=1.05, barreaux=44, barreau_r=0.02, barreau_cotes=6,
    z_dome=2.05, dome=dict(barreaux=22, haut=0.67, moyeu_r=0.15, segs=5, cotes=4),
    moyeu=dict(r=0.18, h=0.1),
    anneau=dict(r=0.08, fil=0.02, haut=3.0),
    cerclages=dict(z=(0.16, 1.05, 2.05), largeur=0.07, epaisseur=0.025, segs=24),
    porte=dict(demi_angle=17.0, z0=0.2, z1=1.88, plat=(0.05, 0.02), steps=4),
    cadenas=dict(z=1.0),
    os=[  # (x, y, angle °, longueur)
        (-0.35, 0.25, 30, 0.42), (0.3, -0.15, -55, 0.36), (0.1, 0.5, 80, 0.3)],
)


def arc_band(B, R, a0, a1, zc, w, t, steps, mat, closed=False):
    """Bande plate (largeur w sur Z, épaisseur t radiale) au rayon R, de l'angle a0 à a1 (radians)."""
    n = steps
    rings = []
    for i in range(n + (0 if closed else 1)):
        a = a0 + (a1 - a0) * i / n
        c, s = math.cos(a), math.sin(a)
        rings.append([Vector((r * c, r * s, z)) for r, z in ((R - t / 2, zc - w / 2), (R + t / 2, zc - w / 2),
                                                              (R + t / 2, zc + w / 2), (R - t / 2, zc + w / 2))])
    if closed:
        rings.append(rings[0])
        B.sweep(rings, mat, caps=(False, False))
    else:
        B.sweep(rings, mat)


def build(P):
    mats = N.materials()
    B = N.builder()
    b = P["base"]
    C.revolve(B, [(b["r"], 0.0), (b["r"], b["h"])], b["segs"], "fer", cap_top=True, cap_bottom=True)
    R, nb = P["R"], P["barreaux"]
    zd = P["z_dome"]
    for k in range(nb):
        a = 2 * math.pi * k / nb
        C.tube(B, [Vector((R * math.cos(a), R * math.sin(a), b["h"])), Vector((R * math.cos(a), R * math.sin(a), zd))],
               P["barreau_r"], P["barreau_cotes"], "fer", caps=False)
    D = P["dome"]
    for k in range(D["barreaux"]):
        a = 2 * math.pi * k / D["barreaux"]
        path = []
        for i in range(D["segs"] + 1):
            ph = (math.pi / 2) * i / D["segs"]
            r = D["moyeu_r"] + (R - D["moyeu_r"]) * math.cos(ph)
            path.append(Vector((r * math.cos(a), r * math.sin(a), zd + D["haut"] * math.sin(ph))))
        C.tube(B, path, P["barreau_r"], D["cotes"], "fer", caps=False, phase=math.pi / 4)
    # moyeu bombé et anneau fixe
    M = P["moyeu"]
    zt = zd + D["haut"]
    C.revolve(B, [(M["r"], zt - 0.03), (M["r"] * 0.9, zt + M["h"] * 0.5), (M["r"] * 0.45, zt + M["h"])], 12, "fer",
              cap_top=True, cap_bottom=True)
    A = P["anneau"]
    zc = A["haut"] - A["r"] - A["fil"]
    ring = [Vector((A["r"] * math.cos(2 * math.pi * k / 12), 0.0, zc + A["r"] * math.sin(2 * math.pi * k / 12))) for k in range(12)]
    C.tube(B, ring, A["fil"], 4, "fer", closed=True, up=Vector((0, 1, 0)))
    # cerclages ; celui du milieu s'interrompt au droit de la porte
    Cc = P["cerclages"]
    Pd = P["porte"]
    front = -math.pi / 2
    da = math.radians(Pd["demi_angle"])
    for j, z in enumerate(Cc["z"]):
        if j == 1:
            arc_band(B, R, front + da, front + 2 * math.pi - da, z, Cc["largeur"], Cc["epaisseur"], Cc["segs"] - 2, "fer")
        else:
            arc_band(B, R, 0.0, 2 * math.pi, z, Cc["largeur"], Cc["epaisseur"], Cc["segs"], "fer", closed=True)
    # porte : cadre plat (haut, bas, milieu) un peu à l'extérieur, montants, charnières, moraillon et cadenas
    w, t = Pd["plat"]
    Rp = R + 0.03
    for z in (Pd["z0"], 1.05, Pd["z1"]):
        arc_band(B, Rp, front - da, front + da, z, w, t, Pd["steps"], "fer")
    for sgn in (-1, 1):
        a = front + sgn * da
        c = Vector((Rp * math.cos(a), Rp * math.sin(a), (Pd["z0"] + Pd["z1"]) / 2))
        rad = Vector((math.cos(a), math.sin(a), 0))
        tan = Vector((-math.sin(a), math.cos(a), 0))
        N.obox(B, c, tan, rad, (0, 0, 1), w / 2, t / 2, (Pd["z1"] - Pd["z0"]) / 2 + w / 2, "fer")
        if sgn < 0:   # charnières (côté gauche vu de face)
            for zh in (Pd["z0"] + 0.2, Pd["z1"] - 0.2):
                N.obox(B, c + rad * 0.02 + Vector((0, 0, zh - c.z)), tan, rad, (0, 0, 1), 0.05, 0.02, 0.06, "fer")
        else:         # moraillon et cadenas (côté droit)
            zl = P["cadenas"]["z"]
            pl = c + rad * 0.025 + Vector((0, 0, zl - c.z))
            N.obox(B, pl, tan, rad, (0, 0, 1), 0.07, 0.012, 0.09, "fer")
            body = pl + rad * 0.045 + Vector((0, 0, -0.1))
            N.obox(B, body, tan, rad, (0, 0, 1), 0.045, 0.02, 0.05, "fer")
            sh = [body + Vector((0, 0, 0.05)) + tan * 0.028 * math.cos(math.pi * i / 4) + Vector((0, 0, 0.05 * math.sin(math.pi * i / 4)))
                  for i in range(5)]
            C.tube(B, sh, 0.008, 4, "fer", caps=False, up=rad)
    cage = B.finish("SM_Cage", mats)

    # os sur le plancher (objet séparé)
    B2 = N.builder()
    z0 = b["h"]
    for x, y, ang, L in P["os"]:
        a = math.radians(ang)
        d = Vector((math.cos(a), math.sin(a), 0))
        c = Vector((x, y, z0 + 0.025))
        C.tube(B2, [c - d * L / 2, c + d * L / 2], 0.018, 5, "os", up=Vector((0, 0, 1)))
        side = Vector((-d.y, d.x, 0))
        for e in (-1, 1):
            for s2 in (-1, 1):
                N.obox(B2, c + d * e * L / 2 + side * s2 * 0.022, d, side, (0, 0, 1), 0.03, 0.025, 0.024, "os")
    bones = B2.finish("SM_Cage_Os", mats)
    F.attach(bones, cage)
    print("cage %d, os %d triangles" % (C.tri_count(cage), C.tri_count(bones)))
    return [cage, bones]


if __name__ == "__main__":
    N.run("SM_Cage", build, PARAMS, views=("face", "cote", "34"))
