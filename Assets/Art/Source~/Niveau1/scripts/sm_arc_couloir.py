# -*- coding: utf-8 -*-
"""SM_Arc_Couloir : arc brisé (tiers-point) de couloir, d'après fiche3d_arc_couloir_v02.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_arc_couloir.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
Changer une cote dans PARAMS puis relancer : la pièce est reconstruite, exportée et rendue.
Pivot : au sol, au milieu entre les deux pieds. Les pieds reposent sur des colonnes de 8 m.
"""
import sys, os, math, importlib
sys.dont_write_bytecode = True          # pas de __pycache__ dans Assets/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)   # recharge toujours la version du dossier courant
import it_common as C
from mathutils import Vector

PARAMS = dict(
    largeur=14.0,          # hors tout (sabots compris)
    hauteur=11.0,          # hors tout (clé comprise)
    face=1.0,              # largeur de la nervure vue de face
    profondeur=1.4,        # profondeur de la nervure
    # mouluration : (début du retrait depuis l'extrados, profondeur du retrait) ; 3 retraits
    retraits=[(0.0, 1.4), (0.35, 1.16), (0.7, 0.92)],
    sabot=(1.2, 1.6, 0.25),     # largeur, profondeur, hauteur des sabots
    cle_dessus=0.2,        # saillie de la clé au-dessus de l'extrados
    cle_saillie=0.1,       # saillie de la clé devant et derrière la nervure
    cle_demi_bas=0.45,     # demi-largeur de la clé à l'intrados
    cle_demi_haut=0.7,     # demi-largeur de la clé en haut
    claveaux=8,            # claveaux par côté (clé non comprise)
    joint=0.03,            # chanfrein des joints entre claveaux
    sous_div=2,            # segments par claveau
)


