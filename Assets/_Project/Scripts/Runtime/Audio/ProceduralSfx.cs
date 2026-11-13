using UnityEngine;

namespace ItHunts.Audio
{
    /// <summary>
    /// Sons provisoires synthétisés par le code (aucun fichier audio) : échantillons mono à 44.1 kHz entre −1 et 1.
    /// Les fonctions renvoient des tableaux (testables) ; ToClip en fait un AudioClip. À remplacer par de vrais sons
    /// plus tard.
    /// </summary>
    public static class ProceduralSfx
    {
        public const int SampleRate = 44100;

        /// <summary>Début de traction : « zip » montant (balayage 300 → 1100 Hz, harmonique douce).</summary>
        public static float[] TractionStart() => Sweep(0.18f, 300f, 1100f, attack: 0.01f, harmonic: 0.3f);

        /// <summary>Fin de traction : clic sec (bruit très court et petit tic aigu).</summary>
        public static float[] TractionEnd()
        {
            float[] s = new float[Samples(0.05f)];
            var rng = new System.Random(7);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Exp(-t * 120f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                s[i] = env * (0.5f * noise + 0.5f * Mathf.Sin(2f * Mathf.PI * 2000f * t));
            }

            return Normalize(s, 0.8f);
        }

        /// <summary>Répulsion : coup sourd (90 → 40 Hz) et souffle (bruit filtré qui s'éteint).</summary>
        public static float[] Repel()
        {
            float duration = 0.4f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(11);
            float phase = 0f, low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(90f, 40f, t / duration);
                phase += 2f * Mathf.PI * f / SampleRate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 9f);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.08f;                         // filtre passe-bas simple : souffle
                float whoosh = low * Mathf.Exp(-t * 7f) * Mathf.Min(1f, t / 0.02f);
                s[i] = thump + 2.5f * whoosh;
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Refus : deux bips graves (220 Hz).</summary>
        public static float[] Refusal()
        {
            float beep = 0.07f, gap = 0.05f;
            float[] s = new float[Samples(beep * 2f + gap)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float local = t < beep ? t : t - beep - gap;
                if (local < 0f || local >= beep)
                    continue;
                float env = Mathf.Min(1f, local / 0.005f) * Mathf.Min(1f, (beep - local) / 0.01f);
                float x = Mathf.Sin(2f * Mathf.PI * 220f * t);
                s[i] = env * (0.7f * x + 0.3f * Mathf.Sign(x));            // un peu « carré » : plus sec
            }

            return Normalize(s, 0.7f);
        }

        /// <summary>Charge revenue : petit tintement (1320 Hz et son octave).</summary>
        public static float[] Ready()
        {
            float[] s = new float[Samples(0.25f)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float env = Mathf.Min(1f, t / 0.003f) * Mathf.Exp(-t * 14f);
                s[i] = env * (Mathf.Sin(2f * Mathf.PI * 1320f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2640f * t));
            }

            return Normalize(s, 0.6f);
        }

        /// <summary>
        /// Bourdonnement de traction en boucle : 1 s à 110 Hz (nombre entier de périodes, boucle sans claquement),
        /// dent de scie douce (5 harmoniques). La hauteur se règle avec AudioSource.pitch.
        /// </summary>
        public static float[] TractionHum()
        {
            float[] s = new float[SampleRate];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float x = 0f;
                for (int k = 1; k <= 5; k++)
                    x += Mathf.Sin(2f * Mathf.PI * 110f * k * t) / k;
                x += 0.15f * Mathf.Sin(2f * Mathf.PI * 3f * t) * Mathf.Sin(2f * Mathf.PI * 220f * t);   // léger battement
                s[i] = x;
            }

            return Normalize(s, 0.5f);
        }

