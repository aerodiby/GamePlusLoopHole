using UnityEngine;

namespace UI.MainMenuUI
{
    /// Procedurally synthesizes UI sound effects (punchy confirm, crisp hover, cancel whoosh)
    /// without requiring external audio asset dependencies.
    public class MainMenuUIAudioSynthesizer : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioClip _hoverClip;
        private AudioClip _confirmClip;
        private AudioClip _cancelClip;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _audioSource.playOnAwake = false;

            GenerateAudioClips();
        }

        private void GenerateAudioClips()
        {
            _hoverClip = CreateSynthClip("HoverBlip", 0.08f, (t, dur) =>
            {
                float freq = Mathf.Lerp(440f, 880f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
            });

            _confirmClip = CreateSynthClip("ConfirmPunch", 0.35f, (t, dur) =>
            {
                float freq1 = 220f;
                float freq2 = 330f;
                float freq3 = 440f;
                float env = Mathf.Exp(-t * 9f);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 25f) * 0.4f;
                float tone = (Mathf.Sin(2f * Mathf.PI * freq1 * t) +
                              Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.7f +
                              Mathf.Sin(2f * Mathf.PI * freq3 * t) * 0.5f) * env * 0.5f;
                return tone + noise;
            });

            _cancelClip = CreateSynthClip("CancelWhoosh", 0.18f, (t, dur) =>
            {
                float freq = Mathf.Lerp(660f, 220f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
            });
        }

        private AudioClip CreateSynthClip(string name, float duration, System.Func<float, float, float> generator)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] data = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                data[i] = Mathf.Clamp(generator(t, duration), -1f, 1f);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void PlayHover()
        {
            if (_audioSource != null && _hoverClip != null)
            {
                _audioSource.PlayOneShot(_hoverClip, 0.45f);
            }
        }

        public void PlayConfirm()
        {
            if (_audioSource != null && _confirmClip != null)
            {
                _audioSource.PlayOneShot(_confirmClip, 0.75f);
            }
        }

        public void PlayCancel()
        {
            if (_audioSource != null && _cancelClip != null)
            {
                _audioSource.PlayOneShot(_cancelClip, 0.45f);
            }
        }
    }
}
