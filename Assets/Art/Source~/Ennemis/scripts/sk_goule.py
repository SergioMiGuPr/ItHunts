# -*- coding: utf-8 -*-
"""SK_Goule : la goule gréée et animée (retours de playtest du 2026-11-16).

- Maillages : ceux de sm_goule (texture repeinte, LOD0 22 000, LOD1 11 000, LOD2 5 000 triangles), 1.1 m de haut (le jeu
  met l'objet à l'échelle, ≈ 1.8 m prévu), avant vers -Y (Blender) = +Z (Unity), pivot au sol.
- Squelette « Goule » (26 os, tous déformants), placé à la main sur le modèle ; métarig Rigify : chaque chaîne porte son
  type Rigify (colonne basic_spine, cou et tête super_head, mâchoire et épaules super_copy, bras front_paw, jambes
  rear_paw) ; Rigify > Generate en tire un rig de contrôle pour animer à la main. Les os exportés sont ceux du métarig.
- Peau (2026-11-16) : poids lissés (poids_lisses : chaleur sur une copie étanche en voxels, report sur le LOD0, lissage,
  4 os par sommet), recopiés sur les LOD1 et LOD2 ; os d'aide à demi-rotation et os correcteurs du creux des genoux
  (Jambe_Pli, voir PLI_SEUIL).
- Animations (30 images/s, sans déplacement de la racine sauf la mort) : Goule_Repos, Goule_Course, Goule_Annonce,
  Goule_Bond, Goule_Reception, Goule_Escalade, Goule_Coup, Goule_Etourdie, Goule_Mort (durées et événements :
  INTEGRATION-PROPS.md § 4 quater).

Lancer dans Blender (avec l'interface) :
    p = r"<dépôt>/Assets/Art/Source~/Ennemis/scripts/sk_goule.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
for _m in ("sm_goule", "peinture", "ennemis_common"):
    sys.modules.pop(_m, None)
import sm_goule as G
sys.modules.pop("poids_lisses", None)
import poids_lisses as PL
E, C = G.E, G.C
import bpy
import numpy as np
from mathutils import Vector, Quaternion, Matrix, kdtree

FPS = 30
VOXEL = 0.006          # copie étanche pour la chaleur (modèle de 1.1 m)
SMOOTH_IT = 0          # passes de lissage des poids (0 : la chaleur est déjà diffuse ; lisser élargit le pli des genoux)
HELPERS = [n + s for n in ("Bras", "AvantBras", "Main", "Jambe", "Pied") for s in (".L", ".R")]   # épaules, coudes, poignets, genoux, chevilles
# Genou très replié (retours du 2026-11-16, PR #8 : genou gauche de Goule_Mort passé de 1.78 à 2.17) : os correcteur
# Jambe_Pli.L/.R, enfant de Jambe_Aide, même repos. Il prend la part de l'aide sur le côté comprimé du genou (−Z local de
# Jambe, rampe sur PLI_LARGEUR m) ; un pilote (driver) le pousse le long de son Z local de PLI_GAIN m par radian au-delà de
# PLI_SEUIL degrés entre la cuisse et la jambe. Sous le seuil il suit l'aide exactement : les clips dont le genou reste
# sous le seuil (tous sauf Goule_Mort : Escalade monte à 101.3°, Mort à 106.2°) ne changent pas. L'export FBX cuit le
# pilote dans les clips ; une animation faite dans Blender (vidéos de référence) le déclenche d'elle-même.
PLI_SEUIL, PLI_GAIN, PLI_LARGEUR = 101.5, 0.11, 0.02

# ----------------------------------------------------------------------------- squelette (modèle de 1.1 m, gauche = +X)
SPINE = [  # nom, tête, queue, parent, relié
    ("Racine", (0, 0, 0), (0, 0, 0.12), None, False),
    ("Bassin", (0, 0.24, 0.60), (0, 0.20, 0.72), "Racine", False),
    ("Dos_1", (0, 0.20, 0.72), (0, 0.12, 0.83), "Bassin", True),
    ("Dos_2", (0, 0.12, 0.83), (0, 0.03, 0.90), "Dos_1", True),
    ("Poitrine", (0, 0.03, 0.90), (0, -0.06, 0.93), "Dos_2", True),
    ("Cou", (0, -0.06, 0.93), (0, -0.14, 0.95), "Poitrine", True),
    ("Tete", (0, -0.14, 0.95), (0, -0.16, 1.10), "Cou", True),
    ("Machoire", (0, -0.16, 0.92), (0, -0.32, 0.86), "Tete", False),
]
LIMBS = [  # côté gauche (+X) ; la droite en miroir
    ("Epaule", (0.05, 0.0, 0.88), (0.19, -0.03, 0.84), "Poitrine", False),
    ("Bras", (0.19, -0.03, 0.84), (0.22, -0.02, 0.50), "Epaule", True),
    ("AvantBras", (0.22, -0.02, 0.50), (0.22, -0.08, 0.18), "Bras", True),
    ("Main", (0.22, -0.08, 0.18), (0.24, -0.17, 0.06), "AvantBras", True),
    ("Doigts", (0.24, -0.17, 0.06), (0.25, -0.28, 0.01), "Main", True),
    ("Cuisse", (0.09, 0.24, 0.60), (0.13, 0.14, 0.35), "Bassin", False),
    ("Jambe", (0.13, 0.14, 0.35), (0.125, 0.27, 0.11), "Cuisse", True),
    ("Pied", (0.125, 0.27, 0.11), (0.14, 0.16, 0.02), "Jambe", True),
    ("Orteils", (0.14, 0.16, 0.02), (0.15, 0.07, 0.01), "Pied", True),
]
RIGIFY = {"Bassin": "spines.basic_spine", "Cou": "spines.super_head", "Machoire": "basic.super_copy",
          "Epaule.L": "basic.super_copy", "Epaule.R": "basic.super_copy", "Bras.L": "limbs.front_paw",
          "Bras.R": "limbs.front_paw", "Cuisse.L": "limbs.rear_paw", "Cuisse.R": "limbs.rear_paw"}


# Unity (conversion d'axes à l'import, bakeAxisConversion) tourne l'avant -Y vers -Z : le modèle est donc tourné d'un
# demi-tour autour de Z avant le gréage (avant vers +Y dans le .blend) ; les tables ci-dessus restent dans le repère du
# modèle de sm_goule (avant -Y), converties par F().
def F(p):
    return (-p[0], -p[1], p[2])


def bone_table():
    out = [(n, F(h), F(t), p, c) for n, h, t, p, c in SPINE]
    for side, sx in (("L", 1), ("R", -1)):
        for n, h, t, p, c in LIMBS:
            par = p if p in ("Poitrine", "Bassin") else p + "." + side
            out.append((n + "." + side, F((h[0] * sx, h[1], h[2])), F((t[0] * sx, t[1], t[2])), par, c))
    return out


def make_armature(name="Goule"):
    arm = bpy.data.armatures.new(name)
    ob = bpy.data.objects.new(name, arm)
    sc = bpy.context.window.scene if bpy.context.window else bpy.context.scene
    sc.collection.objects.link(ob)
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    vl.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for n, h, t, p, c in bone_table():
        b = arm.edit_bones.new(n)
        b.head, b.tail = Vector(h), Vector(t)
        b.roll = 0.0
        eb[n] = b
    for n, h, t, p, c in bone_table():
        if p:
            eb[n].parent = eb[p]
            eb[n].use_connect = c
    bpy.ops.object.mode_set(mode="OBJECT")
    for n, ty in RIGIFY.items():                              # métarig Rigify (types des chaînes)
        try:
            ob.pose.bones[n].rigify_type = ty
        except Exception:
            ob.pose.bones[n]["rigify_type"] = ty
    arm.display_type = "STICK"
    return ob


# ----------------------------------------------------------------------------- peau
def skin(meshes, arm_ob):
    """Poids lissés (module poids_lisses : chaleur sur une copie étanche en voxels, report, lissage, 4 os) sur le LOD0,
    recopiés sur les LOD1 et LOD2 (point le plus proche de la surface du LOD0)."""
    lod0 = meshes[0]
    made = PL.add_helpers(arm_ob, HELPERS)
    names = [b.name for b in arm_ob.data.bones if b.name != "Racine" and "_Aide" not in b.name]
    def base_original(co_, W_, names_):
        # base : la chaleur sur le maillage d'origine (bouche ouverte, hanches nettes) ; la copie en voxels comble les
        # sommets où elle a échoué (trous du maillage Tripo)
        Wo = PL.heat_original(lod0, arm_ob, names_)
        ok = Wo.sum(1, keepdims=True) > 0.5
        # jambes (sous le genou) : la copie en voxels, plus régulière autour du genou
        leg = sum(W_[:, names_.index(n + s)] for n in ("Jambe", "Pied", "Orteils") for s in (".L", ".R"))[:, None]
        return np.where(ok, leg * W_ + (1 - leg) * Wo, W_)
    names, W, rep = PL.skin_lisse(lod0, arm_ob, VOXEL, names=names, smooth_it=SMOOTH_IT, helpers=made,
                                  mix=base_original)
    rep += " ; %d os d'aide (%s)" % (len(made), ", ".join(h for h, _ in made))
    co, tris = PL.surface(lod0)
    PL.copy_to(meshes[1:], co, tris, W, names, arm_ob)
    knee_correctives(arm_ob, meshes)
    rep += " ; os correcteurs des genoux (Jambe_Pli), seuil %.1f°" % PLI_SEUIL
    return rep


def knee_correctives(arm_ob, meshes):
    """Os correcteurs du creux des genoux (voir PLI_SEUIL), puis partage des poids sur chaque maillage (LOD compris)."""
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(o == arm_ob)
    vl.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_ob.data.edit_bones
    for s_ in ("L", "R"):
        a_ = eb["Jambe_Aide." + s_]
        h = eb.new("Jambe_Pli." + s_)
        h.head, h.tail, h.roll = a_.head.copy(), a_.tail.copy(), a_.roll
        h.parent = a_; h.use_connect = False; h.use_deform = True
    bpy.ops.object.mode_set(mode="OBJECT")
    for s_ in ("L", "R"):
        nm, aide, joint, parent = "Jambe_Pli." + s_, "Jambe_Aide." + s_, "Jambe." + s_, "Cuisse." + s_
        pb = arm_ob.pose.bones[nm]
        pb.rotation_mode = "QUATERNION"
        dr = pb.driver_add("location", 2).driver
        dr.type = "SCRIPTED"
        v = dr.variables.new(); v.name = "a"; v.type = "ROTATION_DIFF"
        v.targets[0].id = arm_ob; v.targets[0].bone_target = parent
        v.targets[1].id = arm_ob; v.targets[1].bone_target = joint
        dr.expression = "max(0.0, a - %f) * %f" % (math.radians(PLI_SEUIL), PLI_GAIN)
    for ob in meshes:
        knee_split(arm_ob, ob)


def knee_split(arm_ob, ob):
    """Part de Jambe_Aide passée à Jambe_Pli sur le côté comprimé du genou ; jamais de 5e influence ni de part sous le
    seuil de nettoyage (sous PLI_SEUIL, la déformation reste exactement celle d'avant)."""
    me = ob.data
    names = [g.name for g in ob.vertex_groups]
    W = np.zeros((len(me.vertices), len(names)), np.float32)
    for v in me.vertices:
        for g in v.groups:
            W[v.index, g.group] = g.weight
    co = np.array([v.co[:] for v in me.vertices])
    for s_ in ("L", "R"):
        nm, aide, joint = "Jambe_Pli." + s_, "Jambe_Aide." + s_, "Jambe." + s_
        bj = arm_ob.data.bones[joint]
        side = (co - np.array(bj.head_local[:])) @ (-np.array(bj.matrix_local.col[2][:3]))
        x = np.clip(side / PLI_LARGEUR, 0, 1)
        f = x * x * (3 - 2 * x)
        hcol = names.index(aide)
        nnz = (W > 0.01).sum(1)
        f = np.where((nnz >= 4) & (f > 0), 1.0, f)                 # pas de 5e influence
        move = W[:, hcol] * f
        keep = W[:, hcol] - move
        move = np.where(move < 0.015, 0.0, np.where(keep < 0.015, W[:, hcol], move))   # rien sous le seuil de nettoyage
        W = np.concatenate([W, move[:, None].astype(W.dtype)], 1)
        W[:, hcol] -= move
        names.append(nm)
    PL.write_groups(ob, arm_ob, names, PL.prune(W))


# ----------------------------------------------------------------------------- poses (rotations en axes de l'armature)
# x : bascule (+ = penche l'avant vers le bas ; un membre pendant part vers l'arrière) ; y : roulis (+ = le haut part vers
# la gauche, +X) ; z : lacet (+ = tourne vers la gauche).
AXES = {"x": Vector((-1, 0, 0)), "y": Vector((0, -1, 0)), "z": Vector((0, 0, 1))}     # axes du modèle avant demi-tour


def apply_pose(arm_ob, spec, root=(0, 0, 0)):
    for pb in arm_ob.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion()
        pb.location = (0, 0, 0)
    for name, rots in spec.items():
        pb = arm_ob.pose.bones[name]
        R = pb.bone.matrix_local.to_quaternion()
        q = Quaternion()
        for ax, ang in rots:
            q = Quaternion(AXES[ax], math.radians(ang)) @ q
        pb.rotation_quaternion = R.inverted() @ q @ R
    rb = arm_ob.pose.bones["Racine"]
    rb.location = rb.bone.matrix_local.to_quaternion().inverted() @ Vector(F(root))


def add(spec, bone, ax, ang):
    spec.setdefault(bone, []).append((ax, ang))


def lerp_spec(a, b, u):
    """Mélange de deux poses exprimées en angles (même axe, même ordre) : angles interpolés."""
    out = {}
    for bone in set(a) | set(b):
        ra, rb = dict(), dict()
        for ax, v in a.get(bone, []):
            ra[ax] = ra.get(ax, 0) + v
        for ax, v in b.get(bone, []):
            rb[ax] = rb.get(ax, 0) + v
        out[bone] = [(ax, ra.get(ax, 0) * (1 - u) + rb.get(ax, 0) * u) for ax in ("y", "z", "x")]
    return out


def ss(x):
    x = min(max(x, 0.0), 1.0)
    return x * x * (3 - 2 * x)


def both(spec, name, ax, ang, mirror_sign=1):
    add(spec, name + ".L", ax, ang)
    add(spec, name + ".R", ax, ang * (mirror_sign if ax in ("y", "z") else 1))


# poses clés -----------------------------------------------------------------------------------------------------------
def pose_crouch(depth=1.0, jaw=32.0):
    s = {}
    add(s, "Bassin", "x", 8 * depth)
    both(s, "Cuisse", "x", -22 * depth); both(s, "Jambe", "x", 36 * depth); both(s, "Pied", "x", -16 * depth)
    both(s, "Bras", "x", -10 * depth); both(s, "AvantBras", "x", -26 * depth); both(s, "Main", "x", 20 * depth)
    add(s, "Cou", "x", -16 * depth); add(s, "Tete", "x", -10 * depth); add(s, "Machoire", "x", jaw)
    return s, (0, 0, -0.10 * depth)


def pose_leap():
    s = {}
    add(s, "Bassin", "x", -10); add(s, "Dos_1", "x", -6); add(s, "Dos_2", "x", -6)
    both(s, "Bras", "x", -78); both(s, "AvantBras", "x", -12); both(s, "Main", "x", -22); both(s, "Doigts", "x", -12)
    both(s, "Bras", "y", -10, -1)
    both(s, "Cuisse", "x", 46); both(s, "Jambe", "x", -6); both(s, "Pied", "x", 30)
    add(s, "Cou", "x", -16); add(s, "Machoire", "x", 36)
    return s, (0, 0, 0.05)


def blend_pose(pa, pb, u):
    sa, ra = pa; sb, rb = pb
    return lerp_spec(sa, sb, u), tuple(ra[i] * (1 - u) + rb[i] * u for i in range(3))


REST = ({}, (0, 0, 0))


# animations : fonction t (s) -> (spec, racine), durée, boucle ------------------------------------------------------------
def a_repos(t):
    ph = 2 * math.pi * t / 2.0
    s = {}
    add(s, "Dos_2", "x", 2.0 * math.sin(ph)); add(s, "Poitrine", "x", 1.5 * math.sin(ph + 0.4))
    add(s, "Cou", "z", 5 * math.sin(ph)); add(s, "Tete", "y", 3 * math.sin(ph + 1.0)); add(s, "Tete", "x", 2 * math.sin(2 * ph))
    add(s, "Machoire", "x", 4 + 3 * math.sin(2 * ph))
    both(s, "Doigts", "x", 4 * math.sin(2 * ph + 0.5))
    both(s, "Epaule", "x", 1.5 * math.sin(ph + 0.4))
    return s, (0, 0, 0.006 * math.sin(ph))


def a_course(t):
    T = 0.6
    ph = 2 * math.pi * t / T
    s = {}
    add(s, "Bassin", "x", 8 - 5 * math.sin(ph)); add(s, "Dos_1", "x", 6 * math.sin(ph)); add(s, "Dos_2", "x", 5 * math.sin(ph))
    add(s, "Cou", "x", -10 - 6 * math.sin(ph)); add(s, "Machoire", "x", 10 + 4 * math.sin(2 * ph))
    for side, ofs in (("L", 0.0), ("R", 0.45)):
        f = ph + math.pi + ofs                       # membres avant
        h = ph + ofs                                 # membres arrière
        add(s, "Bras." + side, "x", -30 * math.sin(f) - 8)
        add(s, "AvantBras." + side, "x", -28 * max(0.0, math.cos(f)))
        add(s, "Main." + side, "x", 22 * max(0.0, math.cos(f)) - 6 * math.sin(f))
        add(s, "Cuisse." + side, "x", -30 * math.sin(h) - 6)
        add(s, "Jambe." + side, "x", 38 * max(0.0, math.cos(h)) + 6)
        add(s, "Pied." + side, "x", -18 * max(0.0, math.cos(h)) + 8 * math.sin(h))
    return s, (0, 0, 0.035 * math.cos(ph) - 0.02)


def a_annonce(t):
    return blend_pose(REST, pose_crouch(1.0, 32), ss(t / 0.3))


def a_bond(t):
    return blend_pose(pose_crouch(1.0, 32), pose_leap(), ss(t / 0.4))


def a_reception(t):
    impact = pose_crouch(1.35, 10)
    if t < 0.12:
        return blend_pose(pose_leap(), impact, ss(t / 0.12))
    return blend_pose(impact, REST, ss((t - 0.12) / 0.23))


def a_escalade(t):
    T = 0.8
    ph = 2 * math.pi * t / T
    s = {}
    add(s, "Bassin", "x", 10)
    add(s, "Dos_1", "z", 8 * math.sin(ph)); add(s, "Dos_2", "z", -6 * math.sin(ph)); add(s, "Poitrine", "z", 4 * math.sin(ph))
    add(s, "Cou", "x", -20); add(s, "Cou", "z", -4 * math.sin(ph)); add(s, "Machoire", "x", 8)
    for side, sg in (("L", 1), ("R", -1)):
        f = ph + (0 if side == "L" else math.pi)      # diagonale : avant gauche avec arrière droit
        h = ph + (math.pi if side == "L" else 0)
        add(s, "Bras." + side, "x", -38 * math.sin(f) - 22)
        add(s, "Bras." + side, "y", -12 * sg * max(0.0, math.cos(f)))
        add(s, "AvantBras." + side, "x", -32 * max(0.0, math.cos(f)) - 6)
        add(s, "Main." + side, "x", 18 * max(0.0, -math.cos(f)))
        add(s, "Doigts." + side, "x", 14 * max(0.0, -math.cos(f)))
        add(s, "Cuisse." + side, "x", -30 * math.sin(h) - 12)
        add(s, "Jambe." + side, "x", 40 * max(0.0, math.cos(h)) + 10)
        add(s, "Pied." + side, "x", -14 * max(0.0, math.cos(h)))
    return s, (0, 0, -0.10)


def a_coup(t):
    wind = {}
    add(wind, "Bras.R", "x", 40); add(wind, "Bras.R", "y", 22); add(wind, "AvantBras.R", "x", -42)
    add(wind, "Dos_2", "z", -10); add(wind, "Poitrine", "z", -8); add(wind, "Machoire", "x", 14); add(wind, "Cou", "x", -8)
    strike = {}
    add(strike, "Bras.R", "x", -72); add(strike, "Bras.R", "y", -6); add(strike, "AvantBras.R", "x", -6)
    add(strike, "Main.R", "x", -18); add(strike, "Doigts.R", "x", -14)
    add(strike, "Dos_2", "z", 12); add(strike, "Poitrine", "z", 8); add(strike, "Machoire", "x", 28); add(strike, "Cou", "x", -12)
    if t < 0.15:
        return blend_pose(REST, (wind, (0, 0, -0.02)), ss(t / 0.15))
    if t < 0.24:
        return blend_pose((wind, (0, 0, -0.02)), (strike, (0, 0, 0.0)), ss((t - 0.15) / 0.09))
    return blend_pose((strike, (0, 0, 0)), REST, ss((t - 0.24) / 0.21))


def a_etourdie(t):
    ph = 2 * math.pi * t / 1.2
    s = {}
    add(s, "Cou", "x", 14); add(s, "Tete", "y", 11 * math.sin(ph)); add(s, "Tete", "z", 8 * math.sin(ph + 1.1))
    add(s, "Machoire", "x", 14 + 5 * math.sin(2 * ph))
    add(s, "Dos_2", "y", 5 * math.sin(ph + 0.5)); add(s, "Bassin", "y", -3 * math.sin(ph + 0.5))
    both(s, "Jambe", "x", 8 + 4 * math.sin(2 * ph)); both(s, "Cuisse", "x", -5 - 2 * math.sin(2 * ph))
    both(s, "AvantBras", "x", -6); both(s, "Doigts", "x", 10)
    return s, (0.02 * math.sin(ph), 0, -0.03 + 0.01 * math.sin(2 * ph))


def a_mort(t):
    buckle = {}
    both(buckle, "Jambe", "x", 42); both(buckle, "Cuisse", "x", -30); add(buckle, "Cou", "x", -22); add(buckle, "Machoire", "x", 30)
    both(buckle, "AvantBras", "x", -20)
    down = {}
    both(down, "Jambe", "x", 55); both(down, "Cuisse", "x", -40); add(down, "Cou", "x", 10); add(down, "Machoire", "x", 22)
    both(down, "Bras", "y", 25, -1); both(down, "AvantBras", "x", -10); add(down, "Racine", "y", 84); add(down, "Dos_2", "z", 10)
    if t < 0.3:
        return blend_pose(REST, (buckle, (0, 0, -0.15)), ss(t / 0.3))
    return blend_pose((buckle, (0, 0, -0.15)), (down, (0.05, 0, -0.05)), ss((t - 0.3) / 0.5))


ANIMS = [  # nom, fonction, durée (s), boucle
    ("Goule_Repos", a_repos, 2.0, True),
    ("Goule_Course", a_course, 0.6, True),
    ("Goule_Annonce", a_annonce, 0.3, False),
    ("Goule_Bond", a_bond, 0.4, False),
    ("Goule_Reception", a_reception, 0.35, False),
    ("Goule_Escalade", a_escalade, 0.8, True),
    ("Goule_Coup", a_coup, 0.45, False),
    ("Goule_Etourdie", a_etourdie, 1.2, True),
    ("Goule_Mort", a_mort, 1.0, False),
]


def make_actions(arm_ob):
    arm_ob.animation_data_create()
    acts = []
    for name, fn, dur, loop in ANIMS:
        old = bpy.data.actions.get(name)
        if old:
            bpy.data.actions.remove(old)
        act = bpy.data.actions.new(name)
        arm_ob.animation_data.action = act
        n = int(round(dur * FPS))
        for f in range(0, n + 1):
            spec, root = fn(f / FPS if not loop or f < n else 0.0)    # boucle : dernière image = première
            apply_pose(arm_ob, spec, root)
            for pb in arm_ob.pose.bones:
                if "_Pli" in pb.name:                               # pilotés (driver) : pas de clés
                    continue
                pb.keyframe_insert("rotation_quaternion", frame=1 + f)
                pb.keyframe_insert("location", frame=1 + f)
        acts.append(act)
    arm_ob.animation_data.action = acts[0]
    apply_pose(arm_ob, {})
    return acts


def export(objs, path):
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    vl.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=False, mesh_smooth_type="OFF", use_triangles=True, use_tspace=False,
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", use_armature_deform_only=True,
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode="RELATIVE", embed_textures=False, use_custom_props=False, use_metadata=True)


