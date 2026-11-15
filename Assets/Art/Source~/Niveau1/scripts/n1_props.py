# -*- coding: utf-8 -*-
"""Outils communs aux props du niveau 1 et de l'arène (lot de modélisation, groupes B et C).

S'appuie sur it_common (constructeur, UV en échelle monde, export, vérification, rendus) et sur
fps_common (tuiles peintes, matériaux en aplat, repères vides).

Conventions (Blender) : Z en haut, avant vers -Y ; export FBX : dans Unity, +Y en haut, +Z vers
l'avant, rotation nulle, échelle 1. Rendus en JPEG (qualité 90).
"""
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "..", "FPS", "scripts"))
for m in ("it_common", "fps_common"):
    sys.modules.pop(m, None)
import it_common as C
import fps_common as F
import bpy
from mathutils import Vector

C.RENDER_FMT = "JPEG"
C.RENDER_DIST_ADD = 0.0
TEX_N1 = C.TEX_DIR

# Tuiles peintes du niveau : 256 px pour 1 m (256 px/m, densité visée du décor)
PAINT_N1 = {
    "T_Bois_Brut_BaseColor": dict(ramp=["#3E2A1E", "#55392A", "#6E4C34", "#8A6544"], trainees=(1.5, 30.0), seed=53,
                                  rouille="#2A1C14", rouille_part=0.05),
    "T_Chiffon_Brule_BaseColor": dict(ramp=["#141010", "#221A16", "#33271F", "#4A3A2C"], trainees=(10.0, 3.0), seed=61,
                                      rouille="#0C0A09", rouille_part=0.15),
}
TILE_N1 = 1.0          # une tuile = 1 m


def materials():
    """Matériaux de Blender (mêmes noms que les .mat d'Unity)."""
    m = {k: C.tile_material(k) for k in ("pierre", "fer", "egout", "dalles")}
    for key, tex, name in (("bois", "T_Bois_Brut_BaseColor", "M_Bois_Brut"),
                           ("chiffon", "T_Chiffon_Brule_BaseColor", "M_Chiffon_Brule")):
        path = F.painted_tile(tex, PAINT_N1[tex], out_dir=TEX_N1)
        m[key] = C.make_material(name, path)
    m["corde"] = F.flat_material("M_Corde", "#7A6548")
    m["os"] = F.flat_material("M_Os", "#CFC3A6")
    m["noir"] = F.flat_material("M_Fond_Noir", "#070606")     # fonds de trous (bouche du tuyau)
    return m


def uv_setup(B):
    for k in ("pierre", "egout", "dalles", "fer"):
        B.set_uv_params(k, *C.TILES[k][1:])
    for k in ("bois", "chiffon"):
        B.set_uv_params(k, TILE_N1, TILE_N1)
    for k in ("corde", "os", "noir"):
        B.set_uv_params(k, TILE_N1, TILE_N1)


def builder():
    B = C.Builder()
    uv_setup(B)
    return B


def obox(B, c, ex, ey, ez, hx, hy, hz, mat, skip=(), shell=True):
    """Boîte orientée : centre c, axes unitaires ex, ey, ez, demi-tailles. skip parmi '-x' ... '+z'."""
    if shell:
        B.new_shell()
    c, ex, ey, ez = Vector(c), Vector(ex), Vector(ey), Vector(ez)
    def P(a, b, d):
        return c + ex * a * hx + ey * b * hy + ez * d * hz
    faces = {
        "-x": ([P(-1, -1, -1), P(-1, 1, -1), P(-1, 1, 1), P(-1, -1, 1)], -ex),
        "+x": ([P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1)], ex),
        "-y": ([P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1)], -ey),
        "+y": ([P(-1, 1, -1), P(1, 1, -1), P(1, 1, 1), P(-1, 1, 1)], ey),
        "-z": ([P(-1, -1, -1), P(1, -1, -1), P(1, 1, -1), P(-1, 1, -1)], -ez),
        "+z": ([P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)], ez),
    }
    return [B.face(p, mat, n) for k, (p, n) in faces.items() if k not in skip]


def beam(B, a, b, w, h, mat, up=(0, 0, 1), caps=True):
    """Pièce de section rectangulaire w × h de a à b (h mesurée selon up projeté)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    L = d.length
    ez = d / L
    upv = Vector(up)
    if abs(upv.dot(ez)) > 0.95:
        upv = Vector((1, 0, 0)) if abs(ez.x) < 0.9 else Vector((0, 1, 0))
    ex = upv.cross(ez).normalized()
    ey = ez.cross(ex).normalized()
    return obox(B, (a + b) / 2, ex, ey, ez, w / 2, h / 2, L / 2, mat, skip=() if caps else ("-z", "+z"))


def collar(B, c, axis, rin, rout, h, sides, mat, phase=0.0):
    """Collier (anneau épais) d'axe donné : faces extérieure, intérieure, dessus et dessous."""
    B.new_shell()
    c, ax = Vector(c), Vector(axis).normalized()
    ref = Vector((1, 0, 0)) if abs(ax.x) < 0.9 else Vector((0, 1, 0))
    u = ax.cross(ref).normalized()
    v = ax.cross(u).normalized()
    def ring(r, dz):
        return [c + ax * dz + (u * math.cos(phase + 2 * math.pi * k / sides) + v * math.sin(phase + 2 * math.pi * k / sides)) * r
                for k in range(sides)]
    o0, o1, i0, i1 = ring(rout, -h / 2), ring(rout, h / 2), ring(rin, -h / 2), ring(rin, h / 2)
    for k in range(sides):
        k2 = (k + 1) % sides
        mid = (o0[k] + o0[k2]) / 2 + ax * 0 - c
        rad = (mid - ax * mid.dot(ax)).normalized()
        B.face([o0[k], o0[k2], o1[k2], o1[k]], mat, rad)
        B.face([i0[k], i0[k2], i1[k2], i1[k]], mat, -rad)
        B.face([o1[k], o1[k2], i1[k2], i1[k]], mat, ax)
        B.face([o0[k], o0[k2], i0[k2], i0[k]], mat, -ax)


def rivet(B, p, n, s, h, mat):
    """Petite pyramide (rivet) posée en p, pointant selon n (4 triangles, sans base)."""
    B.new_shell()
    p, n = Vector(p), Vector(n).normalized()
    ref = Vector((0, 0, 1)) if abs(n.z) < 0.9 else Vector((1, 0, 0))
    u = n.cross(ref).normalized()
    v = n.cross(u).normalized()
    sq = [p + (u * a + v * b) * s for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    apex = p + n * h
    for j in range(4):
        tri = [sq[j], sq[(j + 1) % 4], apex]
        B.face(tri, mat, (sum(tri, Vector()) / 3) - (p - n * 0.01))


def orient(B, M):
    """Transforme la géométrie du constructeur avant finish() : les UV en échelle monde sont alors
    calculées dans l'orientation finale (coulures de la texture vers le bas)."""
    import bmesh
    bmesh.ops.transform(B.bm, matrix=M, verts=list(B.bm.verts))


def run(name, build, params, views=("face", "cote", "34"), render_objects=None):
    """Construit, exporte, sauvegarde, vérifie (maillages et repères) et rend une pièce."""
    res = C.run_piece(name, build, params, name, render_objects=render_objects, views=views)
    print(F.verify_empties(os.path.join(C.MODELS_DIR, name + ".fbx")))
    return res
