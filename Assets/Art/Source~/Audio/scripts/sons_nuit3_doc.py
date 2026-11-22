# -*- coding: utf-8 -*-
"""Lignes du manifeste et table « son → événement » d'ART-HANDOFF pour les sons de la nuit du 2026-11-22 (lot 6)."""
import json, os, re
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
R = json.load(open(os.path.join(HERE, "sons_nuit3.json"), encoding="utf-8"))

# nom -> (moment du jeu, remplace, import)
EV = {
    "Musique_Chateau_Boucle": ("exploration de l'acte II (étages du château)", "TensionDrone (Alerte, Recherche) dans l'acte II", "stéréo, streaming"),
    "Musique_Jardin_Boucle": ("exploration de l'acte III (jardin)", "TensionDrone dans l'acte III", "stéréo, streaming"),
    "Musique_Boss_Boucle": ("combat contre le boss (arène de l'acte III)", "(nouveau)", "stéréo, streaming"),
    "Amb_Cave_Boucle": ("ambiance de fond de l'acte I (gouttes, vent dans les galeries), sous la musique", "(nouveau)", "stéréo, streaming, boucle"),
    "Amb_Chateau_Boucle": ("ambiance de fond de l'acte II (vent dans les fenêtres, craquements)", "(nouveau)", "stéréo, streaming, boucle"),
    "Amb_Jardin_Boucle": ("ambiance de fond de l'acte III (vent dans les arbres, corbeaux lointains)", "(nouveau)", "stéréo, streaming, boucle"),
    "Sfx_Porte_Ouverture": ("une porte s'ouvre (porte de module, de pièce, de sortie)", "(nouveau)", "mono, décompressé"),
    "Sfx_Porte_Verrouillee": ("le joueur essaie une porte verrouillée", "Refusal (porte)", "mono, décompressé"),
    "Sfx_Menu_Interaction": ("ouverture d'un menu d'interaction (porte à prix, marchand, coffre)", "(nouveau)", "mono, décompressé"),
    "Sfx_Achat": ("achat confirmé (âmes dépensées)", "(nouveau)", "mono, décompressé"),
    "Sfx_Ame_Collectee": ("une âme est aspirée par l'orbe (une variante de hauteur 0.9 à 1.1 par âme)", "SoulPickup", "mono, décompressé"),
    "Sfx_Attraper": ("télékinésie : objet attrapé", "(nouveau)", "mono, décompressé"),
    "Sfx_Relacher": ("télékinésie : objet relâché", "(nouveau)", "mono, décompressé"),
    "Sfx_Lancer": ("télékinésie : objet lancé", "(nouveau)", "mono, décompressé"),
    "Sfx_Traction_Debut": ("magnétisme : la traction s'accroche", "TractionStart", "mono, décompressé"),
    "Sfx_Traction_Boucle": ("magnétisme : traction maintenue (boucle, hauteur selon la vitesse)", "TractionHum", "mono, boucle, compressé en mémoire"),
    "Sfx_Traction_Fin": ("magnétisme : fin de la traction", "TractionEnd", "mono, décompressé"),
    "Sfx_Repulsion": ("magnétisme : répulsion", "Repel", "mono, décompressé"),
    "Sfx_Refus": ("action refusée (pas de charge, recharge en cours)", "Refusal", "mono, décompressé"),
    "Sfx_Pret": ("charge ou recharge à nouveau prête", "Ready", "mono, décompressé"),
    "Sfx_Impact_Leger": ("petit objet lancé ou balle qui touche un mur", "ImpactHit (léger)", "mono, décompressé"),
    "Sfx_Impact_Lourd": ("caisse lancée qui touche (ImpactDamage), chute d'un gros objet", "ImpactHit (lourd)", "mono, décompressé"),
    "Sfx_Caisse_Brisee": ("une caisse se brise", "Break (caisse)", "mono, décompressé"),
    "Sfx_Dash": ("dash", "(nouveau)", "mono, décompressé"),
    "Sfx_Saut": ("saut (et double saut, hauteur 1.1)", "(nouveau)", "mono, décompressé"),
    "Sfx_Atterrissage": ("atterrissage (volume selon la vitesse de chute)", "(nouveau)", "mono, décompressé"),
    "Sfx_Glissade_Boucle": ("glissade (boucle tant qu'elle dure)", "(nouveau)", "mono, boucle, compressé en mémoire"),
    "Sfx_WallJump": ("wall jump", "(nouveau)", "mono, décompressé"),
    "Sfx_Hissage": ("hissage automatique sur un rebord", "(nouveau)", "mono, décompressé"),
    "Sfx_Parade_Reussie": ("parade réussie", "Parry", "mono, décompressé"),
    "Sfx_Parade_Ratee": ("parade ratée", "(nouveau)", "mono, décompressé"),
    "Sfx_Etourdissement": ("ennemi étourdi", "Stun", "mono, décompressé"),
    "Sfx_Headshot_Critique": ("tir critique à la tête", "(nouveau)", "mono, décompressé"),
    "Sfx_Balle_Paralysante": ("balle paralysante qui touche (cible figée)", "(nouveau)", "mono, décompressé"),
    "Sfx_Explosion_Kill": ("explosion de zone sur un kill", "Kill (explosion)", "mono, décompressé"),
    "Sfx_Chute_Vide": ("chute dans le vide", "(nouveau)", "mono, décompressé"),
    "Sfx_Plongeon_Eau": ("plongeon dans l'eau", "(nouveau)", "mono, décompressé"),
    "Sfx_Coeur_Boucle": ("cœur qui bat quand le traqueur est proche : 3 paliers (volume 0.3 / 0.6 / 1 et vitesse 1 / 1.25 / 1.5 à 60 / 40 / 20 m)", "(nouveau)", "mono, boucle, compressé en mémoire"),
    "Sfx_Traqueur_Arrivee_Porte": ("le traqueur arrive par une porte (enfonce l'ouverture)", "(nouveau)", "mono, décompressé, 3D"),
    "Sfx_Boss_Cri_Phase": ("boss : changement de phase", "(nouveau)", "mono, décompressé, 3D"),
    "Sfx_Boss_Attaque_Griffe": ("boss : attaque de griffe", "(nouveau)", "mono, décompressé, 3D"),
    "Sfx_Boss_Attaque_Morsure": ("boss : attaque de morsure (bond)", "(nouveau)", "mono, décompressé, 3D"),
    "Sfx_Boss_Blessure": ("boss : blessure (point faible touché)", "(nouveau)", "mono, décompressé, 3D"),
    "Sfx_Boss_Mort": ("boss : mort", "(nouveau)", "mono, décompressé, 3D"),
    "Sfx_UI_Survol": ("interface : survol d'un bouton", "(nouveau)", "mono, décompressé"),
    "Sfx_UI_Clic": ("interface : clic, validation", "(nouveau)", "mono, décompressé"),
    "Sfx_UI_Retour": ("interface : retour, fermeture", "(nouveau)", "mono, décompressé"),
    "Sfx_Goule_Mort_Organique": ("mort d'une goule (son organique court, **à la place de l'ancien son de mort**)", "Kill (goule) et Sfx_Goule_Mort", "mono, décompressé, 3D"),
    "Sfx_Lanterne_Rallumage": ("la lanterne se rallume", "LanternRelight", "mono, décompressé"),
    "Sfx_Barillet_Recharge_Fin": ("fin du rechargement du barillet (clic)", "BarrelReloadDone", "mono, décompressé"),
    "Sfx_Vague_Cor": ("une nouvelle vague arrive", "WaveHorn", "mono, décompressé"),
}
MODELE = lambda n: "ElevenLabs Music v2.5" if n.startswith("Musique") else "Mirelo"

