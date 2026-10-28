// sound effects, synthesised when the game starts so there are no audio files:
// soft paper hops and thumps, a crumple for dying, chimes for moths and the door,
// and a quiet filament hum that rises while you drag the lamp
using UnityEngine;

namespace ShadowSet
{
    public class Sfx : MonoBehaviour
    {
        AudioSource one, hum;
        AudioClip jump, land, die, win, moth, click, swap;
        public static bool Muted;
        const int RATE = 44100;

        void Awake()
        {
            one = gameObject.AddComponent<AudioSource>();
            hum = gameObject.AddComponent<AudioSource>();
            hum.loop = true; hum.volume = 0; hum.clip = Make(2f, t => (Mathf.Sin(t * 2 * Mathf.PI * 120) * 0.6f + Mathf.Sin(t * 2 * Mathf.PI * 240) * 0.25f + Rand() * 0.15f) * 0.3f);
            hum.Play();
            jump = Make(0.16f, t => Sweep(t, 0.16f, 330, 620) * Env(t, 0.16f) * 0.35f + Rand() * Env(t, 0.05f) * 0.1f);
            land = Make(0.12f, t => Rand() * Env(t, 0.12f) * Env(t, 0.12f) * 0.5f);
            die = Make(0.5f, t => (Rand() * 0.6f * Env(t, 0.5f) + Sweep(t, 0.5f, 400, 90) * 0.3f * Env(t, 0.5f)) * 0.7f);
            win = Make(1.1f, t => Chime(t, 523) + Chime(t - 0.12f, 659) + Chime(t - 0.24f, 784) + Chime(t - 0.36f, 1046));
            moth = Make(0.4f, t => Chime(t, 1318) * 0.7f + Chime(t - 0.07f, 1760) * 0.5f);
            click = Make(0.05f, t => Rand() * Env(t, 0.05f) * 0.4f);
            swap = Make(0.25f, t => Sweep(t, 0.25f, 500, 900) * Env(t, 0.25f) * 0.25f);
        }

        static System.Random rng = new System.Random(7);
        static float Rand() { return (float)rng.NextDouble() * 2 - 1; }
        static float Env(float t, float len) { return t < 0 ? 0 : Mathf.Clamp01(1 - t / len) * Mathf.Clamp01(t * 400); }
        static float Sweep(float t, float len, float f0, float f1) { float f = Mathf.Lerp(f0, f1, t / len); return Mathf.Sin(2 * Mathf.PI * f * t); }
        static float Chime(float t, float f) { return t < 0 ? 0 : (Mathf.Sin(2 * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4 * Mathf.PI * f * t)) * Mathf.Exp(-t * 6) * 0.18f; }
        static float lp;     // a gentle low-pass, so the noise sounds like paper rather than hiss

        AudioClip Make(float seconds, System.Func<float, float> f)
        {
            int n = (int)(seconds * RATE);
            var data = new float[n];
            lp = 0;
            for (int i = 0; i < n; i++) { float v = f(i / (float)RATE); lp += (v - lp) * 0.35f; data[i] = Mathf.Clamp(lp, -1, 1); }
            var clip = AudioClip.Create("sfx", n, 1, RATE, false);
            clip.SetData(data, 0);
            return clip;
        }

        void Play(AudioClip c, float vol = 1, float pitch = 1)
        {
            if (Muted) return;
            one.pitch = pitch;
            one.PlayOneShot(c, vol);
        }

        public void Jump() { Play(jump, 0.8f, Random.Range(0.95f, 1.08f)); }
        public void Land() { Play(land, 0.9f); }
        public void Die() { Play(die, 1); }
        public void Win() { Play(win, 1); }
        public void Moth() { Play(moth, 1); }
        public void Click() { Play(click, 0.6f); }
        public void Swap() { Play(swap, 0.8f); }

        // the lamp's hum gets louder and higher the faster it's moving
        public void Hum(float speed)
        {
            float target = Muted ? 0 : Mathf.Clamp01(speed / 10f) * 0.18f + 0.02f;
            hum.volume = Mathf.Lerp(hum.volume, target, 0.15f);
            hum.pitch = Mathf.Lerp(hum.pitch, 0.9f + Mathf.Clamp01(speed / 10f) * 0.5f, 0.15f);
        }
    }
}
