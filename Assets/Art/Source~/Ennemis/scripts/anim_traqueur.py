# -*- coding: utf-8 -*-
"""Outils d'animation du traqueur v4 (Blender 5.0), sans jamais toucher au modèle.

Règles (docs/art/rendus/Traqueur/v4/EMPREINTE.md) : les clés ne portent que sur la ROTATION des os, plus la position de
`Racine` et de `Bassin`. Aucune échelle d'os différente de 1, aucun déplacement d'autre os, aucune modification du
maillage, des poids ou des formes. `verifier_action()` contrôle une action, `empreinte()` le modèle.

Méthode des poses : chaque os est orienté par une DIRECTION relevée sur la vidéo, dans le plan de profil :
(avant, haut) en unités quelconques, plus un écart latéral facultatif (vers l'extérieur du côté de l'os). Le repère du
personnage : avant = -Y, haut = +Z, gauche = +X. La rotation calculée est un pivot pur (sans torsion) depuis la pose de repos.
"""
import bpy, math
from mathutils import Vector, Matrix, Quaternion

V = Vector
ORDRE = ["Racine", "Bassin", "Colonne_1", "Colonne_2", "Colonne_3", "Colonne_4", "Cou_1", "Cou_2", "Tete", "Machoire"]
for _s in ("G", "D"):
    ORDRE += ["Clavicule_" + _s, "Bras_" + _s, "AvantBras_" + _s, "Main_" + _s, "Doigts_" + _s,
              "Cuisse_" + _s, "Jambe_" + _s, "Pied_" + _s, "Orteils_" + _s]
OS_AUTORISES_POSITION = {"Racine", "Bassin"}


def arm():
    return bpy.data.objects["Traqueur"]


def vers_monde(avant, haut, lat=0.0, cote=0):
    """(avant, haut, latéral) du plan de profil -> direction Blender. cote : +1 gauche (G), -1 droite (D)."""
    return V((lat * cote, -avant, haut)).normalized()


def remise_a_zero(a=None):
    a = a or arm()
    for pb in a.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion()
        pb.location = V()
        pb.scale = V((1, 1, 1))


def viser(pb, d):
    """Oriente l'os pb (axe Y de l'os) selon la direction monde d (pivot minimal depuis l'orientation héritée)."""
    a = pb.id_data
    bone = pb.bone
    if pb.parent:
        base = pb.parent.matrix @ (pb.parent.bone.matrix_local.inverted() @ bone.matrix_local)
    else:
        base = bone.matrix_local.copy()
    base_rot = (a.matrix_world.to_3x3() @ base.to_3x3()).normalized()
    loc_d = base_rot.inverted() @ V(d).normalized()
    pb.rotation_mode = "QUATERNION"
    pb.rotation_quaternion = V((0, 1, 0)).rotation_difference(loc_d)
    bpy.context.view_layer.update()


def tordre(pb, deg):
    """Torsion autour de l'axe de l'os (en plus du pivot)."""
    pb.rotation_quaternion = pb.rotation_quaternion @ Quaternion(V((0, 1, 0)), math.radians(deg))
    bpy.context.view_layer.update()


def poser(dirs, a=None, twist=None):
    """dirs : {os: Vector direction monde} ; appliqué dans l'ordre de la hiérarchie."""
    a = a or arm()
    for n in ORDRE:
        if n in dirs:
            viser(a.pose.bones[n], dirs[n])
            if twist and n in twist:
                tordre(a.pose.bones[n], twist[n])


def direction_repos(n, a=None):
    a = a or arm()
    b = a.data.bones[n]
    return (a.matrix_world.to_3x3() @ (b.tail_local - b.head_local)).normalized()


def point(n, bout="tail", a=None):
    a = a or arm()
    pb = a.pose.bones[n]
    return a.matrix_world @ (pb.tail if bout == "tail" else pb.head)