def run():
    sc = C.new_scene("SK_Goule")
    sc.render.fps = FPS
    meshes = G.build(G.PARAMS)                                  # textures, LOD0..2 (1.1 m, avant -Y)
    sc2 = bpy.context.window.scene if bpy.context.window else bpy.context.scene
    for o in meshes:
        if o.name not in sc2.collection.objects:
            sc2.collection.objects.link(o)
    for o in meshes:
        o.data.transform(Matrix.Rotation(math.pi, 4, "Z"))
    arm_ob = make_armature()
    rep = skin(meshes, arm_ob)
    acts = make_actions(arm_ob)
    path = os.path.join(C.MODELS_DIR, "SK_Goule.fbx")
    export([arm_ob] + meshes, path)
    blocks = {sc2} | set(sc2.objects) | {o.data for o in sc2.objects if o.data is not None} | set(acts)
    sc2.use_fake_user = True
    bpy.data.libraries.write(os.path.join(C.SOURCE_DIR, "SK_Goule.blend"), blocks, path_remap="RELATIVE_ALL",
                             fake_user=True, compress=True)              # .blend avec les 9 actions
    print("SK_Goule : %d os ; LOD %s triangles ; %s" % (len(arm_ob.data.bones), [E.tri_count(o) for o in meshes], rep))
    print("animations : " + ", ".join("%s %.2f s%s" % (n, d, " boucle" if l else "") for n, _, d, l in ANIMS))
    return arm_ob, meshes


if __name__ == "__main__":
    run()