# ART-HANDOFF
L = ["## Sons de la nuit du 2026-11-22 (lot 6, branche `art-sons-2`)", "",
     "52 sons générés sur Higgsfield (effets et ambiances : Mirelo ; musiques : ElevenLabs Music v2.5), pour 57.6 crédits sur",
     "le plafond de 60 ; sources MP3 dans `docs/art/audio/nuit3/`, préparés par `Assets/Art/Source~/Audio/scripts/sons_nuit3.py`",
     "(silences retirés, queues de bruit coupées à −40 dB sous la crête, effets à −1 dBFS, sons d'interface de 0.45 s au plus,",
     "boucles raccordées par fondu enchaîné, battement de cœur calé sur trois battements exacts de 0.838 s, ambiances à −26 dB",
     "RMS sous la musique, musiques au niveau de la musique du menu). Avec le lot B, **ils remplacent tous les sons synthétisés",
     "provisoires de `ProceduralSfx`** (colonne « Remplace »). Le branchement se fait sur `main`.", "",
     "| Son (`Assets/Art/Audio/…`) | Moment du jeu | Remplace | Import |", "|---|---|---|---|"]
for nom, info in R.items():
    ev = EV[nom]
    L.append(f"| `{info['fichier'].replace('Assets/Art/Audio/', '').replace('.wav', '')}` ({info['duree_s']} s) | {ev[0]} | {ev[1]} | {ev[2]} |")
L += ["", "Le son de mort « aigu » des goules n'est plus utilisé : `Sfx_Goule_Mort_Organique` (court, organique) le remplace. "
      "Ambiances : une par acte, en boucle sous la musique, volume 0.6 ; musiques : fondu croisé de 2 s avec l'exploration.", ""]
sec = "\n".join(L)
p = os.path.join(ROOT, "docs", "art", "ART-HANDOFF.md")
s = open(p, encoding="utf-8").read()
pat = re.compile(re.escape(L[0]) + r"\n.*?(?=\n## |\Z)", re.S)
s = pat.sub(sec.rstrip("\n"), s) if pat.search(s) else s.rstrip("\n") + "\n\n" + sec
open(p, "w", encoding="utf-8").write(s)

# manifeste
man = os.path.join(ROOT, "docs", "assets", "MANIFEST.md")
ms = open(man, encoding="utf-8").read().rstrip("\n").split("\n")
ms = [l for l in ms if not any(l.startswith(f"| `{n}` (") for n in R)]
for nom, info in R.items():
    ms.append(f"| `{nom}` ({EV[nom][0]} ; {info['duree_s']} s) | `{info['fichier']}` | Généré sur Higgsfield ({MODELE(nom)}) la nuit du 2026-11-22, "
              f"source `{info['source']}` ; préparé par script (`Assets/Art/Source~/Audio/scripts/sons_nuit3.py`, ffmpeg + numpy, sans IA) | oui | "
              f"Contenu généré, conditions Higgsfield et du modèle audio | À confirmer par l'utilisateur (forfait Higgsfield Plus). **Outils : Higgsfield, {MODELE(nom)}**. À vérifier : liste **A** sous le tableau | sans objet (son) | sans objet | sans objet | À valider DA |")
open(man, "w", encoding="utf-8").write("\n".join(ms) + "\n")
print("ok", len(R))