        /// <summary>
        /// Impact d'un objet sur un ennemi : coup sourd (140 → 60 Hz) et craquement (bruit bref). La hauteur se règle
        /// avec AudioSource.pitch (objet lourd : plus grave).
        /// </summary>
        public static float[] ImpactHit()
        {
            float duration = 0.25f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(23);
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = Mathf.Lerp(140f, 60f, t / duration);
                phase += 2f * Mathf.PI * f / SampleRate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 18f);
                float crack = (float)(rng.NextDouble() * 2.0 - 1.0) * Mathf.Exp(-t * 60f);
                s[i] = thump + 0.6f * crack;
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Ennemi tué : chute de hauteur (600 → 120 Hz) et souffle bref.</summary>
        public static float[] Kill()
        {
            float duration = 0.45f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(31);
            float phase = 0f, low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float u = t / duration;
                float f = Mathf.Lerp(600f, 120f, Mathf.Sqrt(u));
                phase += 2f * Mathf.PI * f / SampleRate;
                float env = Mathf.Min(1f, t / 0.005f) * (1f - u);
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.15f;
                s[i] = env * (0.8f * Mathf.Sin(phase) + 0.3f * Mathf.Sign(Mathf.Sin(phase))) + 1.5f * low * Mathf.Exp(-t * 10f);
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Objet cassé : craquement (bruit en rafales) et choc grave bref.</summary>
        public static float[] Break()
        {
            float duration = 0.35f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(41);
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float bursts = 0.5f + 0.5f * Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 38f * t));   // éclats successifs
                phase += 2f * Mathf.PI * Mathf.Lerp(110f, 50f, t / duration) / SampleRate;
                s[i] = noise * bursts * Mathf.Exp(-t * 11f) + 0.8f * Mathf.Sin(phase) * Mathf.Exp(-t * 25f);
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Ennemi étourdi : gazouillis (trois notes aiguës qui tournent).</summary>
        public static float[] Stun()
        {
            float duration = 0.5f;
            float[] s = new float[Samples(duration)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float f = 1400f + 300f * Mathf.Sin(2f * Mathf.PI * 9f * t);
                phase += 2f * Mathf.PI * f / SampleRate;
                float env = Mathf.Min(1f, t / 0.01f) * (1f - t / duration);
                s[i] = env * Mathf.Sin(phase) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 6f * t));
            }

            return Normalize(s, 0.7f);
        }

        /// <summary>Tir du barillet (D-032) : claquement (bruit très bref) et coup grave (90 → 45 Hz).</summary>
        public static float[] BarrelShot()
        {
            float duration = 0.22f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(53);
            float phase = 0f, low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.3f;
                phase += 2f * Mathf.PI * Mathf.Lerp(90f, 45f, t / duration) / SampleRate;
                s[i] = noise * Mathf.Exp(-t * 90f) + 0.8f * low * Mathf.Exp(-t * 25f) + 0.9f * Mathf.Sin(phase) * Mathf.Exp(-t * 20f);
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Début du rechargement : deux cliquetis métalliques.</summary>
        public static float[] BarrelReloadStart() => Clicks(new[] { 0f, 0.09f }, 2600f, 0.2f);

        /// <summary>Fin du rechargement : cliquetis plus grave et net (barillet refermé).</summary>
        public static float[] BarrelReloadDone() => Clicks(new[] { 0f }, 1700f, 0.12f);

        private static float[] Clicks(float[] at, float frequency, float duration)
        {
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(61);
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float x = 0f;
                foreach (float start in at)
                {
                    float u = t - start;
                    if (u < 0f)
                        continue;
                    float env = Mathf.Exp(-u * 140f);
                    x += env * (Mathf.Sin(2f * Mathf.PI * frequency * u) + 0.5f * (float)(rng.NextDouble() * 2.0 - 1.0));
                }

                s[i] = x;
            }

            return Normalize(s, 0.7f);
        }

        /// <summary>Goule qui va bondir : sifflement (bruit aigu qui monte), 0.3 s.</summary>
        public static float[] GhoulHiss()
        {
            float duration = 0.32f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(71);
            float prev = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                float hp = noise - prev;                                  // passe-haut grossier : sifflement
                prev = noise;
                float env = Mathf.Min(1f, t / 0.05f) * (1f - t / duration);
                s[i] = hp * env * (0.6f + 0.4f * t / duration);
            }

            return Normalize(s, 0.7f);
        }

        /// <summary>Pas du traqueur : coup très grave (55 → 30 Hz) et frottement.</summary>
        /// <summary>Cri aigu du volant qui annonce son piqué (provisoire, D-040).</summary>
        public static float[] FlyerScreech() => Sweep(0.35f, 2600f, 1300f, attack: 0.01f, harmonic: 0.45f);

        public static float[] StalkerStep()
        {
            float duration = 0.5f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(81);
            float phase = 0f, low = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                phase += 2f * Mathf.PI * Mathf.Lerp(55f, 30f, t / duration) / SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.05f;
                s[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 7f) + 2.5f * low * Mathf.Exp(-t * 12f);
            }

            return Normalize(s, 0.95f);
        }

        /// <summary>Souffle du traqueur, en boucle (3 s) : bruit grave qui enfle et retombe deux fois.</summary>
        public static float[] StalkerBreath()
        {
            float duration = 3f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(91);
            float low = 0f, lower = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                low += (noise - low) * 0.08f;
                lower += (low - lower) * 0.2f;
                float swell = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * 2f * t / 1.5f)), 2f);
                s[i] = lower * swell;
            }

            return Normalize(s, 0.8f);
        }

        /// <summary>Le traqueur repère le joueur : grondement qui descend (180 → 60 Hz), 1 s.</summary>
        public static float[] StalkerRoar()
        {
            float duration = 1f;
            float[] s = new float[Samples(duration)];
            var rng = new System.Random(101);
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float u = t / duration;
                phase += 2f * Mathf.PI * Mathf.Lerp(180f, 60f, u) / SampleRate;
                float rough = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.4f;
                float env = Mathf.Min(1f, t / 0.08f) * (1f - u);
                s[i] = env * (Mathf.Sign(Mathf.Sin(phase)) * 0.5f + Mathf.Sin(phase * 0.5f) + rough);
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Âme ramassée (D-043) : petit tintement clair qui monte (1400 → 2100 Hz), 0.08 s.</summary>
        public static float[] SoulPickup() => Sweep(0.08f, 1400f, 2100f, attack: 0.003f, harmonic: 0.2f);

        /// <summary>Pic de vague (D-041) : cor grave qui monte (90 → 150 Hz), 0.9 s, deux voix.</summary>
        public static float[] WaveHorn()
        {
            float duration = 0.9f;
            float[] s = new float[Samples(duration)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float u = t / duration;
                phase += 2f * Mathf.PI * Mathf.Lerp(90f, 150f, Mathf.Sqrt(u)) / SampleRate;
                float env = Mathf.Min(1f, t / 0.05f) * Mathf.Min(1f, (duration - t) / 0.25f);
                s[i] = env * (Mathf.Sin(phase) + 0.6f * Mathf.Sin(phase * 1.5f) + 0.3f * Mathf.Sin(phase * 2f));
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>Nappe de tension en boucle (4 s) : deux graves légèrement désaccordés qui battent.</summary>
        public static float[] TensionDrone()
        {
            float duration = 4f;
            float[] s = new float[Samples(duration)];
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                s[i] = Mathf.Sin(2f * Mathf.PI * 55f * t) + Mathf.Sin(2f * Mathf.PI * 55.5f * t) + 0.4f * Mathf.Sin(2f * Mathf.PI * 82.5f * t);
            }

            return Normalize(s, 0.6f);
        }

        public static AudioClip ToClip(string name, float[] samples)
        {
            var clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static int Samples(float seconds) => Mathf.CeilToInt(seconds * SampleRate);

        private static float[] Sweep(float duration, float from, float to, float attack, float harmonic)
        {
            float[] s = new float[Samples(duration)];
            float phase = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                float t = i / (float)SampleRate;
                float u = t / duration;
                float f = Mathf.Lerp(from, to, u * u);
                phase += 2f * Mathf.PI * f / SampleRate;
                float env = Mathf.Min(1f, t / attack) * (1f - u) * (1f - u);
                s[i] = env * (Mathf.Sin(phase) + harmonic * Mathf.Sin(2f * phase));
            }

            return Normalize(s, 0.8f);
        }

        /// <summary>Ramène le pic à <paramref name="peak"/>.</summary>
        private static float[] Normalize(float[] s, float peak)
        {
            float max = 0f;
            foreach (float v in s)
                max = Mathf.Max(max, Mathf.Abs(v));
            if (max > 1e-6f)
                for (int i = 0; i < s.Length; i++)
                    s[i] *= peak / max;
            return s;
        }
    }
}
