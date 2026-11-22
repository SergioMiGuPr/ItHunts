# -*- coding: utf-8 -*-
"""Sons de la nuit du 2026-11-22 (lot 6, branche art-sons-2) : remplacent tous les sons synthétisés provisoires
(ProceduralSfx) encore utilisés. Générés sur Higgsfield (effets et ambiances : Mirelo SFX ; musiques : ElevenLabs Music
v2.5), sources MP3 dans docs/art/audio/nuit3/, préparés comme le lot B (sons_jeu.py) :
- silences de tête et de queue retirés (-50 dBFS), fondus anti-clic sur les sons ponctuels, crête à -1 dBFS ;
- boucles (effets) : fondu enchaîné à puissance constante, crête à -1 dBFS ;
- ambiances : stéréo, boucle à fondu de 2 s, niveau RMS -26 dB (sous la musique) ;
- musiques : stéréo, boucle à fondu de 2.5 s, RMS de la musique du menu (-14.2 dB), crête limitée à -1 dBFS.
Rapport : sons_nuit3.json.

Usage : python sons_nuit3.py [--telecharger]   (ffmpeg dans le PATH)
"""
import json, os, sys, urllib.request
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import sons_jeu as J

SRC = os.path.join(J.ROOT, "docs/art/audio/nuit3")
OUT = J.OUT
RMS_AMB_DB = -26.0

# index -> (dossier/nom Unity, type, URL source)
SONS = json.load(open(os.path.join(HERE, "sons_nuit3_sources.json"), encoding="utf-8"))


def traite(nom_rel, typ, src):
    ch = 2 if typ in ("musique", "ambiance") else 1
    a = J.decode(src, ch)
    dur0 = len(a) / J.SR
    env = np.abs(a).max(1)
    nz = np.nonzero(env > 10 ** (J.SEUIL_DB / 20))[0]
    pad = int(0.010 * J.SR)
    i0, i1 = max(nz[0] - pad, 0), min(nz[-1] + 1 + pad, len(a))
    a = a[i0:i1]
    info = {"source": os.path.relpath(src, J.ROOT).replace("\\", "/"), "duree_source_s": round(dur0, 3),
            "silence_retire_s": [round(i0 / J.SR, 3), round(dur0 - i1 / J.SR, 3)]}
    if typ == "un":
        # queue de bruit coupée à 40 dB sous la crête ; interface : 0.45 s au plus (réponse immédiate, pas de traîne)
        e = np.convolve(np.abs(a).max(1), np.ones(220) / 220, "same")
        fin = int(np.nonzero(e > e.max() * 10 ** (-40 / 20))[0][-1]) + int(0.03 * J.SR)
        if nom_rel.startswith("Interface/") and "Achat" not in nom_rel:
            fin = min(fin, int(0.45 * J.SR))
        a = a[:max(fin, int(0.1 * J.SR))]
        f_in, f_out = int(0.004 * J.SR), int(0.025 * J.SR)
        a[:f_in] *= np.linspace(0, 1, f_in)[:, None]
        a[-f_out:] *= np.linspace(1, 0, f_out)[:, None]
        a = a * (10 ** (J.CRETE_DB / 20) / np.abs(a).max())
    else:
        xf = {"musique": 2.5, "ambiance": 2.0}.get(typ, min(0.5, 0.15 * len(a) / J.SR))
        if "Coeur" in nom_rel:
            # boucle calée sur trois battements exacts (sinon deux battements se suivent au raccord)
            e = np.convolve(np.abs(a).max(1), np.ones(400) / 400, "same")
            pk = []
            for k in range(1, len(e) - 1):
                if e[k] > e[k - 1] and e[k] >= e[k + 1] and e[k] > 0.35 * e.max() and (not pk or k - pk[-1] > 0.3 * J.SR):
                    pk.append(k)
            per = int(np.median(np.diff(pk)))
            d0 = max(pk[0] - int(0.08 * J.SR), 0)
            a = a[d0:d0 + 3 * per + int(0.02 * J.SR)]
            xf = 0.02
            info["periode_battement_s"] = round(per / J.SR, 3)
        a = J.boucle(a, xf)
        info["fondu_raccord_s"] = round(xf, 3)
        if typ in ("musique", "ambiance"):
            cible = J.RMS_MENU_DB if typ == "musique" else RMS_AMB_DB
            g = 10 ** (cible / 20) / J.rms(a)
            a = a * g
            s, lim = 10 ** (-6 / 20), 10 ** (J.CRETE_DB / 20)
            m = np.abs(a) > s
            a[m] = np.sign(a[m]) * (s + (lim - s) * np.tanh((np.abs(a[m]) - s) / (lim - s)))
        else:
            a = a * (10 ** (J.CRETE_DB / 20) / np.abs(a).max())
        info["saut_raccord"] = round(float(np.abs(a[-1] - a[0]).max()), 5)
    dossier, nom = nom_rel.split("/")
    os.makedirs(os.path.join(OUT, dossier), exist_ok=True)
    J.encode(os.path.join(OUT, dossier, nom + ".wav"), a)
    info.update({"fichier": "Assets/Art/Audio/%s.wav" % nom_rel, "type": typ, "canaux": ch, "duree_s": round(len(a) / J.SR, 3),
                 "crete_dbfs": round(J.db(np.abs(a).max()), 2), "rms_db": round(J.db(J.rms(a)), 1)})
    return info


def main():
    os.makedirs(SRC, exist_ok=True)
    if "--telecharger" in sys.argv:
        for idx, (nom, typ, url) in SONS.items():
            dst = os.path.join(SRC, nom.split("/")[1].lower() + ".mp3")
            if not os.path.exists(dst):
                urllib.request.urlretrieve(url, dst)
    rep = {}
    for idx, (nom, typ, url) in SONS.items():
        src = os.path.join(SRC, nom.split("/")[1].lower() + ".mp3")
        info = traite(nom, typ, src)
        rep[nom.split("/")[1]] = info
        print(nom, info["duree_s"], "s", info["crete_dbfs"], "dB", info["rms_db"], "dB", info.get("saut_raccord", ""), flush=True)
    json.dump(rep, open(os.path.join(HERE, "sons_nuit3.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