def au_sol(cote_pieds=("G", "D"), a=None):
    """Descend (ou monte) Racine pour que le point de contact le plus bas des pieds donnés touche z = 0."""
    a = a or arm()
    bpy.context.view_layer.update()
    z = min(point("Contact_Pied_" + s, "head", a).z for s in cote_pieds)
    a.pose.bones["Racine"].location.z -= z      # Racine : axe Z monde = -axe Y local ? -> corrigé ci-dessous
    bpy.context.view_layer.update()
    z2 = min(point("Contact_Pied_" + s, "head", a).z for s in cote_pieds)
    if abs(z2) > abs(z) * 0.5 + 1e-4:          # mauvais axe : on essaie les autres axes locaux de Racine
        a.pose.bones["Racine"].location.z += z
        rb = a.data.bones["Racine"]
        loc = rb.matrix_local.to_3x3().inverted() @ V((0, 0, -z))
        a.pose.bones["Racine"].location += loc
        bpy.context.view_layer.update()
    return min(point("Contact_Pied_" + s, "head", a).z for s in cote_pieds)


def deplacer_racine(dw, a=None):
    """Déplace Racine d'un vecteur monde dw."""
    a = a or arm()
    rb = a.data.bones["Racine"]
    a.pose.bones["Racine"].location += rb.matrix_local.to_3x3().inverted() @ V(dw)
    bpy.context.view_layer.update()


# ------------------------------------------------------------------ contrôle du modèle
def empreinte(objs=("SK_Traqueur_v4_Corps", "SK_Traqueur_v4_Tete", "SK_Traqueur_v4_Cheveux")):
    """Nombre de sommets et de triangles, boîte englobante (repos), somme des poids, formes, longueur de chaque os."""
    out = {"maillages": {}, "os": {}}
    for n in objs:
        o = bpy.data.objects[n]
        me = o.data
        co = [v.co for v in me.vertices]
        mn = [round(min(c[i] for c in co), 4) for i in range(3)]
        mx = [round(max(c[i] for c in co), 4) for i in range(3)]
        sw = round(sum(g.weight for v in me.vertices for g in v.groups), 3)
        sk = [k.name for k in me.shape_keys.key_blocks] if me.shape_keys else []
        out["maillages"][n] = dict(sommets=len(me.vertices), triangles=sum(len(p.vertices) - 2 for p in me.polygons),
                                   min=mn, max=mx, somme_poids=sw, formes=sk)
    a = arm()
    for b in a.data.bones:
        out["os"][b.name] = round((b.tail_local - b.head_local).length, 4)
    return out


def verifier_action(act):
    """Renvoie la liste des fautes : clés d'échelle, clés de position hors Racine/Bassin, os inconnus."""
    fautes = []
    a = arm()
    for fc in _fcurves(act):
        dp = fc.data_path
        if not dp.startswith("pose.bones["):
            continue
        nom = dp.split('"')[1]
        prop = dp.split(".")[-1]
        if nom not in a.pose.bones:
            fautes.append("os inconnu " + nom)
        if prop == "scale":
            vals = [k.co[1] for k in fc.keyframe_points]
            if any(abs(v - 1) > 1e-6 for v in vals):
                fautes.append("échelle %s" % nom)
        if prop == "location" and nom not in OS_AUTORISES_POSITION:
            vals = [k.co[1] for k in fc.keyframe_points]
            if any(abs(v) > 1e-6 for v in vals):
                fautes.append("position %s" % nom)
    return fautes


def _fcurves(act):
    """F-curves d'une action (Blender 5 : actions en couches / canaux)."""
    try:
        return list(act.fcurves)
    except AttributeError:
        out = []
        for layer in act.layers:
            for strip in layer.strips:
                for cb in strip.channelbags:
                    out += list(cb.fcurves)
        return out


def cheveux_gravite(a=None, poids=1.0, vent=V((0, 0, 0))):
    """Oriente les 7 chaînes de cheveux vers le bas (gravité), en gardant l'écart latéral de leur racine.
    poids : 1 = pendantes, 0 = pose de repos ; vent : petit décalage monde (inertie d'un mouvement)."""
    a = a or arm()
    for c in range(1, 8):
        for j in range(1, 5):
            n = "Cheveux_%d_%d" % (c, j)
            if n not in a.pose.bones:
                continue
            pb = a.pose.bones[n]
            h = a.matrix_world @ pb.head
            tete = a.matrix_world @ a.pose.bones["Tete"].head
            lat = V((h.x - tete.x, h.y - tete.y, 0)) * 0.15
            d = (V((0, 0, -1)) + lat + vent).normalized()
            rd = (a.matrix_world.to_3x3() @ (pb.tail - pb.head)).normalized()
            viser(pb, rd.lerp(d, poids).normalized())
