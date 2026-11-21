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


def axe_x_repos(n, a=None):
    """Axe X (latéral) de l'os n dans la pose de repos, en monde."""
    a = a or arm()
    return (a.matrix_world.to_3x3() @ a.data.bones[n].matrix_local.to_3x3()).col[0].normalized()


def viser(pb, d, ref_x=None):
    """Oriente l'os pb (axe Y de l'os) selon la direction monde d. Sans ref_x : pivot minimal depuis l'orientation héritée.
    Avec ref_x : orientation complète, l'axe X de l'os aussi proche que possible de ref_x (pas de torsion parasite)."""
    a = pb.id_data
    bone = pb.bone
    if pb.parent:
        base = pb.parent.matrix @ (pb.parent.bone.matrix_local.inverted() @ bone.matrix_local)
    else:
        base = bone.matrix_local.copy()
    base_rot = (a.matrix_world.to_3x3() @ base.to_3x3()).normalized()
    pb.rotation_mode = "QUATERNION"
    if isinstance(ref_x, str) and ref_x == "repos":
        # orientation de repos de l'os (en monde), pivotée au plus court vers d : jamais indéterminée (sauf d opposé au repos)
        rest = (a.matrix_world.to_3x3() @ bone.matrix_local.to_3x3()).normalized()
        R = rest.col[1].normalized().rotation_difference(V(d).normalized()).to_matrix()
        M = R @ rest
        pb.rotation_quaternion = (base_rot.inverted() @ M).to_quaternion()
        bpy.context.view_layer.update()
        return
    if ref_x is None:
        loc_d = base_rot.inverted() @ V(d).normalized()
        pb.rotation_quaternion = V((0, 1, 0)).rotation_difference(loc_d)
    else:
        y = V(d).normalized()
        x = V(ref_x) - y * V(ref_x).dot(y)
        if x.length < 1e-6:
            x = y.orthogonal()
        x.normalize(); z = x.cross(y).normalized()
        M = Matrix((x, y, z)).transposed()
        pb.rotation_quaternion = (base_rot.inverted() @ M).to_quaternion()
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
            d = V((0, 0, -1)) + lat + vent
            if d.z > -0.45:                       # les mèches ne remontent jamais au-dessus de l'horizontale (pas de bascule)
                d.z = -0.45
            d.normalize()
            rd = (a.matrix_world.to_3x3() @ (pb.tail - pb.head)).normalized()
            viser(pb, rd.lerp(d, poids).normalized())


# ------------------------------------------------------------------ IK analytique à deux os (positions exactes, pas de glissement)
def _ik2(n1, n2, cible, pole, a=None):
    """Oriente n1 puis n2 pour que le bout de n2 touche `cible` (monde), genou ou coude du côté de `pole` (direction monde)."""
    a = a or arm()
    p1 = a.pose.bones[n1]
    H = a.matrix_world @ p1.head
    l1 = (a.data.bones[n1].tail_local - a.data.bones[n1].head_local).length
    l2 = (a.data.bones[n2].tail_local - a.data.bones[n2].head_local).length
    D = V(cible) - H
    d = min(max(D.length, abs(l1 - l2) + 1e-4), l1 + l2 - 1e-4)
    u = D.normalized()
    pv = V(pole) - u * V(pole).dot(u)
    if pv.length < 1e-6:
        pv = V((0, -1, 0)) - u * V((0, -1, 0)).dot(u)
    v = pv.normalized()
    ca = (l1 * l1 + d * d - l2 * l2) / (2 * l1 * d)
    sa = math.sqrt(max(0.0, 1 - ca * ca))
    K = H + u * (l1 * ca) + v * (l1 * sa)
    viser(p1, K - H)
    viser(a.pose.bones[n2], (H + u * d) - K)
    return K


def ik_jambe(s, cheville, pole=V((0, -1, 0.2)), pied=None, orteils=None, a=None):
    a = a or arm()
    genou = _ik2("Cuisse_" + s, "Jambe_" + s, cheville, pole, a)
    if pied is not None:
        viser(a.pose.bones["Pied_" + s], pied, axe_x_repos("Pied_" + s, a))
    if orteils is not None:
        viser(a.pose.bones["Orteils_" + s], orteils, axe_x_repos("Orteils_" + s, a))
    return genou


def ik_bras(s, poignet, pole=V((0, 1, 0)), main=None, doigts=None, a=None, mode_main="axe"):
    a = a or arm()
    coude = _ik2("Bras_" + s, "AvantBras_" + s, poignet, pole, a)
    ref = "repos" if mode_main == "repos" else None
    if main is not None:
        viser(a.pose.bones["Main_" + s], main, ref if ref else axe_x_repos("Main_" + s, a))
    if doigts is not None:
        viser(a.pose.bones["Doigts_" + s], doigts, ref if ref else axe_x_repos("Doigts_" + s, a))
    return coude


