using UnityEngine;

namespace UI.MainMenuUI
{
    /// <summary>
    /// Procedurally synthesizes high-energy comic UI sound effects
    /// (punchy confirm, crisp hover, ascending rocket whoosh, descending plummet whoosh, heavy landing impact)
    /// without requiring external audio asset dependencies.
    /// </summary>
    public class MainMenuUIAudioSynthesizer : MonoBehaviour
    {
        private AudioSource _audioSource;
        private AudioClip _hoverClip;
        private AudioClip _confirmClip;
        private AudioClip _cancelClip;
        private AudioClip _ascendWhooshClip;
        private AudioClip _descendWhooshClip;
        private AudioClip _landingImpactClip;

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
            // 1. Crisp hover blip
            _hoverClip = CreateSynthClip("HoverBlip", 0.08f, (t, dur) =>
            {
                float freq = Mathf.Lerp(440f, 880f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.3f;
            });

            // 2. Punchy comic confirm click
            _confirmClip = CreateSynthClip("ConfirmPunch", 0.32f, (t, dur) =>
            {
                float freq1 = 220f;
                float freq2 = 330f;
                float freq3 = 440f;
                float env = Mathf.Exp(-t * 12f);
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 30f) * 0.45f;
                float tone = (Mathf.Sin(2f * Mathf.PI * freq1 * t) +
                              Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.7f +
                              Mathf.Sin(2f * Mathf.PI * freq3 * t) * 0.5f) * env * 0.5f;
                return tone + noise;
            });

            // 3. Crisp cancel whoosh
            _cancelClip = CreateSynthClip("CancelWhoosh", 0.16f, (t, dur) =>
            {
                float freq = Mathf.Lerp(700f, 300f, t / dur);
                float env = 1f - (t / dur);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f;
            });

            // 4. ASCENT ROCKET WHOOSH (Rising pitch + wind surge for "Naik ke Atas")
            _ascendWhooshClip = CreateSynthClip("AscendWhoosh", 0.70f, (t, dur) =>
            {
                float progress = t / dur;
                // Envelope rises then tapers off near arrival
                float env = Mathf.Sin(progress * Mathf.PI * 0.95f);
                // Sweeping rising frequency from 160Hz up to 1050Hz
                float freq = Mathf.Lerp(160f, 1050f, Mathf.Pow(progress, 1.4f));
                float jetTone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f;
                float overtone = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.2f;
                // Pinkish wind noise
                float noise = (Random.value * 2f - 1f) * 0.55f;
                return (jetTone + overtone + noise) * env * 0.85f;
            });

            // 5. DESCENT WHOOSH (Falling pitch for "Turun ke Bawah")
            _descendWhooshClip = CreateSynthClip("DescendWhoosh", 0.55f, (t, dur) =>
            {
                float progress = t / dur;
                float env = Mathf.Sin(progress * Mathf.PI);
                // Sweeping falling frequency from 950Hz down to 140Hz
                float freq = Mathf.Lerp(950f, 140f, Mathf.Pow(progress, 0.8f));
                float fallTone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.45f;
                float wind = (Random.value * 2f - 1f) * 0.5f;
                return (fallTone + wind) * env * 0.80f;
            });

            // 6. HEAVY COMIC LANDING IMPACT (Bass slam + comic thud when Title lands back)
            _landingImpactClip = CreateSynthClip("LandingImpact", 0.45f, (t, dur) =>
            {
                // Sharp comic transient click
                float transient = (Random.value * 2f - 1f) * Mathf.Exp(-t * 50f) * 0.6f;
                // Heavy deep sub-bass drop 90Hz -> 35Hz
                float bassFreq = Mathf.Lerp(90f, 35f, t / dur);
                float bassEnv = Mathf.Exp(-t * 8f);
                float bass = Mathf.Sin(2f * Mathf.PI * bassFreq * t) * bassEnv * 0.75f;
                // Mid comic body tone (160Hz)
                float midTone = Mathf.Sin(2f * Mathf.PI * 160f * t) * Mathf.Exp(-t * 18f) * 0.4f;
                return transient + bass + midTone;
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
                _audioSource.PlayOneShot(_hoverClip, 0.45f);
        }

        public void PlayConfirm()
        {
            if (_audioSource != null && _confirmClip != null)
                _audioSource.PlayOneShot(_confirmClip, 0.75f);
        }

        public void PlayCancel()
        {
            if (_audioSource != null && _cancelClip != null)
                _audioSource.PlayOneShot(_cancelClip, 0.50f);
        }

        public void PlayAscendWhoosh()
        {
            if (_audioSource != null && _ascendWhooshClip != null)
                _audioSource.PlayOneShot(_ascendWhooshClip, 0.85f);
        }

        public void PlayDescendWhoosh()
        {
            if (_audioSource != null && _descendWhooshClip != null)
                _audioSource.PlayOneShot(_descendWhooshClip, 0.80f);
        }

        public void PlayLandingImpact()
        {
            if (_audioSource != null && _landingImpactClip != null)
                _audioSource.PlayOneShot(_landingImpactClip, 0.90f);
        }

        // Backward compatibility alias
        public void PlayDiveWhoosh()
        {
            PlayAscendWhoosh();
        }
    }
}
