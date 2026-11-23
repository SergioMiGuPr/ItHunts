# -*- coding: utf-8 -*-
"""Cris du traqueur (nuit 4, lot 7) : cri d'apparition et cri « je t'ai vu », générés sur Higgsfield (Mirelo, 6 essais,
docs/art/audio/nuit4/) puis traités ici (ffmpeg pour décoder, numpy) :
- choix sur mesures (montée d'énergie, attaque, crête, centroïde) : apparition v1 (montée de -28 à -5 dB puis rugissement),
  variante v3 (long souffle puis rugissement) ; vu v3 (hurlement aigu, attaque à 0.45 s), variante v2 (coupée à 2.1 s) ;
- passe-haut 30 Hz (4 pôles) : plus de continu ni de sous-grave inaudible ; apparition : passe-haut 55 Hz, grave -4 dB,
  présence +7 dB à 1,8 kHz et +5 dB à 3,5 kHz (la source était surtout du grave) ;
- apparition : infra-grave discret (38 Hz, -20 dB, enveloppe du rugissement), légère saturation douce ; le cri démarre en
  moins de 0,5 s (tête coupée à -20 dB sous le maximum) ;
- vu : aspiration inversée de 0,13 s, fondue enchaînée sur 15 ms avec le cri ; Vu_01 coupé avant le second cri ;
- fondus 5 ms / 120-200 ms, niveau calé sur la RMS au-dessus de 80 Hz de Sfx_Traqueur_Cri_Enrage_01 (+2 dB : signaux forts),
  crête sous -1 dBFS ; WAV mono 44,1 kHz 16 bits (comme les autres sons du traqueur) dans Assets/Art/Audio/Traqueur/.
    python -I cris_nuit4.py
"""
import os, subprocess, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
SRC = os.path.join(ROOT, "docs", "art", "audio", "nuit4")
OUT = os.path.join(ROOT, "Assets", "Art", "Audio", "Traqueur")
SR = 44100                                                   # comme les autres sons du traqueur


def lit(path, af="highpass=f=30,highpass=f=30"):
    """Décodage mono, passe-haut 30 Hz (4 pôles : retire le continu et le sous-grave inaudible)."""
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-ar", str(SR), "-af", af, "-f", "f32le", "-"],
                         capture_output=True).stdout
    return np.frombuffer(raw, np.float32).copy()


def filtre(a, af):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-f", "f32le", "-ar", str(SR), "-ac", "1", "-i", "-", "-af", af, "-f", "f32le", "-"],
                         input=a.astype(np.float32).tobytes(), capture_output=True).stdout
    return np.frombuffer(raw, np.float32).copy()


def ecrit(path, a):
    a = np.clip(a, -1, 1)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((a * 32767).astype("<i2").tobytes())


def rms(a):
    return float(np.sqrt((a * a).mean()))


def rms80(a):
    """Niveau perçu : RMS au-dessus de 80 Hz (le grave ne gonfle plus la mesure)."""
    return rms(filtre(a, "highpass=f=80,highpass=f=80"))


def enveloppe(a, n=2205):
    return np.sqrt(np.convolve(a * a, np.ones(n) / n, "same"))


def tete(a, seuil_db=-20, marge=0.25):
    """Coupe la tête : le son commence `marge` s avant le premier passage à seuil_db sous le maximum."""
    e = enveloppe(a, 441)
    db = 20 * np.log10(e + 1e-7)
    i = int(np.argmax(db > db.max() + seuil_db))
    return a[max(0, i - int(marge * SR)):]


def fondus(a, fin_ms=120):
    a = a.copy()
    n0, n1 = int(0.005 * SR), int(fin_ms / 1000 * SR)
    a[:n0] *= np.linspace(0, 1, n0)
    a[-n1:] *= np.linspace(1, 0, n1) ** 2
    return a


def calibre(a, cible):
    a = a * (cible / max(rms80(a), 1e-6))
    lim = 10 ** (-1 / 20)
    if np.abs(a).max() > lim * 0.7:
        a = lim * np.tanh(a / lim)                                # limiteur doux borné à -1 dBFS
    return a


def apparition(a):
    """Infra-grave discret (38 Hz, -20 dB sous la crête) qui suit l'enveloppe du rugissement : le cri « remplit » la salle."""
    e = enveloppe(a, 4410); e = e / max(e.max(), 1e-6)
    t = np.arange(len(a)) / SR
    infra = np.sin(2 * np.pi * 38 * t) * e ** 1.5 * 10 ** (-20 / 20) * np.abs(a).max()
    b = a + infra
    return np.tanh(b * 1.3) / np.tanh(1.3)


def vu(a):
    """Aspiration inversée de 0,13 s avant l'attaque, fondue enchaînée sur 15 ms avec le cri (pas de saut)."""
    n, x = int(0.13 * SR), int(0.015 * SR)
    pre = a[:n * 2][::-1][:n] * np.linspace(0, 1, n) ** 2 * 0.45
    out = np.concatenate([pre, a[x:]])
    out[n - x:n] = pre[n - x:] * np.linspace(1, 0, x) + a[:x] * np.linspace(0, 1, x)
    return out


def main():
    ref = lit(os.path.join(OUT, "Sfx_Traqueur_Cri_Enrage_01.wav"))
    cible = rms80(tete(ref, -45, 0.005)) * 10 ** (2 / 20)
    choix = [("traqueur_apparition_v1", "Sfx_Traqueur_Cri_Apparition_01", apparition, 400, None),
             ("traqueur_apparition_v3", "Sfx_Traqueur_Cri_Apparition_02", apparition, 400, None),
             ("traqueur_vu_v3", "Sfx_Traqueur_Cri_Vu_01", vu, 200, 2.45),
             ("traqueur_vu_v2", "Sfx_Traqueur_Cri_Vu_02", vu, 160, 2.1)]
    for src, nom, f, fin, coupe in choix:
        # apparition : la source de Mirelo est surtout du grave ; on retire le grave (passe-haut 55 Hz, plateau bas -4 dB) et on
        # rend la présence d'un cri (+7 dB à 1,8 kHz, +5 dB à 3,5 kHz), pour qu'il porte aussi sur de petits haut-parleurs
        a = lit(os.path.join(SRC, src + ".mp3"), "highpass=f=55,highpass=f=55,lowshelf=f=150:g=-4,"
                "equalizer=f=1800:t=q:w=1.2:g=7,equalizer=f=3500:t=q:w=1:g=5") if f is apparition else lit(os.path.join(SRC, src + ".mp3"))
        a = tete(a, -20, 0.25 if f is vu else 0.45)
        if coupe: a = a[:int(coupe * SR)]
        a = f(a)
        a = fondus(calibre(a, cible), fin)
        ecrit(os.path.join(OUT, nom + ".wav"), a)
        print(nom, "%.2f s, crête %.2f dBFS, RMS>80Hz %.1f dBFS, continu %.4f" % (len(a) / SR, 20 * np.log10(np.abs(a).max()), 20 * np.log10(rms80(a)), a.mean()))
    print("référence enragé RMS>80Hz %.1f dBFS (cible +2 dB : %.1f)" % (20 * np.log10(rms80(tete(ref, -45, 0.005))), 20 * np.log10(cible)))


if __name__ == "__main__":
    main()