def direction(n, a=None):
    a = a or arm()
    pb = a.pose.bones[n]
    return (a.matrix_world.to_3x3() @ (pb.tail - pb.head)).normalized()


# ------------------------------------------------------------------ actions et clés
def nouvelle_action(nom, fin, a=None, fps=30):
    a = a or arm()
    sc = bpy.context.scene
    sc.render.fps = fps; sc.render.fps_base = 1.0
    sc.frame_start = 0; sc.frame_end = fin
    old = bpy.data.actions.get(nom)
    if old:
        bpy.data.actions.remove(old)
    act = bpy.data.actions.new(nom)
    act.use_fake_user = True
    if a.animation_data is None:
        a.animation_data_create()
    a.animation_data.action = act
    try:
        if act.slots:
            a.animation_data.action_slot = act.slots[0]
    except AttributeError:
        pass
    return act


def cle(frame, a=None, os_noms=None, interp=None):
    """Clé de rotation sur les os (tous les os déformants et non déformants hors cibles IK), position de Racine et Bassin."""
    a = a or arm()
    noms = os_noms or [b.name for b in a.pose.bones if not b.name.startswith(("IK_", "Pole_", "Contact_"))]
    for n in noms:
        pb = a.pose.bones[n]
        pb.rotation_mode = "QUATERNION"
        pb.keyframe_insert("rotation_quaternion", frame=frame, group=n)
    for n in ("Racine", "Bassin"):
        a.pose.bones[n].keyframe_insert("location", frame=frame, group=n)


def lisser_quaternions(act):
    """Rend les quaternions continus (pas de saut de signe d'une clé à l'autre)."""
    a = arm()
    par_os = {}
    for fc in _fcurves(act):
        if fc.data_path.endswith("rotation_quaternion"):
            par_os.setdefault(fc.data_path, {})[fc.array_index] = fc
    for dp, fcs in par_os.items():
        if len(fcs) < 4:
            continue
        n = len(fcs[0].keyframe_points)
        prev = None
        for i in range(n):
            q = Quaternion([fcs[k].keyframe_points[i].co[1] for k in range(4)])
            if prev is not None and q.dot(prev) < 0:
                for k in range(4):
                    fcs[k].keyframe_points[i].co[1] *= -1
                q = -q
            prev = q
        for k in range(4):
            fcs[k].update()


def interpolation(act, mode="BEZIER", poignee="AUTO_CLAMPED"):
    for fc in _fcurves(act):
        for k in fc.keyframe_points:
            k.interpolation = mode
            k.handle_left_type = poignee; k.handle_right_type = poignee
        fc.update()


def mesure_glissement(debut, fin, cotes=("G", "D"), seuil=0.08, a=None, os_contact="Contact_Pied_", bout="head"):
    """Pour chaque côté : glissement horizontal maximal (m) pendant les appuis (point de contact à moins de `seuil` du sol)."""
    a = a or arm()
    sc = bpy.context.scene
    res = {}
    for s in cotes:
        pos = []
        for f in range(debut, fin + 1):
            sc.frame_set(f)
            pb = a.pose.bones[os_contact + s]
            pos.append(a.matrix_world @ (pb.head if bout == "head" else pb.tail))
        pire = 0.0; ancre = None
        for p in pos:
            if p.z < seuil:
                if ancre is None:
                    ancre = p
                pire = max(pire, (V((p.x, p.y, 0)) - V((ancre.x, ancre.y, 0))).length)
            else:
                ancre = None
        res[s] = round(pire, 4)
    return res


# ------------------------------------------------------------------ rendus de contrôle (même cadrage que la vidéo)
def camera_video(cote="profil_droite", largeur_m=21.5, u0=200.0, v0=237.0, W=427, H=240, nom="Cam_Video"):
    """Caméra orthographique calée sur une vidéo : le point monde (0, 0, 0) tombe au pixel (u0, v0) d'une image W x H ;
    largeur_m = largeur de l'image en mètres. profil_droite : la créature regarde vers la droite de l'image (caméra en -X)."""
    sc = bpy.context.scene
    cam = bpy.data.objects.get(nom) or bpy.data.objects.new(nom, bpy.data.cameras.new(nom))
    if cam.name not in sc.objects:
        sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"; cam.data.ortho_scale = largeur_m; cam.data.clip_end = 500
    k = largeur_m / W
    if cote == "profil_droite":         # écran droite = -Y (avant)
        yc = (u0 - W / 2) * k; zc = (v0 - H / 2) * k
        cam.location = (-60, yc, zc); cam.rotation_euler = (math.radians(90), 0, math.radians(-90))
    elif cote == "profil_gauche":       # caméra en +X, écran droite = +Y
        yc = -(u0 - W / 2) * k; zc = (v0 - H / 2) * k
        cam.location = (60, yc, zc); cam.rotation_euler = (math.radians(90), 0, math.radians(90))
    elif cote == "face":                # caméra devant (-Y), écran droite = -X
        xc = -(u0 - W / 2) * k; zc = (v0 - H / 2) * k
        cam.location = (xc, -60, zc); cam.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = cam
    sc.render.resolution_x = W; sc.render.resolution_y = H; sc.render.resolution_percentage = 100
    return cam