def build(P, extra=None, name="SM_Arc_Couloir"):
    """extra(B, geo) : ajouts avant la finition (variante à écoinçons) ; geo : cotes de l'arc."""
    mats = {"pierre": C.tile_material("pierre")}
    B = C.Builder()
    B.set_uv_params("pierre", *C.TILES["pierre"][1:])
    tw, th = C.TILES["pierre"][1:]

    sw, sd, sh = P["sabot"]
    face, depth = P["face"], P["profondeur"]
    z0 = sh
    s_e = P["largeur"] / 2 - (sw - face) / 2          # demi-portée à l'extrados, au pied
    h_e = P["hauteur"] - P["cle_dessus"] - z0         # flèche de l'extrados
    a, Re = C.pointed_arch_centers(s_e, h_e)
    Ri = Re - face
    cx = -a                                           # centre de l'arc droit (x), en z = z0

    # profil (r depuis l'extrados vers l'intérieur, y) en escalier, CCW
    st = P["retraits"]
    bounds = [s[0] for s in st] + [face]
    prof = []
    for i, (r0, d) in enumerate(st):                       # côté +y, de l'extrados vers l'intrados
        prof += [(bounds[i], d / 2), (bounds[i + 1], d / 2)]
    for i in reversed(range(len(st))):                    # côté -y, retour
        prof += [(bounds[i + 1], -st[i][1] / 2), (bounds[i], -st[i][1] / 2)]
    # nettoie les doublons
    pr = []
    for p in prof:
        if not pr or (abs(p[0] - pr[-1][0]) > 1e-9 or abs(p[1] - pr[-1][1]) > 1e-9):
            pr.append(p)
    prof = pr
    area = sum(prof[i][0] * prof[(i + 1) % len(prof)][1] - prof[(i + 1) % len(prof)][0] * prof[i][1] for i in range(len(prof)))
    if area < 0:
        prof = prof[::-1]
    m = len(prof)
    inset = C.offset_polygon(prof, P["joint"])
    # normales extérieures des côtés du profil (en r, y)
    pn = []
    for k in range(m):
        (ar, ay), (br, by) = prof[k], prof[(k + 1) % m]
        pn.append((by - ay, -(br - ar)))
    # abscisse le long du périmètre pour v
    per = [0.0]
    for k in range(m):
        per.append(per[-1] + math.dist(prof[k], prof[(k + 1) % m]))   # m + 1 valeurs

    def pt(theta, r, y, sx):
        rho = Re - r
        return Vector((sx * (cx + rho * math.cos(theta)), y, z0 + rho * math.sin(theta)))

    H = P["hauteur"]

    def zi(x):                       # intrados (côté droit)
        return z0 + math.sqrt(Ri * Ri - (x - cx) ** 2)
    # côté droit de la clé : droite P1 (intrados) -> P2 (sommet)
    P1 = Vector((P["cle_demi_bas"], zi(P["cle_demi_bas"])))
    P2 = Vector((P["cle_demi_haut"], H))
    tl = (P2 - P1).normalized()
    nl = Vector((tl.y, -tl.x))       # normale vers l'extérieur (côté des claveaux)

    def theta_line(rho, off):
        """Angle où le point de rayon rho est à la distance off de la droite de la clé."""
        lo, hi = 0.0, math.pi / 2
        for _ in range(60):
            mid = (lo + hi) / 2
            q = Vector((cx + rho * math.cos(mid), z0 + rho * math.sin(mid)))
            if (q - P1).dot(nl) > off:
                lo = mid
            else:
                hi = mid
        return (lo + hi) / 2

    theta_k = theta_line(Re, 0.0)
    n = P["claveaux"]
    eps = P["joint"] / (Re - face / 2)
    for sx in (1, -1):
        for c in range(n):
            ts, te = theta_k * c / n, theta_k * (c + 1) / n
            last = c == n - 1
            spec = []                              # (angle ou None si contre la clé, type, décalage)
            if c == 0:
                spec.append((ts, "full", 0))
            else:
                spec += [(ts, "inset", 0), (ts + eps, "full", 0)]
            for j in range(1, P["sous_div"]):
                spec.append((ts + (te - ts) * j / P["sous_div"], "full", 0))
            if last:
                spec += [(None, "full", P["joint"]), (None, "inset", 0.0)]
            else:
                spec += [(te - eps, "full", 0), (te, "inset", 0)]
            rings, uvr, tapprox = [], [], []
            for t, kd, off in spec:
                pr_ = inset if kd == "inset" else prof
                ring, uvs = [], []
                for k in range(m + 1):
                    r, y = pr_[k % m]
                    th_ = t if t is not None else theta_line(Re - r, off)
                    if k < m:
                        ring.append(pt(th_, r, y, sx))
                    uvs.append(((Re - r) * th_ / tw * sx, per[k] / th))
                rings.append(ring); uvr.append(uvs)
                tapprox.append(t if t is not None else theta_line(Re - face / 2, off))

            def hint(i, k, pts, thetas=tapprox, sx=sx):
                tm = (thetas[i] + thetas[i + 1]) / 2
                nr, ny = pn[k]
                radial_in = Vector((-math.cos(tm) * sx, 0, -math.sin(tm)))
                return radial_in * nr + Vector((0, ny, 0))
            t0, t1 = tapprox[0], tapprox[-1]
            capn = (Vector((math.sin(t0) * sx, 0, -math.cos(t0))), Vector((-math.sin(t1) * sx, 0, math.cos(t1))))
            sweep_faces(B, rings, uvr, hint, capn)

    # clé de voûte : trapèze qui traverse toute la nervure, dessous en pointe (suit l'intrados)
    z_ia = z0 + math.sqrt(Ri * Ri - a * a)
    dz = 0.06                                      # la clé descend un peu sous l'intrados
    key = [(0, z_ia - dz), (P1.x, P1.y - dz), (P2.x, H), (-P2.x, H), (-P1.x, P1.y - dz)]
    yk = depth / 2 + P["cle_saillie"]
    B.prism_y(key, -yk, yk, "pierre", bevel=0.02)

    # sabots
    xc = s_e - face / 2
    for sx in (1, -1):
        B.box((sx * xc - sw / 2, -sd / 2, 0), (sx * xc + sw / 2, sd / 2, sh), "pierre", skip=("-z",), bevel=0.03)

    if extra:
        extra(B, dict(s_e=s_e, z0=z0, a=a, Re=Re, Ri=Ri, cx=cx, theta_k=theta_k, P1=P1, P2=P2, H=H, depth=depth))
    ob = B.finish(name, mats)
    print("arc : a=%.3f Re=%.3f Ri=%.3f extrados sommet z=%.3f intrados sommet z=%.3f theta_k=%.1f°" % (
        a, Re, Ri, z0 + math.sqrt(Re * Re - a * a), z_ia, math.degrees(theta_k)))
    return [ob]


def sweep_faces(B, rings, uvr, hint, capn):
    B.new_shell()
    m = len(rings[0])
    for i in range(len(rings) - 1):
        for k in range(m):
            k2 = (k + 1) % m
            pts = [rings[i][k], rings[i][k2], rings[i + 1][k2], rings[i + 1][k]]
            uvs = [uvr[i][k], uvr[i][k + 1], uvr[i + 1][k + 1], uvr[i + 1][k]]
            B.face(pts, "pierre", hint(i, k, pts), uvs)
    B.face(rings[0], "pierre", capn[0])
    B.face(rings[-1], "pierre", capn[1])


if __name__ == "__main__":
    C.run_piece("SM_Arc_Couloir", build, PARAMS, "SM_Arc_Couloir")
