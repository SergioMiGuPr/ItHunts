using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Audio
{
    /// <summary>
    /// Accès aux sons du jeu de la DA (Resources/GameAudio_Default) : tirage d'une variante, hauteur légèrement variée, son 3D
    /// ponctuel. Un son absent renvoie null : l'appelant garde alors son son provisoire (ProceduralSfx).
    /// </summary>
    public static class GameAudio
    {
        private static GameAudioSettings _settings;
        private static bool _loaded;

        public static GameAudioSettings Settings
        {
            get
            {
                if (!_loaded || _settings == null)
                {
                    _settings = Resources.Load<GameAudioSettings>("GameAudio_Default");
                    _loaded = true;
                }

                return _settings;
            }
        }

        public static AudioClip Pick(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;
            return clips[Random.Range(0, clips.Length)];
        }

        /// <summary>
        /// Index d'une variante parmi count, jamais égal à last quand il y en a au moins deux (roll dans [0, 1[) : tire parmi les
        /// count - 1 autres. -1 sans variante.
        /// </summary>
        public static int NoRepeatIndex(int count, int last, float roll)
        {
            if (count <= 0)
                return -1;
            if (count == 1)
                return 0;
            bool hasLast = last >= 0 && last < count;
            int n = hasLast ? count - 1 : count;
            int i = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(roll) * n), 0, n - 1);
            return hasLast && i >= last ? i + 1 : i;
        }

        /// <summary>Variante au hasard sans répéter la précédente (last : index du dernier tirage, mis à jour). Ignore les cases vides.</summary>
        public static AudioClip PickNoRepeat(AudioClip[] clips, ref int last)
        {
            if (clips == null || clips.Length == 0)
                return null;
            int i = NoRepeatIndex(clips.Length, last, Random.value);
            for (int k = 0; k < clips.Length; k++, i = (i + 1) % clips.Length)
                if (clips[i] != null)
                {
                    last = i;
                    return clips[i];
                }

            return null;
        }

        /// <summary>Hauteur variée (±pitchVariation) pour éviter la répétition.</summary>
        public static float Pitch()
        {
            GameAudioSettings s = Settings;
            float v = s != null ? s.pitchVariation : 0.06f;
            return 1f + Random.Range(-v, v);
        }

        /// <summary>Son de la DA (art-sons-2) s'il existe, sinon le repli (aujourd'hui du silence : ProceduralSfx coupé).</summary>
        public static AudioClip Or(System.Func<GameAudioSettings, AudioClip> pick, AudioClip fallback)
        {
            GameAudioSettings s = Settings;
            AudioClip c = s != null ? pick(s) : null;
            return c != null ? c : fallback;
        }

        /// <summary>Son 3D ponctuel à cette position (source temporaire, portée des créatures, hauteur variée).</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume, float range = -1f) =>
            PlayAt(clip, position, volume, range, -1f);

        /// <summary>Même chose avec une variation de hauteur propre (±pitchVariation ; négative : celle des réglages).</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume, float range, float pitchVariation)
        {
            if (clip == null)
                return;
            var go = new GameObject("Son : " + clip.name);
            go.transform.position = position;
            var a = go.AddComponent<AudioSource>();
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.minDistance = 2f;
            a.maxDistance = range > 0f ? range : Settings != null ? Settings.creatureRange : 40f;
            a.dopplerLevel = 0f;
            a.pitch = pitchVariation >= 0f ? 1f + Random.Range(-pitchVariation, pitchVariation) : Pitch();
            a.PlayOneShot(clip, volume);
            Object.Destroy(go, clip.length / Mathf.Max(0.5f, a.pitch) + 0.2f);
        }

        /// <summary>Joue ce son (ou, s'il manque, le repli) sur cette source, hauteur variée.</summary>
        public static void PlayOn(AudioSource source, AudioClip clip, AudioClip fallback, float volume)
        {
            AudioClip c = clip != null ? clip : fallback;
            if (source == null || c == null)
                return;
            source.pitch = clip != null ? Pitch() : 1f;
            source.PlayOneShot(c, volume);
        }
    }
}
