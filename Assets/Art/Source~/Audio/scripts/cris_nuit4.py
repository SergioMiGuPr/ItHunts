# -*- coding: utf-8 -*-
"""Cris du traqueur (nuit 4, lot 7) : cri d'apparition et cri « je t'ai vu », générés sur Higgsfield (Mirelo, 6 essais,
docs/art/audio/nuit4/) puis traités ici (ffmpeg pour décoder, numpy) :
- choix sur mesures (montée d'énergie, attaque, crête, centroïde) : apparition v1 (montée de -28 à -5 dB puis rugissement),
  variante v3 (long souffle puis rugissement) ; vu v3 (hurlement aigu, attaque à 0.45 s), variante v2 (coupée à 2.1 s) ;
- apparition : infra-grave (38 Hz, enveloppe du rugissement) mêlé à -12 dB, légère saturation douce : le cri « remplit » la salle ;
- vu : aspiration inversée de 0.25 s avant l'attaque (le cri tiré à l'envers, fondu) : effet de surprise ;
- silence de tête coupé, fondus 5 ms / 120 ms, niveau RMS calé sur Sfx_Traqueur_Cri_Enrage_01 (+2 dB : ce sont des signaux
  forts), crête sous -1 dBFS ; WAV mono 48 kHz 16 bits dans Assets/Art/Audio/Traqueur/.
    python -I cris_nuit4.py
"""
import os, subprocess, wave
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
SRC = os.path.join(ROOT, "docs", "art", "audio", "nuit4")
OUT = os.path.join(ROOT, "Assets", "Art", "Audio", "Traqueur")
SR = 48000


def lit(path):
    raw = subprocess.run(["ffmpeg", "-v", "error", "-i", path, "-ac", "1", "-ar", str(SR), "-f", "f32le", "-"], capture_output=True).stdout
    return np.frombuffer(raw, np.float32).copy()


def ecrit(path, a):
    a = np.clip(a, -1, 1)
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((a * 32767).astype("<i2").tobytes())


def rms(a):
    return float(np.sqrt((a * a).mean()))


def enveloppe(a, n=2400):
    return np.sqrt(np.convolve(a * a, np.ones(n) / n, "same"))


def tete(a, seuil_db=-45):
    e = enveloppe(a, 480)
    db = 20 * np.log10(e + 1e-7)
    i = int(np.argmax(db > db.max() + seuil_db))
    return a[max(0, i - 240):]


def fondus(a, fin_ms=120):
    a = a.copy()
    n0, n1 = int(0.005 * SR), int(fin_ms / 1000 * SR)
    a[:n0] *= np.linspace(0, 1, n0)
    a[-n1:] *= np.linspace(1, 0, n1) ** 2
    return a


def calibre(a, cible_rms):
    a = a * (cible_rms / max(rms(a), 1e-6))
    lim = 10 ** (-1 / 20)
    if np.abs(a).max() > lim * 0.7:                       # limiteur doux au-dessus de -1 dBFS
        a = lim * np.tanh(a / lim)       # écrêtage doux borné à -1 dBFS (au-dessus de -4 dBFS seulement le genou agit)
    return a


def apparition(a):
    e = enveloppe(a, 4800); e = e / max(e.max(), 1e-6)
    t = np.arange(len(a)) / SR
    infra = np.sin(2 * np.pi * 38 * t) * e ** 1.5 * 10 ** (-12 / 20) * np.abs(a).max()
    b = a + infra
    return np.tanh(b * 1.4) / np.tanh(1.4)


def vu(a):
    n = int(0.25 * SR)
    pre = a[:n * 2][::-1][:n] * np.linspace(0, 1, n) ** 2 * 0.45
    return np.concatenate([pre, a])


def main():
    ref = lit(os.path.join(OUT, "Sfx_Traqueur_Cri_Enrage_01.wav"))
    cible = rms(tete(ref)) * 10 ** (2 / 20)
    choix = [("traqueur_apparition_v1", "Sfx_Traqueur_Cri_Apparition_01", apparition, 400, None),
             ("traqueur_apparition_v3", "Sfx_Traqueur_Cri_Apparition_02", apparition, 400, None),
             ("traqueur_vu_v3", "Sfx_Traqueur_Cri_Vu_01", vu, 160, None),
             ("traqueur_vu_v2", "Sfx_Traqueur_Cri_Vu_02", vu, 160, 2.1)]
    for src, nom, f, fin, coupe in choix:
        a = lit(os.path.join(SRC, src + ".mp3"))
        if coupe: a = a[:int(coupe * SR)]
        a = tete(a)
        a = f(a)
        a = fondus(calibre(a, cible), fin)
        ecrit(os.path.join(OUT, nom + ".wav"), a)
        print(nom, "%.2f s, crête %.2f dBFS, RMS %.1f dBFS" % (len(a) / SR, 20 * np.log10(np.abs(a).max()), 20 * np.log10(rms(a))))


if __name__ == "__main__":
    main()