def rendre_images(dossier, f0, f1, pas=1, moteur="BLENDER_WORKBENCH", suivre=False):
    """suivre : la caméra garde sa distance à Racine le long de l'axe de déplacement (Y), comme une caméra qui accompagne."""
    import os
    sc = bpy.context.scene
    cam = sc.camera
    c0 = cam.location.copy()
    sc.frame_set(f0); y0 = point("Racine", "head").y
    os.makedirs(dossier, exist_ok=True)
    sc.render.engine = moteur
    if moteur == "BLENDER_WORKBENCH":
        sh = sc.display.shading
        sh.light = "STUDIO"; sh.color_type = "TEXTURE"; sh.show_shadows = False; sh.show_cavity = False
        sc.display.shading.background_type = "WORLD" if hasattr(sh, "background_type") else None
    sc.render.film_transparent = False
    sc.render.image_settings.file_format = "PNG"
    for f in range(f0, f1 + 1, pas):
        sc.frame_set(f)
        if suivre:
            cam.location.y = c0.y + (point("Racine", "head").y - y0)
        sc.render.filepath = os.path.join(dossier, "%04d.png" % f)
        bpy.ops.render.render(write_still=True)
    cam.location = c0


def fluidite(debut, fin, boucle=False, a=None):
    """À-coups : pour chaque os, vitesse angulaire (°/image) et variation de vitesse d'une image à l'autre (°/image²).
    Renvoie (os le plus rapide, vitesse max), (os au plus fort à-coup, variation max). Sur une boucle, la dernière image
    est comparée à la première (raccord)."""
    a = a or arm()
    sc = bpy.context.scene
    noms = [b.name for b in a.pose.bones if b.bone.use_deform]
    mats = []
    for f in range(debut, fin + 1):
        sc.frame_set(f)
        mats.append({n: (a.matrix_world @ a.pose.bones[n].matrix).to_quaternion() for n in noms})
    if boucle:
        mats.append(mats[1])         # image fin+1 = image debut+1 (fin == debut en pose)
    vmax = (None, 0.0); jmax = (None, 0.0)
    for n in noms:
        vit = []
        for i in range(1, len(mats)):
            q0, q1 = mats[i - 1][n], mats[i][n]
            ang = q0.rotation_difference(q1).angle
            vit.append(math.degrees(min(ang, 2 * math.pi - ang)))
        for i, v in enumerate(vit):
            if v > vmax[1]:
                vmax = (n, round(v, 2))
            if i > 0 and abs(v - vit[i - 1]) > jmax[1]:
                jmax = (n + "@%d" % (debut + i), round(abs(v - vit[i - 1]), 2))
    return vmax, jmax


def boucler(act):
    """Boucle sans raccord : modificateur Cycles sur chaque courbe (tangentes auto calculées à travers la jonction)."""
    for fc in _fcurves(act):
        if not any(m.type == "CYCLES" for m in fc.modifiers):
            fc.modifiers.new("CYCLES")
        fc.update()


def exporter_clip(nom, debut, fin, dossier=r"C:/Users/PC/ItHunts/ItHunts-art/Assets/Art/Animations/Traqueur_v4"):
    """Exporte l'action `nom` (armature seule, une image par clé, sans simplification) dans dossier/nom.fbx."""
    import os
    os.makedirs(dossier, exist_ok=True)
    a = arm()
    act = bpy.data.actions[nom]
    fautes = verifier_action(act)
    if fautes:
        raise RuntimeError("clip refusé (verrou du modèle) : %s" % fautes)
    a.animation_data.action = act
    try:
        if act.slots:
            a.animation_data.action_slot = act.slots[0]
    except AttributeError:
        pass
    sc = bpy.context.scene
    sc.frame_start, sc.frame_end = debut, fin
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    a.select_set(True); bpy.context.view_layer.objects.active = a
    p = os.path.join(dossier, nom + ".fbx")
    bpy.ops.export_scene.fbx(filepath=p, use_selection=True, object_types={"ARMATURE"}, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y", add_leaf_bones=False,
                             use_armature_deform_only=False, bake_anim=True, bake_anim_use_all_bones=True,
                             bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True,
                             bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
    return p


def sous_le_sol(debut, fin, a=None):
    """Point le plus bas des pieds et des griffes (z, image) : négatif = le membre passe sous le sol."""
    a = a or arm()
    sc = bpy.context.scene
    pire = (99.0, debut)
    for f in range(debut, fin + 1):
        sc.frame_set(f)
        z = min([point("Orteils_" + s).z for s in "GD"] + [point("Contact_Pied_" + s, "head").z for s in "GD"]
                + [point("Doigts_" + s).z - 0.15 for s in "GD"])
        if z < pire[0]:
            pire = (round(z, 3), f)
    return pire
