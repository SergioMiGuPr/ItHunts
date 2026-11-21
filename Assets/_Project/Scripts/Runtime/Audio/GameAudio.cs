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

        /// <summary>Hauteur variée (±pitchVariation) pour éviter la répétition.</summary>
        public static float Pitch()
        {
            GameAudioSettings s = Settings;
            float v = s != null ? s.pitchVariation : 0.06f;
            return 1f + Random.Range(-v, v);
        }

        /// <summary>Son 3D ponctuel à cette position (source temporaire, portée des créatures, hauteur variée).</summary>
        public static void PlayAt(AudioClip clip, Vector3 position, float volume, float range = -1f)
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
            a.pitch = Pitch();
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
